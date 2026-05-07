import {
  apiFetch,
  type ApiFetchOptions,
  clearStoredAuthTokens,
  getStoredRefreshToken,
  setStoredAuthTokens,
} from "./apiClient";

type AuthClientRequestOptions = Pick<ApiFetchOptions, "apiBaseUrl" | "fetchFn">;

export type AuthUserDto = {
  id: string;
  email: string;
  displayName: string;
};

export type AuthWorkspaceDto = {
  id: string;
  name: string;
  role: string;
};

export type AuthTokenResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUserDto;
};

export type MeResponse = {
  user: AuthUserDto;
  workspaces: AuthWorkspaceDto[];
};

export type AuthSecurityStateResponse = {
  userId: string;
  recentAuthAt: string | null;
  recentAuthWindowMinutes: number;
  hasRecentAuth: boolean;
  mfaEnabled: boolean;
  mfaVerified: boolean;
  mfaVerifiedAt: string | null;
  stepUpRequiredForHighRiskActions: boolean;
  stepUpMethods: string[];
};

export async function login(request: { email: string; password: string }, options: AuthClientRequestOptions = {}) {
  const response = await apiFetch<AuthTokenResponse>("/auth/login", {
    ...options,
    auth: false,
    body: request,
    method: "POST",
  });
  storeAuthResponse(response);
  return response;
}

export async function register(
  request: { email: string; displayName: string; password: string },
  options: AuthClientRequestOptions = {},
) {
  const response = await apiFetch<AuthTokenResponse>("/auth/register", {
    ...options,
    auth: false,
    body: request,
    method: "POST",
  });
  storeAuthResponse(response);
  return response;
}

export async function refreshAuth() {
  const refreshToken = getStoredRefreshToken();
  if (!refreshToken) {
    return null;
  }

  const response = await apiFetch<AuthTokenResponse>("/auth/refresh", {
    auth: false,
    body: { refreshToken },
    method: "POST",
  });
  storeAuthResponse(response);
  return response;
}

export async function logout() {
  const refreshToken = getStoredRefreshToken();

  try {
    if (refreshToken) {
      await apiFetch<void>("/auth/logout", {
        auth: false,
        body: { refreshToken },
        method: "POST",
      });
    }
  } finally {
    clearStoredAuthTokens();
  }
}

export function getCurrentUser() {
  return apiFetch<MeResponse>("/auth/me");
}

export function getSecurityState() {
  return apiFetch<AuthSecurityStateResponse>("/auth/security-state");
}

function storeAuthResponse(response: AuthTokenResponse) {
  setStoredAuthTokens(response.accessToken, response.refreshToken);
}
