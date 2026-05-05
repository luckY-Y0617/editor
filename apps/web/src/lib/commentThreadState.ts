import type { CommentAnchorStatus, CommentThread } from "../types/editor";

export type CommentThreadsByDocumentId = Record<string, CommentThread[]>;
export type CommentAnchorStatusesByDocumentId = Record<string, Record<string, CommentAnchorStatus>>;

const EMPTY_COMMENT_THREADS: CommentThread[] = [];

export function setThreadsForDocument(
  threadsByDocumentId: CommentThreadsByDocumentId,
  documentId: string,
  threads: CommentThread[],
): CommentThreadsByDocumentId {
  return {
    ...threadsByDocumentId,
    [documentId]: threads,
  };
}

export function prependThreadForDocument(
  threadsByDocumentId: CommentThreadsByDocumentId,
  documentId: string,
  thread: CommentThread,
): CommentThreadsByDocumentId {
  const currentThreads = threadsByDocumentId[documentId] ?? EMPTY_COMMENT_THREADS;

  return setThreadsForDocument(
    threadsByDocumentId,
    documentId,
    [thread, ...currentThreads.filter((currentThread) => currentThread.id !== thread.id)],
  );
}

export function replaceThreadForDocument(
  threadsByDocumentId: CommentThreadsByDocumentId,
  documentId: string,
  thread: CommentThread,
): CommentThreadsByDocumentId {
  const currentThreads = threadsByDocumentId[documentId] ?? EMPTY_COMMENT_THREADS;

  return setThreadsForDocument(
    threadsByDocumentId,
    documentId,
    currentThreads.map((currentThread) => (currentThread.id === thread.id ? thread : currentThread)),
  );
}

export function selectCommentThreadsForDocument(
  threadsByDocumentId: CommentThreadsByDocumentId,
  anchorStatusesByDocumentId: CommentAnchorStatusesByDocumentId,
  documentId: string,
) {
  return applyRuntimeAnchorStatuses(
    threadsByDocumentId[documentId] ?? EMPTY_COMMENT_THREADS,
    anchorStatusesByDocumentId[documentId] ?? {},
  );
}

export function applyRuntimeAnchorStatuses(
  threads: CommentThread[],
  anchorStatusByThreadId: Record<string, CommentAnchorStatus>,
) {
  let hasRuntimeStatus = false;

  const nextThreads = threads.map((thread) => {
    const runtimeAnchorStatus = anchorStatusByThreadId[thread.id];

    if (!runtimeAnchorStatus || runtimeAnchorStatus === thread.anchorStatus) {
      return thread;
    }

    hasRuntimeStatus = true;

    return {
      ...thread,
      anchorStatus: runtimeAnchorStatus,
    };
  });

  return hasRuntimeStatus ? nextThreads : threads;
}
