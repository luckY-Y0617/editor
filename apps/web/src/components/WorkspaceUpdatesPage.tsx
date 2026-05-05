import {
  Activity,
  AtSign,
  Bell,
  ChevronDown,
  ChevronRight,
  Eye,
  FileText,
  Home,
  Library,
  MessageSquare,
  Search,
  Settings,
  ShieldCheck,
  SlidersHorizontal,
  SwatchBook,
  UsersRound,
  Wrench,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { AtlasIcon } from "./AtlasIcon";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { createApiHeaders, getConfiguredApiBaseUrl, getConfiguredWorkspaceId } from "../lib/apiClient";
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
} as CSSProperties;

const updatesTabs = ["All", "Mentions", "Comments", "Documents", "Permissions", "System"];
const updatesApiWorkspaceId = getConfiguredNotificationWorkspaceId();

const updatesNavItems = [
  { label: "Home", href: "#home", icon: Home },
  { label: "Library", href: "#editor", icon: Library },
  { label: "Search", href: "#search", icon: Search },
  { label: "Templates", href: "#home", icon: SwatchBook, deferred: true },
  { label: "Activity", href: "#home", icon: Activity },
  { label: "Updates", href: "#updates", icon: Bell, active: true },
  { label: "Settings", href: "#home", icon: Settings, deferred: true },
];

type NotificationApiStatus = "unconfigured" | "loading" | "ready" | "forbidden" | "error";

type PermissionNotificationDto = {
  id: string;
  workspaceId: string;
  recipientUserId: string;
  actorUserId: string | null;
  type: string;
  resourceType: string | null;
  resourceId: string | null;
  accessRequestId: string | null;
  permissionGrantId: string | null;
  title: string;
  body: string | null;
  actionUrl: string | null;
  readAt: string | null;
  createdAt: string;
};

type PermissionNotificationsResponse = {
  notifications: PermissionNotificationDto[];
  unreadCount: number;
};

