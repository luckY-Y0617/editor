import type { AuthSecurityStateResponse } from "./authClient";
import type {
  BootstrapResponse,
  KnowledgeMapResponse,
  OrganizationMembersResponse,
  OrganizationProfileResponse,
  UpdateOrganizationProfileRequest,
} from "./appApi";
import { createEditorHash, createLibrariesHash, createSettingsHash } from "./hashRouting";
import type { WorkspaceSettingsScope, WorkspaceSettingsTab } from "./hashRouting";
import type { NotificationApiStatus, NotificationPreferenceResourceRow } from "./workspaceUpdatesModel";
import { toNotificationPreferenceResourceRows } from "./workspaceUpdatesModel";
import type { PermissionNotificationPreferenceDto } from "./appApi";

export type WorkspaceSettingsStatus = "assessment" | "deferred" | "live" | "not-exposed" | "reused" | "unavailable";

export type WorkspaceSettingsTabRow = {
  disabled: boolean;
  href: string;
  id: string;
  label: string;
  status: WorkspaceSettingsStatus;
};

export type WorkspaceGeneralSettings = {
  activeSpaceId: string | null;
  activeSpaceName: string;
  spaces: Array<{
    href: string;
    id: string;
    isCurrent: boolean;
    name: string;
  }>;
  updateStatus: WorkspaceSettingsStatus;
  workspaceId: string;
  workspaceName: string;
};

export type SpaceSettingsSummary = {
  collectionCount: number;
  documentCount: number;
  libraryHref: string;
  spaceId: string;
  spaceName: string;
  updateStatus: WorkspaceSettingsStatus;
};

export type LibraryGeneralSettings = {
  collectionCount: number;
  documentCount: number;
  isCurrentLibrary: boolean;
  libraryHref: string;
  spaceId: string;
  spaceName: string;
  updateStatus: WorkspaceSettingsStatus;
  workspaceId: string;
  workspaceName: string;
};

export type LibrarySettingsCollectionRow = {
  documentCount: number;
  href: string;
  id: string;
  position: number;
  sortOrder: number;
  title: string;
};

export type LibrarySettingsDocumentRow = {
  collectionHref: string;
  collectionTitle: string;
  href: string;
  id: string;
  status: string;
  title: string;
  updatedAt: string;
};

export type LibrarySettingsDocumentSummary = {
  archivedCount: number;
  draftCount: number;
  publishedCount: number;
  recentDocuments: LibrarySettingsDocumentRow[];
  totalDocuments: number;
};

export type LibraryNotificationPreferenceRow = {
  href: string;
  id: string;
  label: string;
  resourceType: string;
  state: "muted" | "watched";
  updatedAt: string;
};

export type NotificationSettingsModel = {
  categoryPreferenceStatus: WorkspaceSettingsStatus;
  emailDigestStatus: WorkspaceSettingsStatus;
  preferenceStatus: NotificationApiStatus;
  resourceRows: NotificationPreferenceResourceRow[];
};

export type SecuritySettingsRow = {
  label: string;
  status: WorkspaceSettingsStatus;
  value: string;
};

export type SettingsBoundaryRow = {
  href: string | null;
  id: "library" | "organization" | "system" | "workspace";
  status: WorkspaceSettingsStatus;
};

export type OrganizationContractReadiness = "deferred" | "missing-contract" | "partial" | "reusable";
export type OrganizationReadApiStatus = "error" | "forbidden" | "loading" | "not-found" | "ready" | "unconfigured";

export type OrganizationAssessmentRow = {
  currentSource: string;
  href: string | null;
  id:
    | "audit-log"
    | "billing-plan"
    | "data-retention"
    | "domains"
    | "global-members"
    | "organization-profile"
    | "sso-scim-ownership"
    | "workspace-provisioning";
  implementationDependencies: string;
  implementationRisk: "high" | "low" | "medium";
  proposedDto: string;
  proposedEndpoint: string;
  readiness: OrganizationContractReadiness;
  recommendedPriority: "P1" | "P2" | "P3" | "P4";
  requiredBackendContract: string;
  requiredFrontendSurface: string;
  securityNotes: string;
  status: WorkspaceSettingsStatus;
};

export type OrganizationImplementationSlice = {
  capabilityIds: OrganizationAssessmentRow["id"][];
  reason: string;
  title: string;
};

