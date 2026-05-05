import {
  ArrowUp,
  CheckCircle2,
  ChevronDown,
  Circle,
  Crosshair,
  FileText,
  Info,
  Layers3,
  Link,
  MessageSquare,
  Plus,
  Send,
  Upload,
  Users,
  type LucideIcon,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import { getBootstrap, type BootstrapResponse, type KnowledgeDocumentSummaryDto, type KnowledgeFolderDto } from "../lib/appApi";
import {
  activeDocumentRows,
  collectionSpotlightCounts,
  dashboardActivity,
  dashboardInsights,
  dashboardPeople,
  documentOwnerIdsByDocumentId,
  getDashboardPerson,
  pinnedCollectionIds,
  workspaceUpdates,
  type ActiveDocumentRow,
  type CollectionSpotlight,
  type DashboardActivity,
  type DashboardInsight,
  type DashboardUpdate,
} from "../data/workspaceHomeData";
import { initialKnowledgeDocuments, knowledgeFolders } from "../data/knowledgeDocuments";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";
import dashedRoutePatternUrl from "../assets/svg/decorative/dashed-route-lines.svg";
import type { KnowledgeDocument } from "../types/editor";

type StatCard = {
  id: string;
  label: string;
  value: string;
  detail: string;
  icon: LucideIcon;
};

const workspaceHomePatternStyle = {
  "--workspace-home-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--workspace-home-route-pattern": `url(${routePatternUrl})`,
  "--workspace-home-bottom-route-pattern": `url(${dashedRoutePatternUrl})`,
  "--workspace-home-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

export function WorkspaceHomePage() {
  const bootstrap = useBootstrapData();
  const recentDocuments = useMemo(() => createRecentDocuments(bootstrap.data), [bootstrap.data]);
  const collectionSpotlights = useMemo(() => createCollectionSpotlights(bootstrap.data), [bootstrap.data]);
  const workspaceName = bootstrap.data?.workspace.name ?? "Northstar";
  const statCards: StatCard[] = [
    {
      id: "documents",
      label: "Documents",
      value: String(bootstrap.data?.documents.length ?? 213),
      detail: bootstrap.status === "ready" ? "Live bootstrap" : "Demo fallback",
      icon: FileText,
    },
    {
      id: "collections",
      label: "Collections",
      value: String(bootstrap.data?.folders.length ?? 48),
      detail: bootstrap.status === "ready" ? "Live bootstrap" : "Demo fallback",
      icon: Layers3,
    },
    {
      id: "active-documents",
      label: "Active Documents",
      value: "7",
      detail: "2 new this week",
      icon: Crosshair,
    },
    {
      id: "team-members",
      label: "Team Members",
      value: "24",
      detail: "2 this week",
      icon: Users,
    },
  ];

  return (
    <main className="workspace-home-shell flex h-screen flex-col overflow-hidden" style={workspaceHomePatternStyle}>
      <WorkspaceHomeTopBar />
      <div className="workspace-home-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar starredCollections={collectionSpotlights} />
        <section className="workspace-home-content editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="workspace-home-mobile-nav md:hidden" aria-label="Workspace navigation">
            <a aria-current="page" href="#home">
              Home
            </a>
            <a href="#editor">Library</a>
            <a href="#editor">Documents</a>
            <a href="#home">Activity</a>
          </div>

          <div className="workspace-home-grid">
            <div className="workspace-home-main">
              <header className="workspace-home-heading">
                <div className="min-w-0">
                  <h1>Welcome back, {workspaceName}</h1>
                  <p>{bootstrapStatusLabel(bootstrap.status)}</p>
                </div>
                <a className="workspace-home-primary-action" href="#editor" title="Create a document in the editor">
                  <Plus className="h-4 w-4" />
                  <span>New Document</span>
                  <ChevronDown className="h-4 w-4" />
                </a>
              </header>

              <section className="workspace-home-stat-grid" aria-label="Workspace summary">
                {statCards.map((card) => (
                  <DashboardStatCard card={card} key={card.id} />
                ))}
              </section>

              <div className="workspace-home-section-grid">
                <DashboardPanel action="View all" title="Recent Documents">
                  <div className="workspace-home-list">
                    {recentDocuments.map((document, index) => (
                      <RecentDocumentRow document={document} index={index} key={document.id} />
                    ))}
                  </div>
                </DashboardPanel>

                <DashboardPanel action="View all" title="Pinned Collections">
                  <div className="workspace-home-list">
                    {collectionSpotlights.map((collection) => (
                      <CollectionRow collection={collection} key={collection.id} />
                    ))}
                  </div>
                </DashboardPanel>
              </div>

              <div className="workspace-home-section-grid">
                <DashboardPanel title="Active Documents">
                  <ActiveDocumentsTable rows={activeDocumentRows} />
                </DashboardPanel>

                <DashboardPanel action="View all" title="Recent Activity">
                  <div className="workspace-home-activity-list">
                    {dashboardActivity.map((activity) => (
                      <ActivityRow activity={activity} key={activity.id} />
                    ))}
                  </div>
                </DashboardPanel>
              </div>
            </div>

            <aside className="workspace-home-insights" aria-label="Workspace insights">
              <DashboardPanel title="Insights this week">
                <div className="workspace-home-chart" aria-hidden="true">
                  <svg viewBox="0 0 260 82" role="img">
                    <path d="M0 62 C24 58 34 47 54 52 C78 59 82 30 106 38 C136 48 141 9 165 19 C187 28 197 7 214 21 C230 35 237 27 260 31" />
                    <path d="M0 72 C32 68 43 63 62 65 C84 67 95 45 118 51 C145 59 160 38 182 42 C204 46 218 37 260 45" />
                    <path d="M0 75 C40 74 58 70 80 72 C112 75 132 59 154 62 C188 66 205 57 260 59" />
                  </svg>
                </div>
                <div className="workspace-home-list">
                  {dashboardInsights.map((insight) => (
                    <InsightRow insight={insight} key={insight.id} />
                  ))}
                </div>
              </DashboardPanel>

              <DashboardPanel title="Top contributors">
                <div className="space-y-2.5">
                  {dashboardPeople.map((person) => (
                    <div className="workspace-home-contributor" key={person.id}>
                      <span className="workspace-home-avatar">{person.initials}</span>
                      <span className="min-w-0 flex-1 truncate">{person.name}</span>
                      <span className="tabular-nums text-[var(--ns-slate-700)]">{person.documentCount}</span>
                    </div>
                  ))}
                </div>
                <button className="workspace-home-text-link mt-4" title="View all contributors" type="button">
                  View all contributors
                </button>
              </DashboardPanel>

              <DashboardPanel title="Workspace updates">
                <div className="workspace-home-list">
                  {workspaceUpdates.map((update) => (
                    <WorkspaceUpdateRow key={update.id} update={update} />
                  ))}
                </div>
                <button className="workspace-home-text-link mt-4" title="View all updates" type="button">
                  View all updates
                </button>
              </DashboardPanel>
            </aside>
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

function DashboardStatCard({ card }: { card: StatCard }) {
  const Icon = card.icon;

  return (
    <article className="workspace-home-stat-card">
      <span className="workspace-home-stat-icon">
        <Icon className="h-6 w-6" />
      </span>
      <div className="min-w-0">
        <div className="text-xs font-semibold text-[var(--ns-navy-800)]">{card.label}</div>
        <div className="mt-1 text-2xl leading-none text-[var(--ns-navy-900)]">{card.value}</div>
        <div className="mt-2 flex items-center gap-1.5 text-xs text-[#3f8c86]">
          <ArrowUp className="h-3 w-3" />
          {card.detail}
        </div>
      </div>
    </article>
  );
}

function DashboardPanel({
  action,
  children,
  title,
}: {
  action?: string;
  children: ReactNode;
  title: string;
}) {
  return (
    <section className="workspace-home-panel">
      <div className="workspace-home-panel-header">
        <h2>{title}</h2>
        {action ? (
          <button className="workspace-home-text-link" title={action} type="button">
            {action}
          </button>
        ) : null}
      </div>
      {children}
    </section>
  );
}

function RecentDocumentRow({ document, index }: { document: KnowledgeDocument; index: number }) {
  const person = getDashboardPerson(documentOwnerIdsByDocumentId[document.id] ?? dashboardPeople[0].id);

  return (
    <a className="workspace-home-row" href="#editor" title={document.title}>
      <FileText className="h-4 w-4 shrink-0 text-[var(--ns-slate-500)]" />
      <span className="min-w-0 flex-1 truncate font-semibold text-[var(--ns-blue-600)]">{document.title}</span>
      <span className="hidden w-28 truncate text-[var(--ns-slate-700)] sm:block">{person.name}</span>
      <span className="hidden w-28 text-right text-[var(--ns-slate-700)] sm:block">
        {index === 0 ? "Today, 1:48 PM" : formatDate(document.updatedAt)}
      </span>
      <span className={["workspace-home-status-dot", index < 3 ? "is-green" : "is-blue"].join(" ")} />
    </a>
  );
}

function createRecentDocuments(bootstrap: BootstrapResponse | null): KnowledgeDocument[] {
  if (!bootstrap) {
    return [...initialKnowledgeDocuments]
      .filter((document) => document.id !== "doc-editor-experience")
      .sort((left, right) => new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime())
      .slice(0, 5);
  }

  return [...bootstrap.documents]
    .sort((left, right) => new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime())
    .slice(0, 5)
    .map(toKnowledgeDocument);
}

function CollectionRow({ collection }: { collection: CollectionSpotlight }) {
  return (
    <a className="workspace-home-row" href="#editor" title={collection.displayTitle}>
      <Layers3 className="h-4 w-4 shrink-0 text-[var(--ns-slate-500)]" />
      <span className="min-w-0 flex-1 truncate font-semibold text-[var(--ns-blue-600)]">
        {collection.displayTitle}
      </span>
      <span className="tabular-nums text-[var(--ns-slate-700)]">{collection.documentCount}</span>
    </a>
  );
}

function ActiveDocumentsTable({ rows }: { rows: ActiveDocumentRow[] }) {
  return (
    <div className="workspace-home-table">
      <div className="workspace-home-table-head">
        <span>Document</span>
        <span>Owner</span>
        <span>Status</span>
        <span>Progress</span>
        <span>Updated</span>
      </div>
      {rows.map((row) => {
        const person = getDashboardPerson(row.ownerId);

        return (
          <a className="workspace-home-table-row" href="#editor" key={row.id} title={row.title}>
            <span className="min-w-0 truncate font-semibold text-[var(--ns-blue-600)]">{row.title}</span>
            <span className="workspace-home-table-owner">
              <span className="workspace-home-mini-avatar">{person.initials}</span>
              <span className="min-w-0 truncate">{person.name}</span>
            </span>
            <span className="workspace-home-state">
              <Circle className={["h-2 w-2 fill-current", getStatusClassName(row.status)].join(" ")} />
              {row.status}
            </span>
            <span className="workspace-home-progress">
              <i style={{ width: `${row.progress}%` }} />
              <em>{row.progress}%</em>
            </span>
            <span className="text-right text-[var(--ns-slate-700)]">{row.updatedAt}</span>
          </a>
        );
      })}
    </div>
  );
}

function ActivityRow({ activity }: { activity: DashboardActivity }) {
  const person = getDashboardPerson(activity.actorId);
  const Icon = getActivityIcon(activity.kind);

  return (
    <a className="workspace-home-row" href="#editor" title={`${person.name} ${activity.action} ${activity.target}`}>
      <Icon className="h-4 w-4 shrink-0 text-[var(--ns-slate-500)]" />
      <span className="min-w-0 flex-1 truncate">
        <span className="font-semibold text-[var(--ns-blue-600)]">{person.name}</span>
        <span className="text-[var(--ns-slate-700)]"> {activity.action} </span>
        <span className="font-semibold text-[var(--ns-blue-600)]">{activity.target}</span>
      </span>
      <span className="hidden w-28 text-right text-[var(--ns-slate-700)] sm:block">{activity.date}</span>
    </a>
  );
}

function InsightRow({ insight }: { insight: DashboardInsight }) {
  return (
    <div className="workspace-home-row">
      <Info className="h-4 w-4 shrink-0 text-[var(--ns-slate-500)]" />
      <span className="min-w-0 flex-1 truncate">{insight.label}</span>
      <span className="tabular-nums font-semibold text-[var(--ns-navy-800)]">{insight.value}</span>
      <span className="inline-flex items-center gap-1 text-xs text-[#3f8c86]">
        <ArrowUp className="h-3 w-3" />
        {insight.change}
      </span>
    </div>
  );
}

function WorkspaceUpdateRow({ update }: { update: DashboardUpdate }) {
  const Icon = update.kind === "template" ? CheckCircle2 : update.kind === "map" ? FileText : Upload;

  return (
    <button className="workspace-home-row" title={update.title} type="button">
      <Icon className="h-4 w-4 shrink-0 text-[var(--ns-slate-500)]" />
      <span className="min-w-0 flex-1 truncate text-left font-semibold text-[var(--ns-blue-600)]">
        {update.title}
      </span>
      <span className="hidden w-24 text-right text-[var(--ns-slate-700)] sm:block">{update.date}</span>
    </button>
  );
}

function createCollectionSpotlights(bootstrap: BootstrapResponse | null): CollectionSpotlight[] {
  if (bootstrap) {
    return [...bootstrap.folders]
      .sort((left, right) => Number(left.sortOrder) - Number(right.sortOrder))
      .slice(0, 5)
      .map(toCollectionSpotlight);
  }

  return pinnedCollectionIds
    .map((collectionId) => {
      const folder = knowledgeFolders.find((item) => item.id === collectionId);

      if (!folder) {
        return null;
      }

      return {
        id: collectionId,
        displayTitle: stripCollectionPrefix(folder.title),
        documentCount:
          collectionSpotlightCounts[collectionId] ??
          initialKnowledgeDocuments.filter((document) => document.folderId === collectionId).length,
      };
    })
    .filter((collection): collection is CollectionSpotlight => collection !== null);
}

function toKnowledgeDocument(document: KnowledgeDocumentSummaryDto): KnowledgeDocument {
  return {
    id: document.id,
    title: document.title,
    folderId: document.folderId,
    updatedAt: document.updatedAt,
    tags: document.tags,
    content: { type: "doc", content: [] },
  };
}

function toCollectionSpotlight(folder: KnowledgeFolderDto): CollectionSpotlight {
  return {
    id: folder.id,
    displayTitle: stripCollectionPrefix(folder.title),
    documentCount: folder.documentCount,
  };
}

function useBootstrapData() {
  const [data, setData] = useState<BootstrapResponse | null>(null);
  const [status, setStatus] = useState<"unconfigured" | "loading" | "ready" | "forbidden" | "error">(() =>
    getConfiguredApiBaseUrl() ? "loading" : "unconfigured",
  );

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
  }, []);

  return { data, status };
}

function bootstrapStatusLabel(status: ReturnType<typeof useBootstrapData>["status"]) {
  if (status === "ready") {
    return "Live workspace bootstrap is connected.";
  }

  if (status === "loading") {
    return "Loading workspace bootstrap.";
  }

  if (status === "forbidden") {
    return "Sign in to load live workspace data.";
  }

  if (status === "error") {
    return "Workspace API unavailable; demo data is shown.";
  }

  return "Configure VITE_NORTHSTAR_API_BASE_URL to load live workspace data.";
}

function stripCollectionPrefix(title: string) {
  return title.replace(/^\d+\.\s*/, "");
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

function getStatusClassName(status: ActiveDocumentRow["status"]) {
  if (status === "Review") {
    return "text-[#b57c20]";
  }

  if (status === "Not Started") {
    return "text-[var(--ns-slate-500)]";
  }

  return "text-[var(--ns-blue-600)]";
}

function getActivityIcon(kind: DashboardActivity["kind"]): LucideIcon {
  switch (kind) {
    case "published":
      return Send;
    case "commented":
      return MessageSquare;
    case "created":
      return Plus;
    case "moved":
      return Link;
    case "edited":
    default:
      return FileText;
  }
}
