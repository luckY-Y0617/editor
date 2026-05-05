import { EditorContent, useEditor, useEditorState, type Editor } from "@tiptap/react";
import { CharacterCount, Focus } from "@tiptap/extensions";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import { Details, DetailsContent, DetailsSummary } from "@tiptap/extension-details";
import Highlight from "@tiptap/extension-highlight";
import ImageExtension from "@tiptap/extension-image";
import Link from "@tiptap/extension-link";
import Placeholder from "@tiptap/extension-placeholder";
import StarterKit from "@tiptap/starter-kit";
import { Table } from "@tiptap/extension-table";
import TableCell from "@tiptap/extension-table-cell";
import TableHeader from "@tiptap/extension-table-header";
import TableRow from "@tiptap/extension-table-row";
import TaskItem from "@tiptap/extension-task-item";
import TaskList from "@tiptap/extension-task-list";
import TextAlign from "@tiptap/extension-text-align";
import type { ResolvedPos } from "@tiptap/pm/model";
import { NodeSelection, TextSelection } from "@tiptap/pm/state";
import { useCallback, useEffect, useRef, useState } from "react";
import type {
  KeyboardEvent as ReactKeyboardEvent,
  MouseEvent as ReactMouseEvent,
  PointerEvent as ReactPointerEvent,
} from "react";
import { createPortal } from "react-dom";
import { tiptapInitialContent } from "../data/tiptapInitialContent";
import { EditorBubbleMenu } from "./EditorBubbleMenu";
import { EditorDragHandle } from "./EditorDragHandle";
import { EditorFloatingMenu } from "./EditorFloatingMenu";
import { EditorToolbar } from "./EditorToolbar";
import { TableInlineControls } from "./TableInlineControls";
import type {
  AnchorPoint,
  CommentAnchorV1,
  CommentFocusRequest,
  CommentRuntimeAnchorState,
  CommentThread,
  EditorSelectionRange,
  OutlineFocusRequest,
  OutlineItem,
  OutlineLevel,
  PendingCommentComposer,
  TiptapContentChange,
  TiptapContentStats,
} from "../types/editor";
import { ImageBlock } from "../extensions/ImageBlock";
import { BlockIdentity, isValidBlockId } from "../extensions/BlockIdentity";
import {
  CommentDecorations,
  getCommentRuntimeAnchorState,
  getMappedCommentRange,
  syncCommentDecorations,
} from "../extensions/CommentDecorations";
import { NorthstarTabKeymap } from "../extensions/NorthstarTabKeymap";
import { shouldShowNewCommentButton } from "../lib/commentSelectionUx";
import { lowlight } from "../lib/lowlight";
import { normalizePlainTextV1 } from "../lib/commentAnchorMatching";
import type { JSONContent } from "@tiptap/react";

const IMAGE_RESIZE_STATE_EVENT = "knowledge-image-resize-state-change";
const TABLE_CONTROL_INTERACTION_SELECTOR =
  ".knowledge-table-inline-control, .knowledge-table-inline-control-bridge, .knowledge-table-inline-menu";
const COMMENT_CONTEXT_LENGTH = 32;
const MOCK_COMMENT_BASE_REVISION = 0;

type TiptapEditorProps = {
  content?: JSONContent;
  activeCommentThreadId?: string | null;
  commentFocusRequest?: CommentFocusRequest | null;
  commentThreads?: CommentThread[];
  documentId: string;
  focusRequest?: OutlineFocusRequest | null;
  isCommentComposerOpen?: boolean;
  onCommentRuntimeStateChange?: (runtimeState: CommentRuntimeAnchorState) => void;
  onContentChange?: (change: TiptapContentChange) => void;
  onContentStatsChange?: (stats: TiptapContentStats) => void;
  onOpenCommentComposer?: (composer: PendingCommentComposer) => void;
  onSelectCommentThread?: (threadId: string) => void;
  toolbarPortalId?: string;
};