export type OrganizationOverviewModel = {
  createdAt: string;
  id: string;
  name: string;
  readRule: string;
  slug: string;
  status: string;
  updatedAt: string;
  visibleWorkspaceCount: number;
};

export type OrganizationWorkspaceInventoryRow = {
  createdAt: string;
  currentSpaceId: string;
  currentUserRole: string;
  id: string;
  isCurrentWorkspace: boolean;
  settingsHref: string | null;
  slug: string;
  switchStatus: WorkspaceSettingsStatus;
  switchStatusReason: string;
  name: string;
};

export type OrganizationMemberInventoryRow = {
  displayName: string;
  email: string;
  id: string;
  status: string;
  workspaceCount: number;
  workspaces: Array<{
    joinedAt: string;
    role: string;
    status: string;
    workspaceId: string;
    workspaceName: string;
  }>;
};

export type OrganizationReadSurfaceKind = "members" | "overview" | "workspaces";

export type OrganizationReadSurfaceState = {
  detail: string;
  isEmpty: boolean;
  status: WorkspaceSettingsStatus;
  title: string;
};

export type OrganizationProfileMutationStatus = "error" | "idle" | "saving" | "success";

export type OrganizationProfileEditCapability = {
  canEditOrganizationProfile: boolean;
  editDisabledReason: string;
  mutationStatus: OrganizationProfileMutationStatus;
};

export type OrganizationProfileUpdateInput = {
  name: string;
  slug: string;
};

export type OrganizationProfileUpdateRequestModel = {
  errors: Partial<Record<keyof OrganizationProfileUpdateInput, string>>;
  isValid: boolean;
  request: UpdateOrganizationProfileRequest;
};

export type OrganizationDeferredAction = {
  label: string;
  reason: string;
  status: WorkspaceSettingsStatus;
};

export function createWorkspaceSettingsTabRows(
  _activeTab: string,
  options: { scope?: WorkspaceSettingsScope; spaceId?: string | null } = {},
): WorkspaceSettingsTabRow[] {
  const scope = options.scope ?? "workspace";
  const tabs: Array<{ id: WorkspaceSettingsTab; label: string; status: WorkspaceSettingsStatus }> = scope === "organization"
    ? [
        { id: "overview", label: "Overview", status: "live" },
        { id: "workspaces", label: "Workspaces", status: "live" },
        { id: "members", label: "Members", status: "live" },
        { id: "assessment", label: "Assessment", status: "assessment" },
      ]
    : scope === "library"
    ? [
        { id: "general", label: "General", status: "live" },
        { id: "collections", label: "Collections", status: "live" },
        { id: "documents", label: "Documents", status: "live" },
        { id: "permissions", label: "Permissions", status: "reused" },
        { id: "notifications", label: "Notifications", status: "live" },
        { id: "advanced", label: "Advanced", status: "deferred" },
      ]
    : [
    { id: "general", label: "General", status: "live" },
    { id: "members", label: "Members", status: "reused" },
    { id: "notifications", label: "Notifications", status: "live" },
    { id: "permissions", label: "Permissions", status: "reused" },
    { id: "integrations", label: "Integrations", status: "reused" },
    { id: "security", label: "Security", status: "live" },
    { id: "plan", label: "Plan", status: "deferred" },
    { id: "developer", label: "Developer", status: "deferred" },
  ];

  return tabs.map((tab) => ({
    ...tab,
    disabled: tab.status === "deferred" || tab.status === "not-exposed",
    href: scope === "organization"
      ? createSettingsHash({ scope: "organization", tab: tab.id })
      : scope === "library"
      ? createSettingsHash({ scope: "library", spaceId: options.spaceId, tab: tab.id })
      : createSettingsHash({ tab: tab.id }),
    id: tab.id,
    label: tab.label,
    status: tab.status,
  }));
}

export function toSettingsBoundaryRows(spaceId: string | null): SettingsBoundaryRow[] {
  return [
    {
      href: createSettingsHash({ scope: "workspace", tab: "general" }),
      id: "workspace",
      status: "live",
    },
    {
      href: createSettingsHash({ scope: "library", spaceId, tab: "general" }),
      id: "library",
      status: "live",
    },
    {
      href: createSettingsHash({ scope: "organization", tab: "overview" }),
      id: "organization",
      status: "assessment",
    },
    {
      href: null,
      id: "system",
      status: "not-exposed",
    },
  ];
}

