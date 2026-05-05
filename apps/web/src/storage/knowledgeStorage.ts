import { cloneContent, initialKnowledgeDocuments } from "../data/knowledgeDocuments";
import { migrateDocumentContentBlockIds } from "../extensions/BlockIdentity";
import type { KnowledgeDocument } from "../types/editor";
import type { JSONContent } from "@tiptap/react";

export const KNOWLEDGE_EDITOR_STORAGE_KEY = "northstar.knowledge-editor.v1";

export type KnowledgeEditorPersistedState = {
  documents: KnowledgeDocument[];
  activeDocumentId: string;
};

export function loadKnowledgeEditorState(): KnowledgeEditorPersistedState {
  const fallbackState = createFallbackState();

  if (!canUseLocalStorage()) {
    return fallbackState;
  }

  try {
    const rawValue = window.localStorage.getItem(KNOWLEDGE_EDITOR_STORAGE_KEY);

    if (!rawValue) {
      return fallbackState;
    }

    return normalizePersistedState(JSON.parse(rawValue), fallbackState);
  } catch {
    return fallbackState;
  }
}

export function saveKnowledgeEditorState(state: KnowledgeEditorPersistedState) {
  if (!canUseLocalStorage()) {
    return;
  }

  try {
    window.localStorage.setItem(KNOWLEDGE_EDITOR_STORAGE_KEY, JSON.stringify(state));
  } catch {
    // Local persistence is best-effort only; editing should never fail because storage is unavailable.
  }
}

function createFallbackState(): KnowledgeEditorPersistedState {
  const documents = initialKnowledgeDocuments.map((document) => ({
    ...document,
    content: migrateDocumentContentBlockIds(cloneContent(document.content)),
  }));

  return {
    documents,
    activeDocumentId: documents[0]?.id ?? "",
  };
}

function normalizePersistedState(
  value: unknown,
  fallbackState: KnowledgeEditorPersistedState,
): KnowledgeEditorPersistedState {
  if (!isRecord(value) || !Array.isArray(value.documents)) {
    return fallbackState;
  }

  const documents = value.documents.filter(isKnowledgeDocument).map((document) => ({
    ...document,
    content: migrateDocumentContentBlockIds(document.content),
  }));

  if (documents.length === 0) {
    return fallbackState;
  }

  const requestedActiveDocumentId =
    typeof value.activeDocumentId === "string" ? value.activeDocumentId : "";
  const activeDocumentId = documents.some((document) => document.id === requestedActiveDocumentId)
    ? requestedActiveDocumentId
    : documents[0].id;

  return {
    documents,
    activeDocumentId,
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

function canUseLocalStorage() {
  return typeof window !== "undefined" && typeof window.localStorage !== "undefined";
}
