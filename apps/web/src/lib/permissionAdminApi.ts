import {
  ApiClientError,
  apiFetch,
  createApiHeaders,
  getConfiguredApiBaseUrl,
  getConfiguredWorkspaceId,
  isUuid,
} from "./apiClient";

export type PermissionAdminApiStatus = "unconfigured" | "loading" | "ready" | "forbidden" | "error";

export type WorkspaceMemberDto = {
  userId: string;
  email: string | null;
  displayName: string;
  role: string;
  status: string;
  joinedAt: string | null;
  externalProvider?: string | null;
  externalSubjectId?: string | null;
};

export type WorkspaceMembersResponse = {
  members: WorkspaceMemberDto[];
};

export type WorkspaceGroupDto = {
  id: string;
  workspaceId: string;
  name: string;
  description: string | null;
  type: string;
  isArchived: boolean;
  externalProvider: string | null;
  externalGroupId: string | null;
  externalSyncedAt: string | null;
  membersCount: number;
  createdAt: string;
  updatedAt: string;
};

export type WorkspaceGroupsResponse = {
  groups: WorkspaceGroupDto[];
};

export type ScimSupportedFeature = {
  supported: boolean;
};

export type ScimBulkFeature = {
  supported: boolean;
  maxOperations: number;
  maxPayloadSize: number;
};

export type ScimFilterFeature = {
  supported: boolean;
  maxResults: number;
};

export type ScimAuthenticationSchemeDto = {
  name: string;
  description: string;
  type: string;
  primary: boolean;
};

export type ScimServiceProviderConfigResponse = {
  schemas: string[];
  documentationUri: string;
  patch: ScimSupportedFeature;
  bulk: ScimBulkFeature;
  filter: ScimFilterFeature;
  changePassword: ScimSupportedFeature;
  sort: ScimSupportedFeature;
  etag: ScimSupportedFeature;
  authenticationSchemes: ScimAuthenticationSchemeDto[];
};

export type ScimListResponse<TResource> = {
  schemas: string[];
  totalResults: number;
  itemsPerPage: number;
  startIndex: number;
  resources: TResource[];
};

export type ScimResourceTypeDto = {
  id: string;
  name: string;
  endpoint: string;
  description: string;
  schema: string;
  schemaExtensions: Array<{
    schema: string;
    required: boolean;
  }>;
};

export type ScimSchemaDto = {
  schemas: string[];
  id: string;
  name: string;
  description: string;
  attributes: Array<{
    name: string;
    type: string;
    multiValued: boolean;
    description: string;
    required: boolean;
    mutability: string;
    returned: string;
    uniqueness: string;
  }>;
};

export type ScimTokenDto = {
  id: string;
  workspaceId: string;
  name: string;
  createdBy: string | null;
  createdAt: string;
  expiresAt: string | null;
  revokedAt: string | null;
  lastUsedAt: string | null;
};

export type ScimTokensResponse = {
  tokens: ScimTokenDto[];
};

export type CreateScimTokenResponse = {
  token: ScimTokenDto;
  rawToken: string;
};

export class PermissionAdminApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export function getConfiguredPermissionAdminApiBaseUrl() {
  return getConfiguredApiBaseUrl();
}

export function getConfiguredPermissionAdminWorkspaceId() {
  return getConfiguredWorkspaceId();
}

export async function getWorkspaceMembers(apiBaseUrl: string, workspaceId: string, signal?: AbortSignal) {
  return requestJson<WorkspaceMembersResponse>(apiBaseUrl, `/workspaces/${workspaceId}/members`, { signal });
}

export async function addWorkspaceMember(apiBaseUrl: string, workspaceId: string, email: string, role: string) {
  return requestJson<WorkspaceMemberDto>(apiBaseUrl, `/workspaces/${workspaceId}/members`, {
    body: JSON.stringify({ email, role }),
    headers: createPermissionAdminHeaders("application/json"),
    method: "POST",
  });
}

export async function updateWorkspaceMemberRole(
  apiBaseUrl: string,
  workspaceId: string,
  userId: string,
  role: string,
) {
  return requestJson<WorkspaceMemberDto>(apiBaseUrl, `/workspaces/${workspaceId}/members/${userId}`, {
    body: JSON.stringify({ role }),
    headers: createPermissionAdminHeaders("application/json"),
    method: "PATCH",
  });
}

export async function removeWorkspaceMember(apiBaseUrl: string, workspaceId: string, userId: string) {
  await requestNoContent(apiBaseUrl, `/workspaces/${workspaceId}/members/${userId}`, {
    headers: createPermissionAdminHeaders(),
    method: "DELETE",
  });
}

export async function getWorkspaceGroups(apiBaseUrl: string, workspaceId: string, signal?: AbortSignal) {
  return requestJson<WorkspaceGroupsResponse>(apiBaseUrl, `/workspaces/${workspaceId}/groups`, { signal });
}

export async function getScimServiceProviderConfig(apiBaseUrl: string, workspaceId: string, signal?: AbortSignal) {
  return requestJson<ScimServiceProviderConfigResponse>(
    apiBaseUrl,
    `/workspaces/${workspaceId}/scim/v2/ServiceProviderConfig`,
    { signal },
  );
}

export async function getScimSchemas(apiBaseUrl: string, workspaceId: string, signal?: AbortSignal) {
  return requestJson<ScimListResponse<ScimSchemaDto>>(apiBaseUrl, `/workspaces/${workspaceId}/scim/v2/Schemas`, {
    signal,
  });
}

export async function getScimResourceTypes(apiBaseUrl: string, workspaceId: string, signal?: AbortSignal) {
  return requestJson<ScimListResponse<ScimResourceTypeDto>>(
    apiBaseUrl,
    `/workspaces/${workspaceId}/scim/v2/ResourceTypes`,
    { signal },
  );
}

export async function getScimTokens(apiBaseUrl: string, workspaceId: string, signal?: AbortSignal) {
  return requestJson<ScimTokensResponse>(apiBaseUrl, `/workspaces/${workspaceId}/scim/tokens`, { signal });
}

export async function createScimToken(
  apiBaseUrl: string,
  workspaceId: string,
  request: { name: string; expiresAt: string | null },
) {
  return requestJson<CreateScimTokenResponse>(apiBaseUrl, `/workspaces/${workspaceId}/scim/tokens`, {
    body: JSON.stringify(request),
    headers: createPermissionAdminHeaders("application/json"),
    method: "POST",
  });
}

export async function revokeScimToken(apiBaseUrl: string, workspaceId: string, tokenId: string) {
  await requestNoContent(apiBaseUrl, `/workspaces/${workspaceId}/scim/tokens/${tokenId}`, {
    headers: createPermissionAdminHeaders(),
    method: "DELETE",
  });
}

async function requestJson<T>(_apiBaseUrl: string, path: string, options: RequestInit = {}) {
  try {
    return await apiFetch<T>(path, {
      ...options,
      body: options.body,
    });
  } catch (error) {
    throw toPermissionAdminApiError(error);
  }
}

async function requestNoContent(_apiBaseUrl: string, path: string, options: RequestInit = {}) {
  try {
    await apiFetch<void>(path, {
      ...options,
      body: options.body,
    });
  } catch (error) {
    throw toPermissionAdminApiError(error);
  }
}

function createPermissionAdminHeaders(contentType?: string) {
  return createApiHeaders(contentType);
}

function toPermissionAdminApiError(error: unknown) {
  if (error instanceof ApiClientError) {
    return new PermissionAdminApiError(error.status, error.message);
  }

  return new PermissionAdminApiError(0, "Permission admin API request failed.");
}
