import { describe, expect, test } from "../test/harness";
import {
  assertLocalApiUploadTarget,
  createDocumentUploadIdempotencyKey,
  DOCUMENT_ATTACHMENT_MAX_BYTES,
  fetchFileContentBlob,
  formatFileSize,
  getDocumentAttachmentFileUnavailableReason,
  getDocumentFileUploadUnavailableReason,
  normalizeDownloadFilename,
  validateDocumentAttachmentFiles,
} from "./documentFilesModel";

describe("documentFilesModel", () => {
  test("formats file sizes for attachment rows", () => {
    expect(formatFileSize(42)).toBe("42 B");
    expect(formatFileSize(1536)).toBe("1.5 KB");
    expect(formatFileSize(5 * 1024 * 1024)).toBe("5.0 MB");
  });

  test("keeps invalid sizes honest", () => {
    expect(formatFileSize(-1)).toBe("Unknown size");
  });

  test("fetches file content with auth headers and bound fetch context", async () => {
    await withMockWindow(async () => {
      let capturedInput = "";
      let capturedAuth = "";
      let capturedThis: unknown = null;
      const fetchFn = function fetchWithRequiredContext(this: unknown, input: RequestInfo | URL, init?: RequestInit) {
        capturedThis = this;
        capturedInput = String(input);
        capturedAuth = new Headers(init?.headers).get("Authorization") ?? "";
        return Promise.resolve(new Response(new Blob(["file-bytes"], { type: "text/plain" })));
      } as typeof fetch;

      const blob = await fetchFileContentBlob("11111111-1111-1111-1111-111111111111", {
        apiBaseUrl: "https://northstar.test/api/v1",
        fetchFn,
      });

      expect(capturedThis === globalThis).toBe(true);
      expect(capturedInput).toBe("https://northstar.test/api/v1/files/11111111-1111-1111-1111-111111111111/content");
      expect(capturedAuth).toBe("Bearer token-1");
      expect(blob.type).toBe("text/plain");
    });
  });

  test("rejects invalid file ids before content access", async () => {
    let message = "";

    try {
      await fetchFileContentBlob("not-a-file-id", {
        apiBaseUrl: "https://northstar.test/api/v1",
        fetchFn: (() => Promise.resolve(new Response())) as typeof fetch,
      });
    } catch (error) {
      message = error instanceof Error ? error.message : "";
    }

    expect(message).toBe("A valid file id is required to read file content.");
  });

  test("reports why document file upload is unavailable", () => {
    expect(getDocumentFileUploadUnavailableReason("11111111-1111-1111-1111-111111111111", null)).toBe(
      "Configure the API before uploading files.",
    );
    expect(getDocumentFileUploadUnavailableReason(null, "https://northstar.test/api/v1")).toBe(
      "Save the document before uploading files.",
    );
    expect(getDocumentFileUploadUnavailableReason("not-a-document", "https://northstar.test/api/v1")).toBe(
      "Save the document before uploading files.",
    );
    expect(getDocumentFileUploadUnavailableReason("11111111-1111-1111-1111-111111111111", "https://northstar.test/api/v1")).toBe(null);
  });

  test("reports why selected attachment files are unavailable", () => {
    expect(getDocumentAttachmentFileUnavailableReason(null)).toBe("Choose a file before uploading.");
    expect(getDocumentAttachmentFileUnavailableReason(new File([], "empty.txt"))).toBe("Empty files cannot be uploaded.");
    expect(
      getDocumentAttachmentFileUnavailableReason(
        { name: "oversized.bin", size: DOCUMENT_ATTACHMENT_MAX_BYTES + 1 } as File,
      ),
    ).toBe("Files larger than 50 MB cannot be uploaded.");
    expect(getDocumentAttachmentFileUnavailableReason(new File(["ok"], "brief.txt"))).toBe(null);
  });

  test("validates multi-file attachment selections without hiding rejected files", () => {
    const valid = new File(["ok"], "brief.txt", { type: "text/plain" });
    const empty = new File([], "empty.txt");
    const oversized = { name: "huge.bin", size: DOCUMENT_ATTACHMENT_MAX_BYTES + 1 } as File;

    const result = validateDocumentAttachmentFiles([valid, empty, oversized], { maxFiles: 2 });

    expect(result.acceptedFiles).toEqual([valid]);
    expect(result.rejectedCount).toBe(2);
    expect(result.message).toContain("Only 2 files can be uploaded at once.");
    expect(result.message).toContain("empty.txt: Empty files cannot be uploaded.");
  });

  test("normalizes unsafe download filenames", () => {
    expect(normalizeDownloadFilename(" quarterly/report?.pdf ")).toBe("quarterly-report-.pdf");
    expect(normalizeDownloadFilename("   ")).toBe("northstar-file");
  });

  test("keeps frontend upload target support honest", () => {
    assertLocalApiUploadTarget({
      method: "PUT",
      type: "local-api",
      url: "/api/v1/files/uploads/sessions/session-1/content",
    });

    let message = "";
    try {
      assertLocalApiUploadTarget({
        method: "POST",
        type: "presigned-url",
        url: "https://storage.example/upload",
      });
    } catch (error) {
      message = error instanceof Error ? error.message : "";
    }

    expect(message).toBe("This frontend currently supports the local API upload target only.");
  });

  test("creates stable upload idempotency keys for the same document file input", () => {
    const file = new File(["hello"], "brief.png", {
      lastModified: 1234,
      type: "image/png",
    });

    expect(createDocumentUploadIdempotencyKey("editor-image", "11111111-1111-1111-1111-111111111111", file)).toBe(
      createDocumentUploadIdempotencyKey("editor-image", "11111111-1111-1111-1111-111111111111", file),
    );
    expect(createDocumentUploadIdempotencyKey("editor-image", "11111111-1111-1111-1111-111111111111", file)).toBe(
      "editor-image:11111111-1111-1111-1111-111111111111:brief.png:5:1234:image/png",
    );
  });
});

async function withMockWindow(run: () => Promise<void>) {
  const previousWindow = Object.getOwnPropertyDescriptor(globalThis, "window");
  Object.defineProperty(globalThis, "window", {
    configurable: true,
    value: {
      addEventListener() {
        return undefined;
      },
      dispatchEvent() {
        return true;
      },
      localStorage: {
        getItem(key: string) {
          return key === "northstar.accessToken" ? "token-1" : null;
        },
        removeItem() {
          return undefined;
        },
        setItem() {
          return undefined;
        },
      },
      location: {
        search: "",
      },
      removeEventListener() {
        return undefined;
      },
    } as unknown as Window,
  });

  try {
    await run();
  } finally {
    if (previousWindow) {
      Object.defineProperty(globalThis, "window", previousWindow);
    } else {
      Reflect.deleteProperty(globalThis, "window");
    }
  }
}
