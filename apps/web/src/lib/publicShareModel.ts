import type { PublicShareResourceType, PublicShareTreeNodeDto, PublicShareTreeResponse, ShareLinkContentProtectionDto } from "./appApi";
import type { ShareLinkAudience } from "./appApi";
import { createPublicShareHash, getPublicShareTokenFromHash } from "./hashRouting";

export const publicShareUnavailableMessage = "This link is unavailable or has expired.";

export const defaultContentProtection: ShareLinkContentProtectionDto = {
  disableDownload: true,
  disablePrint: false,
  disableCopy: false,
  watermarkEnabled: false,
  watermarkText: "Public link",
};

export type PublicShareFailureState = {
  canRetryWithPassword: boolean;
  message: string;
};

export type PublicShareKnowledgeBaseState = {
  breadcrumb: PublicShareTreeNodeDto[];
  currentNode: PublicShareTreeNodeDto | null;
  documentOrder: PublicShareTreeNodeDto[];
  emptyState: "empty-scope" | "no-readable-documents" | null;
  nextDocument: PublicShareTreeNodeDto | null;
  orderedNodes: PublicShareTreeNodeDto[];
  previousDocument: PublicShareTreeNodeDto | null;
  scopeLabel: "Collection" | "Document" | "Library";
  scopeTitle: string;
};

