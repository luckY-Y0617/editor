import {
  ArrowUpRight,
  Bell,
  FileText,
  Folder,
  Home,
  Library,
  LoaderCircle,
  Search,
  Settings,
  X,
  type LucideIcon,
} from "lucide-react";
import { type KeyboardEvent, type ReactNode, useEffect, useMemo, useRef, useState } from "react";
import { searchResults, type SearchResultItem } from "../data/searchDiscoveryData";
import { getBootstrap, searchKnowledge, type BootstrapResponse } from "../lib/appApi";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import { createEditorHash, createLibrariesHash } from "../lib/hashRouting";
import {
  toFolderSearchResultItems,
  toSearchResultItems,
  type SearchApiStatus,
  type SearchDisplayResult,
} from "../lib/searchDiscoveryModel";

type SearchCommandPaletteProps = {
  folderId?: string | null;
  folderTitle?: string | null;
  initialQuery?: string;
  libraryId?: string | null;
  onClose: () => void;
};

type SearchCommandKind = "action" | "collection" | "document";
type SearchCommandTab = "all" | "documents" | "folders" | "actions";

type SearchCommandEntry = {
  detail?: string;
  href: string;
  icon: LucideIcon;
  id: string;
  key: string;
  kind: SearchCommandKind;
  meta?: string;
  path: string;
  status?: "Published" | "Draft";
  title: string;
};

type SearchCommandAction = {
  detail: string;
  href: string;
  icon: LucideIcon;
  id: string;
  keywords: string[];
  path: string;
  title: string;
};

type CommandSearchState = {
  mode: "demo" | "live";
  results: SearchDisplayResult[];
  status: SearchApiStatus;
};

const commandActions: SearchCommandAction[] = [
  {
    detail: "Workspace signals and recently touched documents",
    href: "#home",
    icon: Home,
    id: "home",
    keywords: ["dashboard", "workspace", "overview", "recent"],
    path: "Workspace",
    title: "Open Home",
  },
  {
    detail: "Browse libraries, folders, and documents",
    href: "#libraries",
    icon: Library,
    id: "libraries",
    keywords: ["library", "folder", "document", "browse"],
    path: "Library",
    title: "Browse Libraries",
  },
  {
    detail: "Review access, sharing, and notification updates",
    href: "#updates",
    icon: Bell,
    id: "updates",
    keywords: ["notifications", "access", "share", "requests"],
    path: "Updates",
    title: "Open Updates",
  },
  {
    detail: "Manage workspace and personal preferences",
    href: "#settings",
    icon: Settings,
    id: "settings",
    keywords: ["preferences", "members", "security", "workspace"],
    path: "Settings",
    title: "Open Settings",
  },
];

const tabItems: Array<{ id: SearchCommandTab; label: string }> = [
  { id: "all", label: "All" },
  { id: "documents", label: "Documents" },
  { id: "folders", label: "Folders" },
  { id: "actions", label: "Actions" },
];

