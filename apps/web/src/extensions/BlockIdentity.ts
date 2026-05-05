import { Extension, type GlobalAttributes } from "@tiptap/core";
import type { JSONContent } from "@tiptap/react";
import type { Node as ProseMirrorNode } from "@tiptap/pm/model";
import { Plugin, PluginKey, type EditorState, type Transaction } from "@tiptap/pm/state";

export const BLOCK_ID_ATTR = "blockId";
export const BLOCK_ID_PREFIX = "blk_";
export const BLOCK_ID_PATTERN = /^blk_[A-Za-z0-9_-]{8,}$/;

// Current schema nodes whose compiled ProseMirror NodeType has isTextblock === true.
export const BLOCK_IDENTITY_TEXTBLOCK_NODE_TYPES = [
  "paragraph",
  "heading",
  "codeBlock",
  "detailsSummary",
] as const;

export type BlockIdGenerator = () => string;

export type BlockIdentityOptions = {
  createId: BlockIdGenerator;
};

export type MigrateDocumentContentBlockIdsOptions = {
  createId?: BlockIdGenerator;
};

type JsonRecord = Record<string, unknown>;

const TEXTBLOCK_NODE_TYPE_SET = new Set<string>(BLOCK_IDENTITY_TEXTBLOCK_NODE_TYPES);

let fallbackBlockIdCounter = 0;

export const blockIdentityPluginKey = new PluginKey("knowledge-block-identity");

export const BlockIdentity = Extension.create<BlockIdentityOptions>({
  name: "blockIdentity",

  addOptions() {
    return {
      createId: generateBlockId,
    };
  },

  addGlobalAttributes(): GlobalAttributes {
    return [
      {
        types: [...BLOCK_IDENTITY_TEXTBLOCK_NODE_TYPES],
        attributes: {
          blockId: {
            default: null,
            parseHTML: (element) => {
              const blockId = element.getAttribute("data-block-id");

              return isValidBlockId(blockId) ? blockId : null;
            },
            renderHTML: (attributes) => {
              const blockId = attributes.blockId;

              return isValidBlockId(blockId) ? { "data-block-id": blockId } : {};
            },
          },
        },
      },
    ];
  },

  addProseMirrorPlugins() {
    return [
      createBlockIdentityPlugin({
        createId: this.options.createId,
      }),
    ];
  },
});

export function createBlockIdentityPlugin({ createId = generateBlockId }: Partial<BlockIdentityOptions> = {}) {
  return new Plugin({
    key: blockIdentityPluginKey,

    view(view) {
      const repairInitialDocument = () => {
        const transaction = createBlockIdRepairTransaction(view.state, createId);

        if (transaction) {
          view.dispatch(transaction);
        }
      };

      if (typeof queueMicrotask === "function") {
        queueMicrotask(repairInitialDocument);
      } else {
        window.setTimeout(repairInitialDocument, 0);
      }

      return {};
    },

    appendTransaction(transactions, _oldState, newState) {
      if (!transactions.some((transaction) => transaction.docChanged)) {
        return null;
      }

      if (transactions.some((transaction) => transaction.getMeta(blockIdentityPluginKey)?.type === "repairBlockIds")) {
        return null;
      }

      return createBlockIdRepairTransaction(newState, createId);
    },
  });
}

export function createBlockIdRepairTransaction(
  state: EditorState,
  createId: BlockIdGenerator = generateBlockId,
): Transaction | null {
  const seenBlockIds = new Set<string>();
  const transaction = state.tr;
  let hasRepairs = false;

  state.doc.descendants((node, pos) => {
    if (!node.isTextblock || !supportsBlockIdAttribute(node)) {
      return true;
    }

    const blockId = node.attrs[BLOCK_ID_ATTR];

    if (isValidBlockId(blockId) && !seenBlockIds.has(blockId)) {
      seenBlockIds.add(blockId);
      return true;
    }

    const repairedBlockId = createUniqueBlockId(seenBlockIds, createId);
    hasRepairs = true;
    transaction.setNodeMarkup(
      pos,
      undefined,
      {
        ...node.attrs,
        [BLOCK_ID_ATTR]: repairedBlockId,
      },
      node.marks,
    );
    seenBlockIds.add(repairedBlockId);

    return true;
  });

  return hasRepairs
    ? transaction
        .setMeta(blockIdentityPluginKey, { type: "repairBlockIds" })
        .setMeta("addToHistory", false)
    : null;
}

