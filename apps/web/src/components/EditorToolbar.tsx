import type { Editor } from "@tiptap/react";
import { useEditorState } from "@tiptap/react";
import { useEffect, useState } from "react";
import {
  AlignCenter,
  AlignLeft,
  AlignRight,
  Bold,
  Code2,
  Eraser,
  Heading2,
  Heading3,
  ImageIcon,
  Italic,
  Link2,
  List,
  ListChecks,
  ListOrdered,
  Quote,
  Redo2,
  SquareCode,
  Strikethrough,
  Undo2,
  ChevronDown,
} from "lucide-react";
import { ImageUrlPopover } from "./ImageUrlPopover";
import { InsertTableButton } from "./TableToolbarGroup";
import { ToolbarButton, ToolbarGroup, ToolbarSeparator } from "./ToolbarControls";
import { clearBlockFormatting } from "../lib/editorCommands";
import { normalizeUrl } from "../lib/url";

type EditorToolbarProps = {
  editor: Editor | null;
  isBlockDragging?: boolean;
  isBlockMenuOpen?: boolean;
  onImagePopoverOpenChange?: (isOpen: boolean) => void;
};

type ToolbarBlockFormat =
  | "blockquote"
  | "bulletList"
  | "codeBlock"
  | "heading2"
  | "heading3"
  | "orderedList"
  | "paragraph"
  | "taskList";

type ToolbarActiveState = {
  blockquote: boolean;
  bulletList: boolean;
  codeBlock: boolean;
  h2: boolean;
  h3: boolean;
  orderedList: boolean;
  taskList: boolean;
};

