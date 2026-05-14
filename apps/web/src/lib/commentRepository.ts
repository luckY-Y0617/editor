import type { CommentAnchorStatus, CommentAnchorV1, CommentMessage, CommentThread } from "../types/editor";
import { getConfiguredApiBaseUrl, getStoredAccessToken, isUuid } from "./apiClient";
import { isCommentBodySubmittable } from "./commentComposerModel";

export type CommentAuthorDTO = {
  id: string;
  name: string;
};

export type CommentMessageDTO = {
  id: string;
  threadId: string;
  body: string;
  author: CommentAuthorDTO;
  createdAt: string;
  updatedAt?: string | null;
  deletedAt?: string | null;
};

export type CommentThreadDTO = {
  id: string;
  documentId: string;
  status: "open" | "resolved";
  anchorStatus: CommentAnchorStatus;
  anchor: CommentAnchorV1;
  messages: CommentMessageDTO[];
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
};

export type CreateCommentThreadDTO = {
  anchor: CommentAnchorV1;
  body: string;
};

export type AddCommentMessageDTO = {
  body: string;
};

export type UpdateCommentThreadDTO = {
  status?: CommentThreadDTO["status"];
};

export interface CommentRepository {
  listThreads(documentId: string): Promise<CommentThread[]>;
  createThread(documentId: string, input: CreateCommentThreadDTO): Promise<CommentThread>;
  addMessage(documentId: string, threadId: string, input: AddCommentMessageDTO): Promise<CommentThread>;
  resolveThread(documentId: string, threadId: string): Promise<CommentThread>;
  reopenThread(documentId: string, threadId: string): Promise<CommentThread>;
}

type FetchLike = typeof fetch;

type InMemoryCommentRepositoryOptions = {
  author?: CommentAuthorDTO;
  createId?: (prefix: string) => string;
  now?: () => string;
};

type HttpCommentRepositoryOptions = {
  apiBaseUrl: string;
  fetchFn?: FetchLike;
  getAccessToken?: () => string | null | undefined;
};

const LOCAL_COMMENT_AUTHOR: CommentAuthorDTO = {
  id: "local-user",
  name: "Local User",
};

export class CommentRepositoryError extends Error {
  readonly code?: string;
  readonly status: number;

