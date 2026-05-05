import type { KnowledgeEditorPersistedState } from "../storage/knowledgeStorage";
import { migrateDocumentContentBlockIds } from "../extensions/BlockIdentity";
import type { KnowledgeDocument } from "../types/editor";
import type { JSONContent } from "@tiptap/react";

export const KNOWLEDGE_EXPORT_VERSION = 1;

export type KnowledgeEditorExportPayload = KnowledgeEditorPersistedState & {
  exportVersion: number;
  exportedAt: string;
};

export type KnowledgeImportResult =
  | {
      ok: true;
      state: KnowledgeEditorPersistedState;
    }
  | {
      ok: false;
      message: string;
    };

export function exportKnowledgeState(state: KnowledgeEditorPersistedState) {
  const payload = buildKnowledgeExportPayload(state);
  const blob = new Blob([JSON.stringify(payload, null, 2)], {
    type: "application/json;charset=utf-8",
  });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = url;
  link.download = buildExportFileName(new Date(payload.exportedAt));
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export function buildKnowledgeExportPayload(
  state: KnowledgeEditorPersistedState,
  exportedAt = new Date().toISOString(),
): KnowledgeEditorExportPayload {
  return {
    exportVersion: KNOWLEDGE_EXPORT_VERSION,
    exportedAt,
    documents: state.documents,
    activeDocumentId: state.activeDocumentId,
  };
}

export function buildExportFileName(date = new Date()) {
  const pad = (value: number) => String(value).padStart(2, "0");
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hour = pad(date.getHours());
  const minute = pad(date.getMinutes());

  return `knowledge-editor-export-${year}${month}${day}-${hour}${minute}.json`;
}

export function parseKnowledgeImport(fileText: string): KnowledgeImportResult {
  let parsedValue: unknown;

  try {
    parsedValue = JSON.parse(fileText);
  } catch {
    return {
      ok: false,
      message: "JSON 文件无法解析，请检查文件内容。",
    };
  }

  return validateImportedKnowledgeState(parsedValue);
}

export function validateImportedKnowledgeState(value: unknown): KnowledgeImportResult {
  if (!isRecord(value) || !Array.isArray(value.documents)) {
    return {
      ok: false,
      message: "导入文件缺少 documents 数组。",
    };
  }

  const documents = value.documents.filter(isKnowledgeDocument).map((document) => ({
    ...document,
    content: migrateDocumentContentBlockIds(document.content),
  }));

  if (documents.length === 0) {
    return {
      ok: false,
      message: "导入文件中没有可用文档。",
    };
  }

  const requestedActiveDocumentId =
    typeof value.activeDocumentId === "string" ? value.activeDocumentId : "";
  const activeDocumentId = documents.some((document) => document.id === requestedActiveDocumentId)
    ? requestedActiveDocumentId
    : documents[0].id;

  return {
    ok: true,
    state: {
      documents,
      activeDocumentId,
    },
  };
}

function isKnowledgeDocument(value: unknown): value is KnowledgeDocument {
  if (!isRecord(value)) {
    return false;
  }

  return (
    typeof value.id === "string" &&
    typeof value.title === "string" &&
    typeof value.folderId === "string" &&
    typeof value.updatedAt === "string" &&
    isJsonContent(value.content) &&
    (value.tags === undefined || isStringArray(value.tags))
  );
}

function isJsonContent(value: unknown): value is JSONContent {
  return isRecord(value) && typeof value.type === "string";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((item) => typeof item === "string");
}
