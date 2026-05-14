import { initialKnowledgeDocuments, knowledgeFolders } from "../data/knowledgeDocuments";
import {
  dashboardActivity,
  dashboardPeople,
  workspaceUpdates,
  collectionSpotlightCounts,
  pinnedCollectionIds,
  type CollectionSpotlight,
} from "../data/workspaceHomeData";
import { isUuid } from "./apiClient";
import type {
  ActivityTimelineItemDto,
  BootstrapResponse,
  KnowledgeDocumentSummaryDto,
  KnowledgeFolderDto,
  PermissionNotificationDto,
  WorkspaceAgendaItemDto,
  WorkspaceAgendaResponse,
  WorkspaceMemberDto,
} from "./appApi";
import {
  createEditorHash,
  createLibrariesHash,
  createWorkspaceMembersHash,
  createWorkspaceUpdatesHash,
  normalizeInternalActionHash,
} from "./hashRouting";

export type HomeActivityRow = {
  actorName?: string;
  date: string;
  detail: string;
  href: string;
  id: string;
  source: HomeDataSource;
  title: string;
};

export type HomeAgendaRow = {
  calendarStatus?: string;
  connectedToCalendar?: boolean;
  date?: string;
  detail: string;
  durationMinutes?: number;
  endTime?: string | null;
  href?: string;
  id: string;
  kind?: string;
  meta: string;
  source: HomeDataSource;
  startTime?: string;
  time: string;
  title: string;
};

export type HomeAttentionRow = {
  detail: string;
  href: string;
  id: string;
  kind: "approval" | "access" | "decision" | "notification";
  source: HomeDataSource;
  title: string;
};

export type HomeConversationRow = {
  badge?: string;
  detail: string;
  href: string;
  id: string;
  kind: "conversation" | "decision" | "activity" | "notification";
  source: HomeDataSource;
  title: string;
  tone?: "green" | "orange";
};

export type HomeContributorRow = {
  contributionLabel: string;
  href: string;
  id: string;
  initials: string;
  name: string;
  role: string;
  source: HomeDataSource;
  status: string;
};

export type HomeCollectionRow = CollectionSpotlight & {
  href: string;
};

export type HomeDocumentRow = {
  folderTitle: string;
  href: string;
  id: string;
  source: HomeDataSource;
  status: string;
  title: string;
  updatedAt: string;
};

export type HomeSignalRow = {
  detail: string;
  href: string;
  id: string;
  label: string;
  source: HomeDataSource;
  value: string;
};

export type HomeDigestRow = HomeSignalRow;

export type HomeDataSource = "demo" | "empty" | "live";

export type HomeInsightRow = {
  id: string;
  label: string;
  value: string;
};

export type HomeUpdateRow = {
  body: string;
  date: string;
  href: string;
  id: string;
  source: HomeDataSource;
  title: string;
  type: string;
};

export type HomeQuickActionRow = {
  disabledReason?: string;
  href?: string;
  id: "new-document" | "new-decision" | "request-access" | "share-update" | "log-note" | "more-actions";
  isEnabled: boolean;
  label: string;
};

export type WorkspaceHomeSupplementalData = {
  activityItems?: ActivityTimelineItemDto[];
  agenda?: WorkspaceAgendaResponse;
  members?: WorkspaceMemberDto[];
  notifications?: PermissionNotificationDto[];
};

export type WorkspaceHomeModel = {
  activeDocumentHref: string;
  activeLibraryHref: string;
  activeLibraryName: string;
  activityRows: HomeActivityRow[];
  agendaRows: HomeAgendaRow[];
  collectionsLabel: string;
  collections: HomeCollectionRow[];
  conversationRows: HomeConversationRow[];
  contributorRows: HomeContributorRow[];
  digestRows: HomeDigestRow[];
  documentRows: HomeDocumentRow[];
  documentsLabel: string;
  foldersLabel: string;
  insightRows: HomeInsightRow[];
  librariesLabel: string;
  mode: "demo" | "live";
  recentDecisionRows: HomeConversationRow[];
  signalRows: HomeSignalRow[];
  spacesLabel: string;
  updateRows: HomeUpdateRow[];
  updatesLabel: string;
  waitingRows: HomeAttentionRow[];
  workspaceName: string;
};

