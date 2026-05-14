using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Northstar.Application.Common;
using Northstar.Contracts.Auth;
using Northstar.Contracts.Files;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Files;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Api.Tests;

public sealed class PostgreSqlSmokeTests
{
    private const string OwnerEmail = "owner@northstar.local";
    private const string OwnerPassword = "Northstar.test.123!";

    [Fact]
    public async Task PostgreSqlSmoke_MigrationSeedAndCoreApis()
    {
        var connectionString = Environment.GetEnvironmentVariable("NORTHSTAR_POSTGRES_SMOKE_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var dbOptions = new DbContextOptionsBuilder<NorthstarDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using (var dbContext = new NorthstarDbContext(dbOptions))
        {
            await dbContext.Database.EnsureDeletedAsync();
        }

        using var factory = new PostgreSqlApiFactory(connectionString);
        var client = factory.CreateClient();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<INorthstarDataSeeder>();
            await seeder.SeedAsync();
            await seeder.SeedAsync();
            var dbContext = scope.ServiceProvider.GetRequiredService<NorthstarDbContext>();
            var pgTrgmEnabled = await dbContext.Database.SqlQueryRaw<bool>(
                "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm') AS \"Value\"")
                .SingleAsync();
            var searchVectorExists = await dbContext.Database.SqlQueryRaw<bool>(
                "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'document_search_index' AND column_name = 'search_vector') AS \"Value\"")
                .SingleAsync();
            var fullTextMatches = await dbContext.Database.SqlQueryRaw<int>(
                "SELECT count(*)::int AS \"Value\" FROM document_search_index WHERE search_vector @@ websearch_to_tsquery('simple', 'principles')")
                .SingleAsync();

            Assert.True(pgTrgmEnabled);
            Assert.True(searchVectorExists);
            Assert.True(fullTextMatches > 0);
        }

        await LoginOwnerAsync(client);
        var bootstrap = await client.GetFromJsonAsync<BootstrapResponse>("/api/v1/bootstrap");
        Assert.NotNull(bootstrap);

        var documentId = bootstrap.ActiveDocumentId;
        var context = await client.GetFromJsonAsync<DocumentContextResponse>($"/api/v1/documents/{documentId}/context");
        var activity = await client.GetFromJsonAsync<DocumentActivityResponse>($"/api/v1/documents/{documentId}/activity");
        var search = await client.GetFromJsonAsync<SearchResponse>($"/api/v1/search?q=principles&spaceId={bootstrap.ActiveSpaceId}");
        var document = await client.GetFromJsonAsync<GetDocumentResponse>($"/api/v1/documents/{documentId}");
        var content = JsonSerializer.Deserialize<JsonElement>("""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"postgres smoke searchable body"}]}]}""");
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/v1/documents/{documentId}",
            new UpdateDocumentRequest(document!.Document.Revision, null, content, null));
        patchResponse.EnsureSuccessStatusCode();
        var updatedSearch = await client.GetFromJsonAsync<SearchResponse>($"/api/v1/search?q=postgres%20smoke&spaceId={bootstrap.ActiveSpaceId}");
        var mission = bootstrap.Documents.Single(document => document.Title == "Mission & Vision");
        var operatingSystem = bootstrap.Documents.Single(document => document.Title == "Operating System");
        var archiveResponse = await client.PatchAsync($"/api/v1/documents/{mission.Id}/archive", null);
        archiveResponse.EnsureSuccessStatusCode();
        var archivedSearch = await client.GetFromJsonAsync<SearchResponse>($"/api/v1/search?q=Mission&spaceId={bootstrap.ActiveSpaceId}");
        var deleteResponse = await client.DeleteAsync($"/api/v1/documents/{operatingSystem.Id}");
        deleteResponse.EnsureSuccessStatusCode();
        var deletedSearch = await client.GetFromJsonAsync<SearchResponse>($"/api/v1/search?q=Operating&spaceId={bootstrap.ActiveSpaceId}");
        var export = await client.GetFromJsonAsync<ExportSpaceResponse>($"/api/v1/spaces/{bootstrap.ActiveSpaceId}/export");
        var importMarker = $"postgres-import-{Guid.NewGuid():N}";
        var importResponse = await client.PostAsJsonAsync(
            $"/api/v1/spaces/{bootstrap.ActiveSpaceId}/import",
            new ImportSpaceRequest(
                "append",
                null,
                [
                    new DocumentImportDto(
                        Guid.NewGuid().ToString(),
                        bootstrap.Folders[0].Id,
                        "PostgreSQL Imported",
                        "draft",
                        null,
                        ["postgres"],
                        JsonSerializer.Deserialize<JsonElement>($$"""
                        {
                          "type": "doc",
                          "content": [
                            {
                              "type": "paragraph",
                              "content": [
                                {
                                  "type": "text",
                                  "text": "{{importMarker}}"
                                }
                              ]
                            }
                          ]
                        }
                        """))
                ]));
        importResponse.EnsureSuccessStatusCode();
        var imported = await importResponse.Content.ReadFromJsonAsync<ImportSpaceResponse>();
        var importSearch = await client.GetFromJsonAsync<SearchResponse>($"/api/v1/search?q={importMarker}&spaceId={bootstrap.ActiveSpaceId}");
        var fileBytes = Encoding.UTF8.GetBytes("postgres smoke file content");
        var uploadSessionResponse = await client.PostAsJsonAsync(
            "/api/v1/files/uploads/sessions",
            new CreateUploadSessionRequest(
                $"postgres-smoke-{Guid.NewGuid():N}",
                "postgres-smoke.txt",
                "text/plain",
                fileBytes.LongLength,
                Sha256(fileBytes),
                "postgres-smoke",
                "single",
                null,
                documentId));
        uploadSessionResponse.EnsureSuccessStatusCode();
        var uploadSession = await uploadSessionResponse.Content.ReadFromJsonAsync<CreateUploadSessionResponse>();
        Assert.NotNull(uploadSession);
        using (var uploadContent = new ByteArrayContent(fileBytes))
        {
            var uploadContentResponse = await client.PutAsync(
                $"/api/v1/files/uploads/sessions/{uploadSession.SessionId}/content",
                uploadContent);
            uploadContentResponse.EnsureSuccessStatusCode();
        }

