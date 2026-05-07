import type {
  BootstrapResponse,
  KnowledgeDocumentSummaryDto,
  KnowledgeFolderDto,
  KnowledgeMapResponse,
  SpaceDto,
} from "./appApi";
import { createEditorHash, createLibrariesHash } from "./hashRouting";

export type LibrarySortKey = "collection" | "status" | "title" | "updatedAt";

export type LibrariesPageFilters = {
  collectionId: string | null;
  libraryId: string | null;
  query?: string;
  sortKey?: LibrarySortKey;
  status?: string | null;
  tag?: string | null;
};

export type LibraryNavRow = {
  href: string;
  id: string;
  isActive: boolean;
  name: string;
};

export type LibraryCollectionRow = {
  canDelete: boolean;
  documentCount: number;
  href: string;
  id: string;
  isActive: boolean;
  sortOrder: number;
  title: string;
};

export type LibraryDocumentRow = {
  canArchive: boolean;
  canDelete: boolean;
  canMove: boolean;
  canRestore: boolean;
  collectionId: string;
  collectionTitle: string;
  hiddenTagCount: number;
  href: string;
  id: string;
  isArchived: boolean;
  moveOptions: LibraryMoveOption[];
  status: string;
  statusTone: "archived" | "draft" | "neutral" | "published";
  tags: string[];
  title: string;
  updatedAt: string;
  visibleTags: string[];
};

export type LibraryFilterOption = {
  count: number;
  id: string;
  label: string;
};

export type LibraryMoveOption = LibraryFilterOption & {
  isCurrent: boolean;
};

export type LibraryStatRow = {
  id: string;
  label: string;
  value: string;
};

export type LibrariesPageModel = {
  activeCollectionId: string | null;
  activeCollectionTitle: string | null;
  activeLibraryId: string | null;
  activeLibraryName: string;
  canCreateCollection: boolean;
  canCreateDocument: boolean;
  canDeleteActiveCollection: boolean;
  canRenameActiveCollection: boolean;
  canReorderActiveCollectionDown: boolean;
  canReorderActiveCollectionUp: boolean;
  collectionOptions: LibraryFilterOption[];
  collections: LibraryCollectionRow[];
  createDocumentDisabledReason: string | null;
  documents: LibraryDocumentRow[];
  hasCollections: boolean;
  hasLibraries: boolean;
  libraries: LibraryNavRow[];
  query: string;
  sortKey: LibrarySortKey;
  stats: LibraryStatRow[];
  statusFilter: string | null;
  statusOptions: LibraryFilterOption[];
  tagFilter: string | null;
  tagOptions: LibraryFilterOption[];
  totalDocumentCount: number;
  visibleDocumentCount: number;
  workspaceName: string;
};

