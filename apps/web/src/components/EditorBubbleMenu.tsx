import type { Editor } from "@tiptap/react";
import { useEditorState } from "@tiptap/react";
import { BubbleMenu } from "@tiptap/react/menus";
import {
  Bold,
  Code2,
  Eraser,
  Highlighter,
  Italic,
  Link2,
  MessageSquarePlus,
  Strikethrough,
  Unlink,
} from "lucide-react";
import { useEffect, useState } from "react";
import type { LucideIcon } from "lucide-react";
import { LinkEditPopover } from "./LinkEditPopover";
import { shouldShowNewCommentButton } from "../lib/commentSelectionUx";
import { clearInlineFormatting } from "../lib/editorCommands";
import { normalizeUrl } from "../lib/url";
import type { EditorSelectionRange } from "../types/editor";

type BubbleMenuButtonProps = {
  active?: boolean;
  icon: LucideIcon;
  label: string;
  onClick: () => void;
};

type EditorBubbleMenuProps = {
  editor: Editor | null;
  isBlockDragging?: boolean;
  isBlockMenuOpen?: boolean;
  isCommentComposerOpen?: boolean;
  onOpenCommentComposer?: () => void;
};

export function EditorBubbleMenu({
  editor,
  isBlockDragging = false,
  isBlockMenuOpen = false,
  isCommentComposerOpen = false,
  onOpenCommentComposer,
}: EditorBubbleMenuProps) {
  const [linkEditor, setLinkEditor] = useState<{
    href: string;
    range: EditorSelectionRange;
  } | null>(null);
  const state = useEditorState({
    editor,
    selector: ({ editor: currentEditor }) => ({
      bold: currentEditor?.isActive("bold") ?? false,
      italic: currentEditor?.isActive("italic") ?? false,
      strike: currentEditor?.isActive("strike") ?? false,
      code: currentEditor?.isActive("code") ?? false,
      highlight: currentEditor?.isActive("highlight") ?? false,
      link: currentEditor?.isActive("link") ?? false,
      canOpenCommentComposer: currentEditor
        ? shouldShowNewCommentButton(currentEditor, isCommentComposerOpen)
        : false,
    }),
  });
  const activeState = state ?? {
    bold: false,
    italic: false,
    strike: false,
    code: false,
    highlight: false,
    link: false,
    canOpenCommentComposer: false,
  };

  useEffect(() => {
    if (isBlockDragging || isBlockMenuOpen || isCommentComposerOpen) {
      setLinkEditor(null);
    }
  }, [isBlockDragging, isBlockMenuOpen, isCommentComposerOpen]);

  if (!editor) {
    return null;
  }

  const closeLinkEditor = () => setLinkEditor(null);

  if (isBlockDragging || isBlockMenuOpen || isCommentComposerOpen) {
    return null;
  }

  return (
    <BubbleMenu
      className="relative z-[80] flex items-center gap-0.5 rounded-[var(--northstar-radius-panel)] border border-[var(--northstar-border)] bg-white/95 p-1 shadow-[var(--northstar-shadow-popover)] backdrop-blur"
      editor={editor}
      options={{
        placement: "top",
        offset: 8,
        shift: true,
      }}
      pluginKey="knowledge-inline-bubble-menu"
      shouldShow={({ editor: currentEditor, from, to }) => {
        if (!currentEditor.isEditable || isBlockDragging || isBlockMenuOpen || isCommentComposerOpen || from === to) {
          return false;
        }

        if (currentEditor.isActive("codeBlock") || selectionHasAncestorType(currentEditor, "codeBlock")) {
          return false;
        }

        return currentEditor.state.doc.textBetween(from, to, " ").trim().length > 0;
      }}
    >
      <BubbleMenuButton
        active={activeState.bold}
        icon={Bold}
        label="加粗"
        onClick={() => editor.chain().focus().toggleBold().run()}
      />
      <BubbleMenuButton
        active={activeState.italic}
        icon={Italic}
        label="斜体"
        onClick={() => editor.chain().focus().toggleItalic().run()}
      />
      <BubbleMenuButton
        active={activeState.strike}
        icon={Strikethrough}
        label="删除线"
        onClick={() => editor.chain().focus().toggleStrike().run()}
      />
      <BubbleMenuButton
        active={activeState.code}
        icon={Code2}
        label="行内代码"
        onClick={() => editor.chain().focus().toggleCode().run()}
      />
      <BubbleMenuButton
        active={activeState.highlight}
        icon={Highlighter}
        label="高亮"
        onClick={() => editor.chain().focus().toggleHighlight().run()}
      />
      {onOpenCommentComposer && activeState.canOpenCommentComposer ? (
        <>
          <span className="mx-0.5 h-4 w-px bg-[var(--northstar-border)]" />
          <BubbleMenuButton
            icon={MessageSquarePlus}
            label="Add comment"
            onClick={() => {
              closeLinkEditor();
              onOpenCommentComposer();
            }}
          />
        </>
      ) : null}
      <span className="mx-0.5 h-4 w-px bg-[var(--northstar-border)]" />
      <BubbleMenuButton
        icon={Eraser}
        label="清除格式"
        onClick={() => {
          clearInlineFormatting(editor);
          closeLinkEditor();
        }}
      />
      <span className="mx-0.5 h-4 w-px bg-[var(--northstar-border)]" />
      <BubbleMenuButton
        active={activeState.link}
        icon={Link2}
        label={activeState.link ? "编辑链接" : "添加链接"}
        onClick={() => setLinkEditor(createLinkEditorState(editor))}
      />
      {activeState.link ? (
        <BubbleMenuButton
          icon={Unlink}
          label="移除链接"
          onClick={() => {
            removeLink(editor, createLinkEditorState(editor).range);
            closeLinkEditor();
          }}
        />
      ) : null}
      {linkEditor ? (
        <LinkEditPopover
          className="absolute left-0 top-[calc(100%+6px)] z-[90]"
          initialHref={linkEditor.href}
          onCancel={closeLinkEditor}
          onRemove={() => {
            removeLink(editor, linkEditor.range);
            closeLinkEditor();
          }}
          onSubmit={(href) => {
            setLink(editor, href, linkEditor.range);
            closeLinkEditor();
          }}
          showRemove={activeState.link}
        />
      ) : null}
    </BubbleMenu>
  );
}

