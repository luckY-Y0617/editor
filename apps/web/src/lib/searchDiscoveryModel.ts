import type { BootstrapResponse, KnowledgeDocumentSummaryDto, SearchResponse } from "./appApi";
import { isUuid } from "./apiClient";
import { createEditorHash, createLibrariesHash, createSearchHash } from "./hashRouting";

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
  type: "document" | "collection";
  updatedAt?: string;
};

export type SearchResultCountLabelOptions = {
  isDemo: boolean;
  resultCount: number;
  status: SearchApiStatus;
};

export type SearchEmptyStateOptions = {
  folderId?: string | null;
  folderTitle?: string | null;
  libraryId?: string | null;
  query?: string | null;
};

export type SearchEmptyState = {
  actionHref?: string;
  actionLabel?: string;
  detail: string;
  title: string;
};

export type SearchScopeState = {
  allResultsHref: string;
  folderHref: string | null;
  label: string;
};

export function getSearchStatusLabel(status: SearchApiStatus) {
  if (status === "ready") {
    return "Live document search API is connected.";
  }

  if (status === "loading") {
    return "Loading search results.";
  }

  if (status === "idle") {
    return "Enter a search term to search Library content.";
  }

  if (status === "forbidden") {
    return "Sign in to load live search results.";
  }

  if (status === "error") {
    return "Search API unavailable; live results could not be loaded.";
  }

  return "Configure VITE_NORTHSTAR_API_BASE_URL to load live search results.";
}

export function getSearchResultCountLabel(options: SearchResultCountLabelOptions) {
  if (options.status === "loading") {
    return "Loading results";
  }

  if (options.status === "idle") {
    return "Ready to search";
  }

  const suffix = options.resultCount === 1 ? "result" : "results";
  return options.isDemo
    ? `${options.resultCount} demo ${suffix}`
    : `${options.resultCount} ${suffix}`;
}

export function getSearchEmptyState(status: SearchApiStatus, options: SearchEmptyStateOptions = {}): SearchEmptyState {
  const query = options.query?.trim();
  const folderTitle = options.folderTitle?.trim();

  if (status === "idle") {
    if (folderTitle) {
      return {
        detail: `Type a query to search documents inside ${folderTitle}.`,
        title: "Search this folder",
      };
    }

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

  if (options.folderId || folderTitle) {
    return {
      actionHref: createSearchHash({ libraryId: options.libraryId, q: query }),
      actionLabel: "Search all Library content",
      detail: query
        ? `No documents in ${folderTitle ?? "this folder"} matched "${query}".`
        : `No documents are available in ${folderTitle ?? "this folder"}.`,
      title: "No folder results found.",
    };
  }

  return {
    detail: query
      ? `No documents or folders matched "${query}" in the current Library.`
      : "No matching documents or folders were returned.",
    title: "No results found.",
  };
}

export function getSearchScopeState(options: SearchEmptyStateOptions): SearchScopeState | null {
  if (!options.folderId && !options.folderTitle) {
    return null;
  }

  const folderTitle = options.folderTitle?.trim() || "this folder";

  return {
    allResultsHref: createSearchHash({ libraryId: options.libraryId, q: options.query?.trim() }),
    folderHref: options.folderId ? createLibrariesHash({ collectionId: options.folderId, libraryId: options.libraryId }) : null,
    label: `Searching in folder: ${folderTitle}`,
  };
}

export function toFolderSearchResultItems(
  bootstrap: BootstrapResponse,
  folderId: string,
  query: string,
): SearchDisplayResult[] {
  const folder = bootstrap.folders.find((item) => item.id === folderId);
  const libraryName = getLibraryName(bootstrap, bootstrap.activeSpaceId);
  const folderTitle = folder?.title ?? "Folder";
  return bootstrap.documents
    .filter((document) => document.folderId === folderId)
    .filter((document) => matchesFolderQuery(document, folderTitle, query))
    .map((document, index) => ({
      excerpt: document.tags.length > 0 ? `Tags: ${document.tags.join(", ")}` : "",
      href: createEditorHash(document.id),
      id: document.id,
      path: `${libraryName} / ${folderTitle}`,
      selected: index === 0,
      status: normalizeStatus(document.status),
      title: document.title,
      type: "document",
      updatedAt: formatDate(document.updatedAt),
    }));
}

export function toSearchResultItems(
  response: SearchResponse,
  bootstrap: BootstrapResponse,
  libraryId?: string | null,
): SearchDisplayResult[] {
  const folderTitlesById = new Map(bootstrap.folders.map((folder) => [folder.id, folder.title]));
  const foldersById = new Map(bootstrap.folders.map((folder) => [folder.id, folder]));
  const documentsById = new Map(bootstrap.documents.map((document) => [document.id, document]));
  const resolvedLibraryId = libraryId && isUuid(libraryId) ? libraryId : bootstrap.activeSpaceId;
  const libraryName = getLibraryName(bootstrap, resolvedLibraryId);

  return response.results.flatMap((result) => {
    if (!isUuid(result.id)) {
      return [];
    }

    if (result.type !== "document" && result.type !== "collection") {
      return [];
    }

    const type: SearchDisplayResult["type"] = result.type === "collection" ? "collection" : "document";
    const folderTitle = result.folderId ? folderTitlesById.get(result.folderId) : null;
    const collection = type === "collection" ? foldersById.get(result.id) : null;
    const document = type === "document" ? documentsById.get(result.id) : null;

    return [{
      collectionCount: undefined,
      documentCount: collection?.documentCount,
      excerpt: result.excerpt,
      href:
        type === "document"
          ? createEditorHash(result.id)
          : createLibrariesHash({ collectionId: result.id, libraryId: resolvedLibraryId }),
      id: result.id,
      path: folderTitle ? `${libraryName} / ${folderTitle}` : libraryName,
      status: document ? normalizeStatus(document.status) : undefined,
      title: result.title,
      type,
      updatedAt: formatDate(result.updatedAt),
    }];
  }).map((result, index) => ({
    ...result,
    selected: index === 0,
  }));
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

function getLibraryName(bootstrap: BootstrapResponse, libraryId?: string | null) {
  const library = bootstrap.spaces.find((space) => space.id === libraryId)
    ?? bootstrap.spaces.find((space) => space.id === bootstrap.activeSpaceId)
    ?? bootstrap.spaces.find((space) => space.id === bootstrap.workspace.currentSpaceId);

  return library?.name ?? "Library";
}