export function SearchCommandPalette({
  folderId,
  folderTitle,
  initialQuery = "",
  libraryId,
  onClose,
}: SearchCommandPaletteProps) {
  const [query, setQuery] = useState(initialQuery);
  const [activeTab, setActiveTab] = useState<SearchCommandTab>("all");
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const shortcutLabel = getShortcutLabel();
  const searchState = useCommandSearch({
    folderId,
    folderTitle,
    isOpen: true,
    libraryId,
    query,
  });
  const entries = useMemo(() => {
    const resultEntries = searchState.results.map(toResultEntry);
    const actionEntries = getMatchingActions(query).map(toActionEntry);
    return [...resultEntries, ...actionEntries];
  }, [query, searchState.results]);
  const counts = useMemo(() => getTabCounts(entries), [entries]);
  const visibleEntries = useMemo(
    () => filterEntriesByTab(entries, activeTab),
    [activeTab, entries],
  );
  const selectedEntry = visibleEntries[selectedIndex] ?? visibleEntries[0] ?? null;
  const bestMatch = visibleEntries[0] ?? null;
  const groupedEntries = useMemo(() => groupEntries(visibleEntries.slice(1)), [visibleEntries]);
  const scopeLabel = folderTitle?.trim() || null;

  useEffect(() => {
    setQuery(initialQuery);
    setActiveTab("all");
  }, [initialQuery, folderId, libraryId]);

  useEffect(() => {
    inputRef.current?.focus();
    inputRef.current?.select();
  }, []);

  useEffect(() => {
    setSelectedIndex(0);
  }, [activeTab, query, searchState.results]);

  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, []);

  const openEntry = (entry: SearchCommandEntry) => {
    window.location.hash = entry.href;
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Escape") {
      event.preventDefault();
      onClose();
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      setSelectedIndex((current) => Math.min(current + 1, Math.max(visibleEntries.length - 1, 0)));
      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      setSelectedIndex((current) => Math.max(current - 1, 0));
      return;
    }

    if (event.key === "Enter" && selectedEntry) {
      event.preventDefault();
      openEntry(selectedEntry);
    }
  };

  return (
    <div className="search-command-overlay" onMouseDown={onClose}>
      <section
        aria-label="Search Northstar"
        aria-modal="true"
        className="search-command-dialog"
        onMouseDown={(event) => event.stopPropagation()}
        role="dialog"
      >
        <div className="search-command-input-row">
          <Search className="h-5 w-5" />
          {scopeLabel ? <span className="search-command-scope">{scopeLabel}</span> : null}
          <input
            ref={inputRef}
            aria-label="Search Northstar"
            autoComplete="off"
            onChange={(event) => setQuery(event.currentTarget.value)}
            onKeyDown={handleKeyDown}
            placeholder="Search Northstar"
            type="search"
            value={query}
          />
          {query ? (
            <button aria-label="Clear search" className="search-command-icon-button" onClick={() => setQuery("")} title="Clear search" type="button">
              <X className="h-4 w-4" />
            </button>
          ) : null}
          <kbd>{shortcutLabel}</kbd>
        </div>

        <div className="search-command-tabs" role="tablist" aria-label="Search result type">
          {tabItems.map((tab) => (
            <button
              aria-selected={activeTab === tab.id}
              className={activeTab === tab.id ? "is-active" : ""}
              disabled={counts[tab.id] === 0 && tab.id !== "all"}
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              role="tab"
              type="button"
            >
              {tab.label}
              <span>{counts[tab.id]}</span>
            </button>
          ))}
        </div>

        <div className="search-command-results editor-scrollbar">
          <SearchCommandState status={searchState.status} mode={searchState.mode} />
          {visibleEntries.length === 0 ? (
            <div className="search-command-empty">
              <h2>No matching results</h2>
              <p>Try a document title, folder name, owner, or workspace action.</p>
            </div>
          ) : (
            <>
              {bestMatch ? (
                <SearchCommandSection title="Best Match">
                  <SearchCommandResult
                    entry={bestMatch}
                    index={0}
                    isSelected={selectedEntry?.key === bestMatch.key}
                    onOpen={openEntry}
                    onSelect={setSelectedIndex}
                  />
                </SearchCommandSection>
              ) : null}

              {groupedEntries.documents.length > 0 ? (
                <SearchCommandSection title="Documents">
                  {groupedEntries.documents.map((entry) => (
                    <SearchCommandResult
                      entry={entry}
                      index={visibleEntries.findIndex((item) => item.key === entry.key)}
                      isSelected={selectedEntry?.key === entry.key}
                      key={entry.key}
                      onOpen={openEntry}
                      onSelect={setSelectedIndex}
                    />
                  ))}
                </SearchCommandSection>
              ) : null}

              {groupedEntries.folders.length > 0 ? (
                <SearchCommandSection title="Folders">
                  {groupedEntries.folders.map((entry) => (
                    <SearchCommandResult
                      entry={entry}
                      index={visibleEntries.findIndex((item) => item.key === entry.key)}
                      isSelected={selectedEntry?.key === entry.key}
                      key={entry.key}
                      onOpen={openEntry}
                      onSelect={setSelectedIndex}
                    />
                  ))}
                </SearchCommandSection>
              ) : null}

              {groupedEntries.actions.length > 0 ? (
                <SearchCommandSection title="Actions">
                  {groupedEntries.actions.map((entry) => (
                    <SearchCommandResult
                      entry={entry}
                      index={visibleEntries.findIndex((item) => item.key === entry.key)}
                      isSelected={selectedEntry?.key === entry.key}
                      key={entry.key}
                      onOpen={openEntry}
                      onSelect={setSelectedIndex}
                    />
                  ))}
                </SearchCommandSection>
              ) : null}
            </>
          )}
        </div>
      </section>
    </div>
  );
}

