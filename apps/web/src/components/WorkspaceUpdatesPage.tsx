import {
  AlertCircle,
  ArrowUpDown,
  Check,
  Clock3,
  ChevronRight,
  Eye,
  FileText,
  Link2,
  Loader2,
  RotateCcw,
  ShieldCheck,
  Trash2,
  UserCheck,
  UsersRound,
  Wrench,
  X,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl, getConfiguredWorkspaceId } from "../lib/apiClient";
import {
  getAccessSharingSummary,
  getAccessRequests,
  getResourceEmailInvites,
  getShareLink,
  getWorkspaceNotificationPreferences,
  getWorkspaceNotifications,
  markAllWorkspaceNotificationsRead,
  markWorkspaceNotificationRead,
  retryEmailInvite,
  reviewAccessRequest,
  revokeEmailInvite,
  revokeShareLink,
  type AccessRequestDto,
  type AccessSharingSummaryResponse,
  type EmailInviteDto,
  type PermissionNotificationDto,
  type PermissionNotificationPreferenceDto,
  type ShareLinkDto,
} from "../lib/appApi";
import {
  createAccessSharingSummaryFromNotifications,
  filterWorkspaceNotifications,
  getWorkspaceUpdatesTabFromHash,
  toNotificationPreferenceResourceRows,
  toWorkspaceNotification,
  type NotificationApiStatus,
  type WorkspaceUpdatesTab,
} from "../lib/workspaceUpdatesModel";
import { getWorkspaceUpdatesTabDisplayLabel, t, useDisplayLanguage } from "../lib/i18n";
import {
  notificationGroups,
  watchedDocuments,
  type NotificationKind,
  type NotificationGroup,
  type WorkspaceNotification,
} from "../data/workspaceUpdatesData";
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

const updatesTabs: WorkspaceUpdatesTab[] = ["all", "access", "grants", "links", "invites", "failed"];
const updatesApiWorkspaceId = getConfiguredNotificationWorkspaceId();

export function WorkspaceUpdatesPage() {
  const { locale } = useDisplayLanguage();
  const [activeTab, setActiveTab] = useState<WorkspaceUpdatesTab>(() => getWorkspaceUpdatesTabFromHash(window.location.hash));
  const [selectedNotificationId, setSelectedNotificationId] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<"desc" | "asc">("desc");
  const { markAllRead, markRead, notifications, refresh, status, summary, unreadCount } = usePermissionNotifications(updatesApiWorkspaceId);
  const preferences = useNotificationPreferences(updatesApiWorkspaceId);
  const filteredNotifications = useMemo(
    () => filterWorkspaceNotifications(notifications, activeTab),
    [activeTab, notifications],
  );
  const sortedFilteredNotifications = useMemo(
    () =>
      [...filteredNotifications].sort((left, right) => {
        const leftTime = new Date(left.createdAt).getTime();
        const rightTime = new Date(right.createdAt).getTime();
        return sortDirection === "desc" ? rightTime - leftTime : leftTime - rightTime;
      }),
    [filteredNotifications, sortDirection],
  );
  const displayGroups = useMemo(() => {
    if (status === "unconfigured") {
      return filterDemoNotificationGroups(notificationGroups, activeTab, sortDirection);
    }

    if (status !== "ready") {
      return [];
    }

    if (sortedFilteredNotifications.length === 0) {
      return [];
    }

    return [
      {
        id: "permission-notifications",
        label: t(locale, "updates.latest"),
        notifications: sortedFilteredNotifications.map((notification) => toWorkspaceNotification(notification, locale)),
      },
    ];
  }, [activeTab, locale, sortDirection, sortedFilteredNotifications, status]);
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
  const selectedRawNotification = status === "ready"
    ? selectedNotificationId
      ? notifications.find((notification) => notification.id === selectedNotificationId) ?? null
      : null
    : null;
  const selectedDemoNotification = status === "unconfigured"
    ? selectedNotificationId
      ? displayGroups
        .flatMap((group) => group.notifications)
        .find((notification) => notification.id === selectedNotificationId) ?? null
      : null
    : null;
  const selectedDisplayNotification = selectedRawNotification
    ? toWorkspaceNotification(selectedRawNotification, locale)
    : selectedDemoNotification;

  useEffect(() => {
    const syncActiveTabFromHash = () => setActiveTab(getWorkspaceUpdatesTabFromHash(window.location.hash));
    window.addEventListener("hashchange", syncActiveTabFromHash);
    return () => window.removeEventListener("hashchange", syncActiveTabFromHash);
  }, []);

  useEffect(() => {
    if (!selectedNotificationId) {
      return;
    }

    const visibleNotificationIds = status === "ready"
      ? sortedFilteredNotifications.map((notification) => notification.id)
      : status === "unconfigured"
        ? displayGroups.flatMap((group) => group.notifications.map((notification) => notification.id))
        : [];

    if (!visibleNotificationIds.includes(selectedNotificationId)) {
      setSelectedNotificationId(null);
    }
  }, [displayGroups, selectedNotificationId, sortedFilteredNotifications, status]);

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
              <p className="share-permissions-inline-status">{getLocalizedNotificationStatusLabel(status, locale)}</p>
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
              <span>{status === "ready" ? t(locale, "updates.shown", { count: filteredNotifications.length }) : getLocalizedNotificationStatusLabel(status, locale)}</span>
              <button
                aria-label={sortDirection === "desc"
                  ? localText(locale, "Newest first", "最新优先")
                  : localText(locale, "Oldest first", "最早优先")}
                aria-pressed={sortDirection === "asc"}
                className="updates-page-sort-button"
                onClick={() => setSortDirection((current) => (current === "desc" ? "asc" : "desc"))}
                title={sortDirection === "desc"
                  ? localText(locale, "Newest first", "最新优先")
                  : localText(locale, "Oldest first", "最早优先")}
                type="button"
              >
                <ArrowUpDown className="h-4 w-4" />
              </button>
              {status === "ready" && unreadCount > 0 ? (
                <button
                  onClick={markAllRead}
                  title={t(locale, "updates.markAllRead")}
                  type="button"
                >
                  {t(locale, "updates.markAllRead")}
                </button>
              ) : null}
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
                        locale={locale}
                        onMarkRead={status === "ready" ? markRead : undefined}
                        onSelect={() => setSelectedNotificationId(notification.id)}
                        selected={selectedNotificationId === notification.id}
                      />
                    ))}
                  </div>
                </section>
              ))}
            </div>
          </div>
        </section>
        {selectedDisplayNotification ? (
          <>
            <button
              aria-label={localText(locale, "Close detail", "关闭详情")}
              className="updates-page-detail-backdrop"
              onClick={() => setSelectedNotificationId(null)}
              type="button"
            />
            <AccessSharingDetailPanel
              displayNotification={selectedDisplayNotification}
              preferences={preferences}
              locale={locale}
              notification={selectedRawNotification}
              onClose={() => setSelectedNotificationId(null)}
              onRefresh={refresh}
              status={status}
              summary={summary}
              totalCount={totalCount}
              unreadCount={displayUnreadCount}
              workspaceId={updatesApiWorkspaceId}
            />
          </>
        ) : null}
      </div>
    </main>
  );
}

