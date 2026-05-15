import type { CreateShareLinkRequest, ShareLinkAudience, ShareLinkRole } from "./appApi";
import { formatApiOperationError } from "./apiClient";

export type SharePermissionApiStatus = "error" | "forbidden" | "loading" | "ready" | "unconfigured";
export type ShareDrawerApiStatus = SharePermissionApiStatus | "idle";

export type ShareTargetSource = "bootstrap" | "configured" | "hash" | "missing" | "query";

export type ShareTargetResolution = {
  documentId: string | null;
  reason: string | null;
  source: ShareTargetSource;
  workspaceId: string | null;
};

export type ShareLinkCapability = {
  canUse: boolean;
  reason: string | null;
};

export type DocumentShareLinkStatus = "active" | "expired" | "paused" | "policy-paused" | "revoked";

export type ShareInviteRole = "commenter" | "editor" | "viewer";
export type DocumentAdvancedRoleAccess = "Can comment" | "Can edit" | "Can manage" | "Can view";
export type DocumentPolicyLinkMode = "disabled" | "external" | "internal";
export type DocumentPolicyInheritanceMode = "inherit" | "restricted";

export type DocumentAdvancedRoleRow = {
  access: DocumentAdvancedRoleAccess;
  description: string;
  label: string;
  roleKey: string;
};

export type DocumentAdvancedGrantLike = {
  id: string;
  roleKey: string;
};

export type BatchGrantRevokeResult = {
  failed: Array<{ grantId: string; reason: string }>;
  succeeded: string[];
};

export type WorkspaceGroupLike = {
  externalGroupId?: string | null;
  externalProvider?: string | null;
  externalSyncedAt?: string | null;
  isArchived?: boolean;
  membersCount?: number | null;
};

export type AccessRequestReviewDraft = {
  decision: "approve" | "deny";
  expiresAt?: string | null;
  requestedRole: string;
  roleKey?: string | null;
};

export type ShareDrawerInviteCapabilityOptions = {
  apiConfigured: boolean;
  availableRoles: Set<ShareInviteRole>;
  inviteIsEmail: boolean;
  isDirectMemberInvite: boolean;
  memberStatus: ShareDrawerApiStatus;
  operation: string | null;
  selectedRole: ShareInviteRole;
  status: ShareDrawerApiStatus;
  value: string;
};

export type ShareDrawerLinkCapabilityOptions = {
  apiConfigured: boolean;
  expiresAt: string | null;
  linkScope: "invited" | "public" | "workspace";
  operation: string | null;
  status: ShareDrawerApiStatus;
};

export function resolveShareTarget(options: {
  apiConfigured: boolean;
  bootstrapDocumentId?: string | null;
  bootstrapWorkspaceId?: string | null;
  configuredDocumentId?: string | null;
  configuredDocumentSource?: Exclude<ShareTargetSource, "bootstrap" | "missing"> | null;
  configuredWorkspaceId?: string | null;
}): ShareTargetResolution {
  if (!options.apiConfigured) {
    return {
      documentId: null,
      reason: "Share API is not configured.",
      source: "missing",
      workspaceId: null,
    };
  }

  const configuredDocumentId = options.configuredDocumentId?.trim() || null;
  if (configuredDocumentId) {
    return {
      documentId: configuredDocumentId,
      reason: null,
      source: options.configuredDocumentSource ?? "configured",
      workspaceId: options.configuredWorkspaceId ?? options.bootstrapWorkspaceId ?? null,
    };
  }

  const bootstrapDocumentId = options.bootstrapDocumentId?.trim() || null;
  if (bootstrapDocumentId) {
    return {
      documentId: bootstrapDocumentId,
      reason: null,
      source: "bootstrap",
      workspaceId: options.bootstrapWorkspaceId ?? options.configuredWorkspaceId ?? null,
    };
  }

  return {
    documentId: null,
    reason: "Open Share from a document or configure a share document id.",
    source: "missing",
    workspaceId: options.configuredWorkspaceId ?? options.bootstrapWorkspaceId ?? null,
  };
}

