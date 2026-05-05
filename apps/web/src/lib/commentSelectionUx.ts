import type { Editor } from "@tiptap/react";
import type { EditorState } from "@tiptap/pm/state";
import { doesSelectionOverlapExistingCommentRange } from "../extensions/CommentDecorations";

export type CommentSelectionEditor = Pick<Editor, "isActive" | "isEditable" | "state">;

export function shouldShowNewCommentButton(
  editor: CommentSelectionEditor,
  isCommentComposerOpen: boolean,
) {
  if (isCommentComposerOpen || !editor.isEditable) {
    return false;
  }

  const { from, to } = editor.state.selection;

  if (!isTextSelectionNonEmpty(editor.state, from, to)) {
    return false;
  }

  if (editor.isActive("codeBlock") || selectionHasAncestorType(editor.state, "codeBlock")) {
    return false;
  }

  return !doesSelectionOverlapExistingCommentRange(editor.state, from, to);
}

export function isTextSelectionNonEmpty(state: EditorState, from = state.selection.from, to = state.selection.to) {
  return from !== to && state.doc.textBetween(from, to, " ").trim().length > 0;
}

function selectionHasAncestorType(state: EditorState, typeName: string) {
  const { $from, $to } = state.selection;

  return resolvedPosHasAncestorType($from, typeName) || resolvedPosHasAncestorType($to, typeName);
}

function resolvedPosHasAncestorType(
  resolvedPos: EditorState["selection"]["$from"],
  typeName: string,
) {
  for (let depth = resolvedPos.depth; depth > 0; depth -= 1) {
    if (resolvedPos.node(depth).type.name === typeName) {
      return true;
    }
  }

  return false;
}
