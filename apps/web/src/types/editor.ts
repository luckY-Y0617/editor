import type { JSONContent } from "@tiptap/react";
import type { AnchorMatchResult } from "../lib/commentAnchorMatching";
import type { AnchorRelocationResult } from "../lib/commentAnchorRelocation";

export type OutlineLevel = 1 | 2 | 3;

export type OutlineItem = {
  id: string;
  level: OutlineLevel;
  text: string;
  // Session-only ProseMirror position for lightweight outline focus; not persisted.
  pos: number;
};

export type EditorSelectionRange = {
  from: number;
  to: number;
};

export type TiptapContentStats = {
  isEmpty: boolean;
  textLength: number;
  outlineItems: OutlineItem[];
};

export type TiptapContentChange = TiptapContentStats & {
  content: JSONContent;
};

export type OutlineFocusRequest = {
  pos: number;
  requestId: number;
};

export type AnchorPoint = {
  blockId?: string;
  path: number[];
  nodeType: string;
  textOffset: number;
};

export type CommentAnchorV1 = {
  schema: "northstar.commentAnchor.v1";
  kind: "tiptap.textRange";
  documentId: string;
  baseRevision: number;
  contentHash?: string;
  pm: EditorSelectionRange;
  block: {
    start: AnchorPoint;
    end: AnchorPoint;
  };
  quote: {
    exact: string;
    prefix: string;
    suffix: string;
    normalizedExact: string;
    normalizer: "northstar.plainText.v1";
  };
  display: {
    excerpt: string;
  };
};

export type CommentMessage = {
  id: string;
  threadId: string;
  body: string;
  author: {
    id: string;
    name: string;
  };
  createdAt: string;
  updatedAt?: string | null;
  deletedAt?: string | null;
};

export type CommentAnchorStatus = "active" | "stale" | "orphaned";

export type CommentThread = {
  id: string;
  documentId: string;
  status: "open" | "resolved";
  anchorStatus: CommentAnchorStatus;
  anchor: CommentAnchorV1;
  comments: CommentMessage[];
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
};

export type CreateCommentThreadRequest = {
  anchor: CommentAnchorV1;
  body: string;
};

export type PendingCommentComposer = {
  documentId: string;
  anchor: CommentAnchorV1;
  excerpt: string;
  body?: string;
  error?: string | null;
  isSubmitting?: boolean;
};

export type CommentRuntimeAnchorState = {
  anchorStatusByThreadId: Record<string, CommentAnchorStatus>;
  matchResultByThreadId: Record<string, AnchorMatchResult>;
  relocationResultByThreadId: Record<string, AnchorRelocationResult>;
};

export type CommentFocusRequest = {
  threadId: string;
  requestId: number;
};

export type KnowledgeFolder = {
  id: string;
  title: string;
};

export type KnowledgeDocument = {
  id: string;
  title: string;
  folderId: string;
  updatedAt: string;
  owner?: {
    id: string;
    name: string;
  };
  revision?: number;
  sortOrder?: number;
  status?: string;
  tags?: string[];
  version?: string;
  content: JSONContent;
};
