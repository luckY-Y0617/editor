using Microsoft.Extensions.DependencyInjection;
using Northstar.Application.Bootstrap;
using Northstar.Application.Files;
using Northstar.Application.Knowledge;
using Northstar.Application.Organizations;
using Northstar.Application.Security;
using Northstar.Application.Workspaces;

namespace Northstar.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBootstrapService, BootstrapService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IKnowledgeMapService, KnowledgeMapService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentContextService, DocumentContextService>();
        services.AddScoped<IDocumentActivityService, DocumentActivityService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ISpaceTransferService, SpaceTransferService>();
        services.AddScoped<IOrganizationSettingsQueryService, OrganizationSettingsQueryService>();
        services.AddScoped<IOrganizationSettingsCommandService, OrganizationSettingsCommandService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthSecurityStateService, AuthSecurityStateService>();
        services.AddScoped<IAuthMfaService, AuthMfaService>();
        services.AddScoped<IAuthStepUpService, AuthStepUpService>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IPermissionCatalog, PermissionCatalog>();
        services.AddScoped<IEffectivePermissionService, EffectivePermissionService>();
        services.AddScoped<IEffectivePermissionQueryService, EffectivePermissionQueryService>();
        services.AddScoped<IScopedResourceAccessService, ScopedResourceAccessService>();
        services.AddScoped<IPermissionAuditService, PermissionAuditService>();
        services.AddScoped<IPermissionNotificationService, PermissionNotificationService>();
        services.AddScoped<IPermissionNotificationPreferenceService, PermissionNotificationPreferenceService>();
        services.AddScoped<IPermissionNotificationFanoutService, PermissionNotificationFanoutService>();
        services.AddScoped<IAccessRequestService, AccessRequestService>();
        services.AddScoped<IResourcePermissionManagementService, ResourcePermissionManagementService>();
        services.AddSingleton<IShareLinkTokenService, ShareLinkTokenService>();
        services.AddScoped<IShareLinkService, ShareLinkService>();
        services.AddScoped<IShareLinkAccessAuditService, ShareLinkAccessAuditService>();
        services.AddScoped<IEmailInviteService, EmailInviteService>();
        services.AddScoped<IEmailInviteDeliveryOutboxProcessor, EmailInviteDeliveryOutboxProcessor>();
        services.AddScoped<IDocumentPermissionFilterService, DocumentPermissionFilterService>();
        services.AddScoped<IWorkspaceAccessService, WorkspaceAccessService>();
        services.AddScoped<IWorkspaceAgendaService, WorkspaceAgendaService>();
        services.AddScoped<IWorkspaceMembersService, WorkspaceMembersService>();
        services.AddScoped<IWorkspaceGroupService, WorkspaceGroupService>();
        services.AddScoped<IIamSyncService, IamSyncService>();
        services.AddScoped<IScimService, ScimService>();
        services.AddScoped<IScimTokenService, ScimTokenService>();
        services.AddSingleton<IDocumentLinkExtractor, TiptapDocumentLinkExtractor>();
        services.AddScoped<IUploadSessionService, UploadSessionService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IDocumentAttachmentService, DocumentAttachmentService>();
        services.AddScoped<IFileReferenceService, FileReferenceService>();
        services.AddSingleton<IFileReferenceExtractor, FileReferenceExtractor>();

        return services;
    }
}
