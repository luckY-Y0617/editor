import { TextSelection } from "@tiptap/pm/state";
import { describe, expect, test } from "../test/harness";
import {
  createCommentTestState,
  createTestCommentThread,
  findTextRange,
  getCommentPluginState,
  getDecorationAttrs,
  setCommentThreads,
} from "../test/commentTestUtils";

describe("knowledge-comment-decorations", () => {
  test("setThreads creates one decoration per non-orphaned thread", () => {
    const text = "alpha beta gamma";
    const state = setCommentThreads(createCommentTestState(text), [
      createTestCommentThread({ id: "thread-alpha", exact: "alpha", range: findTextRange(text, "alpha") }),
      createTestCommentThread({ id: "thread-beta", exact: "beta", range: findTextRange(text, "beta") }),
    ]);
    const pluginState = getCommentPluginState(state);

    expect(pluginState.decorations.find().length).toBe(2);
    expect(Object.keys(pluginState.rangesByThreadId).sort()).toEqual(["thread-alpha", "thread-beta"]);
  });

  test("multiple active exact threads render together and activeThreadId only selects one", () => {
    const text = "alpha beta gamma";
    const state = setCommentThreads(
      createCommentTestState(text),
      [
        createTestCommentThread({ id: "thread-alpha", exact: "alpha", range: findTextRange(text, "alpha") }),
        createTestCommentThread({ id: "thread-beta", exact: "beta", range: findTextRange(text, "beta") }),
        createTestCommentThread({ id: "thread-gamma", exact: "gamma", range: findTextRange(text, "gamma") }),
      ],
      "thread-beta",
    );
    const decorations = getCommentPluginState(state).decorations.find();
    const selectedDecorations = decorations.filter((decoration) =>
      getDecorationAttrs(decoration).class.includes("knowledge-comment-highlight--selected"),
    );

    expect(decorations.length).toBe(3);
    expect(selectedDecorations.length).toBe(1);
    expect(getDecorationAttrs(selectedDecorations[0])["data-thread-id"]).toBe("thread-beta");
  });

  test("resolved thread still renders with resolved modifier", () => {
    const text = "alpha beta gamma";
    const state = setCommentThreads(createCommentTestState(text), [
      createTestCommentThread({
        id: "thread-beta",
        exact: "beta",
        range: findTextRange(text, "beta"),
        resolved: true,
      }),
    ]);
    const decoration = getCommentPluginState(state).decorations.find()[0];

    expect(getDecorationAttrs(decoration).class).toContain("knowledge-comment-highlight--resolved");
  });

  test("stale thread renders with stale modifier", () => {
    const text = "alpha beta gamma";
    const state = setCommentThreads(createCommentTestState(text), [
      createTestCommentThread({
        id: "thread-beta",
        exact: "unrelated replacement",
        range: findTextRange(text, "beta"),
      }),
    ]);
    const pluginState = getCommentPluginState(state);
    const decoration = pluginState.decorations.find()[0];

    expect(pluginState.matchResultByThreadId["thread-beta"].status).toBe("stale");
    expect(getDecorationAttrs(decoration).class).toContain("knowledge-comment-highlight--stale-anchor");
  });

  test("orphaned thread does not render a normal highlight", () => {
    const state = setCommentThreads(createCommentTestState("alpha beta gamma"), [
      createTestCommentThread({
        id: "thread-invalid",
        exact: "missing",
        range: { from: 1, to: 999 },
      }),
    ]);
    const pluginState = getCommentPluginState(state);

    expect(pluginState.decorations.find().length).toBe(0);
    expect(pluginState.anchorStatusByThreadId["thread-invalid"]).toBe("orphaned");
    expect(pluginState.matchResultByThreadId["thread-invalid"].reason).toBe("invalid_range");
  });

  test("DecorationSet.map keeps runtime range following text inserted before the comment", () => {
    const text = "alpha beta gamma";
    const betaRange = findTextRange(text, "beta");
    let state = setCommentThreads(createCommentTestState(text), [
      createTestCommentThread({ id: "thread-beta", exact: "beta", range: betaRange }),
    ]);

    state = state.apply(state.tr.insertText("new ", 1));

    const pluginState = getCommentPluginState(state);
    const range = pluginState.rangesByThreadId["thread-beta"];

    expect(state.doc.textBetween(range.from, range.to, " ")).toBe("beta");
    expect(pluginState.matchResultByThreadId["thread-beta"].status).toBe("active");
  });

  test("setThreads initializes stale pm snapshots through relocation", () => {
    const originalText = "alpha beta gamma";
    const currentText = "alpha new beta gamma";
    const betaRange = findTextRange(originalText, "beta");
    const state = setCommentThreads(createCommentTestState(currentText, "blk_relocate01"), [
      createTestCommentThread({
        id: "thread-beta",
        exact: "beta",
        range: betaRange,
        startBlockId: "blk_relocate01",
        endBlockId: "blk_relocate01",
      }),
    ]);
    const pluginState = getCommentPluginState(state);
    const range = pluginState.rangesByThreadId["thread-beta"];

    expect(state.doc.textBetween(range.from, range.to, " ")).toBe("beta");
    expect(pluginState.matchResultByThreadId["thread-beta"].status).toBe("active");
    expect(pluginState.relocationResultByThreadId["thread-beta"].reason).toBe("block_exact_quote");
  });

  test("relocation result is load-time only while transactions map the runtime range", () => {
    const originalText = "alpha beta gamma";
    const currentText = "alpha new beta gamma";
    const betaRange = findTextRange(originalText, "beta");
    let state = setCommentThreads(createCommentTestState(currentText, "blk_relocate02"), [
      createTestCommentThread({
        id: "thread-beta",
        exact: "beta",
        range: betaRange,
        startBlockId: "blk_relocate02",
        endBlockId: "blk_relocate02",
      }),
    ]);
    const initialPluginState = getCommentPluginState(state);
    const initialRelocationResult = initialPluginState.relocationResultByThreadId["thread-beta"];

    state = state.apply(state.tr.insertText("preface ", 1));

    const pluginState = getCommentPluginState(state);
    const range = pluginState.rangesByThreadId["thread-beta"];

    expect(pluginState.relocationResultByThreadId["thread-beta"]).toBe(initialRelocationResult);
    expect(state.doc.textBetween(range.from, range.to, " ")).toBe("beta");
    expect(pluginState.matchResultByThreadId["thread-beta"].status).toBe("active");
  });

  test("deleting the commented text makes the thread orphaned", () => {
    const text = "alpha beta gamma";
    const betaRange = findTextRange(text, "beta");
    let state = setCommentThreads(createCommentTestState(text), [
      createTestCommentThread({ id: "thread-beta", exact: "beta", range: betaRange }),
    ]);

    state = state.apply(state.tr.delete(betaRange.from, betaRange.to));

    const pluginState = getCommentPluginState(state);

    expect(pluginState.decorations.find().length).toBe(0);
    expect(pluginState.anchorStatusByThreadId["thread-beta"]).toBe("orphaned");
    expect(pluginState.matchResultByThreadId["thread-beta"].reason).toBe("invalid_range");
  });

  test("matchResultByThreadId is populated for every thread", () => {
    const text = "alpha beta gamma";
    const state = setCommentThreads(createCommentTestState(text), [
      createTestCommentThread({ id: "thread-alpha", exact: "alpha", range: findTextRange(text, "alpha") }),
      createTestCommentThread({ id: "thread-invalid", exact: "missing", range: { from: 1, to: 999 } }),
    ]);
    const pluginState = getCommentPluginState(state);

    expect(Object.keys(pluginState.matchResultByThreadId).sort()).toEqual([
      "thread-alpha",
      "thread-invalid",
    ]);
  });

  test("every rendered decoration includes base class and thread data attributes", () => {
    const text = "alpha beta gamma";
    const state = setCommentThreads(createCommentTestState(text), [
      createTestCommentThread({ id: "thread-alpha", exact: "alpha", range: findTextRange(text, "alpha") }),
    ]);
    const attrs = getDecorationAttrs(getCommentPluginState(state).decorations.find()[0]);

    expect(attrs.class).toContain("knowledge-comment-highlight");
    expect(attrs["data-comment-thread-id"]).toBe("thread-alpha");
    expect(attrs["data-thread-id"]).toBe("thread-alpha");
  });

  test("mapped stale ranges remain selectable and tracked", () => {
    const text = "alpha long anchor phrase gamma";
    const anchorRange = findTextRange(text, "long anchor phrase");
    let state = setCommentThreads(createCommentTestState(text), [
      createTestCommentThread({ id: "thread-anchor", exact: "long anchor phrase", range: anchorRange }),
    ]);

    state = state.apply(
      state.tr
        .setSelection(TextSelection.create(state.doc, anchorRange.from, anchorRange.to))
        .insertText("zz"),
    );

    const pluginState = getCommentPluginState(state);

    expect(pluginState.anchorStatusByThreadId["thread-anchor"]).toBe("stale");
    expect(Object.keys(pluginState.rangesByThreadId)).toEqual(["thread-anchor"]);
    expect(pluginState.decorations.find().length).toBe(1);
  });
});
