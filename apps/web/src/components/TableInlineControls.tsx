import type { Editor } from "@tiptap/react";
import { Columns3, Plus, Rows3, Trash2 } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import type { CSSProperties, MouseEvent as ReactMouseEvent, ReactNode } from "react";

type TableInlineControlsProps = {
  editor: Editor | null;
  isBlockDragging?: boolean;
  isBlockMenuOpen?: boolean;
  onControlHoverChange?: (isHovered: boolean) => void;
  onControlsVisibleChange?: (isVisible: boolean) => void;
  onMenuOpenChange?: (isOpen: boolean) => void;
};

type TableControlRect = {
  height: number;
  left: number;
  top: number;
  width: number;
};

type TableControlGeometry = {
  isLastColumn: boolean;
  isLastRow: boolean;
  cell: TableControlRect;
  row: TableControlRect;
  table: TableControlRect;
};

type TableContextDom = {
  cell: HTMLTableCellElement;
  row: HTMLTableRowElement;
  table: HTMLTableElement;
};

type TableControlContext = TableContextDom & {
  cellPos: number | null;
};

type TableMenuType = "row" | "column";
type EditorCommandChain = ReturnType<Editor["chain"]>;

const TABLE_CONTROLS_HIDE_DELAY = 140;
const TABLE_CONTROL_HOVER_RELEASE_DELAY = 180;

