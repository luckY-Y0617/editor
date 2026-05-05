import type { Editor } from "@tiptap/react";
import { useEditorState } from "@tiptap/react";
import { FloatingMenu } from "@tiptap/react/menus";
import {
  ChevronDown,
  CodeSquare,
  Heading2,
  Heading3,
  ImageIcon,
  List,
  ListChecks,
  ListOrdered,
  Pilcrow,
  Quote,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useCallback, useEffect, useRef } from "react";

type FloatingMenuButtonProps = {
  active?: boolean;
  icon: LucideIcon;
  label: string;
  onClick: () => void;
};

type EditorFloatingMenuProps = {
  editor: Editor | null;
  isBlockDragging?: boolean;
  isBlockMenuOpen?: boolean;
  onSlashMenuOpenChange?: (isOpen: boolean) => void;
};

export function EditorFloatingMenu({
  editor,
  isBlockDragging = false,
  isBlockMenuOpen = false,
  onSlashMenuOpenChange,
}: EditorFloatingMenuProps) {
  const visibleReasonRef = useRef<FloatingMenuVisibleReason | null>(null);
  const state = useEditorState({
    editor,
    selector: ({ editor: currentEditor }) => ({
      paragraph: currentEditor?.isActive("paragraph") ?? false,
      h2: currentEditor?.isActive("heading", { level: 2 }) ?? false,
      h3: currentEditor?.isActive("heading", { level: 3 }) ?? false,
      bulletList: currentEditor?.isActive("bulletList") ?? false,
      orderedList: currentEditor?.isActive("orderedList") ?? false,
      taskList: currentEditor?.isActive("taskList") ?? false,
      blockquote: currentEditor?.isActive("blockquote") ?? false,
      codeBlock: currentEditor?.isActive("codeBlock") ?? false,
      details: currentEditor?.isActive("details") ?? false,
      imageBlock: currentEditor?.isActive("imageBlock") ?? false,
    }),
  });
  const activeState = state ?? {
    paragraph: false,
    h2: false,
    h3: false,
    bulletList: false,
    orderedList: false,
    taskList: false,
    blockquote: false,
    codeBlock: false,
    details: false,
    imageBlock: false,
  };

  const syncSlashMenuOpenState = useCallback(() => {
    onSlashMenuOpenChange?.(visibleReasonRef.current === "slash");
  }, [onSlashMenuOpenChange]);

  useEffect(() => {
    if (isBlockDragging || isBlockMenuOpen) {
      visibleReasonRef.current = null;
      onSlashMenuOpenChange?.(false);
    }
  }, [isBlockDragging, isBlockMenuOpen, onSlashMenuOpenChange]);

  useEffect(
    () => () => {
      visibleReasonRef.current = null;
      onSlashMenuOpenChange?.(false);
    },
    [onSlashMenuOpenChange],
  );

  if (!editor) {
    return null;
  }

  if (isBlockDragging || isBlockMenuOpen) {
    return null;
  }

  return (
    <FloatingMenu
      className="knowledge-floating-menu"
      editor={editor}
      options={{
        placement: "bottom-start",
        offset: 18,
        shift: true,
        onDestroy: () => {
          visibleReasonRef.current = null;
          onSlashMenuOpenChange?.(false);
        },
        onHide: () => {
          visibleReasonRef.current = null;
          onSlashMenuOpenChange?.(false);
        },
        onShow: syncSlashMenuOpenState,
        onUpdate: syncSlashMenuOpenState,
      }}
      pluginKey="knowledge-block-floating-menu"
      updateDelay={0}
      shouldShow={({ editor: currentEditor, state: editorState }) => {
        const { selection } = editorState;
        visibleReasonRef.current = null;

        if (!currentEditor.isEditable || isBlockDragging || isBlockMenuOpen || !selection.empty) {
          return false;
        }

        if (currentEditor.isActive("codeBlock") || selectionHasAncestorType(currentEditor, "codeBlock")) {
          return false;
        }

        for (let depth = selection.$from.depth; depth > 0; depth -= 1) {
          const nodeName = selection.$from.node(depth).type.name;

          if (floatingMenuExcludedAncestors.has(nodeName)) {
            return false;
          }
        }

        if (selection.$from.parent.type.name !== "paragraph") {
          return false;
        }

        const paragraphText = selection.$from.parent.textContent;

        if (paragraphText === "/") {
          visibleReasonRef.current = "slash";
          return true;
        }

        if (currentEditor.isEmpty && paragraphText.length === 0) {
          visibleReasonRef.current = "empty";
          return true;
        }

        return false;
      }}
    >
      <FloatingMenuButton
        active={activeState.paragraph}
        icon={Pilcrow}
        label="正文"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.setParagraph())}
      />
      <FloatingMenuButton
        active={activeState.h2}
        icon={Heading2}
        label="二级标题"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.toggleHeading({ level: 2 }))}
      />
      <FloatingMenuButton
        active={activeState.h3}
        icon={Heading3}
        label="三级标题"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.toggleHeading({ level: 3 }))}
      />
      <FloatingMenuButton
        active={activeState.bulletList}
        icon={List}
        label="无序列表"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.toggleBulletList())}
      />
      <FloatingMenuButton
        active={activeState.orderedList}
        icon={ListOrdered}
        label="有序列表"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.toggleOrderedList())}
      />
      <FloatingMenuButton
        active={activeState.taskList}
        icon={ListChecks}
        label="任务列表"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.toggleTaskList())}
      />
      <FloatingMenuButton
        active={activeState.blockquote}
        icon={Quote}
        label="引用"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.toggleBlockquote())}
      />
      <FloatingMenuButton
        active={activeState.codeBlock}
        icon={CodeSquare}
        label="代码块"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.toggleCodeBlock())}
      />
      <FloatingMenuButton
        active={activeState.details}
        icon={ChevronDown}
        label="折叠块"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.insertContent(detailsInsertContent))}
      />
      <FloatingMenuButton
        active={activeState.imageBlock}
        icon={ImageIcon}
        label="图片"
        onClick={() => runFloatingMenuCommand(editor, (chain) => chain.setImageBlock())}
      />
    </FloatingMenu>
  );
}

