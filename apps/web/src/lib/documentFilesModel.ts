import {
  completeUploadSession,
  createUploadSession,
  finalizeUploadSession,
  uploadUploadSessionContent,
  type DocumentAttachmentDto,
} from "./appApi";
import { ApiClientError, buildApiUrl, createApiHeaders, getConfiguredApiBaseUrl, isUuid } from "./apiClient";

export const documentAttachmentChangedEvent = "northstar:document-attachment-changed";
export const DOCUMENT_ATTACHMENT_MAX_BYTES = 50 * 1024 * 1024;
export const DOCUMENT_ATTACHMENT_MAX_FILES = 10;

export type UploadedDocumentImage = {
  attachment: DocumentAttachmentDto | null;
  fileId: string;
  src: string;
};

export function canUseDocumentFileUpload(documentId: string | null | undefined) {
  return getDocumentFileUploadUnavailableReason(documentId) === null;
}

export function getDocumentAttachmentFileUnavailableReason(
  file: File | null | undefined,
  maxBytes = DOCUMENT_ATTACHMENT_MAX_BYTES,
) {
  if (!file) {
    return "Choose a file before uploading.";
  }

  if (file.size <= 0) {
    return "Empty files cannot be uploaded.";
  }

  if (file.size > maxBytes) {
    return `Files larger than ${formatFileSize(maxBytes)} cannot be uploaded.`;
  }

  return null;
}

export type DocumentAttachmentFilesValidation = {
  acceptedFiles: File[];
  message: string | null;
  rejectedCount: number;
};

export function validateDocumentAttachmentFiles(
  files: FileList | File[] | null | undefined,
  options: { maxBytes?: number; maxFiles?: number } = {},
): DocumentAttachmentFilesValidation {
  const maxBytes = options.maxBytes ?? DOCUMENT_ATTACHMENT_MAX_BYTES;
  const maxFiles = options.maxFiles ?? DOCUMENT_ATTACHMENT_MAX_FILES;
  const selectedFiles = files ? Array.from(files) : [];

  if (selectedFiles.length === 0) {
    return {
      acceptedFiles: [],
      message: "Choose at least one file before uploading.",
      rejectedCount: 0,
    };
  }

  const acceptedFiles: File[] = [];
  const messages: string[] = [];
  const limitedFiles = selectedFiles.slice(0, maxFiles);

  if (selectedFiles.length > maxFiles) {
    messages.push(`Only ${maxFiles} files can be uploaded at once.`);
  }

  for (const file of limitedFiles) {
    const unavailableReason = getDocumentAttachmentFileUnavailableReason(file, maxBytes);
    if (unavailableReason) {
      messages.push(`${file.name || "Selected file"}: ${unavailableReason}`);
      continue;
    }

    acceptedFiles.push(file);
  }

  return {
    acceptedFiles,
    message: messages.length > 0 ? messages.join(" ") : null,
    rejectedCount: selectedFiles.length - acceptedFiles.length,
  };
}

export function getDocumentFileUploadUnavailableReason(
  documentId: string | null | undefined,
  apiBaseUrl: string | null = getConfiguredApiBaseUrl(),
) {
  if (!apiBaseUrl) {
    return "Configure the API before uploading files.";
  }

  if (!documentId || !isUuid(documentId)) {
    return "Save the document before uploading files.";
  }

  return null;
}