export function migrateDocumentContentBlockIds(
  content: JSONContent,
  options: MigrateDocumentContentBlockIdsOptions = {},
): JSONContent {
  const seenBlockIds = new Set<string>();
  const createId = options.createId ?? generateBlockId;

  return migrateJsonNode(content, seenBlockIds, createId);
}

export function generateBlockId(): string {
  const cryptoObject = globalThis.crypto;

  if (typeof cryptoObject?.randomUUID === "function") {
    return `${BLOCK_ID_PREFIX}${cryptoObject.randomUUID().replace(/-/g, "")}`;
  }

  if (typeof cryptoObject?.getRandomValues === "function") {
    const bytes = new Uint8Array(12);

    cryptoObject.getRandomValues(bytes);

    return `${BLOCK_ID_PREFIX}${Array.from(bytes, (byte) => byte.toString(36).padStart(2, "0")).join("")}`;
  }

  return `${BLOCK_ID_PREFIX}${Date.now().toString(36)}${Math.random().toString(36).slice(2, 12)}`;
}

export function isValidBlockId(value: unknown): value is string {
  return typeof value === "string" && BLOCK_ID_PATTERN.test(value);
}

function migrateJsonNode(
  node: JSONContent,
  seenBlockIds: Set<string>,
  createId: BlockIdGenerator,
): JSONContent {
  const migratedNode: JSONContent = { ...node };

  if (Array.isArray(node.content)) {
    migratedNode.content = node.content.map((childNode) => migrateJsonNode(childNode, seenBlockIds, createId));
  }

  if (typeof node.type === "string" && TEXTBLOCK_NODE_TYPE_SET.has(node.type)) {
    const attrs = isRecord(node.attrs) ? { ...node.attrs } : {};
    const blockId = attrs[BLOCK_ID_ATTR];

    if (isValidBlockId(blockId) && !seenBlockIds.has(blockId)) {
      seenBlockIds.add(blockId);
    } else {
      const repairedBlockId = createUniqueBlockId(seenBlockIds, createId);
      attrs[BLOCK_ID_ATTR] = repairedBlockId;
      seenBlockIds.add(repairedBlockId);
    }

    migratedNode.attrs = attrs;
    return migratedNode;
  }

  if (isRecord(node.attrs) && Object.prototype.hasOwnProperty.call(node.attrs, BLOCK_ID_ATTR)) {
    const attrs = { ...node.attrs };

    delete attrs[BLOCK_ID_ATTR];

    if (Object.keys(attrs).length > 0) {
      migratedNode.attrs = attrs;
    } else {
      delete migratedNode.attrs;
    }
  }

  return migratedNode;
}

function createUniqueBlockId(seenBlockIds: Set<string>, createId: BlockIdGenerator) {
  for (let attempt = 0; attempt < 50; attempt += 1) {
    const candidate = createId();

    if (isValidBlockId(candidate) && !seenBlockIds.has(candidate)) {
      return candidate;
    }
  }

  let fallbackBlockId = "";

  do {
    fallbackBlockId = `${BLOCK_ID_PREFIX}local${String(++fallbackBlockIdCounter).padStart(8, "0")}`;
  } while (seenBlockIds.has(fallbackBlockId));

  return fallbackBlockId;
}

function supportsBlockIdAttribute(node: ProseMirrorNode) {
  return Object.prototype.hasOwnProperty.call(node.type.spec.attrs ?? {}, BLOCK_ID_ATTR);
}

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === "object" && value !== null;
}
