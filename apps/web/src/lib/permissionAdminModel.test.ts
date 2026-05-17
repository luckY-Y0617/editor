import { describe, expect, test } from "../test/harness";
import type { WorkspaceGroupDto, WorkspaceMemberDto } from "./permissionAdminApi";
import {
  getCurrentWorkspaceRole,
  getMemberLifecycleGuard,
  getMemberActionCapability,
  getRoleChangeOptions,
  addExistingUserRoleOptions,
  isSupportedWorkspaceMemberRole,
  getWorkspaceGroupDetailRows,
  getWorkspaceGroupReadOnlyReason,
  getWorkspaceGroupSourceLabel,
  resolvePermissionAdminWorkspace,
  toPermissionMutationError,
} from "./permissionAdminModel";

describe("permissionAdminModel", () => {
  test("derives current workspace role from auth workspaces", () => {
    expect(
      getCurrentWorkspaceRole(
        [
          { id: "workspace-1", name: "One", role: "viewer" },
          { id: "workspace-2", name: "Two", role: "admin" },
        ],
        "workspace-2",
      ),
    ).toBe("admin");
    expect(getCurrentWorkspaceRole(null, "workspace-1")).toBe("unknown");
  });

  test("resolves workspace members context from bootstrap when no explicit workspace id is configured", () => {
    expect(
      resolvePermissionAdminWorkspace({
        apiConfigured: true,
        bootstrapWorkspaceId: "workspace-from-bootstrap",
        configuredWorkspaceId: null,
      }),
    ).toEqual({
      reason: null,
      source: "bootstrap",
      workspaceId: "workspace-from-bootstrap",
    });

    expect(
      resolvePermissionAdminWorkspace({
        apiConfigured: true,
        bootstrapWorkspaceId: "workspace-from-bootstrap",
        configuredWorkspaceId: "workspace-from-config",
      }).workspaceId,
    ).toBe("workspace-from-config");
  });

  test("keeps viewer member management unavailable", () => {
    const capability = getMemberActionCapability({
      action: "add-member",
      apiConfigured: true,
      currentRole: "viewer",
      operation: null,
      status: "ready",
    });

    expect(capability.canUse).toBe(false);
    expect(capability.reason).toContain("owners and admins");
  });

  test("reports member mutation disabled reasons for unavailable API states", () => {
    expect(
      getMemberActionCapability({
        action: "add-member",
        apiConfigured: false,
        currentRole: "owner",
        operation: null,
        status: "unconfigured",
      }),
    ).toEqual({
      canUse: false,
      reason: "Workspace members API is not configured.",
    });

    expect(
      getMemberActionCapability({
        action: "add-member",
        apiConfigured: true,
        currentRole: "owner",
        operation: null,
        status: "forbidden",
      }).reason,
    ).toContain("cannot manage workspace members");

    expect(
      getMemberActionCapability({
        action: "update-role",
        apiConfigured: true,
        currentRole: "admin",
        member: createMember(),
        operation: "user-2",
        status: "ready",
      }).reason,
    ).toContain("operation is in progress");
  });

  test("guards last owner role changes and removal in the frontend model", () => {
    const owner = createMember({ role: "owner", userId: "owner-1" });
    const members = [owner, createMember({ role: "viewer", userId: "viewer-1" })];
    const ownerCapability = getMemberActionCapability({
      action: "remove-member",
      apiConfigured: true,
      currentRole: "admin",
      member: owner,
      members,
      operation: null,
      status: "ready",
    });

    expect(ownerCapability.canUse).toBe(false);
    expect(ownerCapability.reason).toContain("last workspace owner");

    const roleCapability = getMemberActionCapability({
      action: "update-role",
      apiConfigured: true,
      currentRole: "owner",
      member: owner,
      members,
      operation: null,
      status: "ready",
    });
    expect(roleCapability.canUse).toBe(false);
    expect(roleCapability.reason).toContain("downgraded");
    expect(getRoleChangeOptions(owner, members)).toEqual(["owner"]);
  });

  test("keeps unsupported workspace roles out of member lifecycle controls", () => {
    expect(addExistingUserRoleOptions).toEqual(["admin", "editor", "viewer"]);
    expect(isSupportedWorkspaceMemberRole("viewer")).toBe(true);
    expect(isSupportedWorkspaceMemberRole("commenter")).toBe(false);
    expect(
      getMemberLifecycleGuard({
        action: "update-role",
        member: createMember(),
        nextRole: "commenter",
      }),
    ).toEqual({
      canUse: false,
      reason: "Workspace member role is not supported.",
      requiresConfirmation: false,
    });
  });

  test("requires confirmation for owner role changes when another owner remains", () => {
    const owner = createMember({ role: "owner", userId: "owner-1" });
    const members = [owner, createMember({ role: "owner", userId: "owner-2" })];

    expect(getRoleChangeOptions(owner, members)).toEqual(["owner", "admin", "editor", "viewer"]);
    expect(
      getMemberLifecycleGuard({
        action: "update-role",
        member: owner,
        members,
        nextRole: "admin",
      }),
    ).toMatchObject({
      canUse: true,
      requiresConfirmation: true,
    });
  });

  test("guards current user self removal and role changes when identity is available", () => {
    const member = createMember({ role: "admin", userId: "self" });

    expect(
      getMemberLifecycleGuard({
        action: "remove-member",
        currentUserId: "self",
        member,
      }),
    ).toMatchObject({
      canUse: false,
      reason: "You cannot remove your own workspace membership here.",
    });

    expect(
      getMemberLifecycleGuard({
        action: "update-role",
        currentUserId: "self",
        member,
        nextRole: "viewer",
      }),
    ).toMatchObject({
      canUse: false,
      reason: "You cannot change your own workspace role here.",
    });
  });

  test("uses backend mutation error messages before generic fallbacks", () => {
    expect(toPermissionMutationError({ message: "The last workspace owner cannot be removed.", status: 409 }, "Fallback")).toBe(
      "The last workspace owner cannot be removed.",
    );
    expect(toPermissionMutationError({ message: "API returned 409", status: 409 }, "Fallback")).toBe(
      "The requested change conflicts with current workspace state.",
    );
  });

  test("labels group sources without treating every group as directory-managed", () => {
    expect(getWorkspaceGroupSourceLabel(createGroup())).toBe("Local static");
    expect(getWorkspaceGroupReadOnlyReason(createGroup())).toContain("deferred");

    const scimGroup = createGroup({
      externalGroupId: "00g-scim",
      externalProvider: "scim",
      externalSyncedAt: "2026-05-16T00:00:00.000Z",
    });

    expect(getWorkspaceGroupSourceLabel(scimGroup)).toBe("SCIM synced");
    expect(getWorkspaceGroupReadOnlyReason(scimGroup)).toBe("Managed by directory sync.");
    expect(getWorkspaceGroupDetailRows(scimGroup).find(([label]) => label === "Last synced")).toEqual([
      "Last synced",
      "2026-05-16T00:00:00.000Z",
    ]);
  });
});

function createMember(overrides: Partial<WorkspaceMemberDto> = {}): WorkspaceMemberDto {
  return {
    displayName: "North Star",
    email: "north@example.com",
    joinedAt: "2024-02-01T00:00:00.000Z",
    role: "viewer",
    status: "active",
    userId: "user-1",
    ...overrides,
  };
}

function createGroup(overrides: Partial<WorkspaceGroupDto> = {}): WorkspaceGroupDto {
  return {
    createdAt: "2024-02-01T00:00:00.000Z",
    description: null,
    externalGroupId: null,
    externalProvider: null,
    externalSyncedAt: null,
    id: "group-1",
    isArchived: false,
    membersCount: 3,
    name: "Research",
    type: "static",
    updatedAt: "2024-02-02T00:00:00.000Z",
    workspaceId: "workspace-1",
    ...overrides,
  };
}