  constructor(status: number, message: string, code?: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

export class InMemoryCommentRepository implements CommentRepository {
  private readonly author: CommentAuthorDTO;
  private readonly createId: (prefix: string) => string;
  private readonly now: () => string;
  private readonly threadsByDocumentId = new Map<string, CommentThreadDTO[]>();

  constructor(options: InMemoryCommentRepositoryOptions = {}) {
    this.author = options.author ?? LOCAL_COMMENT_AUTHOR;
    this.createId = options.createId ?? createLocalId;
    this.now = options.now ?? (() => new Date().toISOString());
  }

  async listThreads(documentId: string): Promise<CommentThread[]> {
    return this.getDocumentThreads(documentId).map(commentThreadDtoToModel);
  }

  async createThread(documentId: string, input: CreateCommentThreadDTO): Promise<CommentThread> {
    if (!isCommentBodySubmittable(input.body)) {
      throw new Error("Comment body must not be empty");
    }

    if (input.anchor.documentId !== documentId) {
      throw new Error("Comment anchor documentId must match the target documentId");
    }

    const now = this.now();
    const threadId = this.createId("thread");
    const messageId = this.createId("comment");
    const thread: CommentThreadDTO = {
      id: threadId,
      documentId,
      status: "open",
      anchorStatus: "active",
      anchor: cloneJson(input.anchor),
      messages: [
        {
          id: messageId,
          threadId,
          body: input.body,
          author: { ...this.author },
          createdAt: now,
          updatedAt: null,
          deletedAt: null,
        },
      ],
      createdAt: now,
      updatedAt: now,
      resolvedAt: null,
    };

    this.threadsByDocumentId.set(documentId, [thread, ...this.getDocumentThreads(documentId)]);

    return commentThreadDtoToModel(thread);
  }

  async addMessage(
    documentId: string,
    threadId: string,
    input: AddCommentMessageDTO,
  ): Promise<CommentThread> {
    if (!isCommentBodySubmittable(input.body)) {
      throw new Error("Comment body must not be empty");
    }

    const now = this.now();
    const thread = this.getThreadOrThrow(documentId, threadId);
    const nextThread: CommentThreadDTO = {
      ...thread,
      messages: [
        ...thread.messages,
        {
          id: this.createId("comment"),
          threadId,
          body: input.body,
          author: { ...this.author },
          createdAt: now,
          updatedAt: null,
          deletedAt: null,
        },
      ],
      updatedAt: now,
    };

    this.replaceThread(documentId, nextThread);

    return commentThreadDtoToModel(nextThread);
  }

  async resolveThread(documentId: string, threadId: string): Promise<CommentThread> {
    const now = this.now();
    const thread = this.getThreadOrThrow(documentId, threadId);
    const nextThread: CommentThreadDTO = {
      ...thread,
      status: "resolved",
      resolvedAt: now,
      updatedAt: now,
    };

    this.replaceThread(documentId, nextThread);

    return commentThreadDtoToModel(nextThread);
  }

  async reopenThread(documentId: string, threadId: string): Promise<CommentThread> {
    const now = this.now();
    const thread = this.getThreadOrThrow(documentId, threadId);
    const nextThread: CommentThreadDTO = {
      ...thread,
      status: "open",
      resolvedAt: null,
      updatedAt: now,
    };

    this.replaceThread(documentId, nextThread);

    return commentThreadDtoToModel(nextThread);
  }

  clear() {
    this.threadsByDocumentId.clear();
  }

  private getDocumentThreads(documentId: string) {
    return this.threadsByDocumentId.get(documentId) ?? [];
  }

  private getThreadOrThrow(documentId: string, threadId: string) {
    const thread = this.getDocumentThreads(documentId).find((candidate) => candidate.id === threadId);

    if (!thread) {
      throw new Error(`Comment thread ${threadId} was not found for document ${documentId}`);
    }

    return thread;
  }

  private replaceThread(documentId: string, nextThread: CommentThreadDTO) {
    this.threadsByDocumentId.set(
      documentId,
      this.getDocumentThreads(documentId).map((thread) =>
        thread.id === nextThread.id ? nextThread : thread,
      ),
    );
  }
}

export function createInMemoryCommentRepository(options?: InMemoryCommentRepositoryOptions) {
  return new InMemoryCommentRepository(options);
}

export class HttpCommentRepository implements CommentRepository {
  private readonly apiBaseUrl: string;
  private readonly fetchFn: FetchLike;
  private readonly getAccessToken?: () => string | null | undefined;

  constructor({ apiBaseUrl, fetchFn, getAccessToken }: HttpCommentRepositoryOptions) {
    const normalizedApiBaseUrl = apiBaseUrl.trim().replace(/\/+$/, "");
    if (!normalizedApiBaseUrl) {
      throw new CommentRepositoryError(0, "Comment API base URL is not configured.");
    }

    this.apiBaseUrl = normalizedApiBaseUrl;
    this.fetchFn = fetchFn ?? getBoundGlobalFetch();
    this.getAccessToken = getAccessToken;
  }

  async listThreads(documentId: string): Promise<CommentThread[]> {
    const response = await this.fetchJson<CommentThreadsResponseDTO>(
      `/documents/${encodeURIComponent(documentId)}/comments`,
    );

    return response.threads.map(commentThreadDtoToModel);
  }

  async createThread(documentId: string, input: CreateCommentThreadDTO): Promise<CommentThread> {
    return commentThreadDtoToModel(
      await this.fetchJson<CommentThreadDTO>(
        `/documents/${encodeURIComponent(documentId)}/comments`,
        {
          body: JSON.stringify(input),
          method: "POST",
        },
      ),
    );
  }

  async addMessage(
    documentId: string,
    threadId: string,
    input: AddCommentMessageDTO,
  ): Promise<CommentThread> {
    return commentThreadDtoToModel(
      await this.fetchJson<CommentThreadDTO>(
        `/documents/${encodeURIComponent(documentId)}/comments/${encodeURIComponent(threadId)}/messages`,
        {
          body: JSON.stringify(input),
          method: "POST",
        },
      ),
    );
  }

  async resolveThread(documentId: string, threadId: string): Promise<CommentThread> {
    return commentThreadDtoToModel(
      await this.fetchJson<CommentThreadDTO>(
        `/documents/${encodeURIComponent(documentId)}/comments/${encodeURIComponent(threadId)}/resolve`,
        {
          method: "POST",
        },
      ),
    );
  }

  async reopenThread(documentId: string, threadId: string): Promise<CommentThread> {
    return commentThreadDtoToModel(
      await this.fetchJson<CommentThreadDTO>(
        `/documents/${encodeURIComponent(documentId)}/comments/${encodeURIComponent(threadId)}/reopen`,
        {
          method: "POST",
        },
      ),
    );
  }

  private async fetchJson<TResponse>(path: string, init: RequestInit = {}): Promise<TResponse> {
    const headers = new Headers(init.headers);
    headers.set("Accept", "application/json");

    if (init.body !== undefined) {
      headers.set("Content-Type", "application/json");
    }

    const accessToken = this.getAccessToken?.();

    if (accessToken) {
      headers.set("Authorization", `Bearer ${accessToken}`);
    }

    const url = `${this.apiBaseUrl}${path}`;
    let response: Response;

    try {
      response = await this.fetchFn.call(globalThis, url, {
        ...init,
        credentials: init.credentials ?? "include",
        headers,
      });
    } catch (error) {
      throw toCommentRequestError(error, url);
    }

    if (!response.ok) {
      throw await toCommentApiError(response);
    }

    return (await response.json()) as TResponse;
  }
}

export function createHttpCommentRepository(options: HttpCommentRepositoryOptions) {
  return new HttpCommentRepository(options);
}

export function createCommentRepository(): CommentRepository {
  const apiBaseUrl = getConfiguredApiBaseUrl();

  return apiBaseUrl
    ? new DocumentIdRoutingCommentRepository(
        createHttpCommentRepository({ apiBaseUrl, getAccessToken: getStoredAccessToken }),
        createInMemoryCommentRepository(),
      )
    : createInMemoryCommentRepository();
}

function commentThreadDtoToModel(dto: CommentThreadDTO): CommentThread {
  return {
    id: dto.id,
    documentId: dto.documentId,
    status: dto.status,
    anchorStatus: dto.anchorStatus ?? "active",
    anchor: cloneJson(dto.anchor),
    comments: dto.messages.map(commentMessageDtoToModel),
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
    resolvedAt: dto.resolvedAt ?? null,
  };
}

function commentMessageDtoToModel(dto: CommentMessageDTO): CommentMessage {
  return {
    id: dto.id,
    threadId: dto.threadId,
    body: dto.body,
    author: { ...dto.author },
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt ?? null,
    deletedAt: dto.deletedAt ?? null,
  };
}

function createLocalId(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function cloneJson<T>(value: T): T {
  return JSON.parse(JSON.stringify(value)) as T;
}

function getBoundGlobalFetch(): FetchLike {
  if (typeof globalThis.fetch !== "function") {
    throw new CommentRepositoryError(0, "Fetch is not available in this runtime.");
  }

  return globalThis.fetch.bind(globalThis);
}

async function toCommentApiError(response: Response) {
  const fallback = `Comment API request failed with ${response.status}`;

  try {
    const body = (await response.json()) as {
      code?: string;
      error?: { code?: string; message?: string };
      message?: string;
      title?: string;
    };
    const code = body.error?.code ?? body.code;
    const message = body.error?.message ?? body.message ?? body.title ?? fallback;
    return new CommentRepositoryError(response.status, message, code);
  } catch {
    return new CommentRepositoryError(response.status, fallback);
  }
}

function toCommentRequestError(error: unknown, url: string) {
  if (isAbortError(error) || error instanceof CommentRepositoryError) {
    return error;
  }

  const reason = error instanceof Error && error.message ? ` ${error.message}` : "";
  return new CommentRepositoryError(0, `Could not reach Comment API endpoint ${url}.${reason}`);
}

function isAbortError(error: unknown) {
  return (
    typeof DOMException !== "undefined" &&
    error instanceof DOMException &&
    error.name === "AbortError"
  );
}

type CommentThreadsResponseDTO = {
  threads: CommentThreadDTO[];
};

class DocumentIdRoutingCommentRepository implements CommentRepository {
  constructor(
    private readonly httpRepository: CommentRepository,
    private readonly fallbackRepository: CommentRepository,
  ) {}

  listThreads(documentId: string): Promise<CommentThread[]> {
    return this.getRepository(documentId).listThreads(documentId);
  }

  createThread(documentId: string, input: CreateCommentThreadDTO): Promise<CommentThread> {
    return this.getRepository(documentId).createThread(documentId, input);
  }

  addMessage(
    documentId: string,
    threadId: string,
    input: AddCommentMessageDTO,
  ): Promise<CommentThread> {
    return this.getRepository(documentId).addMessage(documentId, threadId, input);
  }

  resolveThread(documentId: string, threadId: string): Promise<CommentThread> {
    return this.getRepository(documentId).resolveThread(documentId, threadId);
  }

  reopenThread(documentId: string, threadId: string): Promise<CommentThread> {
    return this.getRepository(documentId).reopenThread(documentId, threadId);
  }

  private getRepository(documentId: string) {
    return isUuid(documentId) ? this.httpRepository : this.fallbackRepository;
  }
}
