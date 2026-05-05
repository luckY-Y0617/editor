import type { CommentAnchorV1, CommentThread } from "../types/editor";

type CreateLocalCommentThreadInput = {
  anchor: CommentAnchorV1;
  body: string;
  commentId?: string;
  now?: string;
  threadId?: string;
};

const LOCAL_COMMENT_AUTHOR = {
  id: "local-user",
  name: "Local User",
};

export function isCommentBodySubmittable(body: string) {
  return body.trim().length > 0;
}

export function createLocalCommentThread({
  anchor,
  body,
  commentId = createLocalId("comment"),
  now = new Date().toISOString(),
  threadId = createLocalId("thread"),
}: CreateLocalCommentThreadInput): CommentThread {
  if (!isCommentBodySubmittable(body)) {
    throw new Error("Comment body must not be empty");
  }

  return {
    id: threadId,
    documentId: anchor.documentId,
    status: "open",
    anchorStatus: "active",
    anchor,
    comments: [
      {
        id: commentId,
        threadId,
        body,
        author: LOCAL_COMMENT_AUTHOR,
        createdAt: now,
        updatedAt: null,
        deletedAt: null,
      },
    ],
    createdAt: now,
    updatedAt: now,
    resolvedAt: null,
  };
}

function createLocalId(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}
