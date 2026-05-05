using Northstar.Domain.Security;
using Northstar.Domain.Shared;

namespace Northstar.Domain.Tests;

public sealed class ResourceAccessPolicyTests
{
    [Theory]
    [InlineData(InheritanceModes.Inherit)]
    [InlineData(InheritanceModes.Restricted)]
    public void Constructor_AcceptsSupportedInheritanceModes(string inheritanceMode)
    {
        var policy = new ResourceAccessPolicy(
            Guid.NewGuid(),
            ResourceTypes.Document,
            Guid.NewGuid(),
            inheritanceMode);

        Assert.Equal(inheritanceMode, policy.InheritanceMode);
        Assert.Equal(LinkModes.Disabled, policy.LinkMode);
    }

    [Fact]
    public void Constructor_RejectsInvalidInheritanceMode()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new ResourceAccessPolicy(
                Guid.NewGuid(),
                ResourceTypes.Document,
                Guid.NewGuid(),
                "private"));

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }

    [Theory]
    [InlineData(LinkModes.Disabled)]
    [InlineData(LinkModes.Internal)]
    [InlineData(LinkModes.External)]
    [InlineData(LinkModes.Public)]
    public void SetLinkMode_AcceptsSupportedModes(string linkMode)
    {
        var policy = new ResourceAccessPolicy(
            Guid.NewGuid(),
            ResourceTypes.Collection,
            Guid.NewGuid());

        policy.SetLinkMode(linkMode, PermissionRole.Viewer);

        Assert.Equal(linkMode, policy.LinkMode);
        Assert.Equal(PermissionRole.Viewer, policy.DefaultLinkRole);
    }
}

public sealed class ResourceEmailInviteTests
{
    [Fact]
    public void Accept_TransitionsPendingInvite()
    {
        var invite = CreateInvite();
        var userId = Guid.NewGuid();

        invite.Accept(userId);

        Assert.Equal(EmailInviteStatuses.Accepted, invite.Status);
        Assert.Equal(userId, invite.AcceptedBy);
        Assert.NotNull(invite.AcceptedAt);
    }

    [Fact]
    public void Revoke_RemovesAcceptedInviteAccess()
    {
        var invite = CreateInvite();
        var userId = Guid.NewGuid();
        invite.Accept(userId);

        invite.Revoke(userId);

        Assert.Equal(EmailInviteStatuses.Revoked, invite.Status);
        Assert.False(invite.IsAcceptedActive(DateTimeOffset.UtcNow));
    }

    private static ResourceEmailInvite CreateInvite()
    {
        return new ResourceEmailInvite(
            Guid.NewGuid(),
            ResourceTypes.Document,
            Guid.NewGuid(),
            "Person@Example.Test",
            "hash",
            PermissionRole.Viewer,
            DateTimeOffset.UtcNow.AddHours(1));
    }
}

public sealed class ResourceAccessGrantTests
{
    [Theory]
    [InlineData(PermissionRole.Viewer)]
    [InlineData(PermissionRole.Editor)]
    [InlineData(PermissionRole.Commenter)]
    public void Constructor_AcceptsScopedRoles(string role)
    {
        var grant = new ResourceAccessGrant(
            Guid.NewGuid(),
            ResourceTypes.Document,
            Guid.NewGuid(),
            SubjectTypes.User,
            Guid.NewGuid(),
            role);

        Assert.Equal(role, grant.RoleKey);
    }

