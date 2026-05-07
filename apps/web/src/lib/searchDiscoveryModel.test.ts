import { describe, expect, test } from "../test/harness";
import type { BootstrapResponse, SearchResponse } from "./appApi";
import {
  getSearchEmptyState,
  getSearchStatusLabel,
  toFolderSearchResultItems,
  toSearchResultItems,
} from "./searchDiscoveryModel";

const documentId = "11111111-1111-4111-8111-111111111111";
const folderId = "22222222-2222-4222-8222-222222222222";
const libraryId = "33333333-3333-4333-8333-333333333333";

describe("searchDiscoveryModel", () => {
  test("returns stable copy for idle and error states", () => {
    expect(getSearchStatusLabel("idle")).toBe("Enter a search term to search Northstar.");
    expect(getSearchEmptyState("error")).toEqual({
      detail: "No demo data is shown while the API is configured.",
      title: "Live search results could not be loaded.",
    });
  });

  test("filters collection deep-link results by query", () => {
    const results = toFolderSearchResultItems(createBootstrap(), folderId, "strategy");

    expect(results.length).toBe(1);
    expect(results[0]).toMatchObject({
      href: `#editor?documentId=${documentId}`,
      path: "Foundations",
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

    expect(toSearchResultItems(response, createBootstrap()).map((result) => result.href)).toEqual([
      `#editor?documentId=${documentId}`,
      `#libraries?libraryId=${libraryId}&collectionId=${folderId}`,
    ]);
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
