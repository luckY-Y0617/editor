import {
  AtSign,
  Bell,
  CalendarDays,
  CheckCircle2,
  Clock3,
  FileText,
  Hourglass,
  Link,
  ListChecks,
  MessageSquare,
  MoreHorizontal,
  PenSquare,
  Plus,
  Send,
  Share2,
  UserRound,
  Users,
  type LucideIcon,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl, isUuid } from "../lib/apiClient";
import {
  getBootstrap,
  getDocumentActivity,
  getWorkspaceMembers,
  getWorkspaceNotifications,
  type BootstrapResponse,
  type DocumentActivityResponse,
} from "../lib/appApi";
import {
  createHomeQuickActions,
  createDemoWorkspaceHomeModel,
  createEmptyWorkspaceHomeModel,
  createLiveWorkspaceHomeModel,
  type HomeActivityRow,
  type HomeAgendaRow,
  type HomeAttentionRow,
  type HomeConversationRow,
  type HomeContributorRow,
  type HomeDocumentRow,
  type HomeQuickActionRow,
  type HomeSignalRow,
  type WorkspaceHomeModel,
  type WorkspaceHomeSupplementalData,
} from "../lib/workspaceHomeModel";
import { getQuickActionLabel, t, type DisplayLocale, useDisplayLanguage } from "../lib/i18n";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";
import dashedRoutePatternUrl from "../assets/svg/decorative/dashed-route-lines.svg";

type HomeDataStatus = "idle" | "loading" | "ready" | "forbidden" | "error";

type HomeSupplementalState = {
  activity: HomeDataStatus;
  contributors: HomeDataStatus;
  data: WorkspaceHomeSupplementalData;
  updates: HomeDataStatus;
};

type QuickAction = {
  icon: LucideIcon;
};

const quickActionIcons: Record<HomeQuickActionRow["id"], QuickAction> = {
  "log-note": { icon: PenSquare },
  "more-actions": { icon: MoreHorizontal },
  "new-decision": { icon: Hourglass },
  "new-document": { icon: FileText },
  "request-access": { icon: Users },
  "share-update": { icon: Share2 },
};

