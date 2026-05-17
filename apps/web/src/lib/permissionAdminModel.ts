import type { AuthWorkspaceDto } from "./authClient";
import type { PermissionAdminApiStatus, WorkspaceGroupDto, WorkspaceMemberDto } from "./permissionAdminApi";

export type PermissionAdminAction = "add-member" | "remove-member" | "update-role";
export type MembersTeamsTab = "directory" | "members" | "summary" | "teams";

export type PermissionAdminCapability = {
  canUse: boolean;
  reason: string | null;
};

export type CurrentWorkspaceRole = "admin" | "editor" | "owner" | "viewer" | "unknown";
export type PermissionAdminWorkspaceSource = "bootstrap" | "configured" | "missing";
export type WorkspaceMemberRole = "admin" | "editor" | "owner" | "viewer";

export type MemberLifecycleAction = "remove-member" | "update-role";

export type MemberLifecycleGuard = {
  canUse: boolean;
  reason: string | null;
  requiresConfirmation: boolean;
};

export type PermissionAdminWorkspaceResolution = {
  reason: string | null;
  source: PermissionAdminWorkspaceSource;
  workspaceId: string | null;
};

const managementRoles = new Set(["admin", "owner"]);
export const supportedWorkspaceMemberRoles: WorkspaceMemberRole[] = ["owner", "admin", "editor", "viewer"];
export const addExistingUserRoleOptions: WorkspaceMemberRole[] = ["admin", "editor", "viewer"];

export function getCurrentWorkspaceRole(workspaces: AuthWorkspaceDto[] | null, workspaceId: string | null): CurrentWorkspaceRole {
  if (!workspaces || !workspaceId) {
    return "unknown";
  }

  const workspace = workspaces.find((candidate) => candidate.id === workspaceId);
  const role = workspace?.role?.toLowerCase();
  if (role === "owner" || role === "admin" || role === "editor" || role === "viewer") {
    return role;
  }

  return "unknown";
}

export function resolvePermissionAdminWorkspace(options: {
  apiConfigured: boolean;
  bootstrapWorkspaceId?: string | null;
  configuredWorkspaceId?: string | null;
}): PermissionAdminWorkspaceResolution {
  if (!options.apiConfigured) {
    return {
      reason: "Permission admin API is not configured.",
      source: "missing",
      workspaceId: null,
    };
  }

  if (options.configuredWorkspaceId) {
    return {
      reason: null,
      source: "configured",
      workspaceId: options.configuredWorkspaceId,
    };
  }

  if (options.bootstrapWorkspaceId) {
    return {
      reason: null,
      source: "bootstrap",
      workspaceId: options.bootstrapWorkspaceId,
    };
  }

  return {
    reason: "Current workspace could not be resolved from bootstrap.",
    source: "missing",
    workspaceId: null,
  };
}

export function getMemberActionCapability(options: {
  action: PermissionAdminAction;
  apiConfigured: boolean;
  currentRole: CurrentWorkspaceRole;
  currentUserId?: string | null;
  member?: WorkspaceMemberDto | null;
  members?: WorkspaceMemberDto[] | null;
  operation: string | null;
  status: PermissionAdminApiStatus;
}): PermissionAdminCapability {
  if (!options.apiConfigured) {
    return { canUse: false, reason: "Workspace members API is not configured." };
  }

  if (options.status === "loading") {
    return { canUse: false, reason: "Workspace members are still loading." };
  }

  if (options.status === "forbidden") {
    return { canUse: false, reason: "Your account cannot manage workspace members." };
  }

  if (options.status === "error") {
    return { canUse: false, reason: "Workspace members API is unavailable." };
  }

  if (options.status !== "ready") {
    return { canUse: false, reason: "Workspace members are not connected." };
  }

  if (!managementRoles.has(options.currentRole)) {
    return { canUse: false, reason: "Only workspace owners and admins can manage members." };
  }

  if (options.operation) {
    return { canUse: false, reason: "Another member operation is in progress." };
  }

  if (options.action === "add-member") {
    return { canUse: true, reason: null };
  }

  if (!options.member) {
    return { canUse: false, reason: "Member is not loaded." };
  }

  if (options.action === "update-role" && isLastActiveOwner(options.member, options.members)) {
    return { canUse: false, reason: "The last workspace owner cannot be downgraded." };
  }

  const guard = getMemberLifecycleGuard({
    action: options.action,
    currentUserId: options.currentUserId,
    member: options.member,
    members: options.members,
  });
  if (!guard.canUse) {
    return { canUse: false, reason: guard.reason };
  }

  return { canUse: true, reason: guard.reason };
}

