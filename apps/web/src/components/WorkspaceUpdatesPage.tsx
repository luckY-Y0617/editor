import {
  AtSign,
  Bell,
  ChevronDown,
  ChevronRight,
  Eye,
  FileText,
  MessageSquare,
  ShieldCheck,
  SlidersHorizontal,
  UsersRound,
  Wrench,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { AtlasIcon } from "./AtlasIcon";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl, getConfiguredWorkspaceId } from "../lib/apiClient";
import {
  getWorkspaceNotificationPreferences,
  getWorkspaceNotifications,
  markAllWorkspaceNotificationsRead,
  markWorkspaceNotificationRead,
  type PermissionNotificationDto,
  type PermissionNotificationPreferenceDto,
} from "../lib/appApi";
import {
  filterWorkspaceNotifications,
  getNotificationKind,
  getNotificationPreferenceStatusLabel,
  getNotificationStatusLabel,
  toNotificationPreferenceResourceRows,
  toWorkspaceNotification,
  type NotificationApiStatus,
  type WorkspaceUpdatesTab,
} from "../lib/workspaceUpdatesModel";
import { getWorkspaceUpdatesTabDisplayLabel, t, useDisplayLanguage } from "../lib/i18n";
import {
  mutedCollections,
  notificationGroups,
  notificationPreferences,
  watchedDocuments,
  type NotificationKind,
  type NotificationPreference,
  type WorkspaceNotification,
} from "../data/workspaceUpdatesData";
import compassMarkUrl from "../assets/svg/decorative/compass-mark-small.svg";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

