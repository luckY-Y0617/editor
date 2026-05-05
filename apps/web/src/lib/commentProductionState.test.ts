import { describe, expect, test } from "../test/harness";
import { createTestCommentAnchor } from "../test/commentTestUtils";
import type { PendingCommentComposer } from "../types/editor";
import {
  beginCommentLoad,
  beginThreadLifecycleAction,
  createThreadLifecycleActionState,
  finishCommentLoadFailure,
  finishCommentLoadSuccess,
  finishThreadLifecycleActionFailure,
  finishThreadLifecycleActionSuccess,
  getCommentLoadState,
  markComposerSubmitFailed,
  markComposerSubmitting,
  shouldAcceptCommentLoadResult,
} from "./commentProductionState";

describe("commentProductionState", () => {
  test("latest active document load result is accepted", () => {
    let statesByDocumentId = beginCommentLoad({}, "doc-a", 1);

    expect(
      shouldAcceptCommentLoadResult({
        activeDocumentId: "doc-a",
        documentId: "doc-a",
        requestId: 1,
        statesByDocumentId,
      }),
    ).toBe(true);

    statesByDocumentId = finishCommentLoadSuccess(statesByDocumentId, "doc-a", 1, "doc-a");

    expect(getCommentLoadState(statesByDocumentId, "doc-a").status).toBe("loaded");
  });

  test("stale same-document load result cannot overwrite newer state", () => {
    let statesByDocumentId = beginCommentLoad({}, "doc-a", 1);
    statesByDocumentId = beginCommentLoad(statesByDocumentId, "doc-a", 2);

    const nextStatesByDocumentId = finishCommentLoadSuccess(
      statesByDocumentId,
      "doc-a",
      1,
      "doc-a",
    );

    expect(nextStatesByDocumentId).toBe(statesByDocumentId);
    expect(getCommentLoadState(nextStatesByDocumentId, "doc-a")).toEqual({
      requestId: 2,
      status: "loading",
    });
  });

  test("inactive document load result is suppressed", () => {
    const statesByDocumentId = beginCommentLoad({}, "doc-a", 1);
    const nextStatesByDocumentId = finishCommentLoadSuccess(
      statesByDocumentId,
      "doc-a",
      1,
      "doc-b",
    );

    expect(nextStatesByDocumentId).toBe(statesByDocumentId);
    expect(
      shouldAcceptCommentLoadResult({
        activeDocumentId: "doc-b",
        documentId: "doc-a",
        requestId: 1,
        statesByDocumentId,
      }),
    ).toBe(false);
  });

  test("load failure stores document-scoped retry error", () => {
    const statesByDocumentId = finishCommentLoadFailure(
      beginCommentLoad({}, "doc-a", 1),
      "doc-a",
      1,
      "doc-a",
      new Error("Network unavailable"),
    );

    expect(getCommentLoadState(statesByDocumentId, "doc-a")).toEqual({
      error: "Network unavailable",
      requestId: 1,
      status: "error",
    });
    expect(getCommentLoadState(statesByDocumentId, "doc-b").status).toBe("idle");
  });

  test("composer submit preserves body and prevents duplicate submit", () => {
    const composer = createPendingComposer();
    const submitting = markComposerSubmitting(composer, "Keep this body");
    const duplicate = markComposerSubmitting(submitting.composer, "Second submit");

    expect(submitting.accepted).toBe(true);
    expect(submitting.composer.body).toBe("Keep this body");
    expect(submitting.composer.isSubmitting).toBe(true);
    expect(duplicate.accepted).toBe(false);
    expect(duplicate.composer.body).toBe("Keep this body");
  });

  test("composer failure keeps captured anchor and recoverable error", () => {
    const composer = createPendingComposer();
    const failedComposer = markComposerSubmitFailed(
      {
        ...composer,
        body: "Retryable body",
        isSubmitting: true,
      },
      "Retryable body",
      new Error("Create failed"),
    );

    expect(failedComposer.anchor).toBe(composer.anchor);
    expect(failedComposer.body).toBe("Retryable body");
    expect(failedComposer.isSubmitting).toBe(false);
    expect(failedComposer.error).toBe("Create failed");
  });

  test("thread lifecycle action is confirmed-update and duplicate guarded", () => {
    const initialState = createThreadLifecycleActionState();
    const started = beginThreadLifecycleAction(initialState, "thread-a");
    const duplicate = beginThreadLifecycleAction(started.state, "thread-a");

    expect(started.accepted).toBe(true);
    expect(started.state.pendingByThreadId["thread-a"]).toBe(true);
    expect(duplicate.accepted).toBe(false);

    const failedState = finishThreadLifecycleActionFailure(
      started.state,
      "thread-a",
      "Resolve failed",
    );

    expect(Boolean(failedState.pendingByThreadId["thread-a"])).toBe(false);
    expect(failedState.errorsByThreadId["thread-a"]).toBe("Resolve failed");

    const retryStarted = beginThreadLifecycleAction(failedState, "thread-a");
    const successState = finishThreadLifecycleActionSuccess(retryStarted.state, "thread-a");

    expect(Boolean(successState.pendingByThreadId["thread-a"])).toBe(false);
    expect(Boolean(successState.errorsByThreadId["thread-a"])).toBe(false);
  });
});

function createPendingComposer(): PendingCommentComposer {
  return {
    anchor: createTestCommentAnchor({
      documentId: "doc-a",
      exact: "alpha",
      range: { from: 1, to: 6 },
    }),
    documentId: "doc-a",
    excerpt: "alpha",
  };
}
