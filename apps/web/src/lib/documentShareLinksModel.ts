import type { CreateShareLinkRequest, ShareLinkAudience, ShareLinkContentProtectionDto, ShareLinkRole } from "./appApi";
import { ApiClientError, formatApiOperationError } from "./apiClient";
import { getContentProtectionLabels, normalizeContentProtection } from "./publicShareModel";

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
export type PublicShareCreationScope = "collection" | "document" | "library";

export type PublicSharePolicy = {
  allowCollectionScope: boolean;
  allowDocumentScope: boolean;
  allowLibraryScope: boolean;
  allowNoExpiry: boolean;
  maxExpiryDays: number | null;
  maxExpiryDaysForLibrary: number | null;
  requireExpiry: boolean;
  requirePassword: boolean;
  requirePasswordForCollection: boolean;
  requirePasswordForLibrary: boolean;
  requireWatermarkForLibrary: boolean;
  viewerOnly: boolean;
  defaultDisableDownload: boolean;
  defaultDisablePrint: boolean;
  defaultDisableCopy: boolean;
  defaultWatermarkEnabled: boolean;
};

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
  collectionId?: string | null;
  contentProtection?: Partial<ShareLinkContentProtectionDto> | null;
  expiresAt: string | null;
  libraryId?: string | null;
  linkScope: "invited" | "public" | "workspace";
  operation: string | null;
  password?: string | null;
  passwordEnabled?: boolean;
  policy?: PublicSharePolicy;
  publicScope?: PublicShareCreationScope;
  status: ShareDrawerApiStatus;
};

export type PublicShareScopeOptionState = {
  disabled: boolean;
  label: string;
  reason: string | null;
  value: PublicShareCreationScope;
};

export const defaultPublicSharePolicy: PublicSharePolicy = {
  allowCollectionScope: true,
  allowDocumentScope: true,
  allowLibraryScope: false,
  allowNoExpiry: false,
  maxExpiryDays: 30,
  maxExpiryDaysForLibrary: 30,
  requireExpiry: true,
  requirePassword: false,
  requirePasswordForCollection: true,
  requirePasswordForLibrary: true,
  requireWatermarkForLibrary: false,
  viewerOnly: true,
  defaultDisableDownload: true,
  defaultDisablePrint: false,
  defaultDisableCopy: false,
  defaultWatermarkEnabled: false,
};

export const defaultPublicContentProtection: ShareLinkContentProtectionDto = {
  disableDownload: defaultPublicSharePolicy.defaultDisableDownload,
  disablePrint: defaultPublicSharePolicy.defaultDisablePrint,
  disableCopy: defaultPublicSharePolicy.defaultDisableCopy,
  watermarkEnabled: defaultPublicSharePolicy.defaultWatermarkEnabled,
  watermarkText: "Public link",
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
    const publicScope = options.publicScope ?? "document";
    const policyReason = getPublicSharePolicyViolation({
      collectionId: options.collectionId,
      contentProtection: options.contentProtection,
      expiresAt: options.expiresAt,
      libraryId: options.libraryId,
      password: options.password,
      passwordEnabled: options.passwordEnabled,
      policy: options.policy,
      scope: publicScope,
    });
    if (policyReason) {
      return policyReason;
    }
  }

  return null;
}

export function getPublicShareScopeOptions(
  collectionId?: string | null,
  policy: PublicSharePolicy = defaultPublicSharePolicy,
  libraryId?: string | null,
): PublicShareScopeOptionState[] {
  const hasCollection = Boolean(collectionId?.trim());
  const hasLibrary = Boolean(libraryId?.trim());
  return [
    {
      disabled: !policy.allowDocumentScope,
      label: "Current document",
      reason: policy.allowDocumentScope ? null : "Enterprise policy does not allow document public links.",
      value: "document",
    },
    {
      disabled: !hasCollection || !policy.allowCollectionScope,
      label: "Current collection",
      reason: !hasCollection
        ? "This document is not in a shareable collection."
        : policy.allowCollectionScope
          ? null
          : "Enterprise policy does not allow Collection public links.",
      value: "collection",
    },
    {
      disabled: !hasLibrary || !policy.allowLibraryScope,
      label: "Library",
      reason: !hasLibrary
        ? "Library context is not available for this document."
        : policy.allowLibraryScope
          ? null
          : "Enterprise policy does not allow Library public links.",
      value: "library",
    },
  ];
}

