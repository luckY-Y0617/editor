import {
  CalendarDays,
  Check,
  ChevronDown,
  FileText,
  Folder,
  ListFilter,
  MoreHorizontal,
  Plus,
  Search,
  UserRound,
  X,
} from "lucide-react";
import { type CSSProperties, type FormEvent, type ReactNode, useEffect, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import { getBootstrap, searchKnowledge } from "../lib/appApi";
import { createLibrariesHash, createSearchHash, getSearchFiltersFromHash } from "../lib/hashRouting";
import {
  getSearchEmptyState,
  getSearchStatusLabel,
  toFolderSearchResultItems,
  toSearchResultItems,
  type SearchApiStatus,
  type SearchDisplayResult,
} from "../lib/searchDiscoveryModel";
import {
  searchFilterGroups,
  searchResults,
  selectedSearchDetail,
  type SearchFilterGroup,
  type SearchFilterOption,
  type SearchResultItem,
} from "../data/searchDiscoveryData";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

const searchPatternStyle = {
  "--search-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--search-topographic-pattern": `url(${topographicPatternUrl})`,
  "--workspace-home-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--workspace-home-route-pattern": `url(${routePatternUrl})`,
  "--workspace-home-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

const resultTabs = ["All", "Documents (128)", "Folders (24)", "People (18)", "Tags (64)"];

export function SearchDiscoveryPage() {
  const [hash, setHash] = useState(window.location.hash);
  const searchFilters = getSearchFiltersFromHash(hash);
  const searchQuery = searchFilters.q ?? "";
  const [draftQuery, setDraftQuery] = useState(searchQuery);
  const [isPreviewOpen, setIsPreviewOpen] = useState(true);
  const liveSearch = useLiveSearch(searchQuery, searchFilters.folderId);
  const isDemoSearch = liveSearch.status === "unconfigured";
  const displayResults = liveSearch.status === "unconfigured" ? searchResults : liveSearch.results ?? [];
  const tabs = isDemoSearch ? resultTabs : ["All"];
  const resultCount = displayResults.length;
  const headingLabel = searchFilters.folderTitle
    ? `Folder ${searchFilters.folderTitle}`
    : searchQuery
      ? `Search results for '${searchQuery}'`
      : "Search Northstar";

  useEffect(() => {
    const syncHash = () => setHash(window.location.hash);
    window.addEventListener("hashchange", syncHash);
    return () => window.removeEventListener("hashchange", syncHash);
  }, []);

  useEffect(() => {
    setDraftQuery(searchQuery);
    setIsPreviewOpen(true);
  }, [searchQuery]);

  const submitSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    window.location.hash = createSearchHash({
      folderId: searchFilters.folderId,
      folderTitle: searchFilters.folderTitle,
      q: draftQuery.trim(),
    });
  };

  const clearSearch = () => {
    setDraftQuery("");
    window.location.hash = createSearchHash({
      folderId: searchFilters.folderId,
      folderTitle: searchFilters.folderTitle,
    });
  };

  return (
    <main className="search-discovery-shell flex h-screen flex-col overflow-hidden" style={searchPatternStyle}>
      <WorkspaceHomeTopBar searchHref="#search" searchValue={searchQuery} />
      <div className="search-discovery-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar activeItem="search" showCollections={false} />
        <section className="search-discovery-results editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="search-discovery-results-inner">
            <header className="search-discovery-heading">
              <h1>{headingLabel}</h1>
              <p className="share-permissions-inline-status">{getSearchStatusLabel(liveSearch.status)}</p>
            </header>

            <form className="search-discovery-query-form" onSubmit={submitSearch}>
              <label>
                <Search className="h-4 w-4" />
                <span className="sr-only">Search query</span>
                <input
                  autoComplete="off"
                  onChange={(event) => setDraftQuery(event.currentTarget.value)}
                  placeholder="Search Northstar"
                  type="search"
                  value={draftQuery}
                />
              </label>
              {draftQuery ? (
                <button aria-label="Clear search" onClick={clearSearch} title="Clear search" type="button">
                  <X className="h-4 w-4" />
                </button>
              ) : null}
              <button title="Run search" type="submit">
                Search
              </button>
            </form>

            <nav className="search-discovery-tabs" aria-label="Search result types">
              {tabs.map((tab, index) => (
                <button
                  className={["search-discovery-tab", index === 0 ? "is-active" : ""].join(" ")}
                  disabled={index !== 0}
                  key={tab}
                  title={index === 0 ? tab : "Type filters are not connected in this view."}
                  type="button"
                >
                  {tab}
                </button>
              ))}
            </nav>

            <div className="search-discovery-list-toolbar">
              <span>{resultCount} results</span>
              <div className="ml-auto flex items-center gap-3">
                <span>Sort by</span>
                <button className="search-discovery-sort" disabled title="Search results are ordered by relevance." type="button">
                  Relevance
                  <ChevronDown className="h-3.5 w-3.5" />
                </button>
                <button className="search-discovery-view-button" disabled title="Only list view is available." type="button">
                  <ListFilter className="h-4 w-4" />
                </button>
              </div>
            </div>

            <div className="search-discovery-list">
              {displayResults.length > 0 ? (
                displayResults.map((result) => <SearchResultRow key={result.id} query={searchQuery} result={result} />)
              ) : (
                <div className="search-discovery-result is-empty">
                  <div className="min-w-0">
                    <h2>{getSearchEmptyState(liveSearch.status).title}</h2>
                    <p className="search-discovery-path">{getSearchEmptyState(liveSearch.status).detail}</p>
                  </div>
                </div>
              )}
            </div>
          </div>
        </section>
        {isDemoSearch && isPreviewOpen ? <SearchPreview onClose={() => setIsPreviewOpen(false)} query={searchQuery} /> : null}
      </div>
    </main>
  );
}

function useLiveSearch(query: string, folderId: string | null) {
  const [results, setResults] = useState<SearchDisplayResult[] | null>(null);
  const [status, setStatus] = useState<SearchApiStatus>(() =>
    getConfiguredApiBaseUrl() ? "loading" : "unconfigured",
  );

  useEffect(() => {
    if (!getConfiguredApiBaseUrl()) {
      setResults(null);
      setStatus("unconfigured");
      return;
    }

    if (!folderId && !query.trim()) {
      setResults([]);
      setStatus("idle");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getBootstrap(controller.signal)
      .then((bootstrap) =>
        folderId
          ? toFolderSearchResultItems(bootstrap, folderId, query)
          : searchKnowledge({ q: query, spaceId: bootstrap.activeSpaceId }, controller.signal)
            .then((response) => toSearchResultItems(response, bootstrap)),
      )
      .then((nextResults) => {
        setResults(nextResults);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setResults(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [folderId, query]);

  return { results, status };
}

function SearchFilters() {
  return (
    <aside className="search-discovery-filters hidden h-full w-[344px] shrink-0 overflow-hidden border-r border-[var(--ns-border)] lg:flex lg:flex-col">
      <div className="search-discovery-ruler" aria-hidden="true">
        <span>N 90</span>
        <span>N 60</span>
        <span>N 30</span>
        <span>0</span>
        <span>S 30</span>
        <span>S 60</span>
      </div>
      <div className="search-discovery-filter-content editor-scrollbar min-h-0 flex-1 overflow-y-auto px-6 py-6 pl-[78px]">
        <div className="mb-5 flex items-center justify-between border-b border-[var(--ns-border)] pb-4">
          <h2 className="m-0 font-serif text-2xl font-medium text-[var(--ns-navy-900)]">Filters</h2>
          <button className="text-xs font-semibold text-[var(--ns-navy-800)] underline underline-offset-2" type="button">
            Clear all
          </button>
        </div>
        {searchFilterGroups.map((group) => (
          <FilterGroup group={group} key={group.id} />
        ))}
      </div>
    </aside>
  );
}

function FilterGroup({ group }: { group: SearchFilterGroup }) {
  return (
    <section className="search-discovery-filter-group">
      <h3>{group.title}</h3>
      <div className="space-y-2">
        {group.options.map((option) => (
          <FilterOption group={group} key={option.id} option={option} />
        ))}
      </div>
    </section>
  );
}

function FilterOption({ group, option }: { group: SearchFilterGroup; option: SearchFilterOption }) {
  return (
    <label className="search-discovery-filter-option">
      <input
        checked={option.selected}
        readOnly
        type={group.mode === "radio" ? "radio" : "checkbox"}
      />
      <span className={group.mode === "radio" ? "is-radio" : ""}>
        {option.selected && group.mode === "checkbox" ? <Check className="h-3 w-3" /> : null}
      </span>
      <span className="min-w-0 flex-1 truncate">{option.label}</span>
      {option.id === "custom" ? <CalendarDays className="h-3.5 w-3.5 text-[var(--ns-slate-500)]" /> : null}
      {typeof option.count === "number" ? (
        <span className="tabular-nums text-[var(--ns-slate-500)]">{option.count}</span>
      ) : null}
    </label>
  );
}

function SearchResultRow({ query, result }: { query: string; result: SearchDisplayResult | SearchResultItem }) {
  const Icon = result.type === "collection" ? Folder : result.type === "person" ? UserRound : FileText;
  const href = "href" in result
    ? result.href
    : result.type === "document"
      ? "#editor"
      : result.type === "collection"
        ? createLibrariesHash({ collectionId: result.id })
        : "#workspace-members";

  return (
    <a
      aria-label={`Open ${result.title}`}
      className={["search-discovery-result", result.selected ? "is-selected" : ""].join(" ")}
      href={href}
      title={result.title}
    >
      <div className="search-discovery-result-icon">
        {result.type === "person" ? <span>AK</span> : <Icon className="h-5 w-5" />}
      </div>
      <div className="min-w-0">
        <h2>{highlightQuery(result.title, query)}</h2>
        <p className="search-discovery-path">{result.path}</p>
        {result.excerpt ? <p className="search-discovery-excerpt">{highlightQuery(result.excerpt, query)}</p> : null}
        {result.type === "collection" ? (
          <p className="search-discovery-path">
            Folder&nbsp;&nbsp;-&nbsp;&nbsp;{result.documentCount} documents
          </p>
        ) : null}
        {result.type === "person" ? <p className="search-discovery-path">People</p> : null}
      </div>
      <div className="search-discovery-result-meta">
        {result.updatedAt ? <span>{result.updatedAt}</span> : null}
        {result.owner ? (
          <span className="search-discovery-owner">
            <span className="search-discovery-mini-face">{getInitials(result.owner)}</span>
            {result.owner}
          </span>
        ) : null}
        {result.collaboratorCount ? (
          <span className="search-discovery-owner">
            <span className="search-discovery-mini-face">AK</span>
            <span className="search-discovery-mini-face is-overlap">BP</span>
            {result.collaboratorCount} collaborators
          </span>
        ) : null}
        {result.type === "person" ? (
          <>
            <span>{result.documentCount} documents</span>
            <span>{result.collectionCount} folders</span>
          </>
        ) : null}
        {result.status ? <StatusBadge status={result.status} /> : <MoreHorizontal className="ml-auto h-4 w-4" />}
      </div>
    </a>
  );
}

function SearchPreview({ onClose, query }: { onClose: () => void; query: string }) {
  return (
    <aside className="search-discovery-preview hidden h-full w-[470px] shrink-0 overflow-y-auto border-l border-[var(--ns-border)] xl:block">
      <div className="search-discovery-preview-inner">
        <button className="search-discovery-close" onClick={onClose} title="Close preview" type="button">
          <X className="h-5 w-5" />
        </button>

        <header className="search-discovery-preview-header">
          <FileText className="mt-1 h-5 w-5 shrink-0 text-[var(--ns-slate-500)]" />
          <div className="min-w-0">
            <h2>
              {selectedSearchDetail.code} {highlightQuery(selectedSearchDetail.title, query)}
            </h2>
            <div className="mt-2 flex flex-wrap items-center gap-2">
              <StatusBadge status={selectedSearchDetail.status} />
              <span className="text-[var(--ns-slate-500)]">-</span>
              <span className="text-xs text-[var(--ns-slate-700)]">
                Last updated {selectedSearchDetail.updatedAt}
              </span>
            </div>
          </div>
        </header>

        <p className="search-discovery-path mt-4">{selectedSearchDetail.path}</p>

        <PreviewSection title="Summary">
          <p>{selectedSearchDetail.summary}</p>
        </PreviewSection>

        <PreviewSection title="Key Elements">
          <ul className="search-discovery-key-list">
            {selectedSearchDetail.keyElements.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </PreviewSection>

        <PreviewSection title="Excerpt">
          <div className="search-discovery-excerpt-box">{highlightQuery(selectedSearchDetail.excerpt, query)}</div>
        </PreviewSection>

        <PreviewSection title="Tags">
          <div className="flex flex-wrap gap-2">
            {selectedSearchDetail.tags.map((tag) => (
              <span className="search-discovery-tag" key={tag}>
                {tag}
              </span>
            ))}
            <button className="search-discovery-tag is-add" disabled title="Tag management is not available from search preview." type="button">
              <Plus className="h-3.5 w-3.5" />
            </button>
          </div>
        </PreviewSection>

        <PreviewSection title="Related Documents">
          <div className="space-y-2">
            {selectedSearchDetail.relatedDocuments.map((document) => (
              <a className="search-discovery-related" href="#editor" key={`${document.code}-${document.title}`}>
                <FileText className="h-4 w-4 shrink-0 text-[var(--ns-slate-500)]" />
                <span className="font-semibold text-[var(--ns-blue-600)]">{document.code}</span>
                <span className="min-w-0 flex-1 truncate">{document.title}</span>
                <span>{document.updatedAt}</span>
              </a>
            ))}
          </div>
          <a className="search-discovery-preview-link mt-3 inline-flex" href={createSearchHash({ q: selectedSearchDetail.title })}>
            View all 8 related documents
          </a>
        </PreviewSection>

        <PreviewSection title="Details">
          <dl className="search-discovery-details">
            {selectedSearchDetail.details.map((item) => (
              <div key={item.label}>
                <dt>{item.label}</dt>
                <dd className={item.label === "Status" ? "is-positive" : ""}>{item.value}</dd>
              </div>
            ))}
          </dl>
        </PreviewSection>
      </div>
    </aside>
  );
}

function PreviewSection({ children, title }: { children: ReactNode; title: string }) {
  return (
    <section className="search-discovery-preview-section">
      <h3>{title}</h3>
      {children}
    </section>
  );
}

function StatusBadge({ status }: { status: "Published" | "Draft" }) {
  return (
    <span className={["search-discovery-status", status === "Draft" ? "is-draft" : ""].join(" ")}>
      {status}
    </span>
  );
}

function highlightQuery(text: string, query: string): ReactNode {
  const queryParts = query
    .split(/\s+/)
    .map((part) => part.trim())
    .filter(Boolean);

  if (queryParts.length === 0) {
    return text;
  }

  const pattern = new RegExp(`(${queryParts.map(escapeRegExp).join("|")})`, "gi");
  const parts = text.split(pattern).filter(Boolean);

  return parts.map((part, index) =>
    queryParts.some((queryPart) => queryPart.toLowerCase() === part.toLowerCase()) ? (
      <mark key={`${part}-${index}`}>{part}</mark>
    ) : (
      <span key={`${part}-${index}`}>{part}</span>
    ),
  );
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function getInitials(name: string) {
  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}
