import {
  Archive,
  AlertTriangle,
  BookOpen,
  CalendarClock,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  Edit3,
  ExternalLink,
  FileText,
  FolderPlus,
  Grid2X2,
  Layers3,
  Library,
  List,
  Plus,
  RefreshCw,
  RotateCcw,
  Search,
  Settings,
  Trash2,
  X,
  type LucideIcon,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import {
  archiveDocument,
  createCollection,
  createDocument,
  deleteCollection,
  deleteDocument,
  getBootstrap,
  getSpaceMap,
  moveDocument,
  reorderCollections,
  restoreDocument,
  updateCollection,
  type BootstrapResponse,
  type KnowledgeMapResponse,
} from "../lib/appApi";
import {
  createLibrariesPageModel,
  createCollectionReorderIds,
  getCollectionIdAfterDelete,
  getPreferredLibraryId,
  type LibrariesPageModel,
  type LibraryDocumentRow,
  type LibrarySortKey,
  type LibraryStatRow,
} from "../lib/librariesPageModel";
import { createEditorHash, createLibrariesHash, createSettingsHash, getLibrariesFiltersFromHash } from "../lib/hashRouting";
import { t, type DisplayLocale, useDisplayLanguage } from "../lib/i18n";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";
import dashedRoutePatternUrl from "../assets/svg/decorative/dashed-route-lines.svg";

type DataStatus = "idle" | "unconfigured" | "loading" | "ready" | "forbidden" | "error";
type ViewMode = "grid" | "list";
type DocumentOperation = "archive" | "delete" | "move" | "restore";
type CollectionOperation = "create" | "delete" | "rename" | "reorder";
type DocumentMutationState = {
  documentId: string;
  operation: DocumentOperation;
} | null;
type CollectionMutationState = {
  collectionId: string | null;
  operation: CollectionOperation;
} | null;
type DocumentActionMessage = {
  kind: "error" | "success";
  text: string;
} | null;
type LibraryActionPanelState =
  | { kind: "create-collection"; title: string }
  | { collectionId: string; kind: "delete-collection"; title: string; documentCount: number }
  | { document: LibraryDocumentRow; kind: "delete-document" }
  | { collectionId: string; kind: "rename-collection"; title: string }
  | null;

const librariesPatternStyle = {
  "--workspace-home-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--workspace-home-route-pattern": `url(${routePatternUrl})`,
  "--workspace-home-bottom-route-pattern": `url(${dashedRoutePatternUrl})`,
  "--workspace-home-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

const statIcons: Record<string, LucideIcon> = {
  "archived-documents": Archive,
  collections: Layers3,
  "draft-documents": BookOpen,
  "published-documents": CheckCircle2,
  "total-documents": FileText,
  "updated-30-days": CalendarClock,
};

export function LibrariesPage() {
  const { locale } = useDisplayLanguage();
  const [hash, setHash] = useState(window.location.hash);
  const [bootstrapRetryKey, setBootstrapRetryKey] = useState(0);
  const [mapRetryKey, setMapRetryKey] = useState(0);
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [tagFilter, setTagFilter] = useState<string | null>(null);
  const [sortKey, setSortKey] = useState<LibrarySortKey>("updatedAt");
  const [viewMode, setViewMode] = useState<ViewMode>("grid");
  const [createStatus, setCreateStatus] = useState<"idle" | "creating" | "error">("idle");
  const [createError, setCreateError] = useState<string | null>(null);
  const [documentMutation, setDocumentMutation] = useState<DocumentMutationState>(null);
  const [collectionMutation, setCollectionMutation] = useState<CollectionMutationState>(null);
  const [documentActionMessage, setDocumentActionMessage] = useState<DocumentActionMessage>(null);
  const [collectionActionMessage, setCollectionActionMessage] = useState<DocumentActionMessage>(null);
  const [actionPanel, setActionPanel] = useState<LibraryActionPanelState>(null);
  const filters = getLibrariesFiltersFromHash(hash);
  const bootstrap = useBootstrapData(bootstrapRetryKey);
  const selectedLibraryId = getPreferredLibraryId(bootstrap.data, filters.libraryId);
  const map = useLibraryMapData(selectedLibraryId, bootstrap.status, mapRetryKey);
  const model = useMemo(() => {
    if (bootstrap.status !== "ready" || !bootstrap.data || map.status !== "ready" || !map.data) {
      return null;
    }

    return createLibrariesPageModel(bootstrap.data, map.data, {
      collectionId: filters.collectionId,
      libraryId: filters.libraryId,
      query: searchQuery,
      sortKey,
      status: statusFilter,
      tag: tagFilter,
    });
  }, [
    bootstrap.data,
    bootstrap.status,
    filters.collectionId,
    filters.libraryId,
    map.data,
    map.status,
    searchQuery,
    sortKey,
    statusFilter,
    tagFilter,
  ]);
  const sidebarCollections = model
    ? model.collections.slice(0, 8).map((collection) => ({
        displayTitle: collection.title,
        documentCount: collection.documentCount,
        href: collection.href,
        id: collection.id,
      }))
    : [];
  const pageTitle = model && model.hasLibraries ? model.activeLibraryName : t(locale, "nav.libraries");
  const stats = model?.stats ?? createPlaceholderStats(bootstrap.status, map.status);
  const newDocumentDisabled = createStatus === "creating" || !model || !model.hasCollections;
  const collectionBusy = Boolean(collectionMutation);
  const settingsHref = model?.activeLibraryId
    ? createSettingsHash({ scope: "library", spaceId: model.activeLibraryId, tab: "general" })
    : createSettingsHash({ scope: "workspace", tab: "general" });

  useEffect(() => {
    const syncHash = () => setHash(window.location.hash);
    window.addEventListener("hashchange", syncHash);
    return () => window.removeEventListener("hashchange", syncHash);
  }, []);

  useEffect(() => {
    setSearchQuery("");
    setStatusFilter(null);
    setTagFilter(null);
    setCreateError(null);
    setDocumentActionMessage(null);
    setCollectionActionMessage(null);
    setActionPanel(null);
  }, [selectedLibraryId]);

  const retryAll = () => {
    setBootstrapRetryKey((current) => current + 1);
    setMapRetryKey((current) => current + 1);
  };

  const handleCreateDocument = async () => {
    if (!model) {
      return;
    }

    if (!model.activeCollectionId) {
      setCreateStatus("error");
      setCreateError(model.createDocumentDisabledReason ?? "Select a folder before creating a document.");
      return;
    }

    setCreateStatus("creating");
    setCreateError(null);
    try {
      const response = await createDocument({
        folderId: model.activeCollectionId,
        title: "Untitled Document",
      });
      window.location.hash = createEditorHash(response.document.id);
    } catch (error) {
      setCreateStatus("error");
      setCreateError(error instanceof ApiClientError ? error.message : "Document could not be created.");
    }
  };

  const runDocumentMutation = async (
    document: LibraryDocumentRow,
    operation: DocumentOperation,
    action: () => Promise<unknown>,
    successMessage: string,
    failureMessage: string,
  ) => {
    setDocumentMutation({ documentId: document.id, operation });
    setDocumentActionMessage(null);
    setCollectionActionMessage(null);
    setCreateError(null);

    try {
      await action();
      setDocumentActionMessage({ kind: "success", text: successMessage });
      setMapRetryKey((current) => current + 1);
    } catch (error) {
      setDocumentActionMessage({
        kind: "error",
        text: error instanceof ApiClientError ? error.message : failureMessage,
      });
    } finally {
      setDocumentMutation(null);
    }
  };

  const runCollectionMutation = async (
    operation: CollectionOperation,
    collectionId: string | null,
    action: () => Promise<{ collectionId?: string | null }>,
    successMessage: string,
    failureMessage: string,
  ) => {
    setCollectionMutation({ collectionId, operation });
    setCollectionActionMessage(null);
    setCreateError(null);
    setDocumentActionMessage(null);

    try {
      const result = await action();
      setCollectionActionMessage({ kind: "success", text: successMessage });
      setMapRetryKey((current) => current + 1);
      if ("collectionId" in result && model?.activeLibraryId) {
        window.location.hash = createLibrariesHash({
          collectionId: result.collectionId ?? null,
          libraryId: model.activeLibraryId,
        });
      }
      setActionPanel(null);
    } catch (error) {
      setCollectionActionMessage({
        kind: "error",
        text: error instanceof ApiClientError ? error.message : failureMessage,
      });
    } finally {
      setCollectionMutation(null);
    }
  };

  const handleCreateCollection = () => {
    if (!model?.activeLibraryId) {
      return;
    }

    setActionPanel({ kind: "create-collection", title: "" });
  };

  const handleRenameCollection = () => {
    if (!model?.activeLibraryId || !model.activeCollectionId) {
      return;
    }

    setActionPanel({
      collectionId: model.activeCollectionId,
      kind: "rename-collection",
      title: model.activeCollectionTitle ?? "",
    });
  };

  const handleDeleteCollection = () => {
    if (!model?.activeLibraryId || !model.activeCollectionId) {
      return;
    }

    const collection = model.collections.find((item) => item.id === model.activeCollectionId);
    setActionPanel({
      collectionId: model.activeCollectionId,
      documentCount: collection?.documentCount ?? 0,
      kind: "delete-collection",
      title: model.activeCollectionTitle ?? "Selected folder",
    });
  };

  const handleReorderCollection = (direction: "down" | "up") => {
    if (!model?.activeLibraryId || !model.activeCollectionId) {
      return;
    }

    const collectionIds = createCollectionReorderIds(model.collections, model.activeCollectionId, direction);
    if (!collectionIds) {
      return;
    }

    void runCollectionMutation(
      "reorder",
      model.activeCollectionId,
      async () => {
        await reorderCollections(model.activeLibraryId!, { collectionIds });
        return { collectionId: model.activeCollectionId };
      },
      "Folder order updated.",
      "Folder order could not be updated.",
    );
  };

  const submitActionPanel = () => {
    if (!model?.activeLibraryId || !actionPanel) {
      return;
    }

    if (actionPanel.kind === "create-collection") {
      const title = actionPanel.title.trim();
      if (!title) {
        setCollectionActionMessage({ kind: "error", text: "Folder name is required." });
        return;
      }

      void runCollectionMutation(
        "create",
        null,
        async () => {
          const response = await createCollection(model.activeLibraryId!, { title });
          return { collectionId: response.collection.id };
        },
        "Folder created.",
        "Folder could not be created.",
      );
      return;
    }

    if (actionPanel.kind === "rename-collection") {
      const title = actionPanel.title.trim();
      if (!title) {
        setCollectionActionMessage({ kind: "error", text: "Folder name is required." });
        return;
      }

      void runCollectionMutation(
        "rename",
        actionPanel.collectionId,
        async () => {
          const response = await updateCollection(model.activeLibraryId!, actionPanel.collectionId, { title });
          return { collectionId: response.collection.id };
        },
        "Folder renamed.",
        "Folder could not be renamed.",
      );
      return;
    }

    if (actionPanel.kind === "delete-collection") {
      if (actionPanel.documentCount > 0) {
        setCollectionActionMessage({
          kind: "error",
          text: t(locale, "library.deleteNonEmptyCollectionDescription", { count: actionPanel.documentCount }),
        });
        return;
      }

      const nextCollectionId = getCollectionIdAfterDelete(model.collections, actionPanel.collectionId);
      void runCollectionMutation(
        "delete",
        actionPanel.collectionId,
        async () => {
          await deleteCollection(model.activeLibraryId!, actionPanel.collectionId);
          return { collectionId: nextCollectionId };
        },
        nextCollectionId ? "Folder deleted. Showing the next folder." : "Folder deleted. Showing all folders.",
        "Folder could not be deleted.",
      );
      return;
    }

    void runDocumentMutation(
      actionPanel.document,
      "delete",
      () => deleteDocument(actionPanel.document.id),
      "Document deleted.",
      "Document could not be deleted.",
    );
    setActionPanel(null);
  };

  const handleArchiveDocument = (document: LibraryDocumentRow) => {
    void runDocumentMutation(
      document,
      "archive",
      () => archiveDocument(document.id),
      "Document archived.",
      "Document could not be archived.",
    );
  };

  const handleRestoreDocument = (document: LibraryDocumentRow) => {
    void runDocumentMutation(
      document,
      "restore",
      () => restoreDocument(document.id),
      "Document restored.",
      "Document could not be restored.",
    );
  };

  const handleDeleteDocument = (document: LibraryDocumentRow) => {
    setActionPanel({ document, kind: "delete-document" });
  };

  const handleMoveDocument = (document: LibraryDocumentRow, folderId: string) => {
    if (!folderId || folderId === document.collectionId) {
      return;
    }

    const targetCollectionTitle = document.moveOptions.find((option) => option.id === folderId)?.label ?? "another folder";
    const leavesCurrentCollection = Boolean(model?.activeCollectionId && model.activeCollectionId === document.collectionId);

    void runDocumentMutation(
      document,
      "move",
      () => moveDocument(document.id, { folderId }),
      leavesCurrentCollection
        ? `Document moved to ${targetCollectionTitle}; it no longer matches this folder view.`
        : `Document moved to ${targetCollectionTitle}.`,
      "Document could not be moved.",
    );
  };

  return (
    <main className="workspace-home-shell flex h-screen flex-col overflow-hidden" style={librariesPatternStyle}>
      <WorkspaceHomeTopBar />
      <div className="workspace-home-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar
          activeItem="libraries"
          currentLibraryCollections={sidebarCollections}
        />
        <section className="workspace-home-content editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="workspace-home-mobile-nav md:hidden" aria-label="Workspace navigation">
            <a href="#home">{t(locale, "nav.home")}</a>
            <a aria-current="page" href="#libraries">{t(locale, "nav.libraries")}</a>
            <a href="#search">{t(locale, "nav.search")}</a>
            <a href="#updates">{t(locale, "nav.updates")}</a>
            <a href="#workspace-members">{t(locale, "nav.members")}</a>
            <a href="#settings">{t(locale, "nav.settings")}</a>
          </div>

          <div className="libraries-workbench">
            <header className="workspace-home-heading libraries-workbench-heading">
              <div className="min-w-0">
                <h1>{pageTitle}</h1>
                <p>{librariesStatusLabel(bootstrap.status, map.status, model, locale)}</p>
                {isRetryableStatus(bootstrap.status) || isRetryableStatus(map.status) ? (
                  <button className="workspace-home-text-link mt-2" onClick={retryAll} type="button">
                    {t(locale, "common.retry")}
                  </button>
                ) : null}
              </div>
              <div className="libraries-workbench-heading-actions">
                <a
                  className="workspace-home-secondary-action"
                  href={settingsHref}
                  title={t(locale, "settings.openWorkspaceSettings")}
                >
                  <Settings className="h-4 w-4" />
                  <span>{t(locale, "settings.openWorkspaceSettings")}</span>
                </a>
                <button
                  className="workspace-home-primary-action"
                  disabled={newDocumentDisabled}
                  onClick={handleCreateDocument}
                  title={getNewDocumentTitle(model, locale)}
                  type="button"
                >
                  {createStatus === "creating" ? <RefreshCw className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
                  <span>{createStatus === "creating" ? t(locale, "library.creating") : t(locale, "library.newDocument")}</span>
                </button>
              </div>
            </header>

            <LibraryToolbar
              collectionBusy={collectionBusy}
              collectionMutation={collectionMutation}
              onCreateCollection={handleCreateCollection}
              onDeleteCollection={handleDeleteCollection}
              onRenameCollection={handleRenameCollection}
              onReorderCollection={handleReorderCollection}
              model={model}
              searchQuery={searchQuery}
              setCreateError={setCreateError}
              setSearchQuery={setSearchQuery}
              setSortKey={setSortKey}
              setStatusFilter={setStatusFilter}
              setTagFilter={setTagFilter}
              setViewMode={setViewMode}
              sortKey={sortKey}
              viewMode={viewMode}
            />

            <LibraryActionPanel
              busy={Boolean(collectionMutation) || Boolean(documentMutation)}
              state={actionPanel}
              onCancel={() => setActionPanel(null)}
              onSubmit={submitActionPanel}
              onTitleChange={(title) => {
                setActionPanel((current) => {
                  if (!current || (current.kind !== "create-collection" && current.kind !== "rename-collection")) {
                    return current;
                  }

                  return { ...current, title };
                });
              }}
            />

            {createError ? <div className="share-permissions-inline-status mb-4">{createError}</div> : null}
            {collectionActionMessage ? (
              <div className={`libraries-workbench-action-status is-${collectionActionMessage.kind}`}>
                {collectionActionMessage.text}
              </div>
            ) : null}
            {documentActionMessage ? (
              <div className={`libraries-workbench-action-status is-${documentActionMessage.kind}`}>
                {documentActionMessage.text}
              </div>
            ) : null}

            <section className="libraries-workbench-stat-grid" aria-label="Library summary">
              {stats.map((stat) => (
                <LibraryStatCard key={stat.id} stat={stat} />
              ))}
            </section>

            <DocumentSurface
              model={model}
              mutation={documentMutation}
              onArchive={handleArchiveDocument}
              onDelete={handleDeleteDocument}
              onMove={handleMoveDocument}
              onRestore={handleRestoreDocument}
              status={map.status}
              viewMode={viewMode}
            />
          </div>

          <div className="workspace-home-coordinate-footer" aria-hidden="true">
            47.61 / 122.33
            <span>+</span>
          </div>
        </section>
      </div>
    </main>
  );
}

function LibraryToolbar({
  collectionBusy,
  collectionMutation,
  model,
  onCreateCollection,
  onDeleteCollection,
  onRenameCollection,
  onReorderCollection,
  searchQuery,
  setCreateError,
  setSearchQuery,
  setSortKey,
  setStatusFilter,
  setTagFilter,
  setViewMode,
  sortKey,
  viewMode,
}: {
  collectionBusy: boolean;
  collectionMutation: CollectionMutationState;
  model: LibrariesPageModel | null;
  onCreateCollection: () => void;
  onDeleteCollection: () => void;
  onRenameCollection: () => void;
  onReorderCollection: (direction: "down" | "up") => void;
  searchQuery: string;
  setCreateError: (value: string | null) => void;
  setSearchQuery: (value: string) => void;
  setSortKey: (value: LibrarySortKey) => void;
  setStatusFilter: (value: string | null) => void;
  setTagFilter: (value: string | null) => void;
  setViewMode: (value: ViewMode) => void;
  sortKey: LibrarySortKey;
  viewMode: ViewMode;
}) {
  const { locale } = useDisplayLanguage();
  const disabled = !model;
  const selectedCollectionTitle = model?.activeCollectionTitle ?? t(locale, "library.collection");
  const selectedCollectionDocumentCount =
    model?.collections.find((collection) => collection.id === model.activeCollectionId)?.documentCount ?? 0;

  return (
    <div className="libraries-workbench-toolbar" aria-label="Library controls">
      <label className="libraries-workbench-search">
        <Search className="h-4 w-4" />
        <span className="sr-only">{t(locale, "library.searchPlaceholder")}</span>
        <input
          disabled={disabled}
          onChange={(event) => setSearchQuery(event.currentTarget.value)}
          placeholder={t(locale, "library.searchPlaceholder")}
          type="search"
          value={searchQuery}
        />
      </label>

      <select
        aria-label={t(locale, "library.library")}
        className="libraries-workbench-select"
        disabled={disabled || !model.hasLibraries}
        onChange={(event) => {
          setCreateError(null);
          window.location.hash = createLibrariesHash({ libraryId: event.currentTarget.value || null });
        }}
        value={model?.activeLibraryId ?? ""}
      >
        {model?.libraries.map((library) => (
          <option key={library.id} value={library.id}>
            {library.name}
          </option>
        ))}
      </select>

      <select
        aria-label={t(locale, "library.collection")}
        className="libraries-workbench-select"
        disabled={disabled || !model.hasCollections}
        onChange={(event) => {
          setCreateError(null);
          window.location.hash = createLibrariesHash({
            collectionId: event.currentTarget.value || null,
            libraryId: model?.activeLibraryId ?? null,
          });
        }}
        value={model?.activeCollectionId ?? ""}
      >
        <option value="">{t(locale, "library.allCollections")}</option>
        {model?.collectionOptions.map((collection) => (
          <option key={collection.id} value={collection.id}>
            {collection.label} ({collection.count})
          </option>
        ))}
      </select>

      <div className="libraries-workbench-collection-tools" aria-label={t(locale, "nav.currentLibraryCollections")}>
        <button
          aria-label={t(locale, "library.newCollection")}
          className="libraries-workbench-icon-action"
          disabled={disabled || !model?.canCreateCollection || collectionBusy}
          onClick={onCreateCollection}
          title={t(locale, "library.newCollection")}
          type="button"
        >
          {collectionMutation?.operation === "create" ? <RefreshCw className="h-4 w-4" /> : <FolderPlus className="h-4 w-4" />}
        </button>
        <button
          aria-label={`${t(locale, "library.renameCollection")}: ${selectedCollectionTitle}`}
          className="libraries-workbench-icon-action"
          disabled={disabled || !model?.canRenameActiveCollection || collectionBusy}
          onClick={onRenameCollection}
          title={`${t(locale, "library.renameCollection")}: ${selectedCollectionTitle}`}
          type="button"
        >
          {collectionMutation?.operation === "rename" ? <RefreshCw className="h-4 w-4" /> : <Edit3 className="h-4 w-4" />}
        </button>
        <button
          aria-label={t(locale, "library.moveCollectionUp", { title: selectedCollectionTitle })}
          className="libraries-workbench-icon-action"
          disabled={disabled || !model?.canReorderActiveCollectionUp || collectionBusy}
          onClick={() => onReorderCollection("up")}
          title={t(locale, "library.moveCollectionUp", { title: selectedCollectionTitle })}
          type="button"
        >
          <ChevronUp className="h-4 w-4" />
        </button>
        <button
          aria-label={t(locale, "library.moveCollectionDown", { title: selectedCollectionTitle })}
          className="libraries-workbench-icon-action"
          disabled={disabled || !model?.canReorderActiveCollectionDown || collectionBusy}
          onClick={() => onReorderCollection("down")}
          title={t(locale, "library.moveCollectionDown", { title: selectedCollectionTitle })}
          type="button"
        >
          <ChevronDown className="h-4 w-4" />
        </button>
        <button
          aria-label={`${t(locale, "library.deleteCollection")}: ${selectedCollectionTitle}`}
          className="libraries-workbench-icon-action is-danger"
          disabled={disabled || !model.activeCollectionId || !model.canDeleteActiveCollection || collectionBusy}
          onClick={onDeleteCollection}
          title={
            model?.activeCollectionId && !model.canDeleteActiveCollection
              ? t(locale, "library.deleteNonEmptyCollectionDescription", { count: selectedCollectionDocumentCount })
              : `${t(locale, "library.deleteCollection")}: ${selectedCollectionTitle}`
          }
          type="button"
        >
          {collectionMutation?.operation === "delete" ? <RefreshCw className="h-4 w-4" /> : <Trash2 className="h-4 w-4" />}
        </button>
      </div>

      <select
        aria-label="Status"
        className="libraries-workbench-select"
        disabled={disabled || model.statusOptions.length === 0}
        onChange={(event) => setStatusFilter(event.currentTarget.value || null)}
        value={model?.statusFilter ?? ""}
      >
        <option value="">All statuses</option>
        {model?.statusOptions.map((status) => (
          <option key={status.id} value={status.id}>
            {status.label} ({status.count})
          </option>
        ))}
      </select>

      <select
        aria-label="Tag"
        className="libraries-workbench-select"
        disabled={disabled || model.tagOptions.length === 0}
        onChange={(event) => setTagFilter(event.currentTarget.value || null)}
        value={model?.tagFilter ?? ""}
      >
        <option value="">All tags</option>
        {model?.tagOptions.map((tag) => (
          <option key={tag.id} value={tag.id}>
            {tag.label} ({tag.count})
          </option>
        ))}
      </select>

      <select
        aria-label="Sort documents"
        className="libraries-workbench-select is-sort"
        disabled={disabled}
        onChange={(event) => setSortKey(event.currentTarget.value as LibrarySortKey)}
        value={model?.sortKey ?? sortKey}
      >
        <option value="updatedAt">Last updated</option>
        <option value="title">Title</option>
        <option value="status">Status</option>
        <option value="collection">Folder</option>
      </select>

      <div className="libraries-workbench-view-toggle" aria-label="View mode">
        <button
          aria-label="Grid view"
          aria-pressed={viewMode === "grid"}
          className={viewMode === "grid" ? "is-active" : ""}
          onClick={() => setViewMode("grid")}
          type="button"
        >
          <Grid2X2 className="h-4 w-4" />
        </button>
        <button
          aria-label="List view"
          aria-pressed={viewMode === "list"}
          className={viewMode === "list" ? "is-active" : ""}
          onClick={() => setViewMode("list")}
          type="button"
        >
          <List className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

function LibraryActionPanel({
  busy,
  onCancel,
  onSubmit,
  onTitleChange,
  state,
}: {
  busy: boolean;
  onCancel: () => void;
  onSubmit: () => void;
  onTitleChange: (title: string) => void;
  state: LibraryActionPanelState;
}) {
  const { locale } = useDisplayLanguage();
  if (!state) {
    return null;
  }

  const isDeleteCollection = state.kind === "delete-collection";
  const isDeleteDocument = state.kind === "delete-document";
  const isDelete = isDeleteCollection || isDeleteDocument;
  const title = getActionPanelTitle(state, locale);
  const description = getActionPanelDescription(state, locale);
  const submitLabel = getActionPanelSubmitLabel(state, busy, locale);
  const canSubmit =
    !busy &&
    (state.kind === "create-collection" || state.kind === "rename-collection"
      ? state.title.trim().length > 0
      : true);

  return (
    <section className={isDelete ? "libraries-workbench-action-panel is-danger" : "libraries-workbench-action-panel"}>
      <div className="libraries-workbench-action-panel-head">
        <span>
          {isDelete ? <AlertTriangle className="h-4 w-4" /> : <Layers3 className="h-4 w-4" />}
        </span>
        <div className="min-w-0">
          <h2>{title}</h2>
          <p>{description}</p>
        </div>
        <button aria-label={t(locale, "common.close")} disabled={busy} onClick={onCancel} title={t(locale, "common.close")} type="button">
          <X className="h-4 w-4" />
        </button>
      </div>

      {state.kind === "create-collection" || state.kind === "rename-collection" ? (
        <label className="libraries-workbench-action-panel-field">
          <span>{t(locale, "library.collectionName")}</span>
          <input
            autoFocus
            disabled={busy}
            onChange={(event) => onTitleChange(event.currentTarget.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && canSubmit) {
                onSubmit();
              }
            }}
            type="text"
            value={state.title}
          />
        </label>
      ) : null}

      <div className="libraries-workbench-action-panel-actions">
        <button disabled={busy} onClick={onCancel} type="button">
          {t(locale, "common.cancel")}
        </button>
        <button className={isDelete ? "is-danger" : "is-primary"} disabled={!canSubmit} onClick={onSubmit} type="button">
          {submitLabel}
        </button>
      </div>
    </section>
  );
}

function getActionPanelTitle(state: Exclude<LibraryActionPanelState, null>, locale: DisplayLocale) {
  if (state.kind === "create-collection") {
    return t(locale, "library.newCollection");
  }

  if (state.kind === "rename-collection") {
    return t(locale, "library.renameCollection");
  }

  if (state.kind === "delete-collection") {
    return `${t(locale, "library.deleteCollection")}: ${state.title}`;
  }

  return `${t(locale, "library.deleteDocument")}: ${state.document.title}`;
}

function getActionPanelDescription(state: Exclude<LibraryActionPanelState, null>, locale: DisplayLocale) {
  if (state.kind === "create-collection") {
    return t(locale, "library.createCollectionDescription");
  }

  if (state.kind === "rename-collection") {
    return t(locale, "library.renameCollectionDescription");
  }

  if (state.kind === "delete-collection" && state.documentCount > 0) {
    return t(locale, "library.deleteNonEmptyCollectionDescription", { count: state.documentCount });
  }

  if (state.kind === "delete-collection") {
    return t(locale, "library.deleteEmptyCollectionDescription");
  }

  return "This document delete action cannot be undone.";
}

function getActionPanelSubmitLabel(state: Exclude<LibraryActionPanelState, null>, busy: boolean, locale: DisplayLocale) {
  if (busy) {
    return t(locale, "library.working");
  }

  if (state.kind === "create-collection") {
    return t(locale, "library.createCollection");
  }

  if (state.kind === "rename-collection") {
    return t(locale, "library.saveName");
  }

  if (state.kind === "delete-collection") {
    return t(locale, "library.deleteCollection");
  }

  return t(locale, "library.deleteDocument");
}

function LibraryStatCard({ stat }: { stat: LibraryStatRow }) {
  const Icon = statIcons[stat.id] ?? Library;

  return (
    <article className="libraries-workbench-stat-card">
      <span className="libraries-workbench-stat-icon">
        <Icon className="h-5 w-5" />
      </span>
      <div className="min-w-0">
        <div className="libraries-workbench-stat-label">{stat.label}</div>
        <div className="libraries-workbench-stat-value">{stat.value}</div>
      </div>
    </article>
  );
}

function DocumentSurface({
  model,
  mutation,
  onArchive,
  onDelete,
  onMove,
  onRestore,
  status,
  viewMode,
}: {
  model: LibrariesPageModel | null;
  mutation: DocumentMutationState;
  onArchive: (document: LibraryDocumentRow) => void;
  onDelete: (document: LibraryDocumentRow) => void;
  onMove: (document: LibraryDocumentRow, folderId: string) => void;
  onRestore: (document: LibraryDocumentRow) => void;
  status: DataStatus;
  viewMode: ViewMode;
}) {
  const { locale } = useDisplayLanguage();
  if (!model) {
    return <PanelMessage>{status === "loading" ? `${t(locale, "common.loading")} ${t(locale, "library.documents").toLowerCase()}...` : getBlockedMessage(status, locale)}</PanelMessage>;
  }

  if (!model.hasCollections) {
    return (
      <PanelMessage>
        {t(locale, "library.noCollections")}
      </PanelMessage>
    );
  }

  if (model.totalDocumentCount === 0) {
    return <PanelMessage>{t(locale, "library.noDocuments")}</PanelMessage>;
  }

  if (model.documents.length === 0) {
    return <PanelMessage>No documents match the current search and filters.</PanelMessage>;
  }

  if (viewMode === "list") {
    return (
      <DocumentList
        documents={model.documents}
        mutation={mutation}
        onArchive={onArchive}
        onDelete={onDelete}
        onMove={onMove}
        onRestore={onRestore}
      />
    );
  }

  return (
    <DocumentGrid
      documents={model.documents}
      mutation={mutation}
      onArchive={onArchive}
      onDelete={onDelete}
      onMove={onMove}
      onRestore={onRestore}
    />
  );
}

function DocumentGrid({
  documents,
  mutation,
  onArchive,
  onDelete,
  onMove,
  onRestore,
}: {
  documents: LibraryDocumentRow[];
  mutation: DocumentMutationState;
  onArchive: (document: LibraryDocumentRow) => void;
  onDelete: (document: LibraryDocumentRow) => void;
  onMove: (document: LibraryDocumentRow, folderId: string) => void;
  onRestore: (document: LibraryDocumentRow) => void;
}) {
  return (
    <section className="libraries-workbench-doc-grid" aria-label="Documents">
      {documents.map((document) => (
        <article className="libraries-workbench-doc-card" key={document.id} title={document.title}>
          <div className="libraries-workbench-doc-card-head">
            <span className="libraries-workbench-doc-icon">
              <FileText className="h-5 w-5" />
            </span>
            <div className="min-w-0">
              <h2>{document.title}</h2>
              <p>{document.collectionTitle}</p>
            </div>
          </div>
          <div className="libraries-workbench-doc-meta">
            <StatusBadge document={document} />
            <span>{formatDate(document.updatedAt)}</span>
          </div>
          <TagList document={document} />
          <DocumentActions
            document={document}
            mutation={mutation}
            onArchive={onArchive}
            onDelete={onDelete}
            onMove={onMove}
            onRestore={onRestore}
          />
        </article>
      ))}
    </section>
  );
}

function DocumentList({
  documents,
  mutation,
  onArchive,
  onDelete,
  onMove,
  onRestore,
}: {
  documents: LibraryDocumentRow[];
  mutation: DocumentMutationState;
  onArchive: (document: LibraryDocumentRow) => void;
  onDelete: (document: LibraryDocumentRow) => void;
  onMove: (document: LibraryDocumentRow, folderId: string) => void;
  onRestore: (document: LibraryDocumentRow) => void;
}) {
  return (
    <div className="libraries-workbench-list" role="table" aria-label="Documents">
      <div className="libraries-workbench-list-head" role="row">
        <span>Document</span>
        <span>Folder</span>
        <span>Status</span>
        <span>Updated</span>
        <span>Tags</span>
        <span>Actions</span>
      </div>
      {documents.map((document) => (
        <div className="libraries-workbench-list-row" key={document.id} role="row" title={document.title}>
          <span className="min-w-0 truncate font-semibold text-[var(--ns-blue-600)]">{document.title}</span>
          <span className="min-w-0 truncate">{document.collectionTitle}</span>
          <StatusBadge document={document} />
          <span>{formatDate(document.updatedAt)}</span>
          <TagList document={document} compact />
          <DocumentActions
            compact
            document={document}
            mutation={mutation}
            onArchive={onArchive}
            onDelete={onDelete}
            onMove={onMove}
            onRestore={onRestore}
          />
        </div>
      ))}
    </div>
  );
}

function DocumentActions({
  compact = false,
  document,
  mutation,
  onArchive,
  onDelete,
  onMove,
  onRestore,
}: {
  compact?: boolean;
  document: LibraryDocumentRow;
  mutation: DocumentMutationState;
  onArchive: (document: LibraryDocumentRow) => void;
  onDelete: (document: LibraryDocumentRow) => void;
  onMove: (document: LibraryDocumentRow, folderId: string) => void;
  onRestore: (document: LibraryDocumentRow) => void;
}) {
  const isBusy = mutation?.documentId === document.id;
  const isAnyBusy = Boolean(mutation);
  const moveTitle = document.canMove
    ? `Move ${document.title} to another folder`
    : "No other folder is available.";

  return (
    <div className={compact ? "libraries-workbench-actions is-compact" : "libraries-workbench-actions"}>
      <a
        aria-label={`Open ${document.title}`}
        className="libraries-workbench-action-button"
        href={document.href}
        title={`Open ${document.title}`}
      >
        <ExternalLink className="h-3.5 w-3.5" />
        <span>Open</span>
      </a>

      <label className="libraries-workbench-move-control" title={moveTitle}>
        <span className="sr-only">Move {document.title} to folder</span>
        <select
          aria-label={`Move ${document.title} to folder`}
          disabled={isAnyBusy || !document.canMove}
          onChange={(event) => onMove(document, event.currentTarget.value)}
          value={document.collectionId}
        >
          {document.moveOptions.map((option) => (
            <option disabled={option.isCurrent} key={option.id} value={option.id}>
              {option.label}
              {option.isCurrent ? " (current)" : ""}
            </option>
          ))}
        </select>
      </label>

      {document.canArchive ? (
        <button
          aria-label={`Archive ${document.title}`}
          className="libraries-workbench-action-button"
          disabled={isAnyBusy}
          onClick={() => onArchive(document)}
          title={`Archive ${document.title}`}
          type="button"
        >
          <Archive className="h-3.5 w-3.5" />
          <span>Archive</span>
        </button>
      ) : null}

      {document.canRestore ? (
        <button
          aria-label={`Restore ${document.title}`}
          className="libraries-workbench-action-button"
          disabled={isAnyBusy}
          onClick={() => onRestore(document)}
          title={`Restore ${document.title}`}
          type="button"
        >
          <RotateCcw className="h-3.5 w-3.5" />
          <span>Restore</span>
        </button>
      ) : null}

      {document.canDelete ? (
        <button
          aria-label={`Delete ${document.title}`}
          className="libraries-workbench-action-button is-danger"
          disabled={isAnyBusy}
          onClick={() => onDelete(document)}
          title={`Delete ${document.title}`}
          type="button"
        >
          <Trash2 className="h-3.5 w-3.5" />
          <span>Delete</span>
        </button>
      ) : null}

      {isBusy ? <span className="libraries-workbench-operation">{getOperationLabel(mutation.operation)}</span> : null}
    </div>
  );
}

function StatusBadge({ document }: { document: LibraryDocumentRow }) {
  return (
    <span className={`libraries-workbench-status is-${document.statusTone}`}>
      {document.status}
    </span>
  );
}

function TagList({ compact = false, document }: { compact?: boolean; document: LibraryDocumentRow }) {
  if (document.visibleTags.length === 0) {
    return <span className="libraries-workbench-muted">No tags</span>;
  }

  return (
    <div className={compact ? "libraries-workbench-tags is-compact" : "libraries-workbench-tags"}>
      {document.visibleTags.map((tag) => (
        <span key={tag}>{tag}</span>
      ))}
      {document.hiddenTagCount > 0 ? <span>+{document.hiddenTagCount}</span> : null}
    </div>
  );
}

function PanelMessage({ children }: { children: ReactNode }) {
  return <div className="libraries-workbench-empty">{children}</div>;
}

function useBootstrapData(retryKey: number) {
  const [data, setData] = useState<BootstrapResponse | null>(null);
  const [status, setStatus] = useState<DataStatus>(() => (getConfiguredApiBaseUrl() ? "loading" : "unconfigured"));

  useEffect(() => {
    if (!getConfiguredApiBaseUrl()) {
      setData(null);
      setStatus("unconfigured");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getBootstrap(controller.signal)
      .then((response) => {
        setData(response);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setData(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [retryKey]);

  return { data, status };
}

function useLibraryMapData(libraryId: string | null, bootstrapStatus: DataStatus, retryKey: number) {
  const [data, setData] = useState<KnowledgeMapResponse | null>(null);
  const [status, setStatus] = useState<DataStatus>("idle");

  useEffect(() => {
    if (bootstrapStatus === "unconfigured") {
      setData(null);
      setStatus("unconfigured");
      return;
    }

    if (bootstrapStatus !== "ready" || !libraryId) {
      setData(null);
      setStatus(bootstrapStatus === "loading" ? "loading" : "idle");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getSpaceMap(libraryId, controller.signal)
      .then((response) => {
        setData(response);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setData(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [bootstrapStatus, libraryId, retryKey]);

  return { data, status };
}

function createPlaceholderStats(bootstrapStatus: DataStatus, mapStatus: DataStatus): LibraryStatRow[] {
  const loading = bootstrapStatus === "loading" || mapStatus === "loading";
  const value = loading ? "..." : "0";

  return [
    { id: "total-documents", label: "Total Documents", value },
    { id: "collections", label: "Folders", value },
    { id: "published-documents", label: "Published", value },
    { id: "draft-documents", label: "Drafts", value },
    { id: "archived-documents", label: "Archived", value },
    { id: "updated-30-days", label: "Updated in 30 days", value },
  ];
}

function librariesStatusLabel(
  bootstrapStatus: DataStatus,
  mapStatus: DataStatus,
  model: LibrariesPageModel | null,
  locale: DisplayLocale,
) {
  if (bootstrapStatus === "unconfigured") {
    return locale === "zh-CN" ? "演示模式。配置 VITE_NORTHSTAR_API_BASE_URL 后加载实时资料库。" : "Demo mode. Configure VITE_NORTHSTAR_API_BASE_URL to load live libraries.";
  }

  if (bootstrapStatus === "loading" || mapStatus === "loading") {
    return locale === "zh-CN" ? "正在加载工作区资料库。" : "Loading workspace libraries.";
  }

  if (bootstrapStatus === "forbidden" || mapStatus === "forbidden") {
    return locale === "zh-CN" ? "登录后加载工作区资料库。" : "Sign in to load workspace libraries.";
  }

  if (bootstrapStatus === "error" || mapStatus === "error") {
    return locale === "zh-CN" ? "无法加载资料库数据。" : "Library data could not be loaded.";
  }

  if (model) {
    return locale === "zh-CN"
      ? `${model.workspaceName}: 当前显示 ${model.visibleDocumentCount} / ${model.totalDocumentCount} 个实时文档。`
      : `${model.workspaceName}: ${model.visibleDocumentCount} of ${model.totalDocumentCount} documents visible from live library data.`;
  }

  return locale === "zh-CN" ? "选择资料库以加载文件夹和文档。" : "Select a library to load its folders and documents.";
}

function getBlockedMessage(status: DataStatus, locale: DisplayLocale) {
  if (status === "unconfigured") {
    return locale === "zh-CN" ? "配置 API 后加载实时资料库。" : "Configure the API to load live libraries.";
  }

  if (status === "forbidden") {
    return locale === "zh-CN" ? "当前会话不可用。" : "Not available for this session.";
  }

  if (status === "error") {
    return locale === "zh-CN" ? "无法加载实时资料库数据。" : "Live library data could not be loaded.";
  }

  return locale === "zh-CN" ? "暂无资料库数据。" : "No library data available.";
}

function getNewDocumentTitle(model: LibrariesPageModel | null, locale: DisplayLocale) {
  if (!model) {
    return locale === "zh-CN" ? "请先加载资料库，再创建文档。" : "Load a library before creating a document.";
  }

  if (!model.hasCollections) {
    return locale === "zh-CN" ? "当前资料库暂无文件夹。" : "No folders are available in this library.";
  }

  return model.activeCollectionId
    ? (locale === "zh-CN" ? "在选中文件夹中创建文档" : "Create document in selected folder")
    : (locale === "zh-CN" ? "请先选择文件夹，再创建文档。" : "Select a folder before creating a document.");
}

function getOperationLabel(operation: DocumentOperation) {
  if (operation === "archive") {
    return "Archiving...";
  }

  if (operation === "restore") {
    return "Restoring...";
  }

  if (operation === "delete") {
    return "Deleting...";
  }

  return "Moving...";
}

function isRetryableStatus(status: DataStatus) {
  return status === "error" || status === "forbidden";
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
