import { describe, expect, test } from "../test/harness";
import {
  getPublicShareCollection,
  getPublicShareDocument,
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
