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

export type UploadTargetDto = {
  headers: Record<string, string>;
  method: string;
  type: string;
  url: string;
};

export type CreateUploadSessionRequest = {
  bizType?: string | null;
  byteSize: number;
  checksumSha256?: string | null;
  documentId?: string | null;
  idempotencyKey: string;
  mimeType: string;
  originalFilename: string;
  uploadMode: string;
  workspaceId?: string | null;
};

export type CreateUploadSessionResponse = {
  expiresAt: string;
  sessionId: string;
  status: string;
  uploadMode: string;
  uploadTarget: UploadTargetDto;
};

export type UploadSessionDto = {
  bizType?: string | null;
  byteSize: number;
  checksumSha256?: string | null;
  createdAt: string;
  expiresAt: string;
  finalizedFileId?: string | null;
  id: string;
  idempotencyKey: string;
  mimeType: string;
  originalFilename: string;
  ownerId: string;
  status: string;
  updatedAt: string;
  uploadMode: string;
  workspaceId: string;
};

export type FileDto = {
  byteSize: number;
  checksumSha256?: string | null;
  createdAt: string;
  height?: number | null;
  id: string;
  metadata: Record<string, unknown>;
  mimeType: string;
  originalFilename: string;
  processingStatus: string;
  scanStatus: string;
  uploadedBy?: string | null;
  width?: number | null;
  workspaceId: string;
};

export type DocumentAttachmentDto = {
  createdAt: string;
  documentId: string;
  file: FileDto;
  fileId: string;
  id: string;
  metadata: Record<string, unknown>;
  relationType: "attachment" | "cover" | "inline_image" | string;
  workspaceId: string;
};

export type FinalizeUploadSessionRequest = {
  documentId?: string | null;
  metadata?: Record<string, unknown> | null;
  relationType?: "attachment" | "cover" | "inline_image" | null;
};

export type FinalizeUploadSessionResponse = {
  attachment?: DocumentAttachmentDto | null;
  file: FileDto;
};

export type AttachFileToDocumentRequest = {
  fileId: string;
  metadata?: Record<string, unknown> | null;
  relationType: "attachment" | "cover" | "inline_image";
};