        var completeResponse = await client.PostAsJsonAsync(
            $"/api/v1/files/uploads/sessions/{uploadSession.SessionId}/complete",
            new CompleteUploadSessionRequest());
        completeResponse.EnsureSuccessStatusCode();
        var finalizeResponse = await client.PostAsJsonAsync(
            $"/api/v1/files/uploads/sessions/{uploadSession.SessionId}/finalize",
            new FinalizeUploadSessionRequest(documentId, DocumentAttachmentRelationType.Attachment, null));
        finalizeResponse.EnsureSuccessStatusCode();
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<FinalizeUploadSessionResponse>();
        Assert.NotNull(finalized);
        var attachments = await client.GetFromJsonAsync<DocumentAttachmentsResponse>(
            $"/api/v1/documents/{documentId}/attachments");
        var fileContent = await client.GetStringAsync($"/api/v1/files/{finalized.File.Id}/content");
        var deleteWithAttachmentResponse = await client.DeleteAsync($"/api/v1/files/{finalized.File.Id}");
        var deleteAttachmentResponse = await client.DeleteAsync(
            $"/api/v1/documents/{documentId}/attachments/{finalized.Attachment!.Id}");
        deleteAttachmentResponse.EnsureSuccessStatusCode();
        var deleteFileResponse = await client.DeleteAsync($"/api/v1/files/{finalized.File.Id}");

        Assert.NotNull(context);
        Assert.NotEmpty(context.VersionTrail);
        Assert.NotNull(activity);
        Assert.NotEmpty(activity.Items);
        Assert.NotNull(search);
        Assert.NotEmpty(search.Results);
        Assert.NotNull(updatedSearch);
        Assert.Contains(updatedSearch.Results, result => result.Id == documentId);
        Assert.NotNull(archivedSearch);
        Assert.DoesNotContain(archivedSearch.Results, result => result.Id == mission.Id);
        Assert.NotNull(deletedSearch);
        Assert.DoesNotContain(deletedSearch.Results, result => result.Id == operatingSystem.Id);
        Assert.NotNull(export);
        Assert.Contains(export.Documents, document => document.Id == mission.Id && document.Status == "archived");
        Assert.DoesNotContain(export.Documents, document => document.Id == operatingSystem.Id);
        Assert.NotNull(imported);
        Assert.Contains(imported.Map.Documents, document => document.Title == "PostgreSQL Imported");
        Assert.NotNull(importSearch);
        Assert.Contains(importSearch.Results, result => result.Title == "PostgreSQL Imported");
        Assert.NotNull(attachments);
        Assert.Contains(attachments.Attachments, attachment => attachment.FileId == finalized.File.Id);
        Assert.Equal("postgres smoke file content", fileContent);
        Assert.Equal(HttpStatusCode.Conflict, deleteWithAttachmentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteFileResponse.StatusCode);
    }

    private static async Task LoginOwnerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(OwnerEmail, OwnerPassword));
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed class PostgreSqlApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly string _filesRoot = Path.Combine(
            Path.GetTempPath(),
            $"northstar-postgres-smoke-files-{Guid.NewGuid():N}");

        public PostgreSqlApiFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Northstar"] = _connectionString,
                    ["Auth:SeedOwnerPassword"] = OwnerPassword,
                    ["Auth:Jwt:Issuer"] = "Northstar",
                    ["Auth:Jwt:Audience"] = "Northstar",
                    ["Auth:Jwt:SigningKey"] = "northstar-local-development-signing-key-change-me",
                    ["Auth:Jwt:AccessTokenMinutes"] = "15",
                    ["Auth:Jwt:RefreshTokenDays"] = "14",
                    ["Files:StorageProvider"] = "Local",
                    ["Files:LocalRootPath"] = _filesRoot,
                    ["Files:DefaultBucket"] = "northstar-smoke",
                    ["Files:UploadSessionMinutes"] = "60",
                    ["Files:MaxFileBytes"] = "1048576",
                    ["Files:AllowedMimeTypes:0"] = "text/plain",
                    ["Files:AllowedMimeTypes:1"] = "image/png",
                    ["Files:AllowedMimeTypes:2"] = "image/jpeg",
                    ["Files:AllowedMimeTypes:3"] = "image/webp",
                    ["Files:AllowedMimeTypes:4"] = "application/pdf"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<NorthstarDbContext>>();
                services.AddDbContext<NorthstarDbContext>(options => options.UseNpgsql(_connectionString));
            });
        }
    }
}
