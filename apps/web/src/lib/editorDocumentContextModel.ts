import type {
  ActivityTimelineItemDto,
  BacklinkItemDto,
  DocumentActivityResponse,
  DocumentContextResponse,
  RelatedDocumentDto,
  VersionTrailItemDto,
} from "./appApi";
import { createEditorHash } from "./hashRouting";

export type EditorContextLoadStatus = "demo" | "error" | "idle" | "loading" | "ready";

export type EditorRelatedDocumentRow = {
  id: string;
  code: string;
  href: string;
  title: string;
};

export type EditorVersionTrailRow = {
  id: string;
  author: string;
  date: string;
  status: string;
  version: string;
};

export type EditorBacklinkRow = {
  id: string;
  code: string;
  excerpt: string;
  href: string;
  title: string;
};

export type EditorActivityRow = {
  id: string;
  date: string;
  detail: string;
  title: string;
};

export type EditorDocumentContextPanelModel = {
  activity: EditorActivityRow[];
  backlinks: EditorBacklinkRow[];
  relatedDocuments: EditorRelatedDocumentRow[];
  versionTrail: EditorVersionTrailRow[];
};

export function createEditorDocumentContextPanelModel(
  context: DocumentContextResponse | null,
  activity: DocumentActivityResponse | null,
): EditorDocumentContextPanelModel {
  return {
    activity: activity?.items.map(toActivityRow) ?? [],
    backlinks: context?.backlinks.map(toBacklinkRow) ?? [],
    relatedDocuments: context?.relatedDocuments.map(toRelatedDocumentRow) ?? [],
    versionTrail: context?.versionTrail.map(toVersionTrailRow) ?? [],
  };
}

export function formatDocumentStatus(value?: string | null) {
  const normalized = value?.trim();

  if (!normalized) {
    return "Draft";
  }

  return normalized
    .replace(/[_-]+/g, " ")
    .split(/\s+/)
    .filter(Boolean)
    .map((part) => `${part.charAt(0).toUpperCase()}${part.slice(1).toLowerCase()}`)
    .join(" ");
}

function toRelatedDocumentRow(document: RelatedDocumentDto): EditorRelatedDocumentRow {
  return {
    id: document.id,
    code: document.code,
    href: createEditorHash(document.id),
    title: document.title,
  };
}

function toVersionTrailRow(item: VersionTrailItemDto): EditorVersionTrailRow {
  return {
    id: item.id,
    author: item.author,
    date: formatPanelDate(item.date),
    status: formatDocumentStatus(item.status),
    version: item.version,
  };
}

function toBacklinkRow(item: BacklinkItemDto): EditorBacklinkRow {
  return {
    id: item.id,
    code: item.code,
    excerpt: item.excerpt,
    href: createEditorHash(item.id),
    title: item.title,
  };
}

function toActivityRow(item: ActivityTimelineItemDto): EditorActivityRow {
  return {
    id: item.id,
    date: formatPanelDate(item.date),
    detail: item.detail,
    title: item.title,
  };
}

function formatPanelDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(date);
}
