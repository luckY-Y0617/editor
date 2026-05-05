import { DragHandle } from "@tiptap/extension-drag-handle-react";
import type { Editor } from "@tiptap/react";
import {
  AlignLeft,
  GripVertical,
  Heading2,
  Heading3,
  Pilcrow,
  Plus,
  Quote,
  SquareCode,
  Trash2,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { Node as ProseMirrorNode } from "@tiptap/pm/model";
import { useCallback, useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import { clearBlockFormatting } from "../lib/editorCommands";

type BlockHandleNodeInfo = {
  typeName: string;
  pos: number | null;
  nodeSize: number;
  level?: number;
  isComplex: boolean;
};

type LockedBlockHandleContext = {
  nodeInfo: BlockHandleNodeInfo;
  rect: DOMRect;
};

type BlockConversionTarget = "paragraph" | "heading2" | "heading3" | "blockquote" | "codeBlock";
type OpenBlockMenu = "block" | null;

type EditorDragHandleProps = {
  editor: Editor | null;
  isOverlayBlocked?: boolean;
  isTableControlHovered?: boolean;
  onDragStateChange?: (isDragging: boolean) => void;
  onMenuOpenChange?: (isOpen: boolean) => void;
};

export function EditorDragHandle({
  editor,
  isOverlayBlocked = false,
  isTableControlHovered = false,
  onDragStateChange,
  onMenuOpenChange,
}: EditorDragHandleProps) {
  const [nodeInfo, setNodeInfo] = useState<BlockHandleNodeInfo | null>(null);
  const [openMenu, setOpenMenu] = useState<OpenBlockMenu>(null);
  const [isDragging, setIsDragging] = useState(false);
  const dragStartedRef = useRef(false);
  const handleContentRef = useRef<HTMLDivElement | null>(null);
  const lockedContextRef = useRef<LockedBlockHandleContext | null>(null);
  const openMenuRef = useRef<OpenBlockMenu>(null);
  const suppressClickRef = useRef(false);
  const suppressClickTimerRef = useRef<number | null>(null);
  const finishDragTimerRef = useRef<number | null>(null);

  const handleNodeChange = useCallback(({ node, pos }: { node: ProseMirrorNode | null; pos: number }) => {
    if (openMenuRef.current === "block") {
      return;
    }

    if (!node) {
      setNodeInfo(null);
      openMenuRef.current = null;
      setOpenMenu(null);
      return;
    }

    setNodeInfo(getBlockHandleNodeInfo(node, pos));
  }, []);

  const suppressNextClick = useCallback((delay = 220) => {
    suppressClickRef.current = true;

    if (suppressClickTimerRef.current !== null) {
      window.clearTimeout(suppressClickTimerRef.current);
    }

    suppressClickTimerRef.current = window.setTimeout(() => {
      suppressClickRef.current = false;
      suppressClickTimerRef.current = null;
    }, delay);
  }, []);

  const closeMenus = useCallback(() => {
    lockedContextRef.current = null;
    openMenuRef.current = null;
    setOpenMenu(null);
    setDragHandleLocked(editor, false);
  }, [editor]);

  useEffect(() => {
    openMenuRef.current = openMenu;
  }, [openMenu]);

  const finishDragInteraction = useCallback(() => {
    suppressNextClick();
    closeMenus();

    if (editor && !editor.isDestroyed) {
      editor.commands.blur();
    }

    if (finishDragTimerRef.current !== null) {
      window.clearTimeout(finishDragTimerRef.current);
    }

    finishDragTimerRef.current = window.setTimeout(() => {
      dragStartedRef.current = false;
      setIsDragging(false);
      finishDragTimerRef.current = null;
    }, 220);
  }, [closeMenus, editor, suppressNextClick]);

  const openBlockMenu = useCallback(() => {
    if (!editor || !nodeInfo || dragStartedRef.current || isDragging || suppressClickRef.current) {
      return;
    }

    const lockedContext = createLockedBlockHandleContext(editor, nodeInfo);

    if (!lockedContext) {
      return;
    }

    lockedContextRef.current = lockedContext;
    openMenuRef.current = "block";
    setNodeInfo(lockedContext.nodeInfo);
    setDragHandleLocked(editor, true);
    setOpenMenu("block");
  }, [editor, isDragging, nodeInfo]);

  const getLockedReferencedVirtualElement = useCallback(() => {
    const lockedContext = lockedContextRef.current;

    if (!lockedContext) {
      return null;
    }

    return {
      getBoundingClientRect: () => lockedContext.rect,
    };
  }, []);

  const handleElementDragEnd = useCallback(() => {
    finishDragInteraction();
  }, [finishDragInteraction]);

  const handleElementDragStart = useCallback(() => {
    dragStartedRef.current = true;
    suppressNextClick();
    setIsDragging(true);
    closeMenus();
  }, [closeMenus, suppressNextClick]);

  const convertCurrentBlock = useCallback(
    (target: BlockConversionTarget) => {
      if (!editor || !nodeInfo || !canRunBlockConversion(nodeInfo, target) || isDragging || dragStartedRef.current) {
        return;
      }

      const selectionPos = getSafeSelectionPos(editor, nodeInfo);

      if (selectionPos === null) {
        return;
      }

      const chain = editor.chain().focus().setTextSelection(selectionPos);

      if (nodeInfo.typeName === "blockquote" && target !== "blockquote") {
        chain.toggleBlockquote();
      }

      if (target === "paragraph") {
        chain.setParagraph().run();
      } else if (target === "heading2") {
        chain.setNode("heading", { level: 2 }).run();
      } else if (target === "heading3") {
        chain.setNode("heading", { level: 3 }).run();
      } else if (target === "blockquote") {
        if (nodeInfo.typeName !== "blockquote") {
          chain.toggleBlockquote().run();
        } else {
          chain.run();
        }
      } else {
        chain.toggleCodeBlock().run();
      }

      closeMenus();
    },
    [closeMenus, editor, isDragging, nodeInfo],
  );

  const insertSlashParagraphBelow = useCallback(() => {
    if (!editor || !nodeInfo || !canInsertSlashParagraphBelow(nodeInfo) || isDragging || dragStartedRef.current) {
      return;
    }

    const insertPos = getSafeInsertPos(editor, nodeInfo);

    if (insertPos === null) {
      return;
    }

    editor
      .chain()
      .focus()
      .insertContentAt(insertPos, slashParagraphContent)
      .setTextSelection(insertPos + 2)
      .run();

    closeMenus();
  }, [closeMenus, editor, isDragging, nodeInfo]);

  const resetCurrentBlockFormatting = useCallback(() => {
    if (!editor || !nodeInfo || nodeInfo.typeName === "image" || isDragging || dragStartedRef.current) {
      return;
    }

    const selectionPos = getSafeSelectionPos(editor, nodeInfo);

    if (selectionPos !== null) {
      editor.commands.setTextSelection(selectionPos);
    }

    clearBlockFormatting(editor);
    closeMenus();
  }, [closeMenus, editor, isDragging, nodeInfo]);

  const isBlockMenuOpen = openMenu === "block";

  useEffect(() => {
    onMenuOpenChange?.(isBlockMenuOpen);
  }, [isBlockMenuOpen, onMenuOpenChange]);

  useEffect(() => {
    setDragHandleLocked(editor, isBlockMenuOpen);

    return () => {
      if (isBlockMenuOpen) {
        setDragHandleLocked(editor, false);
      }
    };
  }, [editor, isBlockMenuOpen]);

  useEffect(() => {
    onDragStateChange?.(isDragging);
  }, [isDragging, onDragStateChange]);

  useEffect(() => {
    if (isOverlayBlocked) {
      closeMenus();
    }
  }, [closeMenus, isOverlayBlocked]);

  useEffect(() => {
    if (!editor) {
      return;
    }

    function closeIfLockedContextInvalid() {
      const lockedContext = lockedContextRef.current;

      if (openMenuRef.current !== "block" || !lockedContext || !editor || editor.isDestroyed) {
        return;
      }

      if (!isLockedBlockHandleContextValid(editor, lockedContext)) {
        closeMenus();
      }
    }

    editor.on("transaction", closeIfLockedContextInvalid);
    editor.on("blur", closeMenus);

    return () => {
      editor.off("transaction", closeIfLockedContextInvalid);
      editor.off("blur", closeMenus);
    };
  }, [closeMenus, editor]);

  useEffect(() => {
    if (!openMenu) {
      return;
    }

    function handlePointerDown(event: PointerEvent) {
      if (!handleContentRef.current?.contains(event.target as Node)) {
        closeMenus();
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        closeMenus();
      }
    }

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [closeMenus, openMenu]);

  useEffect(() => {
    if (!isDragging) {
      return;
    }

    document.addEventListener("dragend", finishDragInteraction);
    document.addEventListener("drop", finishDragInteraction);
    document.addEventListener("pointerup", finishDragInteraction);

    return () => {
      document.removeEventListener("dragend", finishDragInteraction);
      document.removeEventListener("drop", finishDragInteraction);
      document.removeEventListener("pointerup", finishDragInteraction);
    };
  }, [finishDragInteraction, isDragging]);

  useEffect(() => {
    return () => {
      if (suppressClickTimerRef.current !== null) {
        window.clearTimeout(suppressClickTimerRef.current);
      }

      if (finishDragTimerRef.current !== null) {
        window.clearTimeout(finishDragTimerRef.current);
      }

      lockedContextRef.current = null;
      openMenuRef.current = null;
      setDragHandleLocked(editor, false);
      onDragStateChange?.(false);
      onMenuOpenChange?.(false);
    };
  }, [editor, onDragStateChange, onMenuOpenChange]);

  if (!editor) {
    return null;
  }

  return (
    <DragHandle
      className={[
        "knowledge-drag-handle",
        isBlockMenuOpen ? "is-block-menu-open" : "",
        isOverlayBlocked ? "is-overlay-blocked" : "",
        isTableControlHovered ? "is-table-control-hovered" : "",
      ].join(" ")}
      editor={editor}
      getReferencedVirtualElement={getLockedReferencedVirtualElement}
      onElementDragEnd={handleElementDragEnd}
      onElementDragStart={handleElementDragStart}
      onNodeChange={handleNodeChange}
      pluginKey="knowledge-drag-handle"
    >
      <div ref={handleContentRef} className="knowledge-drag-handle-content">
        {isBlockMenuOpen ? (
          <button
            aria-label="关闭块菜单"
            className="knowledge-block-menu-dismiss-layer"
            onClick={(event) => {
              event.preventDefault();
              event.stopPropagation();
            }}
            onPointerDown={(event) => {
              event.preventDefault();
              event.stopPropagation();
              closeMenus();
            }}
            type="button"
          />
        ) : null}
        <button
          aria-expanded={false}
          aria-label="在下方插入"
          className="knowledge-block-control-button knowledge-block-control-insert"
          draggable={false}
          onPointerDown={(event) => {
            event.preventDefault();
            event.stopPropagation();

            if (isBlockMenuOpen) {
              closeMenus();
              suppressNextClick();
            }
          }}
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();

            if (!nodeInfo || isBlockMenuOpen || dragStartedRef.current || isDragging || suppressClickRef.current) {
              return;
            }

            insertSlashParagraphBelow();
          }}
          title="在下方插入"
          type="button"
        >
          <Plus aria-hidden="true" className="h-3.5 w-3.5" />
        </button>
        <button
          aria-expanded={isBlockMenuOpen}
          aria-label="块操作菜单"
          className="knowledge-block-control-button knowledge-drag-handle-button"
          onPointerDown={(event) => {
            event.preventDefault();
            event.stopPropagation();

            if (openMenuRef.current !== null) {
              closeMenus();
              suppressNextClick();
            }
          }}
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();

            if (!nodeInfo || dragStartedRef.current || isDragging || suppressClickRef.current) {
              return;
            }

            if (openMenuRef.current === "block") {
              closeMenus();
              return;
            }

            openBlockMenu();
          }}
          title="块操作菜单"
          type="button"
        >
          <GripVertical aria-hidden="true" className="h-4 w-4" />
        </button>
        {isBlockMenuOpen && nodeInfo ? (
          <BlockHandleMenu
            nodeInfo={nodeInfo}
            onClearFormatting={resetCurrentBlockFormatting}
            onConvertBlock={convertCurrentBlock}
          />
        ) : null}
      </div>
    </DragHandle>
  );
}