function useCommandSearch({
  folderId,
  folderTitle,
  isOpen,
  libraryId,
  query,
}: {
  folderId?: string | null;
  folderTitle?: string | null;
  isOpen: boolean;
  libraryId?: string | null;
  query: string;
}) {
  const debouncedQuery = useDebouncedValue(query.trim(), 150);
  const [state, setState] = useState<CommandSearchState>(() => ({
    mode: getConfiguredApiBaseUrl() ? "live" : "demo",
    results: getDemoResults(query),
    status: getConfiguredApiBaseUrl() ? "idle" : "unconfigured",
  }));

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (!getConfiguredApiBaseUrl()) {
      setState({
        mode: "demo",
        results: getDemoResults(debouncedQuery),
        status: "unconfigured",
      });
      return;
    }

    const controller = new AbortController();
    setState((current) => ({
      ...current,
      mode: "live",
      status: "loading",
    }));

    void getBootstrap(controller.signal)
      .then(async (bootstrap) => {
        const resolvedLibraryId = libraryId ?? bootstrap.activeSpaceId;

        if (folderId) {
          return toFolderSearchResultItems(bootstrap, folderId, debouncedQuery).slice(0, 12);
        }

        if (!debouncedQuery) {
          return toRecentSearchResultItems(bootstrap, resolvedLibraryId);
        }

        const response = await searchKnowledge(
          { q: debouncedQuery, spaceId: resolvedLibraryId },
          controller.signal,
        );
        return mergeResults([
          ...toSearchResultItems(response, bootstrap, resolvedLibraryId),
          ...toLibraryFolderSearchResultItems(bootstrap, debouncedQuery, resolvedLibraryId),
        ]);
      })
      .then((results) => {
        if (controller.signal.aborted) {
          return;
        }

        setState({
          mode: "live",
          results,
          status: "ready",
        });
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setState({
          mode: "live",
          results: [],
          status: error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error",
        });
      });

    return () => controller.abort();
  }, [debouncedQuery, folderId, folderTitle, isOpen, libraryId]);

  return state;
}

function SearchCommandState({ mode, status }: { mode: CommandSearchState["mode"]; status: SearchApiStatus }) {
  if (status === "loading") {
    return (
      <div className="search-command-state">
        <LoaderCircle className="h-3.5 w-3.5 animate-spin" />
        <span>Searching</span>
      </div>
    );
  }

  if (status === "forbidden" || status === "error") {
    return (
      <div className="search-command-state is-warning">
        <span>{status === "forbidden" ? "Sign in required" : "Search unavailable"}</span>
      </div>
    );
  }

  return (
    <div className={["search-command-state", mode === "demo" ? "is-demo" : ""].join(" ")}>
      <span>{mode === "demo" ? "Demo results" : "Live results"}</span>
    </div>
  );
}

function SearchCommandSection({ children, title }: { children: ReactNode; title: string }) {
  return (
    <section className="search-command-section">
      <h2>{title}</h2>
      <div>{children}</div>
    </section>
  );
}

function SearchCommandResult({
  entry,
  index,
  isSelected,
  onOpen,
  onSelect,
}: {
  entry: SearchCommandEntry;
  index: number;
  isSelected: boolean;
  onOpen: (entry: SearchCommandEntry) => void;
  onSelect: (index: number) => void;
}) {
  const Icon = entry.icon;
  return (
    <button
      aria-selected={isSelected}
      className={["search-command-result", isSelected ? "is-selected" : ""].join(" ")}
      onClick={() => onOpen(entry)}
      onMouseEnter={() => onSelect(Math.max(index, 0))}
      role="option"
      title={entry.title}
      type="button"
    >
      <span className={["search-command-result-icon", `is-${entry.kind}`].join(" ")}>
        <Icon className="h-5 w-5" />
      </span>
      <span className="search-command-result-body">
        <strong>{entry.title}</strong>
        <small>{entry.path}</small>
        {entry.detail ? <span>{entry.detail}</span> : null}
      </span>
      <span className="search-command-result-meta">
        {entry.status ? <em className={entry.status === "Draft" ? "is-draft" : ""}>{entry.status}</em> : null}
        {entry.meta ? <small>{entry.meta}</small> : null}
        <ArrowUpRight className="h-4 w-4" />
      </span>
    </button>
  );
}