export function TableInlineControls({
  editor,
  isBlockDragging = false,
  isBlockMenuOpen = false,
  onControlHoverChange,
  onControlsVisibleChange,
  onMenuOpenChange,
}: TableInlineControlsProps) {
  const [geometry, setGeometry] = useState<TableControlGeometry | null>(null);
  const [activeMenu, setActiveMenu] = useState<TableMenuType | null>(null);
  const frameRef = useRef<number | null>(null);
  const controlHoverTimerRef = useRef<number | null>(null);
  const hoverClearTimerRef = useRef<number | null>(null);
  const hoverContextRef = useRef<TableControlContext | null>(null);
  const menuContextRef = useRef<TableControlContext | null>(null);

  const cancelHoverClear = useCallback(() => {
    if (hoverClearTimerRef.current !== null) {
      window.clearTimeout(hoverClearTimerRef.current);
      hoverClearTimerRef.current = null;
    }
  }, []);

  const cancelControlHoverClear = useCallback(() => {
    if (controlHoverTimerRef.current !== null) {
      window.clearTimeout(controlHoverTimerRef.current);
      controlHoverTimerRef.current = null;
    }
  }, []);

  const closeMenu = useCallback(() => {
    menuContextRef.current = null;
    setActiveMenu(null);
  }, []);

  const updatePosition = useCallback(() => {
    if (!editor || editor.isDestroyed || !editor.isEditable || isBlockDragging || isBlockMenuOpen) {
      hoverContextRef.current = null;
      setGeometry(null);
      closeMenu();
      return;
    }

    const lockedContext = activeMenu ? menuContextRef.current : null;

    if (lockedContext) {
      if (getValidatedCellSelectionPos(editor, lockedContext) === null) {
        setGeometry(null);
        closeMenu();
        return;
      }

      const nextGeometry = getGeometryFromContext(editor, lockedContext);

      if (!nextGeometry) {
        setGeometry(null);
        closeMenu();
        return;
      }

      setGeometry(nextGeometry);
      return;
    }

    if (hasRangeSelection(editor)) {
      hoverContextRef.current = null;
      setGeometry(null);
      return;
    }

    const hoverContext = hoverContextRef.current;
    const fallbackContext = editor.isFocused ? findSelectionTableContext(editor) : null;
    const context = hoverContext && isTableContextConnected(editor, hoverContext) ? hoverContext : fallbackContext;
    const nextGeometry = context ? getGeometryFromContext(editor, context) : null;

    if (!nextGeometry) {
      setGeometry(null);
      return;
    }

    setGeometry(nextGeometry);
  }, [activeMenu, closeMenu, editor, isBlockDragging, isBlockMenuOpen]);

  const scheduleUpdate = useCallback(() => {
    if (frameRef.current !== null) {
      window.cancelAnimationFrame(frameRef.current);
    }

    frameRef.current = window.requestAnimationFrame(() => {
      frameRef.current = null;
      updatePosition();
    });
  }, [updatePosition]);

  const scheduleHoverClear = useCallback(() => {
    if (activeMenu) {
      return;
    }

    cancelHoverClear();

    hoverClearTimerRef.current = window.setTimeout(() => {
      hoverContextRef.current = null;
      hoverClearTimerRef.current = null;
      scheduleUpdate();
    }, TABLE_CONTROLS_HIDE_DELAY);
  }, [activeMenu, cancelHoverClear, scheduleUpdate]);

  const handleControlPointerEnter = useCallback(() => {
    cancelControlHoverClear();
    cancelHoverClear();
    onControlHoverChange?.(true);
  }, [cancelControlHoverClear, cancelHoverClear, onControlHoverChange]);

  const handleControlPointerLeave = useCallback(() => {
    scheduleHoverClear();
    cancelControlHoverClear();

    controlHoverTimerRef.current = window.setTimeout(() => {
      controlHoverTimerRef.current = null;
      onControlHoverChange?.(false);

      if (editor && !editor.isDestroyed && !isPointerInEditorControlOrDragHandle(editor)) {
        editor.view.dispatch(editor.state.tr.setMeta("hideDragHandle", true));
      }
    }, TABLE_CONTROL_HOVER_RELEASE_DELAY);
  }, [cancelControlHoverClear, editor, onControlHoverChange, scheduleHoverClear]);

  useEffect(() => {
    if (!editor) {
      cancelHoverClear();
      cancelControlHoverClear();
      hoverContextRef.current = null;
      setGeometry(null);
      closeMenu();
      return;
    }

    scheduleUpdate();

    editor.on("selectionUpdate", scheduleUpdate);
    editor.on("update", scheduleUpdate);
    window.addEventListener("resize", scheduleUpdate);
    window.addEventListener("scroll", scheduleUpdate, true);

    return () => {
      editor.off("selectionUpdate", scheduleUpdate);
      editor.off("update", scheduleUpdate);
      window.removeEventListener("resize", scheduleUpdate);
      window.removeEventListener("scroll", scheduleUpdate, true);

      if (frameRef.current !== null) {
        window.cancelAnimationFrame(frameRef.current);
        frameRef.current = null;
      }
    };
  }, [cancelControlHoverClear, cancelHoverClear, closeMenu, editor, scheduleUpdate]);

  useEffect(() => {
    if (!editor) {
      return;
    }

    function clearTableControls() {
      cancelHoverClear();
      cancelControlHoverClear();
      hoverContextRef.current = null;
      setGeometry(null);
      closeMenu();
      onControlHoverChange?.(false);
    }

    editor.on("blur", clearTableControls);

    return () => {
      editor.off("blur", clearTableControls);
    };
  }, [cancelControlHoverClear, cancelHoverClear, closeMenu, editor, onControlHoverChange]);

  useEffect(() => {
    if (!editor) {
      return;
    }

    const editorDom = editor.view.dom;

    function handlePointerMove(event: PointerEvent) {
      if (!editor || editor.isDestroyed || !editor.isEditable || isBlockDragging || isBlockMenuOpen || activeMenu) {
        return;
      }

      if (hasRangeSelection(editor)) {
        hoverContextRef.current = null;
        scheduleUpdate();
        return;
      }

      const target = event.target;

      if (!(target instanceof Element)) {
        scheduleHoverClear();
        return;
      }

      if (target.closest(".knowledge-table-inline-control, .knowledge-table-inline-menu")) {
        cancelHoverClear();
        return;
      }

      const context = getTableControlContext(editor, target);

      if (context && editor.view.dom.contains(context.table)) {
        cancelHoverClear();
        hoverContextRef.current = context;
        scheduleUpdate();
        return;
      }

      scheduleHoverClear();
    }

    editorDom.addEventListener("pointermove", handlePointerMove);
    editorDom.addEventListener("pointerleave", scheduleHoverClear);

    return () => {
      editorDom.removeEventListener("pointermove", handlePointerMove);
      editorDom.removeEventListener("pointerleave", scheduleHoverClear);
    };
  }, [
    activeMenu,
    cancelHoverClear,
    editor,
    isBlockDragging,
    isBlockMenuOpen,
    scheduleHoverClear,
    scheduleUpdate,
  ]);

  useEffect(() => {
    scheduleUpdate();
  }, [scheduleUpdate]);

  useEffect(() => {
    onControlsVisibleChange?.(geometry !== null);
  }, [geometry, onControlsVisibleChange]);

  useEffect(() => {
    if (geometry && !activeMenu) {
      return;
    }

    cancelControlHoverClear();
    onControlHoverChange?.(false);
  }, [activeMenu, cancelControlHoverClear, geometry, onControlHoverChange]);

  useEffect(() => {
    onMenuOpenChange?.(activeMenu !== null);
  }, [activeMenu, onMenuOpenChange]);

  useEffect(() => {
    if (!activeMenu) {
      return;
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        closeMenu();
      }
    }

    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [activeMenu, closeMenu]);

  useEffect(() => {
    return () => {
      if (hoverClearTimerRef.current !== null) {
        window.clearTimeout(hoverClearTimerRef.current);
        hoverClearTimerRef.current = null;
      }

      if (controlHoverTimerRef.current !== null) {
        window.clearTimeout(controlHoverTimerRef.current);
        controlHoverTimerRef.current = null;
      }

      if (frameRef.current !== null) {
        window.cancelAnimationFrame(frameRef.current);
        frameRef.current = null;
      }

      onControlHoverChange?.(false);
      onControlsVisibleChange?.(false);
      onMenuOpenChange?.(false);
    };
  }, [onControlHoverChange, onControlsVisibleChange, onMenuOpenChange]);

  if (!editor || !geometry) {
    return null;
  }

  const runTableCommand = (command: (chain: EditorCommandChain) => EditorCommandChain) => {
    const context = menuContextRef.current ?? hoverContextRef.current ?? findSelectionTableContext(editor);
    const cellPos = context ? getValidatedCellSelectionPos(editor, context) : null;

    if (!context || cellPos === null) {
      hoverContextRef.current = null;
      setGeometry(null);
      closeMenu();
      return;
    }

    command(editor.chain().focus().setTextSelection(cellPos)).run();
    hoverContextRef.current = null;
    setGeometry(null);
    closeMenu();
    scheduleUpdate();
  };

  const openMenu = (menu: TableMenuType) => {
    if (activeMenu === menu) {
      closeMenu();
      return;
    }

    const context = hoverContextRef.current ?? findSelectionTableContext(editor);

    if (!context || !isTableContextConnected(editor, context)) {
      return;
    }

    menuContextRef.current = context;
    setGeometry(getGeometryFromContext(editor, context));
    setActiveMenu(menu);
  };

  const tableMenuOpen = activeMenu !== null;
  const showRowHandle = !tableMenuOpen || activeMenu === "row";
  const showColumnHandle = !tableMenuOpen || activeMenu === "column";

  return (
    <>
      {tableMenuOpen ? (
        <button
          aria-label="Close table menu"
          className="knowledge-table-menu-dismiss-layer"
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
          }}
          onPointerDown={(event) => {
            event.preventDefault();
            event.stopPropagation();
            closeMenu();
          }}
          type="button"
        />
      ) : null}

      {showRowHandle ? (
        <button
          aria-label="Open row menu"
          className={[
            "knowledge-table-inline-control knowledge-table-inline-control-row-handle",
            activeMenu === "row" ? "knowledge-table-inline-control-active" : "",
          ].join(" ")}
          onClick={(event) => {
            event.stopPropagation();
            openMenu("row");
          }}
          onMouseDown={preventTableControlMouseDown}
          onPointerEnter={handleControlPointerEnter}
          onPointerLeave={handleControlPointerLeave}
          style={{
            height: `${geometry.row.height}px`,
            left: `${geometry.table.left - 14}px`,
            top: `${geometry.row.top}px`,
          }}
          title="Row actions"
          type="button"
        >
          <span aria-hidden="true" className="knowledge-table-row-handle-dots" />
        </button>
      ) : null}

      {showColumnHandle ? (
        <button
          aria-label="Open column menu"
          className={[
            "knowledge-table-inline-control knowledge-table-inline-control-column-handle",
            activeMenu === "column" ? "knowledge-table-inline-control-active" : "",
          ].join(" ")}
          onClick={(event) => {
            event.stopPropagation();
            openMenu("column");
          }}
          onMouseDown={preventTableControlMouseDown}
          onPointerEnter={handleControlPointerEnter}
          onPointerLeave={handleControlPointerLeave}
          style={{
            left: `${geometry.cell.left}px`,
            top: `${geometry.table.top - 14}px`,
            width: `${geometry.cell.width}px`,
          }}
          title="Column actions"
          type="button"
        >
          <span aria-hidden="true" className="knowledge-table-column-handle-dots" />
        </button>
      ) : null}

      {!tableMenuOpen ? (
        <>
          {geometry.isLastColumn ? (
            <button
              aria-label="Add table column"
              className="knowledge-table-inline-control knowledge-table-inline-control-column"
              onClick={(event) => {
                event.stopPropagation();
                runTableCommand((chain) => chain.addColumnAfter());
              }}
              onMouseDown={preventTableControlMouseDown}
              onPointerEnter={handleControlPointerEnter}
              onPointerLeave={handleControlPointerLeave}
              style={{
                height: `${geometry.table.height}px`,
                left: `${geometry.table.left + geometry.table.width + 8}px`,
                top: `${geometry.table.top}px`,
              }}
              title="Add column"
              type="button"
            >
              <Plus aria-hidden="true" className="h-3.5 w-3.5" />
            </button>
          ) : null}
          {geometry.isLastRow ? (
            <>
              <div
                aria-hidden="true"
                className="knowledge-table-inline-control-bridge knowledge-table-inline-control-bridge-bottom"
                onPointerEnter={handleControlPointerEnter}
                onPointerLeave={handleControlPointerLeave}
                style={{
                  height: "34px",
                  left: `${geometry.table.left}px`,
                  top: `${geometry.table.top + geometry.table.height}px`,
                  width: `${geometry.table.width}px`,
                }}
              />
              <button
                aria-label="Add table row"
                className="knowledge-table-inline-control knowledge-table-inline-control-row"
                onClick={(event) => {
                  event.stopPropagation();
                  runTableCommand((chain) => chain.addRowAfter());
                }}
                onMouseDown={preventTableControlMouseDown}
                onPointerEnter={handleControlPointerEnter}
                onPointerLeave={handleControlPointerLeave}
                style={{
                  left: `${geometry.table.left}px`,
                  top: `${geometry.table.top + geometry.table.height + 4}px`,
                  width: `${geometry.table.width}px`,
                }}
                title="Add row"
                type="button"
              >
                <Plus aria-hidden="true" className="h-3.5 w-3.5" />
              </button>
            </>
          ) : null}
        </>
      ) : null}

      {activeMenu === "row" ? (
        <TableInlineMenu
          ariaLabel="Row actions"
          onPointerEnter={cancelHoverClear}
          style={{
            left: `${Math.max(12, geometry.table.left - 204)}px`,
            top: `${Math.max(12, geometry.row.top)}px`,
          }}
          title="Current row"
        >
          <TableMenuButton
            icon={Rows3}
            label="Insert row above"
            onClick={() => runTableCommand((chain) => chain.addRowBefore())}
          />
          <TableMenuButton
            icon={Rows3}
            label="Insert row below"
            onClick={() => runTableCommand((chain) => chain.addRowAfter())}
          />
          <TableMenuButton
            danger
            icon={Trash2}
            label="Delete current row"
            onClick={() => runTableCommand((chain) => chain.deleteRow())}
          />
        </TableInlineMenu>
      ) : null}

      {activeMenu === "column" ? (
        <TableInlineMenu
          ariaLabel="Column actions"
          onPointerEnter={cancelHoverClear}
          style={{
            left: `${Math.max(12, geometry.cell.left)}px`,
            top: `${Math.max(12, geometry.table.top - 154)}px`,
          }}
          title="Current column"
        >
          <TableMenuButton
            icon={Columns3}
            label="Insert column left"
            onClick={() => runTableCommand((chain) => chain.addColumnBefore())}
          />
          <TableMenuButton
            icon={Columns3}
            label="Insert column right"
            onClick={() => runTableCommand((chain) => chain.addColumnAfter())}
          />
          <TableMenuButton
            danger
            icon={Trash2}
            label="Delete current column"
            onClick={() => runTableCommand((chain) => chain.deleteColumn())}
          />
        </TableInlineMenu>
      ) : null}
    </>
  );
}

