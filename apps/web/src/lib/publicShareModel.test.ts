import { describe, expect, test } from "../test/harness";
import {
  buildPublicShareReaderUrl,
  derivePublicShareKnowledgeBaseState,
  extractShareTokenFromApiUrl,
  getContentProtectionLabels,
  getOrderedPublicShareTreeNodes,
  getPublicShareBreadcrumb,
  getPublicSharePasswordHeader,
  getPublicShareReadEndpoint,
  getPublicShareScopeLabel,
  hasForbiddenPublicShareTreeFields,
  hasForbiddenContentProtectionSecretFields,
  normalizeContentProtection,
  publicShareCodeUsesOnlyAllowedEndpoints,
  sanitizeWatermarkText,
  toUserFacingShareUrl,
  publicShareUnavailableMessage,
  toPublicShareFailureState,
} from "./publicShareModel";

describe("publicShareModel", () => {
  test("selects public document and collection read endpoints only", () => {
    expect(getPublicShareReadEndpoint("document")).toBe("document");
    expect(getPublicShareReadEndpoint("collection")).toBe("tree");
    expect(getPublicShareReadEndpoint("library")).toBe("tree");
  });

  test("keeps failure states generic for external users", () => {
    expect(toPublicShareFailureState({ hasPassword: true, passwordSubmitted: false })).toEqual({
      canRetryWithPassword: true,
      message: publicShareUnavailableMessage,
    });
    expect(toPublicShareFailureState({ hasPassword: true, passwordSubmitted: true })).toEqual({
      canRetryWithPassword: true,
      message: "This link is unavailable or has expired.",
    });
  });

  test("keeps password proof in the transient request header shape", () => {
    expect(getPublicSharePasswordHeader(" open ")).toEqual({ "X-Share-Link-Password": "open" });
    expect(getPublicSharePasswordHeader(" ")).toEqual({});
  });

  test("normalizes content protection defaults and token-safe watermark text", () => {
    expect(normalizeContentProtection(null)).toEqual({
      disableDownload: true,
      disablePrint: false,
      disableCopy: false,
      watermarkEnabled: false,
      watermarkText: "Public link",
    });
    expect(normalizeContentProtection({ disableCopy: true, watermarkEnabled: true, watermarkText: "Shared via Northstar" })).toMatchObject({
      disableCopy: true,
      watermarkEnabled: true,
      watermarkText: "Shared via Northstar",
    });
    expect(sanitizeWatermarkText("token password proof")).toBe("Public link");
    expect(getContentProtectionLabels({ disableCopy: true, watermarkEnabled: true })).toEqual([
      "Download disabled",
      "Copy limited",
      "Watermark enabled",
    ]);
    expect(hasForbiddenContentProtectionSecretFields({ watermarkText: "Public link" })).toBe(false);
    expect(hasForbiddenContentProtectionSecretFields({ token: "secret" })).toBe(true);
  });

  test("builds canonical frontend public reader URLs from create response tokens", () => {
    expect(buildPublicShareReaderUrl("public-token_123", "http://localhost:5173")).toBe(
      "http://localhost:5173/#public/share-links/public-token_123",
    );
    expect(
      toUserFacingShareUrl(
        "/api/v1/public/share-links/backend-token/resolve",
        "created-token",
        "public",
        "https://api.example.com/api/v1",
        "https://app.example.com",
      ),
    ).toBe("https://app.example.com/#public/share-links/created-token");
  });

  test("converts public API resolve URLs to frontend public reader URLs", () => {
    expect(
      toUserFacingShareUrl(
        "/api/v1/public/share-links/public-token_123/resolve",
        null,
        "public",
        "https://api.example.com/api/v1",
        "https://app.example.com",
      ),
    ).toBe("https://app.example.com/#public/share-links/public-token_123");
    expect(
      toUserFacingShareUrl(
        "/api/v1/share-links/public-token_123/resolve",
        null,
        "public",
        "https://api.example.com/api/v1",
        "https://app.example.com",
      ),
    ).toBe("https://app.example.com/#public/share-links/public-token_123");
    expect(extractShareTokenFromApiUrl("https://api.example.com/api/v1/public/share-links/public-token_123/document")).toBe(
      "public-token_123",
    );
  });

  test("keeps already frontend public route URLs and ignores non-share URLs", () => {
    const frontendUrl = "https://app.example.com/#public/share-links/public-token_123";
    expect(toUserFacingShareUrl(frontendUrl, null, "public", "https://api.example.com/api/v1", "https://app.example.com")).toBe(
      frontendUrl,
    );
    expect(extractShareTokenFromApiUrl("/api/v1/documents/public-token_123")).toBe(null);
    expect(toUserFacingShareUrl("/api/v1/documents/public-token_123", null, "public", "https://api.example.com/api/v1", "https://app.example.com")).toBe(
      "https://api.example.com/api/v1/documents/public-token_123",
    );
  });

  test("does not put password material in generated public URLs", () => {
    const url = toUserFacingShareUrl(
      "/api/v1/public/share-links/public-token_123/resolve?password=secret",
      null,
      "public",
      "https://api.example.com/api/v1",
      "https://app.example.com",
    );
    expect(url).toBe("https://app.example.com/#public/share-links/public-token_123");
    expect(url.includes("secret")).toBe(false);
    expect(url.includes("password")).toBe(false);
  });

  test("public share code model rejects protected API widening patterns", () => {
    expect(
      publicShareCodeUsesOnlyAllowedEndpoints(
        "/public/share-links/token/resolve /public/share-links/token/document /public/share-links/token/collection /public/share-links/token/tree /public/share-links/token/documents/doc",
      ),
    ).toBe(true);
    expect(publicShareCodeUsesOnlyAllowedEndpoints("/public/share-links/token/document /documents/doc-id/comments")).toBe(false);
  });

  test("derives collection knowledge base current node, breadcrumb, and previous next", () => {
    const nodes = [
      treeNode({ id: "doc-b", parentId: "collection-a", sortOrder: 2, title: "Beta", type: "document" }),
      treeNode({ id: "collection-a", parentId: null, sortOrder: 1, title: "Guides", type: "collection" }),
      treeNode({ id: "doc-a", parentId: "collection-a", sortOrder: 1, title: "Alpha", type: "document" }),
    ];
    const state = derivePublicShareKnowledgeBaseState({ nodes, scopeType: "collection", title: "Public Guides" }, "doc-b");

    expect(state.scopeLabel).toBe("Collection");
    expect(state.currentNode?.id).toBe("doc-b");
    expect(state.breadcrumb.map((node) => node.title)).toEqual(["Public Guides", "Guides", "Beta"]);
    expect(state.previousDocument?.id).toBe("doc-a");
    expect(state.nextDocument).toBe(null);
  });

  test("derives library tree order without crossing public scope", () => {
    const nodes = [
      treeNode({ id: "doc-2", parentId: "collection-2", sortOrder: 1, title: "Second", type: "document" }),
      treeNode({ id: "collection-2", parentId: null, sortOrder: 2, title: "Folder B", type: "collection" }),
      treeNode({ id: "doc-1", parentId: "collection-1", sortOrder: 1, title: "First", type: "document" }),
      treeNode({ id: "collection-1", parentId: null, sortOrder: 1, title: "Folder A", type: "collection" }),
    ];
    const state = derivePublicShareKnowledgeBaseState({ nodes, scopeType: "library", title: "Library KB" }, "doc-1");

    expect(state.scopeLabel).toBe("Library");
    expect(state.documentOrder.map((node) => node.id)).toEqual(["doc-1", "doc-2"]);
    expect(state.nextDocument?.id).toBe("doc-2");
    expect(state.orderedNodes.map((node) => node.id)).toEqual(["collection-1", "doc-1", "collection-2", "doc-2"]);
  });

  test("handles empty and unknown public tree states safely", () => {
    const empty = derivePublicShareKnowledgeBaseState({ nodes: [], scopeType: "collection", title: "Empty" }, null);
    expect(empty.emptyState).toBe("empty-scope");
    expect(empty.currentNode).toBe(null);

    const foldersOnly = derivePublicShareKnowledgeBaseState(
      { nodes: [treeNode({ id: "collection-1", parentId: null, sortOrder: 1, title: "Folder", type: "collection" })], scopeType: "library", title: "KB" },
      "missing",
    );
    expect(foldersOnly.emptyState).toBe("no-readable-documents");
    expect(foldersOnly.breadcrumb.map((node) => node.title)).toEqual(["KB"]);
  });

  test("guards forbidden public tree fields and invalid node types", () => {
    expect(hasForbiddenPublicShareTreeFields({ title: "Doc", tokenHash: "hash" })).toBe(true);
    expect(getOrderedPublicShareTreeNodes([
      treeNode({ id: "doc-1", parentId: null, sortOrder: 1, title: "Doc", type: "document" }),
      { ...treeNode({ id: "secret", parentId: null, sortOrder: 2, title: "Secret", type: "document" }), token: "raw" } as never,
      treeNode({ id: "library-node", parentId: null, sortOrder: 3, title: "Library", type: "library" }),
    ]).map((node) => node.id)).toEqual(["doc-1"]);
    expect(getPublicShareScopeLabel("workspace")).toBe("Document");
  });

  test("builds breadcrumb only from public-safe tree data", () => {
    const nodes = [
      treeNode({ id: "folder", parentId: null, sortOrder: 1, title: "Folder", type: "collection" }),
      treeNode({ id: "doc", parentId: "folder", sortOrder: 1, title: "Doc", type: "document" }),
    ];
    expect(getPublicShareBreadcrumb(nodes, "doc", "Library", "library").map((node) => node.title)).toEqual([
      "Library",
      "Folder",
      "Doc",
    ]);
  });
});

function treeNode(overrides: {
  id: string;
  parentId: string | null;
  sortOrder: number;
  title: string;
  type: "collection" | "document" | "library";
}) {
  return {
    hasChildren: overrides.type === "collection",
    id: overrides.id,
    parentId: overrides.parentId,
    sortOrder: overrides.sortOrder,
    title: overrides.title,
    type: overrides.type,
    updatedAt: "2026-05-14T00:00:00.000Z",
  };
}