function useDebouncedValue(value: string, delayMs: number) {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => setDebouncedValue(value), delayMs);
    return () => window.clearTimeout(timeoutId);
  }, [delayMs, value]);

  return debouncedValue;
}

function toResultEntry(result: SearchDisplayResult): SearchCommandEntry {
  return {
    detail: result.excerpt,
    href: result.href,
    icon: result.type === "collection" ? Folder : FileText,
    id: result.id,
    key: `${result.type}:${result.id}`,
    kind: result.type,
    meta: result.updatedAt,
    path: result.type === "collection" && typeof result.documentCount === "number"
      ? `${result.path} / ${result.documentCount} documents`
      : result.path,
    status: result.status,
    title: result.title,
  };
}

function toActionEntry(action: SearchCommandAction): SearchCommandEntry {
  return {
    detail: action.detail,
    href: action.href,
    icon: action.icon,
    id: action.id,
    key: `action:${action.id}`,
    kind: "action",
    path: action.path,
    title: action.title,
  };
}

function toRecentSearchResultItems(
  bootstrap: BootstrapResponse,
  libraryId?: string | null,
): SearchDisplayResult[] {
  const resolvedLibraryId = libraryId ?? bootstrap.activeSpaceId;
  const libraryName = getLibraryName(bootstrap, resolvedLibraryId);
  const folderTitlesById = new Map(bootstrap.folders.map((folder) => [folder.id, folder.title]));
  const documentResults = [...bootstrap.documents]
    .sort((left, right) => new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime())
    .slice(0, 6)
    .map((document): SearchDisplayResult => ({
      excerpt: document.tags.length > 0 ? `Tags: ${document.tags.join(", ")}` : "Recently updated document",
      href: createEditorHash(document.id),
      id: document.id,
      path: `${libraryName} / ${folderTitlesById.get(document.folderId) ?? "Folder"}`,
      status: document.status.toLowerCase() === "published" ? "Published" : "Draft",
      title: document.title,
      type: "document",
      updatedAt: formatDate(document.updatedAt),
    }));

  const folderResults = bootstrap.folders
    .filter((folder) => folder.documentCount > 0)
    .slice(0, 3)
    .map((folder): SearchDisplayResult => ({
      documentCount: folder.documentCount,
      excerpt: "Folder",
      href: createLibrariesHash({ collectionId: folder.id, libraryId: resolvedLibraryId }),
      id: folder.id,
      path: libraryName,
      title: folder.title,
      type: "collection",
    }));

  return [...documentResults, ...folderResults].slice(0, 8);
}

function toLibraryFolderSearchResultItems(
  bootstrap: BootstrapResponse,
  query: string,
  libraryId?: string | null,
): SearchDisplayResult[] {
  const resolvedLibraryId = libraryId ?? bootstrap.activeSpaceId;
  const libraryName = getLibraryName(bootstrap, resolvedLibraryId);
  const normalizedQuery = query.trim().toLowerCase();

  return bootstrap.folders
    .filter((folder) => matchesSearchText(`${folder.title} ${libraryName}`, normalizedQuery))
    .sort((left, right) => right.documentCount - left.documentCount)
    .slice(0, 4)
    .map((folder): SearchDisplayResult => ({
      documentCount: folder.documentCount,
      excerpt: "Folder",
      href: createLibrariesHash({ collectionId: folder.id, libraryId: resolvedLibraryId }),
      id: folder.id,
      path: libraryName,
      title: folder.title,
      type: "collection",
    }));
}