export function TiptapEditor({
  activeCommentThreadId = null,
  commentFocusRequest,
  commentThreads = [],
  content = tiptapInitialContent,
  documentId,
  focusRequest,
  isCommentComposerOpen = false,
  onCommentRuntimeStateChange,
  onContentChange,
  onContentStatsChange,
  onOpenCommentComposer,
  onSelectCommentThread,
  toolbarPortalId,
}: TiptapEditorProps) {
  const onCommentRuntimeStateChangeRef = useRef(onCommentRuntimeStateChange);
  const onContentChangeRef = useRef(onContentChange);
  const onContentStatsChangeRef = useRef(onContentStatsChange);
  const onSelectCommentThreadRef = useRef(onSelectCommentThread);
  const [toolbarPortalTarget, setToolbarPortalTarget] = useState<HTMLElement | null>(null);
  const [editorUiState, setEditorUiState] = useState({
    isBlockDragging: false,
    isBlockDragMenuSuppressed: false,
    isBlockMenuOpen: false,
    isTableControlHovered: false,
    isTableMenuOpen: false,
    isImagePopoverOpen: false,
    isImageResizing: false,
    // Reserved overlay slots for future features. They intentionally do not enable any feature yet.
    isSlashMenuOpen: false,
    isCommentPopoverOpen: false,
  });

  useEffect(() => {
    onCommentRuntimeStateChangeRef.current = onCommentRuntimeStateChange;
  }, [onCommentRuntimeStateChange]);

  useEffect(() => {
    onContentChangeRef.current = onContentChange;
  }, [onContentChange]);

  useEffect(() => {
    onContentStatsChangeRef.current = onContentStatsChange;
  }, [onContentStatsChange]);

  useEffect(() => {
    onSelectCommentThreadRef.current = onSelectCommentThread;
  }, [onSelectCommentThread]);

  useEffect(() => {
    setToolbarPortalTarget(toolbarPortalId ? document.getElementById(toolbarPortalId) : null);

    return () => setToolbarPortalTarget(null);
  }, [toolbarPortalId]);

  const handleBlockDragStateChange = useCallback((isBlockDragging: boolean) => {
    setEditorUiState((current) => {
      const nextState = isBlockDragging
        ? { ...current, isBlockDragging: true, isBlockDragMenuSuppressed: true }
        : { ...current, isBlockDragging: false };

      return current.isBlockDragging === nextState.isBlockDragging &&
        current.isBlockDragMenuSuppressed === nextState.isBlockDragMenuSuppressed
        ? current
        : nextState;
    });
  }, []);

  const handleBlockMenuOpenChange = useCallback((isBlockMenuOpen: boolean) => {
    setEditorUiState((current) =>
      current.isBlockMenuOpen === isBlockMenuOpen ? current : { ...current, isBlockMenuOpen },
    );
  }, []);

  const handleTableMenuOpenChange = useCallback((isTableMenuOpen: boolean) => {
    setEditorUiState((current) =>
      current.isTableMenuOpen === isTableMenuOpen ? current : { ...current, isTableMenuOpen },
    );
  }, []);

  const handleTableControlHoverChange = useCallback((isTableControlHovered: boolean) => {
    setEditorUiState((current) =>
      current.isTableControlHovered === isTableControlHovered ? current : { ...current, isTableControlHovered },
    );
  }, []);

  const handleImagePopoverOpenChange = useCallback((isImagePopoverOpen: boolean) => {
    setEditorUiState((current) =>
      current.isImagePopoverOpen === isImagePopoverOpen ? current : { ...current, isImagePopoverOpen },
    );
  }, []);

  const handleSlashMenuOpenChange = useCallback((isSlashMenuOpen: boolean) => {
    setEditorUiState((current) =>
      current.isSlashMenuOpen === isSlashMenuOpen ? current : { ...current, isSlashMenuOpen },
    );
  }, []);

  useEffect(() => {
    function handleImageResizeStateChange(event: Event) {
      const isImageResizing = Boolean((event as CustomEvent<{ isResizing?: boolean }>).detail?.isResizing);

      setEditorUiState((current) =>
        current.isImageResizing === isImageResizing ? current : { ...current, isImageResizing },
      );
    }

    window.addEventListener(IMAGE_RESIZE_STATE_EVENT, handleImageResizeStateChange);

    return () => {
      window.removeEventListener(IMAGE_RESIZE_STATE_EVENT, handleImageResizeStateChange);
    };
  }, []);

  const clearBlockDragMenuSuppression = useCallback(() => {
    setEditorUiState((current) =>
      current.isBlockDragMenuSuppressed ? { ...current, isBlockDragMenuSuppressed: false } : current,
    );
  }, []);

  const handleEditorPointerDownCapture = useCallback(
    (event: ReactPointerEvent<HTMLDivElement>) => {
      const target = event.target;

      if (target instanceof Element && target.closest(".knowledge-drag-handle")) {
        return;
      }

      clearBlockDragMenuSuppression();
    },
    [clearBlockDragMenuSuppression],
  );

  const editor = useEditor(
    {
      extensions: [
        StarterKit.configure({
          codeBlock: false,
          dropcursor: {
            class: "knowledge-dropcursor",
            color: "rgba(79, 124, 255, 0.42)",
            width: 2,
          },
          horizontalRule: false,
          link: false,
          // StarterKit already includes ListKeymap, Gapcursor, and TrailingNode.
          listKeymap: {
            listTypes: [
              { itemName: "listItem", wrapperNames: ["bulletList", "orderedList"] },
              { itemName: "taskItem", wrapperNames: ["taskList"] },
            ],
          },
          trailingNode: {
            node: "paragraph",
          },
          underline: false,
        }),
        CharacterCount.configure({
          mode: "textSize",
        }),
        Focus.configure({
          className: "knowledge-focused-block",
          mode: "deepest",
        }),
        NorthstarTabKeymap,
        BlockIdentity,
        CommentDecorations,
        CodeBlockLowlight.configure({
          defaultLanguage: "plaintext",
          enableTabIndentation: true,
          lowlight,
          tabSize: 2,
          HTMLAttributes: {
            class: "knowledge-code-block",
          },
        }),
        Details.configure({
          openClassName: "is-open",
          HTMLAttributes: {
            class: "knowledge-details",
          },
          renderToggleButton: ({ element, isOpen }) => {
            element.setAttribute("aria-label", isOpen ? "收起折叠块" : "展开折叠块");
            element.setAttribute("title", isOpen ? "收起折叠块" : "展开折叠块");
            element.dataset.open = String(isOpen);
          },
        }),
        DetailsSummary.configure({
          HTMLAttributes: {
            class: "knowledge-details-summary",
          },
        }),
        DetailsContent.configure({
          HTMLAttributes: {
            class: "knowledge-details-content",
          },
        }),
        TaskList,
        TaskItem,
        Highlight.configure({
          multicolor: false,
          HTMLAttributes: {
            class: "knowledge-highlight",
          },
        }),
        TextAlign.configure({
          types: ["heading", "paragraph"],
          alignments: ["left", "center", "right"],
          defaultAlignment: "left",
        }),
        ImageExtension.configure({
          inline: false,
          allowBase64: false,
          HTMLAttributes: {
            class: "knowledge-image",
          },
        }),
        ImageBlock,
        // Official basic table support only: no resize, merge UI, or custom NodeView.
        Table.configure({
          resizable: false,
          HTMLAttributes: {
            class: "knowledge-table",
          },
        }),
        TableRow,
        TableHeader,
        TableCell,
        Link.configure({
          openOnClick: false,
          autolink: true,
          linkOnPaste: true,
          enableClickSelection: true,
          HTMLAttributes: {
            class: "knowledge-link",
            rel: "noopener noreferrer nofollow",
            target: "_blank",
          },
        }),
        Placeholder.configure({
          includeChildren: true,
          placeholder: ({ node }) => {
            if (node.type.name === "heading") {
              return "输入标题";
            }

            return "继续写作，或输入 / 插入标题、列表、引用和代码块……";
          },
        }),
      ],
      content,
      editorProps: {
        attributes: {
          class: "knowledge-tiptap-content",
          spellcheck: "false",
        },
      },
      onCreate: ({ editor: currentEditor }) => {
        onContentStatsChangeRef.current?.(getContentStats(currentEditor));
      },
      onUpdate: ({ editor: currentEditor }) => {
        const stats = getContentStats(currentEditor);

        onContentStatsChangeRef.current?.(stats);
        onContentChangeRef.current?.({
          ...stats,
          content: currentEditor.getJSON(),
        });
      },
    },
    [],
  );

  const selectionUiState = useEditorState({
    editor,
    selector: ({ editor: currentEditor }) => {
      if (!currentEditor) {
        return { hasTextSelection: false, isImageBlockSelected: false };
      }

      const { selection } = currentEditor.state;
      const { from, to } = selection;

      return {
        hasTextSelection:
          from !== to && currentEditor.state.doc.textBetween(from, to, " ").trim().length > 0,
        isImageBlockSelected: selection instanceof NodeSelection && selection.node.type.name === "imageBlock",
      };
    },
  }) ?? { hasTextSelection: false, isImageBlockSelected: false };

  const emitCommentRuntimeState = useCallback((currentEditor: Editor) => {
    onCommentRuntimeStateChangeRef.current?.(getCommentRuntimeAnchorState(currentEditor.state));
  }, []);

  useEffect(() => {
    if (!editor) {
      return;
    }

    syncCommentDecorations(editor, commentThreads, activeCommentThreadId);
    emitCommentRuntimeState(editor);
  }, [activeCommentThreadId, commentThreads, editor, emitCommentRuntimeState]);

  useEffect(() => {
    if (!editor) {
      return;
    }

    const handleTransaction = ({ editor: currentEditor }: { editor: Editor }) => {
      emitCommentRuntimeState(currentEditor);
    };

    editor.on("transaction", handleTransaction);

    return () => {
      editor.off("transaction", handleTransaction);
    };
  }, [editor, emitCommentRuntimeState]);

  const handleOpenCommentComposer = useCallback(() => {
    if (!editor || !onOpenCommentComposer) {
      return;
    }

    if (!shouldShowNewCommentButton(editor, isCommentComposerOpen)) {
      return;
    }

    const anchor = createCommentAnchor(editor, documentId);

    if (!anchor) {
      return;
    }

    onOpenCommentComposer({
      documentId,
      anchor,
      excerpt: anchor.display.excerpt,
    });
  }, [documentId, editor, isCommentComposerOpen, onOpenCommentComposer]);

  const handleEditorClick = useCallback((event: ReactMouseEvent<HTMLDivElement>) => {
    const target = event.target;

    if (!(target instanceof Element)) {
      return;
    }

    const highlight = target.closest<HTMLElement>("[data-thread-id]");

    if (!highlight || !highlight.dataset.threadId) {
      return;
    }

    onSelectCommentThreadRef.current?.(highlight.dataset.threadId);
  }, []);

  useEffect(() => {
    if (!editor) {
      return;
    }

    const editorDom = editor.view.dom;

    function preserveDragHandleForTableControls(event: MouseEvent) {
      const relatedTarget = event.relatedTarget;

      if (
        relatedTarget instanceof Element &&
        relatedTarget.closest(TABLE_CONTROL_INTERACTION_SELECTOR)
      ) {
        event.stopImmediatePropagation();
      }
    }

    editorDom.addEventListener("mouseleave", preserveDragHandleForTableControls, true);

    return () => {
      editorDom.removeEventListener("mouseleave", preserveDragHandleForTableControls, true);
    };
  }, [editor]);

  const handleEditorKeyDown = useCallback(
    (event: ReactKeyboardEvent<HTMLDivElement>) => {
      if (event.key !== "Tab" || event.defaultPrevented || event.nativeEvent.isComposing) {
        return;
      }

      const target = event.target;

      if (!(target instanceof Element) || !target.closest(".tiptap-editor")) {
        return;
      }

      const isNodeViewFocusTarget =
        target.matches('input[type="checkbox"]') || Boolean(target.closest('[contenteditable="false"]'));
      const isOutsideProseMirror = !target.closest(".ProseMirror");

      if (!isNodeViewFocusTarget && !isOutsideProseMirror) {
        return;
      }

      event.preventDefault();
      editor?.commands.focus();
    },
    [editor],
  );

  useEffect(() => {
    if (!editor || !focusRequest) {
      return;
    }

    focusEditorAtPosition(editor, focusRequest.pos);
  }, [editor, focusRequest]);

  useEffect(() => {
    if (!editor || !commentFocusRequest) {
      return;
    }

    const range = getMappedCommentRange(editor.state, commentFocusRequest.threadId);

    if (!range) {
      return;
    }

    focusEditorAtRange(editor, range);
  }, [commentFocusRequest, editor]);

  const suppressEditorMenus = editorUiState.isBlockDragging || editorUiState.isBlockDragMenuSuppressed;
  const reservedOverlayOpen =
    editorUiState.isSlashMenuOpen || editorUiState.isCommentPopoverOpen || isCommentComposerOpen;
  const tableOrBlockOverlayOpen = editorUiState.isBlockMenuOpen || editorUiState.isTableMenuOpen;
  const imageBlockOverlayOpen = selectionUiState.isImageBlockSelected || editorUiState.isImageResizing;
  const highPriorityOverlayOpen =
    tableOrBlockOverlayOpen || editorUiState.isImagePopoverOpen || imageBlockOverlayOpen || reservedOverlayOpen;
  const bubbleMenuBlocked = suppressEditorMenus || highPriorityOverlayOpen;
  const bubbleMenuVisible = selectionUiState.hasTextSelection && !bubbleMenuBlocked;
  const floatingMenuBlocked =
    bubbleMenuVisible ||
    suppressEditorMenus ||
    tableOrBlockOverlayOpen ||
    editorUiState.isImagePopoverOpen ||
    imageBlockOverlayOpen ||
    editorUiState.isCommentPopoverOpen;
  const dragHandleBlocked =
    editorUiState.isTableMenuOpen ||
    editorUiState.isImagePopoverOpen ||
    imageBlockOverlayOpen ||
    reservedOverlayOpen ||
    bubbleMenuVisible;
  const tableControlsBlocked =
    suppressEditorMenus ||
    editorUiState.isBlockMenuOpen ||
    (editorUiState.isImagePopoverOpen && !editorUiState.isTableMenuOpen) ||
    imageBlockOverlayOpen ||
    reservedOverlayOpen;
  const toolbar = (
    <EditorToolbar
      editor={editor}
      isBlockDragging={suppressEditorMenus}
      isBlockMenuOpen={tableOrBlockOverlayOpen || reservedOverlayOpen}
      onImagePopoverOpenChange={handleImagePopoverOpenChange}
    />
  );

  return (
    <div
      className={[
        "tiptap-editor",
        editorUiState.isBlockMenuOpen ? "editor-block-menu-open" : "",
        editorUiState.isTableMenuOpen ? "has-table-inline-menu" : "",
        editorUiState.isTableMenuOpen ? "editor-table-menu-open" : "",
        editorUiState.isImagePopoverOpen ? "editor-image-popover-open" : "",
        editorUiState.isImageResizing ? "editor-image-resizing" : "",
        editorUiState.isSlashMenuOpen ? "editor-slash-menu-open" : "",
        editorUiState.isCommentPopoverOpen ? "editor-comment-popover-open" : "",
        bubbleMenuVisible ? "editor-text-selecting" : "",
      ].join(" ")}
      onKeyDown={handleEditorKeyDown}
      onKeyDownCapture={clearBlockDragMenuSuppression}
      onClick={handleEditorClick}
      onPointerDownCapture={handleEditorPointerDownCapture}
    >
      {toolbarPortalTarget ? createPortal(toolbar, toolbarPortalTarget) : toolbarPortalId ? null : toolbar}
      <EditorBubbleMenu
        editor={editor}
        isBlockDragging={suppressEditorMenus}
        isBlockMenuOpen={highPriorityOverlayOpen}
        isCommentComposerOpen={isCommentComposerOpen}
        onOpenCommentComposer={handleOpenCommentComposer}
      />
      <EditorFloatingMenu
        editor={editor}
        isBlockDragging={suppressEditorMenus}
        isBlockMenuOpen={floatingMenuBlocked}
        onSlashMenuOpenChange={handleSlashMenuOpenChange}
      />
      <EditorDragHandle
        editor={editor}
        isOverlayBlocked={dragHandleBlocked}
        isTableControlHovered={editorUiState.isTableControlHovered}
        onDragStateChange={handleBlockDragStateChange}
        onMenuOpenChange={handleBlockMenuOpenChange}
      />
      <TableInlineControls
        editor={editor}
        isBlockDragging={suppressEditorMenus}
        isBlockMenuOpen={tableControlsBlocked}
        onControlHoverChange={handleTableControlHoverChange}
        onMenuOpenChange={handleTableMenuOpenChange}
      />
      <EditorContent editor={editor} />
    </div>
  );
}

