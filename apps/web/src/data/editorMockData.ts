import type { LucideIcon } from "lucide-react";
import {
  BookOpen,
  Brain,
  Clock3,
  FileText,
  Folder,
  Lightbulb,
  MessageSquareText,
  PenLine,
  Sparkles,
  Star,
  Tags,
  Trash2,
} from "lucide-react";

export type SidebarShortcut = {
  label: string;
  icon: LucideIcon;
  count?: number;
};

export type DocumentTreeItem = {
  id: string;
  title: string;
  icon?: LucideIcon;
  active?: boolean;
  children?: DocumentTreeItem[];
};

export type DocumentMetaItem = {
  label: string;
  value: string;
  icon: LucideIcon;
};

export type MockEditorBlock =
  | {
      id: string;
      type: "heading";
      level: 1 | 2 | 3;
      text: string;
    }
  | {
      id: string;
      type: "paragraph" | "quote";
      text: string;
    }
  | {
      id: string;
      type: "code";
      language: string;
      code: string;
    }
  | {
      id: string;
      type: "taskList";
      items: Array<{ text: string; checked: boolean }>;
    }
  | {
      id: string;
      type: "list";
      items: string[];
    };

export type AiAction = {
  label: string;
  description: string;
  icon: LucideIcon;
};

export type RelatedDocument = {
  code: string;
  title: string;
};

export type VersionTrailItem = {
  version: string;
  date: string;
  author: string;
  status: "Published" | "Draft";
};

export type BacklinkItem = {
  code: string;
  title: string;
  excerpt: string;
};

export type ActivityTimelineItem = {
  title: string;
  date: string;
  detail: string;
};

export const workspace = {
  name: "Northstar",
  plan: "Atlas Library",
  avatar: "NK",
};

export const sidebarShortcuts: SidebarShortcut[] = [
  { label: "Recent documents", icon: Clock3, count: 8 },
  { label: "Pinned routes", icon: Star, count: 5 },
  { label: "Atlas cards", icon: BookOpen },
];

export const documentTree: DocumentTreeItem[] = [
  {
    id: "product",
    title: "01. Foundations",
    icon: Folder,
    children: [
      { id: "editor-shell", title: "Our Principles", icon: FileText, active: true },
      { id: "writing-flow", title: "Mission & Vision", icon: FileText },
      { id: "block-system", title: "Operating System", icon: FileText },
    ],
  },
  {
    id: "research",
    title: "02. Strategy",
    icon: Folder,
    children: [{ id: "yuque", title: "Decision Framework", icon: FileText }],
  },
  {
    id: "archive",
    title: "06. Archives",
    icon: Folder,
    children: [{ id: "trash-note", title: "Retired Expedition Notes", icon: Trash2 }],
  },
];

export const documentMeta: DocumentMetaItem[] = [
  { label: "Updated", value: "May 14, 2024", icon: Clock3 },
  { label: "Owner", value: "Alice Kim", icon: PenLine },
  { label: "Tags", value: "Foundations / Atlas / Governance", icon: Tags },
];

export const mockEditorBlocks: MockEditorBlock[] = [
  {
    id: "intro",
    type: "paragraph",
    text: "Our principles are the immutable anchors that guide how we think, decide, and build.",
  },
  {
    id: "principles-title",
    type: "heading",
    level: 2,
    text: "Clarity over Cleverness",
  },
  {
    id: "principles-body",
    type: "paragraph",
    text: "Simple is not simplistic. Clarity respects people's time and attention.",
  },
];

export const relatedDocuments: RelatedDocument[] = [
  { code: "1.2", title: "Mission & Vision" },
  { code: "3.1", title: "Decision Framework" },
  { code: "4.2", title: "Communication Guide" },
];

export const versionTrail: VersionTrailItem[] = [
  { version: "3.2", date: "May 14, 2024", author: "Alice Kim", status: "Published" },
  { version: "3.1", date: "Apr 28, 2024", author: "Alice Kim", status: "Published" },
  { version: "3.0", date: "Apr 10, 2024", author: "Ben Parker", status: "Published" },
  { version: "2.3", date: "Mar 26, 2024", author: "Ben Parker", status: "Draft" },
  { version: "2.2", date: "Mar 12, 2024", author: "Maria Santos", status: "Draft" },
];

export const backlinks: BacklinkItem[] = [
  {
    code: "3.1",
    title: "Decision Framework",
    excerpt: "Principle 1 informs our approach to evaluating options...",
  },
];

export const activityTimeline: ActivityTimelineItem[] = [
  {
    title: "Published version 3.2",
    date: "May 14, 2024",
    detail: "Alice Kim moved this document into the published route.",
  },
  {
    title: "Updated outline",
    date: "May 10, 2024",
    detail: "Section order was adjusted to clarify the operating principles.",
  },
  {
    title: "Linked related document",
    date: "Apr 28, 2024",
    detail: "Mission & Vision was attached as a related foundation note.",
  },
  {
    title: "Created draft",
    date: "Mar 12, 2024",
    detail: "Initial field note was created in Atlas Library.",
  },
];

export const aiActions: AiAction[] = [
  {
    label: "Summarize document",
    description: "Create a short note that can sit near the document heading.",
    icon: Sparkles,
  },
  {
    label: "Extract next steps",
    description: "Turn the current document into a lightweight action list.",
    icon: Brain,
  },
  {
    label: "Rewrite selection",
    description: "Reserved entry point; no real AI connection in this version.",
    icon: MessageSquareText,
  },
  {
    label: "Suggest headings",
    description: "Generate alternate section names from the current structure.",
    icon: Lightbulb,
  },
];
