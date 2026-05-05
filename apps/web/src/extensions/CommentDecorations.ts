import { Extension, type Editor } from "@tiptap/core";
import type { EditorState } from "@tiptap/pm/state";
import { Plugin, PluginKey } from "@tiptap/pm/state";
import { Decoration, DecorationSet } from "@tiptap/pm/view";
import type {
  CommentAnchorStatus,
  CommentRuntimeAnchorState,
  CommentThread,
  EditorSelectionRange,
} from "../types/editor";
import { matchCommentAnchorText, type AnchorMatchResult } from "../lib/commentAnchorMatching";
import { relocateCommentAnchor, type AnchorRelocationResult } from "../lib/commentAnchorRelocation";

export type RuntimeCommentAnchor = {
  threadId: string;
  from: number;
  to: number;
};

export type CommentDecorationPluginState = {
  decorations: DecorationSet;
  rangesByThreadId: Record<string, RuntimeCommentAnchor>;
  anchorStatusByThreadId: Record<string, CommentAnchorStatus>;
  matchResultByThreadId: Record<string, AnchorMatchResult>;
  relocationResultByThreadId: Record<string, AnchorRelocationResult>;
  threadsById: Record<string, CommentThread>;
  activeThreadId: string | null;
};

type CommentDecorationMeta = {
  type: "setThreads";
  activeThreadId: string | null;
  threads: CommentThread[];
};

export const knowledgeCommentDecorationsPluginKey =
  new PluginKey<CommentDecorationPluginState>("knowledge-comment-decorations");

export const CommentDecorations = Extension.create({
  name: "knowledgeCommentDecorations",

  addProseMirrorPlugins() {
    return [createCommentDecorationsPlugin()];
  },
});

export function createCommentDecorationsPlugin() {
  return new Plugin<CommentDecorationPluginState>({
    key: knowledgeCommentDecorationsPluginKey,
    state: {
      init() {
        return createEmptyPluginState();
      },
      apply(tr, previous) {
        const meta = tr.getMeta(knowledgeCommentDecorationsPluginKey) as CommentDecorationMeta | undefined;
        const mappedDecorations = tr.docChanged
          ? previous.decorations.map(tr.mapping, tr.doc)
          : previous.decorations;
        const mappedRangesByThreadId = tr.docChanged
          ? readRangesFromDecorationSet(mappedDecorations)
          : previous.rangesByThreadId;

        if (meta?.type === "setThreads") {
          return createPluginStateFromThreads(
            tr.doc,
            meta.threads,
            mappedRangesByThreadId,
            previous.anchorStatusByThreadId,
            previous.relocationResultByThreadId,
            meta.activeThreadId,
            true,
          );
        }

        if (!tr.docChanged) {
          return previous;
        }

        return createPluginStateFromThreads(
          tr.doc,
          Object.values(previous.threadsById),
          mappedRangesByThreadId,
          previous.anchorStatusByThreadId,
          previous.relocationResultByThreadId,
          previous.activeThreadId,
          false,
        );
      },
    },
    props: {
      decorations(state) {
        return knowledgeCommentDecorationsPluginKey.getState(state)?.decorations ?? DecorationSet.empty;
      },
    },
  });
}

export function syncCommentDecorations(
  editor: Editor,
  threads: CommentThread[],
  activeThreadId: string | null,
) {
  const meta: CommentDecorationMeta = {
    type: "setThreads",
    activeThreadId,
    threads,
  };

  editor.view.dispatch(
    editor.state.tr
      .setMeta(knowledgeCommentDecorationsPluginKey, meta)
      .setMeta("addToHistory", false),
  );
}

export function getMappedCommentRange(
  state: EditorState,
  threadId: string,
): EditorSelectionRange | null {
  const range = knowledgeCommentDecorationsPluginKey.getState(state)?.rangesByThreadId[threadId];

  if (!range) {
    return null;
  }

  return {
    from: range.from,
    to: range.to,
  };
}