export function getPublicShareScopeHint(scope: PublicShareCreationScope, collectionId?: string | null, libraryId?: string | null) {
  if (scope === "collection") {
    return collectionId?.trim()
      ? "Link visitors can browse only public read-only content inside this collection."
      : "This document is not in a shareable collection.";
  }

  if (scope === "library") {
    return libraryId?.trim()
      ? "Library 链接会公开此 Library 范围内允许公开的只读内容。"
      : "Library context is not available for this document.";
  }

  return "Link visitors can read only this document.";
}

export function getShareLinkScopeLabel(link: { resourceType?: string | null }) {
  if (link.resourceType === "collection") {
    return "Collection";
  }

  if (link.resourceType === "library") {
    return "Library";
  }

  return "Current document";
}

export function getShareLinkGovernanceHint(link: {
  audience: ShareLinkAudience;
  contentProtection?: Partial<ShareLinkContentProtectionDto> | null;
  expiresAt?: string | null;
  hasPassword?: boolean | null;
  resourceType?: string | null;
  status?: string | null;
}) {
  const scope = getShareLinkScopeLabel(link);
  const parts = [formatAudienceHint(link.audience), scope, "viewer-only"];
  if (link.audience === "public") {
    parts.push(link.hasPassword ? "password" : "no password");
    parts.push(...getContentProtectionLabels(link.contentProtection).slice(0, 2));
  }

  if (link.status === "paused") {
    parts.push("paused");
  } else if (link.expiresAt && Date.parse(link.expiresAt) - Date.now() <= 7 * 24 * 60 * 60 * 1000) {
    parts.push("expiring soon");
  }

  return parts.filter(Boolean).join(" / ");
}

export function getPublicShareCreateTarget(options: {
  collectionId?: string | null;
  documentId: string;
  libraryId?: string | null;
  policy?: PublicSharePolicy;
  publicScope: PublicShareCreationScope;
}) {
  const policy = options.policy ?? defaultPublicSharePolicy;
  if (options.publicScope === "document" && !policy.allowDocumentScope) {
    return {
      reason: "Enterprise policy does not allow document public links.",
      resourceId: null,
      resourceType: null,
    };
  }

  if (options.publicScope === "collection") {
    const collectionId = options.collectionId?.trim();
    if (!collectionId) {
      return {
        reason: "This document is not in a shareable collection.",
        resourceId: null,
        resourceType: null,
      };
    }

    if (!policy.allowCollectionScope) {
      return {
        reason: "Enterprise policy does not allow Collection public links.",
        resourceId: null,
        resourceType: null,
      };
    }

    return {
      reason: null,
      resourceId: collectionId,
      resourceType: "collection" as const,
    };
  }

  if (options.publicScope === "library") {
    const libraryId = options.libraryId?.trim();
    if (!libraryId) {
      return {
        reason: "Library context is not available for this document.",
        resourceId: null,
        resourceType: null,
      };
    }

    if (!policy.allowLibraryScope) {
      return {
        reason: "Enterprise policy does not allow Library public links.",
        resourceId: null,
        resourceType: null,
      };
    }

    return {
      reason: null,
      resourceId: libraryId,
      resourceType: "library" as const,
    };
  }

  return {
    reason: null,
    resourceId: options.documentId,
    resourceType: "document" as const,
  };
}

