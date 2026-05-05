export type NotificationKind = "mention" | "comment" | "document" | "permission" | "system" | "version";

export type WorkspaceNotification = {
  id: string;
  kind: NotificationKind;
  unread: boolean;
  actor?: {
    name: string;
    initials: string;
    avatarTone: "green" | "blue" | "sand" | "mauve";
  };
  messagePrefix?: string;
  subject: string;
  messageSuffix?: string;
  versionBadge?: string;
  detail?: string;
  time: string;
  actionLabel?: "Open" | "Reply" | "Review" | "View";
  actionHref?: string;
};

export type NotificationGroup = {
  id: string;
  label: string;
  notifications: WorkspaceNotification[];
};

export type NotificationPreference = {
  id: string;
  label: string;
  enabled: boolean;
};

export type WatchedDocument = {
  id: string;
  title: string;
};

export type MutedCollection = {
  id: string;
  title: string;
};

export const notificationGroups: NotificationGroup[] = [
  {
    id: "today",
    label: "Today",
    notifications: [
      {
        id: "mention-mission",
        kind: "mention",
        unread: true,
        actor: { name: "Alice Kim", initials: "AK", avatarTone: "green" },
        messagePrefix: "mentioned you in",
        subject: "Mission & Vision",
        time: "10 min ago",
        actionLabel: "Open",
      },
      {
        id: "comment-decision",
        kind: "comment",
        unread: true,
        actor: { name: "Ben Parker", initials: "BP", avatarTone: "blue" },
        messagePrefix: "replied to your comment in",
        subject: "Decision Framework",
        time: "2h ago",
        actionLabel: "Reply",
      },
      {
        id: "document-operating",
        kind: "document",
        unread: true,
        actor: { name: "Maria Santos", initials: "MS", avatarTone: "green" },
        messagePrefix: "published v4.3 of",
        subject: "Operating System",
        versionBadge: "v4.3",
        time: "3h ago",
        actionLabel: "Review",
      },
      {
        id: "permission-strategy",
        kind: "permission",
        unread: true,
        actor: { name: "James Chen", initials: "JC", avatarTone: "sand" },
        messagePrefix: "requested access to",
        subject: "Strategy Overview",
        time: "5h ago",
        actionLabel: "Review",
      },
    ],
  },
  {
    id: "yesterday",
    label: "Yesterday",
    notifications: [
      {
        id: "system-security",
        kind: "system",
        unread: false,
        subject: "Workspace security settings were updated",
        detail: "Multi-factor authentication is now required for all members",
        time: "Yesterday at 16:20",
        actionLabel: "View",
      },
      {
        id: "version-restore",
        kind: "version",
        unread: true,
        actor: { name: "Taylor Adams", initials: "TA", avatarTone: "blue" },
        messagePrefix: "restored version 3.2 of",
        subject: "Mission & Vision",
        versionBadge: "v3.2",
        time: "Yesterday at 11:08",
        actionLabel: "Open",
      },
      {
        id: "comment-communication",
        kind: "comment",
        unread: true,
        subject: "Communication Guide",
        messagePrefix: "A new comment was added to",
        time: "Yesterday at 09:42",
        actionLabel: "Open",
      },
    ],
  },
  {
    id: "earlier-week",
    label: "Earlier this week",
    notifications: [
      {
        id: "permissions-q2",
        kind: "permission",
        unread: false,
        actor: { name: "Alice Kim", initials: "AK", avatarTone: "green" },
        messagePrefix: "Permissions changed for",
        subject: "Q2 Planning Playbook",
        detail: "Editors: 5 added · Viewers: 3 added",
        time: "Mon at 14:55",
        actionLabel: "Review",
      },
      {
        id: "system-maintenance",
        kind: "system",
        unread: false,
        subject: "System maintenance completed",
        detail: "All services are operating normally",
        time: "Mon at 08:17",
      },
      {
        id: "document-decision",
        kind: "document",
        unread: true,
        actor: { name: "Ben Parker", initials: "BP", avatarTone: "blue" },
        messagePrefix: "published v2.1 of",
        subject: "Decision Framework",
        versionBadge: "v2.1",
        time: "Mon at 07:46",
        actionLabel: "Open",
      },
    ],
  },
];

export const notificationPreferences: NotificationPreference[] = [
  { id: "mentions", label: "Mentions", enabled: true },
  { id: "comments", label: "Comments", enabled: true },
  { id: "document-changes", label: "Document changes", enabled: true },
  { id: "access-requests", label: "Access requests", enabled: true },
  { id: "system-alerts", label: "System alerts", enabled: true },
];

export const watchedDocuments: WatchedDocument[] = [
  { id: "mission-vision", title: "Mission & Vision" },
  { id: "decision-framework", title: "Decision Framework" },
  { id: "operating-system", title: "Operating System" },
  { id: "strategy-overview", title: "Strategy Overview" },
];

export const mutedCollections: MutedCollection[] = [
  { id: "archives", title: "Archives" },
  { id: "legacy-processes", title: "Legacy Processes" },
];