function getContentStats(editor: Editor): TiptapContentStats {
  return {
    isEmpty: editor.isEmpty,
    textLength: editor.storage.characterCount.characters(),
    outlineItems: extractOutlineItems(editor),
  };
}

function extractOutlineItems(editor: Editor): OutlineItem[] {
  const outlineItems: OutlineItem[] = [];

  editor.state.doc.descendants((node, pos) => {
    if (node.type.name !== "heading" || !isOutlineLevel(node.attrs.level)) {
      return true;
    }

    const text = node.textContent.trim();

    if (!text) {
      return true;
    }

    outlineItems.push({
      id: `heading-${outlineItems.length}`,
      level: node.attrs.level,
      pos: pos + 1,
      text,
    });

    return true;
  });

  return outlineItems;
}

function isOutlineLevel(level: unknown): level is OutlineLevel {
  return level === 1 || level === 2 || level === 3;
}

function focusEditorAtPosition(editor: Editor, pos: number) {
  const docSize = editor.state.doc.content.size;

  if (!Number.isInteger(pos) || pos < 1 || pos > docSize) {
    return;
  }

  try {
    editor.chain().focus().setTextSelection(pos).run();
  } catch {
    // Ignore stale outline positions from a prior document state.
  }
}

function focusEditorAtRange(editor: Editor, range: EditorSelectionRange) {
  const docSize = editor.state.doc.content.size;

  if (
    !Number.isInteger(range.from) ||
    !Number.isInteger(range.to) ||
    range.from < 1 ||
    range.to <= range.from ||
    range.to > docSize
  ) {
    return;
  }

  try {
    const selection = TextSelection.create(editor.state.doc, range.from, range.to);

    editor.view.dispatch(
      editor.state.tr
        .setSelection(selection)
        .scrollIntoView()
        .setMeta("addToHistory", false),
    );
    editor.view.focus();
  } catch {
    // Ignore stale comment decoration ranges after aggressive content edits.
  }
}

