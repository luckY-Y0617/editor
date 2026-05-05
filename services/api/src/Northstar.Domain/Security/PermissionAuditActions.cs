namespace Northstar.Domain.Security;

public static class PermissionAuditActions
{
    public const string PolicyUpdated = "permission.policy.updated";
    public const string GrantCreated = "grant.created";
    public const string GrantUpdated = "grant.updated";
    public const string GrantRevoked = "grant.revoked";
    public const string GroupCreated = "group.created";
    public const string GroupUpdated = "group.updated";
    public const string GroupArchived = "group.archived";
    public const string GroupMemberAdded = "group.member_added";
    public const string GroupMemberRemoved = "group.member_removed";
    public const string AccessRequestCreated = "access_request.created";
    public const string AccessRequestApproved = "access_request.approved";
    public const string AccessRequestDenied = "access_request.denied";
    public const string AccessRequestCancelled = "access_request.cancelled";
    public const string ShareLinkCreated = "share_link.created";
    public const string ShareLinkRevoked = "share_link.revoked";
    public const string EmailInviteCreated = "email_invite.created";
    public const string EmailInviteAccepted = "email_invite.accepted";
    public const string EmailInviteRevoked = "email_invite.revoked";
    public const string EmailInviteExpired = "email_invite.expired";
    public const string IamUserMapped = "iam.user_mapped";
    public const string IamGroupSynced = "iam.group_synced";
    public const string IamGroupMemberAdded = "iam.group_member_added";
    public const string IamGroupMemberRemoved = "iam.group_member_removed";
}
