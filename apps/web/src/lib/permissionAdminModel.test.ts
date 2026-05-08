import { describe, expect, test } from "../test/harness";
import type { WorkspaceMemberDto } from "./permissionAdminApi";
import {
  getCurrentWorkspaceRole,
  getMemberActionCapability,
  getRoleChangeOptions,
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

  test("allows admin member mutations while backend safety checks still apply", () => {
    const ownerCapability = getMemberActionCapability({
      action: "remove-member",
      apiConfigured: true,
      currentRole: "admin",
      member: createMember({ role: "owner" }),
      operation: null,
      status: "ready",
    });

    expect(ownerCapability.canUse).toBe(true);
    expect(ownerCapability.reason).toContain("last-owner");
    expect(getRoleChangeOptions(createMember({ role: "owner" }))).toEqual(["owner", "admin", "editor", "viewer"]);
  });

  test("uses backend mutation error messages before generic fallbacks", () => {
    expect(toPermissionMutationError({ message: "The last workspace owner cannot be removed.", status: 409 }, "Fallback")).toBe(
      "The last workspace owner cannot be removed.",
    );
    expect(toPermissionMutationError({ message: "API returned 409", status: 409 }, "Fallback")).toBe(
      "The requested change conflicts with current workspace state.",
    );
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