export async function uploadDocumentImage(
  documentId: string,
  file: File,
  options: { signal?: AbortSignal; workspaceId?: string | null } = {},
): Promise<UploadedDocumentImage> {
  if (!canUseDocumentFileUpload(documentId)) {
    throw new Error("Document file upload requires a configured API and a saved document.");
  }

  const fileUnavailableReason = getDocumentAttachmentFileUnavailableReason(file);
  if (fileUnavailableReason) {
    throw new Error(fileUnavailableReason);
  }

  const session = await createUploadSession(
    {
      bizType: "editor-image",
      byteSize: file.size,
      checksumSha256: null,
      documentId,
      idempotencyKey: createDocumentUploadIdempotencyKey("editor-image", documentId, file),
      mimeType: file.type || "application/octet-stream",
      originalFilename: file.name || "image",
      uploadMode: "single",
      workspaceId: options.workspaceId ?? null,
    },
    options.signal,
  );

  assertLocalApiUploadTarget(session.uploadTarget);
  await uploadUploadSessionContent(session.sessionId, file, options.signal);
  await completeUploadSession(session.sessionId, options.signal);

  const finalized = await finalizeUploadSession(
    session.sessionId,
    {
      documentId,
      metadata: {
        alt: file.name || null,
        source: "editor-image-block",
      },
      relationType: "inline_image",
    },
    options.signal,
  );

  if (finalized.attachment && typeof window !== "undefined") {
    window.dispatchEvent(
      new CustomEvent<DocumentAttachmentChangedEventDetail>(documentAttachmentChangedEvent, {
        detail: {
          attachmentId: finalized.attachment.id,
          documentId,
          operation: "created",
        },
      }),
    );
  }

  return {
    attachment: finalized.attachment ?? null,
    fileId: finalized.file.id,
    src: buildApiUrl(`/files/${finalized.file.id}/content`),
  };
}

export async function uploadDocumentAttachment(
  documentId: string,
  file: File,
  options: { signal?: AbortSignal; workspaceId?: string | null } = {},
): Promise<DocumentAttachmentDto> {
  if (!canUseDocumentFileUpload(documentId)) {
    throw new Error("Document attachment upload requires a configured API and a saved document.");
  }

  const fileUnavailableReason = getDocumentAttachmentFileUnavailableReason(file);
  if (fileUnavailableReason) {
    throw new Error(fileUnavailableReason);
  }

  const session = await createUploadSession(
    {
      bizType: "document-attachment",
      byteSize: file.size,
      checksumSha256: null,
      documentId,
      idempotencyKey: createDocumentUploadIdempotencyKey("document-attachment", documentId, file),
      mimeType: file.type || "application/octet-stream",
      originalFilename: file.name || "attachment",
      uploadMode: "single",
      workspaceId: options.workspaceId ?? null,
    },
    options.signal,
  );

  assertLocalApiUploadTarget(session.uploadTarget);
  await uploadUploadSessionContent(session.sessionId, file, options.signal);
  await completeUploadSession(session.sessionId, options.signal);

  const finalized = await finalizeUploadSession(
    session.sessionId,
    {
      documentId,
      metadata: {
        source: "editor-attachment-panel",
      },
      relationType: "attachment",
    },
    options.signal,
  );

  if (!finalized.attachment) {
    throw new Error("Document attachment upload finished without an attachment relation.");
  }

  if (typeof window !== "undefined") {
    window.dispatchEvent(
      new CustomEvent<DocumentAttachmentChangedEventDetail>(documentAttachmentChangedEvent, {
        detail: {
          attachmentId: finalized.attachment.id,
          documentId,
          operation: "created",
        },
      }),
    );
  }

  return finalized.attachment;
}

export type DocumentAttachmentChangedEventDetail = {
  attachmentId: string;
  documentId: string;
  operation: "created" | "deleted";
};

export function createFileContentHref(fileId: string) {
  return isUuid(fileId) ? buildApiUrl(`/files/${fileId}/content`) : "#";
}

export type FetchFileContentOptions = {
  apiBaseUrl?: string;
  fetchFn?: typeof fetch;
  signal?: AbortSignal;
};

export async function fetchFileContentBlob(fileId: string, options: FetchFileContentOptions = {}) {
  if (!isUuid(fileId)) {
    throw new Error("A valid file id is required to read file content.");
  }

  const url = buildApiUrl(`/files/${fileId}/content`, options.apiBaseUrl);
  const fetchFn = options.fetchFn ?? globalThis.fetch;

  if (typeof fetchFn !== "function") {
    throw new ApiClientError(0, "Fetch is not available in this runtime.");
  }

  let response: Response;
  try {
    response = await fetchFn.call(globalThis, url, {
      headers: createApiHeaders(undefined, true),
      method: "GET",
      signal: options.signal,
    });
  } catch (error) {
    if (isAbortError(error)) {
      throw error;
    }

    const reason = error instanceof Error && error.message ? ` ${error.message}` : "";
    throw new ApiClientError(0, `Could not reach API endpoint ${url}.${reason}`);
  }

  if (!response.ok) {
    throw await toFileContentError(response);
  }

  return response.blob();
}