function setDragHandleLocked(editor: Editor | null, isLocked: boolean) {
  if (!editor || editor.isDestroyed) {
    return;
  }

  editor.view.dispatch(editor.state.tr.setMeta("lockDragHandle", isLocked));
}

function createLockedBlockHandleContext(
  editor: Editor,
  nodeInfo: BlockHandleNodeInfo,
): LockedBlockHandleContext | null {
  if (nodeInfo.pos === null || !isBlockHandleNodeInfoValid(editor, nodeInfo)) {
    return null;
  }

  const nodeDom = editor.view.nodeDOM(nodeInfo.pos);
  const blockElement = nodeDom instanceof Element ? nodeDom : null;

  if (!blockElement) {
    return null;
  }

  return {
    nodeInfo: { ...nodeInfo },
    rect: blockElement.getBoundingClientRect(),
  };
}

function isLockedBlockHandleContextValid(editor: Editor, lockedContext: LockedBlockHandleContext) {
  return isBlockHandleNodeInfoValid(editor, lockedContext.nodeInfo);
}

function isBlockHandleNodeInfoValid(editor: Editor, nodeInfo: BlockHandleNodeInfo) {
  if (nodeInfo.pos === null || nodeInfo.pos < 0 || nodeInfo.pos > editor.state.doc.content.size) {
    return false;
  }

  const currentNode = editor.state.doc.nodeAt(nodeInfo.pos);

  return currentNode?.type.name === nodeInfo.typeName && currentNode.nodeSize === nodeInfo.nodeSize;
}

