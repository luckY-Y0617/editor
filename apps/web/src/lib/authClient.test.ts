import { describe, expect, test } from "../test/harness";
import { getStoredAccessToken, getStoredRefreshToken } from "./apiClient";
import { register } from "./authClient";

describe("authClient", () => {
  test("register posts to the auth register endpoint and stores issued tokens", async () => {
    await withMockWindow(async () => {
      const calls: Array<{ body: unknown; method: string; url: string }> = [];

      const fetchFn: typeof fetch = async (input, init) => {
        calls.push({
          body: init?.body ? JSON.parse(String(init.body)) : null,
          method: init?.method ?? "GET",
          url: String(input),
        });

        return jsonResponse({
          accessToken: "issued-access",
          accessTokenExpiresAt: "2026-05-06T00:15:00Z",
          refreshToken: "issued-refresh",
          refreshTokenExpiresAt: "2026-05-20T00:00:00Z",
          user: {
            displayName: "New User",
            email: "new@example.test",
            id: "user-1",
          },
        });
      };

      const response = await register(
        {
          displayName: "New User",
          email: "new@example.test",
          password: "valid-password",
        },
        {
          apiBaseUrl: "https://northstar.test/api/v1",
          fetchFn,
        },
      );

      expect(response.user.email).toBe("new@example.test");
      expect(getStoredAccessToken()).toBe("issued-access");
      expect(getStoredRefreshToken()).toBe("issued-refresh");
      expect(calls).toEqual([
        {
          body: {
            displayName: "New User",
            email: "new@example.test",
            password: "valid-password",
          },
          method: "POST",
          url: "https://northstar.test/api/v1/auth/register",
        },
      ]);
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
