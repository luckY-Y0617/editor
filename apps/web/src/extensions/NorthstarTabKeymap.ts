import { Extension, type Editor } from "@tiptap/core";

export const NorthstarTabKeymap = Extension.create({
  name: "northstarTabKeymap",
  priority: 10,

  addKeyboardShortcuts() {
    return {
      Tab: () => preventFocusEscape(this.editor),
      "Shift-Tab": () => preventFocusEscape(this.editor),
    };
  },
});

function preventFocusEscape(editor: Editor) {
  if (editor.isActive("codeBlock") || editor.isActive("listItem") || editor.isActive("table")) {
    return false;
  }

  editor.commands.focus();

  return true;
}