export function createLibrariesPageModel(
  bootstrap: BootstrapResponse,
  map: KnowledgeMapResponse,
  filters: LibrariesPageFilters,
): LibrariesPageModel {
  const activeLibrary = getActiveLibrary(bootstrap, filters.libraryId);
  const activeLibraryId = activeLibrary?.id ?? null;
  const collectionTitlesById = createCollectionTitlesById(map.folders);
  const activeCollectionId = filters.collectionId && collectionTitlesById.has(filters.collectionId)
    ? filters.collectionId
    : null;
  const activeCollectionTitle = activeCollectionId ? collectionTitlesById.get(activeCollectionId) ?? null : null;
  const collectionOptions = createCollectionOptions(map.folders);
  const statusOptions = createStatusOptions(map.documents);
  const tagOptions = createTagOptions(map.documents);
  const statusFilter = normalizeSelectedOption(filters.status, statusOptions);
  const tagFilter = normalizeSelectedOption(filters.tag, tagOptions);
  const sortKey = filters.sortKey ?? "updatedAt";
  const query = filters.query?.trim() ?? "";
  const sortedCollections = [...map.folders].sort((left, right) => Number(left.sortOrder) - Number(right.sortOrder));
  const activeCollectionIndex = activeCollectionId
    ? sortedCollections.findIndex((collection) => collection.id === activeCollectionId)
    : -1;
  const documents = [...map.documents]
    .filter((document) =>
      matchesFilters(document, collectionTitlesById, {
        collectionId: activeCollectionId,
        query,
        status: statusFilter,
        tag: tagFilter,
      }),
    )
    .sort((left, right) => compareDocuments(left, right, sortKey, collectionTitlesById))
    .map((document) => toLibraryDocumentRow(document, collectionTitlesById, collectionOptions));
  const canCreateDocument = Boolean(activeCollectionId);

  return {
    activeCollectionId,
    activeCollectionTitle,
    activeLibraryId,
    activeLibraryName: activeLibrary?.name ?? "No library",
    canCreateCollection: Boolean(activeLibraryId),
    canCreateDocument,
    canDeleteActiveCollection: Boolean(activeCollectionId) &&
      (map.folders.find((collection) => collection.id === activeCollectionId)?.documentCount ?? 0) === 0,
    canRenameActiveCollection: Boolean(activeCollectionId),
    canReorderActiveCollectionDown: activeCollectionIndex >= 0 && activeCollectionIndex < sortedCollections.length - 1,
    canReorderActiveCollectionUp: activeCollectionIndex > 0,
    collectionOptions,
    collections: sortedCollections
      .map((collection) => toLibraryCollectionRow(collection, activeLibraryId, activeCollectionId)),
    createDocumentDisabledReason: canCreateDocument ? null : "Select a folder before creating a document.",
    documents,
    hasCollections: map.folders.length > 0,
    hasLibraries: bootstrap.spaces.length > 0,
    libraries: bootstrap.spaces.map((library) => toLibraryNavRow(library, activeLibraryId)),
    query,
    sortKey,
    stats: createStats(map),
    statusFilter,
    statusOptions,
    tagFilter,
    tagOptions,
    totalDocumentCount: map.documents.length,
    visibleDocumentCount: documents.length,
    workspaceName: bootstrap.workspace.name,
  };
}

export function getPreferredLibraryId(bootstrap: BootstrapResponse | null, requestedLibraryId: string | null) {
  if (!bootstrap || bootstrap.spaces.length === 0) {
    return null;
  }

  if (requestedLibraryId && bootstrap.spaces.some((library) => library.id === requestedLibraryId)) {
    return requestedLibraryId;
  }

  if (bootstrap.activeSpaceId && bootstrap.spaces.some((library) => library.id === bootstrap.activeSpaceId)) {
    return bootstrap.activeSpaceId;
  }

  if (bootstrap.workspace.currentSpaceId && bootstrap.spaces.some((library) => library.id === bootstrap.workspace.currentSpaceId)) {
    return bootstrap.workspace.currentSpaceId;
  }

  return bootstrap.spaces[0].id;
}

export function createCollectionReorderIds(
  collections: LibraryCollectionRow[],
  activeCollectionId: string | null,
  direction: "down" | "up",
) {
  if (!activeCollectionId) {
    return null;
  }

  const activeIndex = collections.findIndex((collection) => collection.id === activeCollectionId);
  const targetIndex = direction === "up" ? activeIndex - 1 : activeIndex + 1;
  if (activeIndex < 0 || targetIndex < 0 || targetIndex >= collections.length) {
    return null;
  }

  const orderedIds = collections.map((collection) => collection.id);
  [orderedIds[activeIndex], orderedIds[targetIndex]] = [orderedIds[targetIndex], orderedIds[activeIndex]];
  return orderedIds;
}

export function getCollectionIdAfterDelete(
  collections: LibraryCollectionRow[],
  deletedCollectionId: string | null,
) {
  if (!deletedCollectionId) {
    return null;
  }

  const deletedIndex = collections.findIndex((collection) => collection.id === deletedCollectionId);
  if (deletedIndex < 0) {
    return null;
  }

  return collections[deletedIndex + 1]?.id ?? collections[deletedIndex - 1]?.id ?? null;
}