const complexBlockTypes = new Set([
  "bulletList",
  "orderedList",
  "taskList",
  "listItem",
  "taskItem",
  "image",
  "imageBlock",
  "table",
  "tableRow",
  "tableCell",
  "tableHeader",
  "details",
]);
const convertibleBlockTypes = new Set(["paragraph", "heading", "blockquote"]);
const unsupportedSlashInsertSourceTypes = new Set(["listItem", "taskItem", "tableCell", "tableHeader", "tableRow"]);

const blockConversionActions: Array<{ icon: LucideIcon; label: string; target: BlockConversionTarget }> = [
  { icon: Pilcrow, label: "段落", target: "paragraph" },
  { icon: Heading2, label: "二级标题", target: "heading2" },
  { icon: Heading3, label: "三级标题", target: "heading3" },
  { icon: Quote, label: "引用", target: "blockquote" },
  { icon: SquareCode, label: "代码块", target: "codeBlock" },
];

function getBlockHandleNodeInfo(node: ProseMirrorNode, pos: number): BlockHandleNodeInfo {
  const typeName = node.type.name;
  const level = typeof node.attrs.level === "number" ? node.attrs.level : undefined;

  return {
    typeName,
    pos: Number.isInteger(pos) && pos >= 0 ? pos : null,
    nodeSize: node.nodeSize,
    level,
    isComplex: complexBlockTypes.has(typeName),
  };
}