const workspaceHomePatternStyle = {
  "--workspace-home-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--workspace-home-route-pattern": `url(${routePatternUrl})`,
  "--workspace-home-bottom-route-pattern": `url(${dashedRoutePatternUrl})`,
  "--workspace-home-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

export function WorkspaceHomePage() {
  const { locale } = useDisplayLanguage();
  const [bootstrapRetryKey, setBootstrapRetryKey] = useState(0);
  const [supplementalRetryKey, setSupplementalRetryKey] = useState(0);
  const bootstrap = useBootstrapData(bootstrapRetryKey);
  const supplemental = useHomeSupplementalData(bootstrap.data, bootstrap.status, supplementalRetryKey);
  const homeModel = useMemo(() => {
    if (bootstrap.status === "ready" && bootstrap.data) {
      return createLiveWorkspaceHomeModel(bootstrap.data, supplemental.data);
    }

    if (bootstrap.status === "unconfigured") {
      return createDemoWorkspaceHomeModel();
    }

    return createEmptyWorkspaceHomeModel(bootstrap.status);
  }, [bootstrap.data, bootstrap.status, supplemental.data]);

  const canRetry = bootstrap.status === "error" || bootstrap.status === "forbidden";

  return (
    <main className="workspace-home-shell flex h-screen flex-col overflow-hidden" style={workspaceHomePatternStyle}>
      <WorkspaceHomeTopBar />
      <div className="workspace-home-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar
          activeItem="home"
          currentLibraryCollections={homeModel.collections}
        />
        <section className="workspace-home-content editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <MobileHomeNav locale={locale} />

          <div className="workspace-home-dashboard">
            <div className="workspace-home-dashboard-main">
              <header className="workspace-home-heading">
                <div className="min-w-0">
                  <h1>{t(locale, "home.goodMorning", { workspaceName: homeModel.workspaceName })}</h1>
                  <p>{bootstrapStatusLabel(bootstrap.status, locale)}</p>
                  {canRetry ? (
                    <button
                      className="workspace-home-text-link mt-2"
                      onClick={() => setBootstrapRetryKey((current) => current + 1)}
                      type="button"
                    >
                      {t(locale, "common.retry")}
                    </button>
                  ) : null}
                </div>
              </header>

              <QuickActions actions={createHomeQuickActions(homeModel)} locale={locale} />

              <div className="workspace-home-top-card-grid">
                <HomePanel icon={CalendarDays} title={t(locale, "home.today")}>
                  <p className="workspace-home-panel-kicker">{getTodayLabel(locale)}</p>
                  <AgendaList items={homeModel.agendaRows} />
                  <PanelLink href="#updates">View full agenda</PanelLink>
                </HomePanel>

                <HomePanel badge={homeModel.waitingRows.length} icon={ListChecks} title={t(locale, "home.waitingOnYou")}>
                  <AttentionList items={homeModel.waitingRows} status={supplemental.updates} />
                  <PanelLink href="#updates">View all</PanelLink>
                </HomePanel>

                <HomePanel badge={homeModel.conversationRows.length} icon={MessageSquare} title={t(locale, "home.activeConversations")}>
                  <ConversationList emptyLabel="No active conversation data." items={homeModel.conversationRows} />
                  <PanelLink href="#updates">View all</PanelLink>
                </HomePanel>

                <HomePanel icon={Clock3} title={t(locale, "home.recentlyTouched")}>
                  <RecentlyTouchedList documents={homeModel.documentRows} locale={locale} status={bootstrap.status} />
                  <PanelLink href="#libraries">View all</PanelLink>
                </HomePanel>
              </div>

              <div className="workspace-home-bottom-grid">
                <HomePanel actionLabel={t(locale, "home.allActivity")} icon={Users} title={t(locale, "home.teamActivity")}>
                  <TeamActivityList
                    items={homeModel.activityRows}
                    locale={locale}
                    mode={homeModel.mode}
                    onRetry={() => setSupplementalRetryKey((current) => current + 1)}
                    status={supplemental.activity}
                  />
                  <PanelLink href="#updates">View all activity</PanelLink>
                </HomePanel>

                <HomePanel actionLabel="View archive" icon={MessageSquare} title={t(locale, "home.recentConversationsAndDecisions")}>
                  <ConversationList emptyLabel="No recent conversation or decision data." items={homeModel.recentDecisionRows} variant="stacked" />
                  <PanelLink href="#updates">View all conversations</PanelLink>
                </HomePanel>
              </div>
            </div>

            <aside className="workspace-home-signal-rail" aria-label={t(locale, "home.workspaceSignals")}>
              <HomePanel icon={Bell} title={t(locale, "home.workspaceSignals")}>
                <SignalList items={homeModel.signalRows} />
                <PanelLink href="#updates">View all signals</PanelLink>
              </HomePanel>

              <HomePanel icon={Users} title={t(locale, "home.topContributors")}>
                <p className="workspace-home-panel-kicker">{homeModel.mode === "live" ? t(locale, "home.workspaceMembers") : "30 days"}</p>
                <ContributorList items={homeModel.contributorRows} status={supplemental.contributors} />
                <PanelLink href="#workspace-members">View leaderboard</PanelLink>
              </HomePanel>

              <HomePanel icon={AtSign} title={t(locale, "home.notificationDigest")}>
                <SignalList items={homeModel.digestRows} />
                <PanelLink href="#updates">View all notifications</PanelLink>
              </HomePanel>
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

function MobileHomeNav({ locale }: { locale: DisplayLocale }) {
  return (
    <div className="workspace-home-mobile-nav md:hidden" aria-label="Workspace navigation">
      <a aria-current="page" href="#home">
        {t(locale, "nav.home")}
      </a>
      <a href="#libraries">{t(locale, "nav.libraries")}</a>
      <a href="#search">{t(locale, "nav.search")}</a>
      <a href="#updates">{t(locale, "nav.updates")}</a>
      <a href="#workspace-members">{t(locale, "nav.members")}</a>
      <a href="#settings">{t(locale, "nav.settings")}</a>
    </div>
  );
}

function QuickActions({ actions, locale }: { actions: HomeQuickActionRow[]; locale: DisplayLocale }) {
  return (
    <section className="workspace-home-quick-actions" aria-label={t(locale, "home.quickActions")}>
      <h2>{t(locale, "home.quickActions")}</h2>
      <div>
        {actions.map((action) => {
          const Icon = quickActionIcons[action.id].icon;
          const label = getQuickActionLabel(locale, action.id);

          return action.isEnabled && action.href ? (
            <a href={action.href} key={action.id} title={label}>
              <Icon className="h-4 w-4" />
              <span>{label}</span>
            </a>
          ) : (
            <button disabled key={action.id} title={action.disabledReason ?? `${label} is unavailable`} type="button">
              <Icon className="h-4 w-4" />
              <span>{label}</span>
            </button>
          );
        })}
      </div>
    </section>
  );
}

function HomePanel({
  actionLabel,
  badge,
  children,
  icon: Icon,
  title,
}: {
  actionLabel?: string;
  badge?: number;
  children: ReactNode;
  icon: LucideIcon;
  title: string;
}) {
  return (
    <section className="workspace-home-panel">
      <div className="workspace-home-panel-header">
        <div className="workspace-home-panel-title">
          <span>
            <Icon className="h-4 w-4" />
          </span>
          <h2>{title}</h2>
          {typeof badge === "number" && badge > 0 ? <mark>{badge}</mark> : null}
        </div>
        {actionLabel ? (
          <button className="workspace-home-text-link" title={actionLabel} type="button">
            {actionLabel}
          </button>
        ) : null}
      </div>
      {children}
    </section>
  );
}

function AgendaList({ items }: { items: HomeAgendaRow[] }) {
  if (items.length === 0) {
    return <PanelMessage>No calendar source connected.</PanelMessage>;
  }

  return (
    <div className="workspace-home-agenda-list">
      {items.map((item) => (
        <div className="workspace-home-agenda-row" key={item.id}>
          <time>{item.time}</time>
          <span aria-hidden="true" />
          <div className="min-w-0">
            <strong>{item.title}</strong>
            <p>
              {item.detail}
              {item.meta ? <em>{item.meta}</em> : null}
            </p>
          </div>
        </div>
      ))}
    </div>
  );
}

function AttentionList({ items, status }: { items: HomeAttentionRow[]; status: HomeDataStatus }) {
  if (items.length === 0) {
    return <PanelMessage>{getSupplementalMessage(status, "Nothing waiting on you.")}</PanelMessage>;
  }

  return (
    <div className="workspace-home-compact-list">
      {items.map((item) => (
        <a className="workspace-home-signal-row" href={item.href} key={item.id} title={item.title}>
          <span>
            <Link className="h-4 w-4" />
          </span>
          <div className="min-w-0">
            <strong>{item.title}</strong>
            <p>{item.detail}</p>
          </div>
        </a>
      ))}
    </div>
  );
}

function ConversationList({
  emptyLabel,
  items,
  variant = "compact",
}: {
  emptyLabel: string;
  items: HomeConversationRow[];
  variant?: "compact" | "stacked";
}) {
  if (items.length === 0) {
    return <PanelMessage>{emptyLabel}</PanelMessage>;
  }

  return (
    <div className={variant === "stacked" ? "workspace-home-stacked-list" : "workspace-home-compact-list"}>
      {items.map((item) => {
        const Icon = item.kind === "decision" ? Hourglass : item.kind === "activity" ? FileText : MessageSquare;

        return (
          <a className="workspace-home-signal-row" href={item.href} key={item.id} title={item.title}>
            <span>
              <Icon className="h-4 w-4" />
            </span>
            <div className="min-w-0">
              {variant === "stacked" ? <small>{formatKind(item.kind)}</small> : null}
              <strong>{item.title}</strong>
              <p>{item.detail}</p>
            </div>
            {item.badge ? <mark className={item.tone === "orange" ? "is-orange" : ""}>{item.badge}</mark> : null}
          </a>
        );
      })}
    </div>
  );
}

function RecentlyTouchedList({
  documents,
  locale,
  status,
}: {
  documents: HomeDocumentRow[];
  locale: DisplayLocale;
  status: ReturnType<typeof useBootstrapData>["status"];
}) {
  if (documents.length === 0) {
    return <PanelMessage>{getDocumentListMessage(status)}</PanelMessage>;
  }

  return (
    <div className="workspace-home-compact-list">
      {documents.slice(0, 3).map((document) => (
        <a className="workspace-home-signal-row" href={document.href} key={document.id} title={document.title}>
          <span>
            <FileText className="h-4 w-4" />
          </span>
          <div className="min-w-0">
            <strong>{document.title}</strong>
            <p>
              {formatDate(document.updatedAt, locale)} - {document.folderTitle}
            </p>
          </div>
        </a>
      ))}
    </div>
  );
}

function TeamActivityList({
  items,
  locale,
  mode,
  onRetry,
  status,
}: {
  items: HomeActivityRow[];
  locale: DisplayLocale;
  mode: WorkspaceHomeModel["mode"];
  onRetry: () => void;
  status: HomeDataStatus;
}) {
  if (items.length === 0) {
    return (
      <PanelMessage onRetry={mode === "live" && isRetryableStatus(status) ? onRetry : undefined}>
        {getSupplementalMessage(status, "No active-document activity available.")}
      </PanelMessage>
    );
  }

  return (
    <div className="workspace-home-activity-feed">
      {items.map((item) => (
        <a className="workspace-home-activity-item" href={item.href} key={item.id} title={item.title}>
          <span className="workspace-home-avatar">{getInitials(item.title)}</span>
          <div className="min-w-0">
            <strong>{item.title}</strong>
            <p>{item.detail}</p>
          </div>
          <time>{formatDate(item.date, locale)}</time>
        </a>
      ))}
    </div>
  );
}

function SignalList({ items }: { items: HomeSignalRow[] }) {
  if (items.length === 0) {
    return <PanelMessage>No live signal data.</PanelMessage>;
  }

  return (
    <div className="workspace-home-compact-list">
      {items.map((item) => (
        <a className="workspace-home-signal-row" href={item.href} key={item.id} title={`${item.value} ${item.label}`}>
          <span>
            <Clock3 className="h-4 w-4" />
          </span>
          <div className="min-w-0">
            <strong>
              {item.value} {item.label}
            </strong>
            <p>{item.detail}</p>
          </div>
        </a>
      ))}
    </div>
  );
}

function ContributorList({ items, status }: { items: HomeContributorRow[]; status: HomeDataStatus }) {
  if (items.length === 0) {
    return <PanelMessage>{getSupplementalMessage(status, "No workspace members available.")}</PanelMessage>;
  }

  return (
    <div className="workspace-home-contributor-list">
      {items.slice(0, 3).map((item) => (
        <a className="workspace-home-contributor" href={item.href} key={item.id} title={item.name}>
          <span className="workspace-home-avatar">{item.initials}</span>
          <span className="min-w-0 flex-1">
            <strong>{item.name}</strong>
            <em>{item.contributionLabel}</em>
          </span>
        </a>
      ))}
    </div>
  );
}

function PanelLink({ children, href }: { children: ReactNode; href: string }) {
  return (
    <a className="workspace-home-panel-link" href={href}>
      {children}
      <span aria-hidden="true">{"->"}</span>
    </a>
  );
}

function PanelMessage({ children, onRetry }: { children: ReactNode; onRetry?: () => void }) {
  return (
    <div className="workspace-home-panel-message">
      <span>{children}</span>
      {onRetry ? (
        <button className="workspace-home-text-link" onClick={onRetry} type="button">
          Retry
        </button>
      ) : null}
    </div>
  );
}

function useBootstrapData(retryKey: number) {
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
  }, [retryKey]);

  return { data, status };
}

function useHomeSupplementalData(
  bootstrap: BootstrapResponse | null,
  bootstrapStatus: ReturnType<typeof useBootstrapData>["status"],
  retryKey: number,
) {
  const [state, setState] = useState<HomeSupplementalState>({
    activity: "idle",
    contributors: "idle",
    data: {},
    updates: "idle",
  });

  useEffect(() => {
    if (bootstrapStatus !== "ready" || !bootstrap) {
      setState({
        activity: bootstrapStatus === "loading" ? "loading" : "idle",
        contributors: bootstrapStatus === "loading" ? "loading" : "idle",
        data: {},
        updates: bootstrapStatus === "loading" ? "loading" : "idle",
      });
      return;
    }

    const controller = new AbortController();
    const shouldLoadActivity = isUuid(bootstrap.activeDocumentId);
    setState({
      activity: shouldLoadActivity ? "loading" : "ready",
      contributors: "loading",
      data: {},
      updates: "loading",
    });

    void Promise.allSettled([
      shouldLoadActivity
        ? getDocumentActivity(bootstrap.activeDocumentId, controller.signal)
        : Promise.resolve<DocumentActivityResponse>({ items: [] }),
      getWorkspaceNotifications(bootstrap.workspace.id, controller.signal),
      getWorkspaceMembers(bootstrap.workspace.id, controller.signal),
    ]).then(([activityResult, updatesResult, contributorsResult]) => {
      if (controller.signal.aborted) {
        return;
      }

      setState({
        activity: getRequestStatus(activityResult),
        contributors: getRequestStatus(contributorsResult),
        data: {
          activityItems: activityResult.status === "fulfilled" ? activityResult.value.items : [],
          members: contributorsResult.status === "fulfilled" ? contributorsResult.value.members : [],
          notifications: updatesResult.status === "fulfilled" ? updatesResult.value.notifications : [],
        },
        updates: getRequestStatus(updatesResult),
      });
    });

    return () => controller.abort();
  }, [bootstrap, bootstrapStatus, retryKey]);

  return state;
}

function bootstrapStatusLabel(status: ReturnType<typeof useBootstrapData>["status"], locale: DisplayLocale) {
  if (status === "ready" || status === "unconfigured") {
    return locale === "zh-CN" ? "这是今天工作区的最新情况。" : "Here's what's happening across your workspace today.";
  }

  if (status === "loading") {
    return locale === "zh-CN" ? "正在加载工作区信号。" : "Loading workspace signals.";
  }

  if (status === "forbidden") {
    return locale === "zh-CN" ? "登录后加载实时工作区数据。" : "Sign in to load live workspace data.";
  }

  return locale === "zh-CN"
    ? "工作区 API 不可用；主页显示可用的本地状态。"
    : "Workspace API unavailable; Home is showing available local state.";
}

function getRequestStatus<T>(result: PromiseSettledResult<T>): HomeDataStatus {
  if (result.status === "fulfilled") {
    return "ready";
  }

  if (result.reason instanceof ApiClientError && (result.reason.status === 401 || result.reason.status === 403)) {
    return "forbidden";
  }

  return "error";
}

function getSupplementalMessage(status: HomeDataStatus, emptyMessage: string) {
  if (status === "loading") {
    return "Loading workspace data...";
  }

  if (status === "forbidden") {
    return "Not available for this session.";
  }

  if (status === "error") {
    return "Live workspace data could not be loaded.";
  }

  return emptyMessage;
}

function getDocumentListMessage(status: ReturnType<typeof useBootstrapData>["status"]) {
  if (status === "loading") {
    return "Loading documents...";
  }

  if (status === "ready") {
    return "No documents yet.";
  }

  if (status === "unconfigured") {
    return "No demo documents available.";
  }

  return "Live workspace data could not be loaded.";
}

function isRetryableStatus(status: HomeDataStatus) {
  return status === "error" || status === "forbidden";
}

function formatKind(value: HomeConversationRow["kind"]) {
  if (value === "decision") {
    return "Decision";
  }

  if (value === "activity") {
    return "Activity";
  }

  if (value === "notification") {
    return "Notification";
  }

  return "Conversation";
}

function formatDate(value: string, locale: DisplayLocale = "en") {
  try {
    return new Intl.DateTimeFormat(locale === "zh-CN" ? "zh-CN" : "en-US", {
      month: "short",
      day: "numeric",
    }).format(new Date(value));
  } catch {
    return value;
  }
}

function getTodayLabel(locale: DisplayLocale) {
  return new Intl.DateTimeFormat(locale === "zh-CN" ? "zh-CN" : "en-US", {
    month: "long",
    day: "numeric",
    weekday: "long",
  }).format(new Date());
}

function getInitials(value: string) {
  return value
    .split(/[\s._-]+/)
    .filter(Boolean)
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}
