import { getSchemaByResolvedExtensions, resolveExtensions } from "@tiptap/core";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import { Details, DetailsContent, DetailsSummary } from "@tiptap/extension-details";
import ImageExtension from "@tiptap/extension-image";
import { Table } from "@tiptap/extension-table";
import TableCell from "@tiptap/extension-table-cell";
import TableHeader from "@tiptap/extension-table-header";
import TableRow from "@tiptap/extension-table-row";
import TaskItem from "@tiptap/extension-task-item";
import TaskList from "@tiptap/extension-task-list";
import StarterKit from "@tiptap/starter-kit";
import { Fragment, Schema, type Node as ProseMirrorNode, type NodeType } from "@tiptap/pm/model";
import { EditorState, TextSelection } from "@tiptap/pm/state";
import { createCommentAnchor } from "../components/TiptapEditor";
import { ImageBlock } from "./ImageBlock";
import { lowlight } from "../lib/lowlight";
import { describe, expect, test } from "../test/harness";
import {
  BLOCK_IDENTITY_TEXTBLOCK_NODE_TYPES,
  BLOCK_ID_ATTR,
  BlockIdentity,
  createBlockIdentityPlugin,
  createBlockIdRepairTransaction,
  isValidBlockId,
  migrateDocumentContentBlockIds,
} from "./BlockIdentity";
import type { JSONContent } from "@tiptap/react";

const testSchema = new Schema({
  nodes: {
    doc: {
      content: "block+",
    },
    paragraph: {
      attrs: {
        blockId: { default: null },
      },
      content: "inline*",
      group: "block",
      parseDOM: [{ tag: "p" }],
      toDOM: (node) => ["p", node.attrs, 0],
    },
    heading: {
      attrs: {
        blockId: { default: null },
        level: { default: 1 },
      },
      content: "inline*",
      group: "block",
      parseDOM: [{ tag: "h1" }],
      toDOM: (node) => [`h${node.attrs.level}`, node.attrs, 0],
    },
    blockquote: {
      content: "block+",
      group: "block",
      parseDOM: [{ tag: "blockquote" }],
      toDOM: () => ["blockquote", 0],
    },
    bulletList: {
      content: "listItem+",
      group: "block",
      parseDOM: [{ tag: "ul" }],
      toDOM: () => ["ul", 0],
    },
    listItem: {
      content: "paragraph block*",
      parseDOM: [{ tag: "li" }],
      toDOM: () => ["li", 0],
    },
    text: {
      group: "inline",
    },
  },
});