export type DocumentAttachmentsResponse = {
  attachments: DocumentAttachmentDto[];
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

export type DocumentVersionSummaryDto = {
  id: string;
  documentId: string;
  versionNo: number;
  label: string;
  versionType: string;
  createdAt: string;
  publishedAt?: string | null;
  createdBy?: string | null;
  wordCount: number;
};

export type DocumentVersionsResponse = {
  versions: DocumentVersionSummaryDto[];
};

export type DocumentVersionResponse = {
  version: DocumentVersionSummaryDto;
  content: JSONContent;
  outline: JSONContent;
};

export type PublishDocumentVersionResponse = {
  document: KnowledgeDocumentDto;
  version: DocumentVersionSummaryDto;
};

export type UnpublishDocumentVersionResponse = {
  document: KnowledgeDocumentDto;
  unpublishedVersion?: DocumentVersionSummaryDto | null;
};

export type RestoreDocumentVersionResponse = {
  document: KnowledgeDocumentDto;
  restoredFrom: DocumentVersionSummaryDto;
};

export type DocumentVersionCompareTargetDto = {
  type: "draft" | "current" | "version" | string;
  versionId?: string | null;
};

export type CompareDocumentVersionsRequest = {
  from: DocumentVersionCompareTargetDto;
  to: DocumentVersionCompareTargetDto;
};

export type DocumentVersionCompareSegmentDto = {
  kind: "added" | "removed" | "unchanged" | string;
  text: string;
};

export type DocumentVersionCompareTokenDto = {
  kind: "added" | "modified" | "removed" | "unchanged" | string;
  text: string;
};

export type DocumentVersionCompareLineDto = {
  kind: "added" | "modified" | "removed" | "unchanged" | string;
  leftText?: string | null;
  rightText?: string | null;
  leftTokens: DocumentVersionCompareTokenDto[];
  rightTokens: DocumentVersionCompareTokenDto[];
};

export type CompareDocumentVersionsResponse = {
  from: DocumentVersionCompareTargetDto;
  to: DocumentVersionCompareTargetDto;
  summary: {
    fromLabel: string;
    toLabel: string;
    textChanged: boolean;
    addedSegments: number;
    removedSegments: number;
    wordCountDelta: number;
  };
  segments: DocumentVersionCompareSegmentDto[];
  lines?: DocumentVersionCompareLineDto[];
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
  actor?: PermissionNotificationActorDto | null;
  resource?: PermissionNotificationResourceDto | null;
  action?: PermissionNotificationActionDto | null;
  category?: string | null;
  state?: string | null;
};

export type PermissionNotificationsResponse = {
  notifications: PermissionNotificationDto[];
  unreadCount: number;
};

export type PermissionNotificationResourceDto = {
  resourceType: string;
  resourceId: string;
  title: string;
  path?: string | null;
};

export type PermissionNotificationActionDto = {
  kind: string;
  label: string;
  resourceType?: string | null;
  resourceId?: string | null;
  accessRequestId?: string | null;
  permissionGrantId?: string | null;
  subjectType?: string | null;
  subjectId?: string | null;
};

export type PermissionNotificationActorDto = {
  id: string;
  displayName: string;
  email?: string | null;
};

export type AccessSharingSummaryResponse = {
  totalCount: number;
  unreadCount: number;
  pendingReviewCount: number;
  accessRequestCount: number;
  grantCount: number;
  sharingCount: number;
  expiryCount: number;
  failedInviteCount: number;
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
  resource?: PermissionNotificationPreferenceResourceDto | null;
};

export type PermissionNotificationPreferenceResourceDto = {
  resourceType: string;
  resourceId: string;
  title: string;
  path?: string | null;
};

export type PermissionNotificationPreferencesResponse = {
  preferences: PermissionNotificationPreferenceDto[];
};

export type WorkspaceAuditEventDto = {
  id: string;
  workspaceId: string;
  actorId?: string | null;
  actorName?: string | null;
  actorEmail?: string | null;
  action: string;
  resourceType: string;
  resourceId: string;
  subjectType?: string | null;
  subjectId?: string | null;
  beforeJson?: string | null;
  afterJson?: string | null;
  metadata: string;
  createdAt: string;
};

export type WorkspaceAuditLogResponse = {
  events: WorkspaceAuditEventDto[];
  offset: number;
  limit: number;
  totalCount: number;
  hasMore: boolean;
};

export type WorkspaceAuditLogQuery = {
  action?: string | null;
  actorId?: string | null;
  from?: string | null;
  limit?: number | null;
  offset?: number | null;
  resourceId?: string | null;
  resourceType?: string | null;
  to?: string | null;
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
export type PublicShareResourceType = "collection" | "document" | "library";
export type LinkManagementStatus = "active" | "expired" | "paused" | "policy_paused" | "revoked" | string;
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

export type ShareLinkContentProtectionDto = {
  disableDownload: boolean;
  disablePrint: boolean;
  disableCopy: boolean;
  watermarkEnabled: boolean;
  watermarkText: string;
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
  pausedAt?: string | null;
  hasPassword: boolean;
  contentProtection?: ShareLinkContentProtectionDto | null;
  status?: string | null;
};

export type ShareLinksResponse = {
  links: ShareLinkDto[];
};

export type ResolvePublicShareLinkResponse = {
  audience: "public";
  expiresAt: string;
  hasPassword: boolean;
  resourceId: string;
  resourceType: PublicShareResourceType;
  roleKey: "viewer";
  workspaceId: string;
  contentProtection?: ShareLinkContentProtectionDto | null;
};

export type PublicShareDocumentDto = {
  content: JSONContent;
  id: string;
  revision: number;
  status: "archived" | "draft" | "published" | string;
  tags: string[];
  title: string;
  updatedAt: string;
};

export type PublicShareDocumentResponse = {
  document: PublicShareDocumentDto;
  link: ResolvePublicShareLinkResponse;
};

export type PublicShareTreeNodeDto = {
  hasChildren: boolean;
  id: string;
  parentId: string | null;
  sortOrder: number;
  title: string;
  type: PublicShareResourceType;
  updatedAt: string;
};

export type PublicShareTreeResponse = {
  contentProtection?: ShareLinkContentProtectionDto | null;
  nodes: PublicShareTreeNodeDto[];
  scopeType: PublicShareResourceType;
  shareLinkId: string;
  title: string;
};

export type PublicShareCollectionDocumentDto = {
  id: string;
  sortOrder: number;
  status: "draft" | "published" | string;
  tags: string[];
  title: string;
  updatedAt: string;
};

export type PublicShareCollectionDto = {
  documents: PublicShareCollectionDocumentDto[];
  id: string;
  sortOrder: number;
  title: string;
  updatedAt: string;
};

export type PublicShareCollectionResponse = {
  collection: PublicShareCollectionDto;
  link: ResolvePublicShareLinkResponse;
};

export type PublicShareRequestOptions = {
  apiBaseUrl?: string;
  fetchFn?: typeof fetch;
  password?: string | null;
  signal?: AbortSignal;
};

export type LinkManagementDto = {
  id: string;
  workspaceId: string;
  resourceType: string;
  resourceId: string;
  resourceTitle: string | null;
  roleKey: ShareLinkRole;
  audience: ShareLinkAudience;
  subjectEmail: string | null;
  createdBy: string | null;
  createdByDisplayName: string | null;
  createdAt: string;
  expiresAt: string | null;
  revokedAt: string | null;
  pausedAt: string | null;
  pausedBy: string | null;
  pauseReason: string | null;
  hasPassword: boolean;
  status: LinkManagementStatus;
  linkMode: string | null;
  policyState: string | null;
  lastAccessedAt: string | null;
  accessCount: number;
  uniqueVisitorCount: number;
  recentFailCount: number;
  externalOrPublicAccessCount: number;
  contentProtection?: ShareLinkContentProtectionDto | null;
  canManage: boolean;
  canUpdate: boolean;
  canPause: boolean;
  canRevoke: boolean;
};

export type LinkManagementListResponse = {
  links: LinkManagementDto[];
  offset: number;
  limit: number;
  totalCount: number;
  hasMore: boolean;
};

export type LinkManagementListQuery = {
  audience?: ShareLinkAudience | null;
  limit?: number | null;
  offset?: number | null;
  q?: string | null;
  resourceId?: string | null;
  resourceType?: string | null;
  roleKey?: ShareLinkRole | null;
  status?: string | null;
  workspaceId?: string | null;
};

export type UpdateShareLinkRequest = {
  roleKey?: ShareLinkRole | null;
  expiresAt?: string | null;
  reason?: string | null;
};

export type ShareLinkPauseRequest = {
  reason?: string | null;
};

export type ShareLinkCopyEventRequest = {
  copiedValueType?: "share_url" | "link_id" | string | null;
  reason?: string | null;
};

export type CopyShareLinkResponse = {
  linkId: string;
  url: string;
  reissued: boolean;
};

export type ShareLinkAccessTrendPointDto = {
  date: string;
  successCount: number;
  failCount: number;
};

export type ShareLinkSourceBreakdownDto = {
  source: string;
  count: number;
  percentage: number;
};

export type ShareLinkAccessStatsResponse = {
  shareLinkId: string;
  lastAccessedAt: string | null;
  accessCount: number;
  uniqueVisitorCount: number;
  lastAccessIp: string | null;
  treeViewCount: number;
  documentViewCount: number;
  scopeDeniedCount: number;
  passwordFailedCount: number;
  latestEventCategory: string | null;
  recentWindowDays: number;
  trend: ShareLinkAccessTrendPointDto[];
  sourceBreakdown: ShareLinkSourceBreakdownDto[];
};

export type ShareLinkAccessEventDto = {
  id: string;
  shareLinkId: string;
  accessedBy: string | null;
  actorUserId: string | null;
  actorDisplayName: string | null;
  actorType: string;
  accessedAt: string;
  occurredAt: string;
  ip: string | null;
  userAgent: string | null;
  deviceSummary: string | null;
  eventType: string;
  result: string;
  failureCategory: string | null;
  eventCategory: string;
  scopeType: string | null;
  resourceType: string;
  resourceId: string;
  documentId: string | null;
};

export type ShareLinkAccessEventsResponse = {
  events: ShareLinkAccessEventDto[];
  offset: number;
  limit: number;
  totalCount: number;
  hasMore: boolean;
};

export type ShareLinkAccessEventsQuery = {
  eventType?: string | null;
  limit?: number | null;
  offset?: number | null;
  result?: string | null;
};

export type CreateShareLinkRequest = {
  roleKey: ShareLinkRole;
  audience?: ShareLinkAudience | null;
  expiresAt?: string | null;
  subjectEmail?: string | null;
  password?: string | null;
  contentProtection?: ShareLinkContentProtectionDto | null;
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

export type AccessRequestDto = {
  id: string;
  workspaceId: string;
  resourceType: string;
  resourceId: string;
  requesterId: string;
  subjectType: string;
  subjectId: string;
  requestedRole: PermissionGrantRole;
  reason?: string | null;
  status: "approved" | "cancelled" | "denied" | "pending";
  decidedBy?: string | null;
  decidedAt?: string | null;
  decisionReason?: string | null;
  resultingGrantId?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type AccessRequestsResponse = {
  requests: AccessRequestDto[];
};

export type ReviewAccessRequestRequest = {
  decision: "approve" | "deny";
  roleKey?: PermissionGrantRole | null;
  reason?: string | null;
  expiresAt?: string | null;
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

export function getDocumentVersions(documentId: string, signal?: AbortSignal) {
  return apiFetch<DocumentVersionsResponse>(`/documents/${documentId}/versions`, { signal });
}

export function getDocumentVersion(documentId: string, versionId: string, signal?: AbortSignal) {
  return apiFetch<DocumentVersionResponse>(`/documents/${documentId}/versions/${versionId}`, { signal });
}

export function publishDocumentVersion(
  documentId: string,
  request: { baseRevision: number; label?: string | null },
  signal?: AbortSignal,
) {
  return apiFetch<PublishDocumentVersionResponse>(`/documents/${documentId}/versions/publish`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function unpublishDocumentVersion(
  documentId: string,
  request: { baseRevision: number },
  signal?: AbortSignal,
) {
  return apiFetch<UnpublishDocumentVersionResponse>(`/documents/${documentId}/versions/unpublish`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function restoreDocumentVersion(
  documentId: string,
  versionId: string,
  request: { baseRevision: number },
  signal?: AbortSignal,
) {
  return apiFetch<RestoreDocumentVersionResponse>(`/documents/${documentId}/versions/${versionId}/restore`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function compareDocumentVersions(
  documentId: string,
  request: CompareDocumentVersionsRequest,
  signal?: AbortSignal,
) {
  return apiFetch<CompareDocumentVersionsResponse>(`/documents/${documentId}/versions/compare`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function createUploadSession(request: CreateUploadSessionRequest, signal?: AbortSignal) {
  return apiFetch<CreateUploadSessionResponse>("/files/uploads/sessions", {
    body: request,
    method: "POST",
    signal,
  });
}

export function uploadUploadSessionContent(sessionId: string, file: Blob, signal?: AbortSignal) {
  return apiFetch<UploadSessionDto>(`/files/uploads/sessions/${sessionId}/content`, {
    body: file,
    headers: file.type ? { "Content-Type": file.type } : undefined,
    method: "PUT",
    signal,
  });
}

export function completeUploadSession(sessionId: string, signal?: AbortSignal) {
  return apiFetch<UploadSessionDto>(`/files/uploads/sessions/${sessionId}/complete`, {
    body: {},
    method: "POST",
    signal,
  });
}

export function finalizeUploadSession(
  sessionId: string,
  request: FinalizeUploadSessionRequest,
  signal?: AbortSignal,
) {
  return apiFetch<FinalizeUploadSessionResponse>(`/files/uploads/sessions/${sessionId}/finalize`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function getDocumentAttachments(documentId: string, signal?: AbortSignal) {
  return apiFetch<DocumentAttachmentsResponse>(`/documents/${documentId}/attachments`, { signal });
}

export function attachFileToDocument(documentId: string, request: AttachFileToDocumentRequest, signal?: AbortSignal) {
  return apiFetch<DocumentAttachmentDto>(`/documents/${documentId}/attachments`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function deleteDocumentAttachment(documentId: string, attachmentId: string, signal?: AbortSignal) {
  return apiFetch<void>(`/documents/${documentId}/attachments/${attachmentId}`, {
    method: "DELETE",
    signal,
  });
}

export function getWorkspaceNotifications(workspaceId?: string | null, signal?: AbortSignal) {
  const params = new URLSearchParams();
  if (workspaceId) {
    params.set("workspaceId", workspaceId);
  }

  return apiFetch<PermissionNotificationsResponse>(`/notifications${params.size ? `?${params.toString()}` : ""}`, { signal });
}

export function getAccessSharingSummary(workspaceId?: string | null, signal?: AbortSignal) {
  const params = new URLSearchParams();
  if (workspaceId) {
    params.set("workspaceId", workspaceId);
  }

  return apiFetch<AccessSharingSummaryResponse>(`/notifications/summary${params.size ? `?${params.toString()}` : ""}`, {
    signal,
  });
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

export function getWorkspaceAuditLog(
  workspaceId: string,
  query: WorkspaceAuditLogQuery = {},
  signal?: AbortSignal,
) {
  const params = new URLSearchParams();
  setOptionalQueryParam(params, "action", query.action);
  setOptionalQueryParam(params, "actorId", query.actorId);
  setOptionalQueryParam(params, "from", query.from);
  setOptionalQueryParam(params, "resourceId", query.resourceId);
  setOptionalQueryParam(params, "resourceType", query.resourceType);
  setOptionalQueryParam(params, "to", query.to);
  setOptionalNumberQueryParam(params, "limit", query.limit);
  setOptionalNumberQueryParam(params, "offset", query.offset);

  return apiFetch<WorkspaceAuditLogResponse>(
    `/workspaces/${workspaceId}/audit${params.size ? `?${params.toString()}` : ""}`,
    { signal },
  );
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

export function getResourceShareLinks(resourceType: string, resourceId: string, signal?: AbortSignal) {
  return apiFetch<ShareLinksResponse>(
    `/permissions/resources/${encodeURIComponent(resourceType)}/${resourceId}/share-links`,
    { signal },
  );
}

export function getShareLink(shareLinkId: string, signal?: AbortSignal) {
  return apiFetch<ShareLinkDto>(`/permissions/share-links/${encodeURIComponent(shareLinkId)}`, { signal });
}

export function resolvePublicShareLink(token: string, options: PublicShareRequestOptions = {}) {
  return apiFetch<ResolvePublicShareLinkResponse>(buildPublicSharePath(token, "resolve"), createPublicShareRequestOptions(options));
}

export function getPublicShareDocument(token: string, options: PublicShareRequestOptions = {}) {
  return apiFetch<PublicShareDocumentResponse>(buildPublicSharePath(token, "document"), createPublicShareRequestOptions(options));
}

export function getPublicShareTree(token: string, options: PublicShareRequestOptions = {}) {
  return apiFetch<PublicShareTreeResponse>(buildPublicSharePath(token, "tree"), createPublicShareRequestOptions(options));
}

export function getScopedPublicShareDocument(token: string, documentId: string, options: PublicShareRequestOptions = {}) {
  return apiFetch<PublicShareDocumentResponse>(
    buildPublicSharePath(token, `documents/${encodeURIComponent(documentId)}`),
    createPublicShareRequestOptions(options),
  );
}

export function getPublicShareCollection(token: string, options: PublicShareRequestOptions = {}) {
  return apiFetch<PublicShareCollectionResponse>(buildPublicSharePath(token, "collection"), createPublicShareRequestOptions(options));
}

export function buildShareLinkManagementListPath(query: LinkManagementListQuery = {}) {
  const params = new URLSearchParams();
  setOptionalQueryParam(params, "workspaceId", query.workspaceId);
  setOptionalQueryParam(params, "resourceType", query.resourceType);
  setOptionalQueryParam(params, "resourceId", query.resourceId);
  setOptionalQueryParam(params, "audience", query.audience);
  setOptionalQueryParam(params, "roleKey", query.roleKey);
  setOptionalQueryParam(params, "status", query.status);
  setOptionalQueryParam(params, "q", query.q);
  setOptionalNumberQueryParam(params, "offset", query.offset);
  setOptionalNumberQueryParam(params, "limit", query.limit);

  return `/permissions/share-links${params.size ? `?${params.toString()}` : ""}`;
}

export function buildShareLinkManagementDetailPath(shareLinkId: string) {
  return `/permissions/share-links/${encodeURIComponent(shareLinkId)}`;
}

export function buildShareLinkAccessStatsPath(shareLinkId: string) {
  return `${buildShareLinkManagementDetailPath(shareLinkId)}/stats`;
}

export function buildShareLinkAccessEventsPath(shareLinkId: string, query: ShareLinkAccessEventsQuery = {}) {
  const params = new URLSearchParams();
  setOptionalNumberQueryParam(params, "offset", query.offset);
  setOptionalNumberQueryParam(params, "limit", query.limit);
  setOptionalQueryParam(params, "result", query.result);
  setOptionalQueryParam(params, "eventType", query.eventType);

  return `${buildShareLinkManagementDetailPath(shareLinkId)}/access-events${params.size ? `?${params.toString()}` : ""}`;
}

export function buildShareLinkCopyPath(shareLinkId: string) {
  return `${buildShareLinkManagementDetailPath(shareLinkId)}/copy`;
}

export function listShareLinkManagement(query: LinkManagementListQuery = {}, signal?: AbortSignal) {
  return apiFetch<LinkManagementListResponse>(buildShareLinkManagementListPath(query), { signal });
}

export function getShareLinkManagementDetail(shareLinkId: string, signal?: AbortSignal) {
  return apiFetch<LinkManagementDto>(buildShareLinkManagementDetailPath(shareLinkId), { signal });
}

export function getShareLinkStats(shareLinkId: string, signal?: AbortSignal) {
  return apiFetch<ShareLinkAccessStatsResponse>(buildShareLinkAccessStatsPath(shareLinkId), { signal });
}

export function getShareLinkAccessEvents(
  shareLinkId: string,
  query: ShareLinkAccessEventsQuery = {},
  signal?: AbortSignal,
) {
  return apiFetch<ShareLinkAccessEventsResponse>(buildShareLinkAccessEventsPath(shareLinkId, query), { signal });
}

export function updateShareLinkManagement(
  shareLinkId: string,
  request: UpdateShareLinkRequest,
  signal?: AbortSignal,
) {
  return apiFetch<LinkManagementDto>(buildShareLinkManagementDetailPath(shareLinkId), {
    body: request,
    method: "PATCH",
    signal,
  });
}

export function pauseShareLinkManagement(
  shareLinkId: string,
  request: ShareLinkPauseRequest = {},
  signal?: AbortSignal,
) {
  return apiFetch<LinkManagementDto>(`${buildShareLinkManagementDetailPath(shareLinkId)}/pause`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function resumeShareLinkManagement(shareLinkId: string, signal?: AbortSignal) {
  return apiFetch<LinkManagementDto>(`${buildShareLinkManagementDetailPath(shareLinkId)}/resume`, {
    body: {},
    method: "POST",
    signal,
  });
}

export function copyShareLinkManagementUrl(
  shareLinkId: string,
  request: ShareLinkCopyEventRequest = { copiedValueType: "share_url" },
  signal?: AbortSignal,
) {
  return apiFetch<CopyShareLinkResponse>(buildShareLinkCopyPath(shareLinkId), {
    body: request,
    method: "POST",
    signal,
  });
}

export function recordShareLinkCopyEvent(
  shareLinkId: string,
  request: ShareLinkCopyEventRequest = { copiedValueType: "link_id" },
  signal?: AbortSignal,
) {
  return apiFetch<void>(`${buildShareLinkManagementDetailPath(shareLinkId)}/copy-events`, {
    body: request,
    method: "POST",
    signal,
  });
}

export function getDocumentResourcePermissions(documentId: string, signal?: AbortSignal) {
  return apiFetch<ResourcePermissionsResponse>(`/permissions/resources/document/${documentId}`, { signal });
}

export function getResourcePermissions(resourceType: string, resourceId: string, signal?: AbortSignal) {
  return apiFetch<ResourcePermissionsResponse>(
    `/permissions/resources/${encodeURIComponent(resourceType)}/${resourceId}`,
    { signal },
  );
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

export function createResourceShareLink(
  resourceType: string,
  resourceId: string,
  request: CreateShareLinkRequest,
  signal?: AbortSignal,
) {
  return apiFetch<CreateShareLinkResponse>(
    `/permissions/resources/${encodeURIComponent(resourceType)}/${resourceId}/share-links`,
    {
      body: request,
      method: "POST",
      signal,
    },
  );
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

export function getResourceEmailInvites(resourceType: string, resourceId: string, signal?: AbortSignal) {
  return apiFetch<EmailInvitesResponse>(
    `/permissions/resources/${encodeURIComponent(resourceType)}/${resourceId}/email-invites`,
    { signal },
  );
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

export function createResourceEmailInvite(
  resourceType: string,
  resourceId: string,
  request: CreateEmailInviteRequest,
  signal?: AbortSignal,
) {
  return apiFetch<CreateEmailInviteResponse>(
    `/permissions/resources/${encodeURIComponent(resourceType)}/${resourceId}/email-invites`,
    {
      body: request,
      method: "POST",
      signal,
    },
  );
}

export function revokeEmailInvite(inviteId: string, signal?: AbortSignal) {
  return apiFetch<void>(`/permissions/email-invites/${inviteId}`, {
    method: "DELETE",
    signal,
  });
}

export function retryEmailInvite(inviteId: string, signal?: AbortSignal) {
  return apiFetch<CreateEmailInviteResponse>(`/permissions/email-invites/${inviteId}/retry`, {
    body: {},
    method: "POST",
    signal,
  });
}

export function getAccessRequests(
  workspaceId: string,
  status?: AccessRequestDto["status"] | null,
  signal?: AbortSignal,
) {
  const params = new URLSearchParams({ workspaceId });
  if (status) {
    params.set("status", status);
  }

  return apiFetch<AccessRequestsResponse>(`/permissions/access-requests?${params.toString()}`, { signal });
}

export function reviewAccessRequest(
  requestId: string,
  request: ReviewAccessRequestRequest,
  signal?: AbortSignal,
) {
  return apiFetch<AccessRequestDto>(`/permissions/access-requests/${requestId}/review`, {
    body: request,
    method: "POST",
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

function setOptionalQueryParam(params: URLSearchParams, key: string, value?: string | null) {
  if (value && value.trim()) {
    params.set(key, value.trim());
  }
}

function setOptionalNumberQueryParam(params: URLSearchParams, key: string, value?: number | null) {
  if (typeof value === "number" && Number.isFinite(value)) {
    params.set(key, String(value));
  }
}

function buildPublicSharePath(token: string, action: "collection" | "document" | "resolve" | "tree" | `documents/${string}`) {
  return `/public/share-links/${encodeURIComponent(token)}/${action}`;
}

function createPublicShareRequestOptions(options: PublicShareRequestOptions) {
  const headers = new Headers();
  const password = options.password?.trim();

  if (password) {
    headers.set("X-Share-Link-Password", password);
  }

  return {
    apiBaseUrl: options.apiBaseUrl,
    auth: false,
    fetchFn: options.fetchFn,
    headers,
    signal: options.signal,
    skipAuthRefresh: true,
  };
}
