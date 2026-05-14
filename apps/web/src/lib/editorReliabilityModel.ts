import type { SaveStatus } from "../hooks/useMockAutoSave";
import { formatApiOperationError } from "./apiClient";

export type EditorApiLoadStatus = "demo" | "error" | "loading" | "ready";
export type ApiSaveStatus = "saved" | "editing" | "saving" | "created" | "conflict" | "error";

export function toTopBarSaveStatus(status: ApiSaveStatus): SaveStatus {
  return status === "saving" || status === "saved" || status === "created" ? status : "editing";
}

export function getApiSaveStatusLabel(status: ApiSaveStatus) {
  if (status === "saving") {
    return "Saving";
  }

  if (status === "editing") {
    return "Unsaved changes";
  }

  if (status === "created") {
    return "Created";
  }

  if (status === "conflict") {
    return "Conflict detected";
  }

  if (status === "error") {
    return "Save failed";
  }

  return "Saved";
}

export function formatEditorOperationError(error: unknown, fallback: string) {
  return formatApiOperationError(error, fallback, {
    forbidden: "You do not have permission to change this document.",
    network: "Could not reach the document API. Check the backend session and retry.",
    unauthorized: "Sign in again before changing this document.",
    unconfigured: "Document API is not configured for this environment.",
  });
}
