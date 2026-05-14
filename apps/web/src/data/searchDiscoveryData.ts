export type SearchFilterOption = {
  id: string;
  label: string;
  count?: number;
  selected: boolean;
};

export type SearchFilterGroup = {
  id: string;
  title: string;
  mode: "checkbox" | "radio";
  options: SearchFilterOption[];
};

export type SearchResultType = "document" | "collection";

export type SearchResultItem = {
  id: string;
  type: SearchResultType;
  title: string;
  section?: string;
  path: string;
  excerpt?: string;
  updatedAt?: string;
  owner?: string;
  status?: "Published" | "Draft";
  collaboratorCount?: number;
  documentCount?: number;
  collectionCount?: number;
  selected?: boolean;
};

export type SearchDetail = {
  title: string;
  code: string;
  status: "Published" | "Draft";
  updatedAt: string;
  path: string;
  summary: string;
  keyElements: string[];
  excerpt: string;
  tags: string[];
  relatedDocuments: Array<{
    code: string;
    title: string;
    updatedAt: string;
  }>;
  details: Array<{
    label: string;
    value: string;
  }>;
};

export const searchFilterGroups: SearchFilterGroup[] = [
  {
    id: "content-type",
    title: "Content Type",
    mode: "checkbox",
    options: [
      { id: "documents", label: "Documents", count: 128, selected: true },
      { id: "collections", label: "Folders", count: 24, selected: false },
    ],
  },
  {
    id: "source",
    title: "Source",
    mode: "checkbox",
    options: [
      { id: "atlas", label: "Atlas Library", count: 98, selected: true },
      { id: "workstreams", label: "Workstreams", count: 46, selected: false },
      { id: "personal", label: "Personal", count: 26, selected: false },
    ],
  },
  {
    id: "status",
    title: "Status",
    mode: "checkbox",
    options: [
      { id: "published", label: "Published", count: 84, selected: true },
      { id: "draft", label: "Draft", count: 28, selected: false },
      { id: "archived", label: "Archived", count: 12, selected: false },
    ],
  },
  {
    id: "date",
    title: "Date Updated",
    mode: "radio",
    options: [
      { id: "any", label: "Any time", selected: true },
      { id: "day", label: "Last 24 hours", count: 6, selected: false },
      { id: "week", label: "Last 7 days", count: 18, selected: false },
      { id: "month", label: "Last 30 days", count: 54, selected: false },
      { id: "custom", label: "Custom range", selected: false },
    ],
  },
  {
    id: "topics",
    title: "Topics",
    mode: "checkbox",
    options: [
      { id: "strategy", label: "Strategy & Planning", count: 68, selected: true },
      { id: "decision", label: "Decision Making", count: 55, selected: true },
      { id: "communication", label: "Communication", count: 32, selected: false },
      { id: "operations", label: "Operations", count: 28, selected: false },
      { id: "leadership", label: "Leadership", count: 21, selected: false },
    ],
  },
];

export const searchResults: SearchResultItem[] = [
  {
    id: "decision-framework",
    type: "document",
    title: "3.1 Decision Framework",
    path: "Atlas Library / 01. Foundations / Mission & Vision",
    excerpt:
      "... Our Decision Framework provides a consistent approach for evaluating options and making aligned choices. It balances impact, effort, and risk ...",
    updatedAt: "Updated Apr 28, 2024",
    owner: "Alice Kim",
    status: "Published",
    selected: true,
  },
  {
    id: "decision-making-principles",
    type: "document",
    title: "Decision Making Principles",
    section: "Decision",
    path: "Atlas Library / 02. Strategy / Strategy Overview",
    excerpt:
      "... These principles guide our Decision Framework and ensure we make choices that create long-term value for our teams and customers ...",
    updatedAt: "Updated May 6, 2024",
    owner: "Ben Parker",
    status: "Published",
  },
  {
    id: "decision-review-checklist",
    type: "document",
    title: "Decision Review Checklist",
    section: "Decision",
    path: "Atlas Library / 04. Guides & Playbooks / Decision Making",
    excerpt:
      "... Use this checklist to validate that a decision aligns with our Decision Framework and records the key context, trade-offs, and outcomes ...",
    updatedAt: "Updated Apr 22, 2024",
    owner: "Maria Santos",
    status: "Draft",
  },
  {
    id: "executive-communication-framework",
    type: "document",
    title: "Executive Communication Framework",
    section: "Framework",
    path: "Atlas Library / 03. Workstreams / Communications",
    excerpt:
      "... A communication framework for executive updates, decisions, and cross-functional announcements ...",
    updatedAt: "Updated Apr 16, 2024",
    owner: "Jacob Lee",
    status: "Published",
  },
  {
    id: "decision-frameworks",
    type: "collection",
    title: "Decision Frameworks",
    section: "Decision",
    path: "Atlas Library / 04. Guides & Playbooks",
    updatedAt: "Updated May 3, 2024",
    collaboratorCount: 5,
    documentCount: 12,
  },
  {
    id: "strategy-decision-making",
    type: "collection",
    title: "Strategy & Decision Making",
    section: "Decision",
    path: "Atlas Library / 02. Strategy",
    updatedAt: "Updated Apr 30, 2024",
    collaboratorCount: 8,
    documentCount: 18,
  },
];

export const selectedSearchDetail: SearchDetail = {
  code: "3.1",
  title: "Decision Framework",
  status: "Published",
  updatedAt: "Apr 28, 2024",
  path: "Atlas Library / 01. Foundations / Mission & Vision",
  summary:
    "Defines the criteria and steps we use to evaluate options and make high-quality decisions that align with our mission and strategy.",
  keyElements: [
    "Purpose & scope",
    "Decision criteria",
    "Evaluation process",
    "Roles & responsibilities",
    "Decision recording & review",
  ],
  excerpt:
    "... The Decision Framework provides a consistent approach for evaluating options and making aligned choices. It balances impact, effort, and risk while considering our long-term strategy.\n\nAll major decisions should follow this framework and be documented in the decision log ...",
  tags: ["Decision Making", "Strategy", "Framework", "Process"],
  relatedDocuments: [
    { code: "1.2", title: "Mission & Vision", updatedAt: "Updated Apr 14, 2024" },
    { code: "3.2", title: "Strategy Overview", updatedAt: "Updated Apr 19, 2024" },
    { code: "", title: "Decision Review Checklist", updatedAt: "Updated Apr 22, 2024" },
  ],
  details: [
    { label: "Author", value: "Alice Kim" },
    { label: "Last updated", value: "Apr 28, 2024 at 11:22 AM" },
    { label: "Created", value: "Mar 12, 2024" },
    { label: "Status", value: "Published" },
  ],
};
