namespace Northstar.Domain.Security;

public static class PermissionNotificationTypes
{
    public const string AccessRequestCreated = "access_request.created";
    public const string AccessRequestApproved = "access_request.approved";
    public const string AccessRequestDenied = "access_request.denied";
    public const string GrantCreated = "permission.grant_created";
    public const string GrantUpdated = "permission.grant_updated";
    public const string GrantRevoked = "permission.grant_revoked";
    public const string GrantExpiring = "permission.grant_expiring";
    public const string GrantExpired = "permission.grant_expired";
    public const string GroupMemberAdded = "group.member_added";
    public const string GroupMemberRemoved = "group.member_removed";
    public const string GroupMemberExpiring = "group.member_expiring";
    public const string GroupMemberExpired = "group.member_expired";
    public const string ShareLinkCreated = "share_link.created";
    public const string ShareLinkRevoked = "share_link.revoked";
    public const string EmailInviteCreated = "email_invite.created";
    public const string EmailInviteAccepted = "email_invite.accepted";
    public const string EmailInviteRevoked = "email_invite.revoked";
    public const string EmailInviteDeliveryFailed = "email_invite.delivery_failed";

    public static bool IsSupported(string? type)
    {
        return type is AccessRequestCreated
            or AccessRequestApproved
            or AccessRequestDenied
            or GrantCreated
            or GrantUpdated
            or GrantRevoked
            or GrantExpiring
            or GrantExpired
            or GroupMemberAdded
            or GroupMemberRemoved
            or GroupMemberExpiring
            or GroupMemberExpired
            or ShareLinkCreated
            or ShareLinkRevoked
            or EmailInviteCreated
            or EmailInviteAccepted
            or EmailInviteRevoked
            or EmailInviteDeliveryFailed;
    }
}
