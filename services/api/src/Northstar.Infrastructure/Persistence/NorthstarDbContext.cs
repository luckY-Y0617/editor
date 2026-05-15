using Microsoft.EntityFrameworkCore;
using Northstar.Application.Common;
using Northstar.Domain.Files;
using Northstar.Domain.Knowledge.Activity;
using Northstar.Domain.Knowledge.Comments;
using Northstar.Domain.Knowledge.Collections;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Links;
using Northstar.Domain.Knowledge.Spaces;
using Northstar.Domain.Knowledge.Tags;
using Northstar.Domain.Knowledge.Versions;
using Northstar.Domain.Organizations;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Search;

namespace Northstar.Infrastructure.Persistence;

public sealed class NorthstarDbContext : DbContext, IUnitOfWork
{
    public NorthstarDbContext(DbContextOptions<NorthstarDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuthEvent> AuthEvents => Set<AuthEvent>();
    public DbSet<UserMfaMethod> UserMfaMethods => Set<UserMfaMethod>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentDraft> DocumentDrafts => Set<DocumentDraft>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentLink> DocumentLinks => Set<DocumentLink>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
    public DbSet<DocumentSearchIndex> DocumentSearchIndexes => Set<DocumentSearchIndex>();
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();
    public DbSet<DocumentAttachment> DocumentAttachments => Set<DocumentAttachment>();
    public DbSet<FileOutboxEvent> FileOutboxEvents => Set<FileOutboxEvent>();
    public DbSet<CommentThread> CommentThreads => Set<CommentThread>();
    public DbSet<CommentMessage> CommentMessages => Set<CommentMessage>();
    public DbSet<ResourceAccessPolicy> ResourceAccessPolicies => Set<ResourceAccessPolicy>();
    public DbSet<ResourceAccessGrant> ResourceAccessGrants => Set<ResourceAccessGrant>();
    public DbSet<PermissionAuditEvent> PermissionAuditEvents => Set<PermissionAuditEvent>();
    public DbSet<WorkspaceGroup> WorkspaceGroups => Set<WorkspaceGroup>();
    public DbSet<WorkspaceGroupMember> WorkspaceGroupMembers => Set<WorkspaceGroupMember>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<PermissionNotification> PermissionNotifications => Set<PermissionNotification>();
    public DbSet<PermissionNotificationPreference> PermissionNotificationPreferences => Set<PermissionNotificationPreference>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<ShareLinkAccessEvent> ShareLinkAccessEvents => Set<ShareLinkAccessEvent>();
    public DbSet<ShareLinkAccessStats> ShareLinkAccessStats => Set<ShareLinkAccessStats>();
    public DbSet<ResourceEmailInvite> ResourceEmailInvites => Set<ResourceEmailInvite>();
    public DbSet<EmailInviteDeliveryOutboxItem> EmailInviteDeliveryOutbox => Set<EmailInviteDeliveryOutboxItem>();
    public DbSet<ScimToken> ScimTokens => Set<ScimToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NorthstarDbContext).Assembly);
    }
}
