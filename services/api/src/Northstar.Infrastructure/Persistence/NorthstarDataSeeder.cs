using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Northstar.Application.Common;
using Northstar.Application.Knowledge;
using Northstar.Application.Security;
using Northstar.Domain.Knowledge.Activity;
using Northstar.Domain.Knowledge.Collections;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Links;
using Northstar.Domain.Knowledge.Spaces;
using Northstar.Domain.Knowledge.Tags;
using Northstar.Domain.Knowledge.Versions;
using Northstar.Domain.Organizations;
using Northstar.Domain.Shared;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Knowledge;
using Northstar.Infrastructure.Security;
using Northstar.Infrastructure.Search;

namespace Northstar.Infrastructure.Persistence;

public sealed class NorthstarDataSeeder : INorthstarDataSeeder
{
    private static readonly SeedCollection[] Collections =
    [
        new(SeedDataIds.OrientationCollectionId, "00. Orientation", "00-orientation", 0m),
        new(SeedDataIds.FoundationsCollectionId, "01. Foundations", "01-foundations", 1m),
        new(SeedDataIds.StrategyCollectionId, "02. Strategy", "02-strategy", 2m),
        new(SeedDataIds.WorkstreamsCollectionId, "03. Workstreams", "03-workstreams", 3m),
        new(SeedDataIds.GuidesCollectionId, "04. Guides & Playbooks", "04-guides-playbooks", 4m),
        new(SeedDataIds.ReferenceCollectionId, "05. Reference", "05-reference", 5m),
        new(SeedDataIds.ArchivesCollectionId, "06. Archives", "06-archives", 6m)
    ];

    private static readonly SeedDocument[] Documents =
    [
        new(
            SeedDataIds.PrinciplesDocumentId,
            SeedDataIds.OrientationCollectionId,
            "Our Principles",
            "our-principles",
            0m,
            TiptapParagraph("Our principles define how Northstar teams make durable product and engineering decisions."),
            ["principles", "orientation"]),
        new(
            SeedDataIds.MissionDocumentId,
            SeedDataIds.FoundationsCollectionId,
            "Mission & Vision",
            "mission-vision",
            1m,
            TiptapParagraphWithDocumentLink(
                "Northstar Atlas Library keeps product knowledge clear, connected, and ready for action through ",
                "Our Principles",
                SeedDataIds.PrinciplesDocumentId,
                "."),
            ["strategy"]),
        new(
            SeedDataIds.OperatingSystemDocumentId,
            SeedDataIds.StrategyCollectionId,
            "Operating System",
            "operating-system",
            2m,
            TiptapParagraphWithDocumentLink(
                "The operating system translates ",
                "Mission & Vision",
                SeedDataIds.MissionDocumentId,
                " into planning rhythms, ownership, and review loops for the workspace."),
            ["operations", "playbook"])
    ];

    private readonly NorthstarDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly AuthOptions _authOptions;

