export type WorkspaceRole = "owner" | "admin" | "editor" | "viewer";

export type EffectiveDocumentAccess = "Can manage" | "Can edit" | "Can view";

export type ShareDocumentContext = {
  title: string;
  location: string;
  status: "Published" | "Draft";
  owner: {
    name: string;
    initials: string;
  };
  updatedAt: string;
  version: string;
  readers: string;
  tags: string[];
};

export type WorkspaceAccessMember = {
  id: string;
  name: string;
  initials: string;
  email: string;
  role: WorkspaceRole;
  effectiveAccess: EffectiveDocumentAccess;
  addedAt: string;
  avatarTone: "green" | "blue" | "sand" | "mauve";
};

export type PermissionRoleSummary = {
  id: WorkspaceRole;
  label: string;
  description: string;
  access: EffectiveDocumentAccess;
};

export type RecentAccessEvent = {
  id: string;
  actor: string;
  initials: string;
  action: "viewed" | "commented" | "edited" | "downloaded";
  happenedAt: string;
  avatarTone: "green" | "blue" | "sand" | "mauve";
};

export type ShareLinkState = {
  label: string;
  url: string;
  audience: string;
  roleLabel: string;
  capability: "current-backed" | "deferred";
};

export const shareDocumentContext: ShareDocumentContext = {
  title: "Mission & Vision",
  location: "Atlas Library / 01. Foundations",
  status: "Published",
  owner: {
    name: "Alice Kim",
    initials: "AK",
  },
  updatedAt: "May 14, 2024 · 13:48",
  version: "3.2",
  readers: "12 collaborators",
  tags: ["Strategy", "Vision", "Framework"],
};

export const workspaceAccessMembers: WorkspaceAccessMember[] = [
  {
    id: "alice-kim",
    name: "Alice Kim",
    initials: "AK",
    email: "alice.kim@northstar.com",
    role: "owner",
    effectiveAccess: "Can manage",
    addedAt: "May 14, 2024",
    avatarTone: "green",
  },
  {
    id: "ben-parker",
    name: "Ben Parker",
    initials: "BP",
    email: "ben.parker@northstar.com",
    role: "viewer",
    effectiveAccess: "Can view",
    addedAt: "Apr 10, 2024",
    avatarTone: "blue",
  },
  {
    id: "maria-santos",
    name: "Maria Santos",
    initials: "MS",
    email: "maria.santos@northstar.com",
    role: "editor",
    effectiveAccess: "Can edit",
    addedAt: "Mar 12, 2024",
    avatarTone: "sand",
  },
  {
    id: "james-chen",
    name: "James Chen",
    initials: "JC",
    email: "james.chen@northstar.com",
    role: "viewer",
    effectiveAccess: "Can view",
    addedAt: "Feb 28, 2024",
    avatarTone: "mauve",
  },
  {
    id: "taylor-adams",
    name: "Taylor Adams",
    initials: "TA",
    email: "taylor.adams@northstar.com",
    role: "viewer",
    effectiveAccess: "Can view",
    addedAt: "Jan 22, 2024",
    avatarTone: "blue",
  },
];

export const permissionRoleSummaries: PermissionRoleSummary[] = [
  {
    id: "owner",
    label: "Owner",
    description: "Can manage workspace ownership, members, documents, and protected settings.",
    access: "Can manage",
  },
  {
    id: "admin",
    label: "Admin",
    description: "Can manage members and workspace content without transferring ownership.",
    access: "Can manage",
  },
  {
    id: "editor",
    label: "Editor",
    description: "Can create, update, and move documents in the workspace.",
    access: "Can edit",
  },
  {
    id: "viewer",
    label: "Viewer",
    description: "Can read workspace maps, documents, context, activity, and search results.",
    access: "Can view",
  },
];

export const recentAccessEvents: RecentAccessEvent[] = [
  {
    id: "access-ben-viewed",
    actor: "Ben Parker",
    initials: "BP",
    action: "viewed",
    happenedAt: "2h ago",
    avatarTone: "blue",
  },
  {
    id: "access-maria-commented",
    actor: "Maria Santos",
    initials: "MS",
    action: "commented",
    happenedAt: "Yesterday",
    avatarTone: "sand",
  },
  {
    id: "access-alice-edited",
    actor: "Alice Kim",
    initials: "AK",
    action: "edited",
    happenedAt: "May 14",
    avatarTone: "green",
  },
  {
    id: "access-james-downloaded",
    actor: "James Chen",
    initials: "JC",
    action: "downloaded",
    happenedAt: "May 10",
    avatarTone: "mauve",
  },
  {
    id: "access-taylor-viewed",
    actor: "Taylor Adams",
    initials: "TA",
    action: "viewed",
    happenedAt: "May 8",
    avatarTone: "blue",
  },
];

export const shareLinkState: ShareLinkState = {
  label: "Document Link",
  url: "/documents/mission-vision",
  audience: "Disabled",
  roleLabel: "Deferred",
  capability: "deferred",
};
