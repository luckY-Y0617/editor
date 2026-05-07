import type { BootstrapResponse, KnowledgeDocumentSummaryDto, SearchResponse } from "./appApi";
import { createEditorHash, createLibrariesHash } from "./hashRouting";

export type SearchApiStatus = "idle" | "unconfigured" | "loading" | "ready" | "forbidden" | "error";

export type SearchDisplayResult = {
  collaboratorCount?: number;
  collectionCount?: number;
  documentCount?: number;
  excerpt?: string;
  href: string;
  id: string;
  owner?: string;
  path: string;
  selected?: boolean;
  status?: "Published" | "Draft";
  title: string;
  type: "document" | "collection" | "person";
  updatedAt?: string;
};

export function getSearchStatusLabel(status: SearchApiStatus) {
  if (status === "ready") {
    return "Live search API is connected.";
  }

  if (status === "loading") {
    return "Loading search results.";
  }

  if (status === "idle") {
    return "Enter a search term to search Northstar.";
  }

  if (status === "forbidden") {
    return "Sign in to load live search results.";
  }

  if (status === "error") {
    return "Search API unavailable; live results could not be loaded.";
  }

  return "Configure VITE_NORTHSTAR_API_BASE_URL to load live search results.";
}

export function getSearchEmptyState(status: SearchApiStatus) {
  if (status === "idle") {
    return {
      detail: "Type a query in the search field above and press Enter.",
      title: "Start a search",
    };
  }

  if (status === "loading") {
    return {
      detail: "Live results are being loaded.",
      title: "Loading results...",
    };
  }

  if (status === "forbidden") {
    return {
      detail: "Your session must be authenticated before live results can load.",
      title: "Sign in to load live search results.",
    };
  }

  if (status === "error") {
    return {
      detail: "No demo data is shown while the API is configured.",
      title: "Live search results could not be loaded.",
    };
  }

  return {
    detail: "No matching documents were returned.",
    title: "No results found.",
  };
}

export function toFolderSearchResultItems(
  bootstrap: BootstrapResponse,
  folderId: string,
  query: string,
): SearchDisplayResult[] {
  const folder = bootstrap.folders.find((item) => item.id === folderId);
  return bootstrap.documents
    .filter((document) => document.folderId === folderId)
    .filter((document) => matchesFolderQuery(document, folder?.title ?? "", query))
    .map((document, index) => ({
      excerpt: document.tags.length > 0 ? `Tags: ${document.tags.join(", ")}` : "",
      href: createEditorHash(document.id),
      id: document.id,
      path: folder ? folder.title : "Collection",
      selected: index === 0,
      status: normalizeStatus(document.status),
      title: document.title,
      type: "document",
      updatedAt: formatDate(document.updatedAt),
    }));
}

export function toSearchResultItems(response: SearchResponse, bootstrap: BootstrapResponse): SearchDisplayResult[] {
  const folderTitlesById = new Map(bootstrap.folders.map((folder) => [folder.id, folder.title]));

  return response.results.map((result, index) => {
    const type = result.type === "collection" || result.type === "person" ? result.type : "document";
    const folderTitle = result.folderId ? folderTitlesById.get(result.folderId) : null;

    return {
      excerpt: result.excerpt,
      href:
        type === "document"
          ? createEditorHash(result.id)
          : type === "collection"
            ? createLibrariesHash({ collectionId: result.id, libraryId: bootstrap.activeSpaceId })
            : "#workspace-members",
      id: result.id,
      path: folderTitle ?? (type === "collection" ? "Collection" : "Workspace"),
      selected: index === 0,
      status: type === "document" ? "Published" : undefined,
      title: result.title,
      type,
      updatedAt: formatDate(result.updatedAt),
    };
  });
}

function matchesFolderQuery(document: KnowledgeDocumentSummaryDto, folderTitle: string, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }

  const searchable = [document.title, folderTitle, document.status, ...document.tags]
    .join(" ")
    .toLowerCase();
  return searchable.includes(normalizedQuery);
}

function normalizeStatus(value: string): "Published" | "Draft" {
  return value.toLowerCase() === "published" ? "Published" : "Draft";
}

function formatDate(value: string) {
  try {
    return new Intl.DateTimeFormat("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
    }).format(new Date(value));
  } catch {
    return value;
  }
}
