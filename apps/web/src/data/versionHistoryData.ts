export type VersionStatus = "Published" | "Draft";

export type VersionHistoryItem = {
  id: string;
  version: string;
  date: string;
  author: string;
  status: VersionStatus;
  isCompared?: boolean;
};

export type VersionDiffKind = "heading" | "paragraph" | "bullet";

export type VersionDiffTone = "added" | "removed" | "modified";

export type VersionDiffToken = {
  text: string;
  tone?: VersionDiffTone;
};

export type VersionDiffBlock = {
  id: string;
  kind: VersionDiffKind;
  tokens: VersionDiffToken[];
};

export type VersionCompareDocument = {
  id: string;
  version: string;
  date: string;
  author: string;
  status: VersionStatus;
  title: string;
  sectionTitle: string;
  blocks: VersionDiffBlock[];
};

export type VersionDetail = {
  version: string;
  status: VersionStatus;
  author: string;
  updatedAt: string;
  summary: string;
  relatedDocument: string;
  location: string[];
  readers: number;
  tags: string[];
  linkedVersions: string[];
};

export const versionHistoryItems: VersionHistoryItem[] = [
  {
    id: "version-3-2",
    version: "3.2",
    date: "May 14, 2024",
    author: "Alice Kim",
    status: "Published",
    isCompared: true,
  },
  {
    id: "version-3-1",
    version: "3.1",
    date: "Apr 28, 2024",
    author: "Alice Kim",
    status: "Published",
    isCompared: true,
  },
  {
    id: "version-3-0",
    version: "3.0",
    date: "Apr 10, 2024",
    author: "Ben Parker",
    status: "Published",
  },
  {
    id: "version-2-3",
    version: "2.3",
    date: "Mar 26, 2024",
    author: "Ben Parker",
    status: "Draft",
  },
  {
    id: "version-2-2",
    version: "2.2",
    date: "Mar 12, 2024",
    author: "Maria Santos",
    status: "Draft",
  },
  {
    id: "version-2-1",
    version: "2.1",
    date: "Feb 29, 2024",
    author: "Maria Santos",
    status: "Draft",
  },
  {
    id: "version-2-0",
    version: "2.0",
    date: "Feb 14, 2024",
    author: "Alice Kim",
    status: "Published",
  },
];

export const versionCompareDocuments: [VersionCompareDocument, VersionCompareDocument] = [
  {
    id: "mission-vision-3-1",
    version: "3.1",
    date: "Apr 28, 2024",
    author: "Alice Kim",
    status: "Published",
    title: "Mission & Vision",
    sectionTitle: "Library Promise",
    blocks: [
      {
        id: "left-promise",
        kind: "paragraph",
        tokens: [
          { text: "The Atlas Library makes durable knowledge easy to find " },
          { text: "so teams can act with confidence.", tone: "removed" },
        ],
      },
      {
        id: "left-find",
        kind: "bullet",
        tokens: [{ text: "Make knowledge easy to find and understand." }],
      },
      {
        id: "left-context",
        kind: "bullet",
        tokens: [{ text: "Keep decisions attached to their original context." }],
      },
      {
        id: "left-duplicate",
        kind: "bullet",
        tokens: [{ text: "Reduce duplicated knowledge across the organization.", tone: "removed" }],
      },
      {
        id: "left-purpose",
        kind: "bullet",
        tokens: [{ text: "Support teams with clarity and shared purpose." }],
      },
    ],
  },
  {
    id: "mission-vision-3-2",
    version: "3.2",
    date: "May 14, 2024",
    author: "Alice Kim",
    status: "Published",
    title: "Mission & Vision",
    sectionTitle: "Library Promise",
    blocks: [
      {
        id: "right-promise",
        kind: "paragraph",
        tokens: [
          { text: "The Atlas Library makes " },
          { text: "trusted", tone: "modified" },
          { text: " knowledge easy to find so teams can " },
          { text: "make better decisions", tone: "added" },
          { text: " with confidence." },
        ],
      },
      {
        id: "right-find",
        kind: "bullet",
        tokens: [{ text: "Make knowledge easy to find and understand." }],
      },
      {
        id: "right-context",
        kind: "bullet",
        tokens: [{ text: "Keep decisions attached to their original context." }],
      },
      {
        id: "right-contribution",
        kind: "bullet",
        tokens: [{ text: "Encourage contribution and knowledge sharing.", tone: "added" }],
      },
      {
        id: "right-purpose",
        kind: "bullet",
        tokens: [{ text: "Support teams with clarity and shared purpose." }],
      },
    ],
  },
];

export const selectedVersionDetail: VersionDetail = {
  version: "3.2",
  status: "Published",
  author: "Alice Kim",
  updatedAt: "May 14, 2024 at 13:48",
  summary:
    "Clarified mission statement and strengthened promise. Added emphasis on trusted knowledge and contribution.",
  relatedDocument: "Mission & Vision",
  location: ["Atlas Library", "01. Foundations"],
  readers: 12,
  tags: ["Strategy", "Foundation", "Core"],
  linkedVersions: ["2.0", "2.1", "2.2", "2.3", "3.0", "3.1"],
};
