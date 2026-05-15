import type {
  AccessSharingSummaryResponse,
  PermissionNotificationDto,
  PermissionNotificationPreferenceDto,
} from "./appApi";
import { isUuid } from "./apiClient";
import {
  createDocumentAdvancedPermissionsHash,
  createEditorHash,
  createLibrariesHash,
  createWorkspacePermissionsHash,
  createWorkspaceUpdatesHash,
  normalizeInternalActionHash,
  parseHashRoute,
} from "./hashRouting";
import type { NotificationKind, WorkspaceNotification } from "../data/workspaceUpdatesData";

export type NotificationApiStatus = "unconfigured" | "loading" | "ready" | "forbidden" | "error";
export type WorkspaceUpdatesTab = "access" | "all" | "expiry" | "failed" | "grants" | "invites" | "links" | "sharing" | "unread";
type WorkspaceNotificationLocale = "en" | "en-US" | "zh-CN";

export type NotificationPreferenceResourceRow = {
  description?: string;
  href: string;
  id: string;
  label: string;
  resourceType: string;
  state: "muted" | "watched";
  updatedAt: string;
};

export function toWorkspaceNotification(
  notification: PermissionNotificationDto,
  locale: WorkspaceNotificationLocale = "en-US",
): WorkspaceNotification {
  const kind = getNotificationKind(notification.type, notification.category, notification.state);
  const resourceTitle = notification.resource?.title?.trim();
  const target = resourceTitle || formatNotificationTarget(notification.resourceType, notification.resourceId);
  const resourcePath = notification.resource?.path?.trim();

  return {
    actionHref: getNotificationActionHref(notification),
    actionLabel: getNotificationActionLabel(notification),
    actor: toNotificationActor(notification),
    badgeLabel: getNotificationBadgeLabel(notification.type, kind, locale),
    detail: formatNotificationDetail(notification.body, resourcePath && resourcePath !== target ? resourcePath : null, locale),
    id: notification.id,
    kind,
    messagePrefix: getNotificationActionText(notification.type, locale),
    messageSuffix: formatNotificationMessageSuffix(target, resourcePath, locale),
    subject: resourceTitle || notification.title || target || localText(locale, "Access and sharing notification", "访问与分享通知"),
    time: formatRelativeNotificationTime(notification.createdAt, locale),
    unread: notification.readAt === null,
  };
}

export function getNotificationKind(type: string, category?: string | null, state?: string | null): NotificationKind {
  const normalizedState = state?.trim().toLowerCase();
  const normalizedType = type.toLowerCase();
  if (normalizedState === "failed" || normalizedType === "email_invite.delivery_failed") {
    return "failed";
  }

  return getNotificationCategory(type, category);
}

function getNotificationCategory(type: string, category?: string | null): Exclude<NotificationKind, "failed"> {
  const normalizedCategory = category?.trim().toLowerCase();
  if (
    normalizedCategory === "access" ||
    normalizedCategory === "expiry" ||
    normalizedCategory === "grant" ||
    normalizedCategory === "permission" ||
    normalizedCategory === "sharing"
  ) {
    return normalizedCategory;
  }

  const normalized = type.toLowerCase();

  if (normalized.endsWith("_expiring") || normalized.endsWith("_expired")) {
    return "expiry";
  }

  if (normalized.startsWith("access_request.")) {
    return "access";
  }

  if (normalized.startsWith("permission.") || normalized.startsWith("group.")) {
    return "grant";
  }

  if (normalized.startsWith("share_link.") || normalized.startsWith("email_invite.")) {
    return "sharing";
  }

  return "permission";
}