function NotificationRow({
  locale,
  notification,
  onMarkRead,
  onSelect,
  selected,
}: {
  locale: ReturnType<typeof useDisplayLanguage>["locale"];
  notification: WorkspaceNotification;
  onMarkRead?: (notificationId: string) => void;
  onSelect?: () => void;
  selected?: boolean;
}) {
  const Icon = getNotificationIcon(notification.kind);
  const selectRow = () => {
    if (notification.unread) {
      onMarkRead?.(notification.id);
    }

    onSelect?.();
  };

  return (
    <article
      className={["updates-page-row", selected ? "is-selected" : ""].join(" ")}
      onClick={selectRow}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          selectRow();
        }
      }}
      role="button"
      tabIndex={0}
    >
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
      <div className="updates-page-row-copy min-w-0">
        <p className="updates-page-row-action">
          {notification.actor ? <strong>{notification.actor.name}</strong> : null}
          {notification.messagePrefix ? <span> {notification.messagePrefix} </span> : null}
        </p>
        <p className="updates-page-row-title">
          <span className="updates-page-row-subject">{notification.subject}</span>
          {notification.messageSuffix ? <span> {notification.messageSuffix}</span> : null}
        </p>
        {notification.detail ? <small>{notification.detail}</small> : null}
      </div>
      <span className="updates-page-time">{notification.time}</span>
      <span className={["updates-page-kind-pill", `is-${notification.kind}`].join(" ")}>
        {notification.badgeLabel ?? getNotificationKindLabel(notification.kind, locale)}
      </span>
      <ChevronRight className="h-4 w-4 text-[var(--ns-slate-700)]" />
    </article>
  );
}