function TableInlineMenu({
  ariaLabel,
  children,
  onPointerEnter,
  style,
  title,
}: {
  ariaLabel: string;
  children: ReactNode;
  onPointerEnter: () => void;
  style: CSSProperties;
  title: string;
}) {
  return (
    <div
      aria-label={ariaLabel}
      className="knowledge-table-inline-menu"
      onPointerDown={(event) => event.stopPropagation()}
      onPointerEnter={onPointerEnter}
      role="menu"
      style={style}
    >
      <div className="knowledge-table-inline-menu-title">{title}</div>
      {children}
    </div>
  );
}

function TableMenuButton({
  danger,
  icon: Icon,
  label,
  onClick,
}: {
  danger?: boolean;
  icon: LucideIcon;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      aria-label={label}
      className={[
        "knowledge-table-inline-menu-item",
        danger ? "knowledge-table-inline-menu-item-danger" : "",
      ].join(" ")}
      onClick={(event) => {
        event.stopPropagation();
        onClick();
      }}
      onMouseDown={preventTableControlMouseDown}
      role="menuitem"
      title={label}
      type="button"
    >
      <Icon aria-hidden="true" className="h-3.5 w-3.5" />
      <span>{label}</span>
    </button>
  );
}

function preventTableControlMouseDown(event: ReactMouseEvent<HTMLElement>) {
  event.preventDefault();
  event.stopPropagation();
}

