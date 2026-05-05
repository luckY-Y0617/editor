import { describe, expect, test } from "../test/harness";
import { createTestCommentAnchor } from "../test/commentTestUtils";
import type { CommentThread } from "../types/editor";
import { createHttpCommentRepository, createInMemoryCommentRepository } from "./commentRepository";

describe("commentRepository", () => {
  test("listThreads is isolated by documentId", async () => {
    const repository = createDeterministicRepository();
    const anchorA = createTestCommentAnchor({
      documentId: "doc-a",
      exact: "alpha",
      range: { from: 1, to: 6 },
    });
    const anchorB = createTestCommentAnchor({
      documentId: "doc-b",
      exact: "beta",
      range: { from: 1, to: 5 },
    });

    await repository.createThread("doc-a", { anchor: anchorA, body: "A comment" });
    await repository.createThread("doc-b", { anchor: anchorB, body: "B comment" });

    const documentAThreads = await repository.listThreads("doc-a");
    const documentBThreads = await repository.listThreads("doc-b");

    expect(documentAThreads.length).toBe(1);
    expect(documentBThreads.length).toBe(1);
    expect(documentAThreads[0].documentId).toBe("doc-a");
    expect(documentBThreads[0].documentId).toBe("doc-b");
  });

  test("createThread stores submitted body and captured anchor", async () => {
    const repository = createDeterministicRepository();
    const anchor = createTestCommentAnchor({
      documentId: "doc-a",
      endBlockId: "blk_anchorend1",
      exact: "captured anchor",
      range: { from: 1, to: 16 },
      startBlockId: "blk_anchorstart1",
    });

    const thread = await repository.createThread("doc-a", {
      anchor,
      body: "Use this submitted body",
    });

    expect(thread.documentId).toBe("doc-a");
    expect(thread.status).toBe("open");
    expect(thread.anchorStatus).toBe("active");
    expect(thread.anchor).toEqual(anchor);
    expect(thread.anchor.block.start.blockId).toBe("blk_anchorstart1");
    expect(thread.anchor.block.end.blockId).toBe("blk_anchorend1");
    expect(thread.comments.length).toBe(1);
    expect(thread.comments[0].body).toBe("Use this submitted body");
  });

  test("createThread does not use placeholder text", async () => {
    const repository = createDeterministicRepository();
    const thread = await repository.createThread("doc-a", {
      anchor: createTestCommentAnchor({
        documentId: "doc-a",
        exact: "captured anchor",
        range: { from: 1, to: 16 },
      }),
      body: "Specific reviewer note",
    });

    expect(thread.comments[0].body === "Local comment anchored to this selection.").toBe(false);
    expect(thread.comments[0].body).toBe("Specific reviewer note");
  });

  test("createThread does not include document content JSON", async () => {
    const repository = createDeterministicRepository();
    const thread = await repository.createThread("doc-a", {
      anchor: createTestCommentAnchor({
        documentId: "doc-a",
        exact: "alpha",
        range: { from: 1, to: 6 },
      }),
      body: "External comment",
    });

    expect((thread as Record<string, unknown>).content).toBe(undefined);
    expect((thread.anchor as Record<string, unknown>).content).toBe(undefined);
  });

  test("addMessage appends a message without modifying anchor", async () => {
    const repository = createDeterministicRepository();
    const thread = await repository.createThread("doc-a", {
      anchor: createTestCommentAnchor({
        documentId: "doc-a",
        exact: "alpha",
        range: { from: 1, to: 6 },
      }),
      body: "Initial comment",
    });
    const originalAnchor = JSON.stringify(thread.anchor);

    const updatedThread = await repository.addMessage("doc-a", thread.id, {
      body: "Follow-up comment",
    });

    expect(updatedThread.comments.length).toBe(2);
    expect(updatedThread.comments[0].body).toBe("Initial comment");
    expect(updatedThread.comments[1].body).toBe("Follow-up comment");
    expect(JSON.stringify(updatedThread.anchor)).toBe(originalAnchor);
  });

  test("addMessage failure keeps draft responsibility outside repository and does not mutate thread", async () => {
    const repository = createDeterministicRepository();
    const thread = await repository.createThread("doc-a", {
      anchor: createTestCommentAnchor({
        documentId: "doc-a",
        exact: "alpha",
        range: { from: 1, to: 6 },
      }),
      body: "Initial comment",
    });
    const originalAnchor = JSON.stringify(thread.anchor);
    let errorMessage = "";

    try {
      await repository.addMessage("doc-a", thread.id, {
        body: "   ",
      });
    } catch (error) {
      errorMessage = error instanceof Error ? error.message : String(error);
    }

    const threads = await repository.listThreads("doc-a");

    expect(errorMessage).toBe("Comment body must not be empty");
    expect(threads[0].comments.length).toBe(1);
    expect(JSON.stringify(threads[0].anchor)).toBe(originalAnchor);
  });

  test("resolveThread only changes lifecycle status, resolvedAt, and updatedAt", async () => {
    const repository = createDeterministicRepository([
      "2026-04-29T00:00:00.000Z",
      "2026-04-29T00:01:00.000Z",
    ]);
    const thread = await repository.createThread("doc-a", {
      anchor: createTestCommentAnchor({
        documentId: "doc-a",
        exact: "alpha",
        range: { from: 1, to: 6 },
      }),
      body: "Initial comment",
    });

    const resolvedThread = await repository.resolveThread("doc-a", thread.id);

    expect(resolvedThread.status).toBe("resolved");
    expect(resolvedThread.resolvedAt).toBe("2026-04-29T00:01:00.000Z");
    expect(resolvedThread.updatedAt).toBe("2026-04-29T00:01:00.000Z");
    expect(omitLifecycleFields(resolvedThread)).toEqual(omitLifecycleFields(thread));
  });

  test("reopenThread only changes lifecycle status, resolvedAt, and updatedAt", async () => {
    const repository = createDeterministicRepository([
      "2026-04-29T00:00:00.000Z",
      "2026-04-29T00:01:00.000Z",
      "2026-04-29T00:02:00.000Z",
    ]);
    const thread = await repository.createThread("doc-a", {
      anchor: createTestCommentAnchor({
        documentId: "doc-a",
        exact: "alpha",
        range: { from: 1, to: 6 },
      }),
      body: "Initial comment",
    });
    const resolvedThread = await repository.resolveThread("doc-a", thread.id);

    const reopenedThread = await repository.reopenThread("doc-a", thread.id);

    expect(reopenedThread.status).toBe("open");
    expect(reopenedThread.resolvedAt).toBe(null);
    expect(reopenedThread.updatedAt).toBe("2026-04-29T00:02:00.000Z");
    expect(omitLifecycleFields(reopenedThread)).toEqual(omitLifecycleFields(resolvedThread));
  });

  test("repository output excludes runtime ranges, runtimeMatch, and UI state", async () => {
    const repository = createDeterministicRepository();
    const thread = await repository.createThread("doc-a", {
      anchor: createTestCommentAnchor({
        documentId: "doc-a",
        exact: "alpha",
        range: { from: 1, to: 6 },
      }),
      body: "External comment",
    });
    const serializedThread = JSON.stringify(thread);

    expect(serializedThread.includes("rangesByThreadId")).toBe(false);
    expect(serializedThread.includes("runtimeRange")).toBe(false);
    expect(serializedThread.includes("mappedRange")).toBe(false);
    expect(serializedThread.includes("runtimeMatch")).toBe(false);
    expect(serializedThread.includes("DecorationSet")).toBe(false);
    expect(serializedThread.includes("activeThreadId")).toBe(false);
    expect(serializedThread.includes("pendingCommentComposer")).toBe(false);
  });

  test("HTTP adapter maps backend DTOs to frontend comment threads", async () => {
    const anchor = createTestCommentAnchor({
      documentId: "11111111-1111-4111-8111-111111111111",
      exact: "alpha",
      range: { from: 1, to: 6 },
    });
    const repository = createHttpCommentRepository({
      apiBaseUrl: "https://northstar.test/api/v1",
      fetchFn: async () =>
        jsonResponse({
          threads: [
            {
              id: "22222222-2222-4222-8222-222222222222",
              documentId: anchor.documentId,
              status: "open",
              anchorStatus: "active",
              anchor,
              messages: [
                {
                  id: "33333333-3333-4333-8333-333333333333",
                  threadId: "22222222-2222-4222-8222-222222222222",
                  body: "Backend body",
                  author: {
                    id: "44444444-4444-4444-8444-444444444444",
                    name: "Backend User",
                  },
                  createdAt: "2026-04-29T00:00:00.000Z",
                  updatedAt: null,
                  deletedAt: null,
                },
              ],
              createdAt: "2026-04-29T00:00:00.000Z",
              updatedAt: "2026-04-29T00:00:00.000Z",
              resolvedAt: null,
            },
          ],
        }),
    });

    const threads = await repository.listThreads(anchor.documentId);

    expect(threads.length).toBe(1);
    expect(threads[0].anchor).toEqual(anchor);
    expect(threads[0].comments[0].body).toBe("Backend body");
  });

  test("HTTP adapter sends only comment DTO fields", async () => {
    const calls: Array<{ input: string; init?: RequestInit }> = [];
    const anchor = createTestCommentAnchor({
      documentId: "11111111-1111-4111-8111-111111111111",
      exact: "alpha",
      range: { from: 1, to: 6 },
    });
    const repository = createHttpCommentRepository({
      apiBaseUrl: "https://northstar.test/api/v1/",
      fetchFn: async (input, init) => {
        calls.push({ input: String(input), init });
        return jsonResponse({
          id: "22222222-2222-4222-8222-222222222222",
          documentId: anchor.documentId,
          status: "open",
          anchorStatus: "active",
          anchor,
          messages: [],
          createdAt: "2026-04-29T00:00:00.000Z",
          updatedAt: "2026-04-29T00:00:00.000Z",
          resolvedAt: null,
        });
      },
    });

    await repository.createThread(anchor.documentId, {
      anchor,
      body: "HTTP body",
    });

    const call = calls[0];
    const requestBody = String(call.init?.body);

    expect(call.input).toBe(`https://northstar.test/api/v1/documents/${anchor.documentId}/comments`);
    expect(call.init?.method).toBe("POST");
    expect(requestBody.includes("HTTP body")).toBe(true);
    expect(requestBody.includes("content")).toBe(false);
    expect(requestBody.includes("runtimeMatch")).toBe(false);
    expect(requestBody.includes("activeThreadId")).toBe(false);
    expect(requestBody.includes("pendingCommentComposer")).toBe(false);
  });

  test("HTTP adapter surfaces recoverable request failures", async () => {
    const repository = createHttpCommentRepository({
      apiBaseUrl: "https://northstar.test/api/v1",
      fetchFn: async () =>
        new Response(JSON.stringify({ message: "Service unavailable" }), {
          headers: {
            "Content-Type": "application/json",
          },
          status: 503,
        }),
    });
    let errorMessage = "";

    try {
      await repository.listThreads("11111111-1111-4111-8111-111111111111");
    } catch (error) {
      errorMessage = error instanceof Error ? error.message : String(error);
    }

    expect(errorMessage).toBe("Comment API request failed with 503");
  });

  test("HTTP adapter uses comment-specific endpoints for mutations", async () => {
    const calls: Array<{ input: string; init?: RequestInit }> = [];
    const anchor = createTestCommentAnchor({
      documentId: "11111111-1111-4111-8111-111111111111",
      exact: "alpha",
      range: { from: 1, to: 6 },
    });
    const repository = createHttpCommentRepository({
      apiBaseUrl: "https://northstar.test/api/v1",
      fetchFn: async (input, init) => {
        calls.push({ input: String(input), init });
        return jsonResponse({
          id: "22222222-2222-4222-8222-222222222222",
          documentId: anchor.documentId,
          status: "open",
          anchorStatus: "active",
          anchor,
          messages: [],
          createdAt: "2026-04-29T00:00:00.000Z",
          updatedAt: "2026-04-29T00:00:00.000Z",
          resolvedAt: null,
        });
      },
    });

    await repository.createThread(anchor.documentId, { anchor, body: "Create body" });
    await repository.addMessage(anchor.documentId, "thread-a", { body: "Follow up" });
    await repository.resolveThread(anchor.documentId, "thread-a");
    await repository.reopenThread(anchor.documentId, "thread-a");

    expect(calls.length).toBe(4);
    expect(calls.every((call) => call.input.includes("/comments"))).toBe(true);
    expect(calls.some((call) => call.init?.method === "PATCH")).toBe(false);
  });
});

function createDeterministicRepository(times = ["2026-04-29T00:00:00.000Z"]) {
  let idIndex = 0;
  let timeIndex = 0;

  return createInMemoryCommentRepository({
    createId: (prefix) => `${prefix}-${++idIndex}`,
    now: () => times[Math.min(timeIndex++, times.length - 1)],
  });
}

function omitLifecycleFields(thread: CommentThread) {
  const { resolvedAt, status, updatedAt, ...stableFields } = thread;

  return stableFields;
}

function jsonResponse(body: unknown) {
  return new Response(JSON.stringify(body), {
    headers: {
      "Content-Type": "application/json",
    },
    status: 200,
  });
}
