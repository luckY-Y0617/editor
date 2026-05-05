import { describe, expect, test } from "../test/harness";
import { createTestCommentAnchor } from "../test/commentTestUtils";
import { createLocalCommentThread, isCommentBodySubmittable } from "./commentComposerModel";

describe("commentComposerModel", () => {
  test("empty or whitespace body is not submittable", () => {
    expect(isCommentBodySubmittable("")).toBe(false);
    expect(isCommentBodySubmittable("   \n\t")).toBe(false);
  });

  test("non-empty body is submittable", () => {
    expect(isCommentBodySubmittable("Review this wording")).toBe(true);
  });

  test("submit creates exactly one local thread with submitted body and captured anchor", () => {
    const anchor = createTestCommentAnchor({
      documentId: "doc-a",
      exact: "captured anchor",
      range: { from: 1, to: 16 },
    });
    const thread = createLocalCommentThread({
      anchor,
      body: "Use this comment body",
      commentId: "comment-fixed",
      now: "2026-04-29T01:02:03.000Z",
      threadId: "thread-fixed",
    });

    expect(thread.id).toBe("thread-fixed");
    expect(thread.documentId).toBe("doc-a");
    expect(thread.anchor).toBe(anchor);
    expect(thread.comments.length).toBe(1);
    expect(thread.comments[0].body).toBe("Use this comment body");
    expect(thread.comments[0].body).toBe("Use this comment body");
    expect(thread.comments[0].body === "Local comment anchored to this selection.").toBe(false);
  });

  test("factory rejects whitespace body", () => {
    const anchor = createTestCommentAnchor({
      exact: "captured anchor",
      range: { from: 1, to: 16 },
    });
    let threw = false;

    try {
      createLocalCommentThread({ anchor, body: "   " });
    } catch {
      threw = true;
    }

    expect(threw).toBe(true);
  });
});