export function toOrganizationSettingsAssessmentRows(): OrganizationAssessmentRow[] {
  return [
    {
      currentSource: "Workspace bootstrap/profile data exists; no organization or tenant entity/API was found in the inspected code.",
      href: null,
      id: "organization-profile",
      implementationDependencies: "Requires Organization read model and migration before a live endpoint can be added.",
      implementationRisk: "medium",
      proposedDto: "OrganizationProfileDto(id, name, status, workspaces[], createdAt, updatedAt)",
      proposedEndpoint: "GET /api/v1/organizations/{organizationId}/profile",
      readiness: "missing-contract",
      recommendedPriority: "P1",
      requiredBackendContract: "Organization read model with id, name, workspaces, and ownership boundary.",
      requiredFrontendSurface: "Read-only Organization Overview in Settings before enabling mutations.",
      securityNotes: "Define organization visibility separately from workspace membership before any write action.",
      status: "assessment",
    },
    {
      currentSource: "Workspace members API and Members surface exist, but membership is scoped to one workspace.",
      href: "#workspace-members",
      id: "global-members",
      implementationDependencies: "Requires Organization membership projection plus organization-level read authorization.",
      implementationRisk: "medium",
      proposedDto: "OrganizationMembersResponse(members: OrganizationMemberDto[], workspaces: OrganizationMemberWorkspaceDto[])",
      proposedEndpoint: "GET /api/v1/organizations/{organizationId}/members",
      readiness: "partial",
      recommendedPriority: "P1",
      requiredBackendContract: "Organization/global members read endpoint or cross-workspace membership projection.",
      requiredFrontendSurface: "Read-only global members list that links back to workspace-scoped member management.",
      securityNotes: "Do not reuse workspace admin mutations as organization-wide mutations without a new permission boundary.",
      status: "assessment",
    },
    {
      currentSource: "Workspace routes and bootstrap data exist; organization-level workspace provisioning contract was not found.",
      href: null,
      id: "workspace-provisioning",
      implementationDependencies: "Depends on Organization profile/read model; provisioning mutations remain out of scope.",
      implementationRisk: "high",
      proposedDto: "OrganizationWorkspacesResponse(workspaces: OrganizationWorkspaceDto[])",
      proposedEndpoint: "GET /api/v1/organizations/{organizationId}/workspaces",
      readiness: "partial",
      recommendedPriority: "P2",
      requiredBackendContract: "Organization-owned workspace list plus explicit create/archive provisioning contract.",
      requiredFrontendSurface: "Read-only workspace inventory first; provisioning controls remain deferred.",
      securityNotes: "Provisioning must define owner/admin constraints and avoid silently creating workspace access.",
      status: "assessment",
    },
    {
      currentSource: "No organization domain entity, DTO, route, or frontend surface was found.",
      href: null,
      id: "domains",
      implementationDependencies: "Requires Organization entity, verified domain table, DNS verification lifecycle, and audit events.",
      implementationRisk: "high",
      proposedDto: "OrganizationDomainsResponse(domains: OrganizationDomainDto[])",
      proposedEndpoint: "GET /api/v1/organizations/{organizationId}/domains",
      readiness: "missing-contract",
      recommendedPriority: "P3",
      requiredBackendContract: "Verified domains read model plus verification lifecycle and DNS challenge semantics.",
      requiredFrontendSurface: "Domains tab with verification state after contract exists.",
      securityNotes: "Domain ownership affects SSO and invites, so it needs explicit audit and conflict behavior.",
      status: "assessment",
    },
    {
      currentSource: "Workspace-scoped SCIM discovery, users/groups, and token surfaces exist; OIDC/SAML UI is currently disabled.",
      href: "#scim",
      id: "sso-scim-ownership",
      implementationDependencies: "Requires organization-owned identity configuration and token scope before org-wide SCIM is real.",
      implementationRisk: "high",
      proposedDto: "OrganizationIdentitySettingsDto(scim, sso, domains)",
      proposedEndpoint: "GET /api/v1/organizations/{organizationId}/identity",
      readiness: "partial",
      recommendedPriority: "P2",
      requiredBackendContract: "Organization-owned SSO/SCIM configuration with ownership, domain binding, and token scope.",
      requiredFrontendSurface: "Organization Integrations entry that can link to workspace SCIM until org ownership exists.",
      securityNotes: "SCIM token scope is currently workspace-bound; do not present it as organization-wide.",
      status: "assessment",
    },
    {
      currentSource: "Permission audit API exists with workspaceId query; no organization-wide audit stream was found.",
      href: null,
      id: "audit-log",
      implementationDependencies: "Requires organization audit projection and retention boundary.",
      implementationRisk: "high",
      proposedDto: "OrganizationAuditResponse(events: OrganizationAuditEventDto[], nextCursor)",
      proposedEndpoint: "GET /api/v1/organizations/{organizationId}/audit",
      readiness: "partial",
      recommendedPriority: "P3",
      requiredBackendContract: "Organization audit feed with actor, workspace, resource, and retention semantics.",
      requiredFrontendSurface: "Organization Audit Log read-only surface after cross-workspace feed exists.",
      securityNotes: "Audit visibility needs a dedicated organization permission, not workspace.view_audit reuse alone.",
      status: "assessment",
    },
    {
      currentSource: "Settings Plan tab is deferred; no billing or plan backend contract was found.",
      href: createSettingsHash({ tab: "plan" }),
      id: "billing-plan",
      implementationDependencies: "Requires billing source of truth and organization billing ownership model.",
      implementationRisk: "high",
      proposedDto: "OrganizationPlanDto(plan, billingOwner, limits, status)",
      proposedEndpoint: "GET /api/v1/organizations/{organizationId}/plan",
      readiness: "deferred",
      recommendedPriority: "P4",
      requiredBackendContract: "Plan, subscription, billing owner, and payment/provider boundary contract.",
      requiredFrontendSurface: "Plan summary only after billing source of truth is defined.",
      securityNotes: "Billing is outside workspace permissions and should not be inferred from member roles.",
      status: "deferred",
    },
    {
      currentSource: "No data retention policy entity, DTO, route, or frontend surface was found.",
      href: null,
      id: "data-retention",
      implementationDependencies: "Requires retention policy data model, legal-hold semantics, audit, and restore/export boundary.",
      implementationRisk: "high",
      proposedDto: "OrganizationRetentionPolicyDto(policy, legalHold, effectiveAt)",
      proposedEndpoint: "GET /api/v1/organizations/{organizationId}/retention-policy",
      readiness: "missing-contract",
      recommendedPriority: "P4",
      requiredBackendContract: "Retention policy read model plus explicit deletion/export/legal-hold semantics.",
      requiredFrontendSurface: "Read-only policy summary before any destructive retention controls.",
      securityNotes: "Retention can be destructive and must define audit, restore, and legal-hold behavior first.",
      status: "deferred",
    },
  ];
}

