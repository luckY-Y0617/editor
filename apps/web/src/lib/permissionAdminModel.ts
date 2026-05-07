import type { AuthWorkspaceDto } from "./authClient";
import type { PermissionAdminApiStatus, WorkspaceMemberDto } from "./permissionAdminApi";

export type PermissionAdminAction = "add-member" | "remove-member" | "update-role";

export type PermissionAdminCapability = {
  canUse: boolean;
  reason: string | null;
};

export type CurrentWorkspaceRole = "admin" | "editor" | "owner" | "viewer" | "unknown";
export type PermissionAdminWorkspaceSource = "bootstrap" | "configured" | "missing";

export type PermissionAdminWorkspaceResolution = {
  reason: string | null;
  source: PermissionAdminWorkspaceSource;
  workspaceId: string | null;
};

const managementRoles = new Set(["admin", "owner"]);

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
  member?: WorkspaceMemberDto | null;
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

  if (options.member.role === "owner") {
    return {
      canUse: true,
      reason: "Backend last-owner and step-up checks still apply.",
    };
  }

  return { canUse: true, reason: null };
}

export function getRoleChangeOptions(member: WorkspaceMemberDto) {
  const roles = ["admin", "editor", "viewer"];
  return member.role === "owner" ? ["owner", ...roles] : roles;
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