export function createHomeQuickActions(model: Pick<WorkspaceHomeModel, "activeLibraryHref" | "mode">): HomeQuickActionRow[] {
  return [
    {
      href: model.activeLibraryHref,
      id: "new-document",
      isEnabled: true,
      label: "New document",
    },
    {
      disabledReason: "Decision workflow is not supported by the current API.",
      id: "new-decision",
      isEnabled: false,
      label: "New decision",
    },
    {
      disabledReason: "Request access requires a target resource.",
      id: "request-access",
      isEnabled: false,
      label: "Request access",
    },
    {
      disabledReason: "Share update workflow is deferred.",
      id: "share-update",
      isEnabled: false,
      label: "Share update",
    },
    {
      disabledReason: "Log note workflow is deferred.",
      id: "log-note",
      isEnabled: false,
      label: "Log a note",
    },
    {
      disabledReason: "Choose Library, Search, Updates, or Settings from the workspace navigation.",
      id: "more-actions",
      isEnabled: false,
      label: "More actions",
    },
  ];
}

export function createLiveWorkspaceHomeModel(
  bootstrap: BootstrapResponse,
  supplementalData: WorkspaceHomeSupplementalData = {},
): WorkspaceHomeModel {
  const folderTitlesById = createFolderTitlesById(bootstrap.folders);
  const activeLibrary = bootstrap.spaces.find((space) => space.id === bootstrap.activeSpaceId)
    ?? bootstrap.spaces.find((space) => space.id === bootstrap.workspace.currentSpaceId)
    ?? bootstrap.spaces[0];
  const documentRows = [...bootstrap.documents]
    .sort(compareDocumentsByUpdatedAtDesc)
    .slice(0, 5)
    .map((document) => toHomeDocumentRow(document, folderTitlesById));
  const notifications = supplementalData.notifications ?? [];
  const activeDocument = bootstrap.documents.find((document) => document.id === bootstrap.activeDocumentId) ?? null;
  const activityRows = aggregateHomeActivityRows(
    (supplementalData.activityItems ?? []).map((item) => toHomeActivityRow(item, activeDocument)),
  ).slice(0, 5);
  const contributorRows = (supplementalData.members ?? []).slice(0, 5).map(toHomeContributorRow);

  return {
    activeDocumentHref: isUuid(bootstrap.activeDocumentId) ? createEditorHash(bootstrap.activeDocumentId) : "#home",
    activeLibraryHref: createLibrariesHash({ libraryId: activeLibrary?.id ?? bootstrap.activeSpaceId }),
    activeLibraryName: activeLibrary?.name ?? "No library",
    activityRows,
    agendaRows: (supplementalData.agenda?.today ?? []).slice(0, 4).map(toHomeAgendaRow),
    collections: [...bootstrap.folders]
      .sort((left, right) => Number(left.sortOrder) - Number(right.sortOrder))
      .slice(0, 7)
      .map((folder) => toCollectionSpotlight(folder, activeLibrary?.id ?? bootstrap.activeSpaceId)),
    collectionsLabel: String(bootstrap.folders.length),
    conversationRows: createLiveConversationRows(activityRows, notifications),
    contributorRows,
    digestRows: createNotificationDigestRows(notifications),
    documentRows,
    documentsLabel: String(bootstrap.documents.length),
    foldersLabel: String(bootstrap.folders.length),
    insightRows: createInsightRows(bootstrap),
    librariesLabel: String(bootstrap.spaces.length),
    mode: "live",
    recentDecisionRows: createRecentDecisionRows(activityRows, notifications),
    signalRows: createSignalRows(bootstrap, notifications),
    spacesLabel: String(bootstrap.spaces.length),
    updateRows: notifications.slice(0, 5).map(toHomeUpdateRow),
    updatesLabel: supplementalData.notifications ? String(supplementalData.notifications.length) : "...",
    waitingRows: createWaitingRows(notifications),
    workspaceName: bootstrap.workspace.name,
  };
}

