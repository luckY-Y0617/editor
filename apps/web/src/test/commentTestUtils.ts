import { Schema } from "@tiptap/pm/model";
import { EditorState, TextSelection } from "@tiptap/pm/state";
import type { Decoration } from "@tiptap/pm/view";
import {
  createCommentDecorationsPlugin,
  knowledgeCommentDecorationsPluginKey,
  type CommentDecorationPluginState,
} from "../extensions/CommentDecorations";
import { normalizePlainTextV1 } from "../lib/commentAnchorMatching";
import type { CommentAnchorV1, CommentThread, EditorSelectionRange } from "../types/editor";

export const commentTestSchema = new Schema({
  nodes: {
    doc: {
      content: "block+",
    },
    paragraph: {
      attrs: {
        blockId: { default: null },
      },
      content: "inline*",
      group: "block",
      parseDOM: [{ tag: "p" }],
      toDOM: () => ["p", 0],
    },
    text: {
      group: "inline",
    },
  },
});

export function createCommentTestState(text: string, blockId?: string) {
  return EditorState.create({
    doc: commentTestSchema.node("doc", null, [
      commentTestSchema.node(
        "paragraph",
        blockId ? { blockId } : null,
        text ? [commentTestSchema.text(text)] : undefined,
      ),
    ]),
    plugins: [createCommentDecorationsPlugin()],
    schema: commentTestSchema,
  });
}

export function findTextRange(text: string, exact: string): EditorSelectionRange {
  const index = text.indexOf(exact);

  if (index < 0) {
    throw new Error(`Unable to find text range for ${exact}`);
  }

  return {
    from: index + 1,
    to: index + exact.length + 1,
  };
}

export function setSelection(state: EditorState, range: EditorSelectionRange) {
  return state.apply(state.tr.setSelection(TextSelection.create(state.doc, range.from, range.to)));
}

export function setCommentThreads(
  state: EditorState,
  threads: CommentThread[],
  activeThreadId: string | null = null,
) {
  return state.apply(
    state.tr.setMeta(knowledgeCommentDecorationsPluginKey, {
      type: "setThreads",
      activeThreadId,
      threads,
    }),
  );
}

export function getCommentPluginState(state: EditorState): CommentDecorationPluginState {
  const pluginState = knowledgeCommentDecorationsPluginKey.getState(state);

  if (!pluginState) {
    throw new Error("Comment decoration plugin state is missing");
  }

  return pluginState;
}

export function getDecorationAttrs(decoration: Decoration): Record<string, string> {
  return ((decoration as Decoration & { type: { attrs: Record<string, string> } }).type.attrs);
}

export function createTestCommentThread({
  documentId = "doc-test",
  endBlockId,
  exact,
  id,
  range,
  resolved = false,
  startBlockId,
}: {
  documentId?: string;
  endBlockId?: string;
  exact: string;
  id: string;
  range: EditorSelectionRange;
  resolved?: boolean;
  startBlockId?: string;
}): CommentThread {
  const now = "2026-04-29T00:00:00.000Z";
  const anchor = createTestCommentAnchor({ documentId, endBlockId, exact, range, startBlockId });

  return {
    id,
    documentId,
    status: resolved ? "resolved" : "open",
    anchorStatus: "active",
    anchor,
    comments: [
      {
        id: `comment-${id}`,
        threadId: id,
        body: `Body for ${id}`,
        author: {
          id: "local-user",
          name: "Local User",
        },
        createdAt: now,
        updatedAt: null,
        deletedAt: null,
      },
    ],
    createdAt: now,
    updatedAt: now,
    resolvedAt: resolved ? now : null,
  };
}

export function createTestCommentAnchor({
  documentId = "doc-test",
  endBlockId,
  exact,
  range,
  startBlockId,
}: {
  documentId?: string;
  endBlockId?: string;
  exact: string;
  range: EditorSelectionRange;
  startBlockId?: string;
}): CommentAnchorV1 {
  return {
    schema: "northstar.commentAnchor.v1",
    kind: "tiptap.textRange",
    documentId,
    baseRevision: 0,
    pm: range,
    block: {
      start: {
        ...(startBlockId ? { blockId: startBlockId } : {}),
        path: [0],
        nodeType: "paragraph",
        textOffset: Math.max(0, range.from - 1),
      },
      end: {
        ...(endBlockId ? { blockId: endBlockId } : {}),
        path: [0],
        nodeType: "paragraph",
        textOffset: Math.max(0, range.to - 1),
      },
    },
    quote: {
      exact,
      prefix: "",
      suffix: "",
      normalizedExact: normalizePlainTextV1(exact),
      normalizer: "northstar.plainText.v1",
    },
    display: {
      excerpt: normalizePlainTextV1(exact),
    },
  };
}