function isPointerInEditorControlOrDragHandle(editor: Editor) {
  return (
    editor.view.dom.matches(":hover") ||
    Boolean(
      document.querySelector(
        ".knowledge-drag-handle:hover, .knowledge-table-inline-control:hover, .knowledge-table-inline-control-bridge:hover, .knowledge-table-inline-menu:hover",
      ),
    )
  );
}

function findSelectionTableContext(editor: Editor): TableControlContext | null {
  const { selection } = editor.state;
  const { $from } = selection;

  const domAtPos = editor.view.domAtPos(selection.anchor);
  const nearbyElement =
    domAtPos.node instanceof Element ? domAtPos.node : domAtPos.node.parentElement;
  const nearbyContext = getTableControlContext(editor, nearbyElement);

  if (nearbyContext) {
    return nearbyContext;
  }

  for (let depth = $from.depth; depth > 0; depth -= 1) {
    const nodeName = $from.node(depth).type.name;

    if (nodeName !== "tableCell" && nodeName !== "tableHeader") {
      continue;
    }

    const nodeDom = editor.view.nodeDOM($from.before(depth));
    const context = getTableControlContext(editor, nodeDom instanceof Element ? nodeDom : null);

    if (context) {
      return context;
    }
  }

  return null;
}

function getTableControlContext(editor: Editor, element: Element | null): TableControlContext | null {
  const domContext = getTableContextFromElement(element);

  if (!domContext) {
    return null;
  }

  return {
    ...domContext,
    cellPos: getCellSelectionPos(editor, domContext.cell),
  };
}

