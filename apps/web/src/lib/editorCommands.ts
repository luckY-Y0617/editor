import type { Editor } from "@tiptap/react";

export function clearInlineFormatting(editor: Editor) {
  editor.chain().focus().unsetAllMarks().run();
}

export function clearBlockFormatting(editor: Editor) {
  if (editor.isActive("image") || editor.isActive("imageBlock")) {
    return;
  }

  if (editor.isActive("heading")) {
    const chain = editor.chain().focus().unsetAllMarks().setParagraph();

    if (canSetTextAlignLeft(editor)) {
      chain.setTextAlign("left");
    }

    chain.run();
    return;
  }

  const chain = editor.chain().focus().unsetAllMarks();

  if (canSetTextAlignLeft(editor)) {
    chain.setTextAlign("left");
  }

  chain.run();
}

function canSetTextAlignLeft(editor: Editor) {
  return editor.can().setTextAlign("left");
}
