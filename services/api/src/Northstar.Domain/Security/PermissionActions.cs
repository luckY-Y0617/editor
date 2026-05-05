namespace Northstar.Domain.Security;

public static class PermissionActions
{
    public const string WorkspaceView = "workspace.view";
    public const string WorkspaceManageSettings = "workspace.manage_settings";
    public const string WorkspaceManageMembers = "workspace.manage_members";
    public const string WorkspaceManagePermissions = "workspace.manage_permissions";
    public const string WorkspaceViewAudit = "workspace.view_audit";

    public const string CollectionView = "collection.view";
    public const string CollectionEdit = "collection.edit";
    public const string CollectionCreateDocument = "collection.create_document";
    public const string CollectionShare = "collection.share";
    public const string CollectionManagePermissions = "collection.manage_permissions";
    public const string CollectionArchive = "collection.archive";
    public const string CollectionDelete = "collection.delete";

    public const string DocumentView = "document.view";
    public const string DocumentComment = "document.comment";
    public const string DocumentEdit = "document.edit";
    public const string DocumentShare = "document.share";
    public const string DocumentManagePermissions = "document.manage_permissions";
    public const string DocumentArchive = "document.archive";
    public const string DocumentRestore = "document.restore";
    public const string DocumentDelete = "document.delete";

    public const string VersionView = "version.view";
    public const string VersionCreate = "version.create";
    public const string VersionRestore = "version.restore";

    public const string FileUpload = "file.upload";
    public const string FileDownload = "file.download";
    public const string FileDelete = "file.delete";

    public const string AttachmentView = "attachment.view";
    public const string AttachmentCreate = "attachment.create";
    public const string AttachmentDelete = "attachment.delete";

    public const string SearchQuery = "search.query";
    public const string ActivityView = "activity.view";
}