export function createDemoWorkspaceHomeModel(): WorkspaceHomeModel {
  const folderTitlesById = new Map(knowledgeFolders.map((folder) => [folder.id, stripCollectionPrefix(folder.title)]));

  return {
    activeDocumentHref: "#editor",
    activeLibraryHref: "#libraries",
    activeLibraryName: "Demo Library",
    activityRows: dashboardActivity.slice(0, 5).map((activity) => ({
      actorName: getDemoPersonName(activity.actorId),
      date: activity.date,
      detail: `${getDemoPersonName(activity.actorId)} ${activity.action} ${activity.target}`,
      href: "#editor",
      id: activity.id,
      source: "demo",
      title: activity.target,
    })),
    agendaRows: [
      {
        detail: "Foundations",
        id: "agenda-strategy-sync",
        meta: "6 participants",
        source: "demo",
        time: "9:00 AM",
        title: "Strategy sync",
      },
      {
        detail: "Workstreams",
        id: "agenda-workstream-check-in",
        meta: "4 participants",
        source: "demo",
        time: "11:00 AM",
        title: "Workstream check-in",
      },
      {
        detail: "Strategy - Northstar Office",
        id: "agenda-design-review",
        meta: "5 participants",
        source: "demo",
        time: "1:00 PM",
        title: "Design review",
      },
      {
        detail: "Reference",
        id: "agenda-docs-office-hours",
        meta: "Open",
        source: "demo",
        time: "3:30 PM",
        title: "Docs office hours",
      },
    ],
    collections: pinnedCollectionIds
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
          href: "#editor",
        };
      })
      .filter((collection): collection is HomeCollectionRow => collection !== null),
    documentRows: [...initialKnowledgeDocuments]
      .filter((document) => document.id !== "doc-editor-experience")
      .sort(compareDocumentsByUpdatedAtDesc)
      .slice(0, 5)
      .map((document) => ({
        folderTitle: folderTitlesById.get(document.folderId) ?? "Unfiled",
        href: "#editor",
        id: document.id,
        source: "demo",
        status: "Demo",
        title: document.title,
        updatedAt: document.updatedAt,
      })),
    conversationRows: [
      {
        detail: "Access request awaiting review",
        href: createWorkspaceUpdatesHash({ tab: "access" }),
        id: "conversation-access-request",
        kind: "notification",
        source: "demo",
        title: "Access request",
      },
      {
        detail: "Share link created",
        href: createWorkspaceUpdatesHash({ tab: "sharing" }),
        id: "conversation-share-link",
        kind: "notification",
        source: "demo",
        title: "Sharing link",
      },
      {
        detail: "Grant expires soon",
        href: createWorkspaceUpdatesHash({ tab: "expiry" }),
        id: "conversation-expiry",
        kind: "notification",
        source: "demo",
        title: "Permission expiry",
      },
    ],
    contributorRows: dashboardPeople.slice(0, 3).map((person) => ({
      contributionLabel: `${person.documentCount} contributions`,
      href: createWorkspaceMembersHash(),
      id: person.id,
      initials: person.initials,
      name: person.name,
      role: "contributor",
      source: "demo",
      status: "active",
    })),
    collectionsLabel: String(knowledgeFolders.length),
    digestRows: [
      {
        detail: "Not yet read",
        href: createWorkspaceUpdatesHash(),
        id: "digest-unread",
        label: "unread notifications",
        source: "demo",
        value: "4",
      },
      {
        detail: "Awaiting responses",
        href: createWorkspaceUpdatesHash({ tab: "access" }),
        id: "digest-approvals",
        label: "approvals requested",
        source: "demo",
        value: "3",
      },
      {
        detail: "Sharing links and invites",
        href: createWorkspaceUpdatesHash({ tab: "sharing" }),
        id: "digest-sharing",
        label: "sharing updates",
        source: "demo",
        value: "2",
      },
    ],
    documentsLabel: String(initialKnowledgeDocuments.length),
    foldersLabel: String(knowledgeFolders.length),
    insightRows: [],
    librariesLabel: "Demo",
    mode: "demo",
    recentDecisionRows: [
      {
        badge: "Access",
        detail: "Access request approved",
        href: createWorkspaceUpdatesHash({ tab: "access" }),
        id: "decision-access-approved",
        kind: "notification",
        source: "demo",
        title: "Access approved",
        tone: "green",
      },
      {
        badge: "Sharing",
        detail: "Email invite sent",
        href: createWorkspaceUpdatesHash({ tab: "sharing" }),
        id: "decision-email-invite",
        kind: "notification",
        source: "demo",
        title: "Invite sent",
        tone: "orange",
      },
      {
        badge: "Expiry",
        detail: "Grant expires soon",
        href: createWorkspaceUpdatesHash({ tab: "expiry" }),
        id: "decision-expiry",
        kind: "notification",
        source: "demo",
        title: "Permission expiring",
        tone: "green",
      },
    ],
    signalRows: [
      {
        detail: "drafts need attention",
        href: "#libraries",
        id: "signal-drafts",
        label: "documents",
        source: "demo",
        value: "2",
      },
      {
        detail: "pending",
        href: createWorkspaceUpdatesHash({ tab: "access" }),
        id: "signal-access",
        label: "access requests",
        source: "demo",
        value: "3",
      },
      {
        detail: "requires review",
        href: createWorkspaceUpdatesHash({ tab: "sharing" }),
        id: "signal-share",
        label: "external share",
        source: "demo",
        value: "1",
      },
    ],
    spacesLabel: "Demo",
    updateRows: workspaceUpdates.map((update) => ({
      body: "",
      date: update.date,
      href: createWorkspaceUpdatesHash(),
      id: update.id,
      source: "demo",
      title: update.title,
      type: update.kind,
    })),
    updatesLabel: "Demo",
    waitingRows: [
      {
        detail: "Documents awaiting your review",
        href: createWorkspaceUpdatesHash({ tab: "access" }),
        id: "waiting-approvals",
        kind: "approval",
        source: "demo",
        title: "3 approvals",
      },
      {
        detail: "Needs your response",
        href: createWorkspaceUpdatesHash({ tab: "access" }),
        id: "waiting-access-request",
        kind: "access",
        source: "demo",
        title: "1 access request",
      },
      {
        detail: "Grant expires soon",
        href: createWorkspaceUpdatesHash({ tab: "expiry" }),
        id: "waiting-expiry",
        kind: "notification",
        source: "demo",
        title: "1 expiring grant",
      },
    ],
    workspaceName: "Northstar",
  };
}