export function WorkspaceUpdatesPage() {
  const { markRead, notifications, status, unreadCount } = usePermissionNotifications(updatesApiWorkspaceId);
  const displayGroups = useMemo(() => {
    if (status !== "ready") {
      return notificationGroups;
    }

    if (notifications.length === 0) {
      return [];
    }

    return [
      {
        id: "permission-notifications",
        label: "Latest",
        notifications: notifications.map(toWorkspaceNotification),
      },
    ];
  }, [notifications, status]);
  const totalCount = status === "ready"
    ? notifications.length
    : notificationGroups.reduce((sum, group) => sum + group.notifications.length, 0);
  const displayUnreadCount = status === "ready"
    ? unreadCount
    : notificationGroups.reduce(
        (sum, group) => sum + group.notifications.filter((notification) => notification.unread).length,
        0,
      );

  return (
    <main className="updates-page-shell flex h-screen flex-col overflow-hidden" style={updatesPatternStyle}>
      <WorkspaceHomeTopBar activeItem="updates" />
      <div className="updates-page-body flex min-h-0 flex-1 overflow-hidden">
        <UpdatesSidebar />
        <section className="updates-page-feed editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="updates-page-feed-inner">
            <header className="updates-page-heading">
              <h1>Updates</h1>
              <p>Track comments, mentions, document changes, and workspace activity.</p>
            </header>

            <nav className="updates-page-tabs" aria-label="Notification filters">
              {updatesTabs.map((tab, index) => (
                <button className={index === 0 ? "is-active" : ""} key={tab} type="button">
                  {tab}
                </button>
              ))}
            </nav>

            <div className="updates-page-toolbar">
              <button title="Sort notifications" type="button">
                Newest first
                <ChevronDown className="h-4 w-4" />
              </button>
              <button aria-label="Filter notifications" title="Filter notifications" type="button">
                <SlidersHorizontal className="h-4 w-4" />
              </button>
            </div>

            <div className="updates-page-groups">
              {status === "ready" && notifications.length === 0 ? (
                <section className="updates-page-group">
                  <h2>Latest</h2>
                  <div className="updates-page-list">
                    <div className="share-permissions-empty-state">No notifications</div>
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
        <UpdatesSummaryPanel status={status} totalCount={totalCount} unreadCount={displayUnreadCount} />
      </div>
    </main>
  );
}

function UpdatesSidebar() {
  return (
    <aside className="updates-page-sidebar hidden h-full w-[320px] shrink-0 overflow-hidden border-r border-[var(--ns-border)] md:flex md:flex-col">
      <div className="updates-page-ruler" aria-hidden="true">
        <span>N 90</span>
        <span>N 60</span>
        <span>N 30</span>
        <span>0</span>
        <span>S 30</span>
        <span>S 60</span>
      </div>
      <div className="updates-page-sidebar-inner editor-scrollbar relative z-10 flex min-h-0 flex-1 flex-col overflow-y-auto px-6 py-7 pl-[78px]">
        <nav className="space-y-3" aria-label="Workspace navigation">
          {updatesNavItems.map((item) => {
            const Icon = item.icon;

            return (
              <a
                aria-current={item.active ? "page" : undefined}
                className={[
                  "updates-page-nav-item",
                  item.active ? "is-active" : "",
                  item.deferred ? "is-deferred" : "",
                ].join(" ")}
                href={item.href}
                key={item.label}
                title={item.deferred ? `${item.label} is planned for a later phase` : item.label}
              >
                <Icon className="h-4 w-4 shrink-0" />
                <span className="min-w-0 flex-1 truncate">{item.label}</span>
                {item.active ? <span className="updates-page-active-dot" /> : null}
              </a>
            );
          })}
        </nav>

        <div className="mt-auto pt-10 text-center text-[var(--ns-slate-500)]">
          <AtlasIcon className="mx-auto h-20 w-20 opacity-35" src={compassMarkUrl} />
          <div className="mt-4 text-xs">47&deg;36&apos;36&quot;N&nbsp;&nbsp;122&deg;19&apos;48&quot;W</div>
        </div>
      </div>
    </aside>
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
          <a href={notification.actionHref ?? getNotificationHref(notification.kind)}>{notification.subject}</a>
          {notification.messageSuffix ? <span> {notification.messageSuffix}</span> : null}
          {notification.versionBadge ? <em>{notification.versionBadge}</em> : null}
        </p>
        {notification.detail ? <small>{notification.detail}</small> : null}
      </div>
      <span className="updates-page-time">{notification.time}</span>
      {notification.actionLabel ? (
        <button
          onClick={() => {
            if (notification.unread) {
              onMarkRead?.(notification.id);
            }
            if (notification.actionHref) {
              window.location.href = notification.actionHref;
            }
          }}
          title={`${notification.actionLabel} notification`}
          type="button"
        >
          {notification.actionLabel}
        </button>
      ) : (
        <span />
      )}
      <ChevronRight className="h-4 w-4 text-[var(--ns-slate-700)]" />
    </article>
  );
}

function UpdatesSummaryPanel({
  status,
  totalCount,
  unreadCount,
}: {
  status: NotificationApiStatus;
  totalCount: number;
  unreadCount: number;
}) {
  return (
    <aside className="updates-page-summary editor-scrollbar h-full w-[390px] shrink-0 overflow-y-auto border-l border-[var(--ns-border)]">
      <div className="updates-page-summary-inner">
        <SummaryCard title="Summary">
          <div className="updates-page-summary-count">
            <strong>{unreadCount}</strong>
            <span>unread</span>
            <p>{totalCount} total notifications</p>
            <p>{notificationStatusLabel(status)}</p>
          </div>
          <AtlasIcon className="updates-page-summary-compass" src={compassMarkUrl} />
        </SummaryCard>

        <SummaryCard title="Notification Preferences">
          <div className="updates-page-preferences">
            {notificationPreferences.map((preference) => (
              <PreferenceRow key={preference.id} preference={preference} />
            ))}
          </div>
          <button className="updates-page-text-link" title="Notification preference persistence is deferred" type="button">
            Manage all preferences
          </button>
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

        <SummaryCard action="View all" title="Watched Documents">
          <div className="updates-page-compact-list">
            {watchedDocuments.map((document) => (
              <a href="#editor" key={document.id}>
                <FileText className="h-4 w-4" />
                <span className="min-w-0 flex-1 truncate">{document.title}</span>
              </a>
            ))}
          </div>
        </SummaryCard>

        <SummaryCard action="View all" title="Muted Collections">
          <div className="updates-page-compact-list">
            {mutedCollections.map((collection) => (
              <button title={`${collection.title} mute settings are deferred`} type="button" key={collection.id}>
                <Eye className="h-4 w-4" />
                <span className="min-w-0 flex-1 truncate">{collection.title}</span>
              </button>
            ))}
          </div>
        </SummaryCard>
      </div>
    </aside>
  );
}

function SummaryCard({
  action,
  children,
  title,
}: {
  action?: string;
  children: ReactNode;
  title: string;
}) {
  return (
    <section className="updates-page-summary-card">
      <header>
        <h2>{title}</h2>
        {action ? (
          <button className="updates-page-text-link" title={action} type="button">
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
        title="Notification preference persistence is deferred"
        type="button"
      >
        <span />
      </button>
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

    const controller = new AbortController();
    const params = new URLSearchParams();
    if (workspaceId) {
      params.set("workspaceId", workspaceId);
    }

    setStatus("loading");
    void fetch(`${apiBaseUrl}/notifications${params.size ? `?${params.toString()}` : ""}`, {
      headers: createNotificationHeaders(),
      signal: controller.signal,
    })
      .then(async (response) => {
        if (response.status === 401 || response.status === 403) {
          setNotifications([]);
          setUnreadCount(0);
          setStatus("forbidden");
          return;
        }

        if (!response.ok) {
          throw new Error(`Notifications API returned ${response.status}`);
        }

        const body = (await response.json()) as PermissionNotificationsResponse;
        setNotifications(body.notifications);
        setUnreadCount(body.unreadCount);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
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

    void fetch(`${apiBaseUrl}/notifications/${notificationId}/read`, {
      body: JSON.stringify({ read: true }),
      headers: createNotificationHeaders("application/json"),
      method: "PATCH",
    })
      .then(async (response) => {
        if (!response.ok) {
          return;
        }

        const updated = (await response.json()) as PermissionNotificationDto;
        setNotifications((current) =>
          current.map((notification) => (notification.id === updated.id ? updated : notification)),
        );
        setUnreadCount((current) => Math.max(0, current - 1));
      })
      .catch(() => {
        // Read state is best-effort UI feedback; the backend remains authoritative.
      });
  };

  return { markRead, notifications, status, unreadCount };
}

function toWorkspaceNotification(notification: PermissionNotificationDto): WorkspaceNotification {
  return {
    id: notification.id,
    kind: getNotificationKind(notification.type),
    unread: notification.readAt === null,
    subject: notification.title,
    detail: notification.body ?? undefined,
    time: formatRelativeNotificationTime(notification.createdAt),
    actionLabel: notification.type === "access_request.created" ? "Review" : "Open",
    actionHref: normalizeNotificationActionUrl(notification.actionUrl),
  };
}

function getNotificationKind(type: string): NotificationKind {
  if (type.startsWith("access_request.") || type.startsWith("permission.") || type.startsWith("group.")) {
    return "permission";
  }

  return "system";
}

function normalizeNotificationActionUrl(actionUrl: string | null) {
  if (!actionUrl) {
    return undefined;
  }

  if (actionUrl.startsWith("#") || actionUrl.startsWith("/")) {
    return actionUrl;
  }

  return undefined;
}

function getConfiguredNotificationWorkspaceId() {
  return getConfiguredWorkspaceId();
}

function createNotificationHeaders(contentType?: string) {
  return createApiHeaders(contentType);
}

function notificationStatusLabel(status: NotificationApiStatus) {
  if (status === "ready") {
    return "Live permission notifications";
  }

  if (status === "loading") {
    return "Loading notifications";
  }

  if (status === "forbidden") {
    return "Notification access unavailable";
  }

  if (status === "error") {
    return "Notification API unavailable";
  }

  return "Mock activity shown until API is configured";
}

function formatRelativeNotificationTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