const floatingMenuExcludedAncestors = new Set([
  "codeBlock",
  "listItem",
  "taskItem",
  "table",
  "tableCell",
  "tableHeader",
  "tableRow",
  "imageBlock",
]);

type FloatingMenuVisibleReason = "empty" | "slash";

type EditorCommandChain = ReturnType<Editor["chain"]>;

const detailsInsertContent = {
  type: "details",
  content: [
    {
      type: "detailsSummary",
      content: [{ type: "text", text: "折叠块标题" }],
    },
    {
      type: "detailsContent",
      content: [
        {
          type: "paragraph",
          content: [{ type: "text", text: "在这里补充说明内容..." }],
        },
      ],
    },
  ],
};

function runFloatingMenuCommand(editor: Editor, command: (chain: EditorCommandChain) => EditorCommandChain) {
  const slashRange = getSlashTriggerRange(editor);
  const chain = editor.chain().focus();

  command(slashRange ? chain.deleteRange(slashRange) : chain).run();
}

function getSlashTriggerRange(editor: Editor) {
  const { selection } = editor.state;
  const { $from } = selection;

  if (!selection.empty || $from.parent.type.name !== "paragraph" || $from.parent.textContent !== "/") {
    return null;
  }

  return {
    from: $from.start(),
    to: $from.end(),
  };
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

function FloatingMenuButton({ active, icon: Icon, label, onClick }: FloatingMenuButtonProps) {
  return (
    <button
      aria-label={label}
      className={[
        "knowledge-floating-menu-item",
        active ? "knowledge-floating-menu-item-active" : "",
      ].join(" ")}
      onMouseDown={(event) => event.preventDefault()}
      onClick={onClick}
      title={label}
      type="button"
    >
      <Icon className="h-4 w-4" />
      <span>{label}</span>
    </button>
  );
}