function getActiveLibrary(bootstrap: BootstrapResponse, requestedLibraryId: string | null) {
  const preferredId = getPreferredLibraryId(bootstrap, requestedLibraryId);
  return bootstrap.spaces.find((library) => library.id === preferredId) ?? null;
}

function toLibraryNavRow(library: SpaceDto, activeLibraryId: string | null): LibraryNavRow {
  return {
    href: createLibrariesHash({ libraryId: library.id }),
    id: library.id,
    isActive: library.id === activeLibraryId,
    name: library.name,
  };
}

function toLibraryCollectionRow(
  collection: KnowledgeFolderDto,
  activeLibraryId: string | null,
  activeCollectionId: string | null,
): LibraryCollectionRow {
  const title = stripCollectionPrefix(collection.title);

  return {
    canDelete: collection.documentCount === 0,
    documentCount: collection.documentCount,
    href: createLibrariesHash({ collectionId: collection.id, libraryId: activeLibraryId }),
    id: collection.id,
    isActive: collection.id === activeCollectionId,
    sortOrder: Number(collection.sortOrder),
    title,
  };
}

function toLibraryDocumentRow(
  document: KnowledgeDocumentSummaryDto,
  collectionTitlesById: Map<string, string>,
  collectionOptions: LibraryFilterOption[],
): LibraryDocumentRow {
  const tags = document.tags ?? [];
  const isArchived = normalizeValue(document.status) === "archived";
  const moveOptions = collectionOptions.map((option) => ({
    ...option,
    isCurrent: option.id === document.folderId,
  }));

  return {
    canArchive: !isArchived,
    canDelete: true,
    canMove: moveOptions.some((option) => !option.isCurrent),
    canRestore: isArchived,
    collectionId: document.folderId,
    collectionTitle: collectionTitlesById.get(document.folderId) ?? "Unfiled",
    hiddenTagCount: Math.max(0, tags.length - 3),
    href: createEditorHash(document.id),
    id: document.id,
    isArchived,
    moveOptions,
    status: document.status || "Unknown",
    statusTone: getStatusTone(document.status),
    tags: document.tags,
    title: document.title,
    updatedAt: document.updatedAt,
    visibleTags: tags.slice(0, 3),
  };
}

function createCollectionTitlesById(collections: KnowledgeFolderDto[]) {
  return new Map(collections.map((collection) => [collection.id, stripCollectionPrefix(collection.title)]));
}

function createCollectionOptions(collections: KnowledgeFolderDto[]): LibraryFilterOption[] {
  return [...collections]
    .sort((left, right) => Number(left.sortOrder) - Number(right.sortOrder))
    .map((collection) => ({
      count: collection.documentCount,
      id: collection.id,
      label: stripCollectionPrefix(collection.title),
    }));
}

function createStatusOptions(documents: KnowledgeDocumentSummaryDto[]): LibraryFilterOption[] {
  const counts = new Map<string, { count: number; label: string }>();

  documents.forEach((document) => {
    const label = document.status?.trim() || "Unknown";
    const key = normalizeValue(label);
    const current = counts.get(key);
    counts.set(key, {
      count: (current?.count ?? 0) + 1,
      label: current?.label ?? label,
    });
  });

  return [...counts.entries()]
    .sort((left, right) => left[1].label.localeCompare(right[1].label))
    .map(([id, value]) => ({
      count: value.count,
      id,
      label: value.label,
    }));
}

function createTagOptions(documents: KnowledgeDocumentSummaryDto[]): LibraryFilterOption[] {
  const counts = new Map<string, { count: number; label: string }>();

  documents.forEach((document) => {
    (document.tags ?? []).forEach((tag) => {
      const label = tag.trim();
      if (!label) {
        return;
      }

      const key = normalizeValue(label);
      const current = counts.get(key);
      counts.set(key, {
        count: (current?.count ?? 0) + 1,
        label: current?.label ?? label,
      });
    });
  });

  return [...counts.entries()]
    .sort((left, right) => left[1].label.localeCompare(right[1].label))
    .map(([id, value]) => ({
      count: value.count,
      id,
      label: value.label,
    }));
}