    public NorthstarDataSeeder(
        NorthstarDbContext dbContext,
        IPasswordHashService passwordHashService,
        IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _authOptions = authOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var user = await EnsureUserAsync(cancellationToken);
        await EnsureOwnerCredentialAsync(user, cancellationToken);
        var organization = await EnsureOrganizationAsync(cancellationToken);
        var workspace = await EnsureWorkspaceAsync(organization.Id, user.Id, cancellationToken);
        var space = await EnsureSpaceAsync(workspace.Id, user.Id, cancellationToken);
        await EnsureWorkspaceMemberAsync(workspace.Id, user.Id, cancellationToken);
        await EnsureCollectionsAsync(workspace.Id, space.Id, user.Id, cancellationToken);
        await EnsureDocumentsAsync(workspace.Id, space.Id, user.Id, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await EnsureSeedLinksAsync(workspace.Id, user.Id, cancellationToken);

        if (workspace.DefaultSpaceId is null)
        {
            workspace.SetDefaultSpace(space.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOwnerCredentialAsync(User user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_authOptions.SeedOwnerPassword))
        {
            return;
        }

        var existingCredential = await _dbContext.UserCredentials
            .FirstOrDefaultAsync(credential => credential.UserId == user.Id, cancellationToken);

        if (existingCredential is not null)
        {
            return;
        }

        var passwordHash = _passwordHashService.HashPassword(user, _authOptions.SeedOwnerPassword);
        await _dbContext.UserCredentials.AddAsync(new UserCredential(user.Id, passwordHash), cancellationToken);
    }

    private async Task<User> EnsureUserAsync(CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == SeedDataIds.DefaultUserId, cancellationToken);

        if (user is not null)
        {
            return user;
        }

        user = new User("Northstar Owner", "owner@northstar.local", SeedDataIds.DefaultUserId);
        await _dbContext.Users.AddAsync(user, cancellationToken);
        return user;
    }

    private async Task<Organization> EnsureOrganizationAsync(CancellationToken cancellationToken)
    {
        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(organization => organization.Slug == SeedDataIds.OrganizationSlug, cancellationToken);

        if (organization is not null)
        {
            return organization;
        }

        organization = new Organization("Northstar", SeedDataIds.OrganizationSlug, SeedDataIds.OrganizationId);
        await _dbContext.Organizations.AddAsync(organization, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return organization;
    }

    private async Task<Workspace> EnsureWorkspaceAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var workspace = await _dbContext.Workspaces
            .FirstOrDefaultAsync(workspace => workspace.Slug == SeedDataIds.WorkspaceSlug, cancellationToken);

        if (workspace is not null)
        {
            if (workspace.OrganizationId != organizationId)
            {
                workspace.SetOrganization(organizationId);
            }

            return workspace;
        }

        workspace = new Workspace(
            "Northstar",
            SeedDataIds.WorkspaceSlug,
            userId,
            SeedDataIds.WorkspaceId,
            organizationId);
        await _dbContext.Workspaces.AddAsync(workspace, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return workspace;
    }

    private async Task<Space> EnsureSpaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var space = await _dbContext.Spaces
            .FirstOrDefaultAsync(space => space.WorkspaceId == workspaceId && space.Slug == "atlas-library", cancellationToken);

        if (space is not null)
        {
            return space;
        }

        space = new Space(workspaceId, "Atlas Library", "atlas-library", userId, SeedDataIds.SpaceId);
        await _dbContext.Spaces.AddAsync(space, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return space;
    }

    private async Task EnsureWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.WorkspaceMembers
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken);

        if (!exists)
        {
            await _dbContext.WorkspaceMembers.AddAsync(
                new WorkspaceMember(workspaceId, userId, WorkspaceMemberRole.Owner),
                cancellationToken);
        }
    }

    private async Task EnsureCollectionsAsync(Guid workspaceId, Guid spaceId, Guid userId, CancellationToken cancellationToken)
    {
        foreach (var collection in Collections)
        {
            var exists = await _dbContext.Collections
                .AnyAsync(existing => existing.WorkspaceId == workspaceId && existing.SpaceId == spaceId && existing.Slug == collection.Slug, cancellationToken);

            if (exists)
            {
                continue;
            }

            await _dbContext.Collections.AddAsync(
                new Collection(
                    workspaceId,
                    spaceId,
                    collection.Title,
                    createdBy: userId,
                    id: collection.Id,
                    slug: collection.Slug,
                    sortOrder: collection.SortOrder),
                cancellationToken);
        }
    }