export function findCommentThreadsOverlappingRange(
  state: EditorState,
  from: number,
  to: number,
): RuntimeCommentAnchor[] {
  const pluginState = knowledgeCommentDecorationsPluginKey.getState(state);

  if (!pluginState || from >= to) {
    return [];
  }

  return Object.values(pluginState.rangesByThreadId).filter((range) =>
    rangesOverlap(from, to, range.from, range.to),
  );
}

export function isSelectionInsideExistingCommentRange(
  state: EditorState,
  from: number,
  to: number,
) {
  const pluginState = knowledgeCommentDecorationsPluginKey.getState(state);

  if (!pluginState || from >= to) {
    return false;
  }

  return Object.values(pluginState.rangesByThreadId).some((range) => from >= range.from && to <= range.to);
}

export function doesSelectionOverlapExistingCommentRange(
  state: EditorState,
  from: number,
  to: number,
) {
  return findCommentThreadsOverlappingRange(state, from, to).length > 0;
}

export function getCommentRuntimeAnchorState(state: EditorState): CommentRuntimeAnchorState {
  const pluginState = knowledgeCommentDecorationsPluginKey.getState(state);

  return {
    anchorStatusByThreadId: {
      ...(pluginState?.anchorStatusByThreadId ?? {}),
    },
    matchResultByThreadId: {
      ...(pluginState?.matchResultByThreadId ?? {}),
    },
    relocationResultByThreadId: {
      ...(pluginState?.relocationResultByThreadId ?? {}),
    },
  };
}

function rangesOverlap(fromA: number, toA: number, fromB: number, toB: number) {
  return fromA < toB && toA > fromB;
}

function createEmptyPluginState(): CommentDecorationPluginState {
  return {
    decorations: DecorationSet.empty,
    rangesByThreadId: {},
    anchorStatusByThreadId: {},
    matchResultByThreadId: {},
    relocationResultByThreadId: {},
    threadsById: {},
    activeThreadId: null,
  };
}

function createPluginStateFromThreads(
  doc: EditorState["doc"],
  threads: CommentThread[],
  currentRangesByThreadId: Record<string, RuntimeCommentAnchor>,
  previousAnchorStatusByThreadId: Record<string, CommentAnchorStatus>,
  previousRelocationResultByThreadId: Record<string, AnchorRelocationResult>,
  activeThreadId: string | null,
  allowRelocationForNewThreads: boolean,
): CommentDecorationPluginState {
  const decorations: Decoration[] = [];
  const anchorStatusByThreadId: Record<string, CommentAnchorStatus> = {};
  const matchResultByThreadId: Record<string, AnchorMatchResult> = {};
  const relocationResultByThreadId: Record<string, AnchorRelocationResult> = {};
  const threadsById: Record<string, CommentThread> = {};

  for (const thread of threads) {
    threadsById[thread.id] = thread;

    if (thread.anchor.kind !== "tiptap.textRange") {
      anchorStatusByThreadId[thread.id] = "orphaned";
      matchResultByThreadId[thread.id] = {
        status: "orphaned",
        confidence: "none",
        reason: "invalid_range",
      };
      continue;
    }

    const previousAnchorStatus = previousAnchorStatusByThreadId[thread.id];
    const currentRange: RuntimeCommentAnchor | null = currentRangesByThreadId[thread.id] ?? null;
    let relocationResult = previousRelocationResultByThreadId[thread.id];
    let range: RuntimeCommentAnchor | null = currentRange;

    if (!currentRange && allowRelocationForNewThreads && previousAnchorStatus === undefined) {
      relocationResult = relocateCommentAnchor(doc, thread.anchor);
      range = createRuntimeRangeFromRelocation(thread.id, relocationResult);
    }
    const validation = validateRuntimeRange(doc, thread, range);

    anchorStatusByThreadId[thread.id] = validation.matchResult.status;
    matchResultByThreadId[thread.id] = validation.matchResult;

    if (relocationResult) {
      relocationResultByThreadId[thread.id] = relocationResult;
    }

    if (!validation.range) {
      continue;
    }

    decorations.push(
      Decoration.inline(
        validation.range.from,
        validation.range.to,
        {
          class: getCommentDecorationClassName({
            anchorStatus: validation.matchResult.status,
            confidence: validation.matchResult.confidence,
            selected: thread.id === activeThreadId,
            threadStatus: thread.status,
          }),
          "data-comment-thread-id": thread.id,
          "data-thread-id": thread.id,
        },
        {
          inclusiveEnd: false,
          inclusiveStart: false,
          threadId: thread.id,
        },
      ),
    );
  }

  const decorationSet = DecorationSet.create(doc, decorations);

  return {
    decorations: decorationSet,
    rangesByThreadId: readRangesFromDecorationSet(decorationSet),
    anchorStatusByThreadId,
    matchResultByThreadId,
    relocationResultByThreadId,
    threadsById,
    activeThreadId,
  };
}