export function getPublicShareReadEndpoint(resourceType: PublicShareResourceType) {
  return resourceType === "document" ? "document" : "tree";
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

export function normalizeContentProtection(value?: Partial<ShareLinkContentProtectionDto> | null): ShareLinkContentProtectionDto {
  return {
    disableDownload: typeof value?.disableDownload === "boolean" ? value.disableDownload : defaultContentProtection.disableDownload,
    disablePrint: typeof value?.disablePrint === "boolean" ? value.disablePrint : defaultContentProtection.disablePrint,
    disableCopy: typeof value?.disableCopy === "boolean" ? value.disableCopy : defaultContentProtection.disableCopy,
    watermarkEnabled: typeof value?.watermarkEnabled === "boolean" ? value.watermarkEnabled : defaultContentProtection.watermarkEnabled,
    watermarkText: sanitizeWatermarkText(value?.watermarkText),
  };
}

export function getContentProtectionLabels(value?: Partial<ShareLinkContentProtectionDto> | null) {
  const protection = normalizeContentProtection(value);
  return [
    protection.disableDownload ? "Download disabled" : "Download allowed",
    protection.disablePrint ? "Print disabled" : null,
    protection.disableCopy ? "Copy limited" : null,
    protection.watermarkEnabled ? "Watermark enabled" : null,
  ].filter((label): label is string => Boolean(label));
}

export function hasForbiddenContentProtectionSecretFields(value: Record<string, unknown>) {
  return ["token", "tokenHash", "token_hash", "password", "passwordHash", "password_hash", "passwordProof", "proof", "ip", "userAgent", "email"].some(
    (key) => key in value,
  );
}

export function hasForbiddenPublicShareTreeFields(value: Record<string, unknown>) {
  return [
    "workspaceId",
    "spaceId",
    "grants",
    "members",
    "permissions",
    "token",
    "tokenHash",
    "token_hash",
    "password",
    "passwordHash",
    "password_hash",
    "passwordProof",
    "proof",
    "url",
    "route",
  ].some((key) => key in value);
}

export function getPublicShareScopeLabel(resourceType?: string | null): PublicShareKnowledgeBaseState["scopeLabel"] {
  if (resourceType === "collection") {
    return "Collection";
  }

  if (resourceType === "library") {
    return "Library";
  }

  return "Document";
}

export function derivePublicShareKnowledgeBaseState(
  tree: Pick<PublicShareTreeResponse, "nodes" | "scopeType" | "title">,
  selectedDocumentId?: string | null,
): PublicShareKnowledgeBaseState {
  const orderedNodes = getOrderedPublicShareTreeNodes(tree.nodes);
  const documentOrder = orderedNodes.filter((node) => node.type === "document");
  const requestedNode = selectedDocumentId
    ? documentOrder.find((node) => node.id === selectedDocumentId) ?? null
    : null;
  const currentNode = requestedNode ?? documentOrder[0] ?? null;
  const currentIndex = currentNode ? documentOrder.findIndex((node) => node.id === currentNode.id) : -1;

  return {
    breadcrumb: currentNode ? getPublicShareBreadcrumb(orderedNodes, currentNode.id, tree.title, tree.scopeType) : createScopeBreadcrumb(tree.title, tree.scopeType),
    currentNode,
    documentOrder,
    emptyState: getPublicShareEmptyState(orderedNodes, documentOrder),
    nextDocument: currentIndex >= 0 ? documentOrder[currentIndex + 1] ?? null : null,
    orderedNodes,
    previousDocument: currentIndex > 0 ? documentOrder[currentIndex - 1] ?? null : null,
    scopeLabel: getPublicShareScopeLabel(tree.scopeType),
    scopeTitle: sanitizePublicShareTitle(tree.title, getPublicShareScopeLabel(tree.scopeType)),
  };
}

export function getOrderedPublicShareTreeNodes(nodes: PublicShareTreeNodeDto[]) {
  const safeNodes = nodes.filter(isSafePublicShareTreeNode);
  const nodeById = new Map(safeNodes.map((node) => [node.id, node]));
  const childrenByParent = new Map<string, PublicShareTreeNodeDto[]>();
  const roots: PublicShareTreeNodeDto[] = [];

  for (const node of safeNodes) {
    const parentId = node.parentId?.trim() || null;
    if (parentId && nodeById.has(parentId) && parentId !== node.id) {
      const siblings = childrenByParent.get(parentId) ?? [];
      siblings.push(node);
      childrenByParent.set(parentId, siblings);
    } else {
      roots.push(node);
    }
  }

  const sortNodes = (items: PublicShareTreeNodeDto[]) =>
    [...items].sort((left, right) => Number(left.sortOrder) - Number(right.sortOrder) || left.title.localeCompare(right.title));

  const ordered: PublicShareTreeNodeDto[] = [];
  const visit = (node: PublicShareTreeNodeDto) => {
    ordered.push(node);
    for (const child of sortNodes(childrenByParent.get(node.id) ?? [])) {
      visit(child);
    }
  };

  for (const root of sortNodes(roots)) {
    visit(root);
  }

  return ordered;
}

export function getPublicShareBreadcrumb(
  nodes: PublicShareTreeNodeDto[],
  documentId: string,
  scopeTitle: string,
  scopeType: PublicShareResourceType,
) {
  const nodeById = new Map(nodes.map((node) => [node.id, node]));
  const chain: PublicShareTreeNodeDto[] = [];
  let current = nodeById.get(documentId) ?? null;
  const seen = new Set<string>();

  while (current && !seen.has(current.id)) {
    chain.push(current);
    seen.add(current.id);
    current = current.parentId ? nodeById.get(current.parentId) ?? null : null;
  }

  return [...createScopeBreadcrumb(scopeTitle, scopeType), ...chain.reverse()];
}

export function getPublicShareTreeDepth(nodes: PublicShareTreeNodeDto[], node: PublicShareTreeNodeDto) {
  const nodeById = new Map(nodes.map((item) => [item.id, item]));
  let depth = 0;
  let current = node.parentId ? nodeById.get(node.parentId) ?? null : null;
  const seen = new Set<string>();

  while (current && !seen.has(current.id)) {
    depth += 1;
    seen.add(current.id);
    current = current.parentId ? nodeById.get(current.parentId) ?? null : null;
  }

  return depth;
}

export function getPublicShareEmptyState(nodes: PublicShareTreeNodeDto[], documentOrder: PublicShareTreeNodeDto[]) {
  if (nodes.length === 0) {
    return "empty-scope" as const;
  }

  if (documentOrder.length === 0) {
    return "no-readable-documents" as const;
  }

  return null;
}

export function sanitizeWatermarkText(value?: string | null) {
  const trimmed = value?.trim() || defaultContentProtection.watermarkText;
  const shortened = trimmed.length > 80 ? trimmed.slice(0, 80).trim() : trimmed;
  return /token|password|hash|proof|user-agent|useragent|\bip\b|@/i.test(shortened)
    ? defaultContentProtection.watermarkText
    : shortened;
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
    "/spaces/",
  ];

  const sourceWithoutPublicScopedDocuments = source.replace(/\/public\/share-links\/[^/\s]+\/documents\/[^/\s]+/g, "");
  return protectedEndpointPatterns.every((pattern) => !source.includes(pattern)) &&
    !/\/documents\/[^/\s]+/.test(sourceWithoutPublicScopedDocuments);
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
  const match = path.match(/^\/api\/v1\/(public\/)?share-links\/([^/]+)\/(?:resolve|document|collection|tree|documents\/[^/]+)$/i);
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

function createScopeBreadcrumb(scopeTitle: string, scopeType: PublicShareResourceType): PublicShareTreeNodeDto[] {
  return [
    {
      hasChildren: true,
      id: "__public_scope__",
      parentId: null,
      sortOrder: -1,
      title: sanitizePublicShareTitle(scopeTitle, getPublicShareScopeLabel(scopeType)),
      type: "collection",
      updatedAt: "",
    },
  ];
}

function isSafePublicShareTreeNode(node: PublicShareTreeNodeDto) {
  if (hasForbiddenPublicShareTreeFields(node as unknown as Record<string, unknown>)) {
    return false;
  }

  return Boolean(node.id?.trim()) &&
    Boolean(node.title?.trim()) &&
    (node.type === "collection" || node.type === "document");
}

function sanitizePublicShareTitle(value: string | null | undefined, fallback: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : fallback;
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