export function getShareLinkCapability(options: {
  apiConfigured: boolean;
  documentId: string | null;
  operation: string | null;
  status: SharePermissionApiStatus;
}): ShareLinkCapability {
  if (!options.apiConfigured) {
    return { canUse: false, reason: "Share API is not configured." };
  }

  if (!options.documentId) {
    return { canUse: false, reason: "Open Share from a document before creating links." };
  }

  if (options.status === "loading") {
    return { canUse: false, reason: "Share links are still loading." };
  }

  if (options.status === "forbidden") {
    return { canUse: false, reason: "You do not have permission to manage links for this document." };
  }

  if (options.status === "error") {
    return { canUse: false, reason: "Share link API is unavailable." };
  }

  if (options.status !== "ready") {
    return { canUse: false, reason: "Share links are not connected." };
  }

  if (options.operation) {
    return { canUse: false, reason: "Another share-link operation is in progress." };
  }

  return { canUse: true, reason: null };
}

export function getShareDrawerInviteDisabledReason(options: ShareDrawerInviteCapabilityOptions) {
  const baseReason = getShareDrawerBaseDisabledReason({
    apiConfigured: options.apiConfigured,
    operation: options.operation,
    status: options.status,
  });
  if (baseReason) {
    return baseReason;
  }

  const value = options.value.trim();
  if (!value) {
    return "Enter a workspace member, user ID, or email address.";
  }

  if (options.isDirectMemberInvite) {
    return options.availableRoles.has(options.selectedRole)
      ? null
      : "Selected role is not available for direct document grants.";
  }

  if (options.inviteIsEmail) {
    return options.selectedRole === "editor"
      ? "Email invites support viewer or commenter access only."
      : null;
  }

  if (options.memberStatus === "loading") {
    return "Workspace member search is still loading.";
  }

  if (options.memberStatus === "ready") {
    return "No matching workspace member found. Enter an email address to invite externally.";
  }

  return "Workspace member search is unavailable. Enter an email address to invite externally.";
}

export function getShareDrawerLinkDisabledReason(options: ShareDrawerLinkCapabilityOptions) {
  const baseReason = getShareDrawerBaseDisabledReason({
    apiConfigured: options.apiConfigured,
    operation: options.operation,
    status: options.status,
  });
  if (baseReason) {
    return baseReason;
  }

  if (options.linkScope === "invited") {
    return "Only invited people can access this document; no share link is needed.";
  }

  if (options.linkScope === "public") {
    if (!options.expiresAt) {
      return "Public links require a future expiry time.";
    }

    if (new Date(options.expiresAt).getTime() <= Date.now()) {
      return "Public link expiry must be in the future.";
    }
  }

  return null;
}

export function createWorkspaceShareLinkRequest(roleKey: ShareLinkRole): CreateShareLinkRequest {
  return {
    audience: "workspace",
    expiresAt: null,
    password: null,
    roleKey,
    subjectEmail: null,
  };
}

export function createShareLinkRequest(options: {
  audience: ShareLinkAudience;
  expiresAt?: string | null;
  password?: string | null;
  roleKey: ShareLinkRole;
  subjectEmail?: string | null;
}): CreateShareLinkRequest {
  return {
    audience: options.audience,
    expiresAt: options.expiresAt ?? null,
    password: options.audience === "public" ? options.password?.trim() || null : null,
    roleKey: options.audience === "public" ? "viewer" : options.roleKey,
    subjectEmail: options.audience === "external" ? options.subjectEmail?.trim().toLowerCase() || null : null,
  };
}

export function getDocumentShareLinkStatus(
  link: {
    audience: ShareLinkAudience;
    expiresAt: string | null;
    revokedAt: string | null;
    status?: string | null;
  },
  policyLinkMode: string | null | undefined,
  now = new Date(),
): DocumentShareLinkStatus {
  if (link.revokedAt) {
    return "revoked";
  }

  if (link.expiresAt && Date.parse(link.expiresAt) <= now.getTime()) {
    return "expired";
  }

  const dtoStatus = link.status?.trim().toLowerCase().replace("_", "-");
  if (dtoStatus === "paused") {
    return "paused";
  }

  if (dtoStatus === "policy-paused") {
    return "policy-paused";
  }

  const expectedMode = link.audience === "workspace" ? "internal" : link.audience;
  if ((policyLinkMode ?? "disabled").trim().toLowerCase() !== expectedMode) {
    return "policy-paused";
  }

  return "active";
}

