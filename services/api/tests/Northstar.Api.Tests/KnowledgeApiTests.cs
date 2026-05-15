using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Northstar.Application.Common;
using Northstar.Application.Files;
using Northstar.Application.Knowledge;
using Northstar.Application.Security;
using Northstar.Contracts.Auth;
using Northstar.Contracts.Common;
using Northstar.Contracts.Files;
using Northstar.Contracts.Knowledge;
using Northstar.Contracts.Organizations;
using Northstar.Contracts.Security;
using Northstar.Contracts.Workspaces;
using Northstar.Domain.Files;
using Northstar.Domain.Knowledge.Activity;
using Northstar.Domain.Organizations;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;
using Northstar.Infrastructure.Security;

namespace Northstar.Api.Tests;

public sealed class KnowledgeApiTests
{
    private const string OwnerEmail = "owner@northstar.local";
    private const string OwnerPassword = "Northstar.test.123!";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task InvalidModelBinding_ReturnsNorthstarValidationEnvelope()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        using var content = new StringContent(
            """{"email":{},"password":"Northstar.test.123!"}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.ValidationError, error.Error.Code);
        Assert.Equal("Request validation failed.", error.Error.Message);
        var details = Assert.IsType<JsonElement>(error.Error.Details);
        Assert.True(details.TryGetProperty("fields", out var fields));
        Assert.NotEmpty(fields.EnumerateObject());
    }

    [Fact]
    public async Task CorsPreflight_AllowsConfiguredOriginAndAuthorizationHeader()
    {
        const string allowedOrigin = "http://localhost:5173";
        using var factory = new NorthstarApiFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = allowedOrigin
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var request = CreatePreflightRequest(allowedOrigin);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains(allowedOrigin, origins);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Headers", out var headers));
        Assert.Contains(headers, header => header.Contains("authorization", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CorsPreflight_DoesNotAllowUnconfiguredOrigin()
    {
        using var factory = new NorthstarApiFactory(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var request = CreatePreflightRequest("http://evil.local");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Bootstrap_ReturnsSeededWorkspaceAndMap()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);

        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");

        Assert.NotNull(bootstrap);
        Assert.Equal("Northstar", bootstrap.Workspace.Name);
        Assert.Equal("Atlas Library", Assert.Single(bootstrap.Spaces).Name);
        Assert.Equal(7, bootstrap.Folders.Count);
        Assert.Equal(3, bootstrap.Documents.Count);
        Assert.Contains(bootstrap.Folders, folder => folder.Title == "00. Orientation");
        Assert.Contains(bootstrap.Documents, document => document.Title == "Our Principles");
        Assert.False(string.IsNullOrWhiteSpace(bootstrap.ActiveDocumentId));
    }

    [Fact]
    public async Task WorkspaceAgenda_ReturnsDocumentBackedSchedule()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");

        var response = await client.GetAsync($"/api/v1/workspaces/{bootstrap!.Workspace.Id}/agenda?date=2026-05-07");

        response.EnsureSuccessStatusCode();
        var agenda = await response.Content.ReadFromJsonAsync<WorkspaceAgendaResponse>(JsonOptions);
        Assert.NotNull(agenda);
        Assert.Equal(bootstrap.Workspace.Id, agenda.WorkspaceId);
        Assert.Equal(new DateOnly(2026, 5, 7), agenda.Date);
        Assert.Equal("workspace", agenda.CalendarStatus);
        Assert.Contains(agenda.Today, item => item.Kind == "break" && item.StartTime == "12:00");
        Assert.Contains(agenda.Today, item =>
            item.ResourceType == "document" &&
            item.ResourceId == bootstrap.ActiveDocumentId &&
            !string.IsNullOrWhiteSpace(item.Title) &&
            !string.IsNullOrWhiteSpace(item.Category));
        Assert.NotEmpty(agenda.Upcoming);
    }

    [Fact]
    public async Task UpdateDocument_WithCurrentRevision_SucceedsAndIncrementsRevision()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");

        var request = new UpdateDocumentRequest(
            original!.Document.Revision,
            "Updated Principles",
            JsonSerializer.Deserialize<JsonElement>("""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Updated body"}]}]}"""),
            ["updated", "phase-2"]);

        var response = await client.PatchAsJsonAsync($"/api/v1/documents/{original.Document.Id}", request);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<UpdateDocumentResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Principles", updated.Document.Title);
        Assert.Equal(original.Document.Revision + 1, updated.Document.Revision);
        Assert.Contains("updated", updated.Document.Tags);
        Assert.Equal(JsonValueKind.Object, updated.Document.Content.ValueKind);
    }

    [Fact]
    public async Task UpdateDocument_WithStaleRevision_ReturnsConflict()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");

        var firstUpdate = new UpdateDocumentRequest(original!.Document.Revision, "First Update", null, null);
        var firstResponse = await client.PatchAsJsonAsync($"/api/v1/documents/{original.Document.Id}", firstUpdate);
        firstResponse.EnsureSuccessStatusCode();

        var staleUpdate = new UpdateDocumentRequest(original.Document.Revision, "Stale Update", null, null);
        var conflictResponse = await client.PatchAsJsonAsync($"/api/v1/documents/{original.Document.Id}", staleUpdate);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        var error = await conflictResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.Conflict, error.Error.Code);
        Assert.Equal("Document revision conflict.", error.Error.Message);
    }

    [Fact]
    public async Task UpdateDocument_NoOp_DoesNotIncrementRevisionOrWriteActivity()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");
        var beforeActivity = await client.GetFromJsonAsync<DocumentActivityResponse>($"/api/v1/documents/{original!.Document.Id}/activity");
        var request = new UpdateDocumentRequest(
            original.Document.Revision,
            $"  {original.Document.Title}  ",
            original.Document.Content,
            original.Document.Tags);

        var response = await client.PatchAsJsonAsync($"/api/v1/documents/{original.Document.Id}", request);

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<UpdateDocumentResponse>();
        var afterActivity = await client.GetFromJsonAsync<DocumentActivityResponse>($"/api/v1/documents/{original.Document.Id}/activity");
        Assert.NotNull(updated);
        Assert.Equal(original.Document.Revision, updated.Document.Revision);
        Assert.Equal(beforeActivity!.Items.Count, afterActivity!.Items.Count);
    }

    [Fact]
    public async Task UpdateDocument_NoOpWithStaleRevision_ReturnsConflict()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");
        var firstUpdate = new UpdateDocumentRequest(original!.Document.Revision, "First Update", null, null);
        var firstResponse = await client.PatchAsJsonAsync($"/api/v1/documents/{original.Document.Id}", firstUpdate);
        firstResponse.EnsureSuccessStatusCode();

        var staleNoOp = new UpdateDocumentRequest(original.Document.Revision, "First Update", null, null);
        var conflictResponse = await client.PatchAsJsonAsync($"/api/v1/documents/{original.Document.Id}", staleNoOp);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task DocumentContext_ReturnsVersionTrailAndBacklinks()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var principles = FindDocument(bootstrap!, "Our Principles");

        var context = await client.GetFromJsonAsync<DocumentContextResponse>($"/api/v1/documents/{principles.Id}/context");

        Assert.NotNull(context);
        Assert.NotEmpty(context.VersionTrail);
        Assert.Contains(context.VersionTrail, version => version.Version == "1.0");
        Assert.Contains(context.Backlinks, backlink => backlink.Title == "Mission & Vision");
    }

    [Fact]
    public async Task DocumentContext_FiltersInaccessibleRelatedDocumentsAndBacklinks()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var principles = FindDocument(bootstrap, "Our Principles");
        var mission = FindDocument(bootstrap, "Mission & Vision");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(client, ResourceTypes.Document, mission.Id, InheritanceModes.Restricted, LinkModes.Disabled);

        Authorize(client, viewerTokens);
        var context = await client.GetFromJsonAsync<DocumentContextResponse>($"/api/v1/documents/{principles.Id}/context");

        Assert.NotNull(context);
        Assert.DoesNotContain(context.Backlinks, backlink => backlink.Id == mission.Id);
        Assert.DoesNotContain(context.RelatedDocuments, related => related.Id == mission.Id);
    }

    [Fact]
    public async Task DocumentActivity_ReturnsTimelineItems()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var activity = await client.GetFromJsonAsync<DocumentActivityResponse>($"/api/v1/documents/{document.Id}/activity");

        Assert.NotNull(activity);
        var created = Assert.Single(activity.Items.Where(item => item.Title == "document.created"));
        Assert.NotNull(created.Actor);
        Assert.False(string.IsNullOrWhiteSpace(created.Actor.Name));
        Assert.NotNull(created.Document);
        Assert.Equal(document.Id.ToString(), created.Document.Id);
        Assert.Equal(document.Title, created.Document.Title);
        Assert.NotNull(created.Classification);
        Assert.Equal("document", created.Classification.Category);
        Assert.Equal("activity", Assert.Single(created.Classification.Surfaces));
        Assert.False(created.Classification.IsNotificationCandidate);
    }

    [Fact]
    public async Task DocumentActivity_ClassifiesOrdinaryUpdatesAsCoalescibleActivityOnly()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");
        Assert.NotNull(original);
        using var updatedContent = JsonDocument.Parse(
            """
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Updated activity classification."}]}]}
            """);
        var request = new UpdateDocumentRequest(
            original.Document.Revision,
            original.Document.Title,
            updatedContent.RootElement.Clone(),
            original.Document.Tags);

        var response = await client.PatchAsJsonAsync($"/api/v1/documents/{original.Document.Id}", request);
        response.EnsureSuccessStatusCode();

        var activity = await client.GetFromJsonAsync<DocumentActivityResponse>($"/api/v1/documents/{original.Document.Id}/activity");

        Assert.NotNull(activity);
        var updated = Assert.Single(activity.Items.Where(item => item.Title == ActivityActions.DocumentUpdated));
        Assert.NotNull(updated.Classification);
        Assert.Equal("document", updated.Classification.Category);
        Assert.Equal("low", updated.Classification.Signal);
        Assert.Equal("activity", Assert.Single(updated.Classification.Surfaces));
        Assert.True(updated.Classification.IsCoalescible);
        Assert.False(updated.Classification.IsNotificationCandidate);
        Assert.False(updated.Classification.IsAuditCandidate);
        Assert.NotNull(updated.Classification.CoalescingKey);
        Assert.Contains(
            original.Document.Id.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase),
            updated.Classification.CoalescingKey,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DocumentVersions_PublishCreatesImmutablePublishedSnapshot()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");
        var publishedContent = Json("""
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Published version body."}]}]}
            """);
        var draftContent = Json("""
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Later draft body."}]}]}
            """);
        var update = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{original!.Document.Id}",
            new UpdateDocumentRequest(original.Document.Revision, original.Document.Title, publishedContent, original.Document.Tags));
        update.EnsureSuccessStatusCode();
        var updated = await update.Content.ReadFromJsonAsync<UpdateDocumentResponse>();
        Assert.NotNull(updated);

        var publish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}/versions/publish",
            new PublishDocumentVersionRequest(updated.Document.Revision, null));

        publish.EnsureSuccessStatusCode();
        var published = await publish.Content.ReadFromJsonAsync<PublishDocumentVersionResponse>();
        Assert.NotNull(published);
        Assert.Equal("published", published.Document.Status);
        Assert.Equal(updated.Document.Revision, published.Document.Revision);
        Assert.Equal(2, published.Version.VersionNo);
        Assert.Equal("2.0", published.Version.Label);
        Assert.Equal("published", published.Version.VersionType);
        Assert.NotNull(published.Version.PublishedAt);

        var versionDetail = await client.GetFromJsonAsync<DocumentVersionResponse>(
            $"/api/v1/documents/{original.Document.Id}/versions/{published.Version.Id}");
        var versions = await client.GetFromJsonAsync<DocumentVersionsResponse>(
            $"/api/v1/documents/{original.Document.Id}/versions");
        var draftUpdate = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}",
            new UpdateDocumentRequest(published.Document.Revision, published.Document.Title, draftContent, published.Document.Tags));
        draftUpdate.EnsureSuccessStatusCode();
        var draft = await draftUpdate.Content.ReadFromJsonAsync<UpdateDocumentResponse>();
        var versionAfterDraft = await client.GetFromJsonAsync<DocumentVersionResponse>(
            $"/api/v1/documents/{original.Document.Id}/versions/{published.Version.Id}");
        var activity = await client.GetFromJsonAsync<DocumentActivityResponse>(
            $"/api/v1/documents/{original.Document.Id}/activity");

        Assert.NotNull(versionDetail);
        Assert.NotNull(versions);
        Assert.Contains(versions.Versions, version => version.Label == "1.0" && version.VersionType == "system");
        Assert.Contains(versions.Versions, version => version.Id == published.Version.Id);
        Assert.Contains("Published version body.", versionDetail.Content.GetRawText(), StringComparison.Ordinal);
        Assert.NotNull(draft);
        Assert.Equal("draft", draft.Document.Status);
        Assert.Equal(published.Document.Revision + 1, draft.Document.Revision);
        Assert.NotNull(versionAfterDraft);
        Assert.Contains("Published version body.", versionAfterDraft.Content.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("Later draft body.", versionAfterDraft.Content.GetRawText(), StringComparison.Ordinal);
        Assert.NotNull(activity);
        Assert.Contains(activity.Items, item => item.Title == ActivityActions.DocumentVersionPublished);
    }

    [Fact]
    public async Task DocumentVersions_UnpublishClearsPublishedStateButKeepsVersion()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");
        var publish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{original!.Document.Id}/versions/publish",
            new PublishDocumentVersionRequest(original.Document.Revision, "release-ready"));
        publish.EnsureSuccessStatusCode();
        var published = await publish.Content.ReadFromJsonAsync<PublishDocumentVersionResponse>();
        Assert.NotNull(published);

        var unpublish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}/versions/unpublish",
            new UnpublishDocumentVersionRequest(published.Document.Revision));

        unpublish.EnsureSuccessStatusCode();
        var unpublished = await unpublish.Content.ReadFromJsonAsync<UnpublishDocumentVersionResponse>();
        var versionAfterUnpublish = await client.GetFromJsonAsync<DocumentVersionResponse>(
            $"/api/v1/documents/{original.Document.Id}/versions/{published.Version.Id}");
        var activity = await client.GetFromJsonAsync<DocumentActivityResponse>(
            $"/api/v1/documents/{original.Document.Id}/activity");

        Assert.NotNull(unpublished);
        Assert.Equal("draft", unpublished.Document.Status);
        Assert.Equal(published.Document.Revision, unpublished.Document.Revision);
        Assert.NotNull(unpublished.UnpublishedVersion);
        Assert.Equal(published.Version.Id, unpublished.UnpublishedVersion.Id);
        Assert.NotNull(versionAfterUnpublish);
        Assert.Equal("release-ready", versionAfterUnpublish.Version.Label);
        Assert.NotNull(activity);
        Assert.Contains(activity.Items, item => item.Title == ActivityActions.DocumentVersionUnpublished);
    }

    [Fact]
    public async Task DocumentVersions_CompareReturnsTextDiffBetweenVersionAndDraft()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");
        var publishedContent = Json("""
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Alpha beta. Redo Log 的核心作用是事务持久化。Redo Log 的组成结构在 InnoDB 中是循环日志。"}]}]}
            """);
        var draftContent = Json("""
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Alpha gamma. Redo Log 的核心作用是确保事务持久化。Redo Log 的组成结构在 InnoDB 中是循环日志。"}]}]}
            """);
        var update = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{original!.Document.Id}",
            new UpdateDocumentRequest(original.Document.Revision, original.Document.Title, publishedContent, original.Document.Tags));
        update.EnsureSuccessStatusCode();
        var updated = await update.Content.ReadFromJsonAsync<UpdateDocumentResponse>();
        Assert.NotNull(updated);
        var publish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}/versions/publish",
            new PublishDocumentVersionRequest(updated.Document.Revision, "alpha-beta"));
        publish.EnsureSuccessStatusCode();
        var published = await publish.Content.ReadFromJsonAsync<PublishDocumentVersionResponse>();
        Assert.NotNull(published);
        var draftUpdate = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}",
            new UpdateDocumentRequest(published.Document.Revision, published.Document.Title, draftContent, published.Document.Tags));
        draftUpdate.EnsureSuccessStatusCode();

        var compare = await client.PostAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}/versions/compare",
            new CompareDocumentVersionsRequest(
                new DocumentVersionCompareTargetDto("version", published.Version.Id),
                new DocumentVersionCompareTargetDto("draft", null)));

        compare.EnsureSuccessStatusCode();
        var result = await compare.Content.ReadFromJsonAsync<CompareDocumentVersionsResponse>();

        Assert.NotNull(result);
        Assert.Equal("alpha-beta", result.Summary.FromLabel);
        Assert.Equal("Current draft", result.Summary.ToLabel);
        Assert.True(result.Summary.TextChanged);
        Assert.Contains(result.Segments, segment => segment.Kind == "removed" && segment.Text.Contains("Alpha beta", StringComparison.Ordinal));
        Assert.Contains(result.Segments, segment => segment.Kind == "added" && segment.Text.Contains("Alpha gamma", StringComparison.Ordinal));
        Assert.Contains(result.Lines, line =>
            line.Kind == "modified"
            && line.LeftText is not null
            && line.LeftText.Contains("Redo Log 的核心作用", StringComparison.Ordinal)
            && line.RightTokens.Any(token => token.Kind == "added" && token.Text.Contains("确保", StringComparison.Ordinal)));
        Assert.Contains(result.Lines, line =>
            line.Kind == "unchanged"
            && line.LeftText is not null
            && line.LeftText.Contains("Redo Log 的组成结构", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DocumentVersions_CompareRejectsInvalidTargetAndCrossDocumentVersion()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var firstDocument = FindDocument(bootstrap, "Mission & Vision");
        var secondDocument = FindDocument(bootstrap, "Our Principles");
        var first = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{firstDocument.Id}");
        var second = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{secondDocument.Id}");
        Assert.NotNull(first);
        Assert.NotNull(second);
        var secondPublish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{secondDocument.Id}/versions/publish",
            new PublishDocumentVersionRequest(second.Document.Revision, "other-document"));
        secondPublish.EnsureSuccessStatusCode();
        var secondPublished = await secondPublish.Content.ReadFromJsonAsync<PublishDocumentVersionResponse>();
        Assert.NotNull(secondPublished);

        var invalidTarget = await client.PostAsJsonAsync(
            $"/api/v1/documents/{firstDocument.Id}/versions/compare",
            new CompareDocumentVersionsRequest(
                new DocumentVersionCompareTargetDto("snapshot", null),
                new DocumentVersionCompareTargetDto("draft", null)));
        var crossDocumentTarget = await client.PostAsJsonAsync(
            $"/api/v1/documents/{firstDocument.Id}/versions/compare",
            new CompareDocumentVersionsRequest(
                new DocumentVersionCompareTargetDto("version", secondPublished.Version.Id),
                new DocumentVersionCompareTargetDto("draft", null)));

        Assert.Equal(HttpStatusCode.BadRequest, invalidTarget.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossDocumentTarget.StatusCode);
    }

    [Fact]
    public async Task DocumentVersions_RestoreRequiresFreshRevisionAndRestoresSnapshotToDraft()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{bootstrap!.ActiveDocumentId}");
        var versionContent = Json("""
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Version content to restore."}]}]}
            """);
        var laterContent = Json("""
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Work in progress after publish."}]}]}
            """);
        var update = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{original!.Document.Id}",
            new UpdateDocumentRequest(original.Document.Revision, original.Document.Title, versionContent, original.Document.Tags));
        update.EnsureSuccessStatusCode();
        var updated = await update.Content.ReadFromJsonAsync<UpdateDocumentResponse>();
        Assert.NotNull(updated);
        var publish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}/versions/publish",
            new PublishDocumentVersionRequest(updated.Document.Revision, "customer-ready"));
        publish.EnsureSuccessStatusCode();
        var published = await publish.Content.ReadFromJsonAsync<PublishDocumentVersionResponse>();
        Assert.NotNull(published);
        var laterUpdate = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}",
            new UpdateDocumentRequest(published.Document.Revision, published.Document.Title, laterContent, published.Document.Tags));
        laterUpdate.EnsureSuccessStatusCode();
        var later = await laterUpdate.Content.ReadFromJsonAsync<UpdateDocumentResponse>();
        Assert.NotNull(later);

        var staleRestore = await client.PostAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}/versions/{published.Version.Id}/restore",
            new RestoreDocumentVersionRequest(published.Document.Revision));
        var restore = await client.PostAsJsonAsync(
            $"/api/v1/documents/{original.Document.Id}/versions/{published.Version.Id}/restore",
            new RestoreDocumentVersionRequest(later.Document.Revision));

        Assert.Equal(HttpStatusCode.Conflict, staleRestore.StatusCode);
        restore.EnsureSuccessStatusCode();
        var restored = await restore.Content.ReadFromJsonAsync<RestoreDocumentVersionResponse>();
        var current = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{original.Document.Id}");
        var activity = await client.GetFromJsonAsync<DocumentActivityResponse>(
            $"/api/v1/documents/{original.Document.Id}/activity");

        Assert.NotNull(restored);
        Assert.NotNull(current);
        Assert.Equal("customer-ready", restored.RestoredFrom.Label);
        Assert.Equal(later.Document.Revision + 1, restored.Document.Revision);
        Assert.Equal("draft", restored.Document.Status);
        Assert.Contains("Version content to restore.", current.Document.Content.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("Work in progress after publish.", current.Document.Content.GetRawText(), StringComparison.Ordinal);
        Assert.NotNull(activity);
        Assert.Contains(activity.Items, item => item.Title == ActivityActions.DocumentVersionRestored);
    }

    [Fact]
    public async Task DocumentVersions_ViewerCanReadButCannotPublishOrRestore()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");
        var current = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var publish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/versions/publish",
            new PublishDocumentVersionRequest(current!.Document.Revision, "owner-published"));
        publish.EnsureSuccessStatusCode();
        var published = await publish.Content.ReadFromJsonAsync<PublishDocumentVersionResponse>();
        Assert.NotNull(published);

        Authorize(client, viewerTokens);
        var versions = await client.GetAsync($"/api/v1/documents/{document.Id}/versions");
        var version = await client.GetAsync($"/api/v1/documents/{document.Id}/versions/{published.Version.Id}");
        var compare = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/versions/compare",
            new CompareDocumentVersionsRequest(
                new DocumentVersionCompareTargetDto("version", published.Version.Id),
                new DocumentVersionCompareTargetDto("draft", null)));
        var viewerPublish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/versions/publish",
            new PublishDocumentVersionRequest(published.Document.Revision, "viewer-published"));
        var viewerUnpublish = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/versions/unpublish",
            new UnpublishDocumentVersionRequest(published.Document.Revision));
        var viewerRestore = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/versions/{published.Version.Id}/restore",
            new RestoreDocumentVersionRequest(published.Document.Revision));

        Assert.Equal(HttpStatusCode.OK, versions.StatusCode);
        Assert.Equal(HttpStatusCode.OK, version.StatusCode);
        Assert.Equal(HttpStatusCode.OK, compare.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerPublish.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerUnpublish.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerRestore.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsMatchesByTitleAndBody_AndEmptyQueryReturnsEmptyResults()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");

        var titleSearch = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=Mission&spaceId={bootstrap!.ActiveSpaceId}");
        var bodySearch = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=planning%20rhythms&spaceId={bootstrap.ActiveSpaceId}");
        var emptySearch = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=&spaceId={bootstrap.ActiveSpaceId}");

        Assert.NotNull(titleSearch);
        Assert.Contains(titleSearch.Results, result => result.Title == "Mission & Vision");
        Assert.NotNull(bodySearch);
        Assert.Contains(bodySearch.Results, result => result.Title == "Operating System");
        Assert.NotNull(emptySearch);
        Assert.Empty(emptySearch.Results);
    }

    [Fact]
    public async Task SearchIndexMaintenance_RebuildsMissingRowsAndRemovesInactiveRows()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var mission = FindDocument(bootstrap, "Mission & Vision");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
            dbContext.DocumentSearchIndexes.RemoveRange(dbContext.DocumentSearchIndexes);
            await dbContext.SaveChangesAsync();

            var maintenance = scope.ServiceProvider.GetRequiredService<ISearchIndexMaintenanceService>();
            var rebuild = await maintenance.RebuildAsync(Guid.Parse(bootstrap.ActiveSpaceId));

            Assert.Equal(bootstrap.Documents.Count, rebuild.Created);
            Assert.Equal(0, rebuild.Removed);
            Assert.Equal(bootstrap.Documents.Count, rebuild.ActiveDocuments);
        }

        var rebuiltSearch = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=Mission&spaceId={bootstrap.ActiveSpaceId}");
        Assert.NotNull(rebuiltSearch);
        Assert.Contains(rebuiltSearch.Results, result => result.Id == mission.Id);

        var archiveResponse = await client.PatchAsync($"/api/v1/documents/{mission.Id}/archive", null);
        archiveResponse.EnsureSuccessStatusCode();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
            await dbContext.DocumentSearchIndexes.AddAsync(new Northstar.Infrastructure.Search.DocumentSearchIndex(
                Guid.Parse(mission.Id),
                Guid.Parse(bootstrap.Workspace.Id),
                Guid.Parse(bootstrap.ActiveSpaceId),
                mission.Title,
                "stale archived document"));
            await dbContext.SaveChangesAsync();

            var maintenance = scope.ServiceProvider.GetRequiredService<ISearchIndexMaintenanceService>();
            var repair = await maintenance.RebuildAsync(Guid.Parse(bootstrap.ActiveSpaceId));

            Assert.Equal(1, repair.Removed);
        }

        var archivedSearch = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=Mission&spaceId={bootstrap.ActiveSpaceId}");
        Assert.NotNull(archivedSearch);
        Assert.DoesNotContain(archivedSearch.Results, result => result.Id == mission.Id);
    }

    [Fact]
    public async Task ArchiveDocument_HidesFromDefaultViewsAndSearch_ButDirectGetReturnsArchived()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Mission & Vision");

        var archiveResponse = await client.PatchAsync($"/api/v1/documents/{document.Id}/archive", null);
        var secondArchiveResponse = await client.PatchAsync($"/api/v1/documents/{document.Id}/archive", null);
        var afterBootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var map = await client.GetFromJsonAsync<KnowledgeMapResponse>($"/api/v1/spaces/{bootstrap!.ActiveSpaceId}/map");
        var search = await client.GetFromJsonAsync<SearchResponse>($"/api/v1/search?q=Mission&spaceId={bootstrap.ActiveSpaceId}");
        var directGet = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var archivedActivityCount = await CountActivityAsync(factory, document.Id, ActivityActions.DocumentArchived);

        archiveResponse.EnsureSuccessStatusCode();
        secondArchiveResponse.EnsureSuccessStatusCode();
        Assert.NotNull(afterBootstrap);
        Assert.DoesNotContain(afterBootstrap.Documents, item => item.Id == document.Id);
        Assert.NotNull(map);
        Assert.DoesNotContain(map.Documents, item => item.Id == document.Id);
        Assert.NotNull(search);
        Assert.DoesNotContain(search.Results, item => item.Id == document.Id);
        Assert.NotNull(directGet);
        Assert.Equal("archived", directGet.Document.Status);
        Assert.Equal(1, archivedActivityCount);
    }

    [Fact]
    public async Task ViewerCannotArchiveRestoreOrDelete()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");

        Authorize(client, viewerTokens);
        var archiveResponse = await client.PatchAsync($"/api/v1/documents/{document.Id}/archive", null);
        var restoreResponse = await client.PatchAsync($"/api/v1/documents/{document.Id}/restore", null);
        var deleteResponse = await client.DeleteAsync($"/api/v1/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, archiveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, restoreResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_HidesDirectSearchAndContextLinks_AndIsIdempotent()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var principles = FindDocument(bootstrap!, "Our Principles");
        var mission = FindDocument(bootstrap!, "Mission & Vision");

        var deleteResponse = await client.DeleteAsync($"/api/v1/documents/{principles.Id}");
        var secondDeleteResponse = await client.DeleteAsync($"/api/v1/documents/{principles.Id}");
        var directGet = await client.GetAsync($"/api/v1/documents/{principles.Id}");
        var search = await client.GetFromJsonAsync<SearchResponse>($"/api/v1/search?q=principles&spaceId={bootstrap!.ActiveSpaceId}");
        var missionContext = await client.GetFromJsonAsync<DocumentContextResponse>($"/api/v1/documents/{mission.Id}/context");
        var deletedActivityCount = await CountActivityAsync(factory, principles.Id, ActivityActions.DocumentDeleted);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondDeleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, directGet.StatusCode);
        Assert.NotNull(search);
        Assert.DoesNotContain(search.Results, result => result.Id == principles.Id);
        Assert.NotNull(missionContext);
        Assert.DoesNotContain(missionContext.RelatedDocuments, related => related.Id == principles.Id);
        Assert.DoesNotContain(missionContext.Backlinks, backlink => backlink.Id == principles.Id);
        Assert.Equal(1, deletedActivityCount);
    }

    [Fact]
    public async Task UpdateDocumentContent_RebuildsSearchIndexAndBacklinks()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var principles = FindDocument(bootstrap!, "Our Principles");
        var operatingSystem = FindDocument(bootstrap!, "Operating System");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{principles.Id}");
        var marker = $"phase-three-searchable-{Guid.NewGuid():N}";
        var content = JsonSerializer.Deserialize<JsonElement>($$"""
        {
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                {
                  "type": "text",
                  "text": "{{marker}} references "
                },
                {
                  "type": "text",
                  "text": "Operating System",
                  "marks": [
                    {
                      "type": "link",
                      "attrs": {
                        "href": "/documents/{{operatingSystem.Id}}"
                      }
                    }
                  ]
                }
              ]
            }
          ]
        }
        """);
        var request = new UpdateDocumentRequest(original!.Document.Revision, null, content, null);

        var response = await client.PatchAsJsonAsync($"/api/v1/documents/{principles.Id}", request);

        response.EnsureSuccessStatusCode();
        var search = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q={marker}&spaceId={bootstrap!.ActiveSpaceId}");
        var context = await client.GetFromJsonAsync<DocumentContextResponse>(
            $"/api/v1/documents/{operatingSystem.Id}/context");

        Assert.NotNull(search);
        Assert.Contains(search.Results, result => result.Id == principles.Id);
        Assert.NotNull(context);
        Assert.Contains(context.Backlinks, backlink => backlink.Id == principles.Id);
    }

    [Fact]
    public async Task Login_WithOwnerCredentials_ReturnsTokens()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var tokens = await LoginOwnerAsync(client);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.Equal(OwnerEmail, tokens.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(OwnerEmail, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Seed_RepairsOwnerCredentialWhenConfiguredPasswordChanges()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        client.DefaultRequestHeaders.Authorization = null;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
            var passwordHashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
            var user = await dbContext.Users.SingleAsync(user => user.Email == OwnerEmail);
            var credential = await dbContext.UserCredentials.SingleAsync(credential => credential.UserId == user.Id);
            credential.UpdatePassword(passwordHashService.HashPassword(user, "old-local-password"));
            await dbContext.SaveChangesAsync();
        }

        var staleResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(OwnerEmail, OwnerPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<INorthstarDataSeeder>();
            await seeder.SeedAsync();
        }

        var repairedTokens = await LoginOwnerAsync(client);
        Assert.Equal(OwnerEmail, repairedTokens.User.Email);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndRejectsOldRefreshToken()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var tokens = await LoginOwnerAsync(client);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));
        refreshResponse.EnsureSuccessStatusCode();
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();

        var reuseResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));

        Assert.NotNull(refreshed);
        Assert.NotEqual(tokens.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var tokens = await LoginOwnerAsync(client);

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new LogoutRequest(tokens.RefreshToken));
        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidRefreshTokenWithoutAccessToken_ReturnsNoContentAndRevokesToken()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var tokens = await LoginOwnerAsync(client);
        client.DefaultRequestHeaders.Authorization = null;

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new LogoutRequest(tokens.RefreshToken));
        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithInvalidRefreshToken_ReturnsNoContent()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new LogoutRequest("invalid-refresh-token"));

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task IdpLogin_DisabledBoundaryReturnsForbidden()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/idp/login",
            new IdpLoginRequest("okta", "u-alpha", "alpha@example.test", "Alpha User"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.Forbidden, error.Error.Code);
    }

    [Fact]
    public async Task IdpLogin_BindsExistingLocalEmailUserAndDoesNotCreateWorkspaceMembership()
    {
        using var factory = new NorthstarApiFactory(IdpLoginConfiguration());
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        client.DefaultRequestHeaders.Authorization = null;
        var email = $"idp-bind-{Guid.NewGuid():N}@northstar.local";
        var localTokens = await RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/idp/login",
            new IdpLoginRequest(" OKTA ", " subject-bind ", email.ToUpperInvariant(), "External Alpha"));

        response.EnsureSuccessStatusCode();
        var idpTokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(idpTokens);
        Assert.Equal(localTokens.User.Id, idpTokens.User.Id);
        Assert.Equal(email, idpTokens.User.Email);
        Assert.Equal("External Alpha", idpTokens.User.DisplayName);
        Assert.NotNull(bootstrap);
        Assert.False(await UserIsWorkspaceMemberAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            Guid.Parse(idpTokens.User.Id)));

        var externalUser = await ReadExternalUserAsync(factory, "okta", "subject-bind");
        Assert.NotNull(externalUser);
        Assert.Equal(email, externalUser.Email);
        Assert.Equal("External Alpha", externalUser.DisplayName);
    }

    [Fact]
    public async Task IdpLogin_ExistingExternalIdentityReturnsTokens()
    {
        using var factory = new NorthstarApiFactory(IdpLoginConfiguration());
        var client = factory.CreateClient();
        var email = $"idp-existing-{Guid.NewGuid():N}@northstar.local";
        var localTokens = await RegisterAsync(client, email);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/idp/login",
            new IdpLoginRequest("okta", "existing-subject", email, "External User"));
        firstResponse.EnsureSuccessStatusCode();
        var secondResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/idp/login",
            new IdpLoginRequest("OKTA", "existing-subject", email, "External User Updated"));

        secondResponse.EnsureSuccessStatusCode();
        var idpTokens = await secondResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(idpTokens);
        Assert.Equal(localTokens.User.Id, idpTokens.User.Id);
        Assert.Equal("External User Updated", idpTokens.User.DisplayName);
    }

    [Fact]
    public async Task IdpLogin_RejectsConflictingExternalIdentity()
    {
        using var factory = new NorthstarApiFactory(IdpLoginConfiguration());
        var client = factory.CreateClient();
        var email = $"idp-conflict-{Guid.NewGuid():N}@northstar.local";
        await RegisterAsync(client, email);
        var bindResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/idp/login",
            new IdpLoginRequest("okta", "subject-a", email, "Conflict User"));
        bindResponse.EnsureSuccessStatusCode();

        var conflictResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/idp/login",
            new IdpLoginRequest("okta", "subject-b", email, "Conflict User"));

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        var error = await conflictResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.Conflict, error.Error.Code);
    }

    [Fact]
    public async Task IdpLogin_RejectsUnlinkedIdentityWithoutCreatingUser()
    {
        using var factory = new NorthstarApiFactory(IdpLoginConfiguration());
        var client = factory.CreateClient();
        var beforeCount = await CountExternalUsersAsync(factory, "okta");

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/idp/login",
            new IdpLoginRequest("okta", "unlinked-subject", "unlinked@example.test", "Unlinked User"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(beforeCount, await CountExternalUsersAsync(factory, "okta"));
    }

    [Fact]
    public async Task IdpLogin_WritesSecretSafeAuthEventAndUpdatesRecentAuth()
    {
        using var factory = new NorthstarApiFactory(IdpLoginConfiguration());
        var client = factory.CreateClient();
        var email = $"idp-events-{Guid.NewGuid():N}@northstar.local";
        await RegisterAsync(client, email);
        var externalSubjectId = $"subject-{Guid.NewGuid():N}";

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/idp/login",
            new IdpLoginRequest("okta", externalSubjectId, email, "Event User"));
        loginResponse.EnsureSuccessStatusCode();
        var idpTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(idpTokens);
        Authorize(client, idpTokens);
        var state = await client.GetFromJsonAsync<AuthSecurityStateResponse>("/api/v1/auth/security-state");

        Assert.NotNull(state);
        Assert.True(state.HasRecentAuth);
        Assert.False(state.MfaEnabled);
        var authEvent = await ReadLatestAuthEventAsync(factory, "auth.idp_login", Guid.Parse(idpTokens.User.Id));
        Assert.NotNull(authEvent);
        Assert.True(authEvent.Succeeded);
        Assert.Contains("okta", authEvent.Metadata, StringComparison.Ordinal);
        Assert.DoesNotContain(externalSubjectId, authEvent.Metadata, StringComparison.Ordinal);
        Assert.DoesNotContain(email, authEvent.Metadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", authEvent.Metadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", authEvent.Metadata, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecurityState_RequiresAuthentication()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/security-state");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecurityState_ReturnsBackendRecentAuthAndNoMfaProvider()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var beforeLogin = DateTimeOffset.UtcNow;
        var tokens = await LoginOwnerAsync(client);
        client.DefaultRequestHeaders.Add("X-Recent-Auth-At", DateTimeOffset.UtcNow.AddYears(20).ToString("O"));

        var response = await client.GetFromJsonAsync<AuthSecurityStateResponse>("/api/v1/auth/security-state");

        Assert.NotNull(response);
        Assert.Equal(tokens.User.Id, response.UserId);
        Assert.True(response.HasRecentAuth);
        Assert.False(response.StepUpRequiredForHighRiskActions);
        Assert.False(response.MfaEnabled);
        Assert.False(response.MfaVerified);
        Assert.Null(response.MfaVerifiedAt);
        Assert.Empty(response.StepUpMethods);
        Assert.NotNull(response.RecentAuthAt);
        Assert.True(response.RecentAuthAt >= beforeLogin.AddSeconds(-5));
        Assert.True(response.RecentAuthAt <= DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task SecurityState_RefreshDoesNotReplaceRecentAuthEvent()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var tokens = await LoginOwnerAsync(client);
        var beforeRefresh = await client.GetFromJsonAsync<AuthSecurityStateResponse>("/api/v1/auth/security-state");
        Assert.NotNull(beforeRefresh);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));
        refreshResponse.EnsureSuccessStatusCode();
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(refreshed);
        Authorize(client, refreshed);
        var afterRefresh = await client.GetFromJsonAsync<AuthSecurityStateResponse>("/api/v1/auth/security-state");

        Assert.NotNull(afterRefresh);
        Assert.Equal(beforeRefresh.RecentAuthAt, afterRefresh.RecentAuthAt);
        Assert.True(afterRefresh.HasRecentAuth);
        Assert.False(afterRefresh.MfaEnabled);
        Assert.False(afterRefresh.MfaVerified);
    }

    [Fact]
    public async Task MfaTotpEnrollment_VerifyAndSecurityStateAreBackendBacked()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var tokens = await LoginOwnerAsync(client);
        var userId = Guid.Parse(tokens.User.Id);

        var enrollResponse = await client.PostAsync("/api/v1/auth/mfa/totp/enroll", null);
        enrollResponse.EnsureSuccessStatusCode();
        var enrollment = await enrollResponse.Content.ReadFromJsonAsync<TotpEnrollmentResponse>();
        Assert.NotNull(enrollment);
        Assert.False(string.IsNullOrWhiteSpace(enrollment.Secret));
        Assert.Contains("otpauth://totp/", enrollment.ProvisioningUri, StringComparison.Ordinal);
        var beforeVerify = await client.GetFromJsonAsync<AuthSecurityStateResponse>("/api/v1/auth/security-state");
        Assert.NotNull(beforeVerify);
        Assert.False(beforeVerify.MfaEnabled);
        Assert.True(beforeVerify.HasRecentAuth);

        var invalid = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/totp/verify",
            new VerifyTotpRequest("000000"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var code = CreateTotpCode(enrollment.Secret, DateTimeOffset.UtcNow);
        var verifyResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/totp/verify",
            new VerifyTotpRequest(code));
        verifyResponse.EnsureSuccessStatusCode();
        var verifiedState = await verifyResponse.Content.ReadFromJsonAsync<AuthSecurityStateResponse>();

        Assert.NotNull(verifiedState);
        Assert.True(verifiedState.MfaEnabled);
        Assert.True(verifiedState.MfaVerified);
        Assert.False(verifiedState.StepUpRequiredForHighRiskActions);
        Assert.Contains(MfaMethodTypes.Totp, verifiedState.StepUpMethods);
        Assert.NotNull(verifiedState.MfaVerifiedAt);
        Assert.True(verifiedState.HasRecentAuth);

        var persisted = await ReadUserMfaMethodAsync(factory, userId);
        Assert.NotNull(persisted);
        Assert.Equal(MfaMethodStatuses.Enabled, persisted.Status);
        Assert.NotEqual(enrollment.Secret, persisted.SecretCiphertext);
        Assert.StartsWith("v1.", persisted.SecretCiphertext, StringComparison.Ordinal);
        Assert.DoesNotContain(enrollment.Secret, persisted.SecretCiphertext, StringComparison.Ordinal);

        var authEvent = await ReadLatestAuthEventAsync(factory, "auth.mfa_verified", userId);
        Assert.NotNull(authEvent);
        Assert.DoesNotContain(enrollment.Secret, authEvent.Metadata, StringComparison.Ordinal);
        Assert.DoesNotContain(code, authEvent.Metadata, StringComparison.Ordinal);
        Assert.DoesNotContain("password", authEvent.Metadata, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MfaDisable_RequiresStepUpAndClearsEnabledState()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var tokens = await LoginOwnerAsync(client);
        var userId = Guid.Parse(tokens.User.Id);
        var enrollment = await EnrollAndVerifyTotpAsync(client);
        await DeleteAuthEventsAsync(factory, "auth.mfa_verified", userId);

        var forbidden = await client.PostAsync("/api/v1/auth/mfa/totp/disable", null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var code = CreateTotpCode(enrollment.Secret, DateTimeOffset.UtcNow);
        var stepUp = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/totp/verify",
            new VerifyTotpRequest(code));
        stepUp.EnsureSuccessStatusCode();
        var disableResponse = await client.PostAsync("/api/v1/auth/mfa/totp/disable", null);
        disableResponse.EnsureSuccessStatusCode();
        var state = await disableResponse.Content.ReadFromJsonAsync<AuthSecurityStateResponse>();

        Assert.NotNull(state);
        Assert.False(state.MfaEnabled);
        Assert.False(state.MfaVerified);
        var persisted = await ReadUserMfaMethodAsync(factory, userId);
        Assert.NotNull(persisted);
        Assert.Equal(MfaMethodStatuses.Disabled, persisted.Status);
    }

    [Fact]
    public async Task MfaStepUp_IsRequiredForHighRiskPermissionMutations()
    {
        using var factory = new NorthstarApiFactory(new Dictionary<string, string?>
        {
            ["Permissions:PublicShareLinks:Enabled"] = "true"
        });
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var document = FindDocument(bootstrap, "Our Principles");
        var viewerTokens = await RegisterAndAddMemberAsync(
            client,
            ownerTokens,
            bootstrap.Workspace.Id,
            "viewer");
        Authorize(client, ownerTokens);
        var enrollment = await EnrollAndVerifyTotpAsync(client);
        var ownerId = Guid.Parse(ownerTokens.User.Id);
        await DeleteAuthEventsAsync(factory, "auth.mfa_verified", ownerId);

        var grantResponse = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants",
            new CreatePermissionGrantRequest(SubjectTypes.User, viewerTokens.User.Id, PermissionRole.Viewer, null, null));
        var policyResponse = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/policy",
            new UpdateResourcePolicyRequest(InheritanceModes.Restricted, LinkModes.Internal, PermissionRole.Viewer));
        var shareResponse = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(PermissionRole.Viewer, ShareLinkAudiences.Workspace, DateTimeOffset.UtcNow.AddDays(1), null, null));
        var inviteResponse = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/email-invites",
            new CreateEmailInviteRequest($"invite-{Guid.NewGuid():N}@northstar.local", PermissionRole.Viewer, DateTimeOffset.UtcNow.AddDays(1)));
        var scimTokenResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/tokens",
            new CreateScimTokenRequest("step-up token", DateTimeOffset.UtcNow.AddDays(1)));

        Assert.Equal(HttpStatusCode.Forbidden, grantResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, policyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, shareResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, inviteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, scimTokenResponse.StatusCode);

        var code = CreateTotpCode(enrollment.Secret, DateTimeOffset.UtcNow);
        var verifyResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/totp/verify",
            new VerifyTotpRequest(code));
        verifyResponse.EnsureSuccessStatusCode();
        var allowedGrantResponse = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants",
            new CreatePermissionGrantRequest(SubjectTypes.User, viewerTokens.User.Id, PermissionRole.Viewer, null, null));

        allowedGrantResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ProtectedApi_WithoutLogin_ReturnsUnauthorized()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/bootstrap");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NonWorkspaceMember_ReturnsForbidden()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var tokens = await RegisterAsync(client, $"outsider-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, tokens);

        var response = await client.GetAsync("/api/v1/bootstrap");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewerCanReadButCannotWrite()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerEmail = $"viewer-{Guid.NewGuid():N}@northstar.local";
        var viewerTokens = await RegisterAsync(client, viewerEmail);
        Authorize(client, ownerTokens);
        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap!.Workspace.Id}/members",
            new AddWorkspaceMemberRequest(viewerEmail, "viewer"));
        addResponse.EnsureSuccessStatusCode();

        Authorize(client, viewerTokens);
        var readResponse = await client.GetAsync("/api/v1/bootstrap");
        var writeResponse = await client.PostAsJsonAsync(
            "/api/v1/documents",
            new CreateDocumentRequest(bootstrap.Folders[0].Id, "Viewer Write"));

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, writeResponse.StatusCode);
    }

    [Fact]
    public async Task EditorCanCreateDocument()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var editorEmail = $"editor-{Guid.NewGuid():N}@northstar.local";
        var editorTokens = await RegisterAsync(client, editorEmail);
        Authorize(client, ownerTokens);
        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap!.Workspace.Id}/members",
            new AddWorkspaceMemberRequest(editorEmail, "editor"));
        addResponse.EnsureSuccessStatusCode();

        Authorize(client, editorTokens);
        var writeResponse = await client.PostAsJsonAsync(
            "/api/v1/documents",
            new CreateDocumentRequest(bootstrap.Folders[0].Id, "Editor Write"));

        Assert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);
    }

    [Fact]
    public async Task OwnerCanCreateRenameReorderAndDeleteEmptyCollection()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections",
            new CreateCollectionRequest("Field Notes", null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CollectionMutationResponse>();
        Assert.NotNull(created);
        Assert.Equal("Field Notes", created.Collection.Title);
        Assert.Contains(created.Map.Folders, folder => folder.Id == created.Collection.Id);

        var renameResponse = await client.PatchAsJsonAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections/{created.Collection.Id}",
            new UpdateCollectionRequest("Research Notes", 0.5m));
        renameResponse.EnsureSuccessStatusCode();
        var renamed = await renameResponse.Content.ReadFromJsonAsync<CollectionMutationResponse>();
        Assert.NotNull(renamed);
        Assert.Equal("Research Notes", renamed.Collection.Title);
        Assert.Equal(0.5m, renamed.Collection.SortOrder);

        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections/{created.Collection.Id}");
        deleteResponse.EnsureSuccessStatusCode();
        var afterDelete = await deleteResponse.Content.ReadFromJsonAsync<KnowledgeMapResponse>();
        Assert.NotNull(afterDelete);
        Assert.DoesNotContain(afterDelete.Folders, folder => folder.Id == created.Collection.Id);
    }

    [Fact]
    public async Task DeleteCollection_WithDocuments_ReturnsConflictEnvelope()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var collection = bootstrap.Folders.First(folder => folder.DocumentCount > 0);

        var response = await client.DeleteAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections/{collection.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.Conflict, error.Error.Code);
        Assert.Equal("Collection cannot be deleted while it contains documents.", error.Error.Message);
        var map = await client.GetFromJsonAsync<KnowledgeMapResponse>($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/map");
        Assert.NotNull(map);
        Assert.Contains(map.Folders, folder => folder.Id == collection.Id);
    }

    [Fact]
    public async Task ReorderCollections_StoresStableContiguousOrder()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var reversedIds = bootstrap.Folders.Select(folder => folder.Id).Reverse().ToArray();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections/order",
            new ReorderCollectionsRequest(reversedIds));

        response.EnsureSuccessStatusCode();
        var map = await response.Content.ReadFromJsonAsync<KnowledgeMapResponse>();
        Assert.NotNull(map);
        Assert.Equal(reversedIds, map.Folders.Select(folder => folder.Id));
        Assert.Equal(Enumerable.Range(1, reversedIds.Length).Select(value => (decimal)value), map.Folders.Select(folder => folder.SortOrder));
    }

    [Fact]
    public async Task ViewerCannotManageCollections()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");

        Authorize(client, viewerTokens);
        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections",
            new CreateCollectionRequest("Viewer Collection", null));
        var renameResponse = await client.PatchAsJsonAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections/{bootstrap.Folders[0].Id}",
            new UpdateCollectionRequest("Viewer Rename", null));
        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections/{bootstrap.Folders[0].Id}");
        var reorderResponse = await client.PutAsJsonAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/collections/order",
            new ReorderCollectionsRequest(bootstrap.Folders.Select(folder => folder.Id).Reverse().ToArray()));

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, renameResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reorderResponse.StatusCode);
    }

    [Fact]
    public async Task LastOwnerCannotBeRemovedOrDowngraded()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/auth/me");

        var downgradeResponse = await client.PatchAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap!.Workspace.Id}/members/{me!.User.Id}",
            new UpdateWorkspaceMemberRequest("viewer"));
        var removeResponse = await client.DeleteAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/members/{me.User.Id}");

        Assert.Equal(HttpStatusCode.Conflict, downgradeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, removeResponse.StatusCode);
    }

    [Fact]
    public async Task WorkspaceMembers_RejectCommenterRole()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var commenterEmail = $"commenter-{Guid.NewGuid():N}@northstar.local";
        await RegisterAsync(client, commenterEmail);
        await LoginOwnerAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap!.Workspace.Id}/members",
            new AddWorkspaceMemberRequest(commenterEmail, PermissionRole.Commenter));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.ValidationError, error.Error.Code);
    }

    [Fact]
    public async Task EffectivePermission_WithoutLogin_ReturnsUnauthorized()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/permissions/effective?resourceType=document&resourceId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EffectivePermission_NonWorkspaceMember_ReturnsForbidden()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var outsiderTokens = await RegisterAsync(client, $"outsider-{Guid.NewGuid():N}@northstar.local");
        var document = FindDocument(bootstrap!, "Our Principles");

        Authorize(client, outsiderTokens);
        var response = await client.GetAsync(
            $"/api/v1/permissions/effective?resourceType=document&resourceId={document.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EffectivePermission_WorkspaceViewerReturnsCatalogAllowedDocumentActions()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");

        Authorize(client, viewerTokens);
        var effective = await client.GetFromJsonAsync<EffectivePermissionResponse>(
            $"/api/v1/permissions/effective?resourceType=document&resourceId={document.Id}");

        Assert.NotNull(effective);
        Assert.Equal(ResourceTypes.Document, effective.ResourceType);
        Assert.Equal(document.Id, effective.ResourceId);
        Assert.Equal(PermissionRole.Viewer, effective.EffectiveRole);
        Assert.Equal(EffectivePermissionService.WorkspaceSource, effective.Source);
        Assert.Equal(InheritanceModes.Inherit, effective.InheritanceMode);
        Assert.Contains(PermissionActions.DocumentView, effective.AllowedActions);
        Assert.Contains(PermissionActions.FileDownload, effective.AllowedActions);
        Assert.Contains(PermissionActions.AttachmentView, effective.AllowedActions);
        Assert.Contains(PermissionActions.SearchQuery, effective.AllowedActions);
        Assert.Contains(PermissionActions.ActivityView, effective.AllowedActions);
        Assert.DoesNotContain(PermissionActions.DocumentEdit, effective.AllowedActions);
        Assert.DoesNotContain(PermissionActions.DocumentComment, effective.AllowedActions);
        Assert.DoesNotContain(PermissionActions.FileUpload, effective.AllowedActions);
        Assert.DoesNotContain(PermissionActions.AttachmentCreate, effective.AllowedActions);
    }

    [Fact]
    public async Task EffectivePermission_DirectGrantReturnsHigherScopedRole()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");
        await SeedResourceGrantAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            Guid.Parse(viewerTokens.User.Id),
            PermissionRole.Editor);

        Authorize(client, viewerTokens);
        var effective = await client.GetFromJsonAsync<EffectivePermissionResponse>(
            $"/api/v1/permissions/effective?resourceType=document&resourceId={document.Id}");

        Assert.NotNull(effective);
        Assert.Equal(PermissionRole.Editor, effective.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentSource, effective.Source);
        Assert.Contains(PermissionActions.DocumentEdit, effective.AllowedActions);
        Assert.Contains(PermissionActions.DocumentComment, effective.AllowedActions);
        Assert.Contains(PermissionActions.FileUpload, effective.AllowedActions);
    }

    [Fact]
    public async Task PermissionManagement_EditorCannotManageDocumentGrants()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var editorTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "editor");
        var document = FindDocument(bootstrap, "Our Principles");

        Authorize(client, editorTokens);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants",
            new CreatePermissionGrantRequest(SubjectTypes.User, editorTokens.User.Id, PermissionRole.Viewer, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PermissionManagement_AdminCanCreateGrantButCannotGrantOwner()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "admin");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");

        Authorize(client, adminTokens);
        var createEditor = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants",
            new CreatePermissionGrantRequest(SubjectTypes.User, viewerTokens.User.Id, PermissionRole.Editor, null, null));
        var createOwner = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants",
            new CreatePermissionGrantRequest(SubjectTypes.User, adminTokens.User.Id, PermissionRole.Owner, null, null));

        createEditor.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, createOwner.StatusCode);
    }

    [Fact]
    public async Task PermissionManagement_AllowsInternalLinkModeAndRejectsPublicLinkMode()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var internalResponse = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/policy",
            new UpdateResourcePolicyRequest(InheritanceModes.Restricted, LinkModes.Internal, PermissionRole.Viewer));
        var publicResponse = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/policy",
            new UpdateResourcePolicyRequest(InheritanceModes.Restricted, LinkModes.Public, PermissionRole.Viewer));

        internalResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, publicResponse.StatusCode);
    }

    [Fact]
    public async Task PermissionManagement_WritesAuditAndSoftRevokesGrant()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");

        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);
        var grant = await CreateDocumentGrantAsync(client, document.Id, viewerTokens.User.Id, PermissionRole.Viewer);
        var updatedGrantResponse = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{grant.Id}",
            new UpdatePermissionGrantRequest(PermissionRole.Editor, null, "temporary edit"));
        var revokeRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{grant.Id}")
        {
            Content = JsonContent.Create(new RevokePermissionGrantRequest("done"))
        };
        var revokeResponse = await client.SendAsync(revokeRequest);
        var grantState = await ReadGrantAsync(factory, Guid.Parse(grant.Id));

        Authorize(client, viewerTokens);
        var viewerGetAfterRevoke = await client.GetAsync($"/api/v1/documents/{document.Id}");
        var viewerAudit = await client.GetAsync(
            $"/api/v1/permissions/audit?workspaceId={bootstrap!.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var viewerWorkspaceAudit = await client.GetAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/audit?resourceType=document&resourceId={document.Id}");

        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap!.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var workspaceAudit = await client.GetFromJsonAsync<WorkspaceAuditLogResponse>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/audit?resourceType=document&resourceId={document.Id}&action={PermissionAuditActions.GrantRevoked}&limit=1");
        var secondPage = await client.GetFromJsonAsync<WorkspaceAuditLogResponse>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/audit?resourceType=document&resourceId={document.Id}&offset=1&limit=1");
        var invalidFilter = await client.GetAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/audit?resourceId={document.Id}");

        updatedGrantResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        Assert.NotNull(grantState);
        Assert.NotNull(grantState.RevokedAt);
        Assert.Equal(HttpStatusCode.Forbidden, viewerGetAfterRevoke.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerAudit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerWorkspaceAudit.StatusCode);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.PolicyUpdated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GrantCreated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GrantUpdated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GrantRevoked);
        Assert.NotNull(workspaceAudit);
        Assert.Single(workspaceAudit.Events);
        Assert.Equal(PermissionAuditActions.GrantRevoked, workspaceAudit.Events[0].Action);
        Assert.Equal(document.Id, workspaceAudit.Events[0].ResourceId);
        Assert.Equal(ownerTokens.User.Id, workspaceAudit.Events[0].ActorId);
        Assert.Equal(ownerTokens.User.DisplayName, workspaceAudit.Events[0].ActorName);
        Assert.Equal(0, workspaceAudit.Offset);
        Assert.Equal(1, workspaceAudit.Limit);
        Assert.True(workspaceAudit.TotalCount >= 1);
        Assert.NotNull(secondPage);
        Assert.Equal(1, secondPage.Offset);
        Assert.Equal(1, secondPage.Limit);
        Assert.True(secondPage.TotalCount >= 4);
        Assert.Equal(HttpStatusCode.BadRequest, invalidFilter.StatusCode);
    }

    [Fact]
    public async Task PermissionManagement_RevokeThenRegrantSameUserKeepsAuditHistory()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");

        var firstGrant = await CreateDocumentGrantAsync(client, document.Id, viewerTokens.User.Id, PermissionRole.Viewer);
        var revokeRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{firstGrant.Id}")
        {
            Content = JsonContent.Create(new RevokePermissionGrantRequest("rotate"))
        };
        var revokeResponse = await client.SendAsync(revokeRequest);
        var secondGrant = await CreateDocumentGrantAsync(client, document.Id, viewerTokens.User.Id, PermissionRole.Editor);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap!.Workspace.Id}&resourceType=document&resourceId={document.Id}");

        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        Assert.NotEqual(firstGrant.Id, secondGrant.Id);
        Assert.NotNull(audit);
        Assert.True(audit.Events.Count(item => item.Action == PermissionAuditActions.GrantCreated) >= 2);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GrantRevoked);
    }

    [Fact]
    public async Task TemporaryAccess_FutureGrantAuthorizesAndPastExpiresAtIsRejected()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);

        var pastCreate = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants",
            new CreatePermissionGrantRequest(
                SubjectTypes.User,
                viewerTokens.User.Id,
                PermissionRole.Viewer,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                null));
        var grant = await CreateDocumentGrantAsync(
            client,
            document.Id,
            viewerTokens.User.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));
        var pastUpdate = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{grant.Id}",
            new UpdatePermissionGrantRequest(null, DateTimeOffset.UtcNow.AddMinutes(-1), "expired update"));

        Authorize(client, viewerTokens);
        var viewerGet = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, pastCreate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, pastUpdate.StatusCode);
        Assert.NotNull(viewerGet);
    }

    [Fact]
    public async Task TemporaryAccess_ExpiredDirectGrantAndExpiredGroupMembershipDoNotAuthorize()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var directDocument = FindDocument(bootstrap, "Our Principles");
        var groupDocument = FindDocument(bootstrap, "Mission & Vision");
        await SetDocumentPolicyAsync(client, directDocument.Id, InheritanceModes.Restricted);
        await SetDocumentPolicyAsync(client, groupDocument.Id, InheritanceModes.Restricted);
        await SeedResourceGrantAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(directDocument.Id),
            Guid.Parse(viewerTokens.User.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddMinutes(-10));
        var groupId = await SeedWorkspaceGroupMemberAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            Guid.Parse(viewerTokens.User.Id),
            DateTimeOffset.UtcNow.AddMinutes(-10));
        await CreateDocumentSubjectGrantAsync(client, groupDocument.Id, SubjectTypes.Group, groupId.ToString(), PermissionRole.Viewer);

        Authorize(client, viewerTokens);
        var directGet = await client.GetAsync($"/api/v1/documents/{directDocument.Id}");
        var groupGet = await client.GetAsync($"/api/v1/documents/{groupDocument.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, directGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, groupGet.StatusCode);
    }

    [Fact]
    public async Task TemporaryAccess_PastGroupMemberExpiresAtIsRejected()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var group = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Temporary Readers");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}/members",
            new AddWorkspaceGroupMemberRequest(viewerTokens.User.Id, DateTimeOffset.UtcNow.AddMinutes(-5)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TemporaryAccess_ExpiredGrantCanBeRenewedAndPatchExpiryDistinguishesAbsentFromNull()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);
        await SeedResourceGrantAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            Guid.Parse(viewerTokens.User.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(-1));

        var renewed = await CreateDocumentGrantAsync(
            client,
            document.Id,
            viewerTokens.User.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));
        Authorize(client, viewerTokens);
        var renewedGet = await client.GetAsync($"/api/v1/documents/{document.Id}");

        Authorize(client, ownerTokens);
        var beforePatch = await ReadGrantAsync(factory, Guid.Parse(renewed.Id));
        var roleOnlyPatch = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{renewed.Id}",
            new { roleKey = PermissionRole.Viewer, reason = "role-only" });
        var afterRoleOnlyPatch = await ReadGrantAsync(factory, Guid.Parse(renewed.Id));
        var clearExpiryPatch = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{renewed.Id}",
            new { expiresAt = (DateTimeOffset?)null, reason = "clear expiry" });
        var afterClearPatch = await ReadGrantAsync(factory, Guid.Parse(renewed.Id));
        var grantUpdatedCount = await CountAuditEventsAsync(factory, PermissionAuditActions.GrantUpdated);

        renewedGet.EnsureSuccessStatusCode();
        roleOnlyPatch.EnsureSuccessStatusCode();
        clearExpiryPatch.EnsureSuccessStatusCode();
        Assert.NotNull(beforePatch);
        Assert.NotNull(afterRoleOnlyPatch);
        Assert.NotNull(afterClearPatch);
        Assert.Equal(beforePatch.ExpiresAt, afterRoleOnlyPatch.ExpiresAt);
        Assert.Null(afterClearPatch.ExpiresAt);
        Assert.True(grantUpdatedCount >= 2);
    }

    [Fact]
    public async Task ShareLinks_CreateListRevokeAuditAndStoreOnlyTokenHash()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var created = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));
        var listResponse = await client.GetAsync($"/api/v1/permissions/resources/document/{document.Id}/share-links");
        var listRaw = await listResponse.Content.ReadAsStringAsync();
        var links = JsonSerializer.Deserialize<ShareLinksResponse>(listRaw, JsonOptions);
        var metadataResponse = await client.GetAsync($"/api/v1/permissions/share-links/{created.Link.Id}");
        var metadataRaw = await metadataResponse.Content.ReadAsStringAsync();
        var metadata = JsonSerializer.Deserialize<ShareLinkDto>(metadataRaw, JsonOptions);
        var persisted = await ReadShareLinkAsync(factory, Guid.Parse(created.Link.Id));
        var revoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{created.Link.Id}");
        var revokedMetadata = await client.GetFromJsonAsync<ShareLinkDto>(
            $"/api/v1/permissions/share-links/{created.Link.Id}",
            JsonOptions);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap!.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.False(string.IsNullOrWhiteSpace(created.Token));
        Assert.Contains(created.Token, created.Url, StringComparison.Ordinal);
        listResponse.EnsureSuccessStatusCode();
        Assert.NotNull(links);
        Assert.Single(links.Links);
        metadataResponse.EnsureSuccessStatusCode();
        Assert.NotNull(metadata);
        Assert.Equal(created.Link.Id, metadata.Id);
        Assert.Null(metadata.RevokedAt);
        Assert.DoesNotContain("tokenHash", listRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(created.Token, listRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("tokenHash", metadataRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(created.Token, metadataRaw, StringComparison.Ordinal);
        Assert.NotNull(persisted);
        Assert.NotEqual(created.Token, persisted.TokenHash);
        Assert.DoesNotContain(created.Token, auditRaw, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.NotNull(revokedMetadata);
        Assert.NotNull(revokedMetadata.RevokedAt);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.ShareLinkCreated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.ShareLinkRevoked);
    }

    [Fact]
    public async Task ShareLinks_CreateAndRevokeEmitManagerNotificationsAndRespectMute()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "admin");
        var document = FindDocument(bootstrap, "Our Principles");
        var ownerId = Guid.Parse(ownerTokens.User.Id);
        var adminId = Guid.Parse(adminTokens.User.Id);

        Authorize(client, ownerTokens);
        var firstLink = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));
        var firstRevoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{firstLink.Link.Id}");
        var secondLink = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));

        Authorize(client, adminTokens);
        var linkNotifications = await client.GetFromJsonAsync<PermissionNotificationsResponse>(
            $"/api/v1/notifications?workspaceId={bootstrap.Workspace.Id}");
        var firstLinkCreatedNotification = Assert.Single(
            linkNotifications!.Notifications,
            item => item.Type == PermissionNotificationTypes.ShareLinkCreated &&
                item.Action?.SubjectId == firstLink.Link.Id);
        Assert.NotNull(firstLinkCreatedNotification.Actor);
        Assert.Equal(ownerTokens.User.DisplayName, firstLinkCreatedNotification.Actor.DisplayName);
        Assert.NotNull(firstLinkCreatedNotification.Resource);
        Assert.Equal(document.Title, firstLinkCreatedNotification.Resource.Title);
        Assert.NotNull(firstLinkCreatedNotification.Action);
        Assert.Equal("share_link", firstLinkCreatedNotification.Action.SubjectType);
        var mute = await client.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdatePermissionNotificationPreferenceRequest(
                bootstrap.Workspace.Id,
                ResourceTypes.Document,
                document.Id,
                Watched: false,
                Muted: true));

        Authorize(client, ownerTokens);
        var mutedRevoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{secondLink.Link.Id}");

        Assert.Equal(HttpStatusCode.NoContent, firstRevoke.StatusCode);
        mute.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, mutedRevoke.StatusCode);
        Assert.Equal(2, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.ShareLinkCreated));
        Assert.Equal(1, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.ShareLinkRevoked));
        Assert.Equal(2, await CountNotificationsAsync(factory, ownerId, PermissionNotificationTypes.ShareLinkCreated));
        Assert.Equal(2, await CountNotificationsAsync(factory, ownerId, PermissionNotificationTypes.ShareLinkRevoked));
    }

    [Fact]
    public async Task LinkManagement_ListDetailPatchPauseResumeCopyAndRevokeAreTokenFree()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var created = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));
        var listResponse = await client.GetAsync(
            $"/api/v1/permissions/share-links?workspaceId={bootstrap!.Workspace.Id}&resourceType=document&resourceId={document.Id}&status=active&limit=10");
        var listRaw = await listResponse.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<LinkManagementListResponse>(listRaw, JsonOptions);
        var detailRaw = await client.GetStringAsync($"/api/v1/permissions/share-links/{created.Link.Id}");
        var detail = JsonSerializer.Deserialize<LinkManagementDto>(detailRaw, JsonOptions);
        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}",
            new UpdateShareLinkRequest(PermissionRole.Commenter, DateTimeOffset.UtcNow.AddHours(3), "tighten"));
        var patched = await patch.Content.ReadFromJsonAsync<LinkManagementDto>();
        var invalidRole = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}",
            new { roleKey = PermissionRole.Editor });
        var pause = await client.PostAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}/pause",
            new ShareLinkPauseRequest("incident"));
        var paused = await pause.Content.ReadFromJsonAsync<LinkManagementDto>();
        var pausedResolve = await client.GetAsync($"/api/v1/share-links/{created.Token}/resolve");
        var resume = await client.PostAsync($"/api/v1/permissions/share-links/{created.Link.Id}/resume", null);
        var resumed = await resume.Content.ReadFromJsonAsync<LinkManagementDto>();
        var resumedResolve = await client.GetAsync($"/api/v1/share-links/{created.Token}/resolve");
        var copy = await client.PostAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}/copy-events",
            new ShareLinkCopyEventRequest("created_url", "copy"));
        var copiedUrl = await client.PostAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}/copy",
            new ShareLinkCopyEventRequest("share_url", "copy-url"));
        var copiedUrlBody = await copiedUrl.Content.ReadFromJsonAsync<CopyShareLinkResponse>();
        var firstRevoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{created.Link.Id}");
        var revokeCount = await CountAuditEventsAsync(factory, PermissionAuditActions.ShareLinkRevoked);
        var secondRevoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{created.Link.Id}");
        var secondRevokeCount = await CountAuditEventsAsync(factory, PermissionAuditActions.ShareLinkRevoked);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        listResponse.EnsureSuccessStatusCode();
        Assert.NotNull(list);
        Assert.Contains(list.Links, link => link.Id == created.Link.Id && link.Status == "active");
        Assert.NotNull(detail);
        Assert.Equal(document.Title, detail.ResourceTitle);
        Assert.True(detail.CanManage);
        Assert.DoesNotContain(created.Token, listRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("tokenHash", listRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", detailRaw, StringComparison.OrdinalIgnoreCase);
        patch.EnsureSuccessStatusCode();
        Assert.NotNull(patched);
        Assert.Equal(PermissionRole.Commenter, patched.RoleKey);
        Assert.Equal(HttpStatusCode.BadRequest, invalidRole.StatusCode);
        pause.EnsureSuccessStatusCode();
        Assert.NotNull(paused);
        Assert.Equal("paused", paused.Status);
        Assert.Equal(HttpStatusCode.NotFound, pausedResolve.StatusCode);
        resume.EnsureSuccessStatusCode();
        Assert.NotNull(resumed);
        Assert.Equal("active", resumed.Status);
        resumedResolve.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, copy.StatusCode);
        copiedUrl.EnsureSuccessStatusCode();
        Assert.NotNull(copiedUrlBody);
        Assert.Equal(created.Link.Id, copiedUrlBody.LinkId);
        Assert.False(copiedUrlBody.Reissued);
        Assert.StartsWith("/api/v1/share-links/", copiedUrlBody.Url, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NoContent, firstRevoke.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondRevoke.StatusCode);
        Assert.Equal(revokeCount, secondRevokeCount);
        Assert.Contains(audit!.Events, item => item.Action == PermissionAuditActions.ShareLinkCopyRequested);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.ShareLinkPaused);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.ShareLinkResumed);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.ShareLinkRoleUpdated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.ShareLinkExpiryUpdated);
        Assert.DoesNotContain(created.Token, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("tokenHash", auditRaw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LinkManagement_PublicPatchRulesPolicyStatusAndProtectedBoundaryHold()
    {
        using var factory = new NorthstarApiFactory(PublicShareEnabledConfiguration());
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var created = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2),
            ShareLinkAudiences.Public);
        var commenter = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}",
            new { roleKey = PermissionRole.Commenter });
        var clearExpiry = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}",
            new { expiresAt = (DateTimeOffset?)null });
        var tooLong = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}",
            new UpdateShareLinkRequest(null, DateTimeOffset.UtcNow.AddDays(30), null));
        var validExpiry = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/share-links/{created.Link.Id}",
            new UpdateShareLinkRequest(null, DateTimeOffset.UtcNow.AddHours(4), "extend"));
        var disablePolicy = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/policy",
            new UpdateResourcePolicyRequest(InheritanceModes.Inherit, LinkModes.Disabled, PermissionRole.Viewer));
        var detail = await client.GetFromJsonAsync<LinkManagementDto>(
            $"/api/v1/permissions/share-links/{created.Link.Id}");
        client.DefaultRequestHeaders.Authorization = null;
        var protectedListWithToken = await client.GetAsync(
            $"/api/v1/permissions/share-links?workspaceId={bootstrap!.Workspace.Id}&shareToken={Uri.EscapeDataString(created.Token)}");

        Assert.Equal(HttpStatusCode.BadRequest, commenter.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, clearExpiry.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        validExpiry.EnsureSuccessStatusCode();
        disablePolicy.EnsureSuccessStatusCode();
        Assert.NotNull(detail);
        Assert.Equal("policy_paused", detail.Status);
        Assert.Equal("disabled", detail.PolicyState);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedListWithToken.StatusCode);
    }

    [Fact]
    public async Task LinkManagement_UnauthorizedUsersCannotReadListOrDetail()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");

        Authorize(client, ownerTokens);
        var created = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));

        Authorize(client, viewerTokens);
        var list = await client.GetAsync($"/api/v1/permissions/share-links?workspaceId={bootstrap.Workspace.Id}");
        var detail = await client.GetAsync($"/api/v1/permissions/share-links/{created.Link.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, detail.StatusCode);
    }

    [Fact]
    public void ShareLinkAccessAnalytics_MigrationDefinesTokenFreeSchema()
    {
        var migration = ReadMigrationFile("AddShareLinkAccessAnalyticsV1.cs");

        Assert.Contains("share_link_access_events", migration, StringComparison.Ordinal);
        Assert.Contains("share_link_access_stats", migration, StringComparison.Ordinal);
        Assert.Contains("share_link_access_events_resource_type_check", migration, StringComparison.Ordinal);
        Assert.Contains("share_link_access_events_audience_check", migration, StringComparison.Ordinal);
        Assert.Contains("share_link_access_events_event_type_check", migration, StringComparison.Ordinal);
        Assert.Contains("share_link_access_events_result_check", migration, StringComparison.Ordinal);
        Assert.Contains("idx_share_link_access_events_link_time", migration, StringComparison.Ordinal);
        Assert.Contains("idx_share_link_access_events_resource_time", migration, StringComparison.Ordinal);
        Assert.Contains("idx_share_link_access_events_workspace_time", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("token_hash", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password_hash", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password_proof", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShareLinkAccessAnalytics_InternalResolveAndAccessWriteStatsAndManagerQueries()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var memberTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Restricted, LinkModes.Disabled);
        var link = await CreateShareLinkAsync(client, ResourceTypes.Document, document.Id, PermissionRole.Viewer);

        Authorize(client, memberTokens);
        var resolve = await client.GetAsync($"/api/v1/share-links/{link.Token}/resolve");
        var documentAccess = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");

        Authorize(client, ownerTokens);
        var statsRaw = await client.GetStringAsync($"/api/v1/permissions/share-links/{link.Link.Id}/stats");
        var stats = JsonSerializer.Deserialize<ShareLinkAccessStatsResponse>(statsRaw, JsonOptions);
        var eventsRaw = await client.GetStringAsync($"/api/v1/permissions/share-links/{link.Link.Id}/access-events?limit=10");
        var events = JsonSerializer.Deserialize<ShareLinkAccessEventsResponse>(eventsRaw, JsonOptions);

        resolve.EnsureSuccessStatusCode();
        documentAccess.EnsureSuccessStatusCode();
        Assert.NotNull(stats);
        Assert.Equal(link.Link.Id, stats.ShareLinkId);
        Assert.True(stats.AccessCount >= 2);
        Assert.Equal(1, stats.UniqueVisitorCount);
        Assert.Contains(stats.Trend, item => item.SuccessCount >= 2);
        Assert.Contains(stats.SourceBreakdown, item => item.Source == "workspace_member" && item.Count >= 2);
        Assert.NotNull(events);
        Assert.Contains(events.Events, item => item.EventType == "resolve" && item.Result == "success");
        Assert.Contains(events.Events, item => item.EventType == "access" && item.Result == "success");
        Assert.DoesNotContain(link.Token, statsRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("tokenHash", eventsRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", eventsRaw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShareLinkAccessAnalytics_KnownFailuresAreRecordedButUnknownTokenIsNot()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var memberTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        var active = await CreateShareLinkAsync(client, ResourceTypes.Document, document.Id, PermissionRole.Viewer);
        var paused = await CreateShareLinkAsync(client, ResourceTypes.Document, document.Id, PermissionRole.Viewer);
        var expiredToken = await SeedShareLinkAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        var pause = await client.PostAsJsonAsync(
            $"/api/v1/permissions/share-links/{paused.Link.Id}/pause",
            new ShareLinkPauseRequest("incident"));
        var revoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{active.Link.Id}");

        var beforeUnknown = await CountShareLinkAccessEventsAsync(factory);
        Authorize(client, memberTokens);
        var unknown = await client.GetAsync($"/api/v1/share-links/{Guid.NewGuid():N}/resolve");
        var afterUnknown = await CountShareLinkAccessEventsAsync(factory);
        var expired = await client.GetAsync($"/api/v1/share-links/{expiredToken}/resolve");
        var revoked = await client.GetAsync($"/api/v1/share-links/{active.Token}/resolve");
        var pausedResolve = await client.GetAsync($"/api/v1/share-links/{paused.Token}/resolve");
        var events = await ReadShareLinkAccessEventsAsync(factory);

        pause.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(beforeUnknown, afterUnknown);
        Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, revoked.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, pausedResolve.StatusCode);
        Assert.Contains(events, item => item.Result == "fail" && item.FailureCategory == "expired");
        Assert.Contains(events, item => item.Result == "fail" && item.FailureCategory == "revoked");
        Assert.Contains(events, item => item.Result == "fail" && item.FailureCategory == "paused");
    }

    [Fact]
    public async Task ShareLinkAccessAnalytics_PublicPasswordFailureAndDocumentCollectionAccessAreRecordedSafely()
    {
        using var factory = new NorthstarApiFactory(PublicShareEnabledConfiguration());
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap!, "Our Principles");
        var documentLink = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2),
            ShareLinkAudiences.Public,
            password: "correct horse");
        var collectionLink = await CreateShareLinkAsync(
            client,
            ResourceTypes.Collection,
            document.FolderId,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2),
            ShareLinkAudiences.Public);

        client.DefaultRequestHeaders.Authorization = null;
        var wrongPassword = await GetPublicShareAsync(
            client,
            $"/api/v1/public/share-links/{documentLink.Token}/document",
            "wrong");
        var publicDocument = await GetPublicShareAsync(
            client,
            $"/api/v1/public/share-links/{documentLink.Token}/document",
            "correct horse");
        var publicCollection = await client.GetAsync($"/api/v1/public/share-links/{collectionLink.Token}/collection");
        var protectedWithPublicToken = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={documentLink.Token}");

        Authorize(client, ownerTokens);
        var documentEvents = await client.GetFromJsonAsync<ShareLinkAccessEventsResponse>(
            $"/api/v1/permissions/share-links/{documentLink.Link.Id}/access-events?limit=10");
        var collectionStats = await client.GetFromJsonAsync<ShareLinkAccessStatsResponse>(
            $"/api/v1/permissions/share-links/{collectionLink.Link.Id}/stats");

        Assert.Equal(HttpStatusCode.NotFound, wrongPassword.StatusCode);
        publicDocument.EnsureSuccessStatusCode();
        publicCollection.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, protectedWithPublicToken.StatusCode);
        Assert.NotNull(documentEvents);
        Assert.Contains(documentEvents.Events, item => item.Result == "fail" && item.FailureCategory == "password_required_or_invalid");
        Assert.Contains(documentEvents.Events, item => item.Result == "success" && item.ActorType == "public_visitor");
        Assert.NotNull(collectionStats);
        Assert.True(collectionStats.AccessCount >= 1);
        Assert.Contains(collectionStats.SourceBreakdown, item => item.Source == "public_visitor");
    }

    [Fact]
    public async Task ShareLinkAccessAnalytics_UnauthorizedAndOtherWorkspaceManagersCannotRead()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var otherOwnerTokens = await RegisterAsync(client, $"other-owner-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        var link = await CreateShareLinkAsync(client, ResourceTypes.Document, document.Id, PermissionRole.Viewer);

        Authorize(client, viewerTokens);
        var viewerStats = await client.GetAsync($"/api/v1/permissions/share-links/{link.Link.Id}/stats");
        var viewerEvents = await client.GetAsync($"/api/v1/permissions/share-links/{link.Link.Id}/access-events");

        Authorize(client, otherOwnerTokens);
        var otherWorkspaceStats = await client.GetAsync($"/api/v1/permissions/share-links/{link.Link.Id}/stats");

        Assert.Equal(HttpStatusCode.Forbidden, viewerStats.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerEvents.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherWorkspaceStats.StatusCode);
    }

    [Fact]
    public async Task ShareLinks_RejectInvalidRoleAudiencePastExpiryAndPublicPolicy()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var invalidRole = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(PermissionRole.Editor, ShareLinkAudiences.Workspace, null));
        var invalidAudience = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(PermissionRole.Viewer, "public", null));
        var pastExpiry = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(PermissionRole.Viewer, ShareLinkAudiences.Workspace, DateTimeOffset.UtcNow.AddMinutes(-1)));
        var publicPolicy = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/policy",
            new UpdateResourcePolicyRequest(InheritanceModes.Inherit, LinkModes.Public, PermissionRole.Viewer));
        var internalPolicy = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/policy",
            new UpdateResourcePolicyRequest(InheritanceModes.Inherit, LinkModes.Internal, PermissionRole.Viewer));

        Assert.Equal(HttpStatusCode.BadRequest, invalidRole.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidAudience.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, pastExpiry.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, publicPolicy.StatusCode);
        internalPolicy.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ShareLinks_ResolveRequiresWorkspaceMemberAndRejectsExpiredRevokedOrUnknownTokens()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var memberTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var outsiderTokens = await RegisterAsync(client, $"outsider-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        var created = await CreateShareLinkAsync(client, ResourceTypes.Document, document.Id, PermissionRole.Viewer);
        var expiredToken = await SeedShareLinkAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddMinutes(-10));

        Authorize(client, memberTokens);
        var memberResolve = await client.GetFromJsonAsync<ResolveShareLinkResponse>(
            $"/api/v1/share-links/{created.Token}/resolve");
        var unknownResolve = await client.GetAsync($"/api/v1/share-links/{Guid.NewGuid():N}/resolve");
        var expiredResolve = await client.GetAsync($"/api/v1/share-links/{expiredToken}/resolve");

        Authorize(client, outsiderTokens);
        var outsiderResolve = await client.GetAsync($"/api/v1/share-links/{created.Token}/resolve");

        Authorize(client, ownerTokens);
        var revoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{created.Link.Id}");
        Authorize(client, memberTokens);
        var revokedResolve = await client.GetAsync($"/api/v1/share-links/{created.Token}/resolve");

        Assert.NotNull(memberResolve);
        Assert.Equal(document.Id, memberResolve.ResourceId);
        Assert.Equal(PermissionRole.Viewer, memberResolve.RoleKey);
        Assert.Equal(HttpStatusCode.NotFound, unknownResolve.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, expiredResolve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, outsiderResolve.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, revokedResolve.StatusCode);
    }

    [Fact]
    public async Task ShareLinks_AuthorizeViewerAndCommenterWithoutLeakingSearch()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap!, "Our Principles");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);
        var viewerLink = await CreateShareLinkAsync(client, ResourceTypes.Document, document.Id, PermissionRole.Viewer);
        var commenterLink = await CreateShareLinkAsync(client, ResourceTypes.Document, document.Id, PermissionRole.Commenter);

        Authorize(client, viewerTokens);
        var directGet = await client.GetAsync($"/api/v1/documents/{document.Id}");
        var linkedGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={viewerLink.Token}");
        var viewerComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={viewerLink.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "viewer should not comment"));
        var commenterComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={commenterLink.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "commenter can comment"));
        var editWithToken = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}?shareToken={commenterLink.Token}",
            new UpdateDocumentRequest(0, "No edit", null, null));
        var search = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=Principles&spaceId={bootstrap.ActiveSpaceId}&shareToken={viewerLink.Token}");

        Assert.Equal(HttpStatusCode.Forbidden, directGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, linkedGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerComment.StatusCode);
        Assert.Equal(HttpStatusCode.OK, commenterComment.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, editWithToken.StatusCode);
        Assert.NotNull(search);
        Assert.DoesNotContain(search.Results, result => result.Id == document.Id);
    }

    [Fact]
    public async Task ShareLinks_CollectionLinkCanAuthorizeChildDocument()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        var collectionId = document.FolderId;
        var restrictedCollection = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/collection/{collectionId}/policy",
            new UpdateResourcePolicyRequest(InheritanceModes.Restricted, LinkModes.Disabled, null));
        var collectionLink = await CreateShareLinkAsync(client, ResourceTypes.Collection, collectionId, PermissionRole.Viewer);

        Authorize(client, viewerTokens);
        var directGet = await client.GetAsync($"/api/v1/documents/{document.Id}");
        var linkedGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={collectionLink.Token}");

        restrictedCollection.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, directGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, linkedGet.StatusCode);
    }

    [Fact]
    public async Task ShareLinks_DocumentLinkAuthorizationRequiresInternalLinkMode()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Restricted, LinkModes.Disabled);
        var link = await CreateShareLinkAsync(client, ResourceTypes.Document, document.Id, PermissionRole.Commenter);

        Authorize(client, viewerTokens);
        var enabledGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var enabledComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "document link enabled"));
        var search = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=Principles&spaceId={bootstrap.ActiveSpaceId}&shareToken={link.Token}");
        var export = await client.GetFromJsonAsync<ExportSpaceResponse>(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/export?shareToken={link.Token}");
        var map = await client.GetFromJsonAsync<KnowledgeMapResponse>(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/map?shareToken={link.Token}");

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Restricted, LinkModes.Disabled);
        var listedWhileDisabled = await client.GetFromJsonAsync<ShareLinksResponse>(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links");

        Authorize(client, viewerTokens);
        var disabledGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var disabledComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "document link disabled"));

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Restricted, LinkModes.Internal, PermissionRole.Commenter);

        Authorize(client, viewerTokens);
        var reenabledGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var reenabledComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "document link reenabled"));

        Assert.Equal(HttpStatusCode.OK, enabledGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enabledComment.StatusCode);
        Assert.NotNull(search);
        Assert.DoesNotContain(search.Results, result => result.Id == document.Id);
        Assert.NotNull(export);
        Assert.DoesNotContain(export.Documents, item => item.Id == document.Id);
        Assert.NotNull(map);
        Assert.DoesNotContain(map.Documents, item => item.Id == document.Id);
        Assert.NotNull(listedWhileDisabled);
        Assert.Contains(listedWhileDisabled.Links, item => item.Id == link.Link.Id);
        Assert.Equal(HttpStatusCode.Forbidden, disabledGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, disabledComment.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reenabledGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reenabledComment.StatusCode);
    }

    [Fact]
    public async Task ShareLinks_CollectionLinkAuthorizationRequiresInternalLinkModeForChildDocument()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        var collectionId = document.FolderId;
        await SetResourcePolicyAsync(client, ResourceTypes.Collection, collectionId, InheritanceModes.Restricted, LinkModes.Disabled);
        var link = await CreateShareLinkAsync(client, ResourceTypes.Collection, collectionId, PermissionRole.Commenter);

        Authorize(client, viewerTokens);
        var enabledGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var enabledComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "collection link enabled"));

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(client, ResourceTypes.Collection, collectionId, InheritanceModes.Restricted, LinkModes.Disabled);

        Authorize(client, viewerTokens);
        var disabledGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var disabledComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "collection link disabled"));

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(client, ResourceTypes.Collection, collectionId, InheritanceModes.Restricted, LinkModes.Internal, PermissionRole.Commenter);

        Authorize(client, viewerTokens);
        var reenabledGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var reenabledComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "collection link reenabled"));

        Assert.Equal(HttpStatusCode.OK, enabledGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enabledComment.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, disabledGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, disabledComment.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reenabledGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reenabledComment.StatusCode);
    }

    [Fact]
    public async Task ShareLinks_ActiveLinkWithoutInternalPolicyDoesNotAuthorize()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Mission & Vision");
        var token = await SeedShareLinkAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            PermissionRole.Commenter,
            expiresAt: null);

        Authorize(client, viewerTokens);
        var comment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "missing policy link denied"));

        Assert.Equal(HttpStatusCode.Forbidden, comment.StatusCode);
    }

    [Fact]
    public async Task ExternalShareLinks_AuthorizeBoundOutsiderOnlyForSingleResource()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var outsiderTokens = await RegisterAsync(client, $"external-{Guid.NewGuid():N}@northstar.local");
        var otherOutsiderTokens = await RegisterAsync(client, $"external-other-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap!, "Our Principles");
        var link = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Commenter,
            DateTimeOffset.UtcNow.AddHours(2),
            ShareLinkAudiences.External,
            outsiderTokens.User.Email);
        var persisted = await ReadShareLinkAsync(factory, Guid.Parse(link.Link.Id));

        Authorize(client, outsiderTokens);
        var noTokenGet = await client.GetAsync($"/api/v1/documents/{document.Id}");
        var linkedGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var linkedComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "external commenter"));
        var editWithToken = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}?shareToken={link.Token}",
            new UpdateDocumentRequest(0, "No external edit", null, null));
        var resolve = await client.GetFromJsonAsync<ResolveShareLinkResponse>(
            $"/api/v1/share-links/{link.Token}/resolve");
        await AssertBulkEndpointsDoNotLeakDocumentAsync(client, bootstrap!, document.Id, link.Token);

        Authorize(client, otherOutsiderTokens);
        var wrongUserGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var wrongUserResolve = await client.GetAsync($"/api/v1/share-links/{link.Token}/resolve");

        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap!.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, noTokenGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, linkedGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, linkedComment.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, editWithToken.StatusCode);
        Assert.NotNull(resolve);
        Assert.Equal(ShareLinkAudiences.External, resolve.Audience);
        Assert.Equal(outsiderTokens.User.Email, resolve.SubjectEmail);
        Assert.Equal(HttpStatusCode.Forbidden, wrongUserGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongUserResolve.StatusCode);
        Assert.NotNull(persisted);
        Assert.Equal(ShareLinkAudiences.External, persisted.Audience);
        Assert.Equal(outsiderTokens.User.Email, persisted.SubjectEmail);
        Assert.NotEqual(link.Token, persisted.TokenHash);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.ShareLinkCreated);
        Assert.DoesNotContain(link.Token, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExternalShareLinks_RejectRevokedExpiredUnknownAndPublicTokens()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var outsiderTokens = await RegisterAsync(client, $"external-expiry-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap!, "Our Principles");
        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Restricted, LinkModes.External, PermissionRole.Viewer);
        var active = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1),
            ShareLinkAudiences.External,
            outsiderTokens.User.Email);
        var expiredToken = await SeedShareLinkAsync(
            factory,
            Guid.Parse(bootstrap!.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            ShareLinkAudiences.External,
            outsiderTokens.User.Email);
        var publicAudience = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(PermissionRole.Viewer, ShareLinkAudiences.Public, DateTimeOffset.UtcNow.AddHours(1)));

        Authorize(client, outsiderTokens);
        var expiredGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={expiredToken}");
        var unknownGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={Guid.NewGuid():N}");

        Authorize(client, ownerTokens);
        var revoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{active.Link.Id}");

        Authorize(client, outsiderTokens);
        var revokedGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={active.Token}");
        var revokedResolve = await client.GetAsync($"/api/v1/share-links/{active.Token}/resolve");

        Assert.Equal(HttpStatusCode.BadRequest, publicAudience.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, expiredGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unknownGet.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, revokedGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, revokedResolve.StatusCode);
    }

    [Fact]
    public async Task ExternalCollectionShareLink_DoesNotExpandRestrictedDocument()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var outsiderTokens = await RegisterAsync(client, $"external-collection-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap!, "Our Principles");
        var link = await CreateShareLinkAsync(
            client,
            ResourceTypes.Collection,
            document.FolderId,
            PermissionRole.Commenter,
            DateTimeOffset.UtcNow.AddHours(1),
            ShareLinkAudiences.External,
            outsiderTokens.User.Email);

        Authorize(client, outsiderTokens);
        var inheritedGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var inheritedComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "external collection comment"));

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Restricted, LinkModes.Disabled);

        Authorize(client, outsiderTokens);
        var restrictedGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var restrictedComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "blocked by restricted document"));

        Assert.Equal(HttpStatusCode.OK, inheritedGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, inheritedComment.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, restrictedGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, restrictedComment.StatusCode);
    }

    [Fact]
    public async Task PublicShareLinks_FeatureFlagOffRejectsPublicAudience()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(
                PermissionRole.Viewer,
                ShareLinkAudiences.Public,
                DateTimeOffset.UtcNow.AddHours(1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublicShareLinks_CreateResolveReadAndDoNotBroadenProtectedPaths()
    {
        using var factory = new NorthstarApiFactory(PublicShareEnabledConfiguration());
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var outsiderTokens = await RegisterAsync(client, $"public-outsider-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);

        var link = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2),
            ShareLinkAudiences.Public);
        var persisted = await ReadShareLinkAsync(factory, Guid.Parse(link.Link.Id));
        var policy = await ReadResourcePolicyAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id));

        client.DefaultRequestHeaders.Authorization = null;
        var publicResolve = await client.GetFromJsonAsync<ResolvePublicShareLinkResponse>(
            $"/api/v1/public/share-links/{link.Token}/resolve");
        var publicDocument = await client.GetFromJsonAsync<PublicShareDocumentResponse>(
            $"/api/v1/public/share-links/{link.Token}/document");
        var anonymousProtectedGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");

        Authorize(client, outsiderTokens);
        var legacyResolve = await client.GetAsync($"/api/v1/share-links/{link.Token}/resolve");
        var protectedGet = await client.GetAsync($"/api/v1/documents/{document.Id}?shareToken={link.Token}");
        var comment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "public token cannot comment"));
        var edit = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}?shareToken={link.Token}",
            new UpdateDocumentRequest(0, "No public edit", null, null));
        await AssertBulkEndpointsDoNotLeakDocumentAsync(client, bootstrap, document.Id, link.Token);

        Authorize(client, ownerTokens);
        var listResponse = await client.GetAsync($"/api/v1/permissions/resources/document/{document.Id}/share-links");
        var listRaw = await listResponse.Content.ReadAsStringAsync();
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.NotNull(publicResolve);
        Assert.Equal(ShareLinkAudiences.Public, publicResolve.Audience);
        Assert.Equal(PermissionRole.Viewer, publicResolve.RoleKey);
        Assert.NotNull(publicDocument);
        Assert.Equal(document.Id, publicDocument.Document.Id);
        Assert.Equal(document.Title, publicDocument.Document.Title);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousProtectedGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, legacyResolve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, protectedGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, comment.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
        Assert.NotNull(persisted);
        Assert.Equal(ShareLinkAudiences.Public, persisted.Audience);
        Assert.NotEqual(link.Token, persisted.TokenHash);
        Assert.NotNull(persisted.ExpiresAt);
        Assert.Null(persisted.SubjectEmail);
        Assert.NotNull(policy);
        Assert.Equal(LinkModes.Public, policy.LinkMode);
        listResponse.EnsureSuccessStatusCode();
        Assert.DoesNotContain("tokenHash", listRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(link.Token, listRaw, StringComparison.Ordinal);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.ShareLinkCreated);
        Assert.DoesNotContain(link.Token, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicShareLinks_ValidateExpiryRoleSubjectAndPasswordBoundary()
    {
        using var factory = new NorthstarApiFactory(PublicShareEnabledConfiguration());
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var missingExpiry = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(PermissionRole.Viewer, ShareLinkAudiences.Public, null));
        var longExpiry = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(PermissionRole.Viewer, ShareLinkAudiences.Public, DateTimeOffset.UtcNow.AddDays(8)));
        var commenter = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(PermissionRole.Commenter, ShareLinkAudiences.Public, DateTimeOffset.UtcNow.AddHours(1)));
        var subjectEmail = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(
                PermissionRole.Viewer,
                ShareLinkAudiences.Public,
                DateTimeOffset.UtcNow.AddHours(1),
                "person@example.test"));
        var internalPassword = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(
                PermissionRole.Viewer,
                ShareLinkAudiences.Workspace,
                DateTimeOffset.UtcNow.AddHours(1),
                null,
                "not-for-internal-links"));

        Assert.Equal(HttpStatusCode.BadRequest, missingExpiry.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longExpiry.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, commenter.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, subjectEmail.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, internalPassword.StatusCode);
    }

    [Fact]
    public async Task PublicShareLinks_RejectPolicyMismatchRevokedExpiredAndUnknownTokens()
    {
        using var factory = new NorthstarApiFactory(PublicShareEnabledConfiguration());
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        var missingPolicyToken = await SeedShareLinkAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1),
            ShareLinkAudiences.Public);
        client.DefaultRequestHeaders.Authorization = null;
        var missingPolicy = await client.GetAsync($"/api/v1/public/share-links/{missingPolicyToken}/document");

        Authorize(client, ownerTokens);
        var active = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1),
            ShareLinkAudiences.Public);
        var toRevoke = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1),
            ShareLinkAudiences.Public);
        var expiredToken = await SeedShareLinkAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            ShareLinkAudiences.Public);

        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Inherit, LinkModes.Disabled);
        client.DefaultRequestHeaders.Authorization = null;
        var disabledPolicy = await client.GetAsync($"/api/v1/public/share-links/{active.Token}/document");

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Inherit, LinkModes.External, PermissionRole.Viewer);
        client.DefaultRequestHeaders.Authorization = null;
        var externalPolicy = await client.GetAsync($"/api/v1/public/share-links/{active.Token}/document");

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(client, ResourceTypes.Document, document.Id, InheritanceModes.Inherit, LinkModes.Internal, PermissionRole.Viewer);
        client.DefaultRequestHeaders.Authorization = null;
        var internalPolicy = await client.GetAsync($"/api/v1/public/share-links/{active.Token}/document");

        Authorize(client, ownerTokens);
        _ = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1),
            ShareLinkAudiences.Public);
        var revoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{toRevoke.Link.Id}");
        client.DefaultRequestHeaders.Authorization = null;
        var revoked = await client.GetAsync($"/api/v1/public/share-links/{toRevoke.Token}/document");
        var expired = await client.GetAsync($"/api/v1/public/share-links/{expiredToken}/document");
        var unknown = await client.GetAsync($"/api/v1/public/share-links/{Guid.NewGuid():N}/document");

        Assert.Equal(HttpStatusCode.NotFound, missingPolicy.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, disabledPolicy.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, externalPolicy.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, internalPolicy.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, revoked.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task PublicCollectionShareLinks_ListOnlyUnrestrictedChildrenAndDoNotBroadenProtectedPaths()
    {
        using var factory = new NorthstarApiFactory(PublicShareEnabledConfiguration());
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var outsiderTokens = await RegisterAsync(client, $"public-collection-outsider-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var collectionId = bootstrap.Folders[0].Id;
        var visible = await CreateDocumentAsync(client, collectionId, "Phase 11 public collection visible child");
        var restricted = await CreateDocumentAsync(client, collectionId, "Phase 11 public collection restricted child");
        var archived = await CreateDocumentAsync(client, collectionId, "Phase 11 public collection archived child");
        var deleted = await CreateDocumentAsync(client, collectionId, "Phase 11 public collection deleted child");
        await SetResourcePolicyAsync(client, ResourceTypes.Document, restricted.Document.Id, InheritanceModes.Restricted, LinkModes.Disabled);
        var archive = await client.PatchAsync($"/api/v1/documents/{archived.Document.Id}/archive", null);
        var delete = await client.DeleteAsync($"/api/v1/documents/{deleted.Document.Id}");
        var link = await CreateShareLinkAsync(
            client,
            ResourceTypes.Collection,
            collectionId,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2),
            ShareLinkAudiences.Public);
        var persisted = await ReadShareLinkAsync(factory, Guid.Parse(link.Link.Id));
        var policy = await ReadResourcePolicyAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Collection,
            Guid.Parse(collectionId));

        client.DefaultRequestHeaders.Authorization = null;
        var resolve = await client.GetFromJsonAsync<ResolvePublicShareLinkResponse>(
            $"/api/v1/public/share-links/{link.Token}/resolve");
        var collectionResponse = await client.GetAsync($"/api/v1/public/share-links/{link.Token}/collection");
        var collectionRaw = await collectionResponse.Content.ReadAsStringAsync();
        var collection = JsonSerializer.Deserialize<PublicShareCollectionResponse>(collectionRaw, JsonOptions);
        var documentEndpoint = await client.GetAsync($"/api/v1/public/share-links/{link.Token}/document");
        var anonymousManagementList = await client.GetAsync(
            $"/api/v1/permissions/resources/collection/{collectionId}/share-links?shareToken={link.Token}");

        Authorize(client, outsiderTokens);
        var legacyResolve = await client.GetAsync($"/api/v1/share-links/{link.Token}/resolve");
        var protectedVisibleGet = await client.GetAsync($"/api/v1/documents/{visible.Document.Id}?shareToken={link.Token}");
        var protectedRestrictedGet = await client.GetAsync($"/api/v1/documents/{restricted.Document.Id}?shareToken={link.Token}");
        var protectedComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{visible.Document.Id}/comments?shareToken={link.Token}",
            new CreateCommentThreadRequest(CreateCommentAnchor(visible.Document.Id), "public collection token cannot comment"));
        await AssertBulkEndpointsDoNotLeakDocumentAsync(client, bootstrap, restricted.Document.Id, link.Token);

        Authorize(client, ownerTokens);
        var listResponse = await client.GetAsync($"/api/v1/permissions/resources/collection/{collectionId}/share-links");
        var listRaw = await listResponse.Content.ReadAsStringAsync();
        await SetResourcePolicyAsync(client, ResourceTypes.Collection, collectionId, InheritanceModes.Inherit, LinkModes.Disabled);
        client.DefaultRequestHeaders.Authorization = null;
        var disabledPolicy = await client.GetAsync($"/api/v1/public/share-links/{link.Token}/collection");

        archive.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.NotNull(resolve);
        Assert.Equal(ResourceTypes.Collection, resolve.ResourceType);
        Assert.Equal(PermissionRole.Viewer, resolve.RoleKey);
        Assert.False(resolve.HasPassword);
        collectionResponse.EnsureSuccessStatusCode();
        Assert.NotNull(collection);
        Assert.Equal(collectionId, collection.Collection.Id);
        Assert.Contains(collection.Collection.Documents, item => item.Id == visible.Document.Id);
        Assert.DoesNotContain(collection.Collection.Documents, item => item.Id == restricted.Document.Id);
        Assert.DoesNotContain(collection.Collection.Documents, item => item.Id == archived.Document.Id);
        Assert.DoesNotContain(collection.Collection.Documents, item => item.Id == deleted.Document.Id);
        Assert.DoesNotContain("\"content\"", collectionRaw, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, documentEndpoint.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousManagementList.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, legacyResolve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, protectedVisibleGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, protectedRestrictedGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, protectedComment.StatusCode);
        Assert.NotNull(persisted);
        Assert.Equal(ShareLinkAudiences.Public, persisted.Audience);
        Assert.Equal(ResourceTypes.Collection, persisted.ResourceType);
        Assert.Null(persisted.PasswordHash);
        Assert.NotNull(policy);
        Assert.Equal(LinkModes.Public, policy.LinkMode);
        listResponse.EnsureSuccessStatusCode();
        Assert.DoesNotContain("tokenHash", listRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(link.Token, listRaw, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, disabledPolicy.StatusCode);
    }

    [Fact]
    public async Task PublicShareLinks_PasswordRequiresProofAndStoresHashOnly()
    {
        using var factory = new NorthstarApiFactory(PublicShareEnabledConfiguration());
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Our Principles");
        const string password = "phase-11-public-password";
        var link = await CreateShareLinkAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2),
            ShareLinkAudiences.Public,
            password: password);
        var expiredToken = await SeedShareLinkAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            ShareLinkAudiences.Public,
            password: password);
        var persisted = await ReadShareLinkAsync(factory, Guid.Parse(link.Link.Id));

        client.DefaultRequestHeaders.Authorization = null;
        var missingResolve = await client.GetAsync($"/api/v1/public/share-links/{link.Token}/resolve");
        var missingDocument = await client.GetAsync($"/api/v1/public/share-links/{link.Token}/document");
        var wrongDocument = await GetPublicShareAsync(client, $"/api/v1/public/share-links/{link.Token}/document", "wrong-password");
        var correctResolveResponse = await GetPublicShareAsync(client, $"/api/v1/public/share-links/{link.Token}/resolve", password);
        var correctResolve = await correctResolveResponse.Content.ReadFromJsonAsync<ResolvePublicShareLinkResponse>();
        var correctDocumentResponse = await GetPublicShareAsync(client, $"/api/v1/public/share-links/{link.Token}/document", password);
        var correctDocument = await correctDocumentResponse.Content.ReadFromJsonAsync<PublicShareDocumentResponse>();

        Authorize(client, ownerTokens);
        var listResponse = await client.GetAsync($"/api/v1/permissions/resources/document/{document.Id}/share-links");
        var listRaw = await listResponse.Content.ReadAsStringAsync();
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);
        var revoke = await client.DeleteAsync($"/api/v1/permissions/share-links/{link.Link.Id}");

        client.DefaultRequestHeaders.Authorization = null;
        var revoked = await GetPublicShareAsync(client, $"/api/v1/public/share-links/{link.Token}/document", password);
        var expired = await GetPublicShareAsync(client, $"/api/v1/public/share-links/{expiredToken}/document", password);
        var unknown = await GetPublicShareAsync(client, $"/api/v1/public/share-links/{Guid.NewGuid():N}/document", password);

        Assert.Equal(HttpStatusCode.NotFound, missingResolve.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingDocument.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, wrongDocument.StatusCode);
        correctResolveResponse.EnsureSuccessStatusCode();
        Assert.NotNull(correctResolve);
        Assert.True(correctResolve.HasPassword);
        correctDocumentResponse.EnsureSuccessStatusCode();
        Assert.NotNull(correctDocument);
        Assert.Equal(document.Id, correctDocument.Document.Id);
        Assert.True(link.Link.HasPassword);
        Assert.NotNull(persisted);
        Assert.True(persisted.HasPassword);
        Assert.False(string.IsNullOrWhiteSpace(persisted.PasswordHash));
        Assert.NotEqual(password, persisted.PasswordHash);
        listResponse.EnsureSuccessStatusCode();
        Assert.DoesNotContain(password, listRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.PasswordHash!, listRaw, StringComparison.Ordinal);
        Assert.NotNull(audit);
        Assert.DoesNotContain(password, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.PasswordHash!, auditRaw, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, revoked.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task EmailInvites_CreateAcceptRevokeAndAuthorizeOnlyBoundResource()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var invitedTokens = await RegisterAsync(client, $"invite-{Guid.NewGuid():N}@northstar.local");
        var wrongTokens = await RegisterAsync(client, $"invite-wrong-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap!, "Our Principles");
        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            invitedTokens.User.Email,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));
        var duplicate = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/email-invites",
            new CreateEmailInviteRequest(invitedTokens.User.Email, PermissionRole.Viewer, DateTimeOffset.UtcNow.AddHours(2)));
        var listResponse = await client.GetAsync($"/api/v1/permissions/resources/document/{document.Id}/email-invites");
        var listRaw = await listResponse.Content.ReadAsStringAsync();
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));

        Authorize(client, wrongTokens);
        var wrongResolve = await client.GetAsync($"/api/v1/permissions/email-invites/{invite.Token}/resolve");
        var wrongAccept = await client.PostAsync($"/api/v1/permissions/email-invites/{invite.Token}/accept", null);

        Authorize(client, invitedTokens);
        var noInviteGet = await client.GetAsync($"/api/v1/documents/{document.Id}");
        var resolve = await client.GetFromJsonAsync<ResolveEmailInviteResponse>(
            $"/api/v1/permissions/email-invites/{invite.Token}/resolve");
        var accepted = await client.PostAsJsonAsync<object?>(
            $"/api/v1/permissions/email-invites/{invite.Token}/accept",
            null);
        var acceptedGet = await client.GetAsync($"/api/v1/documents/{document.Id}");
        var acceptedComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "viewer invite cannot comment"));
        await AssertBulkEndpointsDoNotLeakDocumentAsync(client, bootstrap!, document.Id, shareToken: null);

        Authorize(client, ownerTokens);
        var revoke = await client.DeleteAsync($"/api/v1/permissions/email-invites/{invite.Invite.Id}");

        Authorize(client, invitedTokens);
        var revokedGet = await client.GetAsync($"/api/v1/documents/{document.Id}");

        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap!.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        listResponse.EnsureSuccessStatusCode();
        Assert.DoesNotContain("tokenHash", listRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(invite.Token, listRaw, StringComparison.Ordinal);
        Assert.NotNull(persisted);
        Assert.NotEqual(invite.Token, persisted.TokenHash);
        Assert.Equal(HttpStatusCode.Forbidden, wrongResolve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongAccept.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, noInviteGet.StatusCode);
        Assert.NotNull(resolve);
        Assert.Equal(invitedTokens.User.Email, resolve.Email);
        accepted.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, acceptedGet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, acceptedComment.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, revokedGet.StatusCode);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.EmailInviteCreated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.EmailInviteAccepted);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.EmailInviteRevoked);
        Assert.DoesNotContain(invite.Token, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmailInvites_DefaultDeliveryRemainsDisabled()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var document = FindDocument(bootstrap, "Our Principles");

        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            $"delivery-default-{Guid.NewGuid():N}@northstar.local",
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));

        Assert.Equal(EmailInviteDeliveryStatuses.Disabled, invite.Delivery.Status);
        Assert.Equal("noop", invite.Delivery.Provider);
        Assert.Null(invite.Delivery.AttemptedAt);
        Assert.Equal(EmailInviteDeliveryStatuses.Disabled, invite.Invite.DeliveryStatus);
        Assert.NotNull(persisted);
        Assert.Equal(EmailInviteDeliveryStatuses.Disabled, persisted.DeliveryStatus);
        Assert.Equal("noop", persisted.DeliveryProvider);
        Assert.Null(persisted.DeliveryAttemptedAt);
        Assert.Null(persisted.DeliveryErrorCode);
    }

    [Fact]
    public async Task EmailInvites_UnsupportedConfiguredProviderFailsClosed()
    {
        using var factory = new NorthstarApiFactory(
            new Dictionary<string, string?>
            {
                ["Permissions:EmailInvites:Delivery:Enabled"] = "true",
                ["Permissions:EmailInvites:Delivery:Provider"] = "sendgrid",
                ["Permissions:EmailInvites:Delivery:PublicBaseUrl"] = "https://northstar.example"
            });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var document = FindDocument(bootstrap, "Our Principles");

        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            $"delivery-unsupported-{Guid.NewGuid():N}@northstar.local",
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.Equal(EmailInviteDeliveryStatuses.Failed, invite.Delivery.Status);
        Assert.Equal("sendgrid", invite.Delivery.Provider);
        Assert.Equal("unsupported_provider", invite.Delivery.ErrorCode);
        Assert.NotNull(persisted);
        Assert.Equal(EmailInviteDeliveryStatuses.Failed, persisted.DeliveryStatus);
        Assert.Equal("sendgrid", persisted.DeliveryProvider);
        Assert.Equal("unsupported_provider", persisted.DeliveryErrorCode);
        Assert.DoesNotContain(invite.Token, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.TokenHash, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(invite.Url, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmailInvites_DeliveryFakeReceivesUrlWithoutPersistingRawToken()
    {
        var fakeDelivery = new FakeEmailInviteDeliveryService();
        using var factory = new NorthstarApiFactory(
            new Dictionary<string, string?>
            {
                ["Permissions:EmailInvites:Delivery:Enabled"] = "true",
                ["Permissions:EmailInvites:Delivery:Provider"] = "fake",
                ["Permissions:EmailInvites:Delivery:PublicBaseUrl"] = "https://northstar.example"
            },
            services =>
            {
                services.RemoveAll<IEmailInviteDeliveryService>();
                services.AddSingleton<IEmailInviteDeliveryService>(fakeDelivery);
            });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var document = FindDocument(bootstrap, "Our Principles");
        var email = $"delivery-{Guid.NewGuid():N}@northstar.local";

        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            email,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        var message = Assert.Single(fakeDelivery.Messages);
        Assert.Contains(invite.Token, message.AcceptUrl, StringComparison.Ordinal);
        Assert.StartsWith("https://northstar.example/api/v1/permissions/email-invites/", message.AcceptUrl, StringComparison.Ordinal);
        Assert.Equal(EmailInviteDeliveryStatuses.Sent, invite.Delivery.Status);
        Assert.Equal("fake", invite.Delivery.Provider);
        Assert.Equal(EmailInviteDeliveryStatuses.Sent, invite.Invite.DeliveryStatus);
        Assert.NotNull(persisted);
        Assert.Equal(EmailInviteDeliveryStatuses.Sent, persisted.DeliveryStatus);
        Assert.Equal("fake", persisted.DeliveryProvider);
        Assert.NotEqual(invite.Token, persisted.TokenHash);
        Assert.DoesNotContain(invite.Token, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.TokenHash, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmailInvites_DeliveryOutboxQueuesAndStoresOnlySecretSafeState()
    {
        var fakeDelivery = new FakeEmailInviteDeliveryService();
        using var factory = new NorthstarApiFactory(
            new Dictionary<string, string?>
            {
                ["Permissions:EmailInvites:Delivery:Enabled"] = "true",
                ["Permissions:EmailInvites:Delivery:Provider"] = "fake",
                ["Permissions:EmailInvites:Delivery:PublicBaseUrl"] = "https://northstar.example"
            },
            services =>
            {
                services.RemoveAll<IEmailInviteDeliveryService>();
                services.AddSingleton<IEmailInviteDeliveryService>(fakeDelivery);
            });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var document = FindDocument(bootstrap, "Our Principles");
        var email = $"delivery-outbox-{Guid.NewGuid():N}@northstar.local";

        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            email,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));
        var outbox = await ReadEmailInviteDeliveryOutboxAsync(factory, Guid.Parse(invite.Invite.Id));
        var outboxRaw = JsonSerializer.Serialize(outbox, JsonOptions);

        Assert.NotNull(persisted);
        Assert.NotNull(outbox);
        Assert.Equal(EmailInviteDeliveryOutboxStatuses.Sent, outbox.Status);
        Assert.Equal(1, outbox.AttemptCount);
        Assert.Equal("fake", outbox.Provider);
        Assert.Equal(email, outbox.RecipientEmail);
        Assert.Equal(Guid.Parse(invite.Invite.Id), outbox.InviteId);
        Assert.NotNull(outbox.SentAt);
        Assert.Null(outbox.LastErrorCode);
        Assert.DoesNotContain(invite.Token, outboxRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.TokenHash, outboxRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(invite.Url, outboxRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("tokenHash", outboxRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("acceptUrl", outboxRaw, StringComparison.OrdinalIgnoreCase);
        Assert.Single(fakeDelivery.Messages);
    }

    [Fact]
    public async Task EmailInvites_DeliveryOutboxRetriesDueItemAndMarksSent()
    {
        var sequenceDelivery = new SequenceEmailInviteDeliveryService("fake-sequence", false, true);
        using var factory = new NorthstarApiFactory(
            new Dictionary<string, string?>
            {
                ["Permissions:EmailInvites:Delivery:Enabled"] = "true",
                ["Permissions:EmailInvites:Delivery:Provider"] = "fake-sequence",
                ["Permissions:EmailInvites:Delivery:PublicBaseUrl"] = "https://northstar.example",
                ["Permissions:EmailInvites:Delivery:MaxAttempts"] = "2",
                ["Permissions:EmailInvites:Delivery:RetryDelaySeconds"] = "0"
            },
            services =>
            {
                services.RemoveAll<IEmailInviteDeliveryService>();
                services.AddSingleton<IEmailInviteDeliveryService>(sequenceDelivery);
            });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var document = FindDocument(bootstrap, "Our Principles");

        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            $"delivery-retry-{Guid.NewGuid():N}@northstar.local",
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var inviteId = Guid.Parse(invite.Invite.Id);
        var failedOutbox = await ReadEmailInviteDeliveryOutboxAsync(factory, inviteId);
        Assert.NotNull(failedOutbox);
        Assert.Equal(EmailInviteDeliveryOutboxStatuses.RetryScheduled, failedOutbox.Status);
        Assert.Equal(1, failedOutbox.AttemptCount);
        Assert.Equal("provider_error", failedOutbox.LastErrorCode);

        using (var scope = factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IEmailInviteDeliveryOutboxProcessor>();
            var result = await processor.ProcessDueAsync(
                new Dictionary<Guid, string> { [inviteId] = invite.Url },
                DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.Equal(1, result.Attempted);
            Assert.Equal(1, result.Sent);
            Assert.Equal(0, result.Failed);
        }

        var persisted = await ReadEmailInviteAsync(factory, inviteId);
        var sentOutbox = await ReadEmailInviteDeliveryOutboxAsync(factory, inviteId);
        Assert.NotNull(persisted);
        Assert.NotNull(sentOutbox);
        Assert.Equal(EmailInviteDeliveryStatuses.Sent, persisted.DeliveryStatus);
        Assert.Equal(EmailInviteDeliveryOutboxStatuses.Sent, sentOutbox.Status);
        Assert.Equal(2, sentOutbox.AttemptCount);
        Assert.NotNull(sentOutbox.SentAt);
        Assert.Null(sentOutbox.FailedAt);
        Assert.Equal(2, sequenceDelivery.Messages.Count);
    }

    [Fact]
    public async Task EmailInvites_DeliveryOutboxMarksTerminalFailureAfterMaxAttempts()
    {
        var sequenceDelivery = new SequenceEmailInviteDeliveryService("fake-sequence", false, false);
        using var factory = new NorthstarApiFactory(
            new Dictionary<string, string?>
            {
                ["Permissions:EmailInvites:Delivery:Enabled"] = "true",
                ["Permissions:EmailInvites:Delivery:Provider"] = "fake-sequence",
                ["Permissions:EmailInvites:Delivery:PublicBaseUrl"] = "https://northstar.example",
                ["Permissions:EmailInvites:Delivery:MaxAttempts"] = "2",
                ["Permissions:EmailInvites:Delivery:RetryDelaySeconds"] = "0"
            },
            services =>
            {
                services.RemoveAll<IEmailInviteDeliveryService>();
                services.AddSingleton<IEmailInviteDeliveryService>(sequenceDelivery);
            });
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "admin");
        var document = FindDocument(bootstrap, "Our Principles");
        var adminId = Guid.Parse(adminTokens.User.Id);

        Authorize(client, ownerTokens);
        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            $"delivery-terminal-{Guid.NewGuid():N}@northstar.local",
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var inviteId = Guid.Parse(invite.Invite.Id);

        using (var scope = factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IEmailInviteDeliveryOutboxProcessor>();
            var result = await processor.ProcessDueAsync(
                new Dictionary<Guid, string> { [inviteId] = invite.Url },
                DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.Equal(1, result.Attempted);
            Assert.Equal(0, result.Sent);
            Assert.Equal(1, result.Failed);
        }

        var persisted = await ReadEmailInviteAsync(factory, inviteId);
        var outbox = await ReadEmailInviteDeliveryOutboxAsync(factory, inviteId);
        Assert.NotNull(persisted);
        Assert.NotNull(outbox);
        Assert.Equal(EmailInviteDeliveryStatuses.Failed, persisted.DeliveryStatus);
        Assert.Equal(EmailInviteDeliveryOutboxStatuses.Failed, outbox.Status);
        Assert.Equal(2, outbox.AttemptCount);
        Assert.Equal("provider_error", outbox.LastErrorCode);
        Assert.NotNull(outbox.FailedAt);
        Assert.Null(outbox.NextAttemptAt);
        Assert.Equal(1, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.EmailInviteDeliveryFailed));
        Assert.Equal(2, sequenceDelivery.Messages.Count);
    }

    [Fact]
    public async Task EmailInvites_DeliveryFailureStoresStatusWithoutRawToken()
    {
        using var factory = new NorthstarApiFactory(
            new Dictionary<string, string?>
            {
                ["Permissions:EmailInvites:Delivery:Enabled"] = "true",
                ["Permissions:EmailInvites:Delivery:Provider"] = "fake-failure",
                ["Permissions:EmailInvites:Delivery:PublicBaseUrl"] = "https://northstar.example"
            },
            services =>
            {
                services.RemoveAll<IEmailInviteDeliveryService>();
                services.AddSingleton<IEmailInviteDeliveryService>(new FailingEmailInviteDeliveryService());
            });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var document = FindDocument(bootstrap, "Our Principles");
        var email = $"delivery-failure-{Guid.NewGuid():N}@northstar.local";

        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            email,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.Equal(EmailInviteDeliveryStatuses.Failed, invite.Delivery.Status);
        Assert.Equal("fake-failure", invite.Delivery.Provider);
        Assert.NotNull(invite.Delivery.AttemptedAt);
        Assert.Equal("provider_error", invite.Delivery.ErrorCode);
        Assert.NotNull(persisted);
        Assert.Equal(EmailInviteDeliveryStatuses.Failed, persisted.DeliveryStatus);
        Assert.Equal("fake-failure", persisted.DeliveryProvider);
        Assert.NotNull(persisted.DeliveryAttemptedAt);
        Assert.Equal("provider_error", persisted.DeliveryErrorCode);
        Assert.NotEqual(invite.Token, persisted.TokenHash);
        Assert.DoesNotContain(invite.Token, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.TokenHash, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("https://northstar.example", auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmailInvites_SmtpProviderFailsClosedWhenConfigurationIncomplete()
    {
        const string smtpPassword = "smtp-password-never-persist";
        using var factory = new NorthstarApiFactory(
            new Dictionary<string, string?>
            {
                ["Permissions:EmailInvites:Delivery:Enabled"] = "true",
                ["Permissions:EmailInvites:Delivery:Provider"] = "smtp",
                ["Permissions:EmailInvites:Delivery:PublicBaseUrl"] = "https://northstar.example",
                ["Permissions:EmailInvites:Delivery:FromEmail"] = "invites@northstar.example",
                ["Permissions:EmailInvites:Delivery:FromName"] = "Northstar",
                ["Permissions:EmailInvites:Delivery:Smtp:Username"] = "smtp-user",
                ["Permissions:EmailInvites:Delivery:Smtp:Password"] = smtpPassword
            });
        using (var scope = factory.Services.CreateScope())
        {
            Assert.IsType<SmtpEmailInviteDeliveryService>(
                scope.ServiceProvider.GetRequiredService<IEmailInviteDeliveryService>());
        }

        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "admin");
        var document = FindDocument(bootstrap, "Our Principles");
        var adminId = Guid.Parse(adminTokens.User.Id);

        Authorize(client, ownerTokens);
        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            $"delivery-smtp-{Guid.NewGuid():N}@northstar.local",
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");
        var failedNotifications = await ReadNotificationsAsync(
            factory,
            adminId,
            PermissionNotificationTypes.EmailInviteDeliveryFailed);
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);
        var notificationRaw = JsonSerializer.Serialize(failedNotifications, JsonOptions);

        Assert.Equal(EmailInviteDeliveryStatuses.Failed, invite.Delivery.Status);
        Assert.Equal("smtp", invite.Delivery.Provider);
        Assert.NotNull(invite.Delivery.AttemptedAt);
        Assert.Equal("configuration_error", invite.Delivery.ErrorCode);
        Assert.NotNull(persisted);
        Assert.Equal(EmailInviteDeliveryStatuses.Failed, persisted.DeliveryStatus);
        Assert.Equal("smtp", persisted.DeliveryProvider);
        Assert.NotNull(persisted.DeliveryAttemptedAt);
        Assert.Equal("configuration_error", persisted.DeliveryErrorCode);
        Assert.Single(failedNotifications);
        Assert.DoesNotContain(invite.Token, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.TokenHash, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(invite.Url, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(smtpPassword, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("password", auditRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(invite.Token, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.TokenHash, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(invite.Url, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(smtpPassword, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("password", notificationRaw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmailInvites_CreateAcceptRevokeEmitFanoutNotificationsWithoutSecrets()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "admin");
        var invitedTokens = await RegisterAsync(client, $"fanout-invite-{Guid.NewGuid():N}@northstar.local");
        var document = FindDocument(bootstrap, "Our Principles");
        var adminId = Guid.Parse(adminTokens.User.Id);
        var invitedId = Guid.Parse(invitedTokens.User.Id);

        Authorize(client, ownerTokens);
        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            invitedTokens.User.Email,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));
        var createdNotifications = await ReadNotificationsAsync(
            factory,
            adminId,
            PermissionNotificationTypes.EmailInviteCreated);
        var notificationRaw = JsonSerializer.Serialize(createdNotifications, JsonOptions);

        Authorize(client, invitedTokens);
        var accept = await client.PostAsJsonAsync<object?>(
            $"/api/v1/permissions/email-invites/{invite.Token}/accept",
            null);

        Authorize(client, ownerTokens);
        var revoke = await client.DeleteAsync($"/api/v1/permissions/email-invites/{invite.Invite.Id}");

        Assert.NotNull(persisted);
        Assert.Single(createdNotifications);
        Assert.DoesNotContain(invite.Token, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.TokenHash, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(invite.Url, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain("tokenHash", notificationRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", notificationRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/api/v1/permissions/email-invites/", notificationRaw, StringComparison.OrdinalIgnoreCase);
        accept.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(1, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.EmailInviteCreated));
        Assert.Equal(1, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.EmailInviteAccepted));
        Assert.Equal(1, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.EmailInviteRevoked));
        Assert.Equal(1, await CountNotificationsAsync(factory, invitedId, PermissionNotificationTypes.EmailInviteRevoked));
    }

    [Fact]
    public async Task EmailInvites_DeliveryFailureEmitsManagerNotification()
    {
        using var factory = new NorthstarApiFactory(
            new Dictionary<string, string?>
            {
                ["Permissions:EmailInvites:Delivery:Enabled"] = "true",
                ["Permissions:EmailInvites:Delivery:Provider"] = "fake-failure",
                ["Permissions:EmailInvites:Delivery:PublicBaseUrl"] = "https://northstar.example"
            },
            services =>
            {
                services.RemoveAll<IEmailInviteDeliveryService>();
                services.AddSingleton<IEmailInviteDeliveryService>(new FailingEmailInviteDeliveryService());
            });
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "admin");
        var document = FindDocument(bootstrap, "Our Principles");
        var adminId = Guid.Parse(adminTokens.User.Id);

        Authorize(client, ownerTokens);
        var invite = await CreateEmailInviteAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            $"delivery-fanout-{Guid.NewGuid():N}@northstar.local",
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
        var persisted = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));
        var failedNotifications = await ReadNotificationsAsync(
            factory,
            adminId,
            PermissionNotificationTypes.EmailInviteDeliveryFailed);
        Authorize(client, adminTokens);
        var apiNotifications = await client.GetFromJsonAsync<PermissionNotificationsResponse>(
            $"/api/v1/notifications?workspaceId={bootstrap.Workspace.Id}");
        var failedNotification = Assert.Single(
            apiNotifications!.Notifications,
            item => item.Type == PermissionNotificationTypes.EmailInviteDeliveryFailed);
        var notificationRaw = JsonSerializer.Serialize(failedNotifications, JsonOptions);

        Authorize(client, ownerTokens);
        Assert.Equal(EmailInviteDeliveryStatuses.Failed, invite.Delivery.Status);
        Assert.NotNull(persisted);
        Assert.Single(failedNotifications);
        Assert.NotNull(failedNotification.Actor);
        Assert.Equal(ownerTokens.User.DisplayName, failedNotification.Actor.DisplayName);
        Assert.NotNull(failedNotification.Action);
        Assert.Equal("email_invite", failedNotification.Action.SubjectType);
        Assert.Equal(invite.Invite.Id, failedNotification.Action.SubjectId);
        Assert.Equal(1, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.EmailInviteCreated));
        Assert.Equal(1, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.EmailInviteDeliveryFailed));
        Assert.DoesNotContain(invite.Token, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(persisted.TokenHash, notificationRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(invite.Url, notificationRaw, StringComparison.Ordinal);

        var retry = await client.PostAsync($"/api/v1/permissions/email-invites/{invite.Invite.Id}/retry", null);
        var retried = await retry.Content.ReadFromJsonAsync<CreateEmailInviteResponse>();
        var originalAfterRetry = await ReadEmailInviteAsync(factory, Guid.Parse(invite.Invite.Id));

        retry.EnsureSuccessStatusCode();
        Assert.NotNull(retried);
        Assert.NotEqual(invite.Invite.Id, retried.Invite.Id);
        Assert.Equal(EmailInviteDeliveryStatuses.Failed, retried.Delivery.Status);
        Assert.NotNull(originalAfterRetry);
        Assert.Equal(EmailInviteStatuses.Revoked, originalAfterRetry.Status);
        Assert.Equal(2, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.EmailInviteCreated));
        Assert.Equal(2, await CountNotificationsAsync(factory, adminId, PermissionNotificationTypes.EmailInviteDeliveryFailed));
    }

    [Fact]
    public async Task EmailInvites_RejectPastExpiryExpiredAndUnknownTokens()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var invitedTokens = await RegisterAsync(client, $"invite-expired-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap!, "Our Principles");
        var pastExpiry = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/email-invites",
            new CreateEmailInviteRequest(invitedTokens.User.Email, PermissionRole.Viewer, DateTimeOffset.UtcNow.AddMinutes(-1)));
        var expiredToken = await SeedEmailInviteAsync(
            factory,
            Guid.Parse(bootstrap!.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            invitedTokens.User.Email,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddMinutes(-5));

        Authorize(client, invitedTokens);
        var unknownResolve = await client.GetAsync($"/api/v1/permissions/email-invites/{Guid.NewGuid():N}/resolve");
        var expiredAccept = await client.PostAsync($"/api/v1/permissions/email-invites/{expiredToken}/accept", null);
        var expiredGet = await client.GetAsync($"/api/v1/documents/{document.Id}");
        var expiredAuditCount = await CountAuditEventsAsync(factory, PermissionAuditActions.EmailInviteExpired);

        Assert.Equal(HttpStatusCode.BadRequest, pastExpiry.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownResolve.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, expiredAccept.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, expiredGet.StatusCode);
        Assert.True(expiredAuditCount >= 1);
    }

    [Fact]
    public async Task ShareAndInviteMutations_RequireResourceSharePermission()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, viewerTokens);
        var document = FindDocument(bootstrap, "Our Principles");

        var externalLink = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/share-links",
            new CreateShareLinkRequest(
                PermissionRole.Viewer,
                ShareLinkAudiences.External,
                DateTimeOffset.UtcNow.AddHours(1),
                $"blocked-{Guid.NewGuid():N}@northstar.local"));
        var invite = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/email-invites",
            new CreateEmailInviteRequest(
                $"blocked-invite-{Guid.NewGuid():N}@northstar.local",
                PermissionRole.Viewer,
                DateTimeOffset.UtcNow.AddHours(1)));

        Assert.Equal(HttpStatusCode.Forbidden, externalLink.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, invite.StatusCode);
    }

    [Fact]
    public async Task WorkspaceGroups_AdminCanCreateUpdateArchiveAndEditorCannotManage()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "admin");
        var editorTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "editor");

        Authorize(client, adminTokens);
        var created = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Design Review");
        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{created.Id}",
            new UpdateWorkspaceGroupRequest("Design Council", "Quarterly reviewers"));
        var archiveResponse = await client.DeleteAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{created.Id}");

        Authorize(client, editorTokens);
        var editorCreate = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups",
            new CreateWorkspaceGroupRequest("Editors Group", null, GroupTypes.Static));

        Authorize(client, ownerTokens);
        var groups = await client.GetFromJsonAsync<WorkspaceGroupsResponse>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups");
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}");

        updateResponse.EnsureSuccessStatusCode();
        archiveResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, editorCreate.StatusCode);
        Assert.NotNull(groups);
        Assert.Contains(groups.Groups, group => group.Id == created.Id && group.IsArchived);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GroupCreated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GroupUpdated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GroupArchived);
    }

    [Fact]
    public async Task WorkspaceGroups_AddRemoveMemberWritesAudit()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var group = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Readers");

        var addMember = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}/members",
            new AddWorkspaceGroupMemberRequest(viewerTokens.User.Id, null));
        var removeMember = await client.DeleteAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}/members/{viewerTokens.User.Id}");
        var detail = await client.GetFromJsonAsync<WorkspaceGroupDetailDto>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}");
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}");

        addMember.EnsureSuccessStatusCode();
        removeMember.EnsureSuccessStatusCode();
        Assert.NotNull(detail);
        Assert.Empty(detail.Members);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GroupMemberAdded);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GroupMemberRemoved);
    }

    [Fact]
    public async Task GroupGrant_ElevatesMemberAndWritesAudit()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Mission & Vision");
        var group = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Mission Editors");
        await AddWorkspaceGroupMemberAsync(client, bootstrap.Workspace.Id, group.Id, viewerTokens.User.Id);

        var grant = await CreateDocumentSubjectGrantAsync(
            client,
            document.Id,
            SubjectTypes.Group,
            group.Id,
            PermissionRole.Editor);

        Authorize(client, viewerTokens);
        var effective = await client.GetFromJsonAsync<EffectivePermissionResponse>(
            $"/api/v1/permissions/effective?resourceType=document&resourceId={document.Id}");

        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");

        Assert.Equal(SubjectTypes.Group, grant.SubjectType);
        Assert.NotNull(effective);
        Assert.Equal(PermissionRole.Editor, effective.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentGroupSource, effective.Source);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item =>
            item.Action == PermissionAuditActions.GrantCreated &&
            item.SubjectType == SubjectTypes.Group &&
            item.SubjectId == group.Id);
    }

    [Fact]
    public async Task GroupGrant_CreateUpdateRevokeFanoutRespectsActiveMembersActorAndMute()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var activeTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var removedTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");
        var expiredTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");
        var outsiderTokens = await RegisterAsync(client, $"group-outsider-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Mission & Vision");
        var group = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Fanout Readers");
        await AddWorkspaceGroupMemberAsync(client, bootstrap.Workspace.Id, group.Id, ownerTokens.User.Id);
        await AddWorkspaceGroupMemberAsync(client, bootstrap.Workspace.Id, group.Id, activeTokens.User.Id);
        await AddWorkspaceGroupMemberAsync(client, bootstrap.Workspace.Id, group.Id, removedTokens.User.Id);
        await client.DeleteAsync($"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}/members/{removedTokens.User.Id}");
        await SeedGroupMemberAsync(factory, Guid.Parse(group.Id), Guid.Parse(expiredTokens.User.Id), DateTimeOffset.UtcNow.AddMinutes(-5));
        await SeedGroupMemberAsync(factory, Guid.Parse(group.Id), Guid.Parse(outsiderTokens.User.Id), null);

        var grant = await CreateDocumentSubjectGrantAsync(
            client,
            document.Id,
            SubjectTypes.Group,
            group.Id,
            PermissionRole.Viewer);
        var update = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{grant.Id}",
            new UpdatePermissionGrantRequest(PermissionRole.Editor, null, "fanout update"));

        Authorize(client, activeTokens);
        var mute = await client.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdatePermissionNotificationPreferenceRequest(
                bootstrap.Workspace.Id,
                ResourceTypes.Document,
                document.Id,
                Watched: false,
                Muted: true));

        Authorize(client, ownerTokens);
        var revokeRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{grant.Id}")
        {
            Content = JsonContent.Create(new RevokePermissionGrantRequest("fanout revoke"))
        };
        var revoke = await client.SendAsync(revokeRequest);

        update.EnsureSuccessStatusCode();
        mute.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(1, await CountNotificationsAsync(factory, Guid.Parse(activeTokens.User.Id), PermissionNotificationTypes.GrantCreated));
        Assert.Equal(1, await CountNotificationsAsync(factory, Guid.Parse(activeTokens.User.Id), PermissionNotificationTypes.GrantUpdated));
        Assert.Equal(0, await CountNotificationsAsync(factory, Guid.Parse(activeTokens.User.Id), PermissionNotificationTypes.GrantRevoked));
        Assert.Equal(0, await CountNotificationsAsync(factory, Guid.Parse(ownerTokens.User.Id), PermissionNotificationTypes.GrantCreated));
        Assert.Equal(0, await CountNotificationsAsync(factory, Guid.Parse(removedTokens.User.Id), PermissionNotificationTypes.GrantCreated));
        Assert.Equal(0, await CountNotificationsAsync(factory, Guid.Parse(expiredTokens.User.Id), PermissionNotificationTypes.GrantCreated));
        Assert.Equal(0, await CountNotificationsAsync(factory, Guid.Parse(outsiderTokens.User.Id), PermissionNotificationTypes.GrantCreated));
    }

    [Fact]
    public async Task AccessRequests_CreatedFanoutIncludesGroupManagersAndDeduplicatesRecipients()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var managerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var requesterTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");
        Authorize(client, ownerTokens);
        var document = FindDocument(bootstrap, "Mission & Vision");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);
        var group = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Document Managers");
        await AddWorkspaceGroupMemberAsync(client, bootstrap.Workspace.Id, group.Id, managerTokens.User.Id);
        await CreateDocumentSubjectGrantAsync(
            client,
            document.Id,
            SubjectTypes.Group,
            group.Id,
            PermissionRole.Admin);
        await CreateDocumentGrantAsync(client, document.Id, managerTokens.User.Id, PermissionRole.Admin);

        Authorize(client, requesterTokens);
        _ = await CreateAccessRequestAsync(client, document.Id, PermissionRole.Viewer);

        Assert.Equal(1, await CountNotificationsAsync(factory, Guid.Parse(managerTokens.User.Id), PermissionNotificationTypes.AccessRequestCreated));
    }

    [Fact]
    public async Task GroupGrant_RejectsInvalidOrCrossWorkspaceGroup()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Mission & Vision");
        var otherWorkspaceGroupId = await SeedOtherWorkspaceGroupAsync(factory);

        var invalidGroup = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants",
            new CreatePermissionGrantRequest(SubjectTypes.Group, Guid.NewGuid().ToString(), PermissionRole.Viewer, null, null));
        var crossWorkspaceGroup = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants",
            new CreatePermissionGrantRequest(SubjectTypes.Group, otherWorkspaceGroupId.ToString(), PermissionRole.Viewer, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, invalidGroup.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, crossWorkspaceGroup.StatusCode);
    }

    [Fact]
    public async Task IamSync_CreatesExternalUsersGroupsMembersAndIsIdempotent()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var request = CreateIamSyncRequest(bootstrap!.Workspace.Id);

        var firstSync = await SyncIamAsync(client, bootstrap.Workspace.Id, request);
        var secondSync = await SyncIamAsync(client, bootstrap.Workspace.Id, request);
        var group = await ReadExternalGroupAsync(factory, Guid.Parse(bootstrap.Workspace.Id), "okta", "eng");
        var groupsResponse = await client.GetFromJsonAsync<WorkspaceGroupsResponse>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups");
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.Equal(2, firstSync.Counts.UsersCreated);
        Assert.Equal(2, firstSync.Counts.WorkspaceMembersCreated);
        Assert.Equal(1, firstSync.Counts.GroupsCreated);
        Assert.Equal(2, firstSync.Counts.MembersAdded);
        Assert.Equal(0, secondSync.Counts.UsersCreated);
        Assert.Equal(0, secondSync.Counts.WorkspaceMembersCreated);
        Assert.Equal(0, secondSync.Counts.GroupsCreated);
        Assert.Equal(0, secondSync.Counts.MembersAdded);
        Assert.NotNull(group);
        Assert.Equal(GroupTypes.Dynamic, group.Type);
        Assert.Equal("okta", group.ExternalProvider);
        Assert.NotNull(group.ExternalSyncedAt);
        Assert.Equal(2, await CountExternalUsersAsync(factory, "okta"));
        Assert.Equal(1, await CountExternalGroupsAsync(factory, Guid.Parse(bootstrap.Workspace.Id), "okta"));
        Assert.Equal(2, await CountActiveGroupMembersAsync(factory, group.Id));
        Assert.NotNull(groupsResponse);
        Assert.Contains(groupsResponse.Groups, item =>
            item.Id == group.Id.ToString() &&
            item.ExternalProvider == "okta" &&
            item.ExternalGroupId == "eng" &&
            item.ExternalSyncedAt.HasValue);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamUserMapped);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamGroupSynced);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamGroupMemberAdded);
        Assert.DoesNotContain("token", auditRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", auditRaw, StringComparison.OrdinalIgnoreCase);

        Authorize(client, ownerTokens);
    }

    [Fact]
    public async Task IamSync_RemovesMissingMemberWithoutDeletingLocalUserOrGroup()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var workspaceId = Guid.Parse(bootstrap!.Workspace.Id);
        await SyncIamAsync(client, bootstrap.Workspace.Id, CreateIamSyncRequest(bootstrap.Workspace.Id));

        var secondRequest = new IamSyncRequest(
            "okta",
            [
                new IamSyncUserRequest("u-alpha", "alpha@example.test", "Alpha User", null, bootstrap.Workspace.Id),
                new IamSyncUserRequest("u-beta", "beta@example.test", "Beta User", null, bootstrap.Workspace.Id)
            ],
            [
                new IamSyncGroupRequest("eng", "Engineering", "Synced engineering group", ["u-alpha"], bootstrap.Workspace.Id)
            ]);
        var secondSync = await SyncIamAsync(client, bootstrap.Workspace.Id, secondRequest);
        var group = await ReadExternalGroupAsync(factory, workspaceId, "okta", "eng");
        var beta = await ReadExternalUserAsync(factory, "okta", "u-beta");

        Assert.Equal(1, secondSync.Counts.MembersRemoved);
        Assert.NotNull(group);
        Assert.NotNull(beta);
        Assert.False(group.IsArchived);
        Assert.Equal(2, await CountExternalUsersAsync(factory, "okta"));
        Assert.Equal(1, await CountExternalGroupsAsync(factory, workspaceId, "okta"));
        Assert.Equal(1, await CountActiveGroupMembersAsync(factory, group.Id));
        Assert.True(await UserIsWorkspaceMemberAsync(factory, workspaceId, beta.Id));
    }

    [Fact]
    public async Task IamSync_ManagedGroupsAreReadOnlyThroughNormalGroupApis()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        await SyncIamAsync(client, bootstrap!.Workspace.Id, CreateIamSyncRequest(bootstrap.Workspace.Id));
        var group = await ReadExternalGroupAsync(factory, Guid.Parse(bootstrap.Workspace.Id), "okta", "eng");
        var localViewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");
        Assert.NotNull(group);

        Authorize(client, ownerTokens);
        var update = await client.PatchAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}",
            new UpdateWorkspaceGroupRequest("Renamed Engineering", null));
        var archive = await client.DeleteAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}");
        var addMember = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}/members",
            new AddWorkspaceGroupMemberRequest(localViewerTokens.User.Id, null));
        var removeMember = await client.DeleteAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/groups/{group.Id}/members/{localViewerTokens.User.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, archive.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, addMember.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, removeMember.StatusCode);
    }

    [Fact]
    public async Task IamSync_ManagedGroupGrantCanAuthorizeCollectionChildDocument()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Mission & Vision");
        var request = new IamSyncRequest(
            "okta",
            [
                new IamSyncUserRequest("u-viewer", viewerTokens.User.Email, viewerTokens.User.DisplayName, null, bootstrap.Workspace.Id)
            ],
            [
                new IamSyncGroupRequest("reviewers", "Reviewers", "Synced reviewers", ["u-viewer"], bootstrap.Workspace.Id)
            ]);
        await SyncIamAsync(client, bootstrap.Workspace.Id, request);
        var group = await ReadExternalGroupAsync(factory, Guid.Parse(bootstrap.Workspace.Id), "okta", "reviewers");
        Assert.NotNull(group);
        await SetResourcePolicyAsync(client, ResourceTypes.Collection, document.FolderId, InheritanceModes.Restricted, LinkModes.Disabled);
        await CreateSubjectGrantAsync(
            client,
            ResourceTypes.Collection,
            document.FolderId,
            SubjectTypes.Group,
            group.Id.ToString(),
            PermissionRole.Commenter);

        Authorize(client, viewerTokens);
        var read = await client.GetAsync($"/api/v1/documents/{document.Id}");
        var comment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "iam group grant comment"));
        var currentDocument = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var edit = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}",
            new UpdateDocumentRequest(currentDocument!.Document.Revision, "IAM edit denied", null, null));

        read.EnsureSuccessStatusCode();
        comment.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
    }

    [Fact]
    public async Task IamSync_RejectsNonAdminAndCrossWorkspacePayload()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var editorTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "editor");
        var request = CreateIamSyncRequest(bootstrap.Workspace.Id);

        Authorize(client, editorTokens);
        var editorSync = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/iam/sync",
            request);

        Authorize(client, ownerTokens);
        var crossWorkspaceSync = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/iam/sync",
            new IamSyncRequest(
                "okta",
                [
                    new IamSyncUserRequest("u-cross", "cross@example.test", "Cross User", null, Guid.NewGuid().ToString())
                ],
                []));

        Assert.Equal(HttpStatusCode.Forbidden, editorSync.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, crossWorkspaceSync.StatusCode);
    }

    [Fact]
    public async Task ScimDiscovery_ReturnsWorkspaceScopedSkeleton()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");

        var config = await client.GetFromJsonAsync<ScimServiceProviderConfigResponse>(
            $"/api/v1/workspaces/{bootstrap!.Workspace.Id}/scim/v2/ServiceProviderConfig");
        var schemas = await client.GetFromJsonAsync<ScimListResponse<ScimSchemaDto>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Schemas");
        var resourceTypes = await client.GetFromJsonAsync<ScimListResponse<ScimResourceTypeDto>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/ResourceTypes");

        Assert.NotNull(config);
        Assert.Contains("urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig", config.Schemas);
        Assert.True(config.Patch.Supported);
        Assert.False(config.Bulk.Supported);
        Assert.True(config.Filter.Supported);
        Assert.Contains(config.AuthenticationSchemes, scheme => scheme.Type == "oauthbearertoken" && scheme.Primary);
        Assert.NotNull(schemas);
        Assert.Contains(schemas.Resources, schema => schema.Id == "urn:ietf:params:scim:schemas:core:2.0:User");
        Assert.Contains(schemas.Resources, schema => schema.Id == "urn:ietf:params:scim:schemas:core:2.0:Group");
        Assert.NotNull(resourceTypes);
        Assert.Contains(resourceTypes.Resources, resource => resource.Endpoint == "/Users");
        Assert.Contains(resourceTypes.Resources, resource => resource.Endpoint == "/Groups");
    }

    [Fact]
    public async Task ScimManagement_RequiresAuthenticationAndWorkspaceManagePermission()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var unauthenticated = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{Guid.NewGuid()}/scim/v2/Users",
            new { userName = "scim@example.test" });

        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");

        Authorize(client, viewerTokens);
        var unauthorized = await client.GetAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/ServiceProviderConfig");

        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorized.StatusCode);
    }

    [Fact]
    public async Task ScimProvisioningOperations_RequireDedicatedBearerToken()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");

        var userCreate = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap!.Workspace.Id}/scim/v2/Users",
            new CreateScimUserRequest("scim@example.test", "scim-user", "SCIM User", null, true));
        var groupCreate = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups",
            new CreateScimGroupRequest("SCIM Group", "scim-group", null));

        await AssertScimUnauthorizedAsync(userCreate);
        await AssertScimUnauthorizedAsync(groupCreate);
    }

    [Fact]
    public async Task ScimDiscovery_AcceptsDedicatedBearerTokenAndDoesNotCreateMembership()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var workspaceId = Guid.Parse(bootstrap!.Workspace.Id);
        var beforeMembers = await CountWorkspaceMembersAsync(factory, workspaceId);
        var created = await CreateScimTokenAsync(client, bootstrap.Workspace.Id);

        AuthorizeBearer(client, created.RawToken);
        var config = await client.GetFromJsonAsync<ScimServiceProviderConfigResponse>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/ServiceProviderConfig");

        var stored = await ReadScimTokenAsync(factory, Guid.Parse(created.Token.Id));
        var afterMembers = await CountWorkspaceMembersAsync(factory, workspaceId);
        Assert.NotNull(config);
        Assert.NotNull(stored);
        Assert.NotNull(stored.LastUsedAt);
        Assert.Equal(beforeMembers, afterMembers);
    }

    [Fact]
    public async Task ScimDiscovery_RejectsCrossWorkspaceRevokedAndExpiredTokensWithoutDetails()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var workspaceId = Guid.Parse(bootstrap!.Workspace.Id);
        var tokenService = new ShareLinkTokenService();
        var crossWorkspaceRawToken = tokenService.GenerateToken();
        var revokedRawToken = tokenService.GenerateToken();
        var expiredRawToken = tokenService.GenerateToken();

        await SeedScimTokenAsync(
            factory,
            Guid.NewGuid(),
            crossWorkspaceRawToken,
            DateTimeOffset.UtcNow.AddHours(1));
        await SeedScimTokenAsync(
            factory,
            workspaceId,
            revokedRawToken,
            DateTimeOffset.UtcNow.AddHours(1),
            revoked: true);
        await SeedScimTokenAsync(
            factory,
            workspaceId,
            expiredRawToken,
            DateTimeOffset.UtcNow.AddMinutes(-5));

        var url = $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/ServiceProviderConfig";
        AuthorizeBearer(client, crossWorkspaceRawToken);
        var crossWorkspace = await client.GetAsync(url);
        AuthorizeBearer(client, revokedRawToken);
        var revoked = await client.GetAsync(url);
        AuthorizeBearer(client, expiredRawToken);
        var expired = await client.GetAsync(url);

        await AssertScimUnauthorizedAsync(crossWorkspace);
        await AssertScimUnauthorizedAsync(revoked);
        await AssertScimUnauthorizedAsync(expired);
    }

    [Fact]
    public async Task ScimUsers_CreateListGetPatchAndRejectUnsupportedOperations()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var created = await CreateScimTokenAsync(client, bootstrap!.Workspace.Id);

        AuthorizeBearer(client, created.RawToken);
        var userCreateResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users",
            new CreateScimUserRequest("scim-alpha@example.test", "scim-alpha", "SCIM Alpha", null, true));
        userCreateResponse.EnsureSuccessStatusCode();
        var user = await userCreateResponse.Content.ReadFromJsonAsync<ScimUserResource>();
        Assert.NotNull(user);

        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users/{user.Id}",
            new ScimPatchRequest(
                null,
                [
                    new ScimPatchOperationDto(
                        "replace",
                        "displayName",
                        JsonSerializer.Deserialize<JsonElement>("\"SCIM Alpha Renamed\""))
                ]));
        patch.EnsureSuccessStatusCode();
        var patched = await patch.Content.ReadFromJsonAsync<ScimUserResource>();
        var list = await client.GetFromJsonAsync<ScimListResponse<ScimUserResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users?filter={Uri.EscapeDataString("userName eq \"scim-alpha@example.test\"")}");
        var get = await client.GetFromJsonAsync<ScimUserResource>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users/{user.Id}");
        var unsupportedFilter = await client.GetAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users?filter={Uri.EscapeDataString("title eq \"x\"")}");
        var delete = await client.DeleteAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users/{user.Id}");

        var persisted = await ReadExternalUserAsync(factory, "scim", "scim-alpha");
        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.NotNull(patched);
        Assert.Equal("SCIM Alpha Renamed", patched.DisplayName);
        Assert.NotNull(list);
        Assert.Single(list.Resources);
        Assert.NotNull(get);
        Assert.Equal(user.Id, get.Id);
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedFilter.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
        Assert.NotNull(persisted);
        Assert.Equal("scim-alpha@example.test", persisted.Email);
        Assert.Equal("SCIM Alpha Renamed", persisted.DisplayName);
        Assert.True(await UserIsWorkspaceMemberAsync(factory, Guid.Parse(bootstrap.Workspace.Id), persisted.Id));
        Assert.Equal(0, await CountUserCredentialsAsync(factory, persisted.Id));
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamUserMapped);
        Assert.DoesNotContain(created.RawToken, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScimUsers_BindExistingEmailOnlyWhenExternalIdentityDoesNotConflict()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var email = $"scim-bind-{Guid.NewGuid():N}@northstar.local";
        var localTokens = await RegisterAsync(client, email);

        Authorize(client, ownerTokens);
        var created = await CreateScimTokenAsync(client, bootstrap!.Workspace.Id);

        AuthorizeBearer(client, created.RawToken);
        var bind = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users",
            new CreateScimUserRequest(email, "local-bind", "SCIM Bound User", null, true));
        bind.EnsureSuccessStatusCode();
        var bound = await bind.Content.ReadFromJsonAsync<ScimUserResource>();
        var conflict = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users",
            new CreateScimUserRequest(email, "local-conflict", "SCIM Conflict User", null, true));

        Assert.NotNull(bound);
        Assert.Equal(localTokens.User.Id, bound.Id);
        Assert.Equal(HttpStatusCode.BadRequest, conflict.StatusCode);
        Assert.True(await UserIsWorkspaceMemberAsync(factory, Guid.Parse(bootstrap.Workspace.Id), Guid.Parse(localTokens.User.Id)));
        Assert.Equal(
            WorkspaceMemberRole.Viewer,
            await ReadWorkspaceMemberRoleAsync(factory, Guid.Parse(bootstrap.Workspace.Id), Guid.Parse(localTokens.User.Id)));
    }

    [Fact]
    public async Task ScimUsers_PutReplaceUpdatesProfileAndPreservesIdentity()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var created = await CreateScimTokenAsync(client, bootstrap!.Workspace.Id);

        AuthorizeBearer(client, created.RawToken);
        var user = await CreateScimUserAsync(
            client,
            bootstrap.Workspace.Id,
            "scim-put-alpha@example.test",
            "put-alpha",
            "SCIM Put Alpha");
        var replace = await client.PutAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users/{user.Id}",
            new CreateScimUserRequest(
                "scim-put-alpha-renamed@example.test",
                "put-alpha",
                "SCIM Put Renamed",
                null,
                true));
        var replaceRaw = await replace.Content.ReadAsStringAsync();
        var replaced = JsonSerializer.Deserialize<ScimUserResource>(replaceRaw, JsonOptions);
        var identityChange = await client.PutAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users/{user.Id}",
            new CreateScimUserRequest(
                "scim-put-alpha-renamed@example.test",
                "put-alpha-other",
                "Illegal Identity Change",
                null,
                true));
        var identityChangeError = await identityChange.Content.ReadFromJsonAsync<ApiErrorResponse>();

        var persisted = await ReadExternalUserAsync(factory, "scim", "put-alpha");
        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        replace.EnsureSuccessStatusCode();
        Assert.NotNull(replaced);
        Assert.Equal(user.Id, replaced.Id);
        Assert.Equal("put-alpha", replaced.ExternalId);
        Assert.Equal("scim-put-alpha-renamed@example.test", replaced.UserName);
        Assert.Equal("SCIM Put Renamed", replaced.DisplayName);
        Assert.Equal(HttpStatusCode.BadRequest, identityChange.StatusCode);
        Assert.NotNull(identityChangeError);
        Assert.Equal(ErrorCodes.ValidationError, identityChangeError.Error.Code);
        Assert.NotNull(persisted);
        Assert.Equal("scim-put-alpha-renamed@example.test", persisted.Email);
        Assert.Equal("SCIM Put Renamed", persisted.DisplayName);
        Assert.Equal(0, await CountUserCredentialsAsync(factory, persisted.Id));
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamUserMapped);
        Assert.DoesNotContain(created.RawToken, replaceRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(created.RawToken, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScimGroups_CreateListGetPatchSyncMembersAndRejectLocalGroupMutation()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var workspaceId = Guid.Parse(bootstrap!.Workspace.Id);
        var created = await CreateScimTokenAsync(client, bootstrap.Workspace.Id);

        AuthorizeBearer(client, created.RawToken);
        var alpha = await CreateScimUserAsync(
            client,
            bootstrap.Workspace.Id,
            "scim-group-alpha@example.test",
            "group-alpha",
            "Group Alpha");
        var beta = await CreateScimUserAsync(
            client,
            bootstrap.Workspace.Id,
            "scim-group-beta@example.test",
            "group-beta",
            "Group Beta");
        var createGroup = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups",
            new CreateScimGroupRequest(
                "SCIM Team",
                "team",
                [new ScimGroupMemberDto(alpha.Id, null)]));
        createGroup.EnsureSuccessStatusCode();
        var group = await createGroup.Content.ReadFromJsonAsync<ScimGroupResource>();
        Assert.NotNull(group);

        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups/{group.Id}",
            new ScimPatchRequest(
                null,
                [
                    new ScimPatchOperationDto(
                        "replace",
                        "displayName",
                        JsonSerializer.Deserialize<JsonElement>("\"SCIM Team Renamed\"")),
                    new ScimPatchOperationDto(
                        "replace",
                        "members",
                        JsonSerializer.Deserialize<JsonElement>($$"""[{"value":"{{beta.Id}}"}]"""))
                ]));
        patch.EnsureSuccessStatusCode();
        var patched = await patch.Content.ReadFromJsonAsync<ScimGroupResource>();
        var list = await client.GetFromJsonAsync<ScimListResponse<ScimGroupResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups?filter={Uri.EscapeDataString("externalId eq \"team\"")}");
        var get = await client.GetFromJsonAsync<ScimGroupResource>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups/{group.Id}");
        var unsupportedDelete = await client.DeleteAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups/{group.Id}");

        Authorize(client, ownerTokens);
        var localGroup = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Local SCIM Rejection");
        AuthorizeBearer(client, created.RawToken);
        var localPatch = await client.PatchAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups/{localGroup.Id}",
            new ScimPatchRequest(
                null,
                [
                    new ScimPatchOperationDto(
                        "replace",
                        "displayName",
                        JsonSerializer.Deserialize<JsonElement>("\"Illegal Rename\""))
                ]));

        var persisted = await ReadExternalGroupAsync(factory, workspaceId, "scim", "team");
        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.NotNull(patched);
        Assert.Equal("SCIM Team Renamed", patched.DisplayName);
        Assert.Single(patched.Members);
        Assert.Equal(beta.Id, Assert.Single(patched.Members).Value);
        Assert.NotNull(list);
        Assert.Single(list.Resources);
        Assert.NotNull(get);
        Assert.Equal("SCIM Team Renamed", get.DisplayName);
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedDelete.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, localPatch.StatusCode);
        Assert.NotNull(persisted);
        Assert.Equal(GroupTypes.Dynamic, persisted.Type);
        Assert.Equal("scim", persisted.ExternalProvider);
        Assert.Equal(1, await CountActiveGroupMembersAsync(factory, persisted.Id));
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamGroupSynced);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamGroupMemberAdded);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamGroupMemberRemoved);
        Assert.DoesNotContain(created.RawToken, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScimGroups_PutReplaceUpdatesDisplayNameMembersAndRejectsLocalGroupMutation()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var workspaceId = Guid.Parse(bootstrap!.Workspace.Id);
        var created = await CreateScimTokenAsync(client, bootstrap.Workspace.Id);

        AuthorizeBearer(client, created.RawToken);
        var alpha = await CreateScimUserAsync(
            client,
            bootstrap.Workspace.Id,
            "scim-put-group-alpha@example.test",
            "put-group-alpha",
            "Put Group Alpha");
        var beta = await CreateScimUserAsync(
            client,
            bootstrap.Workspace.Id,
            "scim-put-group-beta@example.test",
            "put-group-beta",
            "Put Group Beta");
        var createGroup = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups",
            new CreateScimGroupRequest(
                "SCIM Put Team",
                "put-team",
                [
                    new ScimGroupMemberDto(alpha.Id, null),
                    new ScimGroupMemberDto(beta.Id, null)
                ]));
        createGroup.EnsureSuccessStatusCode();
        var group = await createGroup.Content.ReadFromJsonAsync<ScimGroupResource>();
        Assert.NotNull(group);

        var replace = await client.PutAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups/{group.Id}",
            new CreateScimGroupRequest(
                "SCIM Put Team Renamed",
                "put-team",
                [new ScimGroupMemberDto(beta.Id, null)]));
        var replaceRaw = await replace.Content.ReadAsStringAsync();
        var replaced = JsonSerializer.Deserialize<ScimGroupResource>(replaceRaw, JsonOptions);
        var identityChange = await client.PutAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups/{group.Id}",
            new CreateScimGroupRequest("Illegal Identity Change", "put-team-other", null));
        var identityChangeError = await identityChange.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Authorize(client, ownerTokens);
        var localGroup = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Local SCIM Put Rejection");
        AuthorizeBearer(client, created.RawToken);
        var localPut = await client.PutAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups/{localGroup.Id}",
            new CreateScimGroupRequest("Illegal Rename", "local-group", []));

        var persisted = await ReadExternalGroupAsync(factory, workspaceId, "scim", "put-team");
        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        replace.EnsureSuccessStatusCode();
        Assert.NotNull(replaced);
        Assert.Equal(group.Id, replaced.Id);
        Assert.Equal("put-team", replaced.ExternalId);
        Assert.Equal("SCIM Put Team Renamed", replaced.DisplayName);
        Assert.Single(replaced.Members);
        Assert.Equal(beta.Id, Assert.Single(replaced.Members).Value);
        Assert.Equal(HttpStatusCode.BadRequest, identityChange.StatusCode);
        Assert.NotNull(identityChangeError);
        Assert.Equal(ErrorCodes.ValidationError, identityChangeError.Error.Code);
        Assert.Equal(HttpStatusCode.BadRequest, localPut.StatusCode);
        Assert.NotNull(persisted);
        Assert.Equal("SCIM Put Team Renamed", persisted.Name);
        Assert.Equal(1, await CountActiveGroupMembersAsync(factory, persisted.Id));
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamGroupSynced);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.IamGroupMemberRemoved);
        Assert.DoesNotContain(created.RawToken, replaceRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(created.RawToken, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScimUsersAndGroups_FiltersPaginationAndValidationAreStable()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var created = await CreateScimTokenAsync(client, bootstrap!.Workspace.Id);

        AuthorizeBearer(client, created.RawToken);
        var alpha = await CreateScimUserAsync(
            client,
            bootstrap.Workspace.Id,
            "scim-filter-alpha@example.test",
            "filter-alpha",
            "Filter Alpha");
        var beta = await CreateScimUserAsync(
            client,
            bootstrap.Workspace.Id,
            "scim-filter-beta@example.test",
            "filter-beta",
            "Filter Beta");
        var gamma = await CreateScimUserAsync(
            client,
            bootstrap.Workspace.Id,
            "scim-filter-gamma@example.test",
            "filter-gamma",
            "Filter Gamma");
        var groupAlphaResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups",
            new CreateScimGroupRequest("SCIM Filter Alpha", "filter-group-alpha", [new ScimGroupMemberDto(alpha.Id, null)]));
        var groupBetaResponse = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups",
            new CreateScimGroupRequest("SCIM Filter Beta", "filter-group-beta", [new ScimGroupMemberDto(beta.Id, null)]));
        groupAlphaResponse.EnsureSuccessStatusCode();
        groupBetaResponse.EnsureSuccessStatusCode();

        var usersNoFilter = await client.GetFromJsonAsync<ScimListResponse<ScimUserResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users");
        var usersByUserName = await client.GetFromJsonAsync<ScimListResponse<ScimUserResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users?filter={Uri.EscapeDataString("userName eq \"scim-filter-beta@example.test\"")}");
        var usersByExternalId = await client.GetFromJsonAsync<ScimListResponse<ScimUserResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users?filter={Uri.EscapeDataString("externalId eq \"filter-gamma\"")}");
        var usersPage = await client.GetFromJsonAsync<ScimListResponse<ScimUserResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users?startIndex=2&count=1");
        var usersBoundedPage = await client.GetFromJsonAsync<ScimListResponse<ScimUserResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users?startIndex=-5&count=0");
        var unsupportedUserFilter = await client.GetAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Users?filter={Uri.EscapeDataString("userName co \"filter\"")}");
        var unsupportedUserFilterError = await unsupportedUserFilter.Content.ReadFromJsonAsync<ApiErrorResponse>();

        var groupsNoFilter = await client.GetFromJsonAsync<ScimListResponse<ScimGroupResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups");
        var groupsByDisplayName = await client.GetFromJsonAsync<ScimListResponse<ScimGroupResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups?filter={Uri.EscapeDataString("displayName eq \"SCIM Filter Alpha\"")}");
        var groupsByExternalId = await client.GetFromJsonAsync<ScimListResponse<ScimGroupResource>>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups?filter={Uri.EscapeDataString("externalId eq \"filter-group-beta\"")}");
        var unsupportedGroupFilter = await client.GetAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/Groups?filter={Uri.EscapeDataString("members eq \"x\"")}");
        var unsupportedGroupFilterError = await unsupportedGroupFilter.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(usersNoFilter);
        Assert.Equal(3, usersNoFilter.TotalResults);
        Assert.Equal(3, usersNoFilter.ItemsPerPage);
        Assert.NotNull(usersByUserName);
        Assert.Single(usersByUserName.Resources);
        Assert.Equal(beta.Id, Assert.Single(usersByUserName.Resources).Id);
        Assert.NotNull(usersByExternalId);
        Assert.Single(usersByExternalId.Resources);
        Assert.Equal(gamma.Id, Assert.Single(usersByExternalId.Resources).Id);
        Assert.NotNull(usersPage);
        Assert.Equal(3, usersPage.TotalResults);
        Assert.Equal(1, usersPage.ItemsPerPage);
        Assert.Equal(2, usersPage.StartIndex);
        Assert.Single(usersPage.Resources);
        Assert.NotNull(usersBoundedPage);
        Assert.Equal(1, usersBoundedPage.StartIndex);
        Assert.Equal(1, usersBoundedPage.ItemsPerPage);
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedUserFilter.StatusCode);
        Assert.NotNull(unsupportedUserFilterError);
        Assert.Equal(ErrorCodes.ValidationError, unsupportedUserFilterError.Error.Code);
        Assert.Equal("Unsupported SCIM filter.", unsupportedUserFilterError.Error.Message);

        Assert.NotNull(groupsNoFilter);
        Assert.Equal(2, groupsNoFilter.TotalResults);
        Assert.NotNull(groupsByDisplayName);
        Assert.Single(groupsByDisplayName.Resources);
        Assert.Equal("SCIM Filter Alpha", Assert.Single(groupsByDisplayName.Resources).DisplayName);
        Assert.NotNull(groupsByExternalId);
        Assert.Single(groupsByExternalId.Resources);
        Assert.Equal("filter-group-beta", Assert.Single(groupsByExternalId.Resources).ExternalId);
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedGroupFilter.StatusCode);
        Assert.NotNull(unsupportedGroupFilterError);
        Assert.Equal(ErrorCodes.ValidationError, unsupportedGroupFilterError.Error.Code);
        Assert.Equal("Unsupported SCIM filter.", unsupportedGroupFilterError.Error.Message);
    }

    [Fact]
    public async Task ScimTokenManagement_RequiresManagerAndKeepsSecretsOutOfStorageListAndAudit()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var unauthenticated = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{Guid.NewGuid()}/scim/tokens",
            new CreateScimTokenRequest("directory sync", DateTimeOffset.UtcNow.AddHours(1)));

        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");

        Authorize(client, viewerTokens);
        var forbidden = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/tokens",
            new CreateScimTokenRequest("directory sync", DateTimeOffset.UtcNow.AddHours(1)));

        Authorize(client, ownerTokens);
        var created = await CreateScimTokenAsync(client, bootstrap.Workspace.Id);
        var stored = await ReadScimTokenAsync(factory, Guid.Parse(created.Token.Id));
        Assert.NotNull(stored);
        var listResponse = await client.GetAsync($"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/tokens");
        var listRaw = await listResponse.Content.ReadAsStringAsync();
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}");
        var auditRaw = JsonSerializer.Serialize(audit, JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(created.RawToken));
        Assert.Equal("directory sync", created.Token.Name);
        Assert.NotEqual(created.RawToken, stored.TokenHash);
        Assert.DoesNotContain(created.RawToken, listRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(stored.TokenHash, listRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(created.RawToken, auditRaw, StringComparison.Ordinal);
        Assert.DoesNotContain(stored.TokenHash, auditRaw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScimTokenRevoke_RejectsRevokedBearerToken()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var created = await CreateScimTokenAsync(client, bootstrap!.Workspace.Id);

        var revoke = await client.DeleteAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/tokens/{created.Token.Id}");

        AuthorizeBearer(client, created.RawToken);
        var discovery = await client.GetAsync(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/v2/ServiceProviderConfig");

        Authorize(client, ownerTokens);
        var listed = await client.GetFromJsonAsync<ScimTokensResponse>(
            $"/api/v1/workspaces/{bootstrap.Workspace.Id}/scim/tokens");

        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        await AssertScimUnauthorizedAsync(discovery);
        Assert.NotNull(listed);
        Assert.Contains(listed.Tokens, token => token.Id == created.Token.Id && token.RevokedAt.HasValue);
    }

    [Fact]
    public async Task ScimTokensMigration_DefinesWorkspaceScopedHashOnlyTokens()
    {
        var migrationsDirectory = Path.GetDirectoryName(FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260502201249_AddScimTokensPhase14.cs"));
        Assert.NotNull(migrationsDirectory);
        var migrationPath = Assert.Single(
            Directory.GetFiles(migrationsDirectory, "*AddScimTokensPhase14.cs")
                .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal)));
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("scim_tokens", migration, StringComparison.Ordinal);
        Assert.Contains("workspace_id", migration, StringComparison.Ordinal);
        Assert.Contains("token_hash", migration, StringComparison.Ordinal);
        Assert.Contains("created_by", migration, StringComparison.Ordinal);
        Assert.Contains("expires_at", migration, StringComparison.Ordinal);
        Assert.Contains("revoked_at", migration, StringComparison.Ordinal);
        Assert.Contains("last_used_at", migration, StringComparison.Ordinal);
        Assert.Contains("idx_scim_tokens_token_hash", migration, StringComparison.Ordinal);
        Assert.Contains("idx_scim_tokens_workspace_active", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("raw_token", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RestrictedDocument_RequiresDirectGrantForReadAndEdit()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);

        Authorize(client, viewerTokens);
        var viewerDenied = await client.GetAsync($"/api/v1/documents/{document.Id}");

        Authorize(client, ownerTokens);
        await CreateDocumentGrantAsync(client, document.Id, viewerTokens.User.Id, PermissionRole.Viewer);

        Authorize(client, viewerTokens);
        var viewerAllowed = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var viewerEditDenied = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}",
            new UpdateDocumentRequest(viewerAllowed!.Document.Revision, "Viewer rename denied", null, null));

        Authorize(client, ownerTokens);
        var grantState = await ReadUserGrantAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            Guid.Parse(viewerTokens.User.Id));
        Assert.NotNull(grantState);
        var updateGrantResponse = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/document/{document.Id}/grants/{grantState.Id}",
            new UpdatePermissionGrantRequest(PermissionRole.Editor, null, "allow edit"));
        updateGrantResponse.EnsureSuccessStatusCode();

        Authorize(client, viewerTokens);
        var editorDocument = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var editAllowed = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}",
            new UpdateDocumentRequest(editorDocument!.Document.Revision, "Viewer rename allowed", null, null));

        Assert.Equal(HttpStatusCode.Forbidden, viewerDenied.StatusCode);
        Assert.NotNull(viewerAllowed);
        Assert.Equal(HttpStatusCode.Forbidden, viewerEditDenied.StatusCode);
        editAllowed.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CommenterGrant_AllowsCommentMutationButDeniesDocumentEdit()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Mission & Vision");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);
        await CreateDocumentGrantAsync(client, document.Id, viewerTokens.User.Id, PermissionRole.Commenter);

        Authorize(client, viewerTokens);
        var createComment = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "Scoped commenter note"));
        var currentDocument = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var edit = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}",
            new UpdateDocumentRequest(currentDocument!.Document.Revision, "Commenter edit denied", null, null));

        createComment.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
    }

    [Fact]
    public async Task SearchAndExport_FilterRestrictedDocumentsForUnauthorizedViewer()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var mission = FindDocument(bootstrap, "Mission & Vision");
        await SetDocumentPolicyAsync(client, mission.Id, InheritanceModes.Restricted);

        Authorize(client, viewerTokens);
        var search = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=Mission&spaceId={bootstrap.ActiveSpaceId}");
        var export = await client.GetFromJsonAsync<ExportSpaceResponse>(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/export");

        Assert.NotNull(search);
        Assert.DoesNotContain(search.Results, result => result.Id == mission.Id);
        Assert.NotNull(export);
        Assert.DoesNotContain(export.Documents, document => document.Id == mission.Id);
    }

    [Fact]
    public async Task BootstrapMapSearchAndExport_DoNotLeakExpiredAccessResource()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var mission = FindDocument(bootstrap, "Mission & Vision");
        await SetDocumentPolicyAsync(client, mission.Id, InheritanceModes.Restricted);
        await SeedResourceGrantAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(mission.Id),
            Guid.Parse(viewerTokens.User.Id),
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddMinutes(-10));

        Authorize(client, viewerTokens);
        var viewerBootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var map = await client.GetFromJsonAsync<KnowledgeMapResponse>($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/map");
        var search = await client.GetFromJsonAsync<SearchResponse>(
            $"/api/v1/search?q=Mission&spaceId={bootstrap.ActiveSpaceId}");
        var export = await client.GetFromJsonAsync<ExportSpaceResponse>(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/export");

        Assert.NotNull(viewerBootstrap);
        Assert.DoesNotContain(viewerBootstrap.Documents, document => document.Id == mission.Id);
        Assert.NotNull(map);
        Assert.DoesNotContain(map.Documents, document => document.Id == mission.Id);
        Assert.NotNull(search);
        Assert.DoesNotContain(search.Results, result => result.Id == mission.Id);
        Assert.NotNull(export);
        Assert.DoesNotContain(export.Documents, document => document.Id == mission.Id);
    }

    [Fact]
    public async Task AccessRequests_WithoutLogin_ReturnsUnauthorized()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();

        var accessRequest = await client.PostAsJsonAsync(
            "/api/v1/permissions/access-requests",
            new CreateAccessRequestRequest(ResourceTypes.Document, Guid.NewGuid().ToString(), PermissionRole.Viewer, null));
        var notifications = await client.GetAsync("/api/v1/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, accessRequest.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, notifications.StatusCode);
    }

    [Fact]
    public async Task AccessRequests_NonMemberCannotCreateRequest()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var outsiderTokens = await RegisterAsync(client, $"outsider-{Guid.NewGuid():N}@northstar.local");
        var document = FindDocument(bootstrap!, "Mission & Vision");

        Authorize(client, outsiderTokens);
        var response = await client.PostAsJsonAsync(
            "/api/v1/permissions/access-requests",
            new CreateAccessRequestRequest(ResourceTypes.Document, document.Id, PermissionRole.Viewer, "need access"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AccessRequests_ViewerCanRequestRestrictedDocumentAndDuplicateIsRejected()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Mission & Vision");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);

        Authorize(client, viewerTokens);
        var created = await client.PostAsJsonAsync(
            "/api/v1/permissions/access-requests",
            new CreateAccessRequestRequest(ResourceTypes.Document, document.Id, PermissionRole.Viewer, "need read"));
        var duplicate = await client.PostAsJsonAsync(
            "/api/v1/permissions/access-requests",
            new CreateAccessRequestRequest(ResourceTypes.Document, document.Id, PermissionRole.Viewer, "again"));
        var adminRequest = await client.PostAsJsonAsync(
            "/api/v1/permissions/access-requests",
            new CreateAccessRequestRequest(ResourceTypes.Document, document.Id, PermissionRole.Admin, "too much"));

        created.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, adminRequest.StatusCode);
    }

    [Fact]
    public async Task AccessRequests_AdminApprovesCreatesGrantAuditAndNotifications()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "admin");
        var editorTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "editor");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Mission & Vision");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);

        Authorize(client, viewerTokens);
        var request = await CreateAccessRequestAsync(client, document.Id, PermissionRole.Editor);

        Authorize(client, editorTokens);
        var editorReview = await client.PostAsJsonAsync(
            $"/api/v1/permissions/access-requests/{request.Id}/review",
            new ReviewAccessRequestRequest("approve", null, "editor cannot manage"));

        Authorize(client, adminTokens);
        var resourceRequests = await client.GetFromJsonAsync<AccessRequestsResponse>(
            $"/api/v1/permissions/resources/document/{document.Id}/access-requests");
        var expiresAt = DateTimeOffset.UtcNow.AddHours(4);
        var approved = await ReviewAccessRequestAsync(client, request.Id, "approve", null, "approved", expiresAt);
        var grant = await ReadUserGrantAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            Guid.Parse(viewerTokens.User.Id));

        Authorize(client, viewerTokens);
        var notifications = await client.GetFromJsonAsync<PermissionNotificationsResponse>(
            $"/api/v1/notifications?workspaceId={bootstrap.Workspace.Id}");

        Authorize(client, ownerTokens);
        var audit = await client.GetFromJsonAsync<PermissionAuditResponse>(
            $"/api/v1/permissions/audit?workspaceId={bootstrap.Workspace.Id}&resourceType=document&resourceId={document.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, editorReview.StatusCode);
        Assert.NotNull(resourceRequests);
        Assert.Contains(resourceRequests.Requests, item => item.Id == request.Id && item.Status == AccessRequestStatus.Pending);
        Assert.Equal(AccessRequestStatus.Approved, approved.Status);
        Assert.NotNull(approved.ResultingGrantId);
        Assert.NotNull(grant);
        Assert.Equal(PermissionRole.Editor, grant.RoleKey);
        Assert.NotNull(grant.ExpiresAt);
        Assert.True(grant.ExpiresAt.Value > DateTimeOffset.UtcNow);
        Assert.NotNull(notifications);
        Assert.Contains(notifications.Notifications, item => item.Type == PermissionNotificationTypes.AccessRequestApproved);
        Assert.NotNull(audit);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.AccessRequestCreated);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.AccessRequestApproved);
        Assert.Contains(audit.Events, item => item.Action == PermissionAuditActions.GrantCreated);
    }

    [Fact]
    public async Task AccessRequests_ApproverCannotApproveAboveOwnRoleAndDenyCreatesNoGrant()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var adminTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "admin");
        var document = FindDocument(bootstrap, "Our Principles");

        Authorize(client, adminTokens);
        var request = await CreateAccessRequestAsync(client, document.Id, PermissionRole.Owner);
        var adminApproveOwner = await client.PostAsJsonAsync(
            $"/api/v1/permissions/access-requests/{request.Id}/review",
            new ReviewAccessRequestRequest("approve", null, "self approval"));

        Authorize(client, ownerTokens);
        var denied = await ReviewAccessRequestAsync(client, request.Id, "deny", null, "not needed");
        var grant = await ReadUserGrantAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.Id),
            ResourceTypes.Document,
            Guid.Parse(document.Id),
            Guid.Parse(adminTokens.User.Id));

        Authorize(client, adminTokens);
        var notifications = await client.GetFromJsonAsync<PermissionNotificationsResponse>(
            $"/api/v1/notifications?workspaceId={bootstrap.Workspace.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, adminApproveOwner.StatusCode);
        Assert.Equal(AccessRequestStatus.Denied, denied.Status);
        Assert.Null(grant);
        Assert.NotNull(notifications);
        Assert.Contains(notifications.Notifications, item => item.Type == PermissionNotificationTypes.AccessRequestDenied);
    }

    [Fact]
    public async Task AccessRequests_CancelOnlyRequesterOrManager()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var otherViewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Operating System");
        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);

        Authorize(client, viewerTokens);
        var request = await CreateAccessRequestAsync(client, document.Id, PermissionRole.Viewer);

        Authorize(client, otherViewerTokens);
        var otherCancel = await client.PostAsJsonAsync(
            $"/api/v1/permissions/access-requests/{request.Id}/cancel",
            new CancelAccessRequestRequest("not mine"));

        Authorize(client, viewerTokens);
        var cancelled = await client.PostAsJsonAsync(
            $"/api/v1/permissions/access-requests/{request.Id}/cancel",
            new CancelAccessRequestRequest("withdrawn"));
        var cancelledRequest = await cancelled.Content.ReadFromJsonAsync<AccessRequestDto>();

        Assert.Equal(HttpStatusCode.Forbidden, otherCancel.StatusCode);
        cancelled.EnsureSuccessStatusCode();
        Assert.NotNull(cancelledRequest);
        Assert.Equal(AccessRequestStatus.Cancelled, cancelledRequest.Status);
    }

    [Fact]
    public async Task Notifications_MarkReadWorksOnlyForRecipient()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var otherViewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Our Principles");

        await CreateDocumentGrantAsync(client, document.Id, viewerTokens.User.Id, PermissionRole.Viewer);

        Authorize(client, viewerTokens);
        var notifications = await client.GetFromJsonAsync<PermissionNotificationsResponse>(
            $"/api/v1/notifications?workspaceId={bootstrap.Workspace.Id}");
        var summary = await client.GetFromJsonAsync<AccessSharingSummaryResponse>(
            $"/api/v1/notifications/summary?workspaceId={bootstrap.Workspace.Id}");
        var notification = Assert.Single(
            notifications!.Notifications,
            item => item.Type == PermissionNotificationTypes.GrantCreated);

        Authorize(client, otherViewerTokens);
        var otherRead = await client.PatchAsJsonAsync(
            $"/api/v1/notifications/{notification.Id}/read",
            new MarkNotificationReadRequest());

        Authorize(client, viewerTokens);
        var read = await client.PatchAsJsonAsync(
            $"/api/v1/notifications/{notification.Id}/read",
            new MarkNotificationReadRequest());
        var readNotification = await read.Content.ReadFromJsonAsync<PermissionNotificationDto>();

        Assert.Equal(HttpStatusCode.Forbidden, otherRead.StatusCode);
        read.EnsureSuccessStatusCode();
        Assert.NotNull(readNotification);
        Assert.NotNull(readNotification.ReadAt);
        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalCount);
        Assert.Equal(1, summary.UnreadCount);
        Assert.Equal(0, summary.PendingReviewCount);
        Assert.Equal(1, summary.GrantCount);
        Assert.Equal(0, summary.SharingCount);
        Assert.Equal(0, summary.ExpiryCount);
        Assert.Equal(0, summary.FailedInviteCount);
        Assert.Equal("grant", notification.Category);
        Assert.Equal("informational", notification.State);
        Assert.NotNull(notification.Actor);
        Assert.Equal(ownerTokens.User.DisplayName, notification.Actor.DisplayName);
        Assert.Equal(ownerTokens.User.Email, notification.Actor.Email);
        Assert.NotNull(notification.Resource);
        Assert.Equal(ResourceTypes.Document, notification.Resource.ResourceType);
        Assert.Equal(document.Id, notification.Resource.ResourceId);
        Assert.Equal(document.Title, notification.Resource.Title);
        Assert.NotNull(notification.Action);
        Assert.Equal("open_permissions", notification.Action.Kind);
        Assert.Equal("Open", notification.Action.Label);
        Assert.Equal(ResourceTypes.Document, notification.Action.ResourceType);
        Assert.Equal(document.Id, notification.Action.ResourceId);
        Assert.Equal("permission_grant", notification.Action.SubjectType);
        Assert.Equal(notification.PermissionGrantId, notification.Action.SubjectId);
    }

    [Fact]
    public async Task Notifications_ReadAllRejectsInvalidWorkspaceId()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);

        var response = await client.PatchAsJsonAsync(
            "/api/v1/notifications/read-all",
            new MarkAllNotificationsReadRequest("not-a-uuid"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.ValidationError, error.Error.Code);
    }

    [Fact]
    public async Task NotificationPreferences_RequireAuthenticationAndWorkspaceMembership()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");

        client.DefaultRequestHeaders.Authorization = null;
        var unauthenticated = await client.GetAsync(
            $"/api/v1/notifications/preferences?workspaceId={bootstrap!.Workspace.Id}");

        var outsiderTokens = await RegisterAsync(client, $"preference-outsider-{Guid.NewGuid():N}@northstar.local");
        Authorize(client, outsiderTokens);
        var nonMember = await client.GetAsync(
            $"/api/v1/notifications/preferences?workspaceId={bootstrap.Workspace.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, nonMember.StatusCode);
        Authorize(client, ownerTokens);
    }

    [Fact]
    public async Task NotificationPreferences_WorkspacePreferenceUpsertsAndLists()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");

        var watched = await client.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdatePermissionNotificationPreferenceRequest(
                bootstrap!.Workspace.Id,
                null,
                null,
                Watched: true,
                Muted: false));
        var muted = await client.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdatePermissionNotificationPreferenceRequest(
                bootstrap.Workspace.Id,
                null,
                null,
                Watched: false,
                Muted: true));
        var preferences = await client.GetFromJsonAsync<PermissionNotificationPreferencesResponse>(
            $"/api/v1/notifications/preferences?workspaceId={bootstrap.Workspace.Id}");

        watched.EnsureSuccessStatusCode();
        muted.EnsureSuccessStatusCode();
        Assert.NotNull(preferences);
        var preference = Assert.Single(preferences.Preferences);
        Assert.Equal(bootstrap.Workspace.Id, preference.WorkspaceId);
        Assert.Null(preference.ResourceType);
        Assert.Null(preference.ResourceId);
        Assert.Null(preference.Resource);
        Assert.False(preference.Watched);
        Assert.True(preference.Muted);
    }

    [Fact]
    public async Task NotificationPreferences_ResourcePreferenceRequiresViewAccess()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var document = FindDocument(bootstrap, "Mission & Vision");

        Authorize(client, ownerTokens);
        await SetResourcePolicyAsync(
            client,
            ResourceTypes.Document,
            document.Id,
            InheritanceModes.Restricted,
            LinkModes.Disabled);

        Authorize(client, viewerTokens);
        var forbidden = await client.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdatePermissionNotificationPreferenceRequest(
                bootstrap.Workspace.Id,
                ResourceTypes.Document,
                document.Id,
                Watched: true,
                Muted: false));

        Authorize(client, ownerTokens);
        await CreateDocumentGrantAsync(client, document.Id, viewerTokens.User.Id, PermissionRole.Viewer);

        Authorize(client, viewerTokens);
        var allowed = await client.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdatePermissionNotificationPreferenceRequest(
                bootstrap.Workspace.Id,
                ResourceTypes.Document,
                document.Id,
                Watched: true,
                Muted: false));
        var preference = await allowed.Content.ReadFromJsonAsync<PermissionNotificationPreferenceDto>();

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        allowed.EnsureSuccessStatusCode();
        Assert.NotNull(preference);
        Assert.Equal(ResourceTypes.Document, preference.ResourceType);
        Assert.Equal(document.Id, preference.ResourceId);
        Assert.NotNull(preference.Resource);
        Assert.Equal(ResourceTypes.Document, preference.Resource.ResourceType);
        Assert.Equal(document.Id, preference.Resource.ResourceId);
        Assert.Equal(document.Title, preference.Resource.Title);
        Assert.Contains(document.Title, preference.Resource.Path);
        Assert.True(preference.Watched);
        Assert.False(preference.Muted);

        var preferences = await client.GetFromJsonAsync<PermissionNotificationPreferencesResponse>(
            $"/api/v1/notifications/preferences?workspaceId={bootstrap.Workspace.Id}");
        var listed = Assert.Single(preferences!.Preferences);
        Assert.NotNull(listed.Resource);
        Assert.Equal(document.Title, listed.Resource.Title);
    }

    [Fact]
    public async Task NotificationPreferences_RejectWatchedAndMutedTogether()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");

        var response = await client.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdatePermissionNotificationPreferenceRequest(
                bootstrap!.Workspace.Id,
                null,
                null,
                Watched: true,
                Muted: true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.ValidationError, error.Error.Code);
    }

    [Fact]
    public async Task PermissionExpiryNotifications_AreIdempotentForGrantsAndGroupMembers()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var workspaceId = Guid.Parse(bootstrap.Workspace.Id);
        var viewerId = Guid.Parse(viewerTokens.User.Id);
        var principles = FindDocument(bootstrap, "Our Principles");
        var operating = FindDocument(bootstrap, "Operating System");
        await CreateDocumentGrantAsync(
            client,
            principles.Id,
            viewerTokens.User.Id,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(2));
        await SeedResourceGrantAsync(
            factory,
            workspaceId,
            ResourceTypes.Document,
            Guid.Parse(operating.Id),
            viewerId,
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(-2));
        var expiringGroup = await CreateWorkspaceGroupAsync(client, bootstrap.Workspace.Id, "Expiring Group");
        await AddWorkspaceGroupMemberAsync(
            client,
            bootstrap.Workspace.Id,
            expiringGroup.Id,
            viewerTokens.User.Id,
            DateTimeOffset.UtcNow.AddHours(2));
        await SeedWorkspaceGroupMemberAsync(
            factory,
            workspaceId,
            viewerId,
            DateTimeOffset.UtcNow.AddHours(-2));

        await RunPermissionExpiryProcessorAsync(factory);
        await RunPermissionExpiryProcessorAsync(factory);

        Assert.Equal(1, await CountNotificationsAsync(factory, viewerId, PermissionNotificationTypes.GrantExpiring));
        Assert.Equal(1, await CountNotificationsAsync(factory, viewerId, PermissionNotificationTypes.GrantExpired));
        Assert.Equal(1, await CountNotificationsAsync(factory, viewerId, PermissionNotificationTypes.GroupMemberExpiring));
        Assert.Equal(1, await CountNotificationsAsync(factory, viewerId, PermissionNotificationTypes.GroupMemberExpired));
        Assert.Equal(0, await CountAuditEventsAsync(factory, PermissionAuditActions.GrantRevoked));
    }

    [Fact]
    public async Task ResourceAccessMigration_DefinesPoliciesAndGrantsWithoutWorkspaceCommenter()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260429154618_AddResourceAccessPoliciesPhase2.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);
        var workspaceMemberConfigurationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Configurations",
            "WorkspaceMemberConfiguration.cs");
        var workspaceMemberConfiguration = await System.IO.File.ReadAllTextAsync(workspaceMemberConfigurationPath);

        Assert.Contains("resource_access_policies", migration, StringComparison.Ordinal);
        Assert.Contains("resource_access_grants", migration, StringComparison.Ordinal);
        Assert.Contains("resource_access_grants_role_key_check", migration, StringComparison.Ordinal);
        Assert.Contains("'owner', 'admin', 'editor', 'commenter', 'viewer'", migration, StringComparison.Ordinal);
        Assert.Contains("idx_grants_workspace_resource", migration, StringComparison.Ordinal);
        Assert.Contains("idx_grants_subject", migration, StringComparison.Ordinal);
        Assert.Contains("idx_grants_expiry", migration, StringComparison.Ordinal);
        Assert.Contains("idx_policies_resource", migration, StringComparison.Ordinal);
        Assert.Contains("role IN ('owner', 'admin', 'editor', 'viewer')", workspaceMemberConfiguration, StringComparison.Ordinal);
        Assert.DoesNotContain("role IN ('owner', 'admin', 'editor', 'commenter', 'viewer')", workspaceMemberConfiguration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchStrategyMigration_DefinesPostgresFullTextAndTrigramIndexes()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260511090000_AddSearchFullTextAndTrigramStrategyV1.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("CREATE EXTENSION IF NOT EXISTS pg_trgm", migration, StringComparison.Ordinal);
        Assert.Contains("search_vector tsvector", migration, StringComparison.Ordinal);
        Assert.Contains("document_search_vector_idx", migration, StringComparison.Ordinal);
        Assert.Contains("document_search_title_trgm_idx", migration, StringComparison.Ordinal);
        Assert.Contains("document_search_text_trgm_idx", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PermissionAuditMigration_DefinesAuditTableAndIndexes()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260429163812_AddPermissionAuditEventsPhase3.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("permission_audit_events", migration, StringComparison.Ordinal);
        Assert.Contains("before_json", migration, StringComparison.Ordinal);
        Assert.Contains("after_json", migration, StringComparison.Ordinal);
        Assert.Contains("metadata", migration, StringComparison.Ordinal);
        Assert.Contains("idx_permission_audit_workspace_created", migration, StringComparison.Ordinal);
        Assert.Contains("idx_permission_audit_resource_created", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("resource_access_grants_role_key_check", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WorkspaceGroupsMigration_DefinesGroupsGroupSubjectsAndKeepsWorkspaceCommenterDisabled()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260430002845_AddWorkspaceGroupsPhase4.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);
        var workspaceMemberConfigurationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Configurations",
            "WorkspaceMemberConfiguration.cs");
        var workspaceMemberConfiguration = await System.IO.File.ReadAllTextAsync(workspaceMemberConfigurationPath);

        Assert.Contains("workspace_groups", migration, StringComparison.Ordinal);
        Assert.Contains("workspace_group_members", migration, StringComparison.Ordinal);
        Assert.Contains("subject_type IN ('user', 'group')", migration, StringComparison.Ordinal);
        Assert.Contains("filter: \"revoked_at IS NULL\"", migration, StringComparison.Ordinal);
        Assert.Contains("workspace_groups_workspace_name_active_key", migration, StringComparison.Ordinal);
        Assert.Contains("workspace_group_members_group_user_active_key", migration, StringComparison.Ordinal);
        Assert.Contains("idx_grants_workspace_resource_subject_type", migration, StringComparison.Ordinal);
        Assert.Contains("DropForeignKey", migration, StringComparison.Ordinal);
        Assert.Contains("role IN ('owner', 'admin', 'editor', 'viewer')", workspaceMemberConfiguration, StringComparison.Ordinal);
        Assert.DoesNotContain("role IN ('owner', 'admin', 'editor', 'commenter', 'viewer')", workspaceMemberConfiguration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessRequestsAndNotificationsMigration_DefinesTablesConstraintsAndIndexes()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);
        var workspaceMemberConfigurationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Configurations",
            "WorkspaceMemberConfiguration.cs");
        var workspaceMemberConfiguration = await System.IO.File.ReadAllTextAsync(workspaceMemberConfigurationPath);

        Assert.Contains("access_requests", migration, StringComparison.Ordinal);
        Assert.Contains("permission_notifications", migration, StringComparison.Ordinal);
        Assert.Contains("access_requests_pending_subject_key", migration, StringComparison.Ordinal);
        Assert.Contains("filter: \"status = 'pending'\"", migration, StringComparison.Ordinal);
        Assert.Contains("access_requests_status_check", migration, StringComparison.Ordinal);
        Assert.Contains("permission_notifications_type_check", migration, StringComparison.Ordinal);
        Assert.Contains("idx_access_requests_workspace_status_created", migration, StringComparison.Ordinal);
        Assert.Contains("idx_access_requests_resource_status", migration, StringComparison.Ordinal);
        Assert.Contains("idx_access_requests_requester_status", migration, StringComparison.Ordinal);
        Assert.Contains("idx_permission_notifications_recipient_read_created", migration, StringComparison.Ordinal);
        Assert.Contains("idx_permission_notifications_workspace_created", migration, StringComparison.Ordinal);
        Assert.Contains("idx_permission_notifications_access_request", migration, StringComparison.Ordinal);
        Assert.Contains("role IN ('owner', 'admin', 'editor', 'viewer')", workspaceMemberConfiguration, StringComparison.Ordinal);
        Assert.DoesNotContain("role IN ('owner', 'admin', 'editor', 'commenter', 'viewer')", workspaceMemberConfiguration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PermissionExpiryNotificationsMigration_DefinesTypesDedupeKeyAndIndex()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260430014036_AddPermissionExpiryNotificationsPhase6.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("dedupe_key", migration, StringComparison.Ordinal);
        Assert.Contains("permission_notifications_dedupe_key", migration, StringComparison.Ordinal);
        Assert.Contains("permission.grant_expiring", migration, StringComparison.Ordinal);
        Assert.Contains("permission.grant_expired", migration, StringComparison.Ordinal);
        Assert.Contains("group.member_expiring", migration, StringComparison.Ordinal);
        Assert.Contains("group.member_expired", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShareLinksMigration_DefinesTableConstraintsAndIndexes()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260430052705_AddInternalShareLinksPhase7.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("share_links", migration, StringComparison.Ordinal);
        Assert.Contains("token_hash", migration, StringComparison.Ordinal);
        Assert.Contains("resource_type IN ('collection', 'document')", migration, StringComparison.Ordinal);
        Assert.Contains("role_key IN ('viewer', 'commenter')", migration, StringComparison.Ordinal);
        Assert.Contains("audience IN ('workspace')", migration, StringComparison.Ordinal);
        Assert.Contains("idx_share_links_resource", migration, StringComparison.Ordinal);
        Assert.Contains("idx_share_links_token_hash", migration, StringComparison.Ordinal);
        Assert.Contains("idx_share_links_expiry", migration, StringComparison.Ordinal);
        Assert.Contains("revoked_at IS NULL", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IamSyncMigration_DefinesExternalFieldsAndFilteredIndexes()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260430063226_AddIamSyncPhase8.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("external_subject_id", migration, StringComparison.Ordinal);
        Assert.Contains("external_synced_at", migration, StringComparison.Ordinal);
        Assert.Contains("users_external_provider_subject_key", migration, StringComparison.Ordinal);
        Assert.Contains("workspace_groups_workspace_external_key", migration, StringComparison.Ordinal);
        Assert.Contains("external_provider IS NOT NULL AND external_subject_id IS NOT NULL", migration, StringComparison.Ordinal);
        Assert.Contains("external_provider IS NOT NULL AND external_group_id IS NOT NULL", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Phase9Migration_DefinesExternalShareLinksAndEmailInvites()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260430070347_AddExternalShareLinksAndEmailInvitesPhase9.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("subject_email", migration, StringComparison.Ordinal);
        Assert.Contains("audience IN ('workspace', 'external', 'public')", migration, StringComparison.Ordinal);
        Assert.Contains("link_mode IN ('disabled', 'internal', 'external', 'public')", migration, StringComparison.Ordinal);
        Assert.Contains("resource_email_invites", migration, StringComparison.Ordinal);
        Assert.Contains("token_hash", migration, StringComparison.Ordinal);
        Assert.Contains("role_key IN ('viewer', 'commenter')", migration, StringComparison.Ordinal);
        Assert.Contains("status IN ('pending', 'accepted', 'revoked', 'expired')", migration, StringComparison.Ordinal);
        Assert.Contains("idx_resource_email_invites_token_hash", migration, StringComparison.Ordinal);
        Assert.Contains("resource_email_invites_pending_resource_email_key", migration, StringComparison.Ordinal);
        Assert.Contains("filter: \"status = 'pending'\"", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Phase10Migration_DefinesPublicShareGateAndInviteDeliveryFields()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260430162513_AddPublicShareLinksAndInviteDeliveryPhase10.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("delivery_status", migration, StringComparison.Ordinal);
        Assert.Contains("delivery_provider", migration, StringComparison.Ordinal);
        Assert.Contains("delivery_attempted_at", migration, StringComparison.Ordinal);
        Assert.Contains("delivery_error_code", migration, StringComparison.Ordinal);
        Assert.Contains("delivery_status IN ('disabled', 'sent', 'failed')", migration, StringComparison.Ordinal);
        Assert.Contains("idx_resource_email_invites_delivery_status_created", migration, StringComparison.Ordinal);
        Assert.Contains("share_links_public_viewer_expiry_check", migration, StringComparison.Ordinal);
        Assert.Contains("resource_type = 'document' AND role_key = 'viewer' AND subject_email IS NULL AND expires_at IS NOT NULL", migration, StringComparison.Ordinal);
        Assert.Contains("idx_share_links_public_document_active", migration, StringComparison.Ordinal);
        Assert.Contains("audience = 'public' AND revoked_at IS NULL", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmailInviteDeliveryOutboxMigration_DefinesSecretSafeRetryTable()
    {
        var migrationsDirectory = Path.GetDirectoryName(FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260503015211_AddEmailInviteDeliveryOutboxPhase15.cs"));
        Assert.NotNull(migrationsDirectory);
        var migrationPath = Assert.Single(
            Directory.GetFiles(migrationsDirectory, "*AddEmailInviteDeliveryOutboxPhase15.cs")
                .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal)));
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("email_invite_delivery_outbox", migration, StringComparison.Ordinal);
        Assert.Contains("workspace_id", migration, StringComparison.Ordinal);
        Assert.Contains("invite_id", migration, StringComparison.Ordinal);
        Assert.Contains("recipient_email", migration, StringComparison.Ordinal);
        Assert.Contains("provider", migration, StringComparison.Ordinal);
        Assert.Contains("attempt_count", migration, StringComparison.Ordinal);
        Assert.Contains("max_attempts", migration, StringComparison.Ordinal);
        Assert.Contains("next_attempt_at", migration, StringComparison.Ordinal);
        Assert.Contains("last_attempt_at", migration, StringComparison.Ordinal);
        Assert.Contains("sent_at", migration, StringComparison.Ordinal);
        Assert.Contains("failed_at", migration, StringComparison.Ordinal);
        Assert.Contains("last_error_code", migration, StringComparison.Ordinal);
        Assert.Contains("last_error_message", migration, StringComparison.Ordinal);
        Assert.Contains("idx_email_invite_delivery_outbox_due", migration, StringComparison.Ordinal);
        Assert.Contains("idx_email_invite_delivery_outbox_invite", migration, StringComparison.Ordinal);
        Assert.Contains("email_invite_delivery_outbox_status_check", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("raw_token", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token_hash", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accept_url", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smtp", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider_secret", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Phase11Migration_DefinesPublicCollectionAndPasswordFields()
    {
        var migrationsDirectory = Path.GetDirectoryName(FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260430162513_AddPublicShareLinksAndInviteDeliveryPhase10.cs"));
        Assert.NotNull(migrationsDirectory);
        var migrationPath = Assert.Single(
            Directory.GetFiles(migrationsDirectory, "*AddPublicCollectionLinksAndLinkPasswordsPhase11.cs")
                .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal)));
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("password_hash", migration, StringComparison.Ordinal);
        Assert.Contains("share_links_password_public_check", migration, StringComparison.Ordinal);
        Assert.Contains("password_hash IS NULL OR audience = 'public'", migration, StringComparison.Ordinal);
        Assert.Contains("resource_type IN ('document', 'collection') AND role_key = 'viewer' AND subject_email IS NULL AND expires_at IS NOT NULL", migration, StringComparison.Ordinal);
        Assert.Contains("idx_share_links_public_active", migration, StringComparison.Ordinal);
        Assert.Contains("\"workspace_id\", \"resource_type\", \"resource_id\", \"expires_at\"", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotificationPreferencesMigration_DefinesTableConstraintsAndIndexes()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260502063653_AddPermissionNotificationPreferencesPhase12.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("permission_notification_preferences", migration, StringComparison.Ordinal);
        Assert.Contains("workspace_id", migration, StringComparison.Ordinal);
        Assert.Contains("user_id", migration, StringComparison.Ordinal);
        Assert.Contains("resource_type", migration, StringComparison.Ordinal);
        Assert.Contains("resource_id", migration, StringComparison.Ordinal);
        Assert.Contains("watched", migration, StringComparison.Ordinal);
        Assert.Contains("muted", migration, StringComparison.Ordinal);
        Assert.Contains("permission_notification_preferences_scope_check", migration, StringComparison.Ordinal);
        Assert.Contains("permission_notification_preferences_resource_type_check", migration, StringComparison.Ordinal);
        Assert.Contains("permission_notification_preferences_watch_mute_check", migration, StringComparison.Ordinal);
        Assert.Contains("permission_notification_preferences_workspace_user_key", migration, StringComparison.Ordinal);
        Assert.Contains("permission_notification_preferences_resource_user_key", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PermissionNotificationFanoutMigration_AddsShareLinkAndInviteTypes()
    {
        var migrationPath = FindRepositoryFile(
            "src",
            "Northstar.Infrastructure",
            "Persistence",
            "Migrations",
            "20260502092712_AddPermissionNotificationFanoutTypesPhase13.cs");
        var migration = await System.IO.File.ReadAllTextAsync(migrationPath);

        Assert.Contains("permission_notifications_type_check", migration, StringComparison.Ordinal);
        Assert.Contains(PermissionNotificationTypes.ShareLinkCreated, migration, StringComparison.Ordinal);
        Assert.Contains(PermissionNotificationTypes.ShareLinkRevoked, migration, StringComparison.Ordinal);
        Assert.Contains(PermissionNotificationTypes.EmailInviteCreated, migration, StringComparison.Ordinal);
        Assert.Contains(PermissionNotificationTypes.EmailInviteAccepted, migration, StringComparison.Ordinal);
        Assert.Contains(PermissionNotificationTypes.EmailInviteRevoked, migration, StringComparison.Ordinal);
        Assert.Contains(PermissionNotificationTypes.EmailInviteDeliveryFailed, migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ViewerCanExport_DefaultExportIncludesArchivedAndExcludesDeleted()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var archivedDocument = FindDocument(bootstrap!, "Mission & Vision");
        var deletedDocument = FindDocument(bootstrap!, "Operating System");
        await client.PatchAsync($"/api/v1/documents/{archivedDocument.Id}/archive", null);
        await client.DeleteAsync($"/api/v1/documents/{deletedDocument.Id}");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, viewerTokens);

        var response = await client.GetAsync($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/export");
        var raw = await response.Content.ReadAsStringAsync();
        var export = JsonSerializer.Deserialize<ExportSpaceResponse>(
            raw,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        response.EnsureSuccessStatusCode();
        Assert.NotNull(export);
        Assert.NotEmpty(export.Collections);
        Assert.Contains(export.Documents, document => document.Id == archivedDocument.Id && document.Status == "archived");
        Assert.DoesNotContain(export.Documents, document => document.Id == deletedDocument.Id);
        Assert.Contains(export.Documents, document => document.Tags.Count > 0 && document.Content.ValueKind == JsonValueKind.Object);
        Assert.False(raw.Contains("refreshToken", StringComparison.OrdinalIgnoreCase));
        Assert.False(raw.Contains("passwordHash", StringComparison.OrdinalIgnoreCase));
        Assert.False(raw.Contains("user_credentials", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EditorCanAppendImport_AndInternalLinksAreMapped()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var editorTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "editor");
        Authorize(client, editorTokens);
        var collectionId = Guid.NewGuid().ToString();
        var oldSourceId = Guid.NewGuid();
        var oldTargetId = Guid.NewGuid();
        var marker = $"phase-five-import-{Guid.NewGuid():N}";
        var sourceTitle = $"Imported Source {Guid.NewGuid():N}";
        var targetTitle = $"Imported Target {Guid.NewGuid():N}";
        var request = new ImportSpaceRequest(
            "append",
            [new CollectionImportDto(collectionId, "Phase 5 Imported", 90m)],
            [
                new DocumentImportDto(
                    oldSourceId.ToString(),
                    collectionId,
                    sourceTitle,
                    "draft",
                    1m,
                    ["imported", "phase-5"],
                    Json($$"""
                    {
                      "type": "doc",
                      "content": [
                        {
                          "type": "paragraph",
                          "content": [
                            {
                              "type": "text",
                              "text": "{{marker}} links to "
                            },
                            {
                              "type": "text",
                              "text": "target",
                              "marks": [
                                {
                                  "type": "link",
                                  "attrs": {
                                    "href": "/documents/{{oldTargetId}}"
                                  }
                                }
                              ]
                            }
                          ]
                        }
                      ]
                    }
                    """)),
                new DocumentImportDto(
                    oldTargetId.ToString(),
                    collectionId,
                    targetTitle,
                    "draft",
                    2m,
                    ["imported-target"],
                    Json("""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Target body"}]}]}"""))
            ]);

        var response = await client.PostAsJsonAsync($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/import", request);

        response.EnsureSuccessStatusCode();
        var import = await response.Content.ReadFromJsonAsync<ImportSpaceResponse>();
        Assert.NotNull(import);
        Assert.Equal(1, import.ImportedCollectionCount);
        Assert.Equal(2, import.ImportedDocumentCount);
        var source = import.Map.Documents.Single(document => document.Title == sourceTitle);
        var target = import.Map.Documents.Single(document => document.Title == targetTitle);
        Assert.Contains("phase-5", source.Tags);

        var sourceDocument = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{source.Id}");
        var sourceContext = await client.GetFromJsonAsync<DocumentContextResponse>($"/api/v1/documents/{source.Id}/context");
        var targetContext = await client.GetFromJsonAsync<DocumentContextResponse>($"/api/v1/documents/{target.Id}/context");
        var sourceActivity = await client.GetFromJsonAsync<DocumentActivityResponse>($"/api/v1/documents/{source.Id}/activity");
        var search = await client.GetFromJsonAsync<SearchResponse>($"/api/v1/search?q={marker}&spaceId={bootstrap.ActiveSpaceId}");

        Assert.NotNull(sourceDocument);
        Assert.Equal(JsonValueKind.Object, sourceDocument.Document.Content.ValueKind);
        Assert.NotNull(sourceContext);
        Assert.Contains(sourceContext.VersionTrail, version => version.Version == "1.0");
        Assert.NotNull(targetContext);
        Assert.Contains(targetContext.Backlinks, backlink => backlink.Id == source.Id);
        Assert.NotNull(sourceActivity);
        Assert.Contains(sourceActivity.Items, item => item.Title == ActivityActions.DocumentImported);
        Assert.NotNull(search);
        Assert.Contains(search.Results, result => result.Id == source.Id);
    }

    [Fact]
    public async Task ViewerCannotImport()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, viewerTokens);
        var request = new ImportSpaceRequest(
            "append",
            null,
            [
                new DocumentImportDto(
                    Guid.NewGuid().ToString(),
                    bootstrap.Folders[0].Id,
                    "Viewer Import",
                    "draft",
                    null,
                    [],
                    Json("""{"type":"doc","content":[]}"""))
            ]);

        var response = await client.PostAsJsonAsync($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/import", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Import_WithNonObjectContent_ReturnsValidationErrorAndDoesNotWrite()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var beforeMap = await client.GetFromJsonAsync<KnowledgeMapResponse>($"/api/v1/spaces/{bootstrap!.ActiveSpaceId}/map");
        var badTitle = $"Bad Import {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/import",
            new
            {
                mode = "append",
                documents = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        folderId = bootstrap.Folders[0].Id,
                        title = badTitle,
                        content = "plain-string-content"
                    }
                }
            });
        var afterMap = await client.GetFromJsonAsync<KnowledgeMapResponse>($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/map");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.ValidationError, error.Error.Code);
        Assert.NotNull(beforeMap);
        Assert.NotNull(afterMap);
        Assert.Equal(beforeMap.Documents.Count, afterMap.Documents.Count);
        Assert.DoesNotContain(afterMap.Documents, document => document.Title == badTitle);
    }

    [Fact]
    public async Task UploadSession_EditorCanCreate_AndViewerCannotCreate()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var session = await CreateUploadSessionAsync(client, document.Id, Encoding.UTF8.GetBytes("owner upload"));
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        Authorize(client, viewerTokens);
        var viewerResponse = await client.PostAsJsonAsync(
            "/api/v1/files/uploads/sessions",
            CreateUploadSessionRequest(document.Id, Encoding.UTF8.GetBytes("viewer upload")));

        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
        Assert.Equal("single", session.UploadMode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerResponse.StatusCode);
    }

    [Fact]
    public async Task UploadSession_S3CompatibleProviderReturnsPresignedPutTarget()
    {
        using var factory = new NorthstarApiFactory(new Dictionary<string, string?>
        {
            ["Files:StorageProvider"] = "S3",
            ["Files:DefaultBucket"] = "northstar-test",
            ["Files:S3:Endpoint"] = "https://storage.example.test",
            ["Files:S3:AccessKey"] = "access-key",
            ["Files:S3:SecretKey"] = "secret-key",
            ["Files:S3:ForcePathStyle"] = "true",
            ["Files:S3:PresignedUploadMinutes"] = "10"
        });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");

        var response = await client.PostAsJsonAsync(
            "/api/v1/files/uploads/sessions",
            CreateUploadSessionRequest(document.Id, Encoding.UTF8.GetBytes("s3 upload target")));
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<CreateUploadSessionResponse>();

        Assert.NotNull(session);
        Assert.Equal("single", session.UploadMode);
        Assert.Equal("s3-compatible", session.UploadTarget.Type);
        Assert.Equal("PUT", session.UploadTarget.Method);
        Assert.StartsWith("https://storage.example.test/", session.UploadTarget.Url, StringComparison.Ordinal);
        Assert.Contains("X-Amz-Signature", session.UploadTarget.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("text/plain", session.UploadTarget.Headers["Content-Type"]);
    }

    [Fact]
    public async Task S3CompatibleStorageAcceptance_UploadReadDeleteThroughApi()
    {
        var endpoint = Environment.GetEnvironmentVariable("NORTHSTAR_S3_ACCEPTANCE_ENDPOINT");
        var accessKey = Environment.GetEnvironmentVariable("NORTHSTAR_S3_ACCEPTANCE_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("NORTHSTAR_S3_ACCEPTANCE_SECRET_KEY");
        var bucket = Environment.GetEnvironmentVariable("NORTHSTAR_S3_ACCEPTANCE_BUCKET");
        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(accessKey) ||
            string.IsNullOrWhiteSpace(secretKey) ||
            string.IsNullOrWhiteSpace(bucket))
        {
            return;
        }

        await EnsureS3AcceptanceBucketAsync(endpoint, accessKey, secretKey, bucket);
        using var factory = new NorthstarApiFactory(new Dictionary<string, string?>
        {
            ["Files:StorageProvider"] = "S3",
            ["Files:DefaultBucket"] = bucket,
            ["Files:S3:Endpoint"] = endpoint,
            ["Files:S3:AccessKey"] = accessKey,
            ["Files:S3:SecretKey"] = secretKey,
            ["Files:S3:ForcePathStyle"] = "true",
            ["Files:S3:UseHttp"] = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase).ToString(),
            ["Files:S3:PresignedUploadMinutes"] = "10"
        });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var bytes = Encoding.UTF8.GetBytes($"s3 acceptance {Guid.NewGuid():N}");
        var session = await CreateUploadSessionAsync(client, document.Id, bytes);

        Assert.Equal("s3-compatible", session.UploadTarget.Type);
        await UploadContentAsync(client, session.SessionId, bytes);
        await CompleteSessionAsync(client, session.SessionId);
        var finalized = await FinalizeSessionAsync(client, session.SessionId, document.Id);
        var content = await client.GetAsync($"/api/v1/files/{finalized.File.Id}/content");
        content.EnsureSuccessStatusCode();

        var deleteAttached = await client.DeleteAsync($"/api/v1/files/{finalized.File.Id}");
        var removeAttachment = await client.DeleteAsync(
            $"/api/v1/documents/{document.Id}/attachments/{finalized.Attachment!.Id}");
        removeAttachment.EnsureSuccessStatusCode();
        var deleteFile = await client.DeleteAsync($"/api/v1/files/{finalized.File.Id}");
        deleteFile.EnsureSuccessStatusCode();
        await RunFileOutboxProcessorAsync(factory);
        var deletedContent = await client.GetAsync($"/api/v1/files/{finalized.File.Id}/content");

        Assert.Equal(bytes, await content.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.Conflict, deleteAttached.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deletedContent.StatusCode);
    }

    [Fact]
    public async Task UploadSession_IsIdempotent_AndFinalizeCreatesFileOnce()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var bytes = Encoding.UTF8.GetBytes("phase six idempotent finalize");
        var idempotencyKey = $"upload-{Guid.NewGuid():N}";

        var first = await CreateUploadSessionAsync(client, document.Id, bytes, idempotencyKey);
        var second = await CreateUploadSessionAsync(client, document.Id, bytes, idempotencyKey);
        await UploadContentAsync(client, first.SessionId, bytes);
        await CompleteSessionAsync(client, first.SessionId);
        var filesBeforeFinalize = await CountFilesAsync(factory);
        var firstFinalize = await FinalizeSessionAsync(client, first.SessionId, document.Id);
        var secondFinalize = await FinalizeSessionAsync(client, first.SessionId, document.Id);
        var filesAfterFinalize = await CountFilesAsync(factory);
        var fileFinalizedEvents = await CountFileOutboxEventsAsync(
            factory,
            Guid.Parse(firstFinalize.File.Id),
            FileOutboxEventTypes.FileFinalized);

        Assert.Equal(first.SessionId, second.SessionId);
        Assert.Equal(0, filesBeforeFinalize);
        Assert.Equal(firstFinalize.File.Id, secondFinalize.File.Id);
        Assert.NotNull(firstFinalize.Attachment);
        Assert.NotNull(secondFinalize.Attachment);
        Assert.Equal(firstFinalize.Attachment.Id, secondFinalize.Attachment.Id);
        Assert.Equal(1, filesAfterFinalize);
        Assert.Equal(1, fileFinalizedEvents);
    }

    [Fact]
    public async Task UploadSession_FinalizeBeforeComplete_AndAbortThenFinalize_ReturnValidationError()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var bytes = Encoding.UTF8.GetBytes("invalid finalize");
        var session = await CreateUploadSessionAsync(client, document.Id, bytes);
        var abortedSession = await CreateUploadSessionAsync(
            client,
            document.Id,
            Encoding.UTF8.GetBytes("abort finalize"));

        var beforeComplete = await client.PostAsJsonAsync(
            $"/api/v1/files/uploads/sessions/{session.SessionId}/finalize",
            new FinalizeUploadSessionRequest(document.Id, DocumentAttachmentRelationType.Attachment, null));
        var abort = await client.PostAsync($"/api/v1/files/uploads/sessions/{abortedSession.SessionId}/abort", null);
        var afterAbort = await client.PostAsJsonAsync(
            $"/api/v1/files/uploads/sessions/{abortedSession.SessionId}/finalize",
            new FinalizeUploadSessionRequest(document.Id, DocumentAttachmentRelationType.Attachment, null));

        Assert.Equal(HttpStatusCode.BadRequest, beforeComplete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, abort.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, afterAbort.StatusCode);
        Assert.Equal(0, await CountFilesAsync(factory));
    }

    [Fact]
    public async Task UploadSession_CompleteRejectsChecksumMismatch_AndMultipartPresignIsPhase6Stub()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var bytes = Encoding.UTF8.GetBytes("checksum mismatch");
        var session = await CreateUploadSessionAsync(
            client,
            document.Id,
            bytes,
            checksum: new string('0', 64));
        await UploadContentAsync(client, session.SessionId, bytes);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/files/uploads/sessions/{session.SessionId}/complete",
            new CompleteUploadSessionRequest());
        var multipart = await client.PostAsync(
            $"/api/v1/files/uploads/sessions/{session.SessionId}/parts/presign",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, complete.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, multipart.StatusCode);
        var error = await multipart.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.ValidationError, error.Error.Code);
    }

    [Fact]
    public async Task FileAccess_ViewerCanRead_OutsiderCannotRead_AndDeleteRequiresNoActiveAttachments()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var finalized = await UploadCompleteAndFinalizeAsync(
            client,
            document.Id,
            Encoding.UTF8.GetBytes("private file content"),
            attachOnFinalize: true);
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");
        var outsiderTokens = await RegisterAsync(client, $"outsider-{Guid.NewGuid():N}@northstar.local");

        Authorize(client, viewerTokens);
        var metadata = await client.GetAsync($"/api/v1/files/{finalized.File.Id}");
        var content = await client.GetAsync($"/api/v1/files/{finalized.File.Id}/content");

        Authorize(client, outsiderTokens);
        var outsider = await client.GetAsync($"/api/v1/files/{finalized.File.Id}");

        Authorize(client, ownerTokens);
        var deleteWithAttachment = await client.DeleteAsync($"/api/v1/files/{finalized.File.Id}");
        var deleteAttachment = await client.DeleteAsync(
            $"/api/v1/documents/{document.Id}/attachments/{finalized.Attachment!.Id}");
        var deleteFile = await client.DeleteAsync($"/api/v1/files/{finalized.File.Id}");
        var metadataAfterDelete = await client.GetAsync($"/api/v1/files/{finalized.File.Id}");
        var contentAfterDelete = await client.GetAsync($"/api/v1/files/{finalized.File.Id}/content");
        var deleteOutboxCount = await CountFileOutboxEventsAsync(
            factory,
            Guid.Parse(finalized.File.Id),
            FileOutboxEventTypes.FileDeleted);

        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
        Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        await AssertFileMetadataDoesNotExposeStorageInternalsAsync(metadata);
        Assert.Equal("private file content", await content.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, outsider.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, deleteWithAttachment.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteAttachment.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteFile.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, metadataAfterDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, contentAfterDelete.StatusCode);
        Assert.Equal(1, deleteOutboxCount);
    }

    [Fact]
    public async Task FileAccess_UsesAttachedDocumentPermissionsWhenFileIsAttached()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var finalized = await UploadCompleteAndFinalizeAsync(
            client,
            document.Id,
            Encoding.UTF8.GetBytes("restricted attachment content"),
            attachOnFinalize: true);
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");

        await SetDocumentPolicyAsync(client, document.Id, InheritanceModes.Restricted);

        Authorize(client, viewerTokens);
        var restrictedMetadata = await client.GetAsync($"/api/v1/files/{finalized.File.Id}");
        var restrictedContent = await client.GetAsync($"/api/v1/files/{finalized.File.Id}/content");

        Authorize(client, ownerTokens);
        await CreateDocumentGrantAsync(client, document.Id, viewerTokens.User.Id, PermissionRole.Viewer);

        Authorize(client, viewerTokens);
        var grantedMetadata = await client.GetAsync($"/api/v1/files/{finalized.File.Id}");
        var grantedContent = await client.GetAsync($"/api/v1/files/{finalized.File.Id}/content");

        Assert.Equal(HttpStatusCode.Forbidden, restrictedMetadata.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, restrictedContent.StatusCode);
        Assert.Equal(HttpStatusCode.OK, grantedMetadata.StatusCode);
        Assert.Equal(HttpStatusCode.OK, grantedContent.StatusCode);
        Assert.Equal("restricted attachment content", await grantedContent.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DocumentAttachments_EditorCanAttachAndList_ViewerCannotWrite()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var finalized = await UploadCompleteAndFinalizeAsync(
            client,
            document.Id,
            Encoding.UTF8.GetBytes("manual attachment"),
            attachOnFinalize: false);
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");

        var attach = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/attachments",
            new AttachFileToDocumentRequest(finalized.File.Id, DocumentAttachmentRelationType.Attachment, null));
        var attachment = await attach.Content.ReadFromJsonAsync<DocumentAttachmentDto>();
        var list = await client.GetFromJsonAsync<DocumentAttachmentsResponse>(
            $"/api/v1/documents/{document.Id}/attachments");

        Authorize(client, viewerTokens);
        var viewerAttach = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/attachments",
            new AttachFileToDocumentRequest(finalized.File.Id, DocumentAttachmentRelationType.Attachment, null));
        var viewerDelete = await client.DeleteAsync(
            $"/api/v1/documents/{document.Id}/attachments/{attachment!.Id}");

        Assert.Equal(HttpStatusCode.OK, attach.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list.Attachments, item => item.FileId == finalized.File.Id);
        await AssertFileMetadataDoesNotExposeStorageInternalsAsync(attach);
        Assert.Equal(HttpStatusCode.Forbidden, viewerAttach.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerDelete.StatusCode);
    }

    [Fact]
    public async Task UpdateDocumentContent_ValidatesTiptapFileReferences_AndCreatesInlineImageAttachment()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var finalized = await UploadCompleteAndFinalizeAsync(
            client,
            document.Id,
            Encoding.UTF8.GetBytes("image bytes"),
            "image.png",
            "image/png",
            attachOnFinalize: false);
        var content = Json($$"""
        {
          "type": "doc",
          "content": [
            {
              "type": "image",
              "attrs": {
                "fileId": "{{finalized.File.Id}}",
                "src": "/api/v1/files/{{finalized.File.Id}}/content"
              }
            }
          ]
        }
        """);

        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}",
            new UpdateDocumentRequest(original!.Document.Revision, null, content, null));
        var attachments = await client.GetFromJsonAsync<DocumentAttachmentsResponse>(
            $"/api/v1/documents/{document.Id}/attachments");

        patch.EnsureSuccessStatusCode();
        Assert.NotNull(attachments);
        Assert.Contains(
            attachments.Attachments,
            attachment => attachment.FileId == finalized.File.Id &&
                attachment.RelationType == DocumentAttachmentRelationType.InlineImage);
    }

    [Fact]
    public async Task UpdateDocumentContent_RejectsMissingAndCrossWorkspaceFileReferences()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var missingFileId = Guid.NewGuid();

        var missing = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}",
            new UpdateDocumentRequest(
                original!.Document.Revision,
                null,
                Json($$"""
                {
                  "type": "doc",
                  "content": [
                    {
                      "type": "image",
                      "attrs": {
                        "fileId": "{{missingFileId}}"
                      }
                    }
                  ]
                }
                """),
                null));

        var crossWorkspaceFileId = await CreateCrossWorkspaceFileAsync(factory);
        var fresh = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var crossWorkspace = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{document.Id}",
            new UpdateDocumentRequest(
                fresh!.Document.Revision,
                null,
                Json($$"""
                {
                  "type": "doc",
                  "content": [
                    {
                      "type": "image",
                      "attrs": {
                        "fileId": "{{crossWorkspaceFileId}}"
                      }
                    }
                  ]
                }
                """),
                null));

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, crossWorkspace.StatusCode);
    }

    [Fact]
    public async Task FileOutboxProcessor_PublishesFinalizeAttachmentAndDeleteEvents()
    {
        var storage = new RecordingObjectStorage();
        using var factory = new NorthstarApiFactory(configureServices: services =>
        {
            services.RemoveAll<IObjectStorage>();
            services.AddSingleton<IObjectStorage>(storage);
        });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var finalized = await UploadCompleteAndFinalizeAsync(
            client,
            document.Id,
            Encoding.UTF8.GetBytes("outbox processor content"),
            attachOnFinalize: true);

        await RunFileOutboxProcessorAsync(factory);
        var finalizedEvent = await ReadFileOutboxEventAsync(
            factory,
            Guid.Parse(finalized.File.Id),
            FileOutboxEventTypes.FileFinalized);
        var attachmentEvent = await ReadFileOutboxEventAsync(
            factory,
            Guid.Parse(finalized.Attachment!.Id),
            FileOutboxEventTypes.DocumentAttachmentCreated);

        var deleteAttachment = await client.DeleteAsync(
            $"/api/v1/documents/{document.Id}/attachments/{finalized.Attachment.Id}");
        deleteAttachment.EnsureSuccessStatusCode();
        var deleteFile = await client.DeleteAsync($"/api/v1/files/{finalized.File.Id}");
        deleteFile.EnsureSuccessStatusCode();
        await RunFileOutboxProcessorAsync(factory);
        var deletedEvent = await ReadFileOutboxEventAsync(
            factory,
            Guid.Parse(finalized.File.Id),
            FileOutboxEventTypes.FileDeleted);

        Assert.NotNull(finalizedEvent);
        Assert.NotNull(attachmentEvent);
        Assert.NotNull(deletedEvent);
        Assert.Equal(FileOutboxEventStatus.Published, finalizedEvent.Status);
        Assert.Equal(FileOutboxEventStatus.Published, attachmentEvent.Status);
        Assert.Equal(FileOutboxEventStatus.Published, deletedEvent.Status);
        Assert.Contains(Guid.Parse(finalized.File.Id), storage.DeletedFileIds);
    }

    [Fact]
    public async Task FileOutboxProcessor_RetriesThenFailsDeleteObjectErrors()
    {
        var storage = new RecordingObjectStorage { FailDeletes = true };
        using var factory = new NorthstarApiFactory(configureServices: services =>
        {
            services.RemoveAll<IObjectStorage>();
            services.AddSingleton<IObjectStorage>(storage);
        });
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var finalized = await UploadCompleteAndFinalizeAsync(
            client,
            document.Id,
            Encoding.UTF8.GetBytes("outbox retry content"),
            attachOnFinalize: false);
        var deleteFile = await client.DeleteAsync($"/api/v1/files/{finalized.File.Id}");
        deleteFile.EnsureSuccessStatusCode();
        var now = DateTimeOffset.UtcNow;

        var first = await RunFileOutboxProcessorAsync(factory, now);
        var second = await RunFileOutboxProcessorAsync(factory, now.AddMinutes(2));
        var third = await RunFileOutboxProcessorAsync(factory, now.AddMinutes(4));
        var deletedEvent = await ReadFileOutboxEventAsync(
            factory,
            Guid.Parse(finalized.File.Id),
            FileOutboxEventTypes.FileDeleted);

        Assert.Equal(1, first.Retrying);
        Assert.Equal(1, second.Retrying);
        Assert.Equal(1, third.Failed);
        Assert.NotNull(deletedEvent);
        Assert.Equal(FileOutboxEventStatus.Failed, deletedEvent.Status);
        Assert.Equal(3, deletedEvent.RetryCount);
        Assert.False(string.IsNullOrWhiteSpace(deletedEvent.LastError));
    }

    [Fact]
    public async Task Comments_CreateListAddResolveReopen_KeepDocumentContentExternal()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var original = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var beforeState = await ReadDocumentPersistenceStateAsync(factory, document.Id);
        var anchor = CreateCommentAnchor(document.Id);

        var create = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments",
            new CreateCommentThreadRequest(anchor, "Persist this comment"));
        var createdRaw = await create.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<CommentThreadDto>(createdRaw, JsonOptions);
        var list = await client.GetFromJsonAsync<CommentThreadsResponse>(
            $"/api/v1/documents/{document.Id}/comments");
        var addMessage = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments/{created!.Id}/messages",
            new AddCommentMessageRequest("Follow-up message"));
        var withMessage = await addMessage.Content.ReadFromJsonAsync<CommentThreadDto>();
        var resolve = await client.PostAsync(
            $"/api/v1/documents/{document.Id}/comments/{created.Id}/resolve",
            null);
        var resolved = await resolve.Content.ReadFromJsonAsync<CommentThreadDto>();
        var reopen = await client.PostAsync(
            $"/api/v1/documents/{document.Id}/comments/{created.Id}/reopen",
            null);
        var reopened = await reopen.Content.ReadFromJsonAsync<CommentThreadDto>();
        var after = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{document.Id}");
        var afterState = await ReadDocumentPersistenceStateAsync(factory, document.Id);

        create.EnsureSuccessStatusCode();
        addMessage.EnsureSuccessStatusCode();
        resolve.EnsureSuccessStatusCode();
        reopen.EnsureSuccessStatusCode();
        Assert.NotNull(created);
        Assert.Equal(document.Id, created.DocumentId);
        Assert.Equal("open", created.Status);
        Assert.Equal("active", created.AnchorStatus);
        Assert.Equal("Persist this comment", Assert.Single(created.Messages).Body);
        Assert.Equal(JsonSerializer.Serialize(anchor), JsonSerializer.Serialize(created.Anchor));
        Assert.NotNull(list);
        Assert.Single(list.Threads);
        Assert.NotNull(withMessage);
        Assert.Equal(2, withMessage.Messages.Count);
        Assert.Equal(JsonSerializer.Serialize(created.Anchor), JsonSerializer.Serialize(withMessage.Anchor));
        Assert.NotNull(resolved);
        Assert.Equal("resolved", resolved.Status);
        Assert.NotNull(resolved.ResolvedAt);
        Assert.Equal(JsonSerializer.Serialize(withMessage.Anchor), JsonSerializer.Serialize(resolved.Anchor));
        Assert.Equal(withMessage.Messages.Count, resolved.Messages.Count);
        Assert.NotNull(reopened);
        Assert.Equal("open", reopened.Status);
        Assert.Null(reopened.ResolvedAt);
        Assert.Equal(JsonSerializer.Serialize(resolved.Anchor), JsonSerializer.Serialize(reopened.Anchor));
        Assert.NotNull(after);
        Assert.Equal(JsonSerializer.Serialize(original!.Document.Content), JsonSerializer.Serialize(after.Document.Content));
        Assert.Equal(beforeState, afterState);
        AssertDoesNotContainRuntimeCommentState(createdRaw);
    }

    [Fact]
    public async Task Comments_RejectEmptyBodyAndAnchorDocumentMismatch()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var document = FindDocument(bootstrap!, "Our Principles");
        var mismatchedDocumentId = Guid.NewGuid().ToString();

        var emptyBody = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments",
            new CreateCommentThreadRequest(CreateCommentAnchor(document.Id), "   "));
        var mismatch = await client.PostAsJsonAsync(
            $"/api/v1/documents/{document.Id}/comments",
            new CreateCommentThreadRequest(CreateCommentAnchor(mismatchedDocumentId), "Wrong anchor"));

        Assert.Equal(HttpStatusCode.BadRequest, emptyBody.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
    }

    [Fact]
    public async Task Comments_AreDocumentScopedAndReuseWorkspacePermissions()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        var principles = FindDocument(bootstrap!, "Our Principles");
        var mission = FindDocument(bootstrap!, "Mission & Vision");

        var createPrinciples = await client.PostAsJsonAsync(
            $"/api/v1/documents/{principles.Id}/comments",
            new CreateCommentThreadRequest(CreateCommentAnchor(principles.Id), "Principles comment"));
        var principlesThread = await createPrinciples.Content.ReadFromJsonAsync<CommentThreadDto>();
        var createMission = await client.PostAsJsonAsync(
            $"/api/v1/documents/{mission.Id}/comments",
            new CreateCommentThreadRequest(CreateCommentAnchor(mission.Id), "Mission comment"));
        var viewerTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap!.Workspace.Id, "viewer");

        var principlesList = await client.GetFromJsonAsync<CommentThreadsResponse>(
            $"/api/v1/documents/{principles.Id}/comments");
        var missionList = await client.GetFromJsonAsync<CommentThreadsResponse>(
            $"/api/v1/documents/{mission.Id}/comments");

        Authorize(client, viewerTokens);
        var viewerList = await client.GetAsync($"/api/v1/documents/{principles.Id}/comments");
        var viewerCreate = await client.PostAsJsonAsync(
            $"/api/v1/documents/{principles.Id}/comments",
            new CreateCommentThreadRequest(CreateCommentAnchor(principles.Id), "Viewer write"));
        var viewerResolve = await client.PostAsync(
            $"/api/v1/documents/{principles.Id}/comments/{principlesThread!.Id}/resolve",
            null);

        createPrinciples.EnsureSuccessStatusCode();
        createMission.EnsureSuccessStatusCode();
        Assert.NotNull(principlesList);
        Assert.NotNull(missionList);
        Assert.Single(principlesList.Threads);
        Assert.Single(missionList.Threads);
        Assert.Equal("Principles comment", Assert.Single(principlesList.Threads).Messages[0].Body);
        Assert.Equal("Mission comment", Assert.Single(missionList.Threads).Messages[0].Body);
        Assert.Equal(HttpStatusCode.OK, viewerList.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerResolve.StatusCode);
    }

    [Fact]
    public async Task LocalFileStorage_StreamsContentWithoutReadAllBytes()
    {
        var sourcePath = FindRepositoryFile("src", "Northstar.Infrastructure", "Files", "LocalFileStorage.cs");
        var source = await System.IO.File.ReadAllTextAsync(sourcePath);

        Assert.DoesNotContain("ReadAllBytes", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadToEnd", source, StringComparison.Ordinal);
        Assert.Contains("ReadAsync", source, StringComparison.Ordinal);
        Assert.Contains("WriteAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrganizationProfile_AllowsActiveWorkspaceMemberRead()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await GetBootstrapAsync(client);

        var response = await client.GetAsync($"/api/v1/organizations/{bootstrap.Workspace.OrganizationId}/profile");

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<OrganizationProfileResponse>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(bootstrap.Workspace.OrganizationId, profile.Organization.Id);
        Assert.Equal("Northstar", profile.Organization.Name);
        var workspace = Assert.Single(profile.Organization.Workspaces);
        Assert.Equal(bootstrap.Workspace.Id, workspace.Id);
        Assert.Equal(WorkspaceMemberRole.Owner, workspace.CurrentUserRole);
    }

    [Fact]
    public async Task OrganizationProfile_ForbidsNonMemberAndDoesNotLeakUnknownOrganization()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await GetBootstrapAsync(client);
        var outsiderTokens = await RegisterAsync(client, $"outsider-{Guid.NewGuid():N}@northstar.local");

        Authorize(client, outsiderTokens);
        var forbidden = await client.GetAsync($"/api/v1/organizations/{bootstrap.Workspace.OrganizationId}/profile");
        Authorize(client, ownerTokens);
        var missing = await client.GetAsync($"/api/v1/organizations/{Guid.NewGuid()}/profile");

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task OrganizationMembers_AllowsViewerReadAndMergesSameUserAcrossWorkspaces()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await GetBootstrapAsync(client);
        var viewerTokens = await RegisterAndAddMemberAsync(
            client,
            ownerTokens,
            bootstrap.Workspace.Id,
            WorkspaceMemberRole.Viewer);
        var viewerId = await GetCurrentUserIdAsync(client, viewerTokens);
        await AddWorkspaceMemberInSecondWorkspaceAsync(
            factory,
            Guid.Parse(bootstrap.Workspace.OrganizationId),
            viewerId,
            WorkspaceMemberRole.Editor);

        Authorize(client, viewerTokens);
        var response = await client.GetAsync($"/api/v1/organizations/{bootstrap.Workspace.OrganizationId}/members");

        response.EnsureSuccessStatusCode();
        var members = await response.Content.ReadFromJsonAsync<OrganizationMembersResponse>(JsonOptions);
        Assert.NotNull(members);
        var viewer = Assert.Single(members.Members.Where(member => member.UserId == viewerId.ToString()));
        Assert.Equal("active", viewer.Status);
        Assert.Equal(2, viewer.Workspaces.Count);
        Assert.Contains(viewer.Workspaces, workspace => workspace.WorkspaceId == bootstrap.Workspace.Id && workspace.Role == WorkspaceMemberRole.Viewer);
        Assert.Contains(viewer.Workspaces, workspace => workspace.Role == WorkspaceMemberRole.Editor);
        Assert.All(members.Members, member => Assert.NotNull(member.Workspaces));
    }

    [Fact]
    public async Task OrganizationProfileUpdate_OwnerCanRenameAndProfileReadReturnsUpdatedDto()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await GetBootstrapAsync(client);
        var organizationId = bootstrap.Workspace.OrganizationId;
        var original = await client.GetFromJsonAsync<OrganizationProfileResponse>(
            $"/api/v1/organizations/{organizationId}/profile",
            JsonOptions);
        Assert.NotNull(original);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/organizations/{organizationId}/profile",
            new UpdateOrganizationProfileRequest("  Northstar Atlas  ", " atlas-team "));

        response.EnsureSuccessStatusCode();
        var rawJson = await response.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<OrganizationProfileResponse>(rawJson, JsonOptions);
        var reread = await client.GetFromJsonAsync<OrganizationProfileResponse>(
            $"/api/v1/organizations/{organizationId}/profile",
            JsonOptions);

        Assert.NotNull(updated);
        Assert.NotNull(reread);
        Assert.Equal("Northstar Atlas", updated.Organization.Name);
        Assert.Equal("atlas-team", updated.Organization.Slug);
        Assert.NotEqual(original.Organization.UpdatedAt, updated.Organization.UpdatedAt);
        Assert.Equal(updated.Organization.Name, reread.Organization.Name);
        Assert.Equal(updated.Organization.Slug, reread.Organization.Slug);
        Assert.DoesNotContain("deletedAt", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("workspaces\":null", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(WorkspaceMemberRole.Admin)]
    [InlineData(WorkspaceMemberRole.Editor)]
    [InlineData(WorkspaceMemberRole.Viewer)]
    public async Task OrganizationProfileUpdate_RequiresOwnerRole(string role)
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await GetBootstrapAsync(client);
        var memberTokens = await RegisterAndAddMemberAsync(client, ownerTokens, bootstrap.Workspace.Id, role);

        Authorize(client, memberTokens);
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/organizations/{bootstrap.Workspace.OrganizationId}/profile",
            new UpdateOrganizationProfileRequest("Blocked", "blocked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.Forbidden, error.Error.Code);
        Assert.Contains("Owner", error.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OrganizationProfileUpdate_ForbidsNonMemberAndMissingOrganizationReturnsNotFound()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        var ownerTokens = await LoginOwnerAsync(client);
        var bootstrap = await GetBootstrapAsync(client);
        var outsiderTokens = await RegisterAsync(client, $"org-outsider-{Guid.NewGuid():N}@northstar.local");

        Authorize(client, outsiderTokens);
        var forbidden = await client.PatchAsJsonAsync(
            $"/api/v1/organizations/{bootstrap.Workspace.OrganizationId}/profile",
            new UpdateOrganizationProfileRequest("Outsider", "outsider"));
        Authorize(client, ownerTokens);
        var missing = await client.PatchAsJsonAsync(
            $"/api/v1/organizations/{Guid.NewGuid()}/profile",
            new UpdateOrganizationProfileRequest("Missing", "missing"));

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task OrganizationProfileUpdate_ReturnsValidationEnvelopeForNameAndSlug()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await GetBootstrapAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/organizations/{bootstrap.Workspace.OrganizationId}/profile",
            new UpdateOrganizationProfileRequest("   ", " ### "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.ValidationError, error.Error.Code);
        Assert.Equal("Request validation failed.", error.Error.Message);
        var details = Assert.IsType<JsonElement>(error.Error.Details);
        Assert.True(details.TryGetProperty("fields", out var fields));
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("slug", out _));
    }

    [Fact]
    public async Task OrganizationProfileUpdate_RejectsDuplicateSlug()
    {
        using var factory = new NorthstarApiFactory();
        var client = factory.CreateClient();
        await LoginOwnerAsync(client);
        var bootstrap = await GetBootstrapAsync(client);
        await AddOrganizationAsync(factory, "Taken Organization", "taken");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/organizations/{bootstrap.Workspace.OrganizationId}/profile",
            new UpdateOrganizationProfileRequest("Northstar", " TAKEN "));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.Conflict, error.Error.Code);
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/bootstrap");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        return request;
    }

    private static async Task<BootstrapResponse> GetBootstrapAsync(HttpClient client)
    {
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap", JsonOptions);
        Assert.NotNull(bootstrap);
        return bootstrap;
    }

    private static async Task<Guid> GetCurrentUserIdAsync(HttpClient client, AuthTokenResponse tokens)
    {
        Authorize(client, tokens);
        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/auth/me", JsonOptions);
        Assert.NotNull(me);
        return Guid.Parse(me.User.Id);
    }

    private static async Task AddWorkspaceMemberInSecondWorkspaceAsync(
        NorthstarApiFactory factory,
        Guid organizationId,
        Guid userId,
        string role)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var workspace = new Workspace(
            "Second Workspace",
            $"second-{Guid.NewGuid():N}",
            createdBy: null,
            id: Guid.NewGuid(),
            organizationId: organizationId);
        await dbContext.Workspaces.AddAsync(workspace);
        await dbContext.WorkspaceMembers.AddAsync(new WorkspaceMember(workspace.Id, userId, role));
        await dbContext.SaveChangesAsync();
    }

    private static async Task AddOrganizationAsync(
        NorthstarApiFactory factory,
        string name,
        string slug)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        await dbContext.Organizations.AddAsync(new Organization(name, slug));
        await dbContext.SaveChangesAsync();
    }

    private static async Task<AuthTokenResponse> LoginOwnerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(OwnerEmail, OwnerPassword));
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(tokens);
        Authorize(client, tokens);
        return tokens;
    }

    private static async Task<AuthTokenResponse> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(email, "Test User", "Northstar.test.123!"));
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(tokens);
        return tokens;
    }

    private static async Task<AuthTokenResponse> RegisterAndAddMemberAsync(
        HttpClient client,
        AuthTokenResponse ownerTokens,
        string workspaceId,
        string role)
    {
        var email = $"{role}-{Guid.NewGuid():N}@northstar.local";
        var tokens = await RegisterAsync(client, email);
        Authorize(client, ownerTokens);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/members",
            new AddWorkspaceMemberRequest(email, role));
        response.EnsureSuccessStatusCode();
        return tokens;
    }

    private static void Authorize(HttpClient client, AuthTokenResponse tokens)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    private static void AuthorizeBearer(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<CreateScimTokenResponse> CreateScimTokenAsync(
        HttpClient client,
        string workspaceId,
        string name = "directory sync")
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/scim/tokens",
            new CreateScimTokenRequest(name, DateTimeOffset.UtcNow.AddHours(1)));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreateScimTokenResponse>();
        Assert.NotNull(created);
        return created;
    }

    private static async Task<ScimUserResource> CreateScimUserAsync(
        HttpClient client,
        string workspaceId,
        string userName,
        string externalId,
        string displayName)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/scim/v2/Users",
            new CreateScimUserRequest(userName, externalId, displayName, null, true));
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<ScimUserResource>();
        Assert.NotNull(user);
        return user;
    }

    private static async Task AssertScimUnauthorizedAsync(HttpResponseMessage response)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.Unauthorized, error.Error.Code);
        Assert.Equal("SCIM authentication is required.", error.Error.Message);
    }

    private static IReadOnlyDictionary<string, string?> IdpLoginConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["Auth:IdpLogin:Enabled"] = "true",
            ["Auth:IdpLogin:AllowedProviders:0"] = "okta"
        };
    }

    private static KnowledgeDocumentSummaryDto FindDocument(BootstrapResponse bootstrap, string title)
    {
        return bootstrap.Documents.Single(document => document.Title == title);
    }

    private static JsonElement Json(string json)
    {
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    private static CreateUploadSessionRequest CreateUploadSessionRequest(
        string documentId,
        byte[] bytes,
        string? idempotencyKey = null,
        string originalFilename = "sample.txt",
        string mimeType = "text/plain",
        string? checksum = null)
    {
        return new CreateUploadSessionRequest(
            idempotencyKey ?? $"upload-{Guid.NewGuid():N}",
            originalFilename,
            mimeType,
            bytes.LongLength,
            checksum ?? Sha256(bytes),
            "knowledge",
            "single",
            null,
            documentId);
    }

    private static async Task<CreateUploadSessionResponse> CreateUploadSessionAsync(
        HttpClient client,
        string documentId,
        byte[] bytes,
        string? idempotencyKey = null,
        string originalFilename = "sample.txt",
        string mimeType = "text/plain",
        string? checksum = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/files/uploads/sessions",
            CreateUploadSessionRequest(documentId, bytes, idempotencyKey, originalFilename, mimeType, checksum));
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<CreateUploadSessionResponse>();
        Assert.NotNull(session);
        return session;
    }

    private static async Task UploadContentAsync(HttpClient client, string sessionId, byte[] bytes)
    {
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        var response = await client.PutAsync($"/api/v1/files/uploads/sessions/{sessionId}/content", content);
        response.EnsureSuccessStatusCode();
    }

    private static async Task CompleteSessionAsync(HttpClient client, string sessionId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/files/uploads/sessions/{sessionId}/complete",
            new CompleteUploadSessionRequest());
        response.EnsureSuccessStatusCode();
    }

    private static async Task<FinalizeUploadSessionResponse> FinalizeSessionAsync(
        HttpClient client,
        string sessionId,
        string? documentId = null,
        string relationType = DocumentAttachmentRelationType.Attachment)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/files/uploads/sessions/{sessionId}/finalize",
            new FinalizeUploadSessionRequest(documentId, relationType, null));
        response.EnsureSuccessStatusCode();
        var finalized = await response.Content.ReadFromJsonAsync<FinalizeUploadSessionResponse>();
        Assert.NotNull(finalized);
        return finalized;
    }

    private static async Task<FinalizeUploadSessionResponse> UploadCompleteAndFinalizeAsync(
        HttpClient client,
        string documentId,
        byte[] bytes,
        string originalFilename = "sample.txt",
        string mimeType = "text/plain",
        bool attachOnFinalize = true)
    {
        var session = await CreateUploadSessionAsync(
            client,
            documentId,
            bytes,
            originalFilename: originalFilename,
            mimeType: mimeType);
        await UploadContentAsync(client, session.SessionId, bytes);
        await CompleteSessionAsync(client, session.SessionId);
        return await FinalizeSessionAsync(
            client,
            session.SessionId,
            attachOnFinalize ? documentId : null);
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static async Task<int> CountFilesAsync(NorthstarApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.Files.CountAsync();
    }

    private static async Task<int> CountFileOutboxEventsAsync(
        NorthstarApiFactory factory,
        Guid aggregateId,
        string eventType)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.FileOutboxEvents.CountAsync(outbox =>
            outbox.AggregateId == aggregateId &&
            outbox.EventType == eventType);
    }

    private static async Task<FileOutboxProcessResult> RunFileOutboxProcessorAsync(
        NorthstarApiFactory factory,
        DateTimeOffset? now = null)
    {
        using var scope = factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IFileOutboxProcessor>();
        return await processor.ProcessDueAsync(now, batchSize: 50);
    }

    private static async Task<FileOutboxEvent?> ReadFileOutboxEventAsync(
        NorthstarApiFactory factory,
        Guid aggregateId,
        string eventType)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.FileOutboxEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(outbox =>
                outbox.AggregateId == aggregateId &&
                outbox.EventType == eventType);
    }

    private static async Task EnsureS3AcceptanceBucketAsync(
        string endpoint,
        string accessKey,
        string secretKey,
        string bucket)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
            RegionEndpoint = RegionEndpoint.USEast1
        };
        using var client = new AmazonS3Client(
            new BasicAWSCredentials(accessKey, secretKey),
            config);
        var buckets = await client.ListBucketsAsync();
        if (buckets.Buckets.Any(item => string.Equals(item.BucketName, bucket, StringComparison.Ordinal)))
        {
            return;
        }

        await client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = bucket
        });
    }

    private static async Task<Guid> CreateCrossWorkspaceFileAsync(NorthstarApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace("Other Workspace", $"other-{Guid.NewGuid():N}", id: workspaceId);
        var file = new StoredFile(
            workspaceId,
            null,
            "Local",
            "northstar-test",
            $"workspaces/{workspaceId}/files/2026/04/{Guid.NewGuid():N}",
            "other.txt",
            "text/plain",
            1);
        await dbContext.Workspaces.AddAsync(workspace);
        await dbContext.Files.AddAsync(file);
        await dbContext.SaveChangesAsync();
        return file.Id;
    }

    private static async Task SeedResourceGrantAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid subjectId,
        string roleKey,
        DateTimeOffset? expiresAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        await dbContext.ResourceAccessGrants.AddAsync(new ResourceAccessGrant(
            workspaceId,
            resourceType,
            resourceId,
            SubjectTypes.User,
            subjectId,
            roleKey,
            expiresAt: expiresAt));
        await dbContext.SaveChangesAsync();
    }

    private static async Task SetDocumentPolicyAsync(
        HttpClient client,
        string documentId,
        string inheritanceMode)
    {
        await SetResourcePolicyAsync(
            client,
            ResourceTypes.Document,
            documentId,
            inheritanceMode,
            LinkModes.Disabled);
    }

    private static async Task SetResourcePolicyAsync(
        HttpClient client,
        string resourceType,
        string resourceId,
        string inheritanceMode,
        string linkMode,
        string? defaultLinkRole = null)
    {
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/permissions/resources/{resourceType}/{resourceId}/policy",
            new UpdateResourcePolicyRequest(inheritanceMode, linkMode, defaultLinkRole));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<PermissionGrantDto> CreateDocumentGrantAsync(
        HttpClient client,
        string documentId,
        string subjectId,
        string roleKey,
        DateTimeOffset? expiresAt = null)
    {
        return await CreateDocumentSubjectGrantAsync(client, documentId, SubjectTypes.User, subjectId, roleKey, expiresAt);
    }

    private static async Task<PermissionGrantDto> CreateDocumentSubjectGrantAsync(
        HttpClient client,
        string documentId,
        string subjectType,
        string subjectId,
        string roleKey,
        DateTimeOffset? expiresAt = null)
    {
        return await CreateSubjectGrantAsync(
            client,
            ResourceTypes.Document,
            documentId,
            subjectType,
            subjectId,
            roleKey,
            expiresAt);
    }

    private static async Task<PermissionGrantDto> CreateSubjectGrantAsync(
        HttpClient client,
        string resourceType,
        string resourceId,
        string subjectType,
        string subjectId,
        string roleKey,
        DateTimeOffset? expiresAt = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/{resourceType}/{resourceId}/grants",
            new CreatePermissionGrantRequest(subjectType, subjectId, roleKey, expiresAt, null));
        response.EnsureSuccessStatusCode();
        var grant = await response.Content.ReadFromJsonAsync<PermissionGrantDto>();
        Assert.NotNull(grant);
        return grant;
    }

    private static async Task<IamSyncResponse> SyncIamAsync(
        HttpClient client,
        string workspaceId,
        IamSyncRequest request)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/iam/sync",
            request);
        response.EnsureSuccessStatusCode();
        var sync = await response.Content.ReadFromJsonAsync<IamSyncResponse>();
        Assert.NotNull(sync);
        return sync;
    }

    private static IamSyncRequest CreateIamSyncRequest(string workspaceId)
    {
        return new IamSyncRequest(
            "okta",
            [
                new IamSyncUserRequest("u-alpha", "alpha@example.test", "Alpha User", null, workspaceId),
                new IamSyncUserRequest("u-beta", "beta@example.test", "Beta User", null, workspaceId)
            ],
            [
                new IamSyncGroupRequest(
                    "eng",
                    "Engineering",
                    "Synced engineering group",
                    ["u-alpha", "u-beta"],
                    workspaceId)
            ]);
    }

    private static Dictionary<string, string?> PublicShareEnabledConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["Permissions:PublicShareLinks:Enabled"] = "true",
            ["Permissions:PublicShareLinks:RequireExpiry"] = "true",
            ["Permissions:PublicShareLinks:ViewerOnly"] = "true",
            ["Permissions:PublicShareLinks:MaxExpiryDays"] = "7",
            ["Permissions:PublicShareLinks:RateLimit:PermitLimit"] = "1000",
            ["Permissions:PublicShareLinks:RateLimit:WindowSeconds"] = "60",
            ["Permissions:PublicShareLinks:RateLimit:QueueLimit"] = "0"
        };
    }

    private static async Task<CreateShareLinkResponse> CreateShareLinkAsync(
        HttpClient client,
        string resourceType,
        string resourceId,
        string roleKey,
        DateTimeOffset? expiresAt = null,
        string? audience = ShareLinkAudiences.Workspace,
        string? subjectEmail = null,
        string? password = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/{resourceType}/{resourceId}/share-links",
            new CreateShareLinkRequest(roleKey, audience, expiresAt, subjectEmail, password));
        response.EnsureSuccessStatusCode();
        var link = await response.Content.ReadFromJsonAsync<CreateShareLinkResponse>();
        Assert.NotNull(link);
        return link;
    }

    private static async Task<CreateDocumentResponse> CreateDocumentAsync(
        HttpClient client,
        string collectionId,
        string title)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/documents",
            new CreateDocumentRequest(collectionId, title));
        response.EnsureSuccessStatusCode();
        var document = await response.Content.ReadFromJsonAsync<CreateDocumentResponse>();
        Assert.NotNull(document);
        return document;
    }

    private static async Task<HttpResponseMessage> GetPublicShareAsync(
        HttpClient client,
        string path,
        string? password = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrWhiteSpace(password))
        {
            request.Headers.Add("X-Share-Link-Password", password);
        }

        return await client.SendAsync(request);
    }

    private static async Task<CreateEmailInviteResponse> CreateEmailInviteAsync(
        HttpClient client,
        string resourceType,
        string resourceId,
        string email,
        string roleKey,
        DateTimeOffset expiresAt)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/permissions/resources/{resourceType}/{resourceId}/email-invites",
            new CreateEmailInviteRequest(email, roleKey, expiresAt));
        response.EnsureSuccessStatusCode();
        var invite = await response.Content.ReadFromJsonAsync<CreateEmailInviteResponse>();
        Assert.NotNull(invite);
        return invite;
    }

    private static async Task AssertBulkEndpointsDoNotLeakDocumentAsync(
        HttpClient client,
        BootstrapResponse bootstrap,
        string documentId,
        string? shareToken)
    {
        var queryToken = string.IsNullOrWhiteSpace(shareToken)
            ? string.Empty
            : $"&shareToken={Uri.EscapeDataString(shareToken)}";
        var pathToken = string.IsNullOrWhiteSpace(shareToken)
            ? string.Empty
            : $"?shareToken={Uri.EscapeDataString(shareToken)}";

        var searchResponse = await client.GetAsync($"/api/v1/search?q=Principles&spaceId={bootstrap.ActiveSpaceId}{queryToken}");
        if (searchResponse.StatusCode == HttpStatusCode.OK)
        {
            var search = await searchResponse.Content.ReadFromJsonAsync<SearchResponse>();
            Assert.NotNull(search);
            Assert.DoesNotContain(search.Results, result => result.Id == documentId);
        }
        else
        {
            Assert.Contains(searchResponse.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        }

        var exportResponse = await client.GetAsync($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/export{pathToken}");
        if (exportResponse.StatusCode == HttpStatusCode.OK)
        {
            var export = await exportResponse.Content.ReadFromJsonAsync<ExportSpaceResponse>();
            Assert.NotNull(export);
            Assert.DoesNotContain(export.Documents, item => item.Id == documentId);
        }
        else
        {
            Assert.Contains(exportResponse.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        }

        var mapResponse = await client.GetAsync($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/map{pathToken}");
        if (mapResponse.StatusCode == HttpStatusCode.OK)
        {
            var map = await mapResponse.Content.ReadFromJsonAsync<KnowledgeMapResponse>();
            Assert.NotNull(map);
            Assert.DoesNotContain(map.Documents, item => item.Id == documentId);
        }
        else
        {
            Assert.Contains(mapResponse.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        }

        var bootstrapResponse = await client.GetAsync($"/api/v1/bootstrap{pathToken}");
        if (bootstrapResponse.StatusCode == HttpStatusCode.OK)
        {
            var bootstrapBody = await bootstrapResponse.Content.ReadFromJsonAsync<BootstrapResponse>();
            Assert.NotNull(bootstrapBody);
            Assert.DoesNotContain(bootstrapBody.Documents, item => item.Id == documentId);
        }
        else
        {
            Assert.Contains(bootstrapResponse.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        }
    }

    private static async Task<AccessRequestDto> CreateAccessRequestAsync(
        HttpClient client,
        string documentId,
        string requestedRole)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/permissions/access-requests",
            new CreateAccessRequestRequest(ResourceTypes.Document, documentId, requestedRole, "request access"));
        response.EnsureSuccessStatusCode();
        var request = await response.Content.ReadFromJsonAsync<AccessRequestDto>();
        Assert.NotNull(request);
        return request;
    }

    private static async Task<AccessRequestDto> ReviewAccessRequestAsync(
        HttpClient client,
        string requestId,
        string decision,
        string? roleKey,
        string reason,
        DateTimeOffset? expiresAt = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/permissions/access-requests/{requestId}/review",
            new ReviewAccessRequestRequest(decision, roleKey, reason, expiresAt));
        response.EnsureSuccessStatusCode();
        var request = await response.Content.ReadFromJsonAsync<AccessRequestDto>();
        Assert.NotNull(request);
        return request;
    }

    private static async Task<WorkspaceGroupDto> CreateWorkspaceGroupAsync(
        HttpClient client,
        string workspaceId,
        string name)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/groups",
            new CreateWorkspaceGroupRequest(name, null, GroupTypes.Static));
        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<WorkspaceGroupDto>();
        Assert.NotNull(group);
        return group;
    }

    private static async Task<WorkspaceGroupMemberDto> AddWorkspaceGroupMemberAsync(
        HttpClient client,
        string workspaceId,
        string groupId,
        string userId,
        DateTimeOffset? expiresAt = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspaceId}/groups/{groupId}/members",
            new AddWorkspaceGroupMemberRequest(userId, expiresAt));
        response.EnsureSuccessStatusCode();
        var member = await response.Content.ReadFromJsonAsync<WorkspaceGroupMemberDto>();
        Assert.NotNull(member);
        return member;
    }

    private static async Task<Guid> SeedWorkspaceGroupMemberAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset? expiresAt)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var group = new WorkspaceGroup(workspaceId, $"Seeded Group {Guid.NewGuid():N}");
        var member = new WorkspaceGroupMember(group.Id, userId, expiresAt: expiresAt);
        await dbContext.WorkspaceGroups.AddAsync(group);
        await dbContext.WorkspaceGroupMembers.AddAsync(member);
        await dbContext.SaveChangesAsync();
        return group.Id;
    }

    private static async Task SeedGroupMemberAsync(
        NorthstarApiFactory factory,
        Guid groupId,
        Guid userId,
        DateTimeOffset? expiresAt)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var member = new WorkspaceGroupMember(groupId, userId, expiresAt: expiresAt);
        await dbContext.WorkspaceGroupMembers.AddAsync(member);
        await dbContext.SaveChangesAsync();
    }

    private static async Task RunPermissionExpiryProcessorAsync(NorthstarApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<PermissionExpiryNotificationProcessor>();
        await processor.RunOnceAsync();
    }

    private static async Task<string> SeedShareLinkAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string roleKey,
        DateTimeOffset? expiresAt,
        string audience = ShareLinkAudiences.Workspace,
        string? subjectEmail = null,
        string? password = null)
    {
        using var scope = factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IShareLinkTokenService>();
        var passwordHashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var token = tokenService.GenerateToken();
        var passwordHash = string.IsNullOrWhiteSpace(password)
            ? null
            : passwordHashService.HashPassword(new User("share-link-password", id: Guid.Empty), password.Trim());
        var link = new ShareLink(
            workspaceId,
            resourceType,
            resourceId,
            tokenService.HashToken(token),
            roleKey,
            audience,
            expiresAt: expiresAt,
            subjectEmail: subjectEmail,
            passwordHash: passwordHash);
        await dbContext.ShareLinks.AddAsync(link);
        await dbContext.SaveChangesAsync();
        return token;
    }

    private static async Task<string> SeedEmailInviteAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string email,
        string roleKey,
        DateTimeOffset expiresAt)
    {
        using var scope = factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IShareLinkTokenService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var token = tokenService.GenerateToken();
        var invite = new ResourceEmailInvite(
            workspaceId,
            resourceType,
            resourceId,
            email,
            tokenService.HashToken(token),
            roleKey,
            expiresAt);
        await dbContext.ResourceEmailInvites.AddAsync(invite);
        await dbContext.SaveChangesAsync();
        return token;
    }

    private static async Task<int> CountNotificationsAsync(
        NorthstarApiFactory factory,
        Guid recipientUserId,
        string type)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.PermissionNotifications.CountAsync(notification =>
            notification.RecipientUserId == recipientUserId &&
            notification.Type == type);
    }

    private static async Task<int> CountAllNotificationsAsync(NorthstarApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.PermissionNotifications.CountAsync();
    }

    private static async Task<IReadOnlyList<PermissionNotification>> ReadNotificationsAsync(
        NorthstarApiFactory factory,
        Guid recipientUserId,
        string type)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.PermissionNotifications
            .AsNoTracking()
            .Where(notification =>
                notification.RecipientUserId == recipientUserId &&
                notification.Type == type)
            .OrderBy(notification => notification.CreatedAt)
            .ToListAsync();
    }

    private static async Task<int> CountAuditEventsAsync(NorthstarApiFactory factory, string action)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.PermissionAuditEvents.CountAsync(audit => audit.Action == action);
    }

    private static async Task<ResourceAccessGrant?> ReadGrantAsync(
        NorthstarApiFactory factory,
        Guid grantId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.ResourceAccessGrants
            .AsNoTracking()
            .SingleOrDefaultAsync(grant => grant.Id == grantId);
    }

    private static async Task<ShareLink?> ReadShareLinkAsync(
        NorthstarApiFactory factory,
        Guid shareLinkId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.ShareLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(link => link.Id == shareLinkId);
    }

    private static async Task<int> CountShareLinkAccessEventsAsync(NorthstarApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.ShareLinkAccessEvents.CountAsync();
    }

    private static async Task<IReadOnlyList<ShareLinkAccessEvent>> ReadShareLinkAccessEventsAsync(
        NorthstarApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.ShareLinkAccessEvents
            .AsNoTracking()
            .OrderBy(accessEvent => accessEvent.OccurredAt)
            .ToListAsync();
    }

    private static string ReadMigrationFile(string suffix)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Northstar.Infrastructure", "Persistence", "Migrations");
            if (Directory.Exists(candidate))
            {
                var match = Directory.GetFiles(candidate, $"*{suffix}").SingleOrDefault();
                if (match is not null)
                {
                    return File.ReadAllText(match);
                }
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Migration ending with '{suffix}' was not found.");
    }

    private static async Task<ScimToken?> ReadScimTokenAsync(
        NorthstarApiFactory factory,
        Guid tokenId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.ScimTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.Id == tokenId);
    }

    private static async Task<Guid> SeedScimTokenAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        string rawToken,
        DateTimeOffset? expiresAt,
        bool revoked = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        if (!await dbContext.Workspaces.AnyAsync(workspace => workspace.Id == workspaceId))
        {
            await dbContext.Workspaces.AddAsync(new Workspace(
                "SCIM Token Workspace",
                $"scim-token-{Guid.NewGuid():N}",
                id: workspaceId));
        }

        var tokenService = new ShareLinkTokenService();
        var token = new ScimToken(
            workspaceId,
            "seeded SCIM token",
            tokenService.HashToken(rawToken),
            expiresAt: expiresAt);
        if (revoked)
        {
            token.Revoke(DateTimeOffset.UtcNow);
        }

        await dbContext.ScimTokens.AddAsync(token);
        await dbContext.SaveChangesAsync();
        return token.Id;
    }

    private static async Task<ResourceAccessPolicy?> ReadResourcePolicyAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        string resourceType,
        Guid resourceId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.ResourceAccessPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(policy =>
                policy.WorkspaceId == workspaceId &&
                policy.ResourceType == resourceType &&
                policy.ResourceId == resourceId);
    }

    private static async Task<ResourceEmailInvite?> ReadEmailInviteAsync(
        NorthstarApiFactory factory,
        Guid inviteId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.ResourceEmailInvites
            .AsNoTracking()
            .SingleOrDefaultAsync(invite => invite.Id == inviteId);
    }

    private static async Task<EmailInviteDeliveryOutboxItem?> ReadEmailInviteDeliveryOutboxAsync(
        NorthstarApiFactory factory,
        Guid inviteId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.EmailInviteDeliveryOutbox
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.InviteId == inviteId);
    }

    private static async Task<User?> ReadExternalUserAsync(
        NorthstarApiFactory factory,
        string provider,
        string externalSubjectId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user =>
                user.ExternalProvider == provider &&
                user.ExternalSubjectId == externalSubjectId);
    }

    private static async Task<int> CountExternalUsersAsync(NorthstarApiFactory factory, string provider)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.Users.CountAsync(user => user.ExternalProvider == provider);
    }

    private static async Task<int> CountUserCredentialsAsync(NorthstarApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.UserCredentials.CountAsync(credential => credential.UserId == userId);
    }

    private static async Task<AuthEvent?> ReadLatestAuthEventAsync(
        NorthstarApiFactory factory,
        string action,
        Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.AuthEvents
            .AsNoTracking()
            .Where(authEvent => authEvent.Action == action && authEvent.UserId == userId)
            .OrderByDescending(authEvent => authEvent.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private static async Task<UserMfaMethod?> ReadUserMfaMethodAsync(
        NorthstarApiFactory factory,
        Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.UserMfaMethods
            .AsNoTracking()
            .Where(method => method.UserId == userId)
            .OrderByDescending(method => method.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private static async Task DeleteAuthEventsAsync(
        NorthstarApiFactory factory,
        string action,
        Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var events = await dbContext.AuthEvents
            .Where(authEvent => authEvent.Action == action && authEvent.UserId == userId)
            .ToListAsync();
        dbContext.AuthEvents.RemoveRange(events);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<TotpEnrollmentResponse> EnrollAndVerifyTotpAsync(HttpClient client)
    {
        var enrollResponse = await client.PostAsync("/api/v1/auth/mfa/totp/enroll", null);
        enrollResponse.EnsureSuccessStatusCode();
        var enrollment = await enrollResponse.Content.ReadFromJsonAsync<TotpEnrollmentResponse>();
        Assert.NotNull(enrollment);
        var verifyResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/totp/verify",
            new VerifyTotpRequest(CreateTotpCode(enrollment.Secret, DateTimeOffset.UtcNow)));
        verifyResponse.EnsureSuccessStatusCode();
        return enrollment;
    }

    private static string CreateTotpCode(string secret, DateTimeOffset now)
    {
        var counter = now.ToUnixTimeSeconds() / 30;
        var secretBytes = FromBase32(secret);
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary =
            ((hash[offset] & 0x7f) << 24) |
            ((hash[offset + 1] & 0xff) << 16) |
            ((hash[offset + 2] & 0xff) << 8) |
            (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] FromBase32(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var normalized = input.Trim().Replace("=", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        var output = new List<byte>();
        var bitBuffer = 0;
        var bitCount = 0;
        foreach (var item in normalized)
        {
            var value = alphabet.IndexOf(item, StringComparison.Ordinal);
            Assert.True(value >= 0, "TOTP secret contains invalid base32 character.");
            bitBuffer = (bitBuffer << 5) | value;
            bitCount += 5;
            if (bitCount >= 8)
            {
                output.Add((byte)((bitBuffer >> (bitCount - 8)) & 0xff));
                bitCount -= 8;
            }
        }

        return output.ToArray();
    }

    private static async Task<WorkspaceGroup?> ReadExternalGroupAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        string provider,
        string externalGroupId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.WorkspaceGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(group =>
                group.WorkspaceId == workspaceId &&
                group.ExternalProvider == provider &&
                group.ExternalGroupId == externalGroupId);
    }

    private static async Task<int> CountExternalGroupsAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        string provider)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.WorkspaceGroups.CountAsync(group =>
            group.WorkspaceId == workspaceId &&
            group.ExternalProvider == provider);
    }

    private static async Task<int> CountActiveGroupMembersAsync(
        NorthstarApiFactory factory,
        Guid groupId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.WorkspaceGroupMembers.CountAsync(member =>
            member.GroupId == groupId &&
            member.RemovedAt == null &&
            (member.ExpiresAt == null || member.ExpiresAt > DateTimeOffset.UtcNow));
    }

    private static async Task<bool> UserIsWorkspaceMemberAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.WorkspaceMembers.AnyAsync(member =>
            member.WorkspaceId == workspaceId &&
            member.UserId == userId &&
            member.Status == WorkspaceMemberStatus.Active);
    }

    private static async Task<string?> ReadWorkspaceMemberRoleAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.WorkspaceMembers
            .Where(member =>
                member.WorkspaceId == workspaceId &&
                member.UserId == userId)
            .Select(member => member.Role)
            .SingleOrDefaultAsync();
    }

    private static async Task<int> CountWorkspaceMembersAsync(
        NorthstarApiFactory factory,
        Guid workspaceId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.WorkspaceMembers.CountAsync(member =>
            member.WorkspaceId == workspaceId &&
            member.Status == WorkspaceMemberStatus.Active);
    }

    private static async Task<ResourceAccessGrant?> ReadUserGrantAsync(
        NorthstarApiFactory factory,
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        return await dbContext.ResourceAccessGrants
            .AsNoTracking()
            .SingleOrDefaultAsync(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == SubjectTypes.User &&
                grant.SubjectId == userId &&
                grant.RevokedAt == null);
    }

    private static async Task<Guid> SeedOtherWorkspaceGroupAsync(NorthstarApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace("Other Workspace", $"other-{Guid.NewGuid():N}", id: workspaceId);
        var group = new WorkspaceGroup(workspaceId, "Other Group");
        await dbContext.Workspaces.AddAsync(workspace);
        await dbContext.WorkspaceGroups.AddAsync(group);
        await dbContext.SaveChangesAsync();
        return group.Id;
    }

    private static string FindRepositoryFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. relativeParts]);
            if (System.IO.File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Repository file was not found.", Path.Combine(relativeParts));
    }

    private static async Task<int> CountActivityAsync(
        NorthstarApiFactory factory,
        string documentId,
        string action)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var id = Guid.Parse(documentId);
        return await dbContext.ActivityEvents.CountAsync(activity =>
            activity.EntityId == id &&
            activity.Action == action);
    }

    private static async Task<DocumentPersistenceState> ReadDocumentPersistenceStateAsync(
        NorthstarApiFactory factory,
        string documentId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
        var id = Guid.Parse(documentId);
        var document = await dbContext.Documents.AsNoTracking().SingleAsync(document => document.Id == id);
        var draft = await dbContext.DocumentDrafts.AsNoTracking().SingleAsync(draft => draft.DocumentId == id);
        var versionCount = await dbContext.DocumentVersions.AsNoTracking().CountAsync(version => version.DocumentId == id);
        var searchCount = await dbContext.DocumentSearchIndexes.AsNoTracking().CountAsync(index => index.DocumentId == id);
        var activityCount = await dbContext.ActivityEvents.AsNoTracking().CountAsync(activity => activity.EntityId == id);

        return new DocumentPersistenceState(
            document.Revision,
            draft.Content,
            draft.ContentHash,
            versionCount,
            searchCount,
            activityCount);
    }

    private static JsonElement CreateCommentAnchor(string documentId)
    {
        return Json($$"""
        {
          "schema": "northstar.commentAnchor.v1",
          "kind": "tiptap.textRange",
          "documentId": "{{documentId}}",
          "baseRevision": 0,
          "pm": {
            "from": 1,
            "to": 6
          },
          "block": {
            "start": {
              "blockId": "blk_backend0001",
              "path": [0],
              "nodeType": "paragraph",
              "textOffset": 0
            },
            "end": {
              "blockId": "blk_backend0001",
              "path": [0],
              "nodeType": "paragraph",
              "textOffset": 5
            }
          },
          "quote": {
            "exact": "alpha",
            "prefix": "",
            "suffix": "",
            "normalizedExact": "alpha",
            "normalizer": "northstar.plainText.v1"
          },
          "display": {
            "excerpt": "alpha"
          }
        }
        """);
    }

    private static void AssertDoesNotContainRuntimeCommentState(string json)
    {
        Assert.DoesNotContain("rangesByThreadId", json, StringComparison.Ordinal);
        Assert.DoesNotContain("runtimeRange", json, StringComparison.Ordinal);
        Assert.DoesNotContain("mappedRange", json, StringComparison.Ordinal);
        Assert.DoesNotContain("runtimeMatch", json, StringComparison.Ordinal);
        Assert.DoesNotContain("DecorationSet", json, StringComparison.Ordinal);
        Assert.DoesNotContain("activeThreadId", json, StringComparison.Ordinal);
        Assert.DoesNotContain("pendingCommentComposer", json, StringComparison.Ordinal);
        Assert.DoesNotContain("content", json, StringComparison.Ordinal);
    }

    private static async Task AssertFileMetadataDoesNotExposeStorageInternalsAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        AssertJsonDoesNotContainProperty(document.RootElement, "storageProvider");
        AssertJsonDoesNotContainProperty(document.RootElement, "bucket");
        AssertJsonDoesNotContainProperty(document.RootElement, "objectKey");
    }

    private static void AssertJsonDoesNotContainProperty(JsonElement element, string propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Assert.NotEqual(propertyName, property.Name);
                    AssertJsonDoesNotContainProperty(property.Value, propertyName);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AssertJsonDoesNotContainProperty(item, propertyName);
                }

                break;
        }
    }

    private sealed record DocumentPersistenceState(
        long Revision,
        string DraftContent,
        string? DraftContentHash,
        int VersionCount,
        int SearchCount,
        int ActivityCount);

    private sealed class FakeEmailInviteDeliveryService : IEmailInviteDeliveryService
    {
        public List<EmailInviteDeliveryMessage> Messages { get; } = [];

        public Task<EmailInviteDeliveryResult> SendAsync(
            EmailInviteDeliveryMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.FromResult(new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Sent,
                "fake",
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class FailingEmailInviteDeliveryService : IEmailInviteDeliveryService
    {
        public Task<EmailInviteDeliveryResult> SendAsync(
            EmailInviteDeliveryMessage message,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("fake delivery failure");
        }
    }

    private sealed class SequenceEmailInviteDeliveryService : IEmailInviteDeliveryService
    {
        private readonly Queue<bool> _results;
        private readonly string _provider;

        public SequenceEmailInviteDeliveryService(string provider, params bool[] results)
        {
            _provider = provider;
            _results = new Queue<bool>(results);
        }

        public List<EmailInviteDeliveryMessage> Messages { get; } = [];

        public Task<EmailInviteDeliveryResult> SendAsync(
            EmailInviteDeliveryMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            var sent = _results.Count == 0 || _results.Dequeue();
            return Task.FromResult(sent
                ? new EmailInviteDeliveryResult(
                    EmailInviteDeliveryStatuses.Sent,
                    _provider,
                    DateTimeOffset.UtcNow)
                : new EmailInviteDeliveryResult(
                    EmailInviteDeliveryStatuses.Failed,
                    _provider,
                    DateTimeOffset.UtcNow,
                    "provider_error"));
        }
    }

    private sealed class RecordingObjectStorage : IObjectStorage
    {
        private readonly Dictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

        public bool FailDeletes { get; init; }

        public List<Guid> DeletedFileIds { get; } = [];

        public UploadTargetDto CreateUploadTarget(UploadSession session)
        {
            return new UploadTargetDto(
                "local-api",
                "PUT",
                $"/api/v1/files/uploads/sessions/{session.Id}/content",
                new Dictionary<string, string>());
        }

        public async Task WriteUploadContentAsync(
            UploadSession session,
            Stream content,
            long maxBytes,
            CancellationToken cancellationToken = default)
        {
            using var memory = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                total += read;
                if (total > maxBytes || total > session.ByteSize)
                {
                    throw new ApplicationErrorException(
                        ErrorCodes.ValidationError,
                        "Uploaded content exceeds the configured size limit.");
                }

                memory.Write(buffer, 0, read);
            }

            _objects[session.ObjectKey] = memory.ToArray();
        }

        public Task<StoredObjectInfo?> GetObjectInfoAsync(
            UploadSession session,
            CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(session.ObjectKey, out var bytes))
            {
                return Task.FromResult<StoredObjectInfo?>(null);
            }

            return Task.FromResult<StoredObjectInfo?>(new StoredObjectInfo(bytes.LongLength, Sha256(bytes)));
        }

        public Task<Stream> OpenReadAsync(StoredFile file, CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(file.ObjectKey, out var bytes))
            {
                throw new ApplicationErrorException(ErrorCodes.NotFound, "File content was not found.");
            }

            return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
        }

        public Task DeleteObjectAsync(StoredFile file, CancellationToken cancellationToken = default)
        {
            if (FailDeletes)
            {
                throw new InvalidOperationException("delete_failed");
            }

            _objects.Remove(file.ObjectKey);
            DeletedFileIds.Add(file.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class NorthstarApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"northstar-api-tests-{Guid.NewGuid():N}";
        private readonly string _filesRoot = Path.Combine(
            Path.GetTempPath(),
            $"northstar-api-tests-files-{Guid.NewGuid():N}");
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;
        private readonly Action<IServiceCollection>? _configureServices;

        public NorthstarApiFactory(
            IReadOnlyDictionary<string, string?>? configurationOverrides = null,
            Action<IServiceCollection>? configureServices = null)
        {
            _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
            _configureServices = configureServices;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["Auth:SeedOwnerPassword"] = OwnerPassword,
                    ["Auth:Jwt:Issuer"] = "Northstar",
                    ["Auth:Jwt:Audience"] = "Northstar",
                    ["Auth:Jwt:SigningKey"] = "northstar-local-development-signing-key-change-me",
                    ["Auth:Jwt:AccessTokenMinutes"] = "15",
                    ["Auth:Jwt:RefreshTokenDays"] = "14",
                    ["Files:StorageProvider"] = "Local",
                    ["Files:LocalRootPath"] = _filesRoot,
                    ["Files:DefaultBucket"] = "northstar-test",
                    ["Files:UploadSessionMinutes"] = "60",
                    ["Files:MaxFileBytes"] = "1048576",
                    ["Files:AllowedMimeTypes:0"] = "text/plain",
                    ["Files:AllowedMimeTypes:1"] = "image/png",
                    ["Files:AllowedMimeTypes:2"] = "image/jpeg",
                    ["Files:AllowedMimeTypes:3"] = "image/webp",
                    ["Files:AllowedMimeTypes:4"] = "application/pdf"
                };
                foreach (var item in _configurationOverrides)
                {
                    settings[item.Key] = item.Value;
                }

                configuration.AddInMemoryCollection(settings);
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<NorthstarDbContext>>();
                services.AddDbContext<NorthstarDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
                _configureServices?.Invoke(services);
            });
        }
    }
}