function BubbleMenuButton({ active, icon: Icon, label, onClick }: BubbleMenuButtonProps) {
  return (
    <button
      aria-label={label}
      className={[
        "grid h-7 w-7 place-items-center rounded-[var(--northstar-radius-control)] text-[var(--northstar-text-muted)] transition",
        active ? "bg-[var(--northstar-accent-tint)] text-[var(--northstar-primary)] shadow-[inset_0_0_0_1px_rgba(79,124,255,0.16)]" : "hover:bg-[var(--northstar-primary-soft)] hover:text-[var(--northstar-primary)]",
      ].join(" ")}
      onMouseDown={(event) => event.preventDefault()}
      onClick={onClick}
      title={label}
      type="button"
    >
      <Icon className="h-3.5 w-3.5" />
    </button>
  );
}

function selectionHasAncestorType(editor: Editor, typeName: string) {
  const { $from, $to } = editor.state.selection;

  return resolvedPosHasAncestorType($from, typeName) || resolvedPosHasAncestorType($to, typeName);
}

function resolvedPosHasAncestorType(
  resolvedPos: Editor["state"]["selection"]["$from"],
  typeName: string,
) {
  for (let depth = resolvedPos.depth; depth > 0; depth -= 1) {
    if (resolvedPos.node(depth).type.name === typeName) {
      return true;
    }
  }

  return false;
}

function createLinkEditorState(editor: Editor) {
  const { from, to } = editor.state.selection;
  const href = (editor.getAttributes("link").href as string | undefined) ?? "";

  // The popover input takes focus, so save the range before opening it.
  return {
    href,
    range: { from, to },
  };
}

function setLink(editor: Editor, href: string, range: EditorSelectionRange) {
  const normalizedHref = normalizeUrl(href, { allowedSchemes: ["http", "https", "mailto", "tel"] });
  const safeRange = getSafeRange(editor, range);

  if (!safeRange) {
    return;
  }

  if (!normalizedHref) {
    removeLink(editor, safeRange);
    return;
  }

  editor
    .chain()
    .focus()
    .setTextSelection(safeRange)
    .extendMarkRange("link")
    .setLink({ href: normalizedHref })
    .run();
}

function removeLink(editor: Editor, range: EditorSelectionRange) {
  const safeRange = getSafeRange(editor, range);

  if (!safeRange) {
    return;
  }

  editor.chain().focus().setTextSelection(safeRange).extendMarkRange("link").unsetLink().run();
}

function getSafeRange(editor: Editor, range: EditorSelectionRange): EditorSelectionRange | null {
  const docSize = editor.state.doc.content.size;

  if (
    !Number.isInteger(range.from) ||
    !Number.isInteger(range.to) ||
    range.from < 0 ||
    range.to < range.from ||
    range.to > docSize
  ) {
    return null;
  }

  return range;
}
