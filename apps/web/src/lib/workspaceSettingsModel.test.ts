import { describe, expect, test } from "../test/harness";
import { ApiClientError } from "./apiClient";
import type { AuthSecurityStateResponse } from "./authClient";
import type {
  BootstrapResponse,
  KnowledgeMapResponse,
  OrganizationMembersResponse,
  OrganizationProfileResponse,
  PermissionNotificationPreferenceDto,
} from "./appApi";
import {
  applyOrganizationReadApiStatus,
  createOrganizationSettingsNavGroups,
  createPersonalSettingsNavGroups,
  createSettingsNavGroups,
  createWorkspaceSettingsTabRows,
  getOrganizationMemberManagementActions,
  getOrganizationWorkspaceProvisioningActions,
  prepareOrganizationProfileUpdateRequest,
  prepareWorkspaceNotificationPreferenceRequest,
  getRecommendedOrganizationSettingsSlice,
  getRecommendedSettingsClosureSlice,
  normalizeSettingsPanel,
  toOrganizationSettingsAssessmentRows,
  toSettingsCapabilityInventoryRows,
  toOrganizationMemberInventoryRows,
  toOrganizationOverviewModel,
  toOrganizationProfileEditCapability,
  toOrganizationReadSurfaceState,
  toOrganizationWorkspaceInventoryRows,
  toWorkspaceNotificationPreferenceModel,
  toWorkspaceNotificationPreferenceMutationError,
  toSettingsBoundaryRows,
  toLibraryGeneralSettings,
  toLibraryNotificationPreferenceRows,
  toLibrarySettingsCollectionRows,
  toLibrarySettingsDocumentSummary,
  toNotificationSettingsModel,
  toSecuritySettingsRows,
  toSpaceSettingsSummary,
  toWorkspaceGeneralSettings,
} from "./workspaceSettingsModel";

const workspaceId = "11111111-1111-4111-8111-111111111111";
const organizationId = "99999999-9999-4999-8999-999999999999";
const firstSpaceId = "22222222-2222-4222-8222-222222222222";
const secondSpaceId = "33333333-3333-4333-8333-333333333333";
const collectionId = "44444444-4444-4444-8444-444444444444";
const secondCollectionId = "66666666-6666-4666-8666-666666666666";
const documentId = "55555555-5555-4555-8555-555555555555";
const secondDocumentId = "77777777-7777-4777-8777-777777777777";
const secondWorkspaceId = "88888888-8888-4888-8888-888888888888";

const bootstrap: BootstrapResponse = {
  activeDocumentId: documentId,
  activeSpaceId: firstSpaceId,
  documents: [],
  folders: [],
  spaces: [
    { id: firstSpaceId, name: "Foundations" },
    { id: secondSpaceId, name: "Strategy" },
  ],
  workspace: {
    currentSpaceId: firstSpaceId,
    id: workspaceId,
    name: "Northstar",
    organizationId,
  },
};

const map: KnowledgeMapResponse = {
  documents: [
    {
      folderId: collectionId,
      id: documentId,
      sortOrder: 0,
      status: "Draft",
      tags: [],
      title: "Q2 Roadmap",
      updatedAt: "2026-05-01T00:00:00Z",
    },
    {
      folderId: secondCollectionId,
      id: secondDocumentId,
      sortOrder: 0,
      status: "Published",
      tags: [],
      title: "Decision log",
      updatedAt: "2026-05-03T00:00:00Z",
    },
  ],
  folders: [
    {
      documentCount: 1,
      id: collectionId,
      sortOrder: 0,
      title: "Roadmaps",
    },
    {
      documentCount: 1,
      id: secondCollectionId,
      sortOrder: -1,
      title: "Decisions",
    },
  ],
};

const organizationProfile: OrganizationProfileResponse = {
  organization: {
    createdAt: "2026-05-01T00:00:00Z",
    id: organizationId,
    name: "Northstar",
    slug: "northstar",
    status: "active",
    updatedAt: "2026-05-02T00:00:00Z",
    workspaces: [
      {
        createdAt: "2026-05-01T00:00:00Z",
        currentSpaceId: firstSpaceId,
        currentUserRole: "owner",
        id: workspaceId,
        name: "Northstar",
        slug: "northstar",
      },
      {
        createdAt: "2026-05-03T00:00:00Z",
        currentSpaceId: "",
        currentUserRole: "viewer",
        id: secondWorkspaceId,
        name: "Research",
        slug: "research",
      },
    ],
  },
};

