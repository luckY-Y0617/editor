import { apiFetch } from "./apiClient";
import type { JSONContent } from "@tiptap/react";

export type WorkspaceDto = {
  id: string;
  name: string;
  currentSpaceId: string;
};

export type SpaceDto = {
  id: string;
  name: string;
};

export type KnowledgeFolderDto = {
  id: string;
  title: string;
  sortOrder: number;
  documentCount: number;
};

export type KnowledgeDocumentSummaryDto = {
  id: string;
  folderId: string;
  title: string;
  status: string;
  updatedAt: string;
  tags: string[];
  sortOrder: number;
};

export type OwnerDto = {
  id: string;
  name: string;
};

export type KnowledgeDocumentDto = KnowledgeDocumentSummaryDto & {
  content: JSONContent;
  owner: OwnerDto;
  revision: number;
  version: string;
};

export type BootstrapResponse = {
  workspace: WorkspaceDto;
  spaces: SpaceDto[];
  activeSpaceId: string;
  folders: KnowledgeFolderDto[];
  documents: KnowledgeDocumentSummaryDto[];
  activeDocumentId: string;
};

export type SearchResultDto = {
  id: string;
  type: string;
  title: string;
  folderId: string;
  excerpt: string;
  updatedAt: string;
};

export type SearchResponse = {
  results: SearchResultDto[];
};

export type CreateDocumentResponse = {
  document: KnowledgeDocumentDto;
  map: {
    folders: KnowledgeFolderDto[];
    documents: KnowledgeDocumentSummaryDto[];
  };
};

export type GetDocumentResponse = {
  document: KnowledgeDocumentDto;
};

export type UpdateDocumentResponse = {
  document: KnowledgeDocumentDto;
};

export function getBootstrap(signal?: AbortSignal) {
  return apiFetch<BootstrapResponse>("/bootstrap", { signal });
}

export function searchKnowledge(request: { q: string; spaceId: string }, signal?: AbortSignal) {
  const params = new URLSearchParams({ q: request.q, spaceId: request.spaceId });
  return apiFetch<SearchResponse>(`/search?${params.toString()}`, { signal });
}

export function createDocument(request: { folderId: string; title?: string | null }, signal?: AbortSignal) {
  return apiFetch<CreateDocumentResponse>("/documents", {
    body: request,
    method: "POST",
    signal,
  });
}

export function getDocument(documentId: string, signal?: AbortSignal) {
  return apiFetch<GetDocumentResponse>(`/documents/${documentId}`, { signal });
}

export function updateDocument(
  documentId: string,
  request: {
    baseRevision: number;
    content?: JSONContent | null;
    tags?: string[] | null;
    title?: string | null;
  },
  signal?: AbortSignal,
) {
  return apiFetch<UpdateDocumentResponse>(`/documents/${documentId}`, {
    body: request,
    method: "PATCH",
    signal,
  });
}
