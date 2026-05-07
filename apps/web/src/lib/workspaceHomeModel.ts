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
  WorkspaceMemberDto,
} from "./appApi";
import { createEditorHash, createLibrariesHash, normalizeInternalActionHash } from "./hashRouting";

export type HomeActivityRow = {
  date: string;
  detail: string;
  href: string;
  id: string;
  source: HomeDataSource;
  title: string;
};

export type HomeAgendaRow = {
  detail: string;
  id: string;
  meta: string;
  source: HomeDataSource;
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
      href: "#updates",
      id: "more-actions",
      isEnabled: true,
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
  const activityRows = (supplementalData.activityItems ?? [])
    .slice(0, 5)
    .map((item) => toHomeActivityRow(item, bootstrap.activeDocumentId));
  const contributorRows = (supplementalData.members ?? []).slice(0, 5).map(toHomeContributorRow);

  return {
    activeDocumentHref: isUuid(bootstrap.activeDocumentId) ? createEditorHash(bootstrap.activeDocumentId) : "#home",
    activeLibraryHref: createLibrariesHash({ libraryId: activeLibrary?.id ?? bootstrap.activeSpaceId }),
    activeLibraryName: activeLibrary?.name ?? "No library",
    activityRows,
    agendaRows: [],
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
        detail: "Design review in 2h",
        href: "#updates",
        id: "conversation-office-plan",
        kind: "conversation",
        source: "demo",
        title: "Office opening plan",
      },
      {
        detail: "3 new comments",
        href: "#updates",
        id: "conversation-hiring-plan",
        kind: "conversation",
        source: "demo",
        title: "Hiring plan v2",
      },
      {
        detail: "Decision requested",
        href: "#updates",
        id: "conversation-roadmap",
        kind: "decision",
        source: "demo",
        title: "Q2 Roadmap",
      },
    ],
    contributorRows: dashboardPeople.slice(0, 3).map((person) => ({
      contributionLabel: `${person.documentCount} contributions`,
      href: "#workspace-members",
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
        detail: "Across 4 documents",
        href: "#updates",
        id: "digest-comments",
        label: "new comments",
        source: "demo",
        value: "7",
      },
      {
        detail: "Awaiting responses",
        href: "#share",
        id: "digest-approvals",
        label: "approvals requested",
        source: "demo",
        value: "3",
      },
      {
        detail: "Across 3 conversations",
        href: "#updates",
        id: "digest-mentions",
        label: "mentions",
        source: "demo",
        value: "5",
      },
    ],
    documentsLabel: String(initialKnowledgeDocuments.length),
    foldersLabel: String(knowledgeFolders.length),
    insightRows: [],
    librariesLabel: "Demo",
    mode: "demo",
    recentDecisionRows: [
      {
        badge: "Decision made",
        detail: "Decided May 4 by Jordan Lee",
        href: "#updates",
        id: "decision-sso",
        kind: "decision",
        source: "demo",
        title: "Adopt new SSO provider",
        tone: "green",
      },
      {
        badge: "In progress",
        detail: "6 participants - Last message 10m ago",
        href: "#updates",
        id: "conversation-office-opening",
        kind: "conversation",
        source: "demo",
        title: "Office opening plan",
        tone: "orange",
      },
      {
        badge: "Decision made",
        detail: "Decided May 3 by Taylor Kim",
        href: "#updates",
        id: "decision-vendor",
        kind: "decision",
        source: "demo",
        title: "Vendor evaluation criteria",
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
        href: "#share",
        id: "signal-access",
        label: "access requests",
        source: "demo",
        value: "3",
      },
      {
        detail: "requires review",
        href: "#updates",
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
      href: "#updates",
      id: update.id,
      source: "demo",
      title: update.title,
      type: update.kind,
    })),
    updatesLabel: "Demo",
    waitingRows: [
      {
        detail: "Documents awaiting your review",
        href: "#share",
        id: "waiting-approvals",
        kind: "approval",
        source: "demo",
        title: "3 approvals",
      },
      {
        detail: "Needs your response",
        href: "#share",
        id: "waiting-access-request",
        kind: "access",
        source: "demo",
        title: "1 access request",
      },
      {
        detail: "Awaiting your input",
        href: "#updates",
        id: "waiting-decision",
        kind: "decision",
        source: "demo",
        title: "1 decision",
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

function toHomeActivityRow(item: ActivityTimelineItemDto, activeDocumentId: string): HomeActivityRow {
  return {
    date: item.date,
    detail: item.detail,
    href: createEditorHash(activeDocumentId),
    id: item.id,
    source: "live",
    title: item.title,
  };
}

function toHomeContributorRow(member: WorkspaceMemberDto): HomeContributorRow {
  return {
    contributionLabel: `${formatRole(member.role)} - ${formatRole(member.status)}`,
    href: "#workspace-members",
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
  activityRows: HomeActivityRow[],
  notifications: PermissionNotificationDto[],
): HomeConversationRow[] {
  const notificationRows = notifications
    .filter((notification) => notification.type.includes("comment") || notification.resourceType === "document")
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

  return activityRows.slice(0, 3).map((activity) => ({
    detail: activity.detail,
    href: activity.href,
    id: activity.id,
    kind: "activity",
    source: "live",
    title: activity.title,
  }));
}

function createRecentDecisionRows(
  activityRows: HomeActivityRow[],
  notifications: PermissionNotificationDto[],
): HomeConversationRow[] {
  const notificationRows = notifications.slice(0, 3).map((notification) => ({
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

  return activityRows.slice(0, 3).map((activity) => ({
    detail: activity.detail,
    href: activity.href,
    id: activity.id,
    kind: "activity",
    source: "live",
    title: activity.title,
  }));
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
      href: "#updates",
      id: "signal-unread",
      label: "notifications",
      source: "live",
      value: String(unreadCount),
    },
    {
      detail: "from permission workflow",
      href: "#share",
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
      href: "#updates",
      id: "digest-unread",
      label: "unread notifications",
      source: "live",
      value: String(unreadCount),
    },
    {
      detail: "Permission workflow",
      href: "#share",
      id: "digest-access",
      label: "access request updates",
      source: "live",
      value: String(accessRequestCount),
    },
    {
      detail: "Grant changes and expiry notices",
      href: "#updates",
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
