import { apiFetch } from "./apiClient";
import type { JSONContent } from "@tiptap/react";

export type WorkspaceDto = {
  id: string;
  name: string;
  currentSpaceId: string;
  organizationId: string;
};

export type SpaceDto = {
  id: string;
  name: string;
};

export type KnowledgeFolderDto = {
  id: string;
  title: string;
  sortOrder: number;
  documentCount: number;
};

export type KnowledgeDocumentSummaryDto = {
  id: string;
  folderId: string;
  title: string;
  status: string;
  updatedAt: string;
  tags: string[];
  sortOrder: number;
};

export type OwnerDto = {
  id: string;
  name: string;
};

export type KnowledgeDocumentDto = KnowledgeDocumentSummaryDto & {
  content: JSONContent;
  owner: OwnerDto;
  revision: number;
  version: string;
};

export type BootstrapResponse = {
  workspace: WorkspaceDto;
  spaces: SpaceDto[];
  activeSpaceId: string;
  folders: KnowledgeFolderDto[];
  documents: KnowledgeDocumentSummaryDto[];
  activeDocumentId: string;
};

export type SearchResultDto = {
  id: string;
  type: string;
  title: string;
  folderId: string;
  excerpt: string;
  updatedAt: string;
};

export type SearchResponse = {
  results: SearchResultDto[];
};

export type CreateDocumentResponse = {
  document: KnowledgeDocumentDto;
  map: KnowledgeMapResponse;
};

export type KnowledgeMapResponse = {
  folders: KnowledgeFolderDto[];
  documents: KnowledgeDocumentSummaryDto[];
};

export type GetDocumentResponse = {
  document: KnowledgeDocumentDto;
};

export type UpdateDocumentResponse = {
  document: KnowledgeDocumentDto;
};

export type MoveDocumentResponse = {
  document: KnowledgeDocumentSummaryDto;
  map: KnowledgeMapResponse;
};

export type CollectionMutationResponse = {
  collection: KnowledgeFolderDto;
  map: KnowledgeMapResponse;
};

export type ActivityTimelineItemDto = {
  actor?: {
    id: string;
    name: string;
  } | null;
  id: string;
  title: string;
  date: string;
  detail: string;
  document?: {
    id: string;
    title: string;
  } | null;
};

export type DocumentActivityResponse = {
  items: ActivityTimelineItemDto[];
};

export type RelatedDocumentDto = {
  id: string;
  code: string;
  title: string;
};

export type VersionTrailItemDto = {
  id: string;
  version: string;
  date: string;
  author: string;
  status: string;
};

export type BacklinkItemDto = {
  id: string;
  code: string;
  title: string;
  excerpt: string;
};

export type DocumentContextResponse = {
  relatedDocuments: RelatedDocumentDto[];
  versionTrail: VersionTrailItemDto[];
  backlinks: BacklinkItemDto[];
};

export type PermissionNotificationDto = {
  id: string;
  workspaceId: string;
  recipientUserId: string;
  actorUserId?: string | null;
  type: string;
  resourceType?: string | null;
  resourceId?: string | null;
  accessRequestId?: string | null;
  permissionGrantId?: string | null;
  title: string;
  body?: string | null;
  actionUrl?: string | null;
  readAt?: string | null;
  createdAt: string;
};

export type PermissionNotificationsResponse = {
  notifications: PermissionNotificationDto[];
  unreadCount: number;
};

export type PermissionNotificationPreferenceDto = {
  id: string;
  workspaceId: string;
  userId: string;
  resourceType?: string | null;
  resourceId?: string | null;
  watched: boolean;
  muted: boolean;
  createdAt: string;
  updatedAt: string;
};

export type PermissionNotificationPreferencesResponse = {
  preferences: PermissionNotificationPreferenceDto[];
};

export type UpdatePermissionNotificationPreferenceRequest = {
  workspaceId: string;
  resourceType?: string | null;
  resourceId?: string | null;
  watched: boolean;
  muted: boolean;
};

export type WorkspaceMemberDto = {
  userId: string;
  email?: string | null;
  displayName: string;
  role: string;
  status: string;
  joinedAt?: string | null;
};

export type WorkspaceMembersResponse = {
  members: WorkspaceMemberDto[];
};

export type WorkspaceAgendaItemDto = {
  id: string;
  title: string;
  detail: string;
  category: string;
  kind: "break" | "document" | "meeting" | "task" | string;
  date: string;
  startTime: string;
  endTime?: string | null;
  durationMinutes: number;
  resourceType?: string | null;
  resourceId?: string | null;
  actionUrl?: string | null;
  connectedToCalendar: boolean;
  calendarStatus: string;
};