export function applyOrganizationReadApiStatus(
  rows: OrganizationAssessmentRow[],
  statuses: {
    members: OrganizationReadApiStatus;
    profile: OrganizationReadApiStatus;
  },
): OrganizationAssessmentRow[] {
  return rows.map((row) => {
    if (row.id === "organization-profile") {
      return applyLiveReadStatus(row, statuses.profile, {
        liveSource: "GET /api/v1/organizations/{organizationId}/profile is live-backed for the current organization.",
        unavailableSource: "Organization profile API did not return live data for the current session.",
      });
    }

    if (row.id === "global-members") {
      return applyLiveReadStatus(row, statuses.members, {
        liveSource: "GET /api/v1/organizations/{organizationId}/members is live-backed for the current organization.",
        unavailableSource: "Organization members API did not return live data for the current session.",
      });
    }

    if (row.id === "workspace-provisioning") {
      return applyWorkspaceInventoryStatus(row, statuses.profile);
    }

    return row;
  });
}

function applyWorkspaceInventoryStatus(
  row: OrganizationAssessmentRow,
  profileStatus: OrganizationReadApiStatus,
): OrganizationAssessmentRow {
  if (profileStatus === "ready") {
    return {
      ...row,
      currentSource: "Organization profile API returns live workspace inventory for visible workspaces.",
      readiness: "partial",
      status: "live",
      implementationDependencies: "Read-only workspace inventory is live-backed; create/archive/provisioning mutations remain deferred.",
    };
  }

  if (profileStatus === "loading") {
    return {
      ...row,
      currentSource: "Loading live organization workspace inventory status.",
      status: "assessment",
    };
  }

  return {
    ...row,
    currentSource: `Organization workspace inventory did not return live data for the current session. Status: ${profileStatus}.`,
    status: "unavailable",
  };
}