export function createEmptyWorkspaceHomeModel(status: "error" | "forbidden" | "loading" | "ready"): WorkspaceHomeModel {
  return {
    activeDocumentHref: "#home",
    activeLibraryHref: "#libraries",
    activeLibraryName: status === "loading" ? "Loading..." : "No library",
    activityRows: [],
    agendaRows: [],
    collections: [],
    collectionsLabel: status === "loading" ? "..." : "0",
    conversationRows: [],
    contributorRows: [],
    digestRows: [],
    documentRows: [],
    documentsLabel: status === "loading" ? "..." : "0",
    foldersLabel: status === "loading" ? "..." : "0",
    insightRows: [],
    librariesLabel: status === "loading" ? "..." : "0",
    mode: "live",
    recentDecisionRows: [],
    signalRows: [],
    spacesLabel: status === "loading" ? "..." : "0",
    updateRows: [],
    updatesLabel: status === "loading" ? "..." : "0",
    waitingRows: [],
    workspaceName: "Northstar",
  };
}

export function aggregateHomeActivityRows(rows: HomeActivityRow[]): HomeActivityRow[] {
  const groupedRows: HomeActivityRow[] = [];
  let index = 0;

  while (index < rows.length) {
    const current = rows[index];
    const groupKey = getHomeActivityGroupKey(current);

    if (!groupKey) {
      groupedRows.push(current);
      index += 1;
      continue;
    }

    const group = [current];
    let nextIndex = index + 1;

    while (nextIndex < rows.length) {
      const next = rows[nextIndex];
      if (getHomeActivityGroupKey(next) !== groupKey || !isWithinActivityGroupWindow(current.date, next.date)) {
        break;
      }

      group.push(next);
      nextIndex += 1;
    }

    groupedRows.push(group.length > 1 ? createGroupedHomeActivityRow(group) : current);
    index = nextIndex;
  }

  return groupedRows;
}

function toHomeActivityRow(item: ActivityTimelineItemDto, activeDocument: KnowledgeDocumentSummaryDto | null): HomeActivityRow {
  const actorName = item.actor?.name?.trim() || undefined;
  const documentId = item.document?.id ?? activeDocument?.id ?? null;
  const documentTitle = item.document?.title?.trim() || activeDocument?.title || "Untitled document";
  const actionLabel = formatActivityAction(item.title, item.detail);

  return {
    actorName,
    date: item.date,
    detail: actorName ? `${actorName} ${actionLabel}.` : `${capitalizeSentence(actionLabel)}.`,
    href: createEditorHash(documentId),
    id: item.id,
    source: "live",
    title: documentTitle,
  };
}