export async function createFileContentObjectUrl(fileId: string, options: FetchFileContentOptions = {}) {
  const blob = await fetchFileContentBlob(fileId, options);

  if (typeof URL === "undefined" || typeof URL.createObjectURL !== "function") {
    throw new Error("File preview is not available in this runtime.");
  }

  return URL.createObjectURL(blob);
}

export function revokeFileContentObjectUrl(url: string | null | undefined) {
  if (url && url.startsWith("blob:") && typeof URL !== "undefined" && typeof URL.revokeObjectURL === "function") {
    URL.revokeObjectURL(url);
  }
}

export async function openFileContent(fileId: string, options: FetchFileContentOptions = {}) {
  const objectUrl = await createFileContentObjectUrl(fileId, options);

  if (typeof document === "undefined") {
    revokeFileContentObjectUrl(objectUrl);
    throw new Error("File opening is not available in this runtime.");
  }

  const link = document.createElement("a");
  link.href = objectUrl;
  link.rel = "noopener noreferrer";
  link.target = "_blank";
  document.body.appendChild(link);
  link.click();
  link.remove();

  window.setTimeout(() => revokeFileContentObjectUrl(objectUrl), 60_000);
}

export async function downloadFileContent(
  fileId: string,
  filename: string,
  options: FetchFileContentOptions = {},
) {
  const objectUrl = await createFileContentObjectUrl(fileId, options);

  if (typeof document === "undefined") {
    revokeFileContentObjectUrl(objectUrl);
    throw new Error("File download is not available in this runtime.");
  }

  const link = document.createElement("a");
  link.href = objectUrl;
  link.download = normalizeDownloadFilename(filename);
  link.rel = "noopener noreferrer";
  document.body.appendChild(link);
  link.click();
  link.remove();

  window.setTimeout(() => revokeFileContentObjectUrl(objectUrl), 60_000);
}

export function formatFileSize(bytes: number) {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return "Unknown size";
  }

  if (bytes < 1024) {
    return `${bytes} B`;
  }

  const units = ["KB", "MB", "GB"];
  let value = bytes / 1024;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  return `${value >= 10 ? value.toFixed(0) : value.toFixed(1)} ${units[unitIndex]}`;
}

export function normalizeDownloadFilename(filename: string | null | undefined) {
  const normalized = filename?.trim().replace(/[\\/:*?"<>|]+/g, "-");
  return normalized && normalized.length > 0 ? normalized : "northstar-file";
}

export function createDocumentUploadIdempotencyKey(scope: string, documentId: string, file: File) {
  const fileType = file.type || "application/octet-stream";
  return [scope, documentId, file.name || "file", file.size, file.lastModified || 0, fileType].join(":");
}

export function assertLocalApiUploadTarget(target: {
  method?: string | null;
  type?: string | null;
  url?: string | null;
}) {
  const targetType = target.type?.trim().toLowerCase();
  const method = target.method?.trim().toUpperCase();

  if (targetType !== "local-api" || method !== "PUT" || !target.url?.trim()) {
    throw new Error("This frontend currently supports the local API upload target only.");
  }
}

async function toFileContentError(response: Response) {
  try {
    const body = (await response.json()) as {
      code?: string;
      error?: { code?: string; message?: string };
      message?: string;
      title?: string;
    };
    const code = body.error?.code ?? body.code;
    const message = body.error?.message ?? body.message ?? body.title ?? `API returned ${response.status}`;
    return new ApiClientError(response.status, message, code);
  } catch {
    return new ApiClientError(response.status, `API returned ${response.status}`);
  }
}

function isAbortError(error: unknown) {
  return (
    typeof DOMException !== "undefined" &&
    error instanceof DOMException &&
    error.name === "AbortError"
  );
}