function applyLiveReadStatus(
  row: OrganizationAssessmentRow,
  apiStatus: OrganizationReadApiStatus,
  labels: { liveSource: string; unavailableSource: string },
): OrganizationAssessmentRow {
  if (apiStatus === "ready") {
    return {
      ...row,
      currentSource: labels.liveSource,
      readiness: "reusable",
      status: "live",
    };
  }

  if (apiStatus === "loading") {
    return {
      ...row,
      currentSource: "Loading live organization read API status.",
      status: "assessment",
    };
  }

  return {
    ...row,
    currentSource: `${labels.unavailableSource} Status: ${apiStatus}.`,
    status: "unavailable",
  };
}

export function getRecommendedOrganizationSettingsSlice(): OrganizationImplementationSlice {
  return {
    capabilityIds: ["organization-profile", "global-members"],
    reason:
      "Start with read-only Organization profile and global members discovery because it defines the ownership layer with low mutation risk and creates the base for domains, SSO, and audit later.",
    title: "Organization profile + global members read-only",
  };
}

export function toOrganizationOverviewModel(
  profile: OrganizationProfileResponse | null,
): OrganizationOverviewModel | null {
  if (!profile) {
    return null;
  }

  return {
    createdAt: profile.organization.createdAt,
    id: profile.organization.id,
    name: profile.organization.name,
    readRule: "Active member of any workspace in organization",
    slug: profile.organization.slug,
    status: profile.organization.status,
    updatedAt: profile.organization.updatedAt,
    visibleWorkspaceCount: profile.organization.workspaces.length,
  };
}

export function toOrganizationProfileEditCapability(
  profile: OrganizationProfileResponse | null,
  status: OrganizationReadApiStatus,
  mutationStatus: OrganizationProfileMutationStatus = "idle",
): OrganizationProfileEditCapability {
  if (status !== "ready" || !profile) {
    return {
      canEditOrganizationProfile: false,
      editDisabledReason: "Rename unavailable",
      mutationStatus,
    };
  }

  const roles = profile.organization.workspaces.map((workspace) => workspace.currentUserRole.toLowerCase());
  const canEdit = roles.includes("owner");
  return {
    canEditOrganizationProfile: canEdit,
    editDisabledReason: canEdit ? "" : "Owner required / insufficient permission",
    mutationStatus,
  };
}

export function prepareOrganizationProfileUpdateRequest(
  input: OrganizationProfileUpdateInput,
): OrganizationProfileUpdateRequestModel {
  const name = input.name.trim();
  const slug = normalizeOrganizationSlug(input.slug);
  const errors: OrganizationProfileUpdateRequestModel["errors"] = {};

  if (!name) {
    errors.name = "Organization name is required.";
  } else if (name.length > 120) {
    errors.name = "Organization name must be 120 characters or fewer.";
  }

  if (!slug) {
    errors.slug = "Organization slug is required.";
  } else if (slug.length > 80) {
    errors.slug = "Organization slug must be 80 characters or fewer.";
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
    request: { name, slug },
  };
}

export function toOrganizationReadSurfaceState(
  kind: OrganizationReadSurfaceKind,
  status: OrganizationReadApiStatus,
  hasLiveRows: boolean,
): OrganizationReadSurfaceState {
  if (status === "ready" && hasLiveRows) {
    return {
      detail: "Live-backed read-only data is available.",
      isEmpty: false,
      status: "live",
      title: "Live-backed",
    };
  }

  if (status === "ready") {
    const emptyTarget = kind === "overview" ? "profile" : kind;
    return {
      detail: `The live organization ${emptyTarget} API returned no rows for this session.`,
      isEmpty: true,
      status: "live",
      title: "No rows returned",
    };
  }

  if (status === "loading") {
    return {
      detail: "Loading the organization read API.",
      isEmpty: true,
      status: "assessment",
      title: "Loading",
    };
  }

  if (status === "unconfigured") {
    return {
      detail: "The API base URL or organization id is not configured for this session.",
      isEmpty: true,
      status: "unavailable",
      title: "Unconfigured",
    };
  }

  if (status === "forbidden") {
    return {
      detail: "The current user cannot read this organization surface.",
      isEmpty: true,
      status: "unavailable",
      title: "Forbidden",
    };
  }

  if (status === "not-found") {
    return {
      detail: "The requested organization was not found or is not visible to this user.",
      isEmpty: true,
      status: "unavailable",
      title: "Not found",
    };
  }

  return {
    detail: "The organization read API failed for this session.",
    isEmpty: true,
    status: "unavailable",
    title: "Error",
  };
}

