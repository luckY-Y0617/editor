import { describe, expect, test } from "../test/harness";
import {
  getPublicShareCollection,
  getPublicShareDocument,
  getPublicShareTree,
  getScopedPublicShareDocument,
  resolvePublicShareLink,
} from "./appApi";

describe("appApi public share", () => {
  test("sends password proof on anonymous public resolve requests", async () => {
    const requests: CapturedRequest[] = [];
    await resolvePublicShareLink("token-123", {
      apiBaseUrl: "https://northstar.test/api/v1",
      fetchFn: jsonFetch({ resourceType: "document" }, requests),
      password: " open ",
    });
    const captured = getCapturedRequest(requests);

    expect(captured.input).toBe("https://northstar.test/api/v1/public/share-links/token-123/resolve");
    expect(captured.headers.get("X-Share-Link-Password")).toBe("open");
    expect(captured.headers.has("Authorization")).toBe(false);
    expect(captured.input.includes("open")).toBe(false);
  });

  test("reads public documents through the dedicated public endpoint", async () => {
    const requests: CapturedRequest[] = [];
    await getPublicShareDocument("doc-token", {
      apiBaseUrl: "https://northstar.test/api/v1",
      fetchFn: jsonFetch(
        {
          document: {
            content: { type: "doc", content: [] },
            id: "doc",
            revision: 1,
            status: "draft",
            tags: [],
            title: "Plan",
            updatedAt: "2026-05-14T00:00:00.000Z",
          },
          link: {
            audience: "public",
            expiresAt: "2026-06-01T00:00:00.000Z",
            hasPassword: false,
            resourceId: "doc",
            resourceType: "document",
            roleKey: "viewer",
            workspaceId: "workspace",
          },
        },
        requests,
      ),
    });
    const captured = getCapturedRequest(requests);

    expect(captured.input).toBe("https://northstar.test/api/v1/public/share-links/doc-token/document");
    expect(captured.headers.has("Authorization")).toBe(false);
  });

  test("reads public collections through the dedicated public endpoint", async () => {
    const requests: CapturedRequest[] = [];
    await getPublicShareCollection("collection-token", {
      apiBaseUrl: "https://northstar.test/api/v1",
      fetchFn: jsonFetch(
        {
          collection: {
            documents: [],
            id: "collection",
            sortOrder: 1,
            title: "Folder",
            updatedAt: "2026-05-14T00:00:00.000Z",
          },
          link: {
            audience: "public",
            expiresAt: "2026-06-01T00:00:00.000Z",
            hasPassword: false,
            resourceId: "collection",
            resourceType: "collection",
            roleKey: "viewer",
            workspaceId: "workspace",
          },
        },
        requests,
      ),
    });
    const captured = getCapturedRequest(requests);

    expect(captured.input).toBe("https://northstar.test/api/v1/public/share-links/collection-token/collection");
    expect(captured.headers.has("Authorization")).toBe(false);
  });

  test("reads public scope trees through the dedicated anonymous endpoint", async () => {
    const requests: CapturedRequest[] = [];
    await getPublicShareTree("collection-token", {
      apiBaseUrl: "https://northstar.test/api/v1",
      fetchFn: jsonFetch(
        {
          link: {
            audience: "public",
            expiresAt: "2026-06-01T00:00:00.000Z",
            hasPassword: false,
            resourceId: "collection",
            resourceType: "collection",
            roleKey: "viewer",
            workspaceId: "workspace",
          },
          nodes: [],
          scopeType: "collection",
          shareLinkId: "share-link",
          title: "Folder",
        },
        requests,
      ),
    });
    const captured = getCapturedRequest(requests);

    expect(captured.input).toBe("https://northstar.test/api/v1/public/share-links/collection-token/tree");
    expect(captured.headers.has("Authorization")).toBe(false);
  });

  test("reads scoped public documents through the dedicated anonymous endpoint", async () => {
    const requests: CapturedRequest[] = [];
    await getScopedPublicShareDocument("collection-token", "doc-1", {
      apiBaseUrl: "https://northstar.test/api/v1",
      fetchFn: jsonFetch(
        {
          document: {
            content: { type: "doc", content: [] },
            id: "doc-1",
            revision: 1,
            status: "published",
            tags: [],
            title: "Plan",
            updatedAt: "2026-05-14T00:00:00.000Z",
          },
          link: {
            audience: "public",
            expiresAt: "2026-06-01T00:00:00.000Z",
            hasPassword: false,
            resourceId: "collection",
            resourceType: "collection",
            roleKey: "viewer",
            workspaceId: "workspace",
          },
        },
        requests,
      ),
      password: " open ",
    });
    const captured = getCapturedRequest(requests);

    expect(captured.input).toBe("https://northstar.test/api/v1/public/share-links/collection-token/documents/doc-1");
    expect(captured.headers.get("X-Share-Link-Password")).toBe("open");
    expect(captured.headers.has("Authorization")).toBe(false);
    expect(captured.input.includes("open")).toBe(false);
  });

  test("does not place public share passwords in URL or localStorage", async () => {
    const requests: CapturedRequest[] = [];
    const storageWrites: string[] = [];
    const originalLocalStorage = Object.getOwnPropertyDescriptor(globalThis, "localStorage");
    Object.defineProperty(globalThis, "localStorage", {
      configurable: true,
      value: {
        getItem: () => null,
        removeItem: () => undefined,
        setItem: (key: string, value: string) => storageWrites.push(`${key}:${value}`),
      },
    });

    try {
      await resolvePublicShareLink("token-123", {
        apiBaseUrl: "https://northstar.test/api/v1",
        fetchFn: jsonFetch({ resourceType: "document" }, requests),
        password: "secret-proof",
      });
    } finally {
      if (originalLocalStorage) {
        Object.defineProperty(globalThis, "localStorage", originalLocalStorage);
      }
    }

    const captured = getCapturedRequest(requests);
    expect(captured.input.includes("secret-proof")).toBe(false);
    expect(captured.headers.get("X-Share-Link-Password")).toBe("secret-proof");
    expect(storageWrites).toEqual([]);
  });
});

type CapturedRequest = {
  headers: Headers;
  input: string;
};

function getCapturedRequest(requests: CapturedRequest[]) {
  const [request] = requests;
  if (!request) {
    throw new Error("Expected a public share request.");
  }

  return request;
}

function jsonFetch(body: unknown, requests: CapturedRequest[]): typeof fetch {
  return async (input, init) => {
    requests.push({
      headers: new Headers(init?.headers),
      input: String(input),
    });

    return new Response(JSON.stringify(body), {
      headers: { "Content-Type": "application/json" },
      status: 200,
    });
  };
}