function AccessSharingDetailPanel({
  displayNotification,
  locale,
  notification,
  onClose,
  onRefresh,
  preferences,
  status,
  summary,
  totalCount,
  unreadCount,
  workspaceId,
}: {
  displayNotification: WorkspaceNotification | null;
  locale: ReturnType<typeof useDisplayLanguage>["locale"];
  notification: PermissionNotificationDto | null;
  onClose: () => void;
  onRefresh: () => Promise<void>;
  preferences: ReturnType<typeof useNotificationPreferences>;
  status: NotificationApiStatus;
  summary: AccessSharingSummaryResponse | null;
  totalCount: number;
  unreadCount: number;
  workspaceId: string | null;
}) {
  const summaryMetrics = summary ?? {
    accessRequestCount: 0,
    expiryCount: 0,
    failedInviteCount: 0,
    grantCount: 0,
    pendingReviewCount: 0,
    sharingCount: 0,
    totalCount,
    unreadCount,
  };
  const actionContext = useAccessSharingActionContext(notification, workspaceId, onRefresh, locale);
  const headerShareLinkStatus = getShareLinkStatus(actionContext.eventShareLink, notification);
  const isHeaderShareLink = Boolean(notification?.type.startsWith("share_link."));
  const headerShareLinkEvent = getShareLinkEvent(notification);
  const canRevokeHeaderShareLink = Boolean(
    isHeaderShareLink &&
    actionContext.eventShareLink &&
    headerShareLinkStatus === "active",
  );

  if (!displayNotification) {
    return (
      <aside className="updates-page-detail editor-scrollbar border-l border-[var(--ns-border)]">
        <div className="updates-page-detail-inner">
          <SummaryCard title={t(locale, "updates.summary")}>
            <div className="updates-page-summary-count">
              <strong>{unreadCount}</strong>
              <span>{t(locale, "updates.unreadLabel")}</span>
              <p>{t(locale, "updates.totalNotifications", { count: totalCount })}</p>
              <p>{getLocalizedNotificationStatusLabel(status, locale)}</p>
            </div>
            <div className="updates-page-summary-metrics">
              <span>
                <strong>{summaryMetrics.pendingReviewCount}</strong>
                <small>{t(locale, "updates.pendingReview")}</small>
              </span>
              <span>
                <strong>{summaryMetrics.grantCount}</strong>
                <small>{t(locale, "updates.grantsGroups")}</small>
              </span>
              <span>
                <strong>{summaryMetrics.sharingCount}</strong>
                <small>{t(locale, "updates.sharing")}</small>
              </span>
              <span>
                <strong>{summaryMetrics.failedInviteCount}</strong>
                <small>{t(locale, "updates.failedInvites")}</small>
              </span>
            </div>
          </SummaryCard>

          <SummaryCard title={t(locale, "updates.watchedDocuments")}>
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
              locale={locale}
              state="watched"
            />
          </SummaryCard>
        </div>
      </aside>
    );
  }

  return (
    <aside className="updates-page-detail editor-scrollbar border-l border-[var(--ns-border)]">
      <div className="updates-page-detail-inner">
        <section className="updates-page-detail-card">
          <header className="updates-page-detail-heading">
            <div>
              <span className={["updates-page-kind-pill", `is-${displayNotification.kind}`].join(" ")}>
                {displayNotification.badgeLabel ?? getNotificationKindLabel(displayNotification.kind, locale)}
              </span>
              <h2>{getDetailTitle(notification, displayNotification, locale)}</h2>
              <p>{formatDetailLead(notification, displayNotification, locale)}</p>
              <p className="updates-page-detail-time">{displayNotification.time}</p>
              {isHeaderShareLink ? (
                <ShareLinkHeaderSummary
                  link={actionContext.eventShareLink}
                  loading={actionContext.status === "loading"}
                  locale={locale}
                  notification={notification}
                  status={headerShareLinkStatus}
                  unavailableLabel={getUnavailableShareLinkLabel(headerShareLinkEvent, locale)}
                />
              ) : null}
            </div>
            <div className="updates-page-detail-heading-actions">
              {canRevokeHeaderShareLink ? (
                <button
                  className="updates-page-detail-revoke"
                  disabled={actionContext.operation !== null}
                  onClick={() => actionContext.eventShareLink ? void actionContext.revokeLink(actionContext.eventShareLink.id) : undefined}
                  type="button"
                >
                  <Trash2 className="h-4 w-4" />
                  {actionContext.eventShareLink && actionContext.operation === `revokeLink:${actionContext.eventShareLink.id}`
                    ? localText(locale, "Revoking", "正在撤销")
                    : localText(locale, "Revoke link", "撤销该链接")}
                </button>
              ) : null}
              <button
                aria-label={localText(locale, "Close detail", "关闭详情")}
                className="updates-page-detail-close"
                onClick={onClose}
                type="button"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </header>

          <section className="updates-page-detail-section">
            <h3>{localText(locale, "Affected resource", "受影响的资源")}</h3>
            <div className="updates-page-resource-card">
              <span className="updates-page-resource-icon">
                <FileText className="h-5 w-5" />
              </span>
              <span className="min-w-0">
                <strong>{notification?.resource?.title ?? displayNotification.subject}</strong>
                <small>{notification?.resource?.path ?? displayNotification.detail ?? localText(locale, "Workspace resource", "工作区资源")}</small>
              </span>
              {notification?.resource?.resourceType === "document" ? (
                <a href={displayNotification.actionHref} title={localText(locale, "Open resource", "打开资源")}>
                  {localText(locale, "Open", "打开")}
                </a>
              ) : null}
            </div>
          </section>

          <section className="updates-page-detail-section">
            <h3>{localText(locale, "Operation details", "操作详情")}</h3>
            <dl className="updates-page-detail-list">
              <div>
                <dt>{localText(locale, "Operation", "操作类型")}</dt>
                <dd>{getNotificationOperationLabel(notification, locale)}</dd>
              </div>
              <div>
                <dt>{localText(locale, "Actor", "操作人")}</dt>
                <dd>{notification?.actor?.displayName ?? displayNotification.actor?.name ?? localText(locale, "System", "系统")}</dd>
              </div>
              <div>
                <dt>{localText(locale, "Time", "时间")}</dt>
                <dd>{displayNotification.time}</dd>
              </div>
              <div>
                <dt>{localText(locale, "Status", "状态")}</dt>
                <dd>
                  <span className={["updates-page-state-pill", notification?.state ? `is-${notification.state}` : ""].join(" ")}>
                    {formatStateLabel(notification?.state, locale)}
                  </span>
                </dd>
              </div>
            </dl>
          </section>

          <ActionClosurePanel context={actionContext} displayNotification={displayNotification} locale={locale} notification={notification} />
        </section>
      </div>
    </aside>
  );
}

type AccessSharingActionContext = {
  accessRequest: AccessRequestDto | null;
  denyAccessRequest: () => Promise<void>;
  emailInvites: EmailInviteDto[];
  error: string | null;
  eventShareLink: ShareLinkDto | null;
  message: string | null;
  operation: string | null;
  reviewAccessRequest: () => Promise<void>;
  reviewExpiresAt: string;
  reviewReason: string;
  reviewRole: string;
  revokeInvite: () => Promise<void>;
  revokeLink: (linkId: string) => Promise<void>;
  retryInvite: () => Promise<void>;
  selectedInvite: EmailInviteDto | null;
  setReviewExpiresAt: (value: string) => void;
  setReviewReason: (value: string) => void;
  setReviewRole: (value: string) => void;
  status: "idle" | "loading" | "ready" | "error";
};