export function createAccessSharingSummaryFromNotifications(
  notifications: PermissionNotificationDto[],
  unreadCount = notifications.filter((notification) => notification.readAt === null).length,
): AccessSharingSummaryResponse {
  return {
    accessRequestCount: notifications.filter((notification) =>
      getNotificationCategory(notification.type, notification.category) === "access"
    ).length,
    expiryCount: notifications.filter((notification) =>
      getNotificationCategory(notification.type, notification.category) === "expiry"
    ).length,
    failedInviteCount: notifications.filter(isFailedNotification).length,
    grantCount: notifications.filter((notification) =>
      getNotificationCategory(notification.type, notification.category) === "grant"
    ).length,
    pendingReviewCount: notifications.filter((notification) =>
      notification.type.toLowerCase() === "access_request.created" && notification.readAt === null
    ).length,
    sharingCount: notifications.filter((notification) =>
      getNotificationCategory(notification.type, notification.category) === "sharing"
    ).length,
    totalCount: notifications.length,
    unreadCount,
  };
}

export function filterWorkspaceNotifications(
  notifications: PermissionNotificationDto[],
  tab: WorkspaceUpdatesTab,
) {
  if (tab === "all") {
    return notifications;
  }

  return notifications.filter((notification) => {
    if (tab === "unread") {
      return notification.readAt === null;
    }

    if (tab === "failed") {
      return isFailedNotification(notification);
    }

    if (tab === "links") {
      return notification.type.toLowerCase().startsWith("share_link.");
    }

    if (tab === "invites") {
      return notification.type.toLowerCase().startsWith("email_invite.");
    }

    const category = getNotificationCategory(notification.type, notification.category);
    if (tab === "access") {
      return category === "access";
    }

    if (tab === "grants") {
      return category === "grant";
    }

    if (tab === "sharing") {
      return category === "sharing";
    }

    return category === "expiry";
  });
}

export function getWorkspaceUpdatesTabLabel(tab: WorkspaceUpdatesTab) {
  switch (tab) {
    case "unread":
      return "Unread";
    case "access":
      return "Access requests";
    case "grants":
      return "Grants & groups";
    case "sharing":
      return "Sharing links & invites";
    case "links":
      return "Links";
    case "invites":
      return "Invites";
    case "expiry":
      return "Expiry";
    case "failed":
      return "Failed invites";
    case "all":
    default:
      return "All";
  }
}

export function getNotificationActionHref(notification: PermissionNotificationDto) {
  const normalizedActionUrl = notification.actionUrl ? normalizeInternalActionHash(notification.actionUrl) : createWorkspaceUpdatesHash();
  if (
    notification.actionUrl &&
    normalizedActionUrl !== createWorkspaceUpdatesHash() &&
    !isGenericPermissionActionUrl(notification.actionUrl)
  ) {
    return normalizedActionUrl;
  }

  const resourceType = notification.action?.resourceType ?? notification.resource?.resourceType ?? notification.resourceType;
  const resourceId = notification.action?.resourceId ?? notification.resource?.resourceId ?? notification.resourceId;

  if (resourceType === "document" && resourceId && isUuid(resourceId)) {
    if (getNotificationCategory(notification.type, notification.category) !== "permission") {
      return createDocumentAdvancedPermissionsHash(resourceId);
    }

    return createEditorHash(resourceId);
  }

  if (resourceType === "collection" && resourceId && isUuid(resourceId)) {
    return createLibrariesHash({ collectionId: resourceId });
  }

  return createWorkspacePermissionsHash();
}

export function getWorkspaceUpdatesTabFromHash(hash: string): WorkspaceUpdatesTab {
  const { params, route } = parseHashRoute(hash);
  if (route !== "#updates" && route !== "#notifications") {
    return "all";
  }

  const tab = params.get("tab");
  return isWorkspaceUpdatesTab(tab) ? tab : "all";
}

export function toNotificationPreferenceResourceRows(
  preferences: PermissionNotificationPreferenceDto[],
): NotificationPreferenceResourceRow[] {
  return preferences
    .filter((preference) => preference.watched || preference.muted)
    .map((preference) => {
      const resourceType = normalizeResourceType(preference.resource?.resourceType ?? preference.resourceType);
      const resourceId = preference.resource?.resourceId ?? preference.resourceId;
      const label = preference.resource?.title?.trim() || formatPreferenceResourceLabel(resourceType, resourceId);
      const description = preference.resource?.path?.trim();
      const href = createPreferenceResourceHref(resourceType, resourceId);
      const state = preference.muted ? "muted" : "watched";
      return {
        description: description && description !== label ? description : undefined,
        href,
        id: preference.id,
        label,
        resourceType,
        state,
        updatedAt: preference.updatedAt,
      };
    });
}

