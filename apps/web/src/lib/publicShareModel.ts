import type { PublicShareResourceType } from "./appApi";
import type { ShareLinkAudience } from "./appApi";
import { createPublicShareHash, getPublicShareTokenFromHash } from "./hashRouting";

export const publicShareUnavailableMessage = "This link is unavailable or has expired.";

export type PublicShareFailureState = {
  canRetryWithPassword: boolean;
  message: string;
};

export function getPublicShareReadEndpoint(resourceType: PublicShareResourceType) {
  return resourceType === "collection" ? "collection" : "document";
}

export function toPublicShareFailureState(options: { hasPassword?: boolean; passwordSubmitted?: boolean } = {}): PublicShareFailureState {
  return {
    canRetryWithPassword: Boolean(options.hasPassword),
    message: publicShareUnavailableMessage,
  };
}

export function getPublicSharePasswordHeader(password?: string | null) {
  const trimmed = password?.trim();
  return trimmed ? { "X-Share-Link-Password": trimmed } : {};
}

export function getPublicShareSafeStatusLabel(value?: string | null) {
  const normalized = value?.trim().toLowerCase();
  if (normalized === "published") {
    return "Published";
  }

  if (normalized === "archived") {
    return "Archived";
  }

  return "Draft";
}

export function publicShareCodeUsesOnlyAllowedEndpoints(source: string) {
  const protectedEndpointPatterns = [
    "/bootstrap",
    "/search",
    "/export",
    "/context",
    "/activity",
    "/comments",
    "/attachments",
    "/files",
    "/versions",
    "/permissions",
    "/documents/",
    "/spaces/",
  ];

  return protectedEndpointPatterns.every((pattern) => !source.includes(pattern));
}

export function buildPublicShareReaderUrl(token: string | null | undefined, appOriginOrBase?: string | null) {
  const normalizedToken = normalizeShareToken(token);
  if (!normalizedToken) {
    return "";
  }

  const appBase = normalizeAppBase(appOriginOrBase) || getDefaultAppBase();
  const hash = createPublicShareHash(normalizedToken);
  return appBase ? `${appBase}/${hash}` : hash;
}

export function toUserFacingShareUrl(
  createOrCopyResponseUrl: string | null | undefined,
  token?: string | null,
  audience?: ShareLinkAudience | null,
  apiBaseUrl?: string | null,
  appOriginOrBase?: string | null,
) {
  const responseUrl = createOrCopyResponseUrl?.trim() ?? "";
  const normalizedAudience = audience?.trim().toLowerCase();
  const directToken = normalizeShareToken(token);

  if (normalizedAudience === "public" && directToken) {
    return buildPublicShareReaderUrl(directToken, appOriginOrBase);
  }

  const frontendToken = extractShareTokenFromFrontendUrl(responseUrl);
  if (frontendToken) {
    return isAbsoluteHttpUrl(responseUrl) ? responseUrl : buildPublicShareReaderUrl(frontendToken, appOriginOrBase);
  }

  const extracted = extractShareTokenFromApiUrlWithSource(responseUrl);
  if (extracted && (normalizedAudience === "public" || extracted.source === "public-api")) {
    return buildPublicShareReaderUrl(extracted.token, appOriginOrBase);
  }

  if (normalizedAudience === "public" && extracted) {
    return buildPublicShareReaderUrl(extracted.token, appOriginOrBase);
  }

  return toAbsoluteUrl(responseUrl, apiBaseUrl ?? undefined);
}

export function extractShareTokenFromApiUrl(url: string | null | undefined) {
  return extractShareTokenFromApiUrlWithSource(url)?.token ?? null;
}

function extractShareTokenFromApiUrlWithSource(url: string | null | undefined) {
  const parsed = parseUrlLike(url);
  if (!parsed) {
    return null;
  }

  const path = parsed.pathname.replace(/\/+$/, "");
  const match = path.match(/^\/api\/v1\/(public\/)?share-links\/([^/]+)\/(?:resolve|document|collection)$/i);
  if (!match) {
    return null;
  }

  const token = normalizeShareToken(safeDecodeURIComponent(match[2]));
  if (!token) {
    return null;
  }

  return {
    source: match[1] ? "public-api" as const : "protected-api" as const,
    token,
  };
}

function extractShareTokenFromFrontendUrl(url: string) {
  if (!url) {
    return null;
  }

  const hashToken = url.startsWith("#") ? getPublicShareTokenFromHash(url) : null;
  if (hashToken) {
    return hashToken;
  }

  const parsed = parseUrlLike(url);
  if (!parsed?.hash) {
    return null;
  }

  return getPublicShareTokenFromHash(parsed.hash);
}

function toAbsoluteUrl(value: string, apiBaseUrl?: string) {
  if (!value) {
    return "";
  }

  try {
    return new URL(value, apiBaseUrl || getDefaultAppBase() || undefined).toString();
  } catch {
    return value;
  }
}

function parseUrlLike(value: string | null | undefined) {
  const trimmed = value?.trim();
  if (!trimmed) {
    return null;
  }

  try {
    return new URL(trimmed, "https://northstar.local");
  } catch {
    return null;
  }
}

function normalizeAppBase(value?: string | null) {
  const trimmed = value?.trim().replace(/\/+$/, "");
  if (!trimmed) {
    return "";
  }

  return trimmed.replace(/\/api\/v1$/i, "");
}

function getDefaultAppBase() {
  if (typeof window !== "undefined" && window.location?.origin) {
    return window.location.origin;
  }

  return "";
}

function normalizeShareToken(token: string | null | undefined) {
  const trimmed = token?.trim();
  if (!trimmed || trimmed.length > 512 || /[\s?#/\\]/.test(trimmed)) {
    return null;
  }

  return trimmed;
}

function safeDecodeURIComponent(value: string) {
  try {
    return decodeURIComponent(value);
  } catch {
    return "";
  }
}

function isAbsoluteHttpUrl(value: string) {
  return /^https?:\/\//i.test(value);
}