describe("BlockIdentity", () => {
  test("adds blockId schema support to current ProseMirror textblock node types only", () => {
    const schema = getSchemaByResolvedExtensions(
      resolveExtensions([
        StarterKit.configure({
          codeBlock: false,
          horizontalRule: false,
          link: false,
          underline: false,
        }),
        BlockIdentity,
        CodeBlockLowlight.configure({
          defaultLanguage: "plaintext",
          lowlight,
        }),
        Details,
        DetailsSummary,
        DetailsContent,
        TaskList,
        TaskItem,
        ImageExtension,
        ImageBlock,
        Table,
        TableRow,
        TableHeader,
        TableCell,
      ]),
    );
    const textblockNodeTypes = Object.entries(schema.nodes)
      .filter(([, nodeType]) => nodeType.isTextblock)
      .map(([name]) => name)
      .sort();

    expect(textblockNodeTypes).toEqual([...BLOCK_IDENTITY_TEXTBLOCK_NODE_TYPES].sort());

    for (const nodeTypeName of textblockNodeTypes) {
      expect(hasBlockIdSchemaAttr(schema.nodes[nodeTypeName])).toBe(true);
    }

    for (const nodeTypeName of ["blockquote", "listItem", "taskItem", "details", "tableCell", "image", "imageBlock"]) {
      expect(hasBlockIdSchemaAttr(schema.nodes[nodeTypeName])).toBe(false);
    }
  });

  test("old JSON without ids gets textblock ids", () => {
    const migrated = migrateDocumentContentBlockIds(
      {
        type: "doc",
        content: [
          paragraph("Alpha"),
          heading("Bravo"),
          {
            type: "blockquote",
            content: [paragraph("Charlie")],
          },
        ],
      },
      { createId: createSequentialBlockIdFactory("old") },
    );

    expect(blockIdAt(migrated, [0])).toBe("blk_old00000001");
    expect(blockIdAt(migrated, [1])).toBe("blk_old00000002");
    expect(hasJsonBlockIdAt(migrated, [2])).toBe(false);
    expect(blockIdAt(migrated, [2, 0])).toBe("blk_old00000003");
  });

  test("existing valid unique ids are preserved", () => {
    const migrated = migrateDocumentContentBlockIds(
      {
        type: "doc",
        content: [
          paragraph("Alpha", "blk_existing0001"),
          paragraph("Bravo", "blk_existing0002"),
        ],
      },
      { createId: createSequentialBlockIdFactory("unused") },
    );

    expect(blockIdAt(migrated, [0])).toBe("blk_existing0001");
    expect(blockIdAt(migrated, [1])).toBe("blk_existing0002");
  });

  test("duplicate ids preserve the first occurrence and repair later duplicates deterministically", () => {
    const migrated = migrateDocumentContentBlockIds(
      {
        type: "doc",
        content: [
          paragraph("Alpha", "blk_duplicate0001"),
          paragraph("Bravo", "blk_duplicate0001"),
          paragraph("Charlie", "blk_duplicate0001"),
        ],
      },
      { createId: createSequentialBlockIdFactory("dup") },
    );

    expect(blockIdAt(migrated, [0])).toBe("blk_duplicate0001");
    expect(blockIdAt(migrated, [1])).toBe("blk_dup00000001");
    expect(blockIdAt(migrated, [2])).toBe("blk_dup00000002");
  });

  test("invalid id formats are repaired", () => {
    const migrated = migrateDocumentContentBlockIds(
      {
        type: "doc",
        content: [
          paragraph("Alpha", "paragraph-1"),
          paragraph("Bravo", "blk_short"),
        ],
      },
      { createId: createSequentialBlockIdFactory("invalid") },
    );

    expect(blockIdAt(migrated, [0])).toBe("blk_invalid00000001");
    expect(blockIdAt(migrated, [1])).toBe("blk_invalid00000002");
  });

  test("wrapper and container JSON nodes do not receive blockId unless they are textblocks", () => {
    const migrated = migrateDocumentContentBlockIds(
      {
        type: "doc",
        content: [
          {
            type: "blockquote",
            attrs: { blockId: "blk_wrapper0001", cite: "source" },
            content: [paragraph("Inside quote")],
          },
          {
            type: "bulletList",
            attrs: { blockId: "blk_list000001" },
            content: [
              {
                type: "listItem",
                attrs: { blockId: "blk_item000001" },
                content: [paragraph("Inside list")],
              },
            ],
          },
          {
            type: "tableCell",
            attrs: { blockId: "blk_cell000001", colspan: 1 },
            content: [paragraph("Inside cell")],
          },
        ],
      },
      { createId: createSequentialBlockIdFactory("wrap") },
    );

    expect(hasJsonBlockIdAt(migrated, [0])).toBe(false);
    expect(attrsAt(migrated, [0]).cite).toBe("source");
    expect(blockIdAt(migrated, [0, 0])).toBe("blk_wrap00000001");
    expect(hasJsonBlockIdAt(migrated, [1])).toBe(false);
    expect(hasJsonBlockIdAt(migrated, [1, 0])).toBe(false);
    expect(blockIdAt(migrated, [1, 0, 0])).toBe("blk_wrap00000002");
    expect(hasJsonBlockIdAt(migrated, [2])).toBe(false);
    expect(attrsAt(migrated, [2]).colspan).toBe(1);
    expect(blockIdAt(migrated, [2, 0])).toBe("blk_wrap00000003");
  });

  test("split-block duplicated or missing ids are repaired outside undo history", () => {
    const state = createBlockIdentityState(
      testSchema.node("doc", null, [
        testSchema.node("paragraph", { blockId: "blk_original0001" }, [testSchema.text("alpha beta")]),
      ]),
      createSequentialBlockIdFactory("split"),
    );

    const result = state.applyTransaction(state.tr.split(6));
    const blockIds = collectPmTextblockIds(result.state.doc);

    expect(result.transactions.length).toBe(2);
    expect(result.transactions[1].getMeta("addToHistory")).toBe(false);
    expect(blockIds.length).toBe(2);
    expect(blockIds[0]).toBe("blk_original0001");
    expect(blockIds[1]).toBe("blk_split00000001");
  });

  test("paste-like inserted content with missing and duplicate ids is repaired", () => {
    const existingBlockId = "blk_existing0001";
    const state = createBlockIdentityState(
      testSchema.node("doc", null, [
        testSchema.node("paragraph", { blockId: existingBlockId }, [testSchema.text("Alpha")]),
      ]),
      createSequentialBlockIdFactory("paste"),
    );
    const pastedFragment = Fragment.fromArray([
      testSchema.node("paragraph", { blockId: existingBlockId }, [testSchema.text("Bravo")]),
      testSchema.node("paragraph", null, [testSchema.text("Charlie")]),
    ]);
    const result = state.applyTransaction(state.tr.insert(state.doc.content.size, pastedFragment));

    expect(collectPmTextblockIds(result.state.doc)).toEqual([
      existingBlockId,
      "blk_paste00000001",
      "blk_paste00000002",
    ]);
  });

  test("created comment anchors include start and end blockId when available", () => {
    const doc = testSchema.node("doc", null, [
      testSchema.node("paragraph", { blockId: "blk_start0001" }, [testSchema.text("Alpha")]),
      testSchema.node("paragraph", { blockId: "blk_end00001" }, [testSchema.text("Bravo Charlie")]),
    ]);
    const state = EditorState.create({
      doc,
      schema: testSchema,
      selection: TextSelection.create(doc, 3, 12),
    });
    const anchor = createCommentAnchor({ state }, "doc-test");

    expect(anchor?.block.start.blockId).toBe("blk_start0001");
    expect(anchor?.block.end.blockId).toBe("blk_end00001");
  });

  test("content JSON contains structural blockId and no comment metadata", () => {
    const migrated = migrateDocumentContentBlockIds(
      {
        type: "doc",
        content: [paragraph("Clean body")],
      },
      { createId: createSequentialBlockIdFactory("clean") },
    );
    const serializedContent = JSON.stringify(migrated);

    expect(serializedContent.includes("blockId")).toBe(true);
    expect(serializedContent.includes("\"type\":\"comment\"")).toBe(false);
    expect(serializedContent.includes("\"marks\":[{\"type\":\"comment\"")).toBe(false);
    expect(serializedContent.includes("threadId")).toBe(false);
    expect(serializedContent.includes("runtimeRange")).toBe(false);
    expect(serializedContent.includes("mappedRange")).toBe(false);
    expect(serializedContent.includes("runtimeMatch")).toBe(false);
    expect(serializedContent.includes("anchorStatus")).toBe(false);
    expect(serializedContent.includes("data-comment-thread-id")).toBe(false);
  });

  test("explicit repair transaction adds ids without changing comment plugin state", () => {
    const state = EditorState.create({
      doc: testSchema.node("doc", null, [testSchema.node("paragraph", null, [testSchema.text("Alpha")])]),
      plugins: [createBlockIdentityPlugin({ createId: createSequentialBlockIdFactory("repair") })],
      schema: testSchema,
    });
    const transaction = createBlockIdRepairTransaction(state, createSequentialBlockIdFactory("repair"));

    expect(transaction?.getMeta("addToHistory")).toBe(false);
    expect(transaction?.getMeta("knowledge-comment-decorations")).toBe(undefined);
  });
});