function getDemoResults(query: string): SearchDisplayResult[] {
  const normalizedQuery = query.trim().toLowerCase();
  return searchResults
    .filter((result) => !normalizedQuery || matchesDemoResult(result, normalizedQuery))
    .sort((left, right) => scoreDemoResult(right, normalizedQuery) - scoreDemoResult(left, normalizedQuery))
    .slice(0, 8)
    .map(toDemoSearchDisplayResult);
}

function toDemoSearchDisplayResult(result: SearchResultItem): SearchDisplayResult {
  return {
    collaboratorCount: result.collaboratorCount,
    documentCount: result.documentCount,
    excerpt: result.excerpt,
    href: result.type === "document" ? "#editor" : "#libraries",
    id: result.id,
    owner: result.owner,
    path: result.path,
    status: result.status,
    title: result.title,
    type: result.type,
    updatedAt: result.updatedAt,
  };
}

function getMatchingActions(query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return commandActions;
  }

  return commandActions.filter((action) =>
    matchesSearchText(
      `${action.title} ${action.detail} ${action.path} ${action.keywords.join(" ")}`,
      normalizedQuery,
    ),
  );
}

function getTabCounts(entries: SearchCommandEntry[]): Record<SearchCommandTab, number> {
  const documents = entries.filter((entry) => entry.kind === "document").length;
  const folders = entries.filter((entry) => entry.kind === "collection").length;
  const actions = entries.filter((entry) => entry.kind === "action").length;

  return {
    actions,
    all: entries.length,
    documents,
    folders,
  };
}

function filterEntriesByTab(entries: SearchCommandEntry[], activeTab: SearchCommandTab) {
  if (activeTab === "documents") {
    return entries.filter((entry) => entry.kind === "document");
  }

  if (activeTab === "folders") {
    return entries.filter((entry) => entry.kind === "collection");
  }

  if (activeTab === "actions") {
    return entries.filter((entry) => entry.kind === "action");
  }

  return entries;
}

function groupEntries(entries: SearchCommandEntry[]) {
  return {
    actions: entries.filter((entry) => entry.kind === "action"),
    documents: entries.filter((entry) => entry.kind === "document"),
    folders: entries.filter((entry) => entry.kind === "collection"),
  };
}

function mergeResults(results: SearchDisplayResult[]) {
  const seenKeys = new Set<string>();
  return results.filter((result) => {
    const key = `${result.type}:${result.id}`;
    if (seenKeys.has(key)) {
      return false;
    }

    seenKeys.add(key);
    return true;
  }).slice(0, 12);
}

function matchesDemoResult(result: SearchResultItem, normalizedQuery: string) {
  return matchesSearchText(
    [
      result.title,
      result.path,
      result.excerpt,
      result.owner,
      result.status,
      result.type === "collection" ? "folder" : "document",
    ].filter(Boolean).join(" "),
    normalizedQuery,
  );
}

function matchesSearchText(text: string, normalizedQuery: string) {
  const terms = normalizedQuery.split(/\s+/).filter(Boolean);
  const normalizedText = text.toLowerCase();
  return terms.every((term) => normalizedText.includes(term));
}

function scoreDemoResult(result: SearchResultItem, normalizedQuery: string) {
  if (!normalizedQuery) {
    return result.selected ? 10 : result.type === "document" ? 4 : 3;
  }

  const title = result.title.toLowerCase();
  if (title === normalizedQuery) {
    return 40;
  }

  if (title.startsWith(normalizedQuery)) {
    return 28;
  }

  if (title.includes(normalizedQuery)) {
    return 18;
  }

  return result.type === "document" ? 8 : 6;
}

function getLibraryName(bootstrap: BootstrapResponse, libraryId?: string | null) {
  const library = bootstrap.spaces.find((space) => space.id === libraryId)
    ?? bootstrap.spaces.find((space) => space.id === bootstrap.activeSpaceId)
    ?? bootstrap.spaces.find((space) => space.id === bootstrap.workspace.currentSpaceId);

  return library?.name ?? "Library";
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

function getShortcutLabel() {
  return /mac|iphone|ipad|ipod/i.test(window.navigator.platform) ? "⌘K" : "Ctrl K";
}
