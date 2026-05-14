import { describe, expect, test } from "../test/harness";
import {
  ApiClientError,
  apiFetch,
  formatApiOperationError,
  getStoredAccessToken,
  getStoredRefreshToken,
  setStoredAuthTokens,
} from "./apiClient";

describe("apiClient", () => {
  test("formats API operation errors without leaking endpoint URLs", () => {
    expect(formatApiOperationError(new ApiClientError(0, "API base URL is not configured."), "Fallback")).toBe(
      "API is not configured for this environment.",
    );
    expect(formatApiOperationError(new ApiClientError(0, "Could not reach API endpoint https://northstar.test/api/v1/documents. Failed to fetch"), "Fallback")).toBe(
      "Could not reach the configured API. Check the backend session and retry.",
    );
    expect(formatApiOperationError(new ApiClientError(401, "Access expired"), "Fallback")).toBe(
      "Sign in again and retry this action.",
    );
    expect(formatApiOperationError(new ApiClientError(403, "Forbidden"), "Fallback")).toBe(
      "You do not have permission to perform this action.",
    );
    expect(formatApiOperationError(new ApiClientError(409, "Revision conflict."), "Fallback")).toBe(
      "Revision conflict.",
    );
  });

  test("refreshes the access token and retries a 401 request once", async () => {
    await withMockWindow(async () => {
      const calls: Array<{ authorization: string | null; method: string; url: string }> = [];
      let documentCalls = 0;
      setStoredAuthTokens("old-access", "refresh-token");

      const fetchFn: typeof fetch = async (input, init) => {
        const url = String(input);
        const authorization = new Headers(init?.headers).get("Authorization");
        calls.push({ authorization, method: init?.method ?? "GET", url });

        if (url.endsWith("/documents/doc-1")) {
          documentCalls += 1;
          return documentCalls === 1 ? unauthorizedResponse() : jsonResponse({ ok: true });
        }

        if (url.endsWith("/auth/refresh")) {
          return jsonResponse({
            accessToken: "new-access",
            refreshToken: "new-refresh",
          });
        }

        return jsonResponse({ ok: true });
      };

      const result = await apiFetch<{ ok: boolean }>("/documents/doc-1", {
        apiBaseUrl: "https://northstar.test/api/v1",
        fetchFn,
      });

      expect(result.ok).toBe(true);
      expect(getStoredAccessToken()).toBe("new-access");
      expect(getStoredRefreshToken()).toBe("new-refresh");
      expect(calls).toEqual([
        {
          authorization: "Bearer old-access",
          method: "GET",
          url: "https://northstar.test/api/v1/documents/doc-1",
        },
        {
          authorization: null,
          method: "POST",
          url: "https://northstar.test/api/v1/auth/refresh",
        },
        {
          authorization: "Bearer new-access",
          method: "GET",
          url: "https://northstar.test/api/v1/documents/doc-1",
        },
      ]);
    });
  });

  test("shares one refresh request across concurrent 401 responses", async () => {
    await withMockWindow(async () => {
      let releaseRefresh: () => void = () => undefined;
      const refreshGate = new Promise<void>((resolve) => {
        releaseRefresh = resolve;
      });
      let refreshCalls = 0;
      setStoredAuthTokens("old-access", "refresh-token");

      const fetchFn: typeof fetch = async (input, init) => {
        const url = String(input);
        const authorization = new Headers(init?.headers).get("Authorization");

        if (url.endsWith("/auth/refresh")) {
          refreshCalls += 1;
          await refreshGate;
          return jsonResponse({
            accessToken: "shared-access",
            refreshToken: "shared-refresh",
          });
        }

        return authorization === "Bearer old-access" ? unauthorizedResponse() : jsonResponse({ ok: true });
      };

      const firstRequest = apiFetch<{ ok: boolean }>("/documents/doc-1", {
        apiBaseUrl: "https://northstar.test/api/v1",
        fetchFn,
      });
      const secondRequest = apiFetch<{ ok: boolean }>("/documents/doc-2", {
        apiBaseUrl: "https://northstar.test/api/v1",
        fetchFn,
      });

      await waitFor(() => refreshCalls === 1);
      expect(refreshCalls).toBe(1);
      releaseRefresh();

      const results = await Promise.all([firstRequest, secondRequest]);

      expect(results[0].ok).toBe(true);
      expect(results[1].ok).toBe(true);
      expect(refreshCalls).toBe(1);
      expect(getStoredAccessToken()).toBe("shared-access");
      expect(getStoredRefreshToken()).toBe("shared-refresh");
    });
  });

  test("clears stored tokens when refresh fails", async () => {
    await withMockWindow(async () => {
      let errorStatus = 0;
      setStoredAuthTokens("old-access", "refresh-token");

      const fetchFn: typeof fetch = async (input) => {
        const url = String(input);
        return url.endsWith("/auth/refresh")
          ? unauthorizedResponse("Refresh expired")
          : unauthorizedResponse("Access expired");
      };

      try {
        await apiFetch("/documents/doc-1", {
          apiBaseUrl: "https://northstar.test/api/v1",
          fetchFn,
        });
      } catch (error) {
        errorStatus = error instanceof Error && "status" in error ? Number(error.status) : 0;
      }

      expect(errorStatus).toBe(401);
      expect(getStoredAccessToken()).toBe(null);
      expect(getStoredRefreshToken()).toBe(null);
    });
  });

  test("surfaces network failures as ApiClientError before a response exists", async () => {
    await withMockWindow(async () => {
      let errorMessage = "";
      let errorStatus = -1;

      const fetchFn: typeof fetch = async () => {
        throw new TypeError("Failed to fetch");
      };

      try {
        await apiFetch("/auth/login", {
          apiBaseUrl: "https://northstar.test/api/v1",
          auth: false,
          body: { email: "owner@northstar.local", password: "secret" },
          fetchFn,
          method: "POST",
        });
      } catch (error) {
        if (error instanceof Error) {
          errorMessage = error.message;
        }

        errorStatus = error instanceof Error && "status" in error ? Number(error.status) : -1;
      }

      expect(errorStatus).toBe(0);
      expect(errorMessage).toBe(
        "Could not reach API endpoint https://northstar.test/api/v1/auth/login. Failed to fetch",
      );
    });
  });

  test("binds injected browser fetch context", async () => {
    await withMockWindow(async () => {
      const fetchFn = function fetchWithRequiredContext(this: unknown) {
        if (this !== globalThis) {
          throw new TypeError("Illegal invocation");
        }

        return Promise.resolve(jsonResponse({ ok: true }));
      } as typeof fetch;

      const result = await apiFetch<{ ok: boolean }>("/documents/doc-1", {
        apiBaseUrl: "https://northstar.test/api/v1",
        fetchFn,
      });

      expect(result.ok).toBe(true);
    });
  });

  test("sends Blob bodies without JSON serialization", async () => {
    await withMockWindow(async () => {
      const blob = new Blob(["image-bytes"], { type: "image/png" });
      let sentBody: BodyInit | null | undefined;
      let sentContentType = "";

      const fetchFn: typeof fetch = async (_input, init) => {
        sentBody = init?.body;
        sentContentType = new Headers(init?.headers).get("Content-Type") ?? "";
        return jsonResponse({ ok: true });
      };

      await apiFetch<{ ok: boolean }>("/files/uploads/sessions/session-1/content", {
        apiBaseUrl: "https://northstar.test/api/v1",
        body: blob,
        fetchFn,
        headers: { "Content-Type": blob.type },
        method: "PUT",
      });

      expect(sentBody as unknown).toBe(blob);
      expect(sentContentType).toBe("image/png");
    });
  });
});