function BlockHandleMenu({
  nodeInfo,
  onClearFormatting,
  onConvertBlock,
}: {
  nodeInfo: BlockHandleNodeInfo;
  onClearFormatting: () => void;
  onConvertBlock: (target: BlockConversionTarget) => void;
}) {
  const canClearFormatting = nodeInfo.typeName !== "image" && nodeInfo.typeName !== "imageBlock";

  return (
    <div className="knowledge-block-menu" role="menu" aria-label="块操作菜单">
      <div className="knowledge-block-menu-header">
        <span>当前块</span>
        <strong>{formatBlockType(nodeInfo)}</strong>
      </div>
      {getBlockMenuNote(nodeInfo) ? <div className="knowledge-block-menu-note">{getBlockMenuNote(nodeInfo)}</div> : null}

      <MenuSection label="转换为">
        {blockConversionActions.map((action) => (
          <MenuActionButton
            key={action.target}
            disabled={!canRunBlockConversion(nodeInfo, action.target)}
            icon={action.icon}
            label={action.label}
            onClick={() => onConvertBlock(action.target)}
          />
        ))}
      </MenuSection>

      <MenuSection label="格式">
        <MenuActionButton
          disabled={!canClearFormatting}
          icon={AlignLeft}
          label="重置格式"
          onClick={onClearFormatting}
        />
      </MenuSection>

      <MenuSection label="危险操作">
        <MenuActionButton danger disabled icon={Trash2} label="删除" />
      </MenuSection>
    </div>
  );
}