function ActionClosurePanel({
  context,
  displayNotification,
  locale,
  notification,
}: {
  context: AccessSharingActionContext;
  displayNotification: WorkspaceNotification;
  locale: ReturnType<typeof useDisplayLanguage>["locale"];
  notification: PermissionNotificationDto | null;
}) {
  const isAccessRequest = notification?.type === "access_request.created";
  const isFailedInvite = notification?.type === "email_invite.delivery_failed" || notification?.state === "failed";
  const isShareLink = notification?.type.startsWith("share_link.");

  if (context.status === "loading") {
    return (
      <section className="updates-page-detail-section">
        <h3>{localText(locale, "Recommended actions", "推荐操作")}</h3>
        <p className="updates-page-summary-note">
          <Loader2 className="mr-2 inline h-4 w-4 animate-spin" />
          {localText(locale, "Loading action context.", "正在加载操作上下文。")}
        </p>
      </section>
    );
  }

  if (isAccessRequest) {
    return (
      <section className="updates-page-detail-section">
        <h3>{localText(locale, "Recommended actions", "推荐操作")}</h3>
        <div className="updates-page-action-grid">
          <label>
            <span>{localText(locale, "Role", "授权角色")}</span>
            <select
              disabled={!context.accessRequest || context.operation !== null}
              onChange={(event) => context.setReviewRole(event.target.value)}
              value={context.reviewRole}
            >
              <option value="viewer">Viewer</option>
              <option value="commenter">Commenter</option>
              <option value="editor">Editor</option>
            </select>
          </label>
          <label>
            <span>{localText(locale, "Expires at", "过期时间")}</span>
            <input
              disabled={!context.accessRequest || context.operation !== null}
              onChange={(event) => context.setReviewExpiresAt(event.target.value)}
              type="datetime-local"
              value={context.reviewExpiresAt}
            />
          </label>
          <label className="updates-page-action-full">
            <span>{localText(locale, "Review note", "审核说明")}</span>
            <textarea
              disabled={!context.accessRequest || context.operation !== null}
              onChange={(event) => context.setReviewReason(event.target.value)}
              placeholder={localText(locale, "Optional reason for audit history", "可选，会进入审计记录")}
              value={context.reviewReason}
            />
          </label>
        </div>
        <div className="updates-page-action-buttons">
          <button
            disabled={!context.accessRequest || context.operation !== null}
            onClick={() => void context.reviewAccessRequest()}
            type="button"
          >
            <Check className="h-4 w-4" />
            {context.operation === "approve" ? localText(locale, "Approving", "正在批准") : localText(locale, "Approve", "批准")}
          </button>
          <button
            className="is-secondary"
            disabled={!context.accessRequest || context.operation !== null}
            onClick={() => void context.denyAccessRequest()}
            type="button"
          >
            {context.operation === "deny" ? localText(locale, "Denying", "正在拒绝") : localText(locale, "Deny", "拒绝")}
          </button>
        </div>
        <ActionFeedback context={context} />
      </section>
    );
  }

  if (isFailedInvite) {
    return (
      <section className="updates-page-detail-section">
        <h3>{localText(locale, "Recommended actions", "推荐操作")}</h3>
        <div className="updates-page-invite-card">
          <span className="updates-page-resource-icon is-danger">
            <AlertCircle className="h-5 w-5" />
          </span>
          <span>
            <strong>{context.selectedInvite?.email ?? localText(locale, "Failed invite", "失败邀请")}</strong>
            <small>
              {context.selectedInvite
                ? `${context.selectedInvite.deliveryStatus} / ${context.selectedInvite.deliveryErrorCode ?? "no_error_code"}`
                : localText(locale, "Invite detail is unavailable.", "邀请详情不可用。")}
            </small>
          </span>
        </div>
        <div className="updates-page-action-buttons">
          <button
            disabled={!context.selectedInvite || context.operation !== null}
            onClick={() => void context.retryInvite()}
            type="button"
          >
            <RotateCcw className="h-4 w-4" />
            {context.operation === "retryInvite" ? localText(locale, "Retrying", "正在重发") : localText(locale, "Retry invite", "重新发送")}
          </button>
          <button
            className="is-secondary"
            disabled={!context.selectedInvite || context.operation !== null}
            onClick={() => void context.revokeInvite()}
            type="button"
          >
            <Trash2 className="h-4 w-4" />
            {localText(locale, "Revoke invite", "撤销邀请")}
          </button>
          <a className="updates-page-action-button-link" href={displayNotification.actionHref}>
            <ShieldCheck className="h-4 w-4" />
            {localText(locale, "Manage permissions", "管理权限")}
          </a>
        </div>
        <ActionFeedback context={context} />
      </section>
    );
  }

  if (isShareLink) {
    return <ActionFeedback context={context} />;
  }

  return (
    <section className="updates-page-detail-section">
      <h3>{localText(locale, "Recommended actions", "推荐操作")}</h3>
      <div className="updates-page-action-buttons">
        <a className="updates-page-action-button-link" href={displayNotification.actionHref}>
          <ShieldCheck className="h-4 w-4" />
          {localText(locale, "Open access settings", "打开权限设置")}
        </a>
      </div>
    </section>
  );
}

function ShareLinkHeaderSummary({
  link,
  loading,
  locale,
  notification,
  status,
  unavailableLabel,
}: {
  link: ShareLinkDto | null;
  loading: boolean;
  locale: ReturnType<typeof useDisplayLanguage>["locale"];
  notification: PermissionNotificationDto | null;
  status: ShareLinkStatus;
  unavailableLabel: string;
}) {
  if (!link) {
    return (
      <div className="updates-page-detail-heading-link is-muted">
        <div className="updates-page-detail-heading-link-title">
          <Link2 className="h-4 w-4" />
          <span>
            <strong>{loading ? localText(locale, "Loading link metadata", "正在加载链接元数据") : unavailableLabel}</strong>
            <small>{loading ? localText(locale, "Loading", "加载中") : formatShareLinkStatus(status, locale)}</small>
          </span>
        </div>
      </div>
    );
  }

  return (
    <div className="updates-page-detail-heading-link">
      <div className="updates-page-detail-heading-link-title">
        <Link2 className="h-4 w-4" />
        <span>
          <strong>{formatShareLinkManagementTitle(link, locale)}</strong>
          <small>{formatShareLinkPermission(link, locale)}</small>
        </span>
      </div>
      <dl className="updates-page-detail-heading-link-facts">
        <div>
          <dt>{localText(locale, "Created", "创建")}</dt>
          <dd>{formatShareLinkCreatedBy(link, notification, locale)}</dd>
        </div>
        <div>
          <dt>{localText(locale, "Expiry", "过期")}</dt>
          <dd>{formatShareLinkExpiry(link, locale)}</dd>
        </div>
        <div>
          <dt>{localText(locale, "Status", "状态")}</dt>
          <dd>{formatShareLinkStatus(status, locale)}</dd>
        </div>
        <div>
          <dt>{localText(locale, "Identifier", "标识")}</dt>
          <dd>{formatShareLinkIdentifier(link.id, locale)}</dd>
        </div>
      </dl>
    </div>
  );
}

