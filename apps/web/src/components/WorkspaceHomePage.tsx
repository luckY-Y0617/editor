import {
  AtSign,
  Bell,
  CalendarCheck,
  CalendarDays,
  CheckCircle2,
  Clock3,
  ExternalLink,
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
  X,
  type LucideIcon,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl, isUuid } from "../lib/apiClient";
import {
  getBootstrap,
  getDocumentActivity,
  getWorkspaceAgenda,
  getWorkspaceMembers,
  getWorkspaceNotifications,
  type BootstrapResponse,
  type DocumentActivityResponse,
  type WorkspaceAgendaItemDto,
  type WorkspaceAgendaResponse,
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
import { createEditorHash, createWorkspaceUpdatesHash, normalizeInternalActionHash } from "../lib/hashRouting";
import { getQuickActionLabel, t, type DisplayLocale, useDisplayLanguage } from "../lib/i18n";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";
import dashedRoutePatternUrl from "../assets/svg/decorative/dashed-route-lines.svg";

type HomeDataStatus = "idle" | "loading" | "ready" | "forbidden" | "error";

type HomeSupplementalState = {
  activity: HomeDataStatus;
  agenda: HomeDataStatus;
  contributors: HomeDataStatus;
  data: WorkspaceHomeSupplementalData;
  updates: HomeDataStatus;
};

type AgendaTab = "today" | "week";

type AgendaDisplayItem = {
  calendarStatus?: string;
  category: string;
  connectedToCalendar?: boolean;
  date?: string;
  detail: string;
  durationMinutes?: number;
  endTime?: string | null;
  href?: string;
  id: string;
  kind?: string;
  startTime: string;
  title: string;
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
  const [isAgendaOpen, setIsAgendaOpen] = useState(false);
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
                  <PanelButton onClick={() => setIsAgendaOpen(true)}>View full agenda</PanelButton>
                </HomePanel>

                <HomePanel badge={homeModel.waitingRows.length} icon={ListChecks} title={t(locale, "home.waitingOnYou")}>
                  <AttentionList items={homeModel.waitingRows} status={supplemental.updates} />
                  <PanelLink href={createWorkspaceUpdatesHash({ tab: "access" })}>View access updates</PanelLink>
                </HomePanel>

                <HomePanel badge={homeModel.conversationRows.length} icon={MessageSquare} title={t(locale, "home.activeConversations")}>
                  <ConversationList emptyLabel="No access or sharing updates." items={homeModel.conversationRows} />
                  <PanelLink href={createWorkspaceUpdatesHash()}>View notifications</PanelLink>
                </HomePanel>

                <HomePanel icon={Clock3} title={t(locale, "home.recentlyTouched")}>
                  <RecentlyTouchedList documents={homeModel.documentRows} locale={locale} status={bootstrap.status} />
                  <PanelLink href="#libraries">View all</PanelLink>
                </HomePanel>
              </div>

              <div className="workspace-home-bottom-grid">
                <HomePanel icon={Users} title={t(locale, "home.teamActivity")}>
                  <TeamActivityList
                    items={homeModel.activityRows}
                    locale={locale}
                    mode={homeModel.mode}
                    onRetry={() => setSupplementalRetryKey((current) => current + 1)}
                    status={supplemental.activity}
                  />
                </HomePanel>

                <HomePanel icon={MessageSquare} title={t(locale, "home.recentConversationsAndDecisions")}>
                  <ConversationList emptyLabel="No recent access or sharing context." items={homeModel.recentDecisionRows} variant="stacked" />
                </HomePanel>
              </div>
            </div>

            <aside className="workspace-home-signal-rail" aria-label={t(locale, "home.workspaceSignals")}>
              <HomePanel icon={Bell} title={t(locale, "home.workspaceSignals")}>
                <SignalList items={homeModel.signalRows} />
                <PanelLink href={createWorkspaceUpdatesHash()}>View notification signals</PanelLink>
              </HomePanel>

              <HomePanel icon={Users} title={homeModel.mode === "live" ? t(locale, "home.workspaceMembers") : t(locale, "home.topContributors")}>
                <p className="workspace-home-panel-kicker">{homeModel.mode === "live" ? t(locale, "home.workspaceMembers") : "30 days"}</p>
                <ContributorList items={homeModel.contributorRows} status={supplemental.contributors} />
                <PanelLink href="#members">Open members</PanelLink>
              </HomePanel>

              <HomePanel icon={AtSign} title={t(locale, "home.notificationDigest")}>
                <SignalList items={homeModel.digestRows} />
                <PanelLink href={createWorkspaceUpdatesHash()}>View all notifications</PanelLink>
              </HomePanel>
            </aside>
          </div>

          <div className="workspace-home-coordinate-footer" aria-hidden="true">
            47.61 / 122.33
            <span>+</span>
          </div>
        </section>
      </div>
      {isAgendaOpen ? (
        <HomeAgendaDrawer
          agenda={supplemental.data.agenda}
          fallbackItems={homeModel.agendaRows}
          locale={locale}
          onClose={() => setIsAgendaOpen(false)}
          status={supplemental.agenda}
        />
      ) : null}
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
      <a href="#access-sharing">{t(locale, "nav.updates")}</a>
      <a href="#members">{t(locale, "nav.members")}</a>
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
  actionHref,
  actionLabel,
  badge,
  children,
  icon: Icon,
  title,
}: {
  actionHref?: string;
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
        {actionLabel && actionHref ? (
          <a className="workspace-home-text-link" href={actionHref} title={actionLabel}>
            {actionLabel}
          </a>
        ) : actionLabel ? (
          <button className="workspace-home-text-link" disabled title={`${actionLabel} is unavailable`} type="button">
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
    return <PanelMessage>No agenda items available.</PanelMessage>;
  }

  return (
    <div className="workspace-home-agenda-list">
      {items.map((item) => {
        const content = (
          <>
            <time>{item.time}</time>
            <span aria-hidden="true" />
            <div className="min-w-0">
              <strong>{item.title}</strong>
              <p>
                {item.detail}
                {item.meta ? <em>{item.meta}</em> : null}
              </p>
            </div>
          </>
        );

        return item.href ? (
          <a className="workspace-home-agenda-row" href={item.href} key={item.id} title={item.title}>
            {content}
          </a>
        ) : (
          <div className="workspace-home-agenda-row" key={item.id}>
            {content}
          </div>
        );
      })}
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
        {getSupplementalMessage(status, "No recent document activity available.")}
      </PanelMessage>
    );
  }

  return (
    <div className="workspace-home-activity-feed">
      {items.map((item) => (
        <a className="workspace-home-activity-item" href={item.href} key={item.id} title={item.title}>
          <span className="workspace-home-avatar">{getInitials(item.actorName ?? item.title)}</span>
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

function PanelButton({ children, onClick }: { children: ReactNode; onClick: () => void }) {
  return (
    <button className="workspace-home-panel-link" onClick={onClick} type="button">
      {children}
      <span aria-hidden="true">{"->"}</span>
    </button>
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

function HomeAgendaDrawer({
  agenda,
  fallbackItems,
  locale,
  onClose,
  status,
}: {
  agenda?: WorkspaceAgendaResponse;
  fallbackItems: HomeAgendaRow[];
  locale: DisplayLocale;
  onClose: () => void;
  status: HomeDataStatus;
}) {
  const [activeTab, setActiveTab] = useState<AgendaTab>("today");
  const todayItems = agenda?.today.map(toAgendaDisplayItem) ?? fallbackItems.map(toAgendaDisplayItemFromRow);
  const upcomingItems = agenda?.upcoming.map(toAgendaDisplayItem) ?? [];
  const visibleItems = activeTab === "today" ? todayItems : [...todayItems, ...upcomingItems].slice(0, 8);
  const text = getAgendaDrawerText(locale);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onClose]);

  return (
    <div className="workspace-home-agenda-overlay" onMouseDown={onClose}>
      <aside
        aria-label={text.title}
        aria-modal="true"
        className="workspace-home-agenda-drawer"
        onMouseDown={(event) => event.stopPropagation()}
        role="dialog"
      >
        <header className="workspace-home-agenda-drawer-header">
          <span className="workspace-home-agenda-drawer-icon">
            <CalendarDays className="h-4 w-4" />
          </span>
          <div className="min-w-0 flex-1">
            <h2>{text.title}</h2>
            <p>{formatAgendaDate(agenda?.date, locale)}</p>
          </div>
          <span className="workspace-home-agenda-status">
            <span aria-hidden="true" />
            {getCalendarStatusLabel(agenda?.calendarStatus, status, locale)}
          </span>
          <button className="workspace-home-agenda-close" onClick={onClose} type="button" aria-label={text.close}>
            <X className="h-4 w-4" />
          </button>
        </header>

        <div className="workspace-home-agenda-tabs" role="tablist" aria-label={text.tabsLabel}>
          <button
            aria-selected={activeTab === "today"}
            className={activeTab === "today" ? "is-active" : ""}
            onClick={() => setActiveTab("today")}
            role="tab"
            type="button"
          >
            {text.today}
          </button>
          <button
            aria-selected={activeTab === "week"}
            className={activeTab === "week" ? "is-active" : ""}
            onClick={() => setActiveTab("week")}
            role="tab"
            type="button"
          >
            {text.week}
          </button>
        </div>

        <div className="workspace-home-agenda-drawer-body">
          {visibleItems.length > 0 ? (
            <div className="workspace-home-agenda-timeline">
              {visibleItems.map((item) => (
                <AgendaDrawerItem item={item} key={item.id} locale={locale} />
              ))}
            </div>
          ) : (
            <PanelMessage>{getSupplementalMessage(status, text.empty)}</PanelMessage>
          )}

          {activeTab === "today" && upcomingItems.length > 0 ? (
            <section className="workspace-home-agenda-later">
              <h3>{text.later}</h3>
              <div>
                {upcomingItems.slice(0, 3).map((item) => (
                  <AgendaLaterItem item={item} key={item.id} locale={locale} />
                ))}
              </div>
            </section>
          ) : null}
        </div>

        <footer className="workspace-home-agenda-footer">
          <button disabled title={text.addDisabled} type="button">
            <Plus className="h-4 w-4" />
            {text.add}
          </button>
          <button disabled title={text.calendarDisabled} type="button">
            {text.viewCalendar}
            <ExternalLink className="h-4 w-4" />
          </button>
        </footer>
      </aside>
    </div>
  );
}

function AgendaDrawerItem({ item, locale }: { item: AgendaDisplayItem; locale: DisplayLocale }) {
  const content = (
    <>
      <div className="workspace-home-agenda-time">
        <time>{item.startTime}</time>
        <span className={item.kind === "task" ? "is-task" : item.kind === "break" ? "is-break" : ""} aria-hidden="true" />
      </div>
      <div className="workspace-home-agenda-event">
        <div className="min-w-0">
          <strong>{item.title}</strong>
          <p>{formatAgendaItemMeta(item, locale)}</p>
        </div>
        {item.category ? <mark>{item.category}</mark> : null}
        <span className="workspace-home-agenda-calendar-state">
          <CalendarCheck className="h-4 w-4" />
          {getAgendaItemStatusLabel(item, locale)}
        </span>
        <button disabled aria-label="More agenda actions" type="button">
          <MoreHorizontal className="h-4 w-4" />
        </button>
      </div>
    </>
  );

  return item.href ? (
    <a className="workspace-home-agenda-timeline-row" href={item.href} title={item.title}>
      {content}
    </a>
  ) : (
    <div className="workspace-home-agenda-timeline-row">{content}</div>
  );
}

function AgendaLaterItem({ item, locale }: { item: AgendaDisplayItem; locale: DisplayLocale }) {
  const content = (
    <>
      <span>
        <FileText className="h-4 w-4" />
      </span>
      <strong>{item.title}</strong>
      <time>{formatAgendaItemDate(item.date, item.startTime, locale)}</time>
      {item.category ? <mark>{item.category}</mark> : null}
    </>
  );

  return item.href ? (
    <a className="workspace-home-agenda-later-row" href={item.href} title={item.title}>
      {content}
    </a>
  ) : (
    <div className="workspace-home-agenda-later-row">{content}</div>
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
    agenda: "idle",
    contributors: "idle",
    data: {},
    updates: "idle",
  });

  useEffect(() => {
    if (bootstrapStatus !== "ready" || !bootstrap) {
      setState({
        activity: bootstrapStatus === "loading" ? "loading" : "idle",
        agenda: bootstrapStatus === "loading" ? "loading" : "idle",
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
      agenda: "loading",
      contributors: "loading",
      data: {},
      updates: "loading",
    });

    void Promise.allSettled([
      shouldLoadActivity
        ? getDocumentActivity(bootstrap.activeDocumentId, controller.signal)
        : Promise.resolve<DocumentActivityResponse>({ items: [] }),
      getWorkspaceAgenda(bootstrap.workspace.id, getLocalIsoDate(), controller.signal),
      getWorkspaceNotifications(bootstrap.workspace.id, controller.signal),
      getWorkspaceMembers(bootstrap.workspace.id, controller.signal),
    ]).then(([activityResult, agendaResult, updatesResult, contributorsResult]) => {
      if (controller.signal.aborted) {
        return;
      }

      setState({
        activity: getRequestStatus(activityResult),
        agenda: getRequestStatus(agendaResult),
        contributors: getRequestStatus(contributorsResult),
        data: {
          activityItems: activityResult.status === "fulfilled" ? activityResult.value.items : [],
          agenda: agendaResult.status === "fulfilled" ? agendaResult.value : undefined,
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
    return locale === "zh-CN"
      ? "\u8fd9\u662f\u4eca\u5929\u5de5\u4f5c\u533a\u7684\u6700\u65b0\u60c5\u51b5\u3002"
      : "Here's what's happening across your workspace today.";
  }

  if (status === "loading") {
    return locale === "zh-CN" ? "\u6b63\u5728\u52a0\u8f7d\u5de5\u4f5c\u533a\u4fe1\u53f7\u3002" : "Loading workspace signals.";
  }

  if (status === "forbidden") {
    return locale === "zh-CN"
      ? "\u767b\u5f55\u540e\u52a0\u8f7d\u5b9e\u65f6\u5de5\u4f5c\u533a\u6570\u636e\u3002"
      : "Sign in to load live workspace data.";
  }

  return locale === "zh-CN"
    ? "\u5de5\u4f5c\u533a API \u4e0d\u53ef\u7528\uff1b\u4e3b\u9875\u663e\u793a\u53ef\u7528\u7684\u672c\u5730\u72b6\u6001\u3002"
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

function toAgendaDisplayItem(item: WorkspaceAgendaItemDto): AgendaDisplayItem {
  return {
    calendarStatus: item.calendarStatus,
    category: item.category,
    connectedToCalendar: item.connectedToCalendar,
    date: item.date,
    detail: item.detail,
    durationMinutes: item.durationMinutes,
    endTime: item.endTime,
    href: createAgendaItemHref(item),
    id: item.id,
    kind: item.kind,
    startTime: item.startTime,
    title: item.title,
  };
}

function toAgendaDisplayItemFromRow(item: HomeAgendaRow): AgendaDisplayItem {
  return {
    calendarStatus: item.calendarStatus,
    category: item.meta,
    connectedToCalendar: item.connectedToCalendar,
    date: item.date,
    detail: item.detail,
    durationMinutes: item.durationMinutes,
    endTime: item.endTime,
    href: item.href,
    id: item.id,
    kind: item.kind,
    startTime: item.startTime ?? item.time,
    title: item.title,
  };
}

function createAgendaItemHref(item: WorkspaceAgendaItemDto) {
  if (item.resourceType === "document" && item.resourceId && isUuid(item.resourceId)) {
    return createEditorHash(item.resourceId);
  }

  if (item.actionUrl) {
    return normalizeInternalActionHash(item.actionUrl);
  }

  return undefined;
}

function getAgendaDrawerText(locale: DisplayLocale) {
  if (locale === "zh-CN") {
    return {
      add: "\u6dfb\u52a0\u4e8b\u9879",
      addDisabled: "\u5f53\u524d\u65e5\u7a0b\u6765\u81ea\u5de5\u4f5c\u533a\u8bfb\u6a21\u578b\uff1b\u5199\u5165\u4e8b\u9879\u9700\u8981\u771f\u5b9e\u65e5\u5386\u96c6\u6210\u3002",
      calendarDisabled: "\u5f53\u524d\u6ca1\u6709\u5916\u90e8\u65e5\u5386\u94fe\u63a5\u3002",
      close: "\u5173\u95ed\u4eca\u65e5\u65e5\u7a0b",
      empty: "\u6682\u65e0\u65e5\u7a0b\u4e8b\u9879\u3002",
      later: "\u7a0d\u540e\u5b89\u6392",
      tabsLabel: "\u65e5\u7a0b\u8303\u56f4",
      title: "\u4eca\u65e5\u65e5\u7a0b",
      today: "\u4eca\u5929",
      viewCalendar: "\u5728\u65e5\u5386\u4e2d\u67e5\u770b",
      week: "\u672c\u5468",
    };
  }

  return {
    add: "Add item",
    addDisabled: "This agenda is a workspace read model; writing items requires calendar integration.",
    calendarDisabled: "No external calendar link is configured.",
    close: "Close today agenda",
    empty: "No agenda items yet.",
    later: "Later",
    tabsLabel: "Agenda range",
    title: "Today agenda",
    today: "Today",
    viewCalendar: "View in calendar",
    week: "This week",
  };
}

function getCalendarStatusLabel(calendarStatus: string | undefined, status: HomeDataStatus, locale: DisplayLocale) {
  if (status === "loading") {
    return locale === "zh-CN" ? "\u52a0\u8f7d\u4e2d" : "Loading";
  }

  if (status === "error" || status === "forbidden") {
    return locale === "zh-CN" ? "\u4e0d\u53ef\u7528" : "Unavailable";
  }

  if (calendarStatus === "connected") {
    return locale === "zh-CN" ? "\u5df2\u8fde\u63a5\u5230\u65e5\u5386" : "Connected to calendar";
  }

  return locale === "zh-CN" ? "\u5de5\u4f5c\u533a\u65e5\u7a0b" : "Workspace agenda";
}

function getAgendaItemStatusLabel(item: AgendaDisplayItem, locale: DisplayLocale) {
  if (item.connectedToCalendar || item.calendarStatus === "connected") {
    return locale === "zh-CN" ? "\u5df2\u8fde\u63a5\u5230\u65e5\u5386" : "Connected";
  }

  return locale === "zh-CN" ? "\u5de5\u4f5c\u533a\u65e5\u7a0b" : "Workspace";
}

function formatAgendaDate(value: string | undefined, locale: DisplayLocale) {
  return new Intl.DateTimeFormat(locale === "zh-CN" ? "zh-CN" : "en-US", {
    month: "long",
    day: "numeric",
    weekday: "long",
  }).format(parseDateOnly(value) ?? new Date());
}

function formatAgendaItemDate(value: string | undefined, startTime: string, locale: DisplayLocale) {
  const date = parseDateOnly(value);
  if (!date) {
    return startTime;
  }

  const formattedDate = new Intl.DateTimeFormat(locale === "zh-CN" ? "zh-CN" : "en-US", {
    month: "short",
    day: "numeric",
  }).format(date);

  return `${formattedDate} ${startTime}`;
}

function formatAgendaItemMeta(item: AgendaDisplayItem, locale: DisplayLocale) {
  if (!item.endTime) {
    return item.kind === "task"
      ? locale === "zh-CN" ? `\u622a\u6b62 ${item.startTime}` : `Due ${item.startTime}`
      : item.detail;
  }

  const duration = item.durationMinutes && item.durationMinutes > 0 ? item.durationMinutes : null;
  const durationLabel = duration ? ` (${duration} ${locale === "zh-CN" ? "\u5206\u949f" : "min"})` : "";
  return `${item.startTime} - ${item.endTime}${durationLabel}`;
}

function parseDateOnly(value: string | undefined) {
  if (!value) {
    return null;
  }

  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (!match) {
    return null;
  }

  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
}

function getLocalIsoDate() {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
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