export function getNotificationStatusLabel(status: NotificationApiStatus) {
  if (status === "ready") {
    return "Live access and sharing notifications";
  }

  if (status === "loading") {
    return "Loading access and sharing notifications";
  }

  if (status === "forbidden") {
    return "Notification access unavailable";
  }

  if (status === "error") {
    return "Notification API unavailable";
  }

  return "Demo access & sharing notifications shown until API is configured";
}

export function getNotificationPreferenceStatusLabel(status: NotificationApiStatus) {
  if (status === "ready") {
    return "Live notification preferences";
  }

  if (status === "loading") {
    return "Loading notification preferences";
  }

  if (status === "forbidden") {
    return "Preference access unavailable";
  }

  if (status === "error") {
    return "Notification preferences API unavailable";
  }

  return "Demo watched and muted resources shown until API is configured";
}

function formatRelativeNotificationTime(value: string, locale: WorkspaceNotificationLocale) {
  return new Intl.DateTimeFormat(locale, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function normalizeResourceType(resourceType?: string | null) {
  return resourceType?.trim().toLowerCase() || "workspace";
}

function toNotificationActor(notification: PermissionNotificationDto): WorkspaceNotification["actor"] {
  const actorName = notification.actor?.displayName?.trim();
  if (actorName) {
    return {
      avatarTone: "blue",
      initials: actorName.slice(0, 1).toUpperCase(),
      name: actorName,
    };
  }

  if (!notification.actorUserId?.trim()) {
    return undefined;
  }

  return {
    avatarTone: "blue",
    initials: "U",
    name: `User ${shortId(notification.actorUserId)}`,
  };
}

function getNotificationActionText(type: string, locale: WorkspaceNotificationLocale) {
  switch (type.toLowerCase()) {
    case "access_request.created":
      return localText(locale, "requested access", "请求访问");
    case "access_request.approved":
      return localText(locale, "approved access", "批准了访问请求");
    case "access_request.denied":
      return localText(locale, "denied access", "拒绝了访问请求");
    case "permission.grant_created":
      return localText(locale, "created a grant", "创建了授权");
    case "permission.grant_updated":
      return localText(locale, "updated a grant", "更新了授权");
    case "permission.grant_revoked":
      return localText(locale, "revoked a grant", "撤销了授权");
    case "permission.grant_expiring":
      return localText(locale, "has an expiring grant", "有即将过期的授权");
    case "permission.grant_expired":
      return localText(locale, "has an expired grant", "有已过期的授权");
    case "group.member_added":
      return localText(locale, "added a group member", "添加了组成员");
    case "group.member_removed":
      return localText(locale, "removed a group member", "移除了组成员");
    case "group.member_expiring":
      return localText(locale, "has expiring group access", "有即将过期的组访问");
    case "group.member_expired":
      return localText(locale, "has expired group access", "有已过期的组访问");
    case "share_link.created":
      return localText(locale, "created a share link", "创建了分享链接");
    case "share_link.revoked":
      return localText(locale, "revoked a share link", "撤销了分享链接");
    case "email_invite.created":
      return localText(locale, "created an email invite", "创建了邮件邀请");
    case "email_invite.accepted":
      return localText(locale, "accepted an email invite", "接受了邮件邀请");
    case "email_invite.revoked":
      return localText(locale, "revoked an email invite", "撤销了邮件邀请");
    case "email_invite.delivery_failed":
      return localText(locale, "has a failed email invite", "发送的邮件邀请失败");
    default:
      return localText(locale, "updated access or sharing", "更新了访问与分享");
  }
}

function getNotificationActionLabel(notification: PermissionNotificationDto): WorkspaceNotification["actionLabel"] {
  const label = notification.action?.label?.trim();
  if (label === "Manage" || label === "Open" || label === "Review" || label === "View") {
    return label;
  }

  if (
    notification.type.toLowerCase() === "access_request.created" ||
    getNotificationKind(notification.type, notification.category, notification.state) === "expiry" ||
    isFailedNotification(notification)
  ) {
    return "Review";
  }

  if (notification.type.toLowerCase().startsWith("share_link.") || notification.type.toLowerCase().startsWith("email_invite.")) {
    return "Manage";
  }

  return "Open";
}

function formatNotificationDetail(
  body?: string | null,
  resourcePath?: string | null,
  locale: WorkspaceNotificationLocale = "en-US",
) {
  if (resourcePath) {
    return localText(locale, `Location: ${resourcePath}.`, `位置：${resourcePath}。`);
  }

  return body?.trim() || undefined;
}

function formatNotificationMessageSuffix(
  _target?: string | null,
  _resourcePath?: string | null,
  _locale: WorkspaceNotificationLocale = "en-US",
) {
  return undefined;
}

function isFailedNotification(notification: PermissionNotificationDto) {
  return notification.state?.trim().toLowerCase() === "failed" ||
    notification.type.toLowerCase() === "email_invite.delivery_failed";
}

function formatNotificationTarget(resourceType?: string | null, resourceId?: string | null) {
  const normalizedType = normalizeResourceType(resourceType);
  if (!resourceId?.trim()) {
    return normalizedType === "workspace" ? "Workspace" : formatResourceType(normalizedType);
  }

  return `${formatResourceType(normalizedType)} ${shortId(resourceId)}`;
}

function isGenericPermissionActionUrl(actionUrl: string) {
  const { route } = parseHashRoute(actionUrl);
  return route === "#permissions" ||
    route === "#share" ||
    route === "#permission-admin" ||
    route === "#members" ||
    route === "#workspace-members" ||
    route === "#workspace-groups" ||
    route === "#groups";
}

function createPreferenceResourceHref(resourceType: string, resourceId?: string | null) {
  if (resourceType === "document" && resourceId && isUuid(resourceId)) {
    return createEditorHash(resourceId);
  }

  if (resourceType === "collection" && resourceId && isUuid(resourceId)) {
    return createLibrariesHash({ collectionId: resourceId });
  }

  return createWorkspaceUpdatesHash();
}

function isWorkspaceUpdatesTab(value: string | null): value is WorkspaceUpdatesTab {
  return value === "all" ||
    value === "unread" ||
    value === "access" ||
    value === "grants" ||
    value === "links" ||
    value === "invites" ||
    value === "sharing" ||
    value === "failed" ||
    value === "expiry";
}

function formatPreferenceResourceLabel(resourceType: string, resourceId?: string | null) {
  const suffix = resourceId && isUuid(resourceId) ? shortId(resourceId) : "workspace";
  return `${formatResourceType(resourceType)} ${suffix}`;
}

function formatResourceType(resourceType: string) {
  if (resourceType === "collection") {
    return "Folder";
  }

  return resourceType
    .split(/[_-]/)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function getNotificationBadgeLabel(type: string, kind: NotificationKind, locale: WorkspaceNotificationLocale) {
  const normalized = type.toLowerCase();
  if (normalized.startsWith("share_link.")) {
    return localText(locale, "Link", "链接");
  }

  if (normalized.startsWith("email_invite.")) {
    return localText(locale, "Invite", "邀请");
  }

  if (kind === "access") {
    return localText(locale, "Access", "访问");
  }

  if (kind === "grant") {
    return localText(locale, "Grant", "授权");
  }

  if (kind === "failed") {
    return localText(locale, "Failed", "失败");
  }

  if (kind === "expiry") {
    return localText(locale, "Expiry", "过期");
  }

  return localText(locale, "Sharing", "分享");
}

function localText(locale: WorkspaceNotificationLocale, en: string, zh: string) {
  return locale === "zh-CN" ? zh : en;
}

function shortId(value: string) {
  return value.length > 12 ? `${value.slice(0, 8)}...${value.slice(-4)}` : value;
}