export function getExistingShareLinkCopyCapability(options: {
  apiConfigured: boolean;
  canManage: boolean;
  copyEndpointAvailable: boolean;
  operation: string | null;
  status: DocumentShareLinkStatus;
}) {
  if (!options.apiConfigured) {
    return "Share API is not configured.";
  }

  if (!options.copyEndpointAvailable) {
    return "Existing full-link copy is disabled because no approved audited copy endpoint is available.";
  }

  if (!options.canManage) {
    return "Only users who can manage sharing can request an audited copy.";
  }

  if (options.operation) {
    return "Another share operation is in progress.";
  }

  if (options.status === "revoked") {
    return "Revoked links cannot be copied.";
  }

  if (options.status === "expired") {
    return "Expired links cannot be copied.";
  }

  return null;
}

export function toDocumentAdvancedRoleRows(availableRoles: string[] | null | undefined): DocumentAdvancedRoleRow[] {
  const roles = getAvailableDocumentGrantRoles(availableRoles);
  const order = new Map(["owner", "admin", "editor", "commenter", "viewer"].map((role, index) => [role, index]));

  return roles
    .sort((left, right) => (order.get(left) ?? 99) - (order.get(right) ?? 99) || left.localeCompare(right))
    .map((roleKey) => {
      if (roleKey === "owner" || roleKey === "admin") {
        return {
          access: "Can manage",
          description: "Can manage document permissions when the backend role catalog allows it.",
          label: toRoleLabel(roleKey),
          roleKey,
        };
      }

      if (roleKey === "editor") {
        return {
          access: "Can edit",
          description: "Can edit this document according to the current resource policy and grants.",
          label: "Editor",
          roleKey,
        };
      }

      if (roleKey === "commenter") {
        return {
          access: "Can comment",
          description: "Can read and comment when comment access is supported by the current permission contract.",
          label: "Commenter",
          roleKey,
        };
      }

      if (roleKey === "viewer") {
        return {
          access: "Can view",
          description: "Can read this document when effective access permits it.",
          label: "Viewer",
          roleKey,
        };
      }

      return {
        access: "Can view",
        description: "Custom backend role returned by the current permission contract.",
        label: toRoleLabel(roleKey),
        roleKey,
      };
    });
}

export function getAvailableDocumentGrantRoles(availableRoles: string[] | null | undefined) {
  const normalizedRoles = Array.from(
    new Set((availableRoles ?? []).map((role) => role.trim().toLowerCase()).filter(Boolean)),
  );
  return normalizedRoles.length ? normalizedRoles : ["viewer"];
}

export function coerceDocumentGrantRole(roleKey: string | null | undefined, availableRoles: string[] | null | undefined) {
  const roles = getAvailableDocumentGrantRoles(availableRoles);
  const normalized = roleKey?.trim().toLowerCase() ?? "";
  return roles.includes(normalized) ? normalized : roles[0] ?? "viewer";
}

export function getGenericPolicyLinkModeOptions(): Array<{ label: string; value: DocumentPolicyLinkMode }> {
  return [
    { label: "Disabled", value: "disabled" },
    { label: "Internal", value: "internal" },
    { label: "External", value: "external" },
  ];
}

export function getInheritanceModeLabel(value: string | null | undefined) {
  return value === "restricted" ? "Restricted to direct document grants" : "Inherits workspace or folder access";
}

export function getInheritedSourceLabel(value: string | null | undefined) {
  if (value === "collection") {
    return "Folder inheritance";
  }

  if (value === "workspace") {
    return "Workspace inheritance";
  }

  return "No inherited source";
}