export type WorkspaceAgendaResponse = {
  workspaceId: string;
  date: string;
  calendarStatus: string;
  today: WorkspaceAgendaItemDto[];
  upcoming: WorkspaceAgendaItemDto[];
};

export type OrganizationWorkspaceDto = {
  id: string;
  name: string;
  slug: string;
  currentSpaceId: string;
  currentUserRole: string;
  createdAt: string;
};

export type OrganizationProfileDto = {
  id: string;
  name: string;
  slug: string;
  status: string;
  workspaces: OrganizationWorkspaceDto[];
  createdAt: string;
  updatedAt: string;
};

export type OrganizationProfileResponse = {
  organization: OrganizationProfileDto;
};

export type UpdateOrganizationProfileRequest = {
  name: string;
  slug: string;
};

export type OrganizationMemberWorkspaceDto = {
  workspaceId: string;
  workspaceName: string;
  role: string;
  status: string;
  joinedAt?: string | null;
};

export type OrganizationMemberDto = {
  userId: string;
  email?: string | null;
  displayName: string;
  status: string;
  workspaces: OrganizationMemberWorkspaceDto[];
};

export type OrganizationMembersResponse = {
  members: OrganizationMemberDto[];
};

export type ShareLinkRole = "commenter" | "viewer";

export type ShareLinkAudience = "external" | "public" | "workspace";
export type PermissionGrantRole = "admin" | "commenter" | "editor" | "owner" | "viewer";
export type PermissionGrantSubjectType = "group" | "user";

export type PermissionPolicyDto = {
  inheritanceMode: string;
  linkMode: string;
  defaultLinkRole: string | null;
};

export type PermissionGrantDto = {
  id: string;
  subjectType: string;
  subjectId: string;
  roleKey: string;
  grantedBy: string | null;
  grantedAt: string;
  expiresAt: string | null;
  reason: string | null;
};

export type EffectivePermissionResponse = {
  allowedActions: string[];
  effectiveRole: string | null;
  source: string;
  inheritanceMode: string;
};

export type ResourcePermissionsResponse = {
  resourceType: string;
  resourceId: string;
  policy: PermissionPolicyDto;
  grants: PermissionGrantDto[];
  effectiveAccess: EffectivePermissionResponse;
  inheritedFrom: string;
  availableRoles: string[];
};

export type ShareLinkDto = {
  id: string;
  workspaceId: string;
  resourceType: string;
  resourceId: string;
  roleKey: ShareLinkRole;
  audience: ShareLinkAudience;
  subjectEmail: string | null;
  createdBy: string | null;
  createdAt: string;
  expiresAt: string | null;
  revokedAt: string | null;
  hasPassword: boolean;
};

export type ShareLinksResponse = {
  links: ShareLinkDto[];
};

export type CreateShareLinkRequest = {
  roleKey: ShareLinkRole;
  audience?: ShareLinkAudience | null;
  expiresAt?: string | null;
  subjectEmail?: string | null;
  password?: string | null;
};

export type CreateShareLinkResponse = {
  link: ShareLinkDto;
  token: string;
  url: string;
};

export type EmailInviteDto = {
  id: string;
  workspaceId: string;
  resourceType: string;
  resourceId: string;
  email: string;
  roleKey: ShareLinkRole;
  status: "accepted" | "expired" | "pending" | "revoked";
  invitedBy: string | null;
  acceptedBy: string | null;
  revokedBy: string | null;
  createdAt: string;
  expiresAt: string;
  acceptedAt: string | null;
  revokedAt: string | null;
  expiredAt: string | null;
  deliveryStatus: string;
  deliveryProvider: string;
  deliveryAttemptedAt: string | null;
  deliveryErrorCode: string | null;
};

export type EmailInvitesResponse = {
  invites: EmailInviteDto[];
};

export type CreateEmailInviteRequest = {
  email: string;
  roleKey: ShareLinkRole;
  expiresAt: string;
};

export type CreateEmailInviteResponse = {
  invite: EmailInviteDto;
  token: string;
  url: string;
  delivery?: {
    status: string;
    provider: string;
    attemptedAt: string | null;
    errorCode: string | null;
  };
};

export function getBootstrap(signal?: AbortSignal) {
  return apiFetch<BootstrapResponse>("/bootstrap", { signal });
}