function getTableContextFromElement(element: Element | null): TableContextDom | null {
  const cell = element?.closest("td, th");

  if (!(cell instanceof HTMLTableCellElement)) {
    return null;
  }

  const row = cell.closest("tr");
  const table = cell.closest("table");

  if (!(row instanceof HTMLTableRowElement) || !(table instanceof HTMLTableElement)) {
    return null;
  }

  return { cell, row, table };
}

function getGeometryFromContext(editor: Editor, context: TableControlContext): TableControlGeometry | null {
  if (!isTableContextConnected(editor, context)) {
    return null;
  }

  const tableRect = context.table.getBoundingClientRect();
  const rowRect = context.row.getBoundingClientRect();
  const cellRect = context.cell.getBoundingClientRect();

  if (tableRect.width <= 0 || tableRect.height <= 0 || rowRect.height <= 0 || cellRect.width <= 0) {
    return null;
  }

  return {
    isLastColumn: isLastColumnCell(context),
    isLastRow: isLastTableRow(context),
    cell: toControlRect(cellRect),
    row: toControlRect(rowRect),
    table: toControlRect(tableRect),
  };
}

function isTableContextConnected(editor: Editor, context: TableContextDom) {
  return (
    context.cell.isConnected &&
    context.row.isConnected &&
    context.table.isConnected &&
    editor.view.dom.contains(context.table)
  );
}

