import type { CreateShareLinkRequest, ShareLinkAudience, ShareLinkRole } from "./appApi";

export type SharePermissionApiStatus = "error" | "forbidden" | "loading" | "ready" | "unconfigured";

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

export function toSharePermissionMutationError(error: { message?: string; status?: number } | unknown, fallback: string) {
  if (isApiLikeError(error)) {
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

function isApiLikeError(error: unknown): error is { message?: string; status?: number } {
  return typeof error === "object" && error !== null && ("message" in error || "status" in error);
}
