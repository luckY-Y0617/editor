import {
  CalendarDays,
  Check,
  ChevronDown,
  FileText,
  Folder,
  Info,
  ListFilter,
  MoreHorizontal,
  Plus,
  Upload,
  UserRound,
  X,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useState } from "react";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import { getBootstrap, searchKnowledge, type SearchResponse } from "../lib/appApi";
import {
  searchFilterGroups,
  searchResults,
  selectedSearchDetail,
  type SearchFilterGroup,
  type SearchFilterOption,
  type SearchResultItem,
} from "../data/searchDiscoveryData";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

const searchPatternStyle = {
  "--search-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--search-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

const resultTabs = ["All", "Documents (128)", "Collections (24)", "People (18)", "Tags (64)"];

export function SearchDiscoveryPage() {
  const liveSearch = useLiveSearch("decision framework");
  const displayResults = liveSearch.results ?? searchResults;
  const resultCount = displayResults.length;

  return (
    <main className="search-discovery-shell flex h-screen flex-col overflow-hidden" style={searchPatternStyle}>
      <WorkspaceHomeTopBar searchHref="#search" searchValue="decision framework" />
      <div className="search-discovery-body flex min-h-0 flex-1 overflow-hidden">
        <SearchFilters />
        <section className="search-discovery-results editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="search-discovery-results-inner">
            <header className="search-discovery-heading">
              <h1>Search results for &apos;decision framework&apos;</h1>
              <p className="share-permissions-inline-status">{searchStatusLabel(liveSearch.status)}</p>
            </header>

            <nav className="search-discovery-tabs" aria-label="Search result types">
              {resultTabs.map((tab, index) => (
                <button
                  className={["search-discovery-tab", index === 0 ? "is-active" : ""].join(" ")}
                  key={tab}
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
                <button className="search-discovery-sort" title="Sort by relevance" type="button">
                  Relevance
                  <ChevronDown className="h-3.5 w-3.5" />
                </button>
                <button className="search-discovery-view-button" title="List view" type="button">
                  <ListFilter className="h-4 w-4" />
                </button>
              </div>
            </div>

            <div className="search-discovery-list">
              {displayResults.map((result) => (
                <SearchResultRow key={result.id} result={result} />
              ))}
            </div>
          </div>
        </section>
        <SearchPreview />
      </div>
    </main>
  );
}

function useLiveSearch(query: string) {
  const [results, setResults] = useState<SearchResultItem[] | null>(null);
  const [status, setStatus] = useState<"unconfigured" | "loading" | "ready" | "forbidden" | "error">(() =>
    getConfiguredApiBaseUrl() ? "loading" : "unconfigured",
  );

  useEffect(() => {
    if (!getConfiguredApiBaseUrl()) {
      setResults(null);
      setStatus("unconfigured");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getBootstrap(controller.signal)
      .then((bootstrap) => searchKnowledge({ q: query, spaceId: bootstrap.activeSpaceId }, controller.signal))
      .then((response) => {
        setResults(toSearchResultItems(response));
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
  }, [query]);

  return { results, status };
}

function toSearchResultItems(response: SearchResponse): SearchResultItem[] {
  return response.results.map((result, index) => ({
    id: result.id,
    type: result.type === "collection" || result.type === "person" ? result.type : "document",
    title: result.title,
    path: result.folderId ? `Folder ${result.folderId}` : "Workspace",
    excerpt: result.excerpt,
    updatedAt: formatDate(result.updatedAt),
    status: "Published",
    selected: index === 0,
  }));
}

function searchStatusLabel(status: ReturnType<typeof useLiveSearch>["status"]) {
  if (status === "ready") {
    return "Live search API is connected.";
  }

  if (status === "loading") {
    return "Loading search results.";
  }

  if (status === "forbidden") {
    return "Sign in to load live search results.";
  }

  if (status === "error") {
    return "Search API unavailable; demo results are shown.";
  }

  return "Configure VITE_NORTHSTAR_API_BASE_URL to load live search results.";
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

function SearchResultRow({ result }: { result: SearchResultItem }) {
  const Icon = result.type === "collection" ? Folder : result.type === "person" ? UserRound : FileText;

  return (
    <article className={["search-discovery-result", result.selected ? "is-selected" : ""].join(" ")}>
      <div className="search-discovery-result-icon">
        {result.type === "person" ? <span>AK</span> : <Icon className="h-5 w-5" />}
      </div>
      <div className="min-w-0">
        <h2>{highlightQuery(result.title)}</h2>
        <p className="search-discovery-path">{result.path}</p>
        {result.excerpt ? <p className="search-discovery-excerpt">{highlightQuery(result.excerpt)}</p> : null}
        {result.type === "collection" ? (
          <p className="search-discovery-path">
            Collection&nbsp;&nbsp;-&nbsp;&nbsp;{result.documentCount} documents
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
            <span>{result.collectionCount} collections</span>
          </>
        ) : null}
        {result.status ? <StatusBadge status={result.status} /> : <MoreHorizontal className="ml-auto h-4 w-4" />}
      </div>
    </article>
  );
}

function SearchPreview() {
  return (
    <aside className="search-discovery-preview hidden h-full w-[470px] shrink-0 overflow-y-auto border-l border-[var(--ns-border)] xl:block">
      <div className="search-discovery-preview-inner">
        <button className="search-discovery-close" title="Close preview" type="button">
          <X className="h-5 w-5" />
        </button>

        <header className="search-discovery-preview-header">
          <FileText className="mt-1 h-5 w-5 shrink-0 text-[var(--ns-slate-500)]" />
          <div className="min-w-0">
            <h2>
              {selectedSearchDetail.code} {highlightQuery(selectedSearchDetail.title)}
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
          <div className="search-discovery-excerpt-box">{highlightQuery(selectedSearchDetail.excerpt)}</div>
        </PreviewSection>

        <PreviewSection title="Tags">
          <div className="flex flex-wrap gap-2">
            {selectedSearchDetail.tags.map((tag) => (
              <span className="search-discovery-tag" key={tag}>
                {tag}
              </span>
            ))}
            <button className="search-discovery-tag is-add" title="Add tag" type="button">
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
          <button className="search-discovery-preview-link mt-3" type="button">
            View all 8 related documents
          </button>
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

function highlightQuery(text: string): ReactNode {
  const queryParts = ["Decision Framework", "Decision", "Framework", "decision", "framework"];
  const pattern = new RegExp(`(${queryParts.join("|")})`, "gi");
  const parts = text.split(pattern).filter(Boolean);

  return parts.map((part, index) =>
    queryParts.some((query) => query.toLowerCase() === part.toLowerCase()) ? (
      <mark key={`${part}-${index}`}>{part}</mark>
    ) : (
      <span key={`${part}-${index}`}>{part}</span>
    ),
  );
}

function getInitials(name: string) {
  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}