const activityGroupWindowMs = 15 * 60 * 1000;

function createGroupedHomeActivityRow(group: HomeActivityRow[]): HomeActivityRow {
  const latest = group[0];
  const documentTitle = latest.title || "this document";
  const actorName = latest.actorName?.trim();
  const actionVerb = getGroupedActivityVerb(latest.detail);
  const actionText = actorName
    ? `${actorName} ${actionVerb} ${documentTitle}`
    : `${documentTitle} was ${actionVerb}`;

  return {
    ...latest,
    detail: `${actionText} ${group.length} times. ${group.length} updates grouped.`,
    id: `${latest.id}:grouped-${group.length}`,
  };
}

function getHomeActivityGroupKey(row: HomeActivityRow) {
  if (!isGroupableHomeActivityRow(row)) {
    return null;
  }

  return [
    normalizeActivityGroupValue(row.actorName ?? "unknown-user"),
    normalizeActivityGroupValue(row.title),
  ].join("|");
}

function isGroupableHomeActivityRow(row: HomeActivityRow) {
  const detail = row.detail.trim().toLowerCase();

  return (
    detail.includes("updated this document") ||
    detail.includes("updated document content") ||
    detail.includes("edited") ||
    detail.includes("saved draft") ||
    detail.includes("autosave")
  );
}

function getGroupedActivityVerb(detail: string) {
  return detail.toLowerCase().includes("edited") ? "edited" : "updated";
}

function isWithinActivityGroupWindow(left: string, right: string) {
  const leftTime = Date.parse(left);
  const rightTime = Date.parse(right);

  if (!Number.isFinite(leftTime) || !Number.isFinite(rightTime)) {
    return true;
  }

  return Math.abs(leftTime - rightTime) <= activityGroupWindowMs;
}

function normalizeActivityGroupValue(value: string) {
  return value.trim().toLowerCase().replace(/\s+/g, " ");
}

function formatActivityAction(action: string, detail: string) {
  const normalizedAction = action.trim().toLowerCase();
  if (normalizedAction === "document.updated") {
    const normalizedDetail = normalizeActivityDetail(detail, "updated this document");
    return normalizedDetail.startsWith("updated ") ? normalizeUpdatedActivityLabel(normalizedDetail) : "updated this document";
  }

  if (normalizedAction === "document.created") {
    return "created this document";
  }

  if (normalizedAction === "document.moved") {
    return "moved this document";
  }

  if (normalizedAction === "document.archived") {
    return "archived this document";
  }

  if (normalizedAction === "document.restored") {
    return "restored this document";
  }

  if (normalizedAction === "document.imported") {
    return "imported this document";
  }

  return normalizeActivityDetail(detail, humanizeNotificationType(action).toLowerCase());
}

function normalizeActivityDetail(detail: string, fallback: string) {
  const normalized = detail.trim();
  if (!normalized) {
    return fallback;
  }

  const withoutPeriod = normalized.endsWith(".") ? normalized.slice(0, -1) : normalized;
  return withoutPeriod.charAt(0).toLowerCase() + withoutPeriod.slice(1);
}

function normalizeUpdatedActivityLabel(value: string) {
  if (value === "updated content") {
    return "updated document content";
  }

  return value;
}