function MenuSection({ children, label }: { children: ReactNode; label: string }) {
  return (
    <div className="knowledge-block-menu-section">
      <div className="knowledge-block-menu-section-label">{label}</div>
      {children}
    </div>
  );
}

function MenuActionButton({
  danger,
  disabled,
  icon: Icon,
  label,
  onClick,
}: {
  danger?: boolean;
  disabled?: boolean;
  icon: LucideIcon;
  label: string;
  onClick?: () => void;
}) {
  return (
    <button
      className={danger ? "knowledge-block-menu-item knowledge-block-menu-item-danger" : "knowledge-block-menu-item"}
      disabled={disabled}
      onClick={(event) => {
        event.stopPropagation();
        onClick?.();
      }}
      onMouseDown={(event) => {
        event.preventDefault();
        event.stopPropagation();
      }}
      role="menuitem"
      type="button"
    >
      <Icon aria-hidden="true" className="h-4 w-4" />
      <span>{label}</span>
    </button>
  );
}

function formatBlockType(nodeInfo: BlockHandleNodeInfo) {
  if (nodeInfo.typeName === "heading" && nodeInfo.level === 2) {
    return "二级标题";
  }

  if (nodeInfo.typeName === "heading" && nodeInfo.level === 3) {
    return "三级标题";
  }

  if (nodeInfo.typeName === "heading") {
    return "标题";
  }

  const labels: Record<string, string> = {
    blockquote: "引用",
    bulletList: "列表",
    codeBlock: "代码块",
    details: "折叠块",
    image: "图片",
    imageBlock: "图片块",
    listItem: "列表项",
    orderedList: "有序列表",
    paragraph: "段落",
    table: "表格",
    tableCell: "表格单元格",
    tableHeader: "表头单元格",
    tableRow: "表格行",
    taskItem: "任务项",
    taskList: "任务列表",
  };

  return labels[nodeInfo.typeName] ?? nodeInfo.typeName;
}

function canConvertBlock(nodeInfo: BlockHandleNodeInfo) {
  return nodeInfo.pos !== null && !nodeInfo.isComplex && convertibleBlockTypes.has(nodeInfo.typeName);
}

function canRunBlockConversion(nodeInfo: BlockHandleNodeInfo, target: BlockConversionTarget) {
  if (!canConvertBlock(nodeInfo)) {
    return false;
  }

  if (
    (target === "paragraph" && nodeInfo.typeName === "paragraph") ||
    (target === "heading2" && nodeInfo.typeName === "heading" && nodeInfo.level === 2) ||
    (target === "heading3" && nodeInfo.typeName === "heading" && nodeInfo.level === 3) ||
    (target === "blockquote" && nodeInfo.typeName === "blockquote")
  ) {
    return false;
  }

  return true;
}

const slashParagraphContent = {
  type: "paragraph",
  content: [{ type: "text", text: "/" }],
};

function canInsertSlashParagraphBelow(nodeInfo: BlockHandleNodeInfo) {
  return nodeInfo.pos !== null && !unsupportedSlashInsertSourceTypes.has(nodeInfo.typeName);
}

function getBlockMenuNote(nodeInfo: BlockHandleNodeInfo) {
  if (nodeInfo.typeName === "codeBlock") {
    return "代码块暂不支持转换为普通文本。";
  }

  if (nodeInfo.isComplex || !canConvertBlock(nodeInfo)) {
    return "此类复杂块暂不支持转换。";
  }

  return "";
}

function getSafeInsertPos(editor: Editor, nodeInfo: BlockHandleNodeInfo) {
  if (nodeInfo.pos === null) {
    return null;
  }

  const docSize = editor.state.doc.content.size;
  const insertPos = nodeInfo.pos + nodeInfo.nodeSize;

  if (!Number.isInteger(insertPos) || insertPos < 0 || insertPos > docSize) {
    return null;
  }

  return insertPos;
}

function getSafeSelectionPos(editor: Editor, nodeInfo: BlockHandleNodeInfo) {
  if (nodeInfo.pos === null) {
    return null;
  }

  const docSize = editor.state.doc.content.size;
  const selectionPos = Math.min(Math.max(nodeInfo.pos + 1, 1), docSize);

  return Number.isInteger(selectionPos) ? selectionPos : null;
}