const updatesPatternStyle = {
  "--updates-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--updates-route-pattern": `url(${routePatternUrl})`,
  "--updates-topographic-pattern": `url(${topographicPatternUrl})`,
  "--workspace-home-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--workspace-home-route-pattern": `url(${routePatternUrl})`,
  "--workspace-home-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

const updatesTabs: WorkspaceUpdatesTab[] = ["all", "unread", "comments", "mentions", "access", "documents", "general"];
const updatesApiWorkspaceId = getConfiguredNotificationWorkspaceId();

export function WorkspaceUpdatesPage() {
  const { locale } = useDisplayLanguage();
  const [activeTab, setActiveTab] = useState<WorkspaceUpdatesTab>("all");
  const { markAllRead, markRead, notifications, status, unreadCount } = usePermissionNotifications(updatesApiWorkspaceId);
  const preferences = useNotificationPreferences(updatesApiWorkspaceId);
  const filteredNotifications = useMemo(
    () => filterWorkspaceNotifications(notifications, activeTab),
    [activeTab, notifications],
  );
  const displayGroups = useMemo(() => {
    if (status === "unconfigured") {
      return notificationGroups;
    }

    if (status !== "ready") {
      return [];
    }

    if (filteredNotifications.length === 0) {
      return [];
    }

    return [
      {
        id: "permission-notifications",
        label: t(locale, "updates.latest"),
        notifications: filteredNotifications.map(toWorkspaceNotification),
      },
    ];
  }, [filteredNotifications, locale, status]);
  const totalCount = status === "ready"
    ? notifications.length
    : status === "unconfigured"
      ? notificationGroups.reduce((sum, group) => sum + group.notifications.length, 0)
      : 0;
  const displayUnreadCount = status === "ready"
    ? unreadCount
    : status === "unconfigured"
      ? notificationGroups.reduce(
        (sum, group) => sum + group.notifications.filter((notification) => notification.unread).length,
        0,
      )
      : 0;

  return (
    <main className="updates-page-shell flex h-screen flex-col overflow-hidden" style={updatesPatternStyle}>
      <WorkspaceHomeTopBar activeItem="updates" />
      <div className="updates-page-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar activeItem="updates" showCollections={false} />
        <section className="updates-page-feed editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="updates-page-feed-inner">
            <header className="updates-page-heading">
              <h1>{t(locale, "updates.title")}</h1>
              <p>{t(locale, "updates.heading")}</p>
              <p className="share-permissions-inline-status">{getNotificationStatusLabel(status)}</p>
            </header>

            <nav className="updates-page-tabs" aria-label="Notification filters">
              {updatesTabs.map((tab) => (
                <button
                  className={activeTab === tab ? "is-active" : ""}
                  key={tab}
                  onClick={() => setActiveTab(tab)}
                  type="button"
                >
                  {getWorkspaceUpdatesTabDisplayLabel(locale, tab)}
                </button>
              ))}
            </nav>

            <div className="updates-page-toolbar">
              <span>{status === "ready" ? t(locale, "updates.shown", { count: filteredNotifications.length }) : getNotificationStatusLabel(status)}</span>
              <button disabled title="Notifications are fixed to newest first." type="button">
                {t(locale, "updates.newestFirst")}
                <ChevronDown className="h-4 w-4" />
              </button>
              <button
                disabled={status !== "ready" || unreadCount === 0}
                onClick={markAllRead}
                title={status === "ready" ? t(locale, "updates.markAllRead") : getNotificationStatusLabel(status)}
                type="button"
              >
                {t(locale, "updates.markAllRead")}
              </button>
              <button aria-label="Filter notifications" disabled title="Advanced filters are deferred" type="button">
                <SlidersHorizontal className="h-4 w-4" />
              </button>
            </div>

            <div className="updates-page-groups">
              {status !== "ready" && status !== "unconfigured" ? (
                <section className="updates-page-group">
                  <h2>{t(locale, "updates.latest")}</h2>
                  <div className="updates-page-list">
                    <div className="share-permissions-empty-state">{getUpdatesUnavailableMessage(status)}</div>
                  </div>
                </section>
              ) : null}
              {status === "ready" && notifications.length === 0 ? (
                <section className="updates-page-group">
                  <h2>{t(locale, "updates.latest")}</h2>
                  <div className="updates-page-list">
                    <div className="share-permissions-empty-state">{t(locale, "updates.noNotifications")}</div>
                  </div>
                </section>
              ) : null}
              {status === "ready" && notifications.length > 0 && filteredNotifications.length === 0 ? (
                <section className="updates-page-group">
                  <h2>{getWorkspaceUpdatesTabDisplayLabel(locale, activeTab)}</h2>
                  <div className="updates-page-list">
                    <div className="share-permissions-empty-state">{t(locale, "updates.noNotificationsForFilter")}</div>
                  </div>
                </section>
              ) : null}
              {displayGroups.map((group) => (
                <section className="updates-page-group" key={group.id}>
                  <h2>{group.label}</h2>
                  <div className="updates-page-list">
                    {group.notifications.map((notification) => (
                      <NotificationRow
                        key={notification.id}
                        notification={notification}
                        onMarkRead={status === "ready" ? markRead : undefined}
                      />
                    ))}
                  </div>
                </section>
              ))}
            </div>
          </div>
        </section>
        <UpdatesSummaryPanel
          preferences={preferences}
          locale={locale}
          status={status}
          totalCount={totalCount}
          unreadCount={displayUnreadCount}
        />
      </div>
    </main>
  );
}

function NotificationRow({
  notification,
  onMarkRead,
}: {
  notification: WorkspaceNotification;
  onMarkRead?: (notificationId: string) => void;
}) {
  const Icon = getNotificationIcon(notification.kind);
  const actionHref = notification.actionHref ?? getNotificationHref(notification.kind);

  return (
    <article className="updates-page-row">
      <span className={["updates-page-unread", notification.unread ? "is-unread" : ""].join(" ")} />
      <span className="updates-page-row-icon">
        <Icon className="h-4 w-4" />
      </span>
      {notification.actor ? (
        <span className={["updates-page-avatar", `is-${notification.actor.avatarTone}`].join(" ")}>
          {notification.actor.initials}
        </span>
      ) : (
        <span className="updates-page-avatar is-system">
          <Wrench className="h-4 w-4" />
        </span>
      )}
      <div className="min-w-0">
        <p>
          {notification.actor ? <strong>{notification.actor.name}</strong> : null}
          {notification.messagePrefix ? <span> {notification.messagePrefix} </span> : null}
          <a
            href={actionHref}
            onClick={() => {
              if (notification.unread) {
                onMarkRead?.(notification.id);
              }
            }}
          >
            {notification.subject}
          </a>
          {notification.messageSuffix ? <span> {notification.messageSuffix}</span> : null}
          {notification.versionBadge ? <em>{notification.versionBadge}</em> : null}
        </p>
        {notification.detail ? <small>{notification.detail}</small> : null}
      </div>
      <span className="updates-page-time">{notification.time}</span>
      {notification.actionLabel ? (
        <a
          className="updates-page-action-link"
          href={actionHref}
          onClick={() => {
            if (notification.unread) {
              onMarkRead?.(notification.id);
            }
          }}
          title={`${notification.actionLabel} notification`}
        >
          {notification.actionLabel}
        </a>
      ) : (
        <span />
      )}
      <ChevronRight className="h-4 w-4 text-[var(--ns-slate-700)]" />
    </article>
  );
}

function UpdatesSummaryPanel({
  locale,
  preferences,
  status,
  totalCount,
  unreadCount,
}: {
  locale: ReturnType<typeof useDisplayLanguage>["locale"];
  preferences: ReturnType<typeof useNotificationPreferences>;
  status: NotificationApiStatus;
  totalCount: number;
  unreadCount: number;
}) {
  return (
    <aside className="updates-page-summary editor-scrollbar h-full w-[390px] shrink-0 overflow-y-auto border-l border-[var(--ns-border)]">
      <div className="updates-page-summary-inner">
        <SummaryCard title={t(locale, "updates.summary")}>
          <div className="updates-page-summary-count">
            <strong>{unreadCount}</strong>
            <span>unread</span>
            <p>{totalCount} total notifications</p>
            <p>{getNotificationStatusLabel(status)}</p>
          </div>
          <AtlasIcon className="updates-page-summary-compass" src={compassMarkUrl} />
        </SummaryCard>

        <SummaryCard title="Category Preferences">
          <div className="updates-page-preferences">
            {notificationPreferences.map((preference) => (
              <PreferenceRow key={preference.id} preference={preference} />
            ))}
          </div>
          <p className="updates-page-summary-note">Category toggles are deferred; resource watch and mute state is live-backed below.</p>
        </SummaryCard>

        <SummaryCard title="Email Digest">
          <div className="updates-page-digest">
            <span>Delivery frequency</span>
            <button title="Email digest settings are deferred" type="button">
              Daily
              <ChevronDown className="h-4 w-4" />
            </button>
            <p>Delivered at 08:00 AM local time</p>
          </div>
        </SummaryCard>

        <SummaryCard action="View all" actionHref="#search" title="Watched Documents">
          <PreferenceResourceList
            fallbackRows={watchedDocuments.map((document) => ({
              href: "#editor",
              id: document.id,
              label: document.title,
              resourceType: "document",
              state: "watched" as const,
              updatedAt: "",
            }))}
            preferences={preferences}
            state="watched"
          />
        </SummaryCard>

        <SummaryCard action="View all" actionHref="#libraries" title="Muted Folders">
          <PreferenceResourceList
            fallbackRows={mutedCollections.map((collection) => ({
              href: "#libraries",
              id: collection.id,
              label: collection.title,
              resourceType: "collection",
              state: "muted" as const,
              updatedAt: "",
            }))}
            preferences={preferences}
            state="muted"
          />
        </SummaryCard>
      </div>
    </aside>
  );
}

function SummaryCard({
  action,
  actionHref,
  children,
  title,
}: {
  action?: string;
  actionHref?: string;
  children: ReactNode;
  title: string;
}) {
  return (
    <section className="updates-page-summary-card">
      <header>
        <h2>{title}</h2>
        {action && actionHref ? (
          <a className="updates-page-text-link" href={actionHref} title={action}>
            {action}
          </a>
        ) : action ? (
          <button className="updates-page-text-link" disabled title={`${action} is unavailable`} type="button">
            {action}
          </button>
        ) : null}
      </header>
      {children}
    </section>
  );
}

function PreferenceRow({ preference }: { preference: NotificationPreference }) {
  const Icon = getPreferenceIcon(preference.id);

  return (
    <div className="updates-page-preference-row">
      <Icon className="h-4 w-4" />
      <span className="min-w-0 flex-1 truncate">{preference.label}</span>
      <button
        aria-pressed={preference.enabled}
        className={preference.enabled ? "is-on" : ""}
        disabled
        title="Category notification preference API is not available"
        type="button"
      >
        <span />
      </button>
    </div>
  );
}

function PreferenceResourceList({
  fallbackRows,
  preferences,
  state,
}: {
  fallbackRows: ReturnType<typeof toNotificationPreferenceResourceRows>;
  preferences: ReturnType<typeof useNotificationPreferences>;
  state: "muted" | "watched";
}) {
  const liveRows = useMemo(
    () => toNotificationPreferenceResourceRows(preferences.preferences).filter((row) => row.state === state),
    [preferences.preferences, state],
  );
  const rows = preferences.status === "unconfigured" ? fallbackRows : liveRows;

  if (preferences.status !== "ready" && preferences.status !== "unconfigured") {
    return <p className="updates-page-summary-note">{getNotificationPreferenceStatusLabel(preferences.status)}</p>;
  }

  if (rows.length === 0) {
    return <p className="updates-page-summary-note">No {state} resources</p>;
  }

  return (
    <div className="updates-page-compact-list">
      {rows.slice(0, 4).map((row) => (
        <a href={row.href} key={row.id} title={row.updatedAt ? `Updated ${row.updatedAt}` : undefined}>
          {row.resourceType === "collection" ? <Eye className="h-4 w-4" /> : <FileText className="h-4 w-4" />}
          <span className="min-w-0 flex-1 truncate">{row.label}</span>
        </a>
      ))}
    </div>
  );
}

function getNotificationIcon(kind: NotificationKind) {
  switch (kind) {
    case "mention":
      return AtSign;
    case "comment":
      return MessageSquare;
    case "permission":
      return UsersRound;
    case "system":
      return ShieldCheck;
    case "version":
    case "document":
    default:
      return FileText;
  }
}

function getPreferenceIcon(id: NotificationPreference["id"]) {
  switch (id) {
    case "mentions":
      return Bell;
    case "comments":
      return MessageSquare;
    case "access-requests":
      return UsersRound;
    case "system-alerts":
      return ShieldCheck;
    case "document-changes":
    default:
      return FileText;
  }
}

function getNotificationHref(kind: NotificationKind) {
  if (kind === "permission") {
    return "#permissions";
  }

  if (kind === "version") {
    return "#versions";
  }

  return "#editor";
}

function usePermissionNotifications(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [notifications, setNotifications] = useState<PermissionNotificationDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [status, setStatus] = useState<NotificationApiStatus>(() =>
    apiBaseUrl ? "loading" : "unconfigured",
  );

  useEffect(() => {
    if (!apiBaseUrl) {
      setNotifications([]);
      setUnreadCount(0);
      setStatus("unconfigured");
      return;
    }

    setStatus("loading");
    const controller = new AbortController();
    void getWorkspaceNotifications(workspaceId, controller.signal)
      .then((body) => {
        setNotifications(body.notifications);
        setUnreadCount(body.unreadCount);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        if (error instanceof ApiClientError && (error.status === 401 || error.status === 403)) {
          setNotifications([]);
          setUnreadCount(0);
          setStatus("forbidden");
          return;
        }

        setNotifications([]);
        setUnreadCount(0);
        setStatus("error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, workspaceId]);

  const markRead = (notificationId: string) => {
    if (!apiBaseUrl) {
      return;
    }

    void markWorkspaceNotificationRead(notificationId)
      .then((updated) => {
        setNotifications((current) =>
          current.map((notification) => (notification.id === updated.id ? updated : notification)),
        );
        setUnreadCount((current) => Math.max(0, current - 1));
      })
      .catch(() => {
        // Read state is best-effort UI feedback; the backend remains authoritative.
      });
  };

  const markAllRead = () => {
    if (!apiBaseUrl || unreadCount === 0) {
      return;
    }

    void markAllWorkspaceNotificationsRead(workspaceId)
      .then(() => {
        const readAt = new Date().toISOString();
        setNotifications((current) => current.map((notification) => ({ ...notification, readAt })));
        setUnreadCount(0);
      })
      .catch(() => {
        // The next reload remains authoritative.
      });
  };

  return { markAllRead, markRead, notifications, status, unreadCount };
}

function useNotificationPreferences(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [preferences, setPreferences] = useState<PermissionNotificationPreferenceDto[]>([]);
  const [status, setStatus] = useState<NotificationApiStatus>(() =>
    apiBaseUrl && workspaceId ? "loading" : "unconfigured",
  );

  useEffect(() => {
    if (!apiBaseUrl || !workspaceId) {
      setPreferences([]);
      setStatus("unconfigured");
      return;
    }

    setStatus("loading");
    const controller = new AbortController();
    void getWorkspaceNotificationPreferences(workspaceId, controller.signal)
      .then((body) => {
        setPreferences(body.preferences);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        if (error instanceof ApiClientError && (error.status === 401 || error.status === 403)) {
          setPreferences([]);
          setStatus("forbidden");
          return;
        }

        setPreferences([]);
        setStatus("error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, workspaceId]);

  return { preferences, status };
}

function getConfiguredNotificationWorkspaceId() {
  return getConfiguredWorkspaceId();
}

function getUpdatesUnavailableMessage(status: NotificationApiStatus) {
  if (status === "loading") {
    return "Loading live notifications.";
  }

  if (status === "forbidden") {
    return "Sign in with notification access to load workspace updates.";
  }

  return "Live notifications could not be loaded. Demo notifications are hidden while the API is configured.";
}