export function getOrganizationWorkspaceProvisioningActions(): OrganizationDeferredAction[] {
  return [
    {
      label: "Create workspace",
      reason: "Organization workspace provisioning has no mutation contract in this slice.",
      status: "deferred",
    },
    {
      label: "Archive workspace",
      reason: "Workspace archive/delete semantics are deferred for organization scope.",
      status: "deferred",
    },
  ];
}

export function getOrganizationMemberManagementActions(): OrganizationDeferredAction[] {
  return [
    {
      label: "Invite member",
      reason: "Organization-level invite flow is not implemented in this read-only slice.",
      status: "deferred",
    },
    {
      label: "Remove member",
      reason: "Organization-level removal needs last-owner and workspace membership safety rules.",
      status: "deferred",
    },
    {
      label: "Change role",
      reason: "Organization-level role changes need a separate permission contract.",
      status: "deferred",
    },
  ];
}

export function toOrganizationWorkspaceInventoryRows(
  profile: OrganizationProfileResponse | null,
  currentWorkspaceId?: string | null,
): OrganizationWorkspaceInventoryRow[] {
  if (!profile) {
    return [];
  }

  return [...profile.organization.workspaces]
    .sort((left, right) => left.name.localeCompare(right.name))
    .map((workspace) => {
      const isCurrentWorkspace = workspace.id === currentWorkspaceId;
      return {
        createdAt: workspace.createdAt,
        currentSpaceId: workspace.currentSpaceId || "Unavailable",
        currentUserRole: workspace.currentUserRole,
        id: workspace.id,
        isCurrentWorkspace,
        name: workspace.name,
        settingsHref: isCurrentWorkspace ? createSettingsHash({ scope: "workspace", tab: "general" }) : null,
        slug: workspace.slug,
        switchStatus: isCurrentWorkspace ? "live" : "deferred",
        switchStatusReason: isCurrentWorkspace
          ? "Current workspace settings are available."
          : "Workspace switching is not supported by this frontend route yet.",
      };
    });
}

export function toOrganizationMemberInventoryRows(
  members: OrganizationMembersResponse | null,
): OrganizationMemberInventoryRow[] {
  if (!members) {
    return [];
  }

  return [...members.members]
    .sort((left, right) => left.displayName.localeCompare(right.displayName))
    .map((member) => ({
      displayName: member.displayName,
      email: member.email ?? "No email",
      id: member.userId,
      status: member.status,
      workspaceCount: member.workspaces.length,
      workspaces: [...member.workspaces]
        .sort((left, right) => left.workspaceName.localeCompare(right.workspaceName))
        .map((workspace) => ({
          joinedAt: workspace.joinedAt ?? "Unavailable",
          role: workspace.role,
          status: workspace.status,
          workspaceId: workspace.workspaceId,
          workspaceName: workspace.workspaceName,
        })),
    }));
}

export function toWorkspaceGeneralSettings(
  bootstrap: BootstrapResponse,
  requestedSpaceId: string | null,
): WorkspaceGeneralSettings {
  const activeSpaceId = getPreferredSpaceId(bootstrap, requestedSpaceId);
  const activeSpace = bootstrap.spaces.find((space) => space.id === activeSpaceId) ?? null;

  return {
    activeSpaceId,
    activeSpaceName: activeSpace?.name ?? "No library selected",
    spaces: bootstrap.spaces.map((space) => ({
      href: createLibrariesHash({ libraryId: space.id }),
      id: space.id,
      isCurrent: space.id === activeSpaceId,
      name: space.name,
    })),
    updateStatus: "deferred",
    workspaceId: bootstrap.workspace.id,
    workspaceName: bootstrap.workspace.name,
  };
}

