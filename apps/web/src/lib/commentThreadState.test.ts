import { describe, expect, test } from "../test/harness";
import { createTestCommentThread } from "../test/commentTestUtils";
import {
  prependThreadForDocument,
  replaceThreadForDocument,
  selectCommentThreadsForDocument,
  setThreadsForDocument,
} from "./commentThreadState";

describe("commentThreadState", () => {
  test("loads and selects comments per document", () => {
    const threadA = createTestCommentThread({
      documentId: "doc-a",
      exact: "alpha",
      id: "thread-a",
      range: { from: 1, to: 6 },
    });
    const threadB = createTestCommentThread({
      documentId: "doc-b",
      exact: "beta",
      id: "thread-b",
      range: { from: 1, to: 5 },
    });
    const threadsByDocumentId = setThreadsForDocument(
      setThreadsForDocument({}, "doc-a", [threadA]),
      "doc-b",
      [threadB],
    );

    expect(selectCommentThreadsForDocument(threadsByDocumentId, {}, "doc-a")).toEqual([threadA]);
    expect(selectCommentThreadsForDocument(threadsByDocumentId, {}, "doc-b")).toEqual([threadB]);
  });

  test("document switch cannot show comments for the wrong document", () => {
    const threadA = createTestCommentThread({
      documentId: "doc-a",
      exact: "alpha",
      id: "thread-a",
      range: { from: 1, to: 6 },
    });
    const threadB = createTestCommentThread({
      documentId: "doc-b",
      exact: "beta",
      id: "thread-b",
      range: { from: 1, to: 5 },
    });
    const threadsByDocumentId = prependThreadForDocument(
      setThreadsForDocument({}, "doc-a", [threadA]),
      "doc-b",
      threadB,
    );

    const activeDocumentAThreads = selectCommentThreadsForDocument(threadsByDocumentId, {}, "doc-a");
    const activeDocumentBThreads = selectCommentThreadsForDocument(threadsByDocumentId, {}, "doc-b");

    expect(activeDocumentAThreads.length).toBe(1);
    expect(activeDocumentAThreads[0].id).toBe("thread-a");
    expect(activeDocumentBThreads.length).toBe(1);
    expect(activeDocumentBThreads[0].id).toBe("thread-b");
  });

  test("runtime anchor statuses are a UI overlay over repository threads", () => {
    const thread = createTestCommentThread({
      documentId: "doc-a",
      exact: "alpha",
      id: "thread-a",
      range: { from: 1, to: 6 },
    });
    const threadsByDocumentId = setThreadsForDocument({}, "doc-a", [thread]);

    const selectedThreads = selectCommentThreadsForDocument(
      threadsByDocumentId,
      {
        "doc-a": {
          "thread-a": "orphaned",
        },
      },
      "doc-a",
    );

    expect(thread.anchorStatus).toBe("active");
    expect(selectedThreads[0].anchorStatus).toBe("orphaned");
  });

  test("replaceThreadForDocument updates one document without touching another", () => {
    const threadA = createTestCommentThread({
      documentId: "doc-a",
      exact: "alpha",
      id: "thread-a",
      range: { from: 1, to: 6 },
    });
    const threadB = createTestCommentThread({
      documentId: "doc-b",
      exact: "beta",
      id: "thread-b",
      range: { from: 1, to: 5 },
    });
    const resolvedThreadA = {
      ...threadA,
      status: "resolved" as const,
      resolvedAt: "2026-04-29T00:01:00.000Z",
      updatedAt: "2026-04-29T00:01:00.000Z",
    };
    const threadsByDocumentId = setThreadsForDocument(
      setThreadsForDocument({}, "doc-a", [threadA]),
      "doc-b",
      [threadB],
    );

    const nextThreadsByDocumentId = replaceThreadForDocument(
      threadsByDocumentId,
      "doc-a",
      resolvedThreadA,
    );

    expect(selectCommentThreadsForDocument(nextThreadsByDocumentId, {}, "doc-a")).toEqual([
      resolvedThreadA,
    ]);
    expect(selectCommentThreadsForDocument(nextThreadsByDocumentId, {}, "doc-b")).toEqual([threadB]);
  });
});
