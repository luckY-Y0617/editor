import { describe, expect, test } from "../test/harness";
import { migrateDocumentContentBlockIds } from "../extensions/BlockIdentity";
import type { KnowledgeDocument } from "../types/editor";
import { buildKnowledgeExportPayload, validateImportedKnowledgeState } from "./knowledgeTransfer";

describe("knowledgeTransfer", () => {
  test("storage and export payload shapes exclude local comment state", () => {
    const document = createCleanDocument();
    const persistedState = {
      activeDocumentId: document.id,
      documents: [document],
    };
    const exportPayload = buildKnowledgeExportPayload(
      persistedState,
      "2026-04-29T00:00:00.000Z",
    );
    const storagePayloadJson = JSON.stringify(persistedState);
    const exportPayloadJson = JSON.stringify(exportPayload);
    const contentJson = JSON.stringify(document.content);

    expect(Object.keys(persistedState).sort()).toEqual(["activeDocumentId", "documents"]);
    expect(Object.keys(exportPayload).sort()).toEqual([
      "activeDocumentId",
      "documents",
      "exportVersion",
      "exportedAt",
    ]);
    expect(storagePayloadJson.includes("commentThreads")).toBe(false);
    expect(storagePayloadJson.includes("commentLoadState")).toBe(false);
    expect(storagePayloadJson.includes("commentLifecycle")).toBe(false);
    expect(storagePayloadJson.includes("pendingCommentComposer")).toBe(false);
    expect(storagePayloadJson.includes("isSubmitting")).toBe(false);
    expect(storagePayloadJson.includes("runtimeMatch")).toBe(false);
    expect(storagePayloadJson.includes("relocationResult")).toBe(false);
    expect(storagePayloadJson.includes("runtimeRange")).toBe(false);
    expect(exportPayloadJson.includes("commentThreads")).toBe(false);
    expect(exportPayloadJson.includes("commentLoadState")).toBe(false);
    expect(exportPayloadJson.includes("commentLifecycle")).toBe(false);
    expect(exportPayloadJson.includes("activeThreadId")).toBe(false);
    expect(exportPayloadJson.includes("isSubmitting")).toBe(false);
    expect(exportPayloadJson.includes("runtimeMatch")).toBe(false);
    expect(exportPayloadJson.includes("relocationResult")).toBe(false);
    expect(exportPayloadJson.includes("runtimeRange")).toBe(false);
    expect(contentJson.includes("blockId")).toBe(true);
    expect(contentJson.includes("comment")).toBe(false);
    expect(contentJson.includes("threadId")).toBe(false);
    expect(contentJson.includes("anchorStatus")).toBe(false);
    expect(contentJson.includes("runtimeMatch")).toBe(false);
    expect(contentJson.includes("relocationResult")).toBe(false);
    expect(contentJson.includes("runtimeRange")).toBe(false);
    expect(contentJson.includes("data-comment-thread-id")).toBe(false);
  });

  test("import normalization adds structural blockId without comment state", () => {
    const importResult = validateImportedKnowledgeState({
      activeDocumentId: "doc-import",
      documents: [
        {
          id: "doc-import",
          title: "Imported Document",
          folderId: "product",
          updatedAt: "2026-04-29T00:00:00.000Z",
          content: {
            type: "doc",
            content: [
              {
                type: "paragraph",
                content: [{ type: "text", text: "Imported body text." }],
              },
            ],
          },
        },
      ],
    });

    if (!importResult.ok) {
      throw new Error(importResult.message);
    }

    const serializedContent = JSON.stringify(importResult.state.documents[0].content);

    expect(serializedContent.includes("blockId")).toBe(true);
    expect(serializedContent.includes("threadId")).toBe(false);
    expect(serializedContent.includes("runtimeMatch")).toBe(false);
    expect(serializedContent.includes("relocationResult")).toBe(false);
    expect(serializedContent.includes("runtimeRange")).toBe(false);
    expect(serializedContent.includes("commentThreads")).toBe(false);
  });
});

function createCleanDocument(): KnowledgeDocument {
  return {
    id: "doc-clean",
    title: "Clean Document",
    folderId: "product",
    updatedAt: "2026-04-29T00:00:00.000Z",
    tags: [],
    content: migrateDocumentContentBlockIds(
      {
        type: "doc",
        content: [
          {
            type: "paragraph",
            content: [
              {
                type: "text",
                text: "Clean body text for export.",
              },
            ],
          },
        ],
      },
      { createId: () => "blk_export0001" },
    ),
  };
}