function createStats(map: KnowledgeMapResponse): LibraryStatRow[] {
  const statusCounts = map.documents.reduce<Record<string, number>>((counts, document) => {
    const key = normalizeValue(document.status || "Unknown");
    counts[key] = (counts[key] ?? 0) + 1;
    return counts;
  }, {});
  const updatedSince = Date.now() - 30 * 24 * 60 * 60 * 1000;
  const updatedIn30Days = map.documents.filter((document) => {
    const updatedAt = new Date(document.updatedAt).getTime();
    return Number.isFinite(updatedAt) && updatedAt >= updatedSince;
  }).length;

  return [
    {
      id: "total-documents",
      label: "Total Documents",
      value: String(map.documents.length),
    },
    {
      id: "collections",
      label: "Folders",
      value: String(map.folders.length),
    },
    {
      id: "published-documents",
      label: "Published",
      value: String(statusCounts.published ?? 0),
    },
    {
      id: "draft-documents",
      label: "Drafts",
      value: String(statusCounts.draft ?? 0),
    },
    {
      id: "archived-documents",
      label: "Archived",
      value: String(statusCounts.archived ?? 0),
    },
    {
      id: "updated-30-days",
      label: "Updated in 30 days",
      value: String(updatedIn30Days),
    },
  ];
}

function matchesFilters(
  document: KnowledgeDocumentSummaryDto,
  collectionTitlesById: Map<string, string>,
  filters: {
    collectionId: string | null;
    query: string;
    status: string | null;
    tag: string | null;
  },
) {
  if (filters.collectionId && document.folderId !== filters.collectionId) {
    return false;
  }

  if (filters.status && normalizeValue(document.status || "Unknown") !== filters.status) {
    return false;
  }

  if (filters.tag && !(document.tags ?? []).some((tag) => normalizeValue(tag) === filters.tag)) {
    return false;
  }

  if (!filters.query) {
    return true;
  }

  const query = normalizeValue(filters.query);
  const collectionTitle = collectionTitlesById.get(document.folderId) ?? "";
  const searchable = [
    document.title,
    collectionTitle,
    ...(document.tags ?? []),
  ].map(normalizeValue);

  return searchable.some((value) => value.includes(query));
}

function compareDocuments(
  left: KnowledgeDocumentSummaryDto,
  right: KnowledgeDocumentSummaryDto,
  sortKey: LibrarySortKey,
  collectionTitlesById: Map<string, string>,
) {
  if (sortKey === "title") {
    return compareText(left.title, right.title);
  }

  if (sortKey === "status") {
    return compareText(left.status, right.status) || compareText(left.title, right.title);
  }

  if (sortKey === "collection") {
    return (
      compareText(collectionTitlesById.get(left.folderId) ?? "", collectionTitlesById.get(right.folderId) ?? "")
      || compareText(left.title, right.title)
    );
  }

  return new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime();
}

function normalizeSelectedOption(value: string | null | undefined, options: LibraryFilterOption[]) {
  if (!value) {
    return null;
  }

  const normalized = normalizeValue(value);
  return options.some((option) => option.id === normalized) ? normalized : null;
}

function getStatusTone(status: string): LibraryDocumentRow["statusTone"] {
  const normalized = normalizeValue(status);

  if (normalized === "published") {
    return "published";
  }

  if (normalized === "draft") {
    return "draft";
  }

  if (normalized === "archived") {
    return "archived";
  }

  return "neutral";
}

function compareText(left: string, right: string) {
  return left.localeCompare(right, undefined, { sensitivity: "base" });
}

function normalizeValue(value: string) {
  return value.trim().toLowerCase();
}

function stripCollectionPrefix(title: string) {
  return title.replace(/^\d+\.\s*/, "");
}
