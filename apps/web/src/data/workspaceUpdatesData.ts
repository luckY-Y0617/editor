export type NotificationKind = "access" | "expiry" | "grant" | "permission" | "sharing";

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
  detail?: string;
  time: string;
  actionLabel?: "Open" | "Review" | "View";
  actionHref?: string;
};

export type NotificationGroup = {
  id: string;
  label: string;
  notifications: WorkspaceNotification[];
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
        id: "demo-access-request",
        kind: "access",
        unread: true,
        actor: { name: "User 8a4f...2c11", initials: "U", avatarTone: "blue" },
        messagePrefix: "requested access",
        subject: "Access request pending",
        messageSuffix: "for Document demo-interview",
        detail: "A workspace member requested viewer access. Target: Document demo-interview.",
        time: "10 min ago",
        actionLabel: "Review",
        actionHref: "#settings?scope=workspace&tab=permissions",
      },
      {
        id: "demo-share-link-created",
        kind: "sharing",
        unread: true,
        actor: { name: "User 91c2...7f04", initials: "U", avatarTone: "green" },
        messagePrefix: "created a share link",
        subject: "Share link created",
        messageSuffix: "for Document customer-signals",
        detail: "A share link was created for this resource. Target: Document customer-signals.",
        time: "2h ago",
        actionLabel: "Open",
        actionHref: "#settings?scope=workspace&tab=permissions",
      },
      {
        id: "demo-grant-expiring",
        kind: "expiry",
        unread: true,
        subject: "Permission expiring",
        messagePrefix: "needs review",
        messageSuffix: "for Folder reference",
        detail: "A temporary grant is nearing expiry. Target: Folder reference.",
        time: "4h ago",
        actionLabel: "Review",
        actionHref: "#settings?scope=workspace&tab=permissions",
      },
    ],
  },
  {
    id: "earlier",
    label: "Earlier",
    notifications: [
      {
        id: "demo-email-invite",
        kind: "sharing",
        unread: false,
        actor: { name: "User 52a0...91aa", initials: "U", avatarTone: "sand" },
        messagePrefix: "created an email invite",
        subject: "Email invite created",
        messageSuffix: "for Document onboarding-guide",
        detail: "An email invite was created for this resource. Target: Document onboarding-guide.",
        time: "Yesterday at 16:20",
        actionLabel: "Open",
        actionHref: "#settings?scope=workspace&tab=permissions",
      },
      {
        id: "demo-group-member-added",
        kind: "grant",
        unread: false,
        actor: { name: "User 0367...ac92", initials: "U", avatarTone: "mauve" },
        messagePrefix: "updated group access",
        subject: "Group member added",
        messageSuffix: "in Workspace",
        detail: "A workspace group membership changed. Target: Workspace.",
        time: "Mon at 09:15",
        actionLabel: "Open",
        actionHref: "#settings?scope=workspace&tab=permissions",
      },
    ],
  },
];

export const watchedDocuments: WatchedDocument[] = [
  { id: "mission-vision", title: "Mission & Vision" },
  { id: "decision-framework", title: "Decision Framework" },
  { id: "strategy-overview", title: "Strategy Overview" },
];

export const mutedCollections: MutedCollection[] = [
  { id: "archives", title: "Archives" },
  { id: "legacy-processes", title: "Legacy Processes" },
];