    [Fact]
    public void Constructor_RejectsUnknownRole()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new ResourceAccessGrant(
                Guid.NewGuid(),
                ResourceTypes.Document,
                Guid.NewGuid(),
                SubjectTypes.User,
                Guid.NewGuid(),
                "auditor"));

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }

    [Fact]
    public void IsActive_ReturnsFalseForRevokedGrant()
    {
        var grant = CreateGrant();

        grant.Revoke(Guid.NewGuid(), "done");

        Assert.False(grant.IsActive(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsActive_ReturnsFalseForExpiredGrant()
    {
        var grant = CreateGrant(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.False(grant.IsActive(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsActive_ReturnsTrueForUnexpiredGrant()
    {
        var grant = CreateGrant(expiresAt: DateTimeOffset.UtcNow.AddMinutes(5));

        Assert.True(grant.IsActive(DateTimeOffset.UtcNow));
    }

    private static ResourceAccessGrant CreateGrant(DateTimeOffset? expiresAt = null)
    {
        return new ResourceAccessGrant(
            Guid.NewGuid(),
            ResourceTypes.Document,
            Guid.NewGuid(),
            SubjectTypes.User,
            Guid.NewGuid(),
            PermissionRole.Viewer,
            expiresAt: expiresAt);
    }
}

public sealed class WorkspaceGroupTests
{
    [Theory]
    [InlineData(GroupTypes.Static)]
    [InlineData(GroupTypes.Dynamic)]
    public void Constructor_AcceptsSupportedGroupTypes(string type)
    {
        var group = new WorkspaceGroup(Guid.NewGuid(), "Design", type: type);

        Assert.Equal(type, group.Type);
        Assert.False(group.IsArchived);
    }

    [Fact]
    public void Constructor_RejectsInvalidGroupType()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new WorkspaceGroup(Guid.NewGuid(), "Design", type: "external"));

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }

    [Fact]
    public void Archive_MarksGroupArchivedAndPreventsUpdate()
    {
        var group = new WorkspaceGroup(Guid.NewGuid(), "Design");

        group.Archive();

        Assert.True(group.IsArchived);
        Assert.NotNull(group.ArchivedAt);
        Assert.Throws<DomainException>(() => group.Update("New name", null));
    }
}

public sealed class WorkspaceGroupMemberTests
{
    [Fact]
    public void IsActive_ReturnsTrueForCurrentMembership()
    {
        var member = new WorkspaceGroupMember(Guid.NewGuid(), Guid.NewGuid(), expiresAt: DateTimeOffset.UtcNow.AddMinutes(5));

        Assert.True(member.IsActive(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsActive_ReturnsFalseForExpiredMembership()
    {
        var member = new WorkspaceGroupMember(Guid.NewGuid(), Guid.NewGuid(), expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.False(member.IsActive(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsActive_ReturnsFalseForRemovedMembership()
    {
        var member = new WorkspaceGroupMember(Guid.NewGuid(), Guid.NewGuid());

        member.Remove();

        Assert.False(member.IsActive(DateTimeOffset.UtcNow));
    }
}

public sealed class SubjectTypesTests
{
    [Theory]
    [InlineData(SubjectTypes.User)]
    [InlineData(SubjectTypes.Group)]
    public void IsSupported_AcceptsSupportedSubjectTypes(string subjectType)
    {
        Assert.True(SubjectTypes.IsSupported(subjectType));
    }

    [Fact]
    public void IsSupported_RejectsUnsupportedSubjectType()
    {
        Assert.False(SubjectTypes.IsSupported("team"));
    }
}

public sealed class AccessRequestTests
{
    [Fact]
    public void Constructor_CreatesPendingRequestAndNormalizesValues()
    {
        var request = new AccessRequest(
            Guid.NewGuid(),
            "DOCUMENT",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "USER",
            Guid.NewGuid(),
            "COMMENTER",
            "  please  ");

        Assert.Equal(ResourceTypes.Document, request.ResourceType);
        Assert.Equal(SubjectTypes.User, request.SubjectType);
        Assert.Equal(PermissionRole.Commenter, request.RequestedRole);
        Assert.Equal(AccessRequestStatus.Pending, request.Status);
        Assert.Equal("please", request.Reason);
    }

    [Fact]
    public void Approve_TransitionsPendingRequestAndRecordsGrant()
    {
        var request = CreateAccessRequest();
        var actorId = Guid.NewGuid();
        var grantId = Guid.NewGuid();

        request.Approve(actorId, grantId, "ok");

        Assert.Equal(AccessRequestStatus.Approved, request.Status);
        Assert.Equal(actorId, request.DecidedBy);
        Assert.Equal(grantId, request.ResultingGrantId);
        Assert.Equal("ok", request.DecisionReason);
        Assert.NotNull(request.DecidedAt);
    }

    [Theory]
    [InlineData("deny")]
    [InlineData("cancel")]
    public void NonPendingRequest_CannotTransitionAgain(string secondTransition)
    {
        var request = CreateAccessRequest();
        request.Approve(Guid.NewGuid(), Guid.NewGuid(), null);

        var exception = Assert.Throws<DomainException>(() =>
        {
            if (secondTransition == "deny")
            {
                request.Deny(Guid.NewGuid(), "no");
            }
            else
            {
                request.Cancel(Guid.NewGuid(), "withdrawn");
            }
        });

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }

    [Fact]
    public void Constructor_RejectsInvalidStatusInputs()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new AccessRequest(
                Guid.NewGuid(),
                ResourceTypes.Workspace,
                Guid.NewGuid(),
                Guid.NewGuid(),
                SubjectTypes.User,
                Guid.NewGuid(),
                PermissionRole.Viewer));

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }

    private static AccessRequest CreateAccessRequest()
    {
        return new AccessRequest(
            Guid.NewGuid(),
            ResourceTypes.Document,
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubjectTypes.User,
            Guid.NewGuid(),
            PermissionRole.Viewer);
    }
}

public sealed class PermissionNotificationTests
{
    [Fact]
    public void Constructor_AcceptsSupportedNotificationTypes()
    {
        var notification = new PermissionNotification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PermissionNotificationTypes.AccessRequestCreated,
            "Access request pending",
            resourceType: ResourceTypes.Document,
            resourceId: Guid.NewGuid());

        Assert.Equal(PermissionNotificationTypes.AccessRequestCreated, notification.Type);
        Assert.Equal(ResourceTypes.Document, notification.ResourceType);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public void MarkRead_IsIdempotent()
    {
        var notification = new PermissionNotification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PermissionNotificationTypes.GrantCreated,
            "Permission granted");

        notification.MarkRead();
        var firstReadAt = notification.ReadAt;
        notification.MarkRead();

        Assert.NotNull(firstReadAt);
        Assert.Equal(firstReadAt, notification.ReadAt);
    }

    [Fact]
    public void Constructor_RejectsInvalidNotificationType()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new PermissionNotification(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "permission.email_digest",
                "Digest"));

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }
}

public sealed class PermissionNotificationPreferenceTests
{
    [Fact]
    public void Constructor_RejectsWatchedAndMutedTogether()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new PermissionNotificationPreference(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ResourceTypes.Document,
                Guid.NewGuid(),
                watched: true,
                muted: true));

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }

    [Fact]
    public void Constructor_AcceptsWorkspacePreference()
    {
        var preference = new PermissionNotificationPreference(
            Guid.NewGuid(),
            Guid.NewGuid(),
            resourceType: null,
            resourceId: null,
            watched: true,
            muted: false);

        Assert.True(preference.Watched);
        Assert.False(preference.Muted);
        Assert.Null(preference.ResourceType);
        Assert.Null(preference.ResourceId);
    }
}
