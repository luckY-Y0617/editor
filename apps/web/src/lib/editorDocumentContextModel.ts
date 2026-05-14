import type {
  ActivityTimelineItemDto,
  BacklinkItemDto,
  DocumentActivityResponse,
  DocumentContextResponse,
  DocumentVersionSummaryDto,
  RelatedDocumentDto,
  VersionTrailItemDto,
} from "./appApi";
import { isUuid } from "./apiClient";
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
  author?: string;
  date: string;
  status: string;
  version: string;
  versionId?: string;
  versionType?: string;
  wordCount?: number;
};

export type EditorBacklinkRow = {
  id: string;
  code: string;
  excerpt: string;
  href: string;
  title: string;
};

export type EditorActivityRow = {
  actorName: string;
  actionLabel: string;
  documentTitle: string;
  href: string;
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
    activity: aggregateEditorActivityRows(activity?.items.map(toActivityRow) ?? []),
    backlinks: context?.backlinks.filter(isValidContextResource).map(toBacklinkRow) ?? [],
    relatedDocuments: context?.relatedDocuments.filter(isValidContextResource).map(toRelatedDocumentRow) ?? [],
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

export function createEditorVersionTrailRowsFromVersions(
  versions: DocumentVersionSummaryDto[] | null | undefined,
): EditorVersionTrailRow[] {
  return (versions ?? []).map((version) => ({
    id: version.id,
    author: formatVersionAuthor(version.createdBy),
    date: formatPanelDate(version.publishedAt ?? version.createdAt),
    status: formatDocumentStatus(version.versionType),
    version: version.label,
    versionId: version.id,
    versionType: version.versionType,
    wordCount: version.wordCount,
  }));
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

function formatVersionAuthor(value?: string | null) {
  if (!value?.trim()) {
    return undefined;
  }

  const trimmed = value.trim();
  return isUuid(trimmed) ? undefined : trimmed;
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
  const actorName = item.actor?.name?.trim() || "Unknown user";
  const documentTitle = item.document?.title?.trim() || item.title?.trim() || "this document";
  const actionLabel = formatActivityAction(item.title, item.detail);

  return {
    actorName,
    actionLabel,
    documentTitle,
    href: item.document?.id && isUuid(item.document.id) ? createEditorHash(item.document.id) : "#",
    id: item.id,
    date: formatPanelDate(item.date),
    detail: formatActivityDetail({
      actionLabel,
      actorName,
      detail: item.detail,
      documentTitle,
    }),
    title: documentTitle,
  };
}

function isValidContextResource(item: { id: string }) {
  return isUuid(item.id);
}

function aggregateEditorActivityRows(rows: EditorActivityRow[]): EditorActivityRow[] {
  const groupedRows: EditorActivityRow[] = [];
  let index = 0;

  while (index < rows.length) {
    const current = rows[index];
    const groupKey = getEditorActivityGroupKey(current);

    if (!groupKey) {
      groupedRows.push(current);
      index += 1;
      continue;
    }

    const group = [current];
    let nextIndex = index + 1;

    while (nextIndex < rows.length) {
      const next = rows[nextIndex];
      if (getEditorActivityGroupKey(next) !== groupKey) {
        break;
      }

      group.push(next);
      nextIndex += 1;
    }

    groupedRows.push(group.length > 1 ? createGroupedEditorActivityRow(group) : current);
    index = nextIndex;
  }

  return groupedRows;
}

function createGroupedEditorActivityRow(group: EditorActivityRow[]): EditorActivityRow {
  const latest = group[0];

  return {
    ...latest,
    detail: `${latest.actorName} updated ${latest.documentTitle} ${group.length} times. ${group.length} updates grouped.`,
    id: `${latest.id}:grouped-${group.length}`,
  };
}

function getEditorActivityGroupKey(row: EditorActivityRow) {
  if (!isGroupableEditorActivityRow(row)) {
    return null;
  }

  return [
    normalizeActivityGroupValue(row.actorName),
    normalizeActivityGroupValue(row.documentTitle),
  ].join("|");
}

function isGroupableEditorActivityRow(row: EditorActivityRow) {
  return row.actionLabel === "updated" && isFormattedGenericUpdateDetail(row);
}

function isFormattedGenericUpdateDetail(row: EditorActivityRow) {
  const normalizedDetail = row.detail.trim().toLowerCase();
  const genericDetail = `${row.actorName} updated ${row.documentTitle}.`.toLowerCase();

  return normalizedDetail === genericDetail || isGenericActivityDetail(row.detail);
}

function normalizeActivityGroupValue(value: string) {
  return value.trim().toLowerCase().replace(/\s+/g, " ");
}

function formatActivityAction(title: string, detail: string) {
  const normalized = `${title} ${detail}`.toLowerCase();

  if (normalized.includes("comment")) {
    return "commented on";
  }

  if (normalized.includes("share") || normalized.includes("invite") || normalized.includes("permission")) {
    return "updated access for";
  }

  if (normalized.includes("create")) {
    return "created";
  }

  if (normalized.includes("publish")) {
    return "published";
  }

  if (normalized.includes("archive")) {
    return "archived";
  }

  return "updated";
}

function formatActivityDetail({
  actionLabel,
  actorName,
  detail,
  documentTitle,
}: {
  actionLabel: string;
  actorName: string;
  detail: string;
  documentTitle: string;
}) {
  const trimmedDetail = detail.trim();

  if (trimmedDetail && !isGenericActivityDetail(trimmedDetail)) {
    return trimmedDetail;
  }

  return `${actorName} ${actionLabel} ${documentTitle}.`;
}

function isGenericActivityDetail(value: string) {
  return /^(updated content\.?|document updated\.?|updated document\.?)$/i.test(value.trim());
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