export function EditorToolbar({
  editor,
  isBlockDragging = false,
  isBlockMenuOpen = false,
  onImagePopoverOpenChange,
}: EditorToolbarProps) {
  const [imagePopoverOpen, setImagePopoverOpen] = useState(false);
  const state = useEditorState({
    editor,
    selector: ({ editor: currentEditor }) => ({
      bold: currentEditor?.isActive("bold") ?? false,
      italic: currentEditor?.isActive("italic") ?? false,
      strike: currentEditor?.isActive("strike") ?? false,
      code: currentEditor?.isActive("code") ?? false,
      h2: currentEditor?.isActive("heading", { level: 2 }) ?? false,
      h3: currentEditor?.isActive("heading", { level: 3 }) ?? false,
      bulletList: currentEditor?.isActive("bulletList") ?? false,
      orderedList: currentEditor?.isActive("orderedList") ?? false,
      taskList: currentEditor?.isActive("taskList") ?? false,
      blockquote: currentEditor?.isActive("blockquote") ?? false,
      codeBlock: currentEditor?.isActive("codeBlock") ?? false,
      alignLeft: currentEditor?.isActive({ textAlign: "left" }) ?? false,
      alignCenter: currentEditor?.isActive({ textAlign: "center" }) ?? false,
      alignRight: currentEditor?.isActive({ textAlign: "right" }) ?? false,
      canUndo: currentEditor?.can().undo() ?? false,
      canRedo: currentEditor?.can().redo() ?? false,
    }),
  });
  const activeState =
    state ?? {
      bold: false,
      italic: false,
      strike: false,
      code: false,
      h2: false,
      h3: false,
      bulletList: false,
      orderedList: false,
      taskList: false,
      blockquote: false,
      codeBlock: false,
      alignLeft: false,
      alignCenter: false,
      alignRight: false,
      canUndo: false,
      canRedo: false,
    };

  useEffect(() => {
    if (isBlockDragging || isBlockMenuOpen) {
      setImagePopoverOpen(false);
    }
  }, [isBlockDragging, isBlockMenuOpen]);

  useEffect(() => {
    onImagePopoverOpenChange?.(imagePopoverOpen);
  }, [imagePopoverOpen, onImagePopoverOpenChange]);

  if (!editor) {
    return null;
  }

  return (
    <div className="atlas-editor-toolbar relative z-[60] flex h-full w-full flex-nowrap items-center gap-x-2 overflow-x-auto px-9 py-0">
      <label className="atlas-toolbar-format-select shrink-0" title="Block style">
        <span className="sr-only">Block style</span>
        <select
          aria-label="Block style"
          disabled={isBlockDragging || isBlockMenuOpen}
          onChange={(event) => applyBlockFormat(editor, event.currentTarget.value as ToolbarBlockFormat)}
          value={getActiveBlockFormat(activeState)}
        >
          <option value="paragraph">Paragraph</option>
          <option value="heading2">Heading 2</option>
          <option value="heading3">Heading 3</option>
          <option value="bulletList">Bullet list</option>
          <option value="orderedList">Numbered list</option>
          <option value="taskList">Task list</option>
          <option value="blockquote">Quote</option>
          <option value="codeBlock">Code block</option>
        </select>
        <ChevronDown className="h-3.5 w-3.5 text-[var(--ns-slate-500)]" />
      </label>
      <ToolbarSeparator />
      <ToolbarGroup>
        <ToolbarButton
          disabled={!activeState.canUndo}
          icon={Undo2}
          label="Undo"
          onClick={() => editor.chain().focus().undo().run()}
        />
        <ToolbarButton
          disabled={!activeState.canRedo}
          icon={Redo2}
          label="Redo"
          onClick={() => editor.chain().focus().redo().run()}
        />
      </ToolbarGroup>

      <ToolbarSeparator />

      <ToolbarGroup>
        <ToolbarButton
          active={activeState.bold}
          icon={Bold}
          label="Bold"
          onClick={() => editor.chain().focus().toggleBold().run()}
        />
        <ToolbarButton
          active={activeState.italic}
          icon={Italic}
          label="Italic"
          onClick={() => editor.chain().focus().toggleItalic().run()}
        />
        <ToolbarButton
          active={activeState.strike}
          icon={Strikethrough}
          label="Strikethrough"
          onClick={() => editor.chain().focus().toggleStrike().run()}
        />
        <ToolbarButton
          active={activeState.code}
          icon={Code2}
          label="Inline code"
          onClick={() => editor.chain().focus().toggleCode().run()}
        />
      </ToolbarGroup>

      <ToolbarSeparator />

      <ToolbarGroup>
        <ToolbarButton
          active={activeState.h2}
          icon={Heading2}
          label="Heading 2"
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
        />
        <ToolbarButton
          active={activeState.h3}
          icon={Heading3}
          label="Heading 3"
          onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
        />
        <ToolbarButton
          active={activeState.bulletList}
          icon={List}
          label="Bullet list"
          onClick={() => editor.chain().focus().toggleBulletList().run()}
        />
        <ToolbarButton
          active={activeState.orderedList}
          icon={ListOrdered}
          label="Numbered list"
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
        />
        <ToolbarButton
          active={activeState.taskList}
          icon={ListChecks}
          label="Task list"
          onClick={() => editor.chain().focus().toggleTaskList().run()}
        />
        <ToolbarButton
          active={activeState.blockquote}
          icon={Quote}
          label="Quote"
          onClick={() => editor.chain().focus().toggleBlockquote().run()}
        />
        <ToolbarButton
          active={activeState.codeBlock}
          icon={SquareCode}
          label="Code block"
          onClick={() => editor.chain().focus().toggleCodeBlock().run()}
        />
      </ToolbarGroup>

      <ToolbarSeparator />

      <ToolbarGroup>
        <div className="relative inline-flex">
          <ToolbarButton
            icon={ImageIcon}
            label="Insert image block"
            onClick={() => editor.chain().focus().setImageBlock().run()}
          />
          <ToolbarButton
            active={imagePopoverOpen}
            icon={Link2}
            label="Insert image from URL"
            onClick={() => setImagePopoverOpen((open) => !open)}
          />
          {imagePopoverOpen ? (
            <ImageUrlPopover
              className="absolute left-0 top-[calc(100%+8px)] z-[70]"
              onCancel={() => setImagePopoverOpen(false)}
              onSubmit={(src) => {
                const normalizedSrc = normalizeUrl(src);

                if (normalizedSrc) {
                  editor.chain().focus().setImageBlock({ src: normalizedSrc }).run();
                }

                setImagePopoverOpen(false);
              }}
            />
          ) : null}
        </div>
        <InsertTableButton editor={editor} />
      </ToolbarGroup>

      <ToolbarSeparator />

      <ToolbarGroup>
        <ToolbarButton
          active={activeState.alignLeft}
          icon={AlignLeft}
          label="Align left"
          onClick={() => editor.chain().focus().setTextAlign("left").run()}
        />
        <ToolbarButton
          active={activeState.alignCenter}
          icon={AlignCenter}
          label="Align center"
          onClick={() => editor.chain().focus().setTextAlign("center").run()}
        />
        <ToolbarButton
          active={activeState.alignRight}
          icon={AlignRight}
          label="Align right"
          onClick={() => editor.chain().focus().setTextAlign("right").run()}
        />
        <ToolbarButton icon={Eraser} label="Clear formatting" onClick={() => clearBlockFormatting(editor)} />
      </ToolbarGroup>
    </div>
  );
}

function getActiveBlockFormat(activeState: ToolbarActiveState): ToolbarBlockFormat {
  if (activeState.codeBlock) {
    return "codeBlock";
  }

  if (activeState.blockquote) {
    return "blockquote";
  }

  if (activeState.taskList) {
    return "taskList";
  }

  if (activeState.orderedList) {
    return "orderedList";
  }

  if (activeState.bulletList) {
    return "bulletList";
  }

  if (activeState.h3) {
    return "heading3";
  }

  if (activeState.h2) {
    return "heading2";
  }

  return "paragraph";
}

function applyBlockFormat(editor: Editor, format: ToolbarBlockFormat) {
  const chain = editor.chain().focus();

  switch (format) {
    case "blockquote":
      chain.toggleBlockquote().run();
      return;
    case "bulletList":
      chain.toggleBulletList().run();
      return;
    case "codeBlock":
      chain.toggleCodeBlock().run();
      return;
    case "heading2":
      chain.setHeading({ level: 2 }).run();
      return;
    case "heading3":
      chain.setHeading({ level: 3 }).run();
      return;
    case "orderedList":
      chain.toggleOrderedList().run();
      return;
    case "taskList":
      chain.toggleTaskList().run();
      return;
    case "paragraph":
    default:
      chain.setParagraph().run();
  }
}