export function getIamManagedGroupState(group: WorkspaceGroupLike | null | undefined) {
  const isManaged = Boolean(group?.externalProvider || group?.externalGroupId || group?.externalSyncedAt);
  const source = isManaged
    ? [group?.externalProvider ?? "IAM", group?.externalGroupId].filter(Boolean).join(" / ")
    : "Local group";

  return {
    isArchived: Boolean(group?.isArchived),
    isManaged,
    membersLabel: `${group?.membersCount ?? 0} members`,
    readOnlyReason: isManaged ? "IAM-managed group. Local member and group editing is disabled." : null,
    source,
  };
}

export function toggleBatchGrantSelection(current: ReadonlySet<string>, grantId: string) {
  const next = new Set(current);
  if (next.has(grantId)) {
    next.delete(grantId);
  } else {
    next.add(grantId);
  }

  return next;
}

export function selectAllGrantIds(grants: DocumentAdvancedGrantLike[]) {
  return new Set(grants.map((grant) => grant.id));
}

export function summarizeBatchGrantRevoke(result: BatchGrantRevokeResult) {
  if (result.failed.length === 0) {
    return `${result.succeeded.length} grant${result.succeeded.length === 1 ? "" : "s"} revoked.`;
  }

  return `${result.succeeded.length} revoked, ${result.failed.length} failed: ${result.failed
    .map((failure) => `${failure.grantId}: ${failure.reason}`)
    .join("; ")}`;
}

export function createAccessRequestReviewRequest(draft: AccessRequestReviewDraft, availableRoles: string[] | null | undefined) {
  const expiresAt = draft.expiresAt ? new Date(draft.expiresAt) : null;
  return {
    decision: draft.decision,
    expiresAt: draft.decision === "approve" && expiresAt && !Number.isNaN(expiresAt.getTime()) ? expiresAt.toISOString() : null,
    reason: null,
    roleKey:
      draft.decision === "approve"
        ? coerceDocumentGrantRole(draft.roleKey ?? draft.requestedRole, availableRoles)
        : null,
  };
}

export function hasForbiddenAdvancedPermissionSecretFields(value: Record<string, unknown>) {
  return ["token", "tokenHash", "token_hash", "password", "passwordHash", "password_hash", "passwordProof", "secret", "url"].some(
    (key) => key in value,
  );
}

export function toSharePermissionMutationError(error: { message?: string; status?: number } | unknown, fallback: string) {
  if (isApiLikeError(error) && error.status !== 0) {
    if (error.message && !error.message.startsWith("API returned ")) {
      return error.message;
    }

    if (error.status === 401 || error.status === 403) {
      return "You do not have permission to manage links for this document.";
    }

    if (error.status === 409) {
      return "The requested share-link change conflicts with current document state.";
    }
  }

  return formatApiOperationError(error, fallback, {
    forbidden: "You do not have permission to manage links for this document.",
    network: "Could not reach the share-link API. Check the backend session and retry.",
    unauthorized: "Sign in again before managing links for this document.",
    unconfigured: "Share API is not configured for this environment.",
  });
}

export function toAbsoluteShareUrl(value: string, apiBaseUrl: string) {
  if (!value.trim()) {
    return "";
  }

  try {
    return new URL(value, apiBaseUrl).toString();
  } catch {
    return value;
  }
}

function toRoleLabel(roleKey: string) {
  return roleKey
    .split(/[-_]/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ") || "Role";
}

function isApiLikeError(error: unknown): error is { message?: string; status?: number } {
  return typeof error === "object" && error !== null && ("message" in error || "status" in error);
}

function getShareDrawerBaseDisabledReason(options: {
  apiConfigured: boolean;
  operation: string | null;
  status: ShareDrawerApiStatus;
}) {
  if (!options.apiConfigured) {
    return "Share API is not configured.";
  }

  if (options.status === "loading" || options.status === "idle") {
    return "Share settings are still loading.";
  }

  if (options.status === "unconfigured") {
    return "Share APIs are not connected for this document.";
  }

  if (options.status === "forbidden") {
    return "You do not have permission to manage sharing for this document.";
  }

  if (options.status === "error") {
    return "Share APIs are unavailable. Retry after the backend session is healthy.";
  }

  if (options.operation) {
    return "Another share operation is in progress.";
  }

  return null;
}
