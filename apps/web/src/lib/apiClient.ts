export type ApiFetchOptions = Omit<RequestInit, "body"> & {
  apiBaseUrl?: string;
  auth?: boolean;
  body?: unknown;
  fetchFn?: typeof fetch;
  skipAuthRefresh?: boolean;
};

export class ApiClientError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(status: number, message: string, code?: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

const apiVersionPrefix = "/api/v1";
const authStateEventName = "northstar:auth-state-changed";
const accessTokenKeys = ["northstar.accessToken", "northstar:accessToken", "accessToken"];
const refreshTokenKeys = ["northstar.refreshToken", "northstar:refreshToken", "refreshToken"];
let refreshPromise: Promise<boolean> | null = null;

export function getConfiguredApiBaseUrl() {
  const env = (import.meta as ImportMeta & { env?: Record<string, string | undefined> }).env;
  const value = env?.VITE_NORTHSTAR_API_BASE_URL ?? env?.VITE_API_BASE_URL ?? "";
  const trimmed = value.trim().replace(/\/+$/, "");

  if (!trimmed) {
    return "";
  }

  return trimmed.endsWith(apiVersionPrefix) ? trimmed : `${trimmed}${apiVersionPrefix}`;
}

export function buildApiUrl(path: string, apiBaseUrl = getConfiguredApiBaseUrl()) {
  if (!apiBaseUrl) {
    throw new ApiClientError(0, "API base URL is not configured.");
  }

  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  const versionlessPath = normalizedPath.startsWith(apiVersionPrefix)
    ? normalizedPath.slice(apiVersionPrefix.length)
    : normalizedPath;

  return `${apiBaseUrl}${versionlessPath}`;
}

export function getStoredAccessToken() {
  return getFirstStoredValue(accessTokenKeys);
}

export function getStoredRefreshToken() {
  return getFirstStoredValue(refreshTokenKeys);
}

export function setStoredAuthTokens(accessToken: string, refreshToken?: string | null) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(accessTokenKeys[0], accessToken);
  removeStorageKeys(accessTokenKeys.slice(1));

  if (refreshToken) {
    window.localStorage.setItem(refreshTokenKeys[0], refreshToken);
    removeStorageKeys(refreshTokenKeys.slice(1));
  }

  notifyAuthStateChanged();
}

export function clearStoredAuthTokens() {
  removeStorageKeys([...accessTokenKeys, ...refreshTokenKeys]);
  notifyAuthStateChanged();
}

export function subscribeToAuthChanges(listener: () => void) {
  if (typeof window === "undefined") {
    return () => undefined;
  }

  const handleChange = () => listener();
  window.addEventListener(authStateEventName, handleChange);
  window.addEventListener("storage", handleChange);

  return () => {
    window.removeEventListener(authStateEventName, handleChange);
    window.removeEventListener("storage", handleChange);
  };
}

export function createApiHeaders(contentType?: string, auth = true, headers?: HeadersInit) {
  const requestHeaders = new Headers(headers);

  if (!requestHeaders.has("Accept")) {
    requestHeaders.set("Accept", "application/json");
  }

  if (contentType && !requestHeaders.has("Content-Type")) {
    requestHeaders.set("Content-Type", contentType);
  }

  const token = auth ? getStoredAccessToken() : null;
  if (token && !requestHeaders.has("Authorization")) {
    requestHeaders.set("Authorization", `Bearer ${token}`);
  }

  return requestHeaders;
}

export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}) {
  const {
    apiBaseUrl,
    auth = true,
    body,
    fetchFn,
    headers,
    skipAuthRefresh = false,
    ...requestOptions
  } = options;
  const request = {
    apiBaseUrl,
    auth,
    body,
    fetchFn,
    headers,
    requestOptions,
  };
  const response = await sendApiRequest(path, request);

  if (response.status === 401 && auth && !skipAuthRefresh) {
    const refreshed = await refreshStoredAuth({ apiBaseUrl, fetchFn });
    if (refreshed) {
      return parseApiResponse<T>(await sendApiRequest(path, request));
    }
  }

  return parseApiResponse<T>(response);
}

export async function refreshStoredAuth(
  options: { apiBaseUrl?: string; fetchFn?: typeof fetch } = {},
) {
  if (!refreshPromise) {
    refreshPromise = refreshStoredAuthCore(options).finally(() => {
      refreshPromise = null;
    });
  }

  return refreshPromise;
}