    private async Task EnsureDocumentsAsync(Guid workspaceId, Guid spaceId, Guid userId, CancellationToken cancellationToken)
    {
        foreach (var seedDocument in Documents)
        {
            var document = await _dbContext.Documents
                .FirstOrDefaultAsync(existing => existing.WorkspaceId == workspaceId && existing.Slug == seedDocument.Slug, cancellationToken);

            if (document is null)
            {
                document = new Document(
                    workspaceId,
                    spaceId,
                    seedDocument.CollectionId,
                    seedDocument.Title,
                    userId,
                    userId,
                    seedDocument.Id,
                    seedDocument.Slug,
                    seedDocument.SortOrder);

                await _dbContext.Documents.AddAsync(document, cancellationToken);
            }

            var hasDraft = await _dbContext.DocumentDrafts
                .FirstOrDefaultAsync(draft => draft.DocumentId == document.Id, cancellationToken);

            if (hasDraft is null)
            {
                hasDraft = new DocumentDraft(document.Id, workspaceId, seedDocument.ContentJson, userId);
                ApplyAnalyzedContent(hasDraft, seedDocument.ContentJson, userId);
                await _dbContext.DocumentDrafts.AddAsync(hasDraft, cancellationToken);
            }
            else if (string.IsNullOrWhiteSpace(hasDraft.TextContent))
            {
                ApplyAnalyzedContent(hasDraft, hasDraft.Content, userId);
            }

            await EnsureDocumentTagsAsync(workspaceId, document.Id, seedDocument.Tags, userId, cancellationToken);
            await EnsureInitialVersionAsync(
                document,
                hasDraft,
                userId,
                SeedDataIds.InitialVersionIdForDocument(seedDocument.Id),
                cancellationToken);
            await EnsureCreatedActivityAsync(
                document,
                userId,
                SeedDataIds.CreatedActivityIdForDocument(seedDocument.Id),
                cancellationToken);
            await EnsureSearchIndexAsync(document, hasDraft, cancellationToken);
        }
    }

    private async Task EnsureDocumentTagsAsync(
        Guid workspaceId,
        Guid documentId,
        IReadOnlyList<string> tagNames,
        Guid userId,
        CancellationToken cancellationToken)
    {
        foreach (var tagName in tagNames)
        {
            var slug = SlugNormalizer.Normalize(tagName);
            var tag = await _dbContext.Tags
                .FirstOrDefaultAsync(existing => existing.WorkspaceId == workspaceId && existing.Slug == slug, cancellationToken);

            if (tag is null)
            {
                tag = new Tag(workspaceId, tagName, slug, userId);
                await _dbContext.Tags.AddAsync(tag, cancellationToken);
            }

            var exists = await _dbContext.DocumentTags
                .AnyAsync(documentTag => documentTag.DocumentId == documentId && documentTag.TagId == tag.Id, cancellationToken);

            if (!exists)
            {
                await _dbContext.DocumentTags.AddAsync(new DocumentTag(workspaceId, documentId, tag.Id), cancellationToken);
            }
        }
    }

    private async Task EnsureInitialVersionAsync(
        Document document,
        DocumentDraft draft,
        Guid userId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.DocumentVersions
            .AnyAsync(version => version.DocumentId == document.Id && version.VersionNo == 1, cancellationToken);

        if (exists)
        {
            return;
        }

        await _dbContext.DocumentVersions.AddAsync(
            new DocumentVersion(
                document.WorkspaceId,
                document.Id,
                1,
                "1.0",
                DocumentVersionType.System,
                draft.Content,
                draft.TextContent,
                draft.Outline,
                draft.WordCount,
                userId,
                versionId),
            cancellationToken);
    }

    private async Task EnsureCreatedActivityAsync(
        Document document,
        Guid userId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ActivityEvents
            .AnyAsync(activity => activity.Id == activityId, cancellationToken);

        if (exists)
        {
            return;
        }

        await _dbContext.ActivityEvents.AddAsync(
            new ActivityEvent(
                document.WorkspaceId,
                userId,
                ActivityEntityTypes.Document,
                document.Id,
                ActivityActions.DocumentCreated,
                "Created document.",
                "{}",
                activityId),
            cancellationToken);
    }