export function createCommentAnchor(editor: Pick<Editor, "state">, documentId: string): CommentAnchorV1 | null {
  const { doc, selection } = editor.state;
  const { from, to } = selection;
  const exact = doc.textBetween(from, to, " ");

  if (from === to || !exact.trim()) {
    return null;
  }

  const prefix = doc.textBetween(Math.max(0, from - COMMENT_CONTEXT_LENGTH), from, " ");
  const suffix = doc.textBetween(to, Math.min(doc.content.size, to + COMMENT_CONTEXT_LENGTH), " ");

  return {
    schema: "northstar.commentAnchor.v1",
    kind: "tiptap.textRange",
    documentId,
    baseRevision: MOCK_COMMENT_BASE_REVISION,
    pm: {
      from,
      to,
    },
    block: {
      start: createAnchorPoint(doc.resolve(from), from),
      end: createAnchorPoint(doc.resolve(to), to),
    },
    quote: {
      exact,
      prefix,
      suffix,
      normalizedExact: normalizePlainTextV1(exact),
      normalizer: "northstar.plainText.v1",
    },
    display: {
      excerpt: createExcerpt(exact),
    },
  };
}

function createAnchorPoint(resolvedPos: ResolvedPos, absolutePos: number): AnchorPoint {
  const blockDepth = getTextBlockDepth(resolvedPos);
  const blockStart = blockDepth > 0 ? resolvedPos.start(blockDepth) : 0;
  const blockNode = resolvedPos.node(blockDepth);
  const blockId = blockNode.attrs.blockId;

  return {
    ...(isValidBlockId(blockId) ? { blockId } : {}),
    path: createNodePath(resolvedPos, blockDepth),
    nodeType: blockNode.type.name,
    textOffset: resolvedPos.doc.textBetween(blockStart, absolutePos, "", "").length,
  };
}

function getTextBlockDepth(resolvedPos: ResolvedPos) {
  for (let depth = resolvedPos.depth; depth >= 0; depth -= 1) {
    if (resolvedPos.node(depth).isTextblock) {
      return depth;
    }
  }

  return resolvedPos.depth;
}

function createNodePath(resolvedPos: ResolvedPos, nodeDepth: number) {
  const path: number[] = [];

  for (let depth = 0; depth < nodeDepth; depth += 1) {
    path.push(resolvedPos.index(depth));
  }

  return path;
}

function createExcerpt(text: string) {
  const normalizedText = normalizePlainTextV1(text);

  return normalizedText.length > 96 ? `${normalizedText.slice(0, 93)}...` : normalizedText;
}