async function sendApiRequest(
  path: string,
  options: {
    apiBaseUrl?: string;
    auth: boolean;
    body: unknown;
    fetchFn?: typeof fetch;
    headers?: HeadersInit;
    requestOptions: Omit<RequestInit, "body" | "headers">;
  },
) {
  const isFormDataBody = typeof FormData !== "undefined" && options.body instanceof FormData;
  const hasJsonBody = options.body !== undefined && !isFormDataBody && typeof options.body !== "string";
  const requestHeaders = createApiHeaders(hasJsonBody ? "application/json" : undefined, options.auth, options.headers);
  const url = buildApiUrl(path, options.apiBaseUrl);

  try {
    return await callFetch(options.fetchFn, url, {
      ...options.requestOptions,
      body: hasJsonBody ? JSON.stringify(options.body) : (options.body as BodyInit | null | undefined),
      headers: requestHeaders,
    });
  } catch (error) {
    throw toRequestError(error, url);
  }
}

async function parseApiResponse<T>(response: Response) {
  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function refreshStoredAuthCore(options: { apiBaseUrl?: string; fetchFn?: typeof fetch }) {
  const refreshToken = getStoredRefreshToken();
  if (!refreshToken) {
    clearStoredAuthTokens();
    return false;
  }

  try {
    const response = await callFetch(options.fetchFn, buildApiUrl("/auth/refresh", options.apiBaseUrl), {
      body: JSON.stringify({ refreshToken }),
      headers: createApiHeaders("application/json", false),
      method: "POST",
    });

    if (!response.ok) {
      clearStoredAuthTokens();
      return false;
    }

    const tokens = (await response.json()) as { accessToken?: string; refreshToken?: string | null };
    if (!tokens.accessToken) {
      clearStoredAuthTokens();
      return false;
    }

    setStoredAuthTokens(tokens.accessToken, tokens.refreshToken ?? refreshToken);
    return true;
  } catch {
    clearStoredAuthTokens();
    return false;
  }
}

export function getConfiguredWorkspaceId() {
  const params = typeof window === "undefined" ? null : new URLSearchParams(window.location.search);
  const queryWorkspaceId = params?.get("workspaceId");
  if (queryWorkspaceId && isUuid(queryWorkspaceId)) {
    return queryWorkspaceId;
  }

  const env = (import.meta as ImportMeta & { env?: Record<string, string | undefined> }).env;
  const envWorkspaceId = env?.VITE_NORTHSTAR_WORKSPACE_ID;
  return envWorkspaceId && isUuid(envWorkspaceId) ? envWorkspaceId : null;
}

export function isUuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}

async function toApiError(response: Response) {
  const fallback = `API returned ${response.status}`;

  try {
    const body = (await response.json()) as {
      code?: string;
      error?: { code?: string; message?: string };
      message?: string;
      title?: string;
    };
    const code = body.error?.code ?? body.code;
    const message = body.error?.message ?? body.message ?? body.title ?? fallback;
    return new ApiClientError(response.status, message, code);
  } catch {
    return new ApiClientError(response.status, fallback);
  }
}

function getFirstStoredValue(keys: string[]) {
  if (typeof window === "undefined") {
    return null;
  }

  for (const key of keys) {
    const value = window.localStorage.getItem(key);
    if (value) {
      return value;
    }
  }

  return null;
}

function removeStorageKeys(keys: string[]) {
  if (typeof window === "undefined") {
    return;
  }

  for (const key of keys) {
    window.localStorage.removeItem(key);
  }
}

function notifyAuthStateChanged() {
  if (typeof window === "undefined") {
    return;
  }

  window.dispatchEvent(new Event(authStateEventName));
}

function callFetch(fetchFn: typeof fetch | undefined, input: RequestInfo | URL, init?: RequestInit) {
  if (fetchFn) {
    return fetchFn(input, init);
  }

  if (typeof globalThis.fetch !== "function") {
    throw new ApiClientError(0, "Fetch is not available in this runtime.");
  }

  return globalThis.fetch.bind(globalThis)(input, init);
}

function toRequestError(error: unknown, url: string) {
  if (isAbortError(error) || error instanceof ApiClientError) {
    return error;
  }

  const reason = error instanceof Error && error.message ? ` ${error.message}` : "";
  return new ApiClientError(0, `Could not reach API endpoint ${url}.${reason}`);
}

function isAbortError(error: unknown) {
  return (
    typeof DOMException !== "undefined" &&
    error instanceof DOMException &&
    error.name === "AbortError"
  );
}