export function toSpaceSettingsSummary(
  bootstrap: BootstrapResponse,
  map: KnowledgeMapResponse | null,
  requestedSpaceId: string | null,
): SpaceSettingsSummary | null {
  const spaceId = getPreferredSpaceId(bootstrap, requestedSpaceId);
  const space = bootstrap.spaces.find((item) => item.id === spaceId);
  if (!space) {
    return null;
  }

  return {
    collectionCount: map?.folders.length ?? 0,
    documentCount: map?.documents.length ?? 0,
    libraryHref: createLibrariesHash({ libraryId: space.id }),
    spaceId: space.id,
    spaceName: space.name,
    updateStatus: "deferred",
  };
}

export function toLibraryGeneralSettings(
  bootstrap: BootstrapResponse,
  map: KnowledgeMapResponse | null,
  requestedSpaceId: string | null,
): LibraryGeneralSettings | null {
  const spaceId = getPreferredSpaceId(bootstrap, requestedSpaceId);
  const space = bootstrap.spaces.find((item) => item.id === spaceId);
  if (!space) {
    return null;
  }

  return {
    collectionCount: map?.folders.length ?? 0,
    documentCount: map?.documents.length ?? 0,
    isCurrentLibrary: space.id === getPreferredSpaceId(bootstrap, null),
    libraryHref: createLibrariesHash({ libraryId: space.id }),
    spaceId: space.id,
    spaceName: space.name,
    updateStatus: "deferred",
    workspaceId: bootstrap.workspace.id,
    workspaceName: bootstrap.workspace.name,
  };
}

export function toLibrarySettingsCollectionRows(
  map: KnowledgeMapResponse | null,
  spaceId: string | null,
): LibrarySettingsCollectionRow[] {
  if (!map) {
    return [];
  }

  return [...map.folders]
    .sort((left, right) => left.sortOrder - right.sortOrder || left.title.localeCompare(right.title))
    .map((folder, index) => ({
      documentCount: folder.documentCount,
      href: createLibrariesHash({ collectionId: folder.id, libraryId: spaceId }),
      id: folder.id,
      position: index + 1,
      sortOrder: folder.sortOrder,
      title: folder.title,
    }));
}

export function toLibrarySettingsDocumentSummary(
  map: KnowledgeMapResponse | null,
  spaceId: string | null,
): LibrarySettingsDocumentSummary {
  if (!map) {
    return {
      archivedCount: 0,
      draftCount: 0,
      publishedCount: 0,
      recentDocuments: [],
      totalDocuments: 0,
    };
  }

  const folderTitles = new Map(map.folders.map((folder) => [folder.id, folder.title]));
  const documents = [...map.documents].sort((left, right) => Date.parse(right.updatedAt) - Date.parse(left.updatedAt));

  return {
    archivedCount: documents.filter((document) => document.status.toLowerCase() === "archived").length,
    draftCount: documents.filter((document) => document.status.toLowerCase() === "draft").length,
    publishedCount: documents.filter((document) => document.status.toLowerCase() === "published").length,
    recentDocuments: documents.slice(0, 8).map((document) => ({
      collectionHref: createLibrariesHash({ collectionId: document.folderId, libraryId: spaceId }),
      collectionTitle: folderTitles.get(document.folderId) ?? "Unassigned",
      href: createEditorHash(document.id),
      id: document.id,
      status: document.status,
      title: document.title,
      updatedAt: document.updatedAt,
    })),
    totalDocuments: documents.length,
  };
}

export function toNotificationSettingsModel(
  preferences: PermissionNotificationPreferenceDto[],
  preferenceStatus: NotificationApiStatus,
): NotificationSettingsModel {
  return {
    categoryPreferenceStatus: "deferred",
    emailDigestStatus: "deferred",
    preferenceStatus,
    resourceRows: preferenceStatus === "ready" ? toNotificationPreferenceResourceRows(preferences) : [],
  };
}

