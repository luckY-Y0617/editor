import type { Editor } from "@tiptap/react";
import { Columns3, Rows3, Table2, Trash2 } from "lucide-react";
import { ToolbarButton, ToolbarGroup } from "./ToolbarControls";

export function TableToolbarGroup({ editor }: { editor: Editor }) {
  if (!isTableActive(editor)) {
    return null;
  }

  return (
    <ToolbarGroup contextual label="表格">
      <ToolbarButton
        icon={Rows3}
        label="加行"
        shortLabel="行+"
        variant="compact"
        onClick={() => editor.chain().focus().addRowAfter().run()}
      />
      <ToolbarButton
        icon={Rows3}
        label="删除行"
        shortLabel="删行"
        tone="danger"
        variant="compact"
        onClick={() => editor.chain().focus().deleteRow().run()}
      />
      <ToolbarButton
        icon={Columns3}
        label="加列"
        shortLabel="列+"
        variant="compact"
        onClick={() => editor.chain().focus().addColumnAfter().run()}
      />
      <ToolbarButton
        icon={Columns3}
        label="删除列"
        shortLabel="删列"
        tone="danger"
        variant="compact"
        onClick={() => editor.chain().focus().deleteColumn().run()}
      />
      <ToolbarButton
        icon={Trash2}
        label="删除表格"
        shortLabel="删表"
        tone="danger"
        variant="compact"
        onClick={() => editor.chain().focus().deleteTable().run()}
      />
    </ToolbarGroup>
  );
}

export function isTableActive(editor: Editor | null) {
  if (!editor || editor.isDestroyed) {
    return false;
  }

  if (editor.isActive("table")) {
    return true;
  }

  const { $from } = editor.state.selection;

  for (let depth = $from.depth; depth > 0; depth -= 1) {
    const nodeName = $from.node(depth).type.name;

    if (
      nodeName === "table" ||
      nodeName === "tableRow" ||
      nodeName === "tableCell" ||
      nodeName === "tableHeader"
    ) {
      return true;
    }
  }

  return false;
}

export function InsertTableButton({ editor }: { editor: Editor }) {
  return (
    <ToolbarButton
      icon={Table2}
      label="插入表格"
      onClick={() =>
        editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()
      }
    />
  );
}
