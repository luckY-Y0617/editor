import { describe, expect, test } from "../test/harness";
import { ApiClientError } from "./apiClient";
import {
  formatEditorOperationError,
  getApiSaveStatusLabel,
  toTopBarSaveStatus,
  type ApiSaveStatus,
} from "./editorReliabilityModel";

describe("editorReliabilityModel", () => {
  test("keeps conflict and error visible while using existing top bar status states", () => {
    expect(toTopBarSaveStatus("conflict")).toBe("editing");
    expect(toTopBarSaveStatus("error")).toBe("editing");
    expect(getApiSaveStatusLabel("conflict")).toBe("Conflict detected");
    expect(getApiSaveStatusLabel("error")).toBe("Save failed");
  });

  test("passes stable save states through to the top bar", () => {
    const states: ApiSaveStatus[] = ["saving", "saved", "created"];

    expect(states.map(toTopBarSaveStatus)).toEqual(["saving", "saved", "created"]);
  });

  test("formats editor operation errors with a readable fallback", () => {
    expect(formatEditorOperationError(new Error("Backend unavailable"), "Fallback")).toBe(
      "Backend unavailable",
    );
    expect(formatEditorOperationError("Revision missing", "Fallback")).toBe("Revision missing");
    expect(formatEditorOperationError("", "Fallback")).toBe("Fallback");
    expect(formatEditorOperationError(null, "Fallback")).toBe("Fallback");
  });

  test("formats document API errors by user action boundary", () => {
    expect(formatEditorOperationError(new ApiClientError(0, "Could not reach API endpoint https://northstar.test/api/v1/documents. Failed to fetch"), "Fallback")).toBe(
      "Could not reach the document API. Check the backend session and retry.",
    );
    expect(formatEditorOperationError(new ApiClientError(401, "Access expired"), "Fallback")).toBe(
      "Sign in again before changing this document.",
    );
    expect(formatEditorOperationError(new ApiClientError(403, "Forbidden"), "Fallback")).toBe(
      "You do not have permission to change this document.",
    );
  });
});
