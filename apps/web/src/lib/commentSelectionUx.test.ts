import { describe, expect, test } from "../test/harness";
import {
  createCommentTestState,
  createTestCommentThread,
  findTextRange,
  setCommentThreads,
  setSelection,
} from "../test/commentTestUtils";
import type { CommentSelectionEditor } from "./commentSelectionUx";
import { shouldShowNewCommentButton } from "./commentSelectionUx";

function createEditorMock(state: CommentSelectionEditor["state"]): CommentSelectionEditor {
  return {
    isActive: () => false,
    isEditable: true,
    state,
  } as CommentSelectionEditor;
}

describe("commentSelectionUx", () => {
  test("selecting normal uncommented text exposes the new comment button", () => {
    const text = "alpha beta gamma";
    const range = findTextRange(text, "beta");
    const state = setSelection(createCommentTestState(text), range);

    expect(shouldShowNewCommentButton(createEditorMock(state), false)).toBe(true);
  });

  test("empty cursor selection hides the new comment button", () => {
    const text = "alpha beta gamma";
    const state = createCommentTestState(text);

    expect(shouldShowNewCommentButton(createEditorMock(state), false)).toBe(false);
  });

  test("composer open suppresses the new comment button", () => {
    const text = "alpha beta gamma";
    const state = setSelection(createCommentTestState(text), findTextRange(text, "beta"));

    expect(shouldShowNewCommentButton(createEditorMock(state), true)).toBe(false);
  });

  test("selection fully inside an existing comment hides new comment creation", () => {
    const text = "alpha beta gamma";
    const commentRange = findTextRange(text, "beta");
    let state = createCommentTestState(text);
    state = setCommentThreads(state, [
      createTestCommentThread({ id: "thread-beta", exact: "beta", range: commentRange }),
    ]);
    state = setSelection(state, commentRange);

    expect(shouldShowNewCommentButton(createEditorMock(state), false)).toBe(false);
  });

  test("selection overlapping an existing comment hides new comment creation", () => {
    const text = "alpha beta gamma";
    const commentRange = findTextRange(text, "beta");
    const overlapRange = { from: commentRange.from - 2, to: commentRange.to - 1 };
    let state = createCommentTestState(text);
    state = setCommentThreads(state, [
      createTestCommentThread({ id: "thread-beta", exact: "beta", range: commentRange }),
    ]);
    state = setSelection(state, overlapRange);

    expect(shouldShowNewCommentButton(createEditorMock(state), false)).toBe(false);
  });
});