async function withMockWindow(run: () => Promise<void>) {
  const previousWindow = Object.getOwnPropertyDescriptor(globalThis, "window");
  Object.defineProperty(globalThis, "window", {
    configurable: true,
    value: {
      addEventListener() {
        return undefined;
      },
      dispatchEvent() {
        return true;
      },
      localStorage: createStorage(),
      location: {
        search: "",
      },
      removeEventListener() {
        return undefined;
      },
    } as unknown as Window,
  });

  try {
    await run();
  } finally {
    if (previousWindow) {
      Object.defineProperty(globalThis, "window", previousWindow);
    } else {
      Reflect.deleteProperty(globalThis, "window");
    }
  }
}

function createStorage() {
  const values = new Map<string, string>();
  return {
    clear() {
      values.clear();
    },
    get length() {
      return values.size;
    },
    getItem(key: string) {
      return values.get(key) ?? null;
    },
    key(index: number) {
      return Array.from(values.keys())[index] ?? null;
    },
    removeItem(key: string) {
      values.delete(key);
    },
    setItem(key: string, value: string) {
      values.set(key, value);
    },
  } as Storage;
}

function jsonResponse(body: unknown) {
  return new Response(JSON.stringify(body), {
    headers: {
      "Content-Type": "application/json",
    },
    status: 200,
  });
}

function unauthorizedResponse(message = "Unauthorized") {
  return new Response(
    JSON.stringify({
      error: {
        code: "UNAUTHORIZED",
        message,
      },
    }),
    {
      headers: {
        "Content-Type": "application/json",
      },
      status: 401,
    },
  );
}

async function waitFor(predicate: () => boolean) {
  for (let attempt = 0; attempt < 20; attempt += 1) {
    if (predicate()) {
      return;
    }

    await Promise.resolve();
  }

  throw new Error("Timed out waiting for condition");
}