function capitalizeSentence(value: string) {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function toHomeContributorRow(member: WorkspaceMemberDto): HomeContributorRow {
  return {
    contributionLabel: `${formatRole(member.role)} - ${formatRole(member.status)}`,
    href: createWorkspaceMembersHash(),
    id: member.userId,
    initials: createInitials(member.displayName),
    name: member.displayName,
    role: member.role,
    source: "live",
    status: member.status,
  };
}

function toHomeUpdateRow(notification: PermissionNotificationDto): HomeUpdateRow {
  return {
    body: notification.body ?? "",
    date: notification.createdAt,
    href: normalizeInternalActionHash(notification.actionUrl),
    id: notification.id,
    source: "live",
    title: notification.title,
    type: notification.type,
  };
}

function toHomeAgendaRow(item: WorkspaceAgendaItemDto): HomeAgendaRow {
  return {
    calendarStatus: item.calendarStatus,
    connectedToCalendar: item.connectedToCalendar,
    date: item.date,
    detail: item.detail || formatAgendaRange(item),
    durationMinutes: item.durationMinutes,
    endTime: item.endTime,
    href: createAgendaHref(item),
    id: item.id,
    kind: item.kind,
    meta: item.category,
    source: "live",
    startTime: item.startTime,
    time: item.startTime,
    title: item.title,
  };
}

function createAgendaHref(item: WorkspaceAgendaItemDto) {
  if (item.resourceType === "document" && item.resourceId && isUuid(item.resourceId)) {
    return createEditorHash(item.resourceId);
  }

  if (item.actionUrl) {
    return normalizeInternalActionHash(item.actionUrl);
  }

  return undefined;
}

function formatAgendaRange(item: WorkspaceAgendaItemDto) {
  if (!item.endTime) {
    return item.startTime;
  }

  return `${item.startTime} - ${item.endTime}`;
}

function createWaitingRows(notifications: PermissionNotificationDto[]): HomeAttentionRow[] {
  return notifications
    .filter((notification) => !notification.readAt || isAttentionType(notification.type))
    .slice(0, 3)
    .map((notification) => ({
      detail: notification.body || humanizeNotificationType(notification.type),
      href: normalizeInternalActionHash(notification.actionUrl),
      id: notification.id,
      kind: notification.type.startsWith("access_request") ? "access" : "notification",
      source: "live",
      title: notification.title,
    }));
}

function createLiveConversationRows(
  _activityRows: HomeActivityRow[],
  notifications: PermissionNotificationDto[],
): HomeConversationRow[] {
  const notificationRows = notifications
    .filter((notification) => isAccessSharingOrPermissionNotification(notification.type))
    .slice(0, 3)
    .map((notification) => ({
      detail: notification.body || humanizeNotificationType(notification.type),
      href: normalizeInternalActionHash(notification.actionUrl),
      id: notification.id,
      kind: "notification" as const,
      source: "live" as const,
      title: notification.title,
    }));

  if (notificationRows.length > 0) {
    return notificationRows;
  }

  return [];
}

function createRecentDecisionRows(
  _activityRows: HomeActivityRow[],
  notifications: PermissionNotificationDto[],
): HomeConversationRow[] {
  const notificationRows = notifications
    .filter((notification) => isAccessSharingOrPermissionNotification(notification.type))
    .slice(0, 3)
    .map((notification) => ({
      badge: humanizeNotificationType(notification.type),
      detail: notification.body || formatDate(notification.createdAt),
      href: normalizeInternalActionHash(notification.actionUrl),
      id: notification.id,
      kind: "notification" as const,
      source: "live" as const,
      title: notification.title,
      tone: notification.type.includes("approved") || notification.type.includes("created") ? "green" as const : undefined,
    }));

  if (notificationRows.length > 0) {
    return notificationRows;
  }

  return [];
}

function createSignalRows(bootstrap: BootstrapResponse, notifications: PermissionNotificationDto[]): HomeSignalRow[] {
  const draftCount = bootstrap.documents.filter((document) => normalizeValue(document.status) === "draft").length;
  const unreadCount = notifications.filter((notification) => !notification.readAt).length;
  const accessRequestCount = notifications.filter((notification) => notification.type.startsWith("access_request")).length;

  return [
    {
      detail: "drafts in current workspace",
      href: "#libraries",
      id: "signal-drafts",
      label: "draft documents",
      source: "live",
      value: String(draftCount),
    },
    {
      detail: "unread workspace notifications",
      href: createWorkspaceUpdatesHash(),
      id: "signal-unread",
      label: "notifications",
      source: "live",
      value: String(unreadCount),
    },
    {
      detail: "from permission workflow",
      href: createWorkspaceUpdatesHash({ tab: "access" }),
      id: "signal-access-requests",
      label: "access requests",
      source: "live",
      value: String(accessRequestCount),
    },
  ];
}

function createNotificationDigestRows(notifications: PermissionNotificationDto[]): HomeDigestRow[] {
  const unreadCount = notifications.filter((notification) => !notification.readAt).length;
  const accessRequestCount = notifications.filter((notification) => notification.type.startsWith("access_request")).length;
  const permissionUpdateCount = notifications.filter((notification) => notification.type.startsWith("permission.")).length;

  return [
    {
      detail: "Not yet read",
      href: createWorkspaceUpdatesHash(),
      id: "digest-unread",
      label: "unread notifications",
      source: "live",
      value: String(unreadCount),
    },
    {
      detail: "Permission workflow",
      href: createWorkspaceUpdatesHash({ tab: "access" }),
      id: "digest-access",
      label: "access request updates",
      source: "live",
      value: String(accessRequestCount),
    },
    {
      detail: "Grant changes and expiry notices",
      href: createWorkspaceUpdatesHash({ tab: "grants" }),
      id: "digest-permissions",
      label: "permission updates",
      source: "live",
      value: String(permissionUpdateCount),
    },
  ];
}

function createInsightRows(bootstrap: BootstrapResponse): HomeInsightRow[] {
  const statusCounts = bootstrap.documents.reduce<Record<string, number>>((counts, document) => {
    const key = document.status.trim().toLowerCase() || "unknown";
    counts[key] = (counts[key] ?? 0) + 1;
    return counts;
  }, {});
  const updatedSince = Date.now() - 30 * 24 * 60 * 60 * 1000;
  const recentlyUpdatedCount = bootstrap.documents.filter((document) => {
    const updatedAt = new Date(document.updatedAt).getTime();
    return Number.isFinite(updatedAt) && updatedAt >= updatedSince;
  }).length;

  return [
    {
      id: "total-documents",
      label: "Total documents",
      value: String(bootstrap.documents.length),
    },
    {
      id: "updated-30-days",
      label: "Updated in 30 days",
      value: String(recentlyUpdatedCount),
    },
    {
      id: "draft-documents",
      label: "Draft documents",
      value: String(statusCounts.draft ?? 0),
    },
    {
      id: "published-documents",
      label: "Published documents",
      value: String(statusCounts.published ?? 0),
    },
  ];
}

function toHomeDocumentRow(document: KnowledgeDocumentSummaryDto, folderTitlesById: Map<string, string>): HomeDocumentRow {
  return {
    folderTitle: folderTitlesById.get(document.folderId) ?? "Unfiled",
    href: createEditorHash(document.id),
    id: document.id,
    source: "live",
    status: document.status || "Unknown",
    title: document.title,
    updatedAt: document.updatedAt,
  };
}

function toCollectionSpotlight(folder: KnowledgeFolderDto, libraryId: string): HomeCollectionRow {
  const displayTitle = stripCollectionPrefix(folder.title);

  return {
    id: folder.id,
    displayTitle,
    documentCount: folder.documentCount,
    href: createLibrariesHash({ collectionId: folder.id, libraryId }),
  };
}

function createFolderTitlesById(folders: KnowledgeFolderDto[]) {
  return new Map(folders.map((folder) => [folder.id, stripCollectionPrefix(folder.title)]));
}

function compareDocumentsByUpdatedAtDesc(left: { updatedAt: string }, right: { updatedAt: string }) {
  return new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime();
}

function createInitials(name: string) {
  const parts = name
    .trim()
    .split(/\s+/)
    .filter(Boolean);

  if (parts.length === 0) {
    return "?";
  }

  return parts
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

function getDemoPersonName(personId: string) {
  return dashboardPeople.find((person) => person.id === personId)?.name ?? "A teammate";
}

function isAttentionType(type: string) {
  return type.startsWith("access_request") || type.includes("expiring") || type.includes("expired");
}

function isAccessSharingOrPermissionNotification(type: string) {
  return (
    type.startsWith("access_request.") ||
    type.startsWith("permission.") ||
    type.startsWith("group.") ||
    type.startsWith("share_link.") ||
    type.startsWith("email_invite.")
  );
}

function humanizeNotificationType(type: string) {
  return type
    .replace(/[_.]/g, " ")
    .replace(/\b\w/g, (letter: string) => letter.toUpperCase());
}

function formatRole(value: string) {
  return value ? value[0].toUpperCase() + value.slice(1) : "Member";
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

function normalizeValue(value: string) {
  return value.trim().toLowerCase();
}

function stripCollectionPrefix(title: string) {
  return title.replace(/^\d+\.\s*/, "");
}
