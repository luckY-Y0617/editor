import type { PermissionNotificationDto, PermissionNotificationPreferenceDto } from "./appApi";
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
export type WorkspaceUpdatesTab = "access" | "all" | "expiry" | "grants" | "sharing" | "unread";

export type NotificationPreferenceResourceRow = {
  href: string;
  id: string;
  label: string;
  resourceType: string;
  state: "muted" | "watched";
  updatedAt: string;
};

export function toWorkspaceNotification(notification: PermissionNotificationDto): WorkspaceNotification {
  const kind = getNotificationKind(notification.type);
  const target = formatNotificationTarget(notification.resourceType, notification.resourceId);

  return {
    actionHref: getNotificationActionHref(notification),
    actionLabel: notification.type === "access_request.created" ? "Review" : "Open",
    actor: toNotificationActor(notification.actorUserId),
    detail: formatNotificationDetail(notification.body, target),
    id: notification.id,
    kind,
    messagePrefix: getNotificationActionText(notification.type),
    messageSuffix: target ? `on ${target}` : undefined,
    subject: notification.title || target || "Access and sharing notification",
    time: formatRelativeNotificationTime(notification.createdAt),
    unread: notification.readAt === null,
  };
}

export function getNotificationKind(type: string): NotificationKind {
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

    const kind = getNotificationKind(notification.type);
    if (tab === "access") {
      return kind === "access";
    }

    if (tab === "grants") {
      return kind === "grant";
    }

    if (tab === "sharing") {
      return kind === "sharing";
    }

    return kind === "expiry";
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
    case "expiry":
      return "Expiry";
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

  if (notification.resourceType === "document" && notification.resourceId && isUuid(notification.resourceId)) {
    if (getNotificationKind(notification.type) !== "permission") {
      return createDocumentAdvancedPermissionsHash(notification.resourceId);
    }

    return createEditorHash(notification.resourceId);
  }

  if (notification.resourceType === "collection" && notification.resourceId && isUuid(notification.resourceId)) {
    return createLibrariesHash({ collectionId: notification.resourceId });
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
      const resourceType = normalizeResourceType(preference.resourceType);
      const href = createPreferenceResourceHref(resourceType, preference.resourceId);
      const state = preference.muted ? "muted" : "watched";
      return {
        href,
        id: preference.id,
        label: formatPreferenceResourceLabel(resourceType, preference.resourceId),
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

function formatRelativeNotificationTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function normalizeResourceType(resourceType?: string | null) {
  return resourceType?.trim().toLowerCase() || "workspace";
}

function toNotificationActor(actorUserId?: string | null): WorkspaceNotification["actor"] {
  if (!actorUserId?.trim()) {
    return undefined;
  }

  return {
    avatarTone: "blue",
    initials: "U",
    name: `User ${shortId(actorUserId)}`,
  };
}

function getNotificationActionText(type: string) {
  switch (type.toLowerCase()) {
    case "access_request.created":
      return "requested access";
    case "access_request.approved":
      return "approved access";
    case "access_request.denied":
      return "denied access";
    case "permission.grant_created":
      return "created a grant";
    case "permission.grant_updated":
      return "updated a grant";
    case "permission.grant_revoked":
      return "revoked a grant";
    case "permission.grant_expiring":
      return "has an expiring grant";
    case "permission.grant_expired":
      return "has an expired grant";
    case "group.member_added":
      return "added a group member";
    case "group.member_removed":
      return "removed a group member";
    case "group.member_expiring":
      return "has expiring group access";
    case "group.member_expired":
      return "has expired group access";
    case "share_link.created":
      return "created a share link";
    case "share_link.revoked":
      return "revoked a share link";
    case "email_invite.created":
      return "created an email invite";
    case "email_invite.accepted":
      return "accepted an email invite";
    case "email_invite.revoked":
      return "revoked an email invite";
    case "email_invite.delivery_failed":
      return "has a failed email invite";
    default:
      return "updated access or sharing";
  }
}

function formatNotificationDetail(body?: string | null, target?: string | null) {
  const parts = [body?.trim(), target ? `Target: ${target}.` : null].filter(Boolean);
  return parts.length > 0 ? parts.join(" ") : undefined;
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
    value === "sharing" ||
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

function shortId(value: string) {
  return value.length > 12 ? `${value.slice(0, 8)}...${value.slice(-4)}` : value;
}
