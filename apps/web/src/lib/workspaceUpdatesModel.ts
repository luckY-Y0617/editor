import type { PermissionNotificationDto, PermissionNotificationPreferenceDto } from "./appApi";
import { isUuid } from "./apiClient";
import { createEditorHash, createLibrariesHash, normalizeInternalActionHash } from "./hashRouting";
import type { NotificationKind, WorkspaceNotification } from "../data/workspaceUpdatesData";

export type NotificationApiStatus = "unconfigured" | "loading" | "ready" | "forbidden" | "error";
export type WorkspaceUpdatesTab = "all" | "unread" | "comments" | "mentions" | "access" | "documents" | "general";

export type NotificationPreferenceResourceRow = {
  href: string;
  id: string;
  label: string;
  resourceType: string;
  state: "muted" | "watched";
  updatedAt: string;
};

export function toWorkspaceNotification(notification: PermissionNotificationDto): WorkspaceNotification {
  return {
    actionHref: getNotificationActionHref(notification),
    actionLabel: notification.type === "access_request.created" ? "Review" : "Open",
    detail: notification.body ?? undefined,
    id: notification.id,
    kind: getNotificationKind(notification.type),
    subject: notification.title,
    time: formatRelativeNotificationTime(notification.createdAt),
    unread: notification.readAt === null,
  };
}

export function getNotificationKind(type: string): NotificationKind {
  const normalized = type.toLowerCase();
  if (normalized.includes("mention")) {
    return "mention";
  }

  if (normalized.includes("comment")) {
    return "comment";
  }

  if (normalized.includes("document") || normalized.includes("version")) {
    return normalized.includes("version") ? "version" : "document";
  }

  if (
    normalized.startsWith("access_request.") ||
    normalized.startsWith("permission.") ||
    normalized.startsWith("group.") ||
    normalized.startsWith("share_link.") ||
    normalized.startsWith("email_invite.")
  ) {
    return "permission";
  }

  return "system";
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
    if (tab === "comments") {
      return kind === "comment";
    }

    if (tab === "mentions") {
      return kind === "mention";
    }

    if (tab === "access") {
      return kind === "permission";
    }

    if (tab === "documents") {
      return kind === "document" || kind === "version";
    }

    return kind === "system";
  });
}

export function getWorkspaceUpdatesTabLabel(tab: WorkspaceUpdatesTab) {
  switch (tab) {
    case "unread":
      return "Unread";
    case "comments":
      return "Comments";
    case "mentions":
      return "Mentions";
    case "access":
      return "Access / approvals";
    case "documents":
      return "Document changes";
    case "general":
      return "General";
    case "all":
    default:
      return "All";
  }
}

export function getNotificationActionHref(notification: PermissionNotificationDto) {
  const normalizedActionUrl = normalizeInternalActionHash(notification.actionUrl);
  if (notification.actionUrl && normalizedActionUrl !== "#updates") {
    return normalizedActionUrl;
  }

  if (notification.resourceType === "document" && notification.resourceId && isUuid(notification.resourceId)) {
    return createEditorHash(notification.resourceId);
  }

  if (notification.resourceType === "collection" && notification.resourceId && isUuid(notification.resourceId)) {
    return createLibrariesHash({ collectionId: notification.resourceId });
  }

  if (getNotificationKind(notification.type) === "permission") {
    return "#permissions";
  }

  return "#updates";
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
    return "Live workspace notifications";
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

  return "Demo notification data shown until API is configured";
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

  return "Demo preference data shown until API is configured";
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

function createPreferenceResourceHref(resourceType: string, resourceId?: string | null) {
  if (resourceType === "document" && resourceId && isUuid(resourceId)) {
    return createEditorHash(resourceId);
  }

  if (resourceType === "collection" && resourceId && isUuid(resourceId)) {
    return createLibrariesHash({ collectionId: resourceId });
  }

  return "#updates";
}

function formatPreferenceResourceLabel(resourceType: string, resourceId?: string | null) {
  const suffix = resourceId && isUuid(resourceId) ? shortId(resourceId) : "workspace";
  return `${formatResourceType(resourceType)} ${suffix}`;
}

function formatResourceType(resourceType: string) {
  return resourceType
    .split(/[_-]/)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function shortId(value: string) {
  return value.length > 12 ? `${value.slice(0, 8)}...${value.slice(-4)}` : value;
}
