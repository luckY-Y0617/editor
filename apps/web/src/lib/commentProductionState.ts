import type { PendingCommentComposer } from "../types/editor";
import { isCommentBodySubmittable } from "./commentComposerModel";

export type CommentLoadState =
  | {
      status: "idle";
    }
  | {
      requestId: number;
      status: "loading";
    }
  | {
      requestId: number;
      status: "loaded";
    }
  | {
      error: string;
      requestId: number;
      status: "error";
    };

export type CommentLoadStatesByDocumentId = Record<string, CommentLoadState>;

export type ThreadLifecycleActionState = {
  errorsByThreadId: Record<string, string>;
  pendingByThreadId: Record<string, true>;
};

export const IDLE_COMMENT_LOAD_STATE: CommentLoadState = {
  status: "idle",
};

export function getCommentLoadState(
  statesByDocumentId: CommentLoadStatesByDocumentId,
  documentId: string,
): CommentLoadState {
  return statesByDocumentId[documentId] ?? IDLE_COMMENT_LOAD_STATE;
}

export function beginCommentLoad(
  statesByDocumentId: CommentLoadStatesByDocumentId,
  documentId: string,
  requestId: number,
): CommentLoadStatesByDocumentId {
  return {
    ...statesByDocumentId,
    [documentId]: {
      requestId,
      status: "loading",
    },
  };
}

export function shouldAcceptCommentLoadResult({
  activeDocumentId,
  documentId,
  requestId,
  statesByDocumentId,
}: {
  activeDocumentId: string;
  documentId: string;
  requestId: number;
  statesByDocumentId: CommentLoadStatesByDocumentId;
}) {
  const currentState = statesByDocumentId[documentId];

  return (
    activeDocumentId === documentId &&
    currentState?.status === "loading" &&
    currentState.requestId === requestId
  );
}

export function finishCommentLoadSuccess(
  statesByDocumentId: CommentLoadStatesByDocumentId,
  documentId: string,
  requestId: number,
  activeDocumentId: string,
): CommentLoadStatesByDocumentId {
  if (
    !shouldAcceptCommentLoadResult({
      activeDocumentId,
      documentId,
      requestId,
      statesByDocumentId,
    })
  ) {
    return statesByDocumentId;
  }

  return {
    ...statesByDocumentId,
    [documentId]: {
      requestId,
      status: "loaded",
    },
  };
}

export function finishCommentLoadFailure(
  statesByDocumentId: CommentLoadStatesByDocumentId,
  documentId: string,
  requestId: number,
  activeDocumentId: string,
  error: unknown,
): CommentLoadStatesByDocumentId {
  if (
    !shouldAcceptCommentLoadResult({
      activeDocumentId,
      documentId,
      requestId,
      statesByDocumentId,
    })
  ) {
    return statesByDocumentId;
  }

  return {
    ...statesByDocumentId,
    [documentId]: {
      error: formatCommentOperationError(error),
      requestId,
      status: "error",
    },
  };
}

export function createThreadLifecycleActionState(): ThreadLifecycleActionState {
  return {
    errorsByThreadId: {},
    pendingByThreadId: {},
  };
}

export function beginThreadLifecycleAction(
  state: ThreadLifecycleActionState,
  threadId: string,
): { accepted: boolean; state: ThreadLifecycleActionState } {
  if (state.pendingByThreadId[threadId]) {
    return {
      accepted: false,
      state,
    };
  }

  const { [threadId]: _clearedError, ...nextErrorsByThreadId } = state.errorsByThreadId;

  return {
    accepted: true,
    state: {
      errorsByThreadId: nextErrorsByThreadId,
      pendingByThreadId: {
        ...state.pendingByThreadId,
        [threadId]: true,
      },
    },
  };
}

export function finishThreadLifecycleActionSuccess(
  state: ThreadLifecycleActionState,
  threadId: string,
): ThreadLifecycleActionState {
  const { [threadId]: _pending, ...nextPendingByThreadId } = state.pendingByThreadId;
  const { [threadId]: _error, ...nextErrorsByThreadId } = state.errorsByThreadId;

  return {
    errorsByThreadId: nextErrorsByThreadId,
    pendingByThreadId: nextPendingByThreadId,
  };
}

export function finishThreadLifecycleActionFailure(
  state: ThreadLifecycleActionState,
  threadId: string,
  error: unknown,
): ThreadLifecycleActionState {
  const { [threadId]: _pending, ...nextPendingByThreadId } = state.pendingByThreadId;

  return {
    errorsByThreadId: {
      ...state.errorsByThreadId,
      [threadId]: formatCommentOperationError(error),
    },
    pendingByThreadId: nextPendingByThreadId,
  };
}

export function markComposerSubmitting(
  composer: PendingCommentComposer,
  body: string,
): { accepted: boolean; composer: PendingCommentComposer } {
  if (composer.isSubmitting || !isCommentBodySubmittable(body)) {
    return {
      accepted: false,
      composer,
    };
  }

  return {
    accepted: true,
    composer: {
      ...composer,
      body,
      error: null,
      isSubmitting: true,
    },
  };
}

export function markComposerSubmitFailed(
  composer: PendingCommentComposer,
  body: string,
  error: unknown,
): PendingCommentComposer {
  return {
    ...composer,
    body,
    error: formatCommentOperationError(error),
    isSubmitting: false,
  };
}

export function formatCommentOperationError(error: unknown) {
  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }

  if (typeof error === "string" && error.trim()) {
    return error;
  }

  return "Comment request failed. Try again.";
}
