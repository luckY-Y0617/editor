import { describe, expect, test } from "../test/harness";
import type { BootstrapResponse, SearchResponse } from "./appApi";
import {
  getSearchEmptyState,
  getSearchResultCountLabel,
  getSearchScopeState,
  getSearchStatusLabel,
  toFolderSearchResultItems,
  toSearchResultItems,
} from "./searchDiscoveryModel";

const documentId = "11111111-1111-4111-8111-111111111111";
const folderId = "22222222-2222-4222-8222-222222222222";
const libraryId = "33333333-3333-4333-8333-333333333333";

describe("searchDiscoveryModel", () => {
  test("returns stable copy for idle and error states", () => {
    expect(getSearchStatusLabel("ready")).toBe("Live document search API is connected.");
    expect(getSearchStatusLabel("idle")).toBe("Enter a search term to search Library content.");
    expect(getSearchResultCountLabel({ isDemo: false, resultCount: 0, status: "idle" })).toBe("Ready to search");
    expect(getSearchResultCountLabel({ isDemo: false, resultCount: 3, status: "ready" })).toBe("3 results");
    expect(getSearchResultCountLabel({ isDemo: true, resultCount: 2, status: "unconfigured" })).toBe("2 demo results");
    expect(getSearchEmptyState("error")).toEqual({
      detail: "No demo data is shown while the API is configured.",
      title: "Live search results could not be loaded.",
    });
  });

  test("returns route-aware empty state for folder-scoped search", () => {
    expect(getSearchEmptyState("ready", { folderId, folderTitle: "Foundations", libraryId, query: "risk" })).toEqual({
      actionHref: `#search?libraryId=${libraryId}&q=risk`,
      actionLabel: "Search all Library content",
      detail: 'No documents in Foundations matched "risk".',
      title: "No folder results found.",
    });
  });

  test("returns folder scope links without exposing collection terminology", () => {
    expect(getSearchScopeState({ folderId, folderTitle: "Foundations", libraryId, query: "strategy" })).toEqual({
      allResultsHref: `#search?libraryId=${libraryId}&q=strategy`,
      folderHref: `#libraries?libraryId=${libraryId}&collectionId=${folderId}`,
      label: "Searching in folder: Foundations",
    });
  });

  test("filters collection deep-link results by query", () => {
    const results = toFolderSearchResultItems(createBootstrap(), folderId, "strategy");

    expect(results.length).toBe(1);
    expect(results[0]).toMatchObject({
      href: `#editor?documentId=${documentId}`,
      path: "Atlas / Foundations",
      title: "Strategy Memo",
    });
  });

  test("maps live search result routes using bootstrap context", () => {
    const response: SearchResponse = {
      results: [
        {
          excerpt: "Match",
          folderId,
          id: documentId,
          title: "Strategy Memo",
          type: "document",
          updatedAt: "2024-02-01T00:00:00.000Z",
        },
        {
          excerpt: "",
          folderId: "",
          id: folderId,
          title: "Foundations",
          type: "collection",
          updatedAt: "2024-02-01T00:00:00.000Z",
        },
      ],
    };

    const results = toSearchResultItems(response, createBootstrap());

    expect(results.map((result) => result.href)).toEqual([
      `#editor?documentId=${documentId}`,
      `#libraries?libraryId=${libraryId}&collectionId=${folderId}`,
    ]);
    expect(results[0]).toMatchObject({
      path: "Atlas / Foundations",
      selected: true,
      status: "Draft",
    });
    expect(results[1]).toMatchObject({
      documentCount: 2,
      path: "Atlas",
      selected: false,
      status: undefined,
    });
  });

  test("preserves requested library context for folder result routes", () => {
    const requestedLibraryId = "55555555-5555-4555-8555-555555555555";
    const response: SearchResponse = {
      results: [
        {
          excerpt: "",
          folderId: "",
          id: folderId,
          title: "Foundations",
          type: "collection",
          updatedAt: "2024-02-01T00:00:00.000Z",
        },
      ],
    };

    expect(toSearchResultItems(response, createBootstrap(), requestedLibraryId)[0]?.href).toBe(
      `#libraries?libraryId=${requestedLibraryId}&collectionId=${folderId}`,
    );
  });

  test("drops unsupported live search entities instead of routing them to workspace admin", () => {
    const response: SearchResponse = {
      results: [
        {
          excerpt: "",
          folderId: "",
          id: "alice-kim",
          title: "Alice Kim",
          type: "person",
          updatedAt: "2024-02-01T00:00:00.000Z",
        },
        {
          excerpt: "",
          folderId,
          id: "not-a-uuid",
          title: "Broken document",
          type: "document",
          updatedAt: "2024-02-01T00:00:00.000Z",
        },
      ],
    };

    expect(toSearchResultItems(response, createBootstrap())).toEqual([]);
  });
});

function createBootstrap(): BootstrapResponse {
  return {
    activeDocumentId: documentId,
    activeSpaceId: libraryId,
    documents: [
      {
        folderId,
        id: documentId,
        sortOrder: 1,
        status: "Draft",
        tags: ["Strategy"],
        title: "Strategy Memo",
        updatedAt: "2024-02-01T00:00:00.000Z",
      },
      {
        folderId,
        id: "44444444-4444-4444-8444-444444444444",
        sortOrder: 2,
        status: "Published",
        tags: ["Hiring"],
        title: "Hiring Plan",
        updatedAt: "2024-01-01T00:00:00.000Z",
      },
    ],
    folders: [
      {
        documentCount: 2,
        id: folderId,
        sortOrder: 1,
        title: "Foundations",
      },
    ],
    spaces: [{ id: libraryId, name: "Atlas" }],
    workspace: {
      currentSpaceId: libraryId,
      id: "workspace-1",
      name: "Northstar",
      organizationId: "organization-1",
    },
  };
}