const organizationMembers: OrganizationMembersResponse = {
  members: [
    {
      displayName: "Avery Chen",
      email: "avery@example.com",
      status: "active",
      userId: "user-1",
      workspaces: [
        {
          joinedAt: "2026-05-04T00:00:00Z",
          role: "viewer",
          status: "active",
          workspaceId: secondWorkspaceId,
          workspaceName: "Research",
        },
        {
          joinedAt: "2026-05-01T00:00:00Z",
          role: "owner",
          status: "active",
          workspaceId,
          workspaceName: "Northstar",
        },
      ],
    },
  ],
};

describe("workspaceSettingsModel", () => {
  test("models PC Settings secondary navigation without Current Library as a top-level scope", () => {
    const groups = createSettingsNavGroups();

    expect(groups.map((group) => group.id)).toEqual(["workspace"]);
    expect(groups.find((group) => group.id === "workspace")?.items.map((item) => item.id)).toEqual([
      "workspace-general",
      "workspace-notifications",
      "workspace-permissions",
      "workspace-security",
      "workspace-integrations",
    ]);
    const panelIds = groups.flatMap((group) => group.items.map((item) => item.id as string));
    expect(panelIds.includes("library-general")).toBe(false);
    expect(panelIds.includes("personal-preferences")).toBe(false);
    expect(panelIds.includes("organization-profile")).toBe(false);
    expect(panelIds.includes("deferred-plan")).toBe(false);
    expect(panelIds.includes("deferred-developer")).toBe(false);

    expect(createPersonalSettingsNavGroups().map((group) => group.id)).toEqual(["personal"]);
    expect(createOrganizationSettingsNavGroups().find((group) => group.id === "organization")?.items.map((item) => item.id)).toEqual([
      "organization-profile",
      "organization-workspaces",
      "organization-members",
    ]);
  });

  test("normalizes legacy Settings hashes into the PC Settings IA panels", () => {
    expect(normalizeSettingsPanel({ scope: "workspace", tab: "general" })).toEqual({
      id: "workspace-general",
      section: "workspace",
    });
    expect(normalizeSettingsPanel({ scope: "library", tab: "collections" })).toEqual({
      id: "workspace-general",
      section: "workspace",
    });
    expect(normalizeSettingsPanel({ scope: "workspace", tab: "members" })).toEqual({
      id: "workspace-general",
      section: "workspace",
    });
    expect(normalizeSettingsPanel({ scope: "workspace", tab: "permissions" })).toEqual({
      id: "workspace-permissions",
      section: "workspace",
    });
    expect(normalizeSettingsPanel({ panel: "workspace-access-identity", scope: "workspace", tab: "general" })).toEqual({
      id: "workspace-permissions",
      section: "workspace",
    });
    expect(normalizeSettingsPanel({ panel: "deferred-plan", scope: "workspace", tab: "general" })).toEqual({
      id: "workspace-general",
      section: "workspace",
    });
    expect(normalizeSettingsPanel({ scope: "workspace", tab: "developer" })).toEqual({
      id: "workspace-general",
      section: "workspace",
    });
    expect(normalizeSettingsPanel({ scope: "organization", tab: "overview" })).toEqual({
      id: "workspace-general",
      section: "workspace",
    });
    expect(normalizeSettingsPanel({ panel: "personal-preferences", scope: "workspace", tab: "general" })).toEqual({
      id: "workspace-general",
      section: "workspace",
    });
  });

  test("keeps workspace Settings primary tabs focused on management surfaces", () => {
    const rows = createWorkspaceSettingsTabRows("general");
    const legacyHrefTargets = new Set([
      "#workspace-members",
      "#permission-admin",
      "#workspace-groups",
      "#scim",
    ]);

    expect(rows.map((row) => row.id)).toEqual([
      "general",
      "notifications",
      "permissions",
      "security",
      "integrations",
    ]);
    expect(rows.find((row) => row.id === "permissions")?.href).toBe("#settings?scope=workspace&tab=permissions");
    expect(rows.find((row) => row.id === "integrations")?.href).toBe("#settings?scope=workspace&tab=integrations");
    expect(rows.map((row) => row.href).some((href) => legacyHrefTargets.has(href))).toBe(false);
    expect(rows.some((row) => row.id === "plan")).toBe(false);
    expect(rows.some((row) => row.id === "developer")).toBe(false);
  });

  test("creates library-scope settings tabs with library hashes", () => {
    const rows = createWorkspaceSettingsTabRows("collections", { scope: "library", spaceId: firstSpaceId });

    expect(rows.map((row) => row.id)).toEqual([
      "general",
      "collections",
      "documents",
      "permissions",
      "notifications",
      "advanced",
    ]);
    expect(rows.find((row) => row.id === "permissions")?.status).toBe("reused");
    expect(rows.find((row) => row.id === "advanced")?.disabled).toBe(true);
    expect(rows.find((row) => row.id === "collections")?.href).toBe(
      `#settings?scope=library&tab=collections&spaceId=${firstSpaceId}`,
    );
  });

  test("creates organization-scope read surface tabs without enabling management", () => {
    const rows = createWorkspaceSettingsTabRows("overview", { scope: "organization" });

    expect(rows.map((row) => [row.id, row.status, row.disabled, row.href])).toEqual([
      ["overview", "live", false, "#settings?scope=organization&tab=overview"],
      ["workspaces", "live", false, "#settings?scope=organization&tab=workspaces"],
      ["members", "live", false, "#settings?scope=organization&tab=members"],
      ["assessment", "assessment", false, "#settings?scope=organization&tab=assessment"],
    ]);
  });

  test("models settings boundaries without exposing system settings", () => {
    const rows = toSettingsBoundaryRows(firstSpaceId);

    expect(rows).toEqual([
      {
        href: "#settings",
        id: "workspace",
        status: "live",
      },
      {
        href: `#libraries?libraryId=${firstSpaceId}`,
        id: "library",
        status: "live",
      },
      {
        href: "#organization-settings?panel=profile",
        id: "organization",
        status: "assessment",
      },
      {
        href: null,
        id: "system",
        status: "not-exposed",
      },
    ]);
  });

  test("classifies Settings capability ownership before enabling actions", () => {
    const rows = toSettingsCapabilityInventoryRows();
    const byId = new Map(rows.map((row) => [row.id, row]));

    expect(byId.get("workspace-notification-preferences")).toMatchObject({
      backendStatus: "live-mutation",
      frontendStatus: "live",
      recommendation: "keep",
      scope: "workspace",
    });
    expect(byId.get("workspace-members")).toMatchObject({
      backendStatus: "live-mutation",
      frontendStatus: "should-move",
      recommendation: "move",
      scope: "workspace",
    });
    expect(byId.get("resource-share")).toMatchObject({
      recommendation: "move",
      scope: "resource",
    });
    expect(byId.get("system-instance-settings")).toMatchObject({
      backendStatus: "missing",
      recommendation: "remove-action-affordance",
    });
  });

  test("recommends final trust pass after workspace members move out of Settings", () => {
    const slice = getRecommendedSettingsClosureSlice();

    expect(slice.title).toBe("Settings final trust pass");
    expect(slice.capabilityIds).toEqual([
      "workspace-profile-update",
      "resource-share",
      "library-collections-documents",
    ]);
    expect(slice.reason).toContain("Workspace members now live in the Members left-nav surface");
    expect(slice.reason).toContain("task surfaces");
  });

  test("models workspace notification preference update request and state", () => {
    const preference: PermissionNotificationPreferenceDto = {
      createdAt: "2026-05-01T00:00:00Z",
      id: "preference-1",
      muted: true,
      resourceId: null,
      resourceType: null,
      updatedAt: "2026-05-02T00:00:00Z",
      userId: "user-1",
      watched: false,
      workspaceId,
    };

    expect(toWorkspaceNotificationPreferenceModel([preference], "ready")).toMatchObject({
      canUpdate: true,
      mode: "muted",
      mutationStatus: "idle",
      updatedAt: "2026-05-02T00:00:00Z",
    });
    expect(toWorkspaceNotificationPreferenceModel([], "forbidden")).toMatchObject({
      canUpdate: false,
      mode: "default",
    });
    expect(prepareWorkspaceNotificationPreferenceRequest(` ${workspaceId} `, "watched")).toEqual({
      muted: false,
      resourceId: null,
      resourceType: null,
      watched: true,
      workspaceId,
    });
    expect(prepareWorkspaceNotificationPreferenceRequest("", "muted")).toBe(null);
    expect(toWorkspaceNotificationPreferenceMutationError(new Error("Backend validation failed."))).toBe(
      "Backend validation failed.",
    );
    expect(
      toWorkspaceNotificationPreferenceMutationError(
        new ApiClientError(0, "Could not reach API endpoint https://northstar.test/api/v1/notifications/preferences. Failed to fetch"),
      ),
    ).toBe("Could not reach the notification preference API. Check the backend session and retry.");
  });

  test("models organization contract readiness without enabling management", () => {
    const rows = toOrganizationSettingsAssessmentRows();
    const byId = new Map(rows.map((row) => [row.id, row]));

    expect(rows.map((row) => row.id)).toEqual([
      "organization-profile",
      "global-members",
      "workspace-provisioning",
      "domains",
      "sso-scim-ownership",
      "audit-log",
      "billing-plan",
      "data-retention",
    ]);
    expect(rows.every((row) => row.status === "assessment" || row.status === "deferred")).toBe(true);
    expect(rows.every((row) => row.requiredBackendContract && row.requiredFrontendSurface && row.securityNotes)).toBe(true);
    expect(rows.every((row) => row.proposedEndpoint && row.proposedDto && row.implementationDependencies)).toBe(true);
    expect(byId.get("organization-profile")?.readiness).toBe("missing-contract");
    expect(byId.get("organization-profile")?.proposedEndpoint).toBe("GET /api/v1/organizations/{organizationId}/profile");
    expect(byId.get("organization-profile")?.proposedDto).toContain("OrganizationProfileDto");
    expect(byId.get("organization-profile")?.implementationRisk).toBe("medium");
    expect(byId.get("global-members")?.readiness).toBe("partial");
    expect(byId.get("global-members")?.href).toBe("#members");
    expect(byId.get("global-members")?.proposedEndpoint).toBe("GET /api/v1/organizations/{organizationId}/members");
    expect(byId.get("global-members")?.proposedDto).toContain("OrganizationMembersResponse");
    expect(byId.get("global-members")?.implementationDependencies).toContain("organization-level read authorization");
    expect(byId.get("sso-scim-ownership")?.readiness).toBe("partial");
    expect(byId.get("sso-scim-ownership")?.href).toBe("#settings?scope=workspace&tab=integrations");
    expect(byId.get("audit-log")?.readiness).toBe("partial");
    expect(byId.get("billing-plan")?.readiness).toBe("deferred");
    expect(byId.get("data-retention")?.readiness).toBe("missing-contract");
  });

  test("marks organization profile and global members live-backed only when APIs are ready", () => {
    const rows = applyOrganizationReadApiStatus(toOrganizationSettingsAssessmentRows(), {
      members: "ready",
      profile: "ready",
    });
    const byId = new Map(rows.map((row) => [row.id, row]));

    expect(byId.get("organization-profile")?.status).toBe("live");
    expect(byId.get("organization-profile")?.readiness).toBe("reusable");
    expect(byId.get("organization-profile")?.currentSource).toContain("/profile");
    expect(byId.get("global-members")?.status).toBe("live");
    expect(byId.get("global-members")?.readiness).toBe("reusable");
    expect(byId.get("workspace-provisioning")?.status).toBe("live");
    expect(byId.get("workspace-provisioning")?.readiness).toBe("partial");
    expect(byId.get("workspace-provisioning")?.implementationDependencies).toContain("mutations remain deferred");
    expect(byId.get("billing-plan")?.status).toBe("deferred");
  });

  test("keeps organization API failures honest instead of falling back to demo success", () => {
    const rows = applyOrganizationReadApiStatus(toOrganizationSettingsAssessmentRows(), {
      members: "forbidden",
      profile: "error",
    });
    const byId = new Map(rows.map((row) => [row.id, row]));

    expect(byId.get("organization-profile")?.status).toBe("unavailable");
    expect(byId.get("organization-profile")?.currentSource).toContain("Status: error");
    expect(byId.get("global-members")?.status).toBe("unavailable");
    expect(byId.get("global-members")?.currentSource).toContain("Status: forbidden");
    expect(byId.get("workspace-provisioning")?.status).toBe("unavailable");
    expect(byId.get("workspace-provisioning")?.currentSource).toContain("Status: error");
  });

  test("maps organization profile into overview read-only rows", () => {
    const model = toOrganizationOverviewModel(organizationProfile);

    expect(model).toEqual({
      createdAt: "2026-05-01T00:00:00Z",
      id: organizationId,
      name: "Northstar",
      readRule: "Active member of any workspace in organization",
      slug: "northstar",
      status: "active",
      updatedAt: "2026-05-02T00:00:00Z",
      visibleWorkspaceCount: 2,
    });
  });

  test("models organization profile edit capability as owner-only", () => {
    expect(toOrganizationProfileEditCapability(organizationProfile, "ready")).toEqual({
      canEditOrganizationProfile: true,
      editDisabledReason: "",
      mutationStatus: "idle",
    });

    for (const role of ["viewer", "editor", "admin"]) {
      const profile: OrganizationProfileResponse = {
        organization: {
          ...organizationProfile.organization,
          workspaces: [
            {
              ...organizationProfile.organization.workspaces[0],
              currentUserRole: role,
            },
          ],
        },
      };

      const capability = toOrganizationProfileEditCapability(profile, "ready", "error");
      expect(capability.canEditOrganizationProfile).toBe(false);
      expect(capability.editDisabledReason).toBe("Owner required / insufficient permission");
      expect(capability.mutationStatus).toBe("error");
    }

    const unknownProfile: OrganizationProfileResponse = {
      organization: {
        ...organizationProfile.organization,
        workspaces: [
          {
            ...organizationProfile.organization.workspaces[0],
            currentUserRole: "unknown",
          },
        ],
      },
    };

    expect(toOrganizationProfileEditCapability(unknownProfile, "ready").canEditOrganizationProfile).toBe(false);
    expect(toOrganizationProfileEditCapability(null, "loading").editDisabledReason).toBe("Rename unavailable");
  });

  test("prepares organization profile update requests with trim, slug normalization, and validation", () => {
    expect(prepareOrganizationProfileUpdateRequest({
      name: "  Northstar Atlas  ",
      slug: " Northstar Atlas ",
    })).toEqual({
      errors: {},
      isValid: true,
      request: {
        name: "Northstar Atlas",
        slug: "northstar-atlas",
      },
    });

    const invalid = prepareOrganizationProfileUpdateRequest({ name: "   ", slug: " ### " });
    expect(invalid.isValid).toBe(false);
    expect(invalid.errors.name).toBe("Organization name is required.");
    expect(invalid.errors.slug).toBe("Organization slug is required.");
  });

  test("models organization read surface states without demo fallback", () => {
    expect(toOrganizationReadSurfaceState("overview", "ready", true)).toEqual({
      detail: "Live-backed read-only data is available.",
      isEmpty: false,
      status: "live",
      title: "Live-backed",
    });
    expect(toOrganizationReadSurfaceState("overview", "ready", false)).toEqual({
      detail: "The live organization profile API returned no rows for this session.",
      isEmpty: true,
      status: "live",
      title: "No rows returned",
    });
    expect(toOrganizationReadSurfaceState("members", "loading", false).title).toBe("Loading");
    expect(toOrganizationReadSurfaceState("members", "unconfigured", false).title).toBe("Unconfigured");
    expect(toOrganizationReadSurfaceState("members", "forbidden", false).title).toBe("Forbidden");
    expect(toOrganizationReadSurfaceState("members", "not-found", false).title).toBe("Not found");
    expect(toOrganizationReadSurfaceState("members", "error", false).title).toBe("Error");
    expect(toOrganizationReadSurfaceState("members", "forbidden", false).status).toBe("unavailable");
  });

  test("maps organization workspace inventory and defers non-current workspace switching", () => {
    const rows = toOrganizationWorkspaceInventoryRows(organizationProfile, workspaceId);

    expect(rows.map((row) => [row.name, row.switchStatus, row.settingsHref])).toEqual([
      ["Northstar", "live", "#settings"],
      ["Research", "deferred", null],
    ]);
    expect(rows[0]?.isCurrentWorkspace).toBe(true);
    expect(rows[1]?.currentSpaceId).toBe("Unavailable");
    expect(rows[1]?.isCurrentWorkspace).toBe(false);
    expect(rows[1]?.switchStatusReason).toContain("not supported");
  });

  test("models empty organization workspace inventory from a ready API honestly", () => {
    const emptyProfile: OrganizationProfileResponse = {
      organization: {
        ...organizationProfile.organization,
        workspaces: [],
      },
    };

    expect(toOrganizationWorkspaceInventoryRows(emptyProfile, workspaceId)).toEqual([]);
    expect(toOrganizationReadSurfaceState("workspaces", "ready", false)).toEqual({
      detail: "The live organization workspaces API returned no rows for this session.",
      isEmpty: true,
      status: "live",
      title: "No rows returned",
    });
  });

  test("maps organization members as read-only aggregated member rows", () => {
    const rows = toOrganizationMemberInventoryRows(organizationMembers);

    expect(rows.length).toBe(1);
    expect(rows[0]?.displayName).toBe("Avery Chen");
    expect(rows[0]?.workspaceCount).toBe(2);
    expect(rows[0]?.workspaces.map((workspace) => `${workspace.workspaceName}:${workspace.role}:${workspace.status}:${workspace.joinedAt}`)).toEqual([
      "Northstar:owner:active:2026-05-01T00:00:00Z",
      "Research:viewer:active:2026-05-04T00:00:00Z",
    ]);
  });

  test("models empty organization members and deferred management actions", () => {
    expect(toOrganizationMemberInventoryRows({ members: [] })).toEqual([]);
    expect(toOrganizationReadSurfaceState("members", "ready", false)).toEqual({
      detail: "The live organization members API returned no rows for this session.",
      isEmpty: true,
      status: "live",
      title: "No rows returned",
    });
    expect(getOrganizationMemberManagementActions().map((action) => [action.label, action.status])).toEqual([
      ["Invite member", "deferred"],
      ["Remove member", "deferred"],
      ["Change role", "deferred"],
    ]);
    expect(getOrganizationWorkspaceProvisioningActions().map((action) => [action.label, action.status])).toEqual([
      ["Create workspace", "deferred"],
      ["Archive workspace", "deferred"],
    ]);
  });

  test("keeps live-backed assessment items ahead of deferred mutation capabilities", () => {
    const rows = applyOrganizationReadApiStatus(toOrganizationSettingsAssessmentRows(), {
      members: "ready",
      profile: "ready",
    });

    expect(rows.slice(0, 3).map((row) => [row.id, row.status, row.readiness])).toEqual([
      ["organization-profile", "live", "reusable"],
      ["global-members", "live", "reusable"],
      ["workspace-provisioning", "live", "partial"],
    ]);
    expect(rows.find((row) => row.id === "billing-plan")?.status).toBe("deferred");
    expect(rows.find((row) => row.id === "data-retention")?.status).toBe("deferred");
  });

  test("recommends organization profile and global members as the first implementation slice", () => {
    const slice = getRecommendedOrganizationSettingsSlice();

    expect(slice.capabilityIds).toEqual(["organization-profile", "global-members"]);
    expect(slice.title).toBe("Organization profile + global members read-only");
    expect(slice.reason).toContain("read-only");
  });

  test("maps workspace bootstrap data into read-only General settings", () => {
    const model = toWorkspaceGeneralSettings(bootstrap, secondSpaceId);

    expect(model.workspaceName).toBe("Northstar");
    expect(model.workspaceId).toBe(workspaceId);
    expect(model.activeSpaceName).toBe("Strategy");
    expect(model.updateStatus).toBe("deferred");
    expect(model.spaces).toEqual([
      {
        href: `#libraries?libraryId=${firstSpaceId}`,
        id: firstSpaceId,
        isCurrent: false,
        name: "Foundations",
      },
      {
        href: `#libraries?libraryId=${secondSpaceId}`,
        id: secondSpaceId,
        isCurrent: true,
        name: "Strategy",
      },
    ]);
  });

  test("creates a live-backed lightweight space settings summary", () => {
    const summary = toSpaceSettingsSummary(bootstrap, map, firstSpaceId);

    expect(summary).toEqual({
      collectionCount: 2,
      documentCount: 2,
      libraryHref: `#libraries?libraryId=${firstSpaceId}`,
      spaceId: firstSpaceId,
      spaceName: "Foundations",
      updateStatus: "deferred",
    });
  });

  test("maps library general settings from bootstrap and map data", () => {
    const model = toLibraryGeneralSettings(bootstrap, map, firstSpaceId);

    expect(model).toEqual({
      collectionCount: 2,
      documentCount: 2,
      isCurrentLibrary: true,
      libraryHref: `#libraries?libraryId=${firstSpaceId}`,
      spaceId: firstSpaceId,
      spaceName: "Foundations",
      updateStatus: "deferred",
      workspaceId,
      workspaceName: "Northstar",
    });
  });

  test("orders library collection rows by sort order", () => {
    const rows = toLibrarySettingsCollectionRows(map, firstSpaceId);

    expect(rows.map((row) => [row.title, row.position, row.href])).toEqual([
      ["Decisions", 1, `#libraries?libraryId=${firstSpaceId}&collectionId=${secondCollectionId}`],
      ["Roadmaps", 2, `#libraries?libraryId=${firstSpaceId}&collectionId=${collectionId}`],
    ]);
  });

  test("summarizes library documents and sorts recent documents", () => {
    const summary = toLibrarySettingsDocumentSummary(map, firstSpaceId);

    expect(summary.totalDocuments).toBe(2);
    expect(summary.draftCount).toBe(1);
    expect(summary.publishedCount).toBe(1);
    expect(summary.recentDocuments.map((document) => document.title)).toEqual(["Decision log", "Q2 Roadmap"]);
    expect(summary.recentDocuments[0]?.collectionHref).toBe(
      `#libraries?libraryId=${firstSpaceId}&collectionId=${secondCollectionId}`,
    );
  });

  test("keeps notification category and email settings deferred while resource preferences are live", () => {
    const preferences: PermissionNotificationPreferenceDto[] = [
      {
        createdAt: "2026-05-01T00:00:00Z",
        id: "pref-1",
        muted: false,
        resourceId: documentId,
        resourceType: "document",
        updatedAt: "2026-05-02T00:00:00Z",
        userId: "user-1",
        watched: true,
        workspaceId,
      },
    ];
    const model = toNotificationSettingsModel(preferences, "ready");

    expect(model.categoryPreferenceStatus).toBe("deferred");
    expect(model.emailDigestStatus).toBe("deferred");
    expect(model.resourceRows[0]?.href).toBe(`#editor?documentId=${documentId}`);
  });

  test("filters notification preferences to resources in the selected library map", () => {
    const preferences: PermissionNotificationPreferenceDto[] = [
      {
        createdAt: "2026-05-01T00:00:00Z",
        id: "pref-1",
        muted: false,
        resourceId: documentId,
        resourceType: "document",
        updatedAt: "2026-05-02T00:00:00Z",
        userId: "user-1",
        watched: true,
        workspaceId,
      },
      {
        createdAt: "2026-05-01T00:00:00Z",
        id: "pref-2",
        muted: true,
        resourceId: secondCollectionId,
        resourceType: "collection",
        updatedAt: "2026-05-04T00:00:00Z",
        userId: "user-1",
        watched: false,
        workspaceId,
      },
      {
        createdAt: "2026-05-01T00:00:00Z",
        id: "pref-3",
        muted: true,
        resourceId: "99999999-9999-4999-8999-999999999999",
        resourceType: "document",
        updatedAt: "2026-05-05T00:00:00Z",
        userId: "user-1",
        watched: false,
        workspaceId,
      },
    ];

    const rows = toLibraryNotificationPreferenceRows(preferences, map, "ready", firstSpaceId);

    expect(rows.map((row) => [row.label, row.state, row.href])).toEqual([
      ["Decisions", "muted", `#libraries?libraryId=${firstSpaceId}&collectionId=${secondCollectionId}`],
      ["Q2 Roadmap", "watched", `#editor?documentId=${documentId}`],
    ]);
  });

  test("maps security state without introducing new MFA flows", () => {
    const securityState: AuthSecurityStateResponse = {
      hasRecentAuth: true,
      mfaEnabled: true,
      mfaVerified: false,
      mfaVerifiedAt: null,
      recentAuthAt: "2026-05-01T00:00:00Z",
      recentAuthWindowMinutes: 15,
      stepUpMethods: ["totp"],
      stepUpRequiredForHighRiskActions: true,
      userId: "user-1",
    };

    const rows = toSecuritySettingsRows(securityState, "ready");

    expect(rows.map((row) => row.label)).toEqual([
      "Recent auth",
      "MFA",
      "MFA verification",
      "High-risk actions",
      "Step-up methods",
    ]);
    expect(rows[2]?.value).toBe("Not verified in this session");
  });
});