export function toLibraryNotificationPreferenceRows(
  preferences: PermissionNotificationPreferenceDto[],
  map: KnowledgeMapResponse | null,
  preferenceStatus: NotificationApiStatus,
  spaceId: string | null,
): LibraryNotificationPreferenceRow[] {
  if (preferenceStatus !== "ready" || !map) {
    return [];
  }

  const folderById = new Map(map.folders.map((folder) => [folder.id, folder]));
  const documentById = new Map(map.documents.map((document) => [document.id, document]));

  return preferences
    .flatMap((preference): LibraryNotificationPreferenceRow[] => {
      if (!preference.resourceId || !preference.resourceType) {
        return [];
      }

      const state = preference.muted ? "muted" : preference.watched ? "watched" : null;
      if (!state) {
        return [];
      }

      if (preference.resourceType === "document") {
        const document = documentById.get(preference.resourceId);
        return document
          ? [{
              href: createEditorHash(document.id),
              id: preference.id,
              label: document.title,
              resourceType: "document",
              state,
              updatedAt: preference.updatedAt,
            }]
          : [];
      }

      if (preference.resourceType === "collection") {
        const folder = folderById.get(preference.resourceId);
        return folder
          ? [{
              href: createLibrariesHash({ collectionId: folder.id, libraryId: spaceId }),
              id: preference.id,
              label: folder.title,
              resourceType: "collection",
              state,
              updatedAt: preference.updatedAt,
            }]
          : [];
      }

      return [];
    })
    .sort((left, right) => Date.parse(right.updatedAt) - Date.parse(left.updatedAt));
}

export function toSecuritySettingsRows(
  state: AuthSecurityStateResponse | null,
  status: "error" | "forbidden" | "loading" | "ready" | "unconfigured",
): SecuritySettingsRow[] {
  if (status !== "ready" || !state) {
    return [
      {
        label: "Security state",
        status: status === "loading" ? "unavailable" : "unavailable",
        value: getSecurityUnavailableLabel(status),
      },
    ];
  }

  return [
    {
      label: "Recent auth",
      status: "live",
      value: state.hasRecentAuth
        ? `Within ${state.recentAuthWindowMinutes} minutes`
        : `Required after ${state.recentAuthWindowMinutes} minutes`,
    },
    {
      label: "MFA",
      status: "live",
      value: state.mfaEnabled ? "Enabled" : "Not enabled",
    },
    {
      label: "MFA verification",
      status: "live",
      value: state.mfaVerified ? "Verified in this session" : "Not verified in this session",
    },
    {
      label: "High-risk actions",
      status: "live",
      value: state.stepUpRequiredForHighRiskActions ? "Step-up required" : "No step-up requirement",
    },
    {
      label: "Step-up methods",
      status: "live",
      value: state.stepUpMethods.length > 0 ? state.stepUpMethods.join(", ") : "No methods advertised",
    },
  ];
}

export function getSettingsStatusLabel(status: WorkspaceSettingsStatus) {
  if (status === "assessment") {
    return "Assessment";
  }

  if (status === "live") {
    return "Live-backed";
  }

  if (status === "reused") {
    return "Reuses existing surface";
  }

  if (status === "deferred") {
    return "Deferred";
  }

  if (status === "not-exposed") {
    return "Not exposed";
  }

  return "Unavailable";
}

function getPreferredSpaceId(bootstrap: BootstrapResponse, requestedSpaceId: string | null) {
  if (requestedSpaceId && bootstrap.spaces.some((space) => space.id === requestedSpaceId)) {
    return requestedSpaceId;
  }

  if (bootstrap.activeSpaceId && bootstrap.spaces.some((space) => space.id === bootstrap.activeSpaceId)) {
    return bootstrap.activeSpaceId;
  }

  if (bootstrap.workspace.currentSpaceId && bootstrap.spaces.some((space) => space.id === bootstrap.workspace.currentSpaceId)) {
    return bootstrap.workspace.currentSpaceId;
  }

  return bootstrap.spaces[0]?.id ?? null;
}

function normalizeOrganizationSlug(value: string) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-{2,}/g, "-");
}

function getSecurityUnavailableLabel(status: "error" | "forbidden" | "loading" | "ready" | "unconfigured") {
  if (status === "loading") {
    return "Loading security state";
  }

  if (status === "forbidden") {
    return "Security state forbidden";
  }

  if (status === "error") {
    return "Security API unavailable";
  }

  return "API not configured";
}