export function getRoleChangeOptions(member: WorkspaceMemberDto, members?: WorkspaceMemberDto[] | null): WorkspaceMemberRole[] {
  if (isLastActiveOwner(member, members)) {
    return ["owner"];
  }

  return member.role === "owner" ? supportedWorkspaceMemberRoles : addExistingUserRoleOptions;
}

export function isSupportedWorkspaceMemberRole(role: string): role is WorkspaceMemberRole {
  return supportedWorkspaceMemberRoles.includes(role as WorkspaceMemberRole);
}

export function getMemberLifecycleGuard(options: {
  action: MemberLifecycleAction;
  currentUserId?: string | null;
  member: WorkspaceMemberDto;
  members?: WorkspaceMemberDto[] | null;
  nextRole?: string | null;
}): MemberLifecycleGuard {
  const isSelf = Boolean(options.currentUserId && options.currentUserId === options.member.userId);

  if (options.action === "remove-member") {
    if (isLastActiveOwner(options.member, options.members)) {
      return {
        canUse: false,
        reason: "The last workspace owner cannot be removed.",
        requiresConfirmation: false,
      };
    }

    if (isSelf) {
      return {
        canUse: false,
        reason: "You cannot remove your own workspace membership here.",
        requiresConfirmation: false,
      };
    }

    return { canUse: true, reason: "Confirm removal before changing workspace access.", requiresConfirmation: true };
  }

  const nextRole = options.nextRole ?? options.member.role;
  if (!isSupportedWorkspaceMemberRole(nextRole)) {
    return {
      canUse: false,
      reason: "Workspace member role is not supported.",
      requiresConfirmation: false,
    };
  }

  if (isLastActiveOwner(options.member, options.members) && nextRole !== "owner") {
    return {
      canUse: false,
      reason: "The last workspace owner cannot be downgraded.",
      requiresConfirmation: false,
    };
  }

  if (isSelf && nextRole !== options.member.role) {
    return {
      canUse: false,
      reason: "You cannot change your own workspace role here.",
      requiresConfirmation: false,
    };
  }

  return {
    canUse: true,
    reason: options.member.role === "owner" || nextRole === "owner"
      ? "Owner role changes require confirmation and backend validation."
      : null,
    requiresConfirmation: options.member.role === "owner" || nextRole === "owner",
  };
}

export function isLastActiveOwner(member: WorkspaceMemberDto, members?: WorkspaceMemberDto[] | null) {
  if (member.role !== "owner" || !members) {
    return false;
  }

  return members.filter((candidate) => candidate.role === "owner" && candidate.status !== "removed").length <= 1;
}

export function getWorkspaceGroupSourceLabel(group: WorkspaceGroupDto) {
  if (group.externalProvider?.trim()) {
    return `${formatGroupSourceProvider(group.externalProvider)} synced`;
  }

  if (group.externalGroupId?.trim()) {
    return "External synced";
  }

  return group.type === "dynamic" ? "Local dynamic" : "Local static";
}

export function getWorkspaceGroupReadOnlyReason(group: WorkspaceGroupDto) {
  if (group.externalProvider || group.externalGroupId) {
    return "Managed by directory sync.";
  }

  return "Local group editing is deferred in this surface.";
}

export function getWorkspaceGroupDetailRows(group: WorkspaceGroupDto) {
  return [
    ["Source", getWorkspaceGroupSourceLabel(group)],
    ["Editable", "Read-only in this V1"],
    ["Members", String(group.membersCount)],
    ["Last synced", group.externalSyncedAt ?? "Not synced"],
    ["External id", group.externalGroupId ?? "None"],
  ];
}

export function toPermissionMutationError(error: { message?: string; status?: number } | unknown, fallback: string) {
  if (isApiLikeError(error)) {
    if (error.message && !error.message.startsWith("API returned ")) {
      return error.message;
    }

    if (error.status === 401 || error.status === 403) {
      return "You do not have permission to change this workspace.";
    }

    if (error.status === 409) {
      return "The requested change conflicts with current workspace state.";
    }
  }

  return fallback;
}

function isApiLikeError(error: unknown): error is { message?: string; status?: number } {
  return typeof error === "object" && error !== null && ("message" in error || "status" in error);
}

function formatGroupSourceProvider(value: string) {
  const normalized = value.trim().toLowerCase();
  if (normalized === "scim") {
    return "SCIM";
  }

  if (normalized === "iam") {
    return "IAM";
  }

  return value.trim();
}