export function getPublicSharePolicyViolation(options: {
  collectionId?: string | null;
  contentProtection?: Partial<ShareLinkContentProtectionDto> | null;
  expiresAt: string | null;
  libraryId?: string | null;
  password?: string | null;
  passwordEnabled?: boolean;
  policy?: PublicSharePolicy;
  scope: PublicShareCreationScope;
  now?: Date;
}) {
  const policy = options.policy ?? defaultPublicSharePolicy;
  const now = options.now ?? new Date();

  if (options.scope === "document" && !policy.allowDocumentScope) {
    return "Enterprise policy does not allow document public links.";
  }

  if (options.scope === "collection") {
    if (!options.collectionId?.trim()) {
      return "This document is not in a shareable collection.";
    }

    if (!policy.allowCollectionScope) {
      return "Enterprise policy does not allow Collection public links.";
    }
  }

  if (options.scope === "library") {
    if (!options.libraryId?.trim()) {
      return "Library context is not available for this document.";
    }

    if (!policy.allowLibraryScope) {
      return "Enterprise policy does not allow Library public links.";
    }
  }

  if ((policy.requireExpiry || !policy.allowNoExpiry) && !options.expiresAt) {
    return "Enterprise policy requires public links to have an expiry.";
  }

  if (options.expiresAt) {
    const expiresAt = new Date(options.expiresAt);
    if (Number.isNaN(expiresAt.getTime()) || expiresAt.getTime() <= now.getTime()) {
      return "Public link expiry must be in the future.";
    }

    const effectiveMaxExpiryDays = getEffectivePublicMaxExpiryDays(policy, options.scope);
    if (effectiveMaxExpiryDays && expiresAt.getTime() - now.getTime() > effectiveMaxExpiryDays * 24 * 60 * 60 * 1000) {
      return "Expiry exceeds the enterprise policy maximum.";
    }
  }

  const hasPassword = Boolean(options.passwordEnabled && options.password?.trim());
  if (policy.requirePassword && !hasPassword) {
    return "Enterprise policy requires public links to use an access password.";
  }

  if (policy.requirePasswordForCollection && options.scope === "collection" && !hasPassword) {
    return "Enterprise policy requires Collection public links to use an access password.";
  }

  if (policy.requirePasswordForLibrary && options.scope === "library" && !hasPassword) {
    return "Enterprise policy requires Library public links to use an access password.";
  }

  if (policy.requireWatermarkForLibrary && options.scope === "library" && !options.contentProtection?.watermarkEnabled) {
    return "Enterprise policy requires Library public links to use a watermark.";
  }

  return null;
}

export function getPublicSharePasswordHint(scope: PublicShareCreationScope, policy: PublicSharePolicy = defaultPublicSharePolicy) {
  if (policy.requirePassword) {
    return "Enterprise policy requires public links to use an access password.";
  }

  if (scope === "collection" && policy.requirePasswordForCollection) {
    return "Enterprise policy requires Collection public links to use an access password.";
  }

  if (scope === "library" && policy.requirePasswordForLibrary) {
    return "Enterprise policy requires Library public links to use an access password.";
  }

  return null;
}