function createRuntimeRangeFromRelocation(
  threadId: string,
  relocationResult: AnchorRelocationResult | undefined,
): RuntimeCommentAnchor | null {
  if (!relocationResult?.range) {
    return null;
  }

  return {
    threadId,
    from: relocationResult.range.from,
    to: relocationResult.range.to,
  };
}

function getCommentDecorationClassName({
  anchorStatus,
  confidence,
  selected,
  threadStatus,
}: {
  anchorStatus: AnchorMatchResult["status"];
  confidence: AnchorMatchResult["confidence"];
  selected: boolean;
  threadStatus: CommentThread["status"];
}) {
  return [
    "knowledge-comment-highlight",
    threadStatus === "resolved"
      ? "knowledge-comment-highlight--resolved"
      : "knowledge-comment-highlight--open",
    anchorStatus === "stale"
      ? "knowledge-comment-highlight--stale-anchor"
      : "knowledge-comment-highlight--active-anchor",
    `knowledge-comment-highlight--confidence-${confidence}`,
    selected ? "knowledge-comment-highlight--selected" : "",
  ]
    .filter(Boolean)
    .join(" ");
}

function readRangesFromDecorationSet(decorations: DecorationSet): Record<string, RuntimeCommentAnchor> {
  const rangesByThreadId: Record<string, RuntimeCommentAnchor> = {};

  for (const decoration of decorations.find()) {
    const threadId = getDecorationThreadId(decoration);

    if (!threadId || decoration.from >= decoration.to) {
      continue;
    }

    rangesByThreadId[threadId] = {
      threadId,
      from: decoration.from,
      to: decoration.to,
    };
  }

  return rangesByThreadId;
}

function getDecorationThreadId(decoration: Decoration) {
  const spec = decoration.spec as { threadId?: unknown };

  return typeof spec.threadId === "string" ? spec.threadId : null;
}

function getSafeRange(
  doc: EditorState["doc"],
  range: RuntimeCommentAnchor | null,
): RuntimeCommentAnchor | null {
  if (!range) {
    return null;
  }

  if (
    !Number.isInteger(range.from) ||
    !Number.isInteger(range.to) ||
    range.from < 1 ||
    range.to <= range.from ||
    range.to > doc.content.size
  ) {
    return null;
  }

  return range;
}

function validateRuntimeRange(
  doc: EditorState["doc"],
  thread: CommentThread,
  range: RuntimeCommentAnchor | null,
): { matchResult: AnchorMatchResult; range: RuntimeCommentAnchor | null } {
  const safeRange = getSafeRange(doc, range);

  if (!safeRange) {
    return {
      matchResult: matchCommentAnchorText({
        mappedText: "",
        normalizedExact: thread.anchor.quote.normalizedExact,
        rangeValid: false,
      }),
      range: null,
    };
  }

  const currentText = doc.textBetween(safeRange.from, safeRange.to, " ");
  const matchResult = matchCommentAnchorText({
    mappedText: currentText,
    normalizedExact: thread.anchor.quote.normalizedExact,
    rangeValid: true,
  });

  return {
    matchResult,
    range: matchResult.status === "orphaned" ? null : safeRange,
  };
}