function ActionFeedback({ context }: { context: AccessSharingActionContext }) {
  if (context.error) {
    return <p className="updates-page-action-feedback is-error">{context.error}</p>;
  }

  if (context.message) {
    return <p className="updates-page-action-feedback is-success">{context.message}</p>;
  }

  return null;
}

function useAccessSharingActionContext(
  notification: PermissionNotificationDto | null,
  workspaceId: string | null,
  onRefresh: () => Promise<void>,
  locale: ReturnType<typeof useDisplayLanguage>["locale"],
): AccessSharingActionContext {
  const [accessRequest, setAccessRequest] = useState<AccessRequestDto | null>(null);
  const [eventShareLink, setEventShareLink] = useState<ShareLinkDto | null>(null);
  const [emailInvites, setEmailInvites] = useState<EmailInviteDto[]>([]);
  const [status, setStatus] = useState<AccessSharingActionContext["status"]>("idle");
  const [operation, setOperation] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [reviewRole, setReviewRole] = useState("viewer");
  const [reviewReason, setReviewReason] = useState("");
  const [reviewExpiresAt, setReviewExpiresAt] = useState("");
  const resourceType = notification?.action?.resourceType ?? notification?.resource?.resourceType ?? notification?.resourceType;
  const resourceId = notification?.action?.resourceId ?? notification?.resource?.resourceId ?? notification?.resourceId;
  const accessRequestId = notification?.action?.accessRequestId ?? notification?.action?.subjectId;
  const subjectType = notification?.action?.subjectType;
  const subjectId = notification?.action?.subjectId;
  const notificationId = notification?.id ?? null;
  const notificationType = notification?.type ?? null;

  useEffect(() => {
    setMessage(null);
    setError(null);
    setAccessRequest(null);
    setEventShareLink(null);
    setEmailInvites([]);
    if (!notificationId || !notificationType) {
      setStatus("idle");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    const loads: Promise<unknown>[] = [];
    if (workspaceId && accessRequestId && notificationType === "access_request.created") {
      loads.push(
        getAccessRequests(workspaceId, null, controller.signal).then((body) => {
          const request = body.requests.find((item) => item.id === accessRequestId) ?? null;
          setAccessRequest(request);
          setReviewRole(request?.requestedRole ?? "viewer");
        }),
      );
    }

    if (resourceType && resourceId) {
      loads.push(getResourceEmailInvites(resourceType, resourceId, controller.signal).then((body) => setEmailInvites(body.invites)));
    }

    if (subjectType === "share_link" && subjectId) {
      loads.push(
        getShareLink(subjectId, controller.signal)
          .then((link) => setEventShareLink(link))
          .catch((value: unknown) => {
            if (value instanceof DOMException && value.name === "AbortError") {
              throw value;
            }

            setEventShareLink(null);
          }),
      );
    }

    if (loads.length === 0) {
      setStatus("ready");
      return () => controller.abort();
    }

    void Promise.all(loads)
      .then(() => setStatus("ready"))
      .catch((value: unknown) => {
        if (value instanceof DOMException && value.name === "AbortError") {
          return;
        }

        setStatus("error");
        setError(toActionError(value, localText(locale, "Could not load action details.", "无法加载操作详情。")));
      });

    return () => controller.abort();
  }, [accessRequestId, locale, notificationId, notificationType, resourceId, resourceType, subjectId, subjectType, workspaceId]);

  const selectedInvite = useMemo(() => {
    if (subjectType === "email_invite" && subjectId) {
      return emailInvites.find((invite) => invite.id === subjectId) ?? null;
    }

    return emailInvites.find((invite) => invite.deliveryStatus === "failed") ?? null;
  }, [emailInvites, subjectId, subjectType]);

  const runOperation = async (operationKey: string, callback: () => Promise<void>) => {
    if (operation) {
      return;
    }

    setOperation(operationKey);
    setMessage(null);
    setError(null);
    try {
      await callback();
      await onRefresh();
      if (resourceType && resourceId) {
        const invitesBody = await getResourceEmailInvites(resourceType, resourceId);
        setEmailInvites(invitesBody.invites);
      }
    } catch (value) {
      setError(toActionError(value, localText(locale, "Operation failed.", "操作失败。")));
    } finally {
      setOperation(null);
    }
  };

  return {
    accessRequest,
    denyAccessRequest: () =>
      runOperation("deny", async () => {
        if (!accessRequest) {
          throw new Error(localText(locale, "Access request is unavailable.", "访问请求不可用。"));
        }

        await reviewAccessRequest(accessRequest.id, {
          decision: "deny",
          reason: reviewReason || null,
          roleKey: null,
        });
        setMessage(localText(locale, "Access request denied.", "访问请求已拒绝。"));
      }),
    emailInvites,
    error,
    eventShareLink,
    message,
    operation,
    reviewAccessRequest: () =>
      runOperation("approve", async () => {
        if (!accessRequest) {
          throw new Error(localText(locale, "Access request is unavailable.", "访问请求不可用。"));
        }

        await reviewAccessRequest(accessRequest.id, {
          decision: "approve",
          expiresAt: reviewExpiresAt ? new Date(reviewExpiresAt).toISOString() : null,
          reason: reviewReason || null,
          roleKey: reviewRole as AccessRequestDto["requestedRole"],
        });
        setMessage(localText(locale, "Access request approved.", "访问请求已批准。"));
      }),
    reviewExpiresAt,
    reviewReason,
    reviewRole,
    revokeInvite: () =>
      runOperation("revokeInvite", async () => {
        if (!selectedInvite) {
          throw new Error(localText(locale, "Invite is unavailable.", "邀请不可用。"));
        }

        await revokeEmailInvite(selectedInvite.id);
        setMessage(localText(locale, "Invite revoked.", "邀请已撤销。"));
      }),
    revokeLink: (linkId: string) =>
      runOperation(`revokeLink:${linkId}`, async () => {
        await revokeShareLink(linkId);
        setEventShareLink((current) => current?.id === linkId ? { ...current, revokedAt: new Date().toISOString() } : current);
        setMessage(localText(locale, "Share link revoked.", "分享链接已撤销。"));
      }),
    retryInvite: () =>
      runOperation("retryInvite", async () => {
        if (!selectedInvite) {
          throw new Error(localText(locale, "Invite is unavailable.", "邀请不可用。"));
        }

        await retryEmailInvite(selectedInvite.id);
        setMessage(localText(locale, "Invite retry created.", "已创建重发邀请。"));
      }),
    selectedInvite,
    setReviewExpiresAt,
    setReviewReason,
    setReviewRole,
    status,
  };
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

function PreferenceResourceList({
  fallbackRows,
  locale,
  preferences,
  state,
}: {
  fallbackRows: ReturnType<typeof toNotificationPreferenceResourceRows>;
  locale: ReturnType<typeof useDisplayLanguage>["locale"];
  preferences: ReturnType<typeof useNotificationPreferences>;
  state: "muted" | "watched";
}) {
  const liveRows = useMemo(
    () => toNotificationPreferenceResourceRows(preferences.preferences).filter((row) => row.state === state),
    [preferences.preferences, state],
  );
  const rows = preferences.status === "unconfigured" ? fallbackRows : liveRows;

  if (preferences.status !== "ready" && preferences.status !== "unconfigured") {
    return <p className="updates-page-summary-note">{getLocalizedNotificationPreferenceStatusLabel(preferences.status, locale)}</p>;
  }

  if (rows.length === 0) {
    return (
      <p className="updates-page-summary-note">
        {state === "watched"
          ? localText(locale, "No watched resources", "暂无关注资源")
          : localText(locale, "No muted resources", "暂无静音资源")}
      </p>
    );
  }

  return (
    <div className="updates-page-compact-list">
      {rows.slice(0, 4).map((row) => (
        <a href={row.href} key={row.id} title={row.updatedAt ? `Updated ${row.updatedAt}` : undefined}>
          {row.resourceType === "collection" ? <Eye className="h-4 w-4" /> : <FileText className="h-4 w-4" />}
          <span className="updates-page-compact-text min-w-0 flex-1">
            <span>{row.label}</span>
            {row.description ? <small>{row.description}</small> : null}
          </span>
        </a>
      ))}
    </div>
  );
}

function getDetailTitle(
  notification: PermissionNotificationDto | null,
  displayNotification: WorkspaceNotification,
  locale: ReturnType<typeof useDisplayLanguage>["locale"],
) {
  switch (notification?.type) {
    case "access_request.created":
      return localText(locale, "Access request pending", "访问请求待处理");
    case "share_link.created":
      return localText(locale, "Share link created", "分享链接已创建");
    case "share_link.revoked":
      return localText(locale, "Share link revoked", "分享链接已撤销");
    case "email_invite.delivery_failed":
      return localText(locale, "Invite delivery failed", "邀请发送失败");
    case "email_invite.created":
      return localText(locale, "Email invite created", "邮件邀请已创建");
    case "email_invite.revoked":
      return localText(locale, "Email invite revoked", "邮件邀请已撤销");
    default:
      return displayNotification.subject;
  }
}

function formatDetailLead(
  notification: PermissionNotificationDto | null,
  displayNotification: WorkspaceNotification,
  locale: ReturnType<typeof useDisplayLanguage>["locale"],
) {
  const actor = notification?.actor?.displayName ?? displayNotification.actor?.name ?? localText(locale, "System", "系统");
  const resource = notification?.resource?.title ?? displayNotification.subject;
  if (notification?.type === "share_link.created") {
    return localText(locale, `${actor} created a share link.`, `${actor} 创建了分享链接。`);
  }

  if (notification?.type === "share_link.revoked") {
    return localText(locale, `${actor} revoked a share link.`, `${actor} 撤销了分享链接。`);
  }

  if (notification?.type === "email_invite.delivery_failed") {
    return localText(locale, `${actor}'s email invite could not be delivered.`, `${actor} 的邮件邀请发送失败。`);
  }

  return localText(
    locale,
    `${actor} ${displayNotification.messagePrefix ?? "updated access"} ${resource}.`,
    `${actor} ${displayNotification.messagePrefix ?? "更新了访问与分享"} ${resource}。`,
  );
}

function getNotificationOperationLabel(
  notification: PermissionNotificationDto | null,
  locale: ReturnType<typeof useDisplayLanguage>["locale"],
) {
  switch (notification?.type) {
    case "access_request.created":
      return localText(locale, "Access request review", "访问请求审核");
    case "access_request.approved":
      return localText(locale, "Access request approved", "访问请求已批准");
    case "access_request.denied":
      return localText(locale, "Access request denied", "访问请求已拒绝");
    case "share_link.created":
      return localText(locale, "Share link created", "分享链接已创建");
    case "share_link.revoked":
      return localText(locale, "Share link revoked", "分享链接已撤销");
    case "email_invite.created":
      return localText(locale, "Email invite created", "邮件邀请已创建");
    case "email_invite.delivery_failed":
      return localText(locale, "Email invite failed", "邮件邀请失败");
    case "email_invite.revoked":
      return localText(locale, "Email invite revoked", "邮件邀请已撤销");
    case "permission.grant_created":
      return localText(locale, "Permission granted", "权限已授予");
    case "permission.grant_revoked":
      return localText(locale, "Permission revoked", "权限已撤销");
    default:
      return notification?.type ?? localText(locale, "Access and sharing update", "访问与分享更新");
  }
}

function formatStateLabel(state: string | null | undefined, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  if (!state) {
    return localText(locale, "informational", "已记录");
  }

  switch (state) {
    case "pending_review":
      return localText(locale, "pending review", "待处理");
    case "failed":
      return localText(locale, "failed", "失败");
    case "revoked":
      return localText(locale, "revoked", "已撤销");
    case "resolved":
      return localText(locale, "resolved", "已处理");
    case "expiring":
      return localText(locale, "expiring", "即将过期");
    case "expired":
      return localText(locale, "expired", "已过期");
    case "informational":
      return localText(locale, "informational", "已记录");
    default:
      return state.replace(/_/g, " ");
  }
}

function getNotificationKindLabel(kind: NotificationKind, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  switch (kind) {
    case "access":
      return localText(locale, "Access", "访问");
    case "grant":
      return localText(locale, "Grant", "授权");
    case "sharing":
      return localText(locale, "Sharing", "分享");
    case "failed":
      return localText(locale, "Failed", "失败");
    case "expiry":
      return localText(locale, "Expiry", "过期");
    case "permission":
    default:
      return localText(locale, "Access", "访问");
  }
}

function formatShareLinkPermission(link: ShareLinkDto, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  const role = formatShareLinkRole(link.roleKey, locale);
  if (link.audience === "public") {
    return localText(locale, `Anyone with the link · ${role}`, `拥有链接的任何人 · ${role}`);
  }

  if (link.audience === "external") {
    return localText(locale, `${link.subjectEmail ?? "Specific email"} · ${role}`, `${link.subjectEmail ?? "指定邮箱用户"} · ${role}`);
  }

  return localText(locale, `Workspace members · ${role}`, `工作区成员 · ${role}`);
}

function formatShareLinkManagementTitle(link: ShareLinkDto, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  return localText(locale, `Link ${shortIdentifier(link.id)}`, `链接 ${shortIdentifier(link.id)}`);
}

type ShareLinkEvent = "created" | "expired" | "other" | "revoked";
type ShareLinkStatus = "active" | "expired" | "missing" | "revoked";

function getShareLinkEvent(notification: PermissionNotificationDto | null): ShareLinkEvent {
  if (notification?.type === "share_link.revoked" || notification?.state === "revoked") {
    return "revoked";
  }

  if (notification?.type === "share_link.expired" || notification?.state === "expired") {
    return "expired";
  }

  if (notification?.type === "share_link.created") {
    return "created";
  }

  return "other";
}

function getShareLinkStatus(link: ShareLinkDto | null, notification: PermissionNotificationDto | null): ShareLinkStatus {
  if (link?.revokedAt) {
    return "revoked";
  }

  if (link?.expiresAt && new Date(link.expiresAt).getTime() <= Date.now()) {
    return "expired";
  }

  if (link) {
    return "active";
  }

  const event = getShareLinkEvent(notification);
  if (event === "revoked") {
    return "revoked";
  }

  if (event === "expired") {
    return "expired";
  }

  return "missing";
}

function formatShareLinkStatus(status: ShareLinkStatus, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  switch (status) {
    case "active":
      return localText(locale, "Active", "有效");
    case "revoked":
      return localText(locale, "Revoked", "已撤销");
    case "expired":
      return localText(locale, "Expired", "已过期");
    case "missing":
    default:
      return localText(locale, "Metadata unavailable", "链接元数据不可用");
  }
}

function getUnavailableShareLinkLabel(event: ShareLinkEvent, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  if (event === "revoked") {
    return localText(locale, "Revoked link metadata unavailable", "已撤销链接的元数据不可用");
  }

  if (event === "expired") {
    return localText(locale, "Expired link metadata unavailable", "已过期链接的元数据不可用");
  }

  return localText(locale, "Share link metadata unavailable", "分享链接元数据不可用");
}

function formatShareLinkRole(roleKey: string, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  if (roleKey === "viewer") {
    return localText(locale, "Can view", "可查看");
  }

  if (roleKey === "commenter") {
    return localText(locale, "Can comment", "可评论");
  }

  return roleKey;
}

function formatShareLinkCreatedBy(
  link: ShareLinkDto,
  notification: PermissionNotificationDto | null,
  locale: ReturnType<typeof useDisplayLanguage>["locale"],
) {
  const actor = notification?.actor?.displayName?.trim() ||
    (link.createdBy ? formatPrincipalIdentifier(link.createdBy, locale) : localText(locale, "Unknown creator", "未知创建人"));

  return `${actor} · ${formatShareLinkCreatedAt(link)}`;
}

function formatShareLinkCreatedAt(link: ShareLinkDto) {
  return formatTimestamp(link.createdAt);
}

function formatShareLinkExpiry(link: ShareLinkDto, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  return link.expiresAt ? formatTimestamp(link.expiresAt) : localText(locale, "No expiry", "不过期");
}

function formatShareLinkIdentifier(id: string, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  return localText(locale, `ID ${shortIdentifier(id)}`, `ID ${shortIdentifier(id)}`);
}

function formatPrincipalIdentifier(id: string, locale: ReturnType<typeof useDisplayLanguage>["locale"]) {
  return localText(locale, `User ${shortIdentifier(id)}`, `用户 ${shortIdentifier(id)}`);
}

function shortIdentifier(id: string) {
  const compact = id.replace(/-/g, "");
  return compact.length > 8 ? `#${compact.slice(0, 8)}` : `#${compact || "unknown"}`;
}

function formatTimestamp(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function localText(locale: ReturnType<typeof useDisplayLanguage>["locale"], en: string, zh: string) {
  return locale === "zh-CN" ? zh : en;
}

function getLocalizedNotificationStatusLabel(
  status: NotificationApiStatus,
  locale: ReturnType<typeof useDisplayLanguage>["locale"],
) {
  if (status === "ready") {
    return localText(locale, "Live access and sharing notifications", "实时访问与分享通知");
  }

  if (status === "loading") {
    return localText(locale, "Loading access and sharing notifications", "正在加载访问与分享通知");
  }

  if (status === "forbidden") {
    return localText(locale, "Notification access unavailable", "当前无法访问通知");
  }

  if (status === "error") {
    return localText(locale, "Notification API unavailable", "通知 API 暂不可用");
  }

  return localText(
    locale,
    "Demo access & sharing notifications shown until API is configured",
    "API 配置前显示演示访问与分享通知",
  );
}

function getLocalizedNotificationPreferenceStatusLabel(
  status: NotificationApiStatus,
  locale: ReturnType<typeof useDisplayLanguage>["locale"],
) {
  if (status === "ready") {
    return localText(locale, "Live notification preferences", "实时通知偏好");
  }

  if (status === "loading") {
    return localText(locale, "Loading notification preferences", "正在加载通知偏好");
  }

  if (status === "forbidden") {
    return localText(locale, "Preference access unavailable", "当前无法访问通知偏好");
  }

  if (status === "error") {
    return localText(locale, "Notification preferences API unavailable", "通知偏好 API 暂不可用");
  }

  return localText(
    locale,
    "Demo watched and muted resources shown until API is configured",
    "API 配置前显示演示关注与静音资源",
  );
}

function toActionError(value: unknown, fallback: string) {
  if (value instanceof ApiClientError) {
    return value.message || fallback;
  }

  if (value instanceof Error) {
    return value.message || fallback;
  }

  return fallback;
}

function getNotificationIcon(kind: NotificationKind) {
  switch (kind) {
    case "access":
      return UsersRound;
    case "grant":
      return UserCheck;
    case "sharing":
      return Link2;
    case "expiry":
      return Clock3;
    case "failed":
      return AlertCircle;
    case "permission":
    default:
      return ShieldCheck;
  }
}

function filterDemoNotificationGroups(
  groups: NotificationGroup[],
  activeTab: WorkspaceUpdatesTab,
  sortDirection: "asc" | "desc",
): NotificationGroup[] {
  const filteredGroups = groups
    .map((group) => ({
      ...group,
      notifications: group.notifications.filter((notification) => {
        if (activeTab === "all") {
          return true;
        }

        if (activeTab === "unread") {
          return notification.unread;
        }

        if (activeTab === "links" || activeTab === "invites") {
          return notification.kind === "sharing";
        }

        return notification.kind === (activeTab === "grants" ? "grant" : activeTab);
      }),
    }))
    .filter((group) => group.notifications.length > 0);

  if (sortDirection === "desc") {
    return filteredGroups;
  }

  return [...filteredGroups]
    .reverse()
    .map((group) => ({ ...group, notifications: [...group.notifications].reverse() }));
}

function usePermissionNotifications(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [notifications, setNotifications] = useState<PermissionNotificationDto[]>([]);
  const [summary, setSummary] = useState<AccessSharingSummaryResponse | null>(null);
  const [unreadCount, setUnreadCount] = useState(0);
  const [status, setStatus] = useState<NotificationApiStatus>(() =>
    apiBaseUrl ? "loading" : "unconfigured",
  );

  const loadNotifications = async (signal?: AbortSignal) => {
    if (!apiBaseUrl) {
      setNotifications([]);
      setSummary(null);
      setUnreadCount(0);
      setStatus("unconfigured");
      return;
    }

    const [body, summaryBody] = await Promise.all([
      getWorkspaceNotifications(workspaceId, signal),
      getAccessSharingSummary(workspaceId, signal),
    ]);
    setNotifications(body.notifications);
    setSummary(summaryBody);
    setUnreadCount(body.unreadCount);
    setStatus("ready");
  };

  useEffect(() => {
    if (!apiBaseUrl) {
      setNotifications([]);
      setSummary(null);
      setUnreadCount(0);
      setStatus("unconfigured");
      return;
    }

    setStatus("loading");
    const controller = new AbortController();
    void loadNotifications(controller.signal)
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        if (error instanceof ApiClientError && (error.status === 401 || error.status === 403)) {
          setNotifications([]);
          setSummary(null);
          setUnreadCount(0);
          setStatus("forbidden");
          return;
        }

        setNotifications([]);
        setSummary(null);
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
        setSummary((current) => {
          if (!current) {
            return current;
          }

          return {
            ...current,
            pendingReviewCount: updated.type === "access_request.created"
              ? Math.max(0, current.pendingReviewCount - 1)
              : current.pendingReviewCount,
            unreadCount: Math.max(0, current.unreadCount - 1),
          };
        });
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
        setSummary((current) => current ? { ...current, pendingReviewCount: 0, unreadCount: 0 } : current);
      })
      .catch(() => {
        // The next reload remains authoritative.
      });
  };

  return {
    markAllRead,
    markRead,
    notifications,
    refresh: () => loadNotifications(),
    status,
    summary: summary ?? (notifications.length > 0 ? createAccessSharingSummaryFromNotifications(notifications, unreadCount) : null),
    unreadCount,
  };
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
    return "Loading live access and sharing notifications.";
  }

  if (status === "forbidden") {
    return "Sign in with notification access to load workspace access and sharing notifications.";
  }

  return "Live access and sharing notifications could not be loaded. Demo notifications are hidden while the API is configured.";
}
