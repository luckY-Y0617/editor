export type DashboardPerson = {
  id: string;
  name: string;
  initials: string;
  documentCount: number;
};

export type DashboardInsight = {
  id: string;
  label: string;
  value: string;
  change: string;
  tone: "positive" | "neutral";
};

export type DashboardUpdate = {
  id: string;
  title: string;
  date: string;
  kind: "template" | "map" | "import";
};

export type DashboardActivity = {
  id: string;
  actorId: string;
  action: string;
  target: string;
  date: string;
  kind: "edited" | "published" | "commented" | "created" | "moved";
};

export type ActiveDocumentRow = {
  id: string;
  title: string;
  ownerId: string;
  status: "In Progress" | "Review" | "Not Started";
  progress: number;
  updatedAt: string;
};

export type CollectionSpotlight = {
  id: string;
  displayTitle: string;
  documentCount: number;
};

export const dashboardPeople: DashboardPerson[] = [
  { id: "alice", name: "Alice Kim", initials: "AK", documentCount: 24 },
  { id: "ben", name: "Ben Parker", initials: "BP", documentCount: 18 },
  { id: "maria", name: "Maria Santos", initials: "MS", documentCount: 16 },
  { id: "david", name: "David Chen", initials: "DC", documentCount: 12 },
  { id: "sarah", name: "Sarah Johnson", initials: "SJ", documentCount: 9 },
];

export const documentOwnerIdsByDocumentId: Record<string, DashboardPerson["id"]> = {
  "doc-editor-experience": "alice",
  "doc-writing-flow": "alice",
  "doc-block-principles": "ben",
  "doc-glossary": "maria",
  "doc-yuque-observation": "ben",
  "doc-communication-guide": "sarah",
};

export const dashboardInsights: DashboardInsight[] = [
  { id: "created", label: "Documents created", value: "12", change: "33%", tone: "positive" },
  { id: "updated", label: "Documents updated", value: "28", change: "17%", tone: "positive" },
  { id: "contributions", label: "Team contributions", value: "96", change: "24%", tone: "positive" },
];

export const workspaceUpdates: DashboardUpdate[] = [
  { id: "templates", title: "New templates added", date: "May 14, 2024", kind: "template" },
  { id: "maps", title: "Maps improvements", date: "May 10, 2024", kind: "map" },
  { id: "import", title: "Import from Confluence", date: "May 8, 2024", kind: "import" },
];

export const dashboardActivity: DashboardActivity[] = [
  {
    id: "activity-mission",
    actorId: "alice",
    action: "updated",
    target: "Mission & Vision",
    date: "Today, 1:48 PM",
    kind: "edited",
  },
  {
    id: "activity-decision",
    actorId: "ben",
    action: "published",
    target: "Decision Framework",
    date: "May 14, 2024",
    kind: "published",
  },
  {
    id: "activity-operating",
    actorId: "maria",
    action: "commented on",
    target: "Operating System",
    date: "May 12, 2024",
    kind: "commented",
  },
  {
    id: "activity-glossary",
    actorId: "david",
    action: "created",
    target: "Glossary",
    date: "May 10, 2024",
    kind: "created",
  },
  {
    id: "activity-guide",
    actorId: "sarah",
    action: "moved",
    target: "Communication Guide",
    date: "May 8, 2024",
    kind: "moved",
  },
];

export const activeDocumentRows: ActiveDocumentRow[] = [
  {
    id: "doc-writing-flow",
    title: "Mission & Vision",
    ownerId: "alice",
    status: "In Progress",
    progress: 65,
    updatedAt: "Today",
  },
  {
    id: "doc-yuque-observation",
    title: "Decision Framework",
    ownerId: "ben",
    status: "In Progress",
    progress: 40,
    updatedAt: "May 14",
  },
  {
    id: "doc-block-principles",
    title: "Operating System",
    ownerId: "maria",
    status: "Review",
    progress: 70,
    updatedAt: "May 12",
  },
  {
    id: "doc-glossary",
    title: "Glossary",
    ownerId: "david",
    status: "In Progress",
    progress: 30,
    updatedAt: "May 10",
  },
  {
    id: "doc-communication-guide",
    title: "Communication Guide",
    ownerId: "sarah",
    status: "Not Started",
    progress: 0,
    updatedAt: "May 8",
  },
];

export const pinnedCollectionIds = ["product", "research", "guides", "reference", "workstreams"];

export const collectionSpotlightCounts: Record<string, number> = {
  product: 24,
  research: 18,
  guides: 37,
  reference: 12,
  workstreams: 21,
};

export function getDashboardPerson(personId: string) {
  return dashboardPeople.find((person) => person.id === personId) ?? dashboardPeople[0];
}