function isLastTableRow(context: TableContextDom) {
  const rows = Array.from(context.table.rows);
  const lastRow = rows[rows.length - 1];

  return lastRow === context.row;
}

function isLastColumnCell(context: TableContextDom) {
  const cells = Array.from(context.row.cells);
  const lastCell = cells[cells.length - 1];

  return lastCell === context.cell;
}

function getCellSelectionPos(editor: Editor, cell: HTMLTableCellElement) {
  const rect = cell.getBoundingClientRect();
  const docSize = editor.state.doc.content.size;
  const candidates: number[] = [];
  const coordPos = editor.view.posAtCoords({
    left: rect.left + Math.min(14, Math.max(1, rect.width / 2)),
    top: rect.top + Math.min(14, Math.max(1, rect.height / 2)),
  })?.pos;

  if (typeof coordPos === "number") {
    candidates.push(coordPos);
  }

  try {
    candidates.push(editor.view.posAtDOM(cell, 0));
  } catch {
    // Ignore DOM positions that are no longer mapped after table changes.
  }

  for (const candidate of candidates) {
    if (Number.isInteger(candidate) && candidate >= 1 && candidate <= docSize) {
      return candidate;
    }
  }

  return null;
}

function getValidatedCellSelectionPos(editor: Editor, context: TableControlContext) {
  if (!isTableContextConnected(editor, context)) {
    return null;
  }

  const candidates = [context.cellPos, getCellSelectionPos(editor, context.cell)];

  for (const candidate of candidates) {
    if (candidate !== null && isValidTableCellPosition(editor, candidate)) {
      return candidate;
    }
  }

  return null;
}

function isValidTableCellPosition(editor: Editor, pos: number) {
  const docSize = editor.state.doc.content.size;

  if (!Number.isInteger(pos) || pos < 0 || pos > docSize) {
    return false;
  }

  try {
    const resolvedPos = editor.state.doc.resolve(pos);

    for (let depth = resolvedPos.depth; depth > 0; depth -= 1) {
      const nodeName = resolvedPos.node(depth).type.name;

      if (nodeName === "tableCell" || nodeName === "tableHeader") {
        return true;
      }
    }
  } catch {
    return false;
  }

  return false;
}

function hasRangeSelection(editor: Editor) {
  return !editor.state.selection.empty;
}

function toControlRect(rect: DOMRect): TableControlRect {
  return {
    height: rect.height,
    left: rect.left,
    top: rect.top,
    width: rect.width,
  };
}