function createBlockIdentityState(doc: ProseMirrorNode, createId: () => string) {
  return EditorState.create({
    doc,
    plugins: [createBlockIdentityPlugin({ createId })],
    schema: testSchema,
  });
}

function createSequentialBlockIdFactory(label: string) {
  let index = 0;

  return () => `blk_${label}${String(++index).padStart(8, "0")}`;
}

function paragraph(text: string, blockId?: string): JSONContent {
  return {
    type: "paragraph",
    attrs: blockId ? { blockId } : undefined,
    content: [{ type: "text", text }],
  };
}

function heading(text: string, blockId?: string): JSONContent {
  return {
    type: "heading",
    attrs: {
      ...(blockId ? { blockId } : {}),
      level: 2,
    },
    content: [{ type: "text", text }],
  };
}

function childAt(node: JSONContent, path: number[]): JSONContent {
  return path.reduce((currentNode, childIndex) => {
    const children = currentNode.content ?? [];
    const childNode = children[childIndex];

    if (!childNode) {
      throw new Error(`Missing JSON node at path ${path.join(".")}`);
    }

    return childNode;
  }, node);
}

function attrsAt(node: JSONContent, path: number[]) {
  return (childAt(node, path).attrs ?? {}) as Record<string, unknown>;
}

function blockIdAt(node: JSONContent, path: number[]) {
  const blockId = attrsAt(node, path)[BLOCK_ID_ATTR];

  if (!isValidBlockId(blockId)) {
    throw new Error(`Missing valid blockId at path ${path.join(".")}`);
  }

  return blockId;
}

function hasJsonBlockIdAt(node: JSONContent, path: number[]) {
  return Object.prototype.hasOwnProperty.call(attrsAt(node, path), BLOCK_ID_ATTR);
}

function collectPmTextblockIds(doc: ProseMirrorNode) {
  const blockIds: string[] = [];

  doc.descendants((node) => {
    if (node.isTextblock) {
      blockIds.push(String(node.attrs.blockId));
    }

    return true;
  });

  return blockIds;
}

function hasBlockIdSchemaAttr(nodeType: NodeType) {
  return Object.prototype.hasOwnProperty.call(nodeType.spec.attrs ?? {}, BLOCK_ID_ATTR);
}