export function getPublicSharePolicyWarnings(
  link: {
    audience: ShareLinkAudience;
    expiresAt?: string | null;
    hasPassword?: boolean | null;
    resourceType?: string | null;
    status?: string | null;
  },
  policy: PublicSharePolicy = defaultPublicSharePolicy,
  now = new Date(),
) {
  if (link.audience !== "public") {
    return [];
  }

  const warnings: string[] = [];
  if (
    (policy.requirePassword ||
      (policy.requirePasswordForCollection && link.resourceType === "collection") ||
      (policy.requirePasswordForLibrary && link.resourceType === "library")) &&
    !link.hasPassword
  ) {
    warnings.push("missing required password");
  }

  if ((policy.requireExpiry || !policy.allowNoExpiry) && !link.expiresAt) {
    warnings.push("no expiry no longer allowed");
  } else if (link.expiresAt) {
    const effectiveMaxExpiryDays = getEffectivePublicMaxExpiryDays(policy, link.resourceType);
    if (effectiveMaxExpiryDays && Date.parse(link.expiresAt) - now.getTime() > effectiveMaxExpiryDays * 24 * 60 * 60 * 1000) {
      warnings.push("expiry longer than current policy");
    }
  }

  if (link.resourceType === "collection" && !policy.allowCollectionScope) {
    warnings.push("collection scope currently disabled");
  }

  if (link.resourceType === "library" && !policy.allowLibraryScope) {
    warnings.push("library scope currently disabled");
  }

  if (link.resourceType === "library" && policy.requireWatermarkForLibrary) {
    const protection = normalizeContentProtection((link as { contentProtection?: Partial<ShareLinkContentProtectionDto> | null }).contentProtection);
    if (!protection.watermarkEnabled) {
      warnings.push("missing required watermark");
    }
  }

  if (link.status === "policy_paused" || link.status === "policy-paused") {
    warnings.push("policy-paused");
  }

  return warnings;
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

function getEffectivePublicMaxExpiryDays(policy: PublicSharePolicy, scope?: string | null) {
  if (scope !== "library" || !policy.maxExpiryDaysForLibrary || policy.maxExpiryDaysForLibrary <= 0) {
    return policy.maxExpiryDays;
  }

  if (!policy.maxExpiryDays || policy.maxExpiryDays <= 0) {
    return policy.maxExpiryDaysForLibrary;
  }

  return Math.min(policy.maxExpiryDays, policy.maxExpiryDaysForLibrary);
}

export function createShareLinkRequest(options: {
  audience: ShareLinkAudience;
  expiresAt?: string | null;
  password?: string | null;
  roleKey: ShareLinkRole;
  subjectEmail?: string | null;
  contentProtection?: ShareLinkContentProtectionDto | null;
}): CreateShareLinkRequest {
  return {
    audience: options.audience,
    contentProtection: options.audience === "public"
      ? normalizeContentProtection(options.contentProtection ?? defaultPublicContentProtection)
      : null,
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
  if (error instanceof ApiClientError) {
    const reason = readPolicyBlockedReason(error.details);
    if (reason) {
      return formatPolicyBlockedReason(reason, error.message);
    }
  }

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

function readPolicyBlockedReason(details: unknown) {
  if (typeof details !== "object" || details === null) {
    return null;
  }

  const value = details as { policyBlocked?: unknown; reason?: unknown };
  return value.policyBlocked === true && typeof value.reason === "string" ? value.reason : null;
}

function formatPolicyBlockedReason(reason: string, fallback: string) {
  if (reason === "PUBLIC_SHARE_PASSWORD_REQUIRED") {
    return "Enterprise policy requires this public link to use an access password.";
  }

  if (reason === "PUBLIC_SHARE_EXPIRY_REQUIRED") {
    return "Enterprise policy requires public links to have an expiry.";
  }

  if (reason === "PUBLIC_SHARE_EXPIRY_TOO_LONG") {
    return "Expiry exceeds the enterprise policy maximum.";
  }

  if (reason === "PUBLIC_SHARE_SCOPE_DISABLED") {
    return "Enterprise policy does not allow this public share scope.";
  }

  if (reason === "PUBLIC_SHARE_WORKSPACE_UNSUPPORTED") {
    return "Workspace public sharing is not supported.";
  }

  if (reason === "PUBLIC_SHARE_ROLE_NOT_ALLOWED") {
    return "Public links are viewer-only by enterprise policy.";
  }

  if (reason === "PUBLIC_SHARE_DISABLED") {
    return "Public share links are disabled by enterprise policy.";
  }

  if (reason === "PUBLIC_SHARE_DOWNLOAD_DISABLED_REQUIRED") {
    return "Enterprise policy requires public links to disable downloads.";
  }

  if (reason === "PUBLIC_SHARE_WATERMARK_REQUIRED") {
    return "Enterprise policy requires this public link to display a watermark.";
  }

  return fallback;
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

function formatAudienceHint(audience: ShareLinkAudience) {
  if (audience === "public") {
    return "Public";
  }

  if (audience === "external") {
    return "External";
  }

  return "Workspace";
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