    private async Task EnsureSearchIndexAsync(
        Document document,
        DocumentDraft draft,
        CancellationToken cancellationToken)
    {
        var index = await _dbContext.DocumentSearchIndexes
            .FirstOrDefaultAsync(index => index.DocumentId == document.Id, cancellationToken);

        if (index is null)
        {
            await _dbContext.DocumentSearchIndexes.AddAsync(
                new DocumentSearchIndex(
                    document.Id,
                    document.WorkspaceId,
                    document.SpaceId,
                    document.Title,
                    draft.TextContent),
                cancellationToken);
            return;
        }

        index.Update(document.Title, draft.TextContent, document.SpaceId);
    }

    private async Task EnsureSeedLinksAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await EnsureDocumentLinkAsync(
            workspaceId,
            SeedDataIds.MissionDocumentId,
            SeedDataIds.PrinciplesDocumentId,
            "Our Principles",
            SeedDataIds.MissionToPrinciplesLinkId,
            userId,
            cancellationToken);
        await EnsureDocumentLinkAsync(
            workspaceId,
            SeedDataIds.OperatingSystemDocumentId,
            SeedDataIds.MissionDocumentId,
            "Mission & Vision",
            SeedDataIds.OperatingToMissionLinkId,
            userId,
            cancellationToken);
    }

    private async Task EnsureDocumentLinkAsync(
        Guid workspaceId,
        Guid sourceDocumentId,
        Guid targetDocumentId,
        string anchorText,
        Guid linkId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var documentsExist = await _dbContext.Documents
            .CountAsync(document =>
                document.WorkspaceId == workspaceId &&
                document.DeletedAt == null &&
                (document.Id == sourceDocumentId || document.Id == targetDocumentId),
                cancellationToken) == 2;

        if (!documentsExist)
        {
            return;
        }

        var exists = await _dbContext.DocumentLinks
            .AnyAsync(link => link.WorkspaceId == workspaceId &&
                link.SourceDocumentId == sourceDocumentId &&
                link.TargetDocumentId == targetDocumentId,
                cancellationToken);

        if (exists)
        {
            return;
        }

        await _dbContext.DocumentLinks.AddAsync(
            new DocumentLink(
                workspaceId,
                sourceDocumentId,
                targetDocumentId,
                targetUrl: null,
                DocumentLinkType.Reference,
                anchorText,
                createdBy: userId,
                id: linkId),
            cancellationToken);
    }

    private static void ApplyAnalyzedContent(DocumentDraft draft, string contentJson, Guid userId)
    {
        using var document = System.Text.Json.JsonDocument.Parse(contentJson);
        var metadata = DocumentContentAnalyzer.Analyze(document.RootElement);
        draft.UpdateContent(
            metadata.ContentJson,
            metadata.TextContent,
            metadata.OutlineJson,
            metadata.WordCount,
            metadata.ContentHash,
            userId);
    }

    private static string TiptapParagraph(string text)
    {
        return $$"""
        {
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                {
                  "type": "text",
                  "text": "{{text}}"
                }
              ]
            }
          ]
        }
        """;
    }

    private static string TiptapParagraphWithDocumentLink(
        string textBefore,
        string linkText,
        Guid documentId,
        string textAfter)
    {
        return $$"""
        {
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                {
                  "type": "text",
                  "text": "{{textBefore}}"
                },
                {
                  "type": "text",
                  "text": "{{linkText}}",
                  "marks": [
                    {
                      "type": "link",
                      "attrs": {
                        "href": "/documents/{{documentId}}"
                      }
                    }
                  ]
                },
                {
                  "type": "text",
                  "text": "{{textAfter}}"
                }
              ]
            }
          ]
        }
        """;
    }

    private sealed record SeedCollection(Guid Id, string Title, string Slug, decimal SortOrder);

    private sealed record SeedDocument(
        Guid Id,
        Guid CollectionId,
        string Title,
        string Slug,
        decimal SortOrder,
        string ContentJson,
        IReadOnlyList<string> Tags);
}