export function searchKnowledge(request: { q: string; spaceId: string }, signal?: AbortSignal) {
  const params = new URLSearchParams({ q: request.q, spaceId: request.spaceId });
  return apiFetch<SearchResponse>(`/search?${params.toString()}`, { signal });
}

export function getSpaceMap(spaceId: string, signal?: AbortSignal) {
  return apiFetch<KnowledgeMapResponse>(`/spaces/${spaceId}/map`, { signal });
}

export function createCollection(
  spaceId: string,
  request: { sortOrder?: number | null; title: string },
  signal?: AbortSignal,
) {
  return apiFetch<CollectionMutationResponse>(`/spaces/${spaceId}/collections`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function updateCollection(
  spaceId: string,
  collectionId: string,
  request: { sortOrder?: number | null; title?: string | null },
  signal?: AbortSignal,
) {
  return apiFetch<CollectionMutationResponse>(`/spaces/${spaceId}/collections/${collectionId}`, {
    body: request,
    method: "PATCH",
    signal,
  });
}

export function deleteCollection(spaceId: string, collectionId: string, signal?: AbortSignal) {
  return apiFetch<KnowledgeMapResponse>(`/spaces/${spaceId}/collections/${collectionId}`, {
    method: "DELETE",
    signal,
  });
}

export function reorderCollections(
  spaceId: string,
  request: { collectionIds: string[] },
  signal?: AbortSignal,
) {
  return apiFetch<KnowledgeMapResponse>(`/spaces/${spaceId}/collections/order`, {
    body: request,
    method: "PUT",
    signal,
  });
}

export function createDocument(request: { folderId: string; title?: string | null }, signal?: AbortSignal) {
  return apiFetch<CreateDocumentResponse>("/documents", {
    body: request,
    method: "POST",
    signal,
  });
}

export function getDocument(documentId: string, signal?: AbortSignal) {
  return apiFetch<GetDocumentResponse>(`/documents/${documentId}`, { signal });
}

export function moveDocument(
  documentId: string,
  request: { folderId: string; sortOrder?: number | null },
  signal?: AbortSignal,
) {
  return apiFetch<MoveDocumentResponse>(`/documents/${documentId}/location`, {
    body: request,
    method: "PATCH",
    signal,
  });
}

export function archiveDocument(documentId: string, signal?: AbortSignal) {
  return apiFetch<MoveDocumentResponse>(`/documents/${documentId}/archive`, {
    method: "PATCH",
    signal,
  });
}

export function restoreDocument(documentId: string, signal?: AbortSignal) {
  return apiFetch<MoveDocumentResponse>(`/documents/${documentId}/restore`, {
    method: "PATCH",
    signal,
  });
}

export function deleteDocument(documentId: string, signal?: AbortSignal) {
  return apiFetch<void>(`/documents/${documentId}`, {
    method: "DELETE",
    signal,
  });
}

export function getDocumentActivity(documentId: string, signal?: AbortSignal) {
  return apiFetch<DocumentActivityResponse>(`/documents/${documentId}/activity`, { signal });
}

export function getDocumentContext(documentId: string, signal?: AbortSignal) {
  return apiFetch<DocumentContextResponse>(`/documents/${documentId}/context`, { signal });
}

export function getWorkspaceNotifications(workspaceId?: string | null, signal?: AbortSignal) {
  const params = new URLSearchParams();
  if (workspaceId) {
    params.set("workspaceId", workspaceId);
  }

  return apiFetch<PermissionNotificationsResponse>(`/notifications${params.size ? `?${params.toString()}` : ""}`, { signal });
}

export function markWorkspaceNotificationRead(notificationId: string, signal?: AbortSignal) {
  return apiFetch<PermissionNotificationDto>(`/notifications/${notificationId}/read`, {
    body: { read: true },
    method: "PATCH",
    signal,
  });
}

export function markAllWorkspaceNotificationsRead(workspaceId?: string | null, signal?: AbortSignal) {
  return apiFetch<void>("/notifications/read-all", {
    body: { workspaceId: workspaceId ?? null },
    method: "PATCH",
    signal,
  });
}

export function getWorkspaceNotificationPreferences(workspaceId: string, signal?: AbortSignal) {
  const params = new URLSearchParams({ workspaceId });
  return apiFetch<PermissionNotificationPreferencesResponse>(`/notifications/preferences?${params.toString()}`, {
    signal,
  });
}

export function updateWorkspaceNotificationPreference(
  request: UpdatePermissionNotificationPreferenceRequest,
  signal?: AbortSignal,
) {
  return apiFetch<PermissionNotificationPreferenceDto>("/notifications/preferences", {
    body: request,
    method: "PUT",
    signal,
  });
}

export function getWorkspaceMembers(workspaceId: string, signal?: AbortSignal) {
  return apiFetch<WorkspaceMembersResponse>(`/workspaces/${workspaceId}/members`, { signal });
}

export function getWorkspaceAgenda(workspaceId: string, date: string, signal?: AbortSignal) {
  const params = new URLSearchParams({ date });
  return apiFetch<WorkspaceAgendaResponse>(`/workspaces/${workspaceId}/agenda?${params.toString()}`, { signal });
}

export function getOrganizationProfile(organizationId: string, signal?: AbortSignal) {
  return apiFetch<OrganizationProfileResponse>(`/organizations/${organizationId}/profile`, { signal });
}

export function updateOrganizationProfile(
  organizationId: string,
  request: UpdateOrganizationProfileRequest,
  signal?: AbortSignal,
) {
  return apiFetch<OrganizationProfileResponse>(`/organizations/${organizationId}/profile`, {
    body: request,
    method: "PATCH",
    signal,
  });
}

export function getOrganizationMembers(organizationId: string, signal?: AbortSignal) {
  return apiFetch<OrganizationMembersResponse>(`/organizations/${organizationId}/members`, { signal });
}

export function getDocumentShareLinks(documentId: string, signal?: AbortSignal) {
  return apiFetch<ShareLinksResponse>(`/permissions/resources/document/${documentId}/share-links`, { signal });
}

export function getDocumentResourcePermissions(documentId: string, signal?: AbortSignal) {
  return apiFetch<ResourcePermissionsResponse>(`/permissions/resources/document/${documentId}`, { signal });
}

export function createDocumentPermissionGrant(
  documentId: string,
  request: {
    subjectType: PermissionGrantSubjectType;
    subjectId: string;
    roleKey: string;
    expiresAt?: string | null;
    reason?: string | null;
  },
  signal?: AbortSignal,
) {
  return apiFetch<PermissionGrantDto>(`/permissions/resources/document/${documentId}/grants`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function updateDocumentPermissionGrant(
  documentId: string,
  grantId: string,
  request: {
    roleKey: string;
    expiresAt?: string | null;
    reason?: string | null;
  },
  signal?: AbortSignal,
) {
  return apiFetch<PermissionGrantDto>(`/permissions/resources/document/${documentId}/grants/${grantId}`, {
    body: request,
    method: "PATCH",
    signal,
  });
}

export function revokeDocumentPermissionGrant(documentId: string, grantId: string, reason?: string | null, signal?: AbortSignal) {
  return apiFetch<void>(`/permissions/resources/document/${documentId}/grants/${grantId}`, {
    body: { reason: reason ?? null },
    method: "DELETE",
    signal,
  });
}

export function createDocumentShareLink(
  documentId: string,
  request: CreateShareLinkRequest,
  signal?: AbortSignal,
) {
  return apiFetch<CreateShareLinkResponse>(`/permissions/resources/document/${documentId}/share-links`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function revokeShareLink(shareLinkId: string, signal?: AbortSignal) {
  return apiFetch<void>(`/permissions/share-links/${shareLinkId}`, {
    method: "DELETE",
    signal,
  });
}

export function getDocumentEmailInvites(documentId: string, signal?: AbortSignal) {
  return apiFetch<EmailInvitesResponse>(`/permissions/resources/document/${documentId}/email-invites`, { signal });
}

export function createDocumentEmailInvite(
  documentId: string,
  request: CreateEmailInviteRequest,
  signal?: AbortSignal,
) {
  return apiFetch<CreateEmailInviteResponse>(`/permissions/resources/document/${documentId}/email-invites`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function revokeEmailInvite(inviteId: string, signal?: AbortSignal) {
  return apiFetch<void>(`/permissions/email-invites/${inviteId}`, {
    method: "DELETE",
    signal,
  });
}

export function updateDocument(
  documentId: string,
  request: {
    baseRevision: number;
    content?: JSONContent | null;
    tags?: string[] | null;
    title?: string | null;
  },
  signal?: AbortSignal,
) {
  return apiFetch<UpdateDocumentResponse>(`/documents/${documentId}`, {
    body: request,
    method: "PATCH",
    signal,
  });
}
