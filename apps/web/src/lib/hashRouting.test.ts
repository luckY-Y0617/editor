import { describe, expect, test } from "../test/harness";
import {
  createEditorHash,
  createDocumentAdvancedPermissionsHash,
  createLibrariesHash,
  createOrganizationSettingsHash,
  createPersonalSettingsHash,
  createSearchHash,
  createSettingsHash,
  createShareHash,
  createWorkspaceIntegrationsHash,
  createWorkspaceMembersHash,
  createWorkspacePermissionsHash,
  createWorkspaceUpdatesHash,
  getEditorDocumentIdFromHash,
  getHashRoute,
  getCanonicalHashRedirect,
  getLibrariesFiltersFromHash,
  getOrganizationSettingsPanelFromHash,
  getPostLoginRedirectHash,
  getSearchFiltersFromHash,
  getSettingsFiltersFromHash,
  getSettingsRouteTarget,
  getShareDocumentIdFromHash,
  normalizeInternalActionHash,
  parseHashRoute,
} from "./hashRouting";

const documentId = "11111111-1111-4111-8111-111111111111";
const libraryId = "22222222-2222-4222-8222-222222222222";
const collectionId = "33333333-3333-4333-8333-333333333333";

describe("hashRouting", () => {
  test("parses a hash route and query params", () => {
    const route = parseHashRoute(`#editor?documentId=${documentId}&panel=outline`);

    expect(route.route).toBe("#editor");
    expect(route.params.get("documentId")).toBe(documentId);
    expect(route.params.get("panel")).toBe("outline");
  });

  test("uses the route base for protected route checks", () => {
    expect(getHashRoute(`#editor?documentId=${documentId}`)).toBe("#editor");
  });

  test("returns only valid editor document ids", () => {
    expect(getEditorDocumentIdFromHash(`#editor?documentId=${documentId}`)).toBe(documentId);
    expect(getEditorDocumentIdFromHash("#editor?documentId=not-a-uuid")).toBe(null);
    expect(getEditorDocumentIdFromHash(`#home?documentId=${documentId}`)).toBe(null);
  });

  test("creates editor document hashes only for UUID document ids", () => {
    expect(createEditorHash(documentId)).toBe(`#editor?documentId=${documentId}`);
    expect(createEditorHash("doc-demo")).toBe("#editor");
    expect(createEditorHash(null)).toBe("#editor");
  });

  test("creates and parses share document hashes only for UUID document ids", () => {
    expect(createShareHash(documentId)).toBe(`#share?documentId=${documentId}`);
    expect(createDocumentAdvancedPermissionsHash(documentId)).toBe(`#share?documentId=${documentId}`);
    expect(createShareHash("doc-demo")).toBe("#share");
    expect(createShareHash(null)).toBe("#share");
    expect(getShareDocumentIdFromHash(`#share?documentId=${documentId}`)).toBe(documentId);
    expect(getShareDocumentIdFromHash(`#permissions?documentId=${documentId}`)).toBe(documentId);
    expect(getShareDocumentIdFromHash("#share?documentId=not-a-uuid")).toBe(null);
  });

  test("creates and parses folder search hashes", () => {
    const hash = createSearchHash({ folderId: documentId, folderTitle: "Foundations", libraryId, q: "mission" });

    expect(hash).toBe(`#search?libraryId=${libraryId}&folderId=${documentId}&folderTitle=Foundations&q=mission`);
    expect(getSearchFiltersFromHash(hash)).toEqual({
      folderId: documentId,
      folderTitle: "Foundations",
      libraryId,
      q: "mission",
    });
    expect(getSearchFiltersFromHash("#search?folderId=not-a-uuid&folderTitle=Drafts")).toEqual({
      folderId: null,
      folderTitle: "Drafts",
      libraryId: null,
      q: null,
    });
  });

  test("creates and parses libraries hashes", () => {
    const hash = createLibrariesHash({ collectionId, libraryId });

    expect(hash).toBe(`#libraries?libraryId=${libraryId}&collectionId=${collectionId}`);
    expect(getLibrariesFiltersFromHash(hash)).toEqual({
      collectionId,
      libraryId,
    });
    expect(getLibrariesFiltersFromHash("#libraries?libraryId=invalid&collectionId=also-invalid")).toEqual({
      collectionId: null,
      libraryId: null,
    });
    expect(createLibrariesHash({ collectionId: "invalid", libraryId: "invalid" })).toBe("#libraries");
  });

  test("creates and parses workspace settings hashes", () => {
    expect(createSettingsHash()).toBe("#settings");
    expect(createWorkspaceMembersHash()).toBe("#settings?scope=workspace&tab=members");
    expect(createWorkspacePermissionsHash()).toBe("#settings?scope=workspace&tab=permissions");
    expect(createWorkspaceIntegrationsHash()).toBe("#settings?scope=workspace&tab=integrations");
    expect(createWorkspaceUpdatesHash()).toBe("#updates");
    expect(createWorkspaceUpdatesHash({ tab: "access" })).toBe("#updates?tab=access");
    expect(createSettingsHash({ tab: "notifications" })).toBe("#settings?tab=notifications");
    expect(createSettingsHash({ spaceId: libraryId, tab: "general" })).toBe(`#settings?spaceId=${libraryId}`);
    expect(createSettingsHash({ spaceId: libraryId, tab: "security" })).toBe(`#settings?tab=security&spaceId=${libraryId}`);
    expect(createSettingsHash({ scope: "workspace", tab: "general" })).toBe("#settings?scope=workspace");
    expect(createSettingsHash({ scope: "workspace", tab: "developer" })).toBe("#settings?scope=workspace");
    expect(createSettingsHash({ scope: "library", spaceId: libraryId, tab: "general" })).toBe(
      `#settings?scope=library&tab=general&spaceId=${libraryId}`,
    );
    expect(createSettingsHash({ scope: "library", spaceId: libraryId, tab: "collections" })).toBe(
      `#settings?scope=library&tab=collections&spaceId=${libraryId}`,
    );
    expect(createSettingsHash({ scope: "organization", tab: "overview" })).toBe("#settings?scope=organization&tab=overview");
    expect(createSettingsHash({ scope: "organization", tab: "workspaces" })).toBe("#settings?scope=organization&tab=workspaces");
    expect(createSettingsHash({ scope: "organization", tab: "members" })).toBe("#settings?scope=organization&tab=members");
    expect(createSettingsHash({ scope: "organization", tab: "assessment" })).toBe("#settings?scope=organization&tab=assessment");
    expect(createSettingsHash({ panel: "workspace-notifications" })).toBe("#settings?panel=workspace-notifications");
    expect(createSettingsHash({ panel: "workspace-access-identity" })).toBe("#settings?scope=workspace&tab=permissions");
    expect(createSettingsHash({ panel: "deferred-plan" })).toBe("#settings?scope=workspace");
    expect(getSettingsFiltersFromHash("#settings?panel=workspace-notifications")).toEqual({
      panel: "workspace-notifications",
      scope: "workspace",
      spaceId: null,
      tab: "general",
    });
    expect(getSettingsFiltersFromHash(`#settings?tab=members&spaceId=${libraryId}`)).toEqual({
      panel: null,
      scope: "workspace",
      spaceId: libraryId,
      tab: "members",
    });
    expect(getSettingsFiltersFromHash(`#settings?scope=library&tab=documents&spaceId=${libraryId}`)).toEqual({
      panel: null,
      scope: "library",
      spaceId: libraryId,
      tab: "documents",
    });
    expect(getSettingsFiltersFromHash(`#settings?scope=library&tab=members&spaceId=${libraryId}`)).toEqual({
      panel: null,
      scope: "library",
      spaceId: libraryId,
      tab: "general",
    });
    expect(getSettingsFiltersFromHash("#settings?scope=organization&tab=overview")).toEqual({
      panel: null,
      scope: "organization",
      spaceId: null,
      tab: "overview",
    });
    expect(getSettingsFiltersFromHash("#settings?scope=organization&tab=workspaces")).toEqual({
      panel: null,
      scope: "organization",
      spaceId: null,
      tab: "workspaces",
    });
    expect(getSettingsFiltersFromHash("#settings?scope=organization&tab=members")).toEqual({
      panel: null,
      scope: "organization",
      spaceId: null,
      tab: "members",
    });
    expect(getSettingsFiltersFromHash("#settings?scope=organization&tab=assessment")).toEqual({
      panel: null,
      scope: "organization",
      spaceId: null,
      tab: "assessment",
    });
    expect(getSettingsFiltersFromHash(`#settings?scope=organization&tab=security&spaceId=${libraryId}`)).toEqual({
      panel: null,
      scope: "organization",
      spaceId: libraryId,
      tab: "overview",
    });
    expect(getSettingsFiltersFromHash("#settings?tab=unknown&spaceId=invalid")).toEqual({
      panel: null,
      scope: "workspace",
      spaceId: null,
      tab: "general",
    });
    expect(getSettingsFiltersFromHash(`#settings?scope=admin&tab=security&spaceId=${libraryId}`)).toEqual({
      panel: null,
      scope: "workspace",
      spaceId: libraryId,
      tab: "security",
    });
  });

  test("routes personal and organization settings outside workspace settings", () => {
    expect(createPersonalSettingsHash()).toBe("#personal-settings");
    expect(createOrganizationSettingsHash()).toBe("#organization-settings");
    expect(createOrganizationSettingsHash({ panel: "members" })).toBe("#organization-settings?panel=members");

    expect(getSettingsRouteTarget("#settings")).toBe("workspace");
    expect(getSettingsRouteTarget("#personal-settings")).toBe("personal");
    expect(getSettingsRouteTarget("#organization-settings")).toBe("organization");
    expect(getSettingsRouteTarget("#settings?panel=personal-preferences")).toBe("personal");
    expect(getSettingsRouteTarget("#settings?scope=organization&tab=overview")).toBe("organization");
    expect(getSettingsRouteTarget("#settings?scope=library&tab=collections")).toBe("workspace");

    expect(getOrganizationSettingsPanelFromHash("#organization-settings")).toBe("profile");
    expect(getOrganizationSettingsPanelFromHash("#organization-settings?panel=workspaces")).toBe("workspaces");
    expect(getOrganizationSettingsPanelFromHash("#organization-settings?panel=members")).toBe("members");
    expect(getOrganizationSettingsPanelFromHash("#settings?scope=organization&tab=workspaces")).toBe("workspaces");
    expect(getOrganizationSettingsPanelFromHash("#settings?panel=organization-members")).toBe("members");
  });

  test("normalizes action urls to safe internal hashes only", () => {
    expect(normalizeInternalActionHash("#workspace-members")).toBe("#settings?scope=workspace&tab=members");
    expect(normalizeInternalActionHash("#members")).toBe("#settings?scope=workspace&tab=members");
    expect(normalizeInternalActionHash("#permission-admin")).toBe("#settings?scope=workspace&tab=permissions");
    expect(normalizeInternalActionHash("#workspace-groups")).toBe("#settings?scope=workspace&tab=permissions");
    expect(normalizeInternalActionHash("#groups")).toBe("#settings?scope=workspace&tab=permissions");
    expect(normalizeInternalActionHash("#scim")).toBe("#settings?scope=workspace&tab=integrations");
    expect(normalizeInternalActionHash("#permissions")).toBe("#settings?scope=workspace&tab=permissions");
    expect(normalizeInternalActionHash("#share")).toBe("#settings?scope=workspace&tab=permissions");
    expect(normalizeInternalActionHash(`#share?documentId=${documentId}`)).toBe(`#share?documentId=${documentId}`);
    expect(normalizeInternalActionHash("#settings?tab=notifications")).toBe("#settings?tab=notifications");
    expect(normalizeInternalActionHash("#personal-settings")).toBe("#personal-settings");
    expect(normalizeInternalActionHash("#organization-settings?panel=members")).toBe("#organization-settings?panel=members");
    expect(normalizeInternalActionHash(`#libraries?libraryId=${libraryId}`)).toBe(`#libraries?libraryId=${libraryId}`);
    expect(normalizeInternalActionHash(`#editor?documentId=${documentId}`)).toBe(`#editor?documentId=${documentId}`);
    expect(normalizeInternalActionHash("https://example.com")).toBe("#updates");
    expect(normalizeInternalActionHash("javascript:alert(1)")).toBe("#updates");
    expect(normalizeInternalActionHash("#unknown")).toBe("#updates");
    expect(normalizeInternalActionHash(null)).toBe("#updates");
  });

  test("canonicalizes legacy permission and member routes", () => {
    expect(getCanonicalHashRedirect("#workspace-members")).toBe("#settings?scope=workspace&tab=members");
    expect(getCanonicalHashRedirect("#members")).toBe("#settings?scope=workspace&tab=members");
    expect(getCanonicalHashRedirect("#permission-admin")).toBe("#settings?scope=workspace&tab=permissions");
    expect(getCanonicalHashRedirect("#workspace-groups")).toBe("#settings?scope=workspace&tab=permissions");
    expect(getCanonicalHashRedirect("#groups")).toBe("#settings?scope=workspace&tab=permissions");
    expect(getCanonicalHashRedirect("#scim")).toBe("#settings?scope=workspace&tab=integrations");
    expect(getCanonicalHashRedirect("#share")).toBe("#settings?scope=workspace&tab=permissions");
    expect(getCanonicalHashRedirect("#settings?panel=workspace-access-identity")).toBe("#settings?scope=workspace&tab=permissions");
    expect(getCanonicalHashRedirect("#settings?panel=deferred-plan")).toBe("#settings?scope=workspace");
    expect(getCanonicalHashRedirect("#settings?scope=workspace&tab=developer")).toBe("#settings?scope=workspace");
    expect(getCanonicalHashRedirect(`#discovery?libraryId=${libraryId}&folderId=${collectionId}&folderTitle=Foundations&q=risk`)).toBe(
      `#search?libraryId=${libraryId}&folderId=${collectionId}&folderTitle=Foundations&q=risk`,
    );
    expect(getCanonicalHashRedirect(`#share?documentId=${documentId}`)).toBe(null);
    expect(getCanonicalHashRedirect("#settings")).toBe(null);
  });

  test("preserves safe protected target after login", () => {
    expect(getPostLoginRedirectHash("#settings")).toBe("#settings");
    expect(getPostLoginRedirectHash("#workspace-members")).toBe("#settings?scope=workspace&tab=members");
    expect(getPostLoginRedirectHash("#permission-admin")).toBe("#settings?scope=workspace&tab=permissions");
    expect(getPostLoginRedirectHash("#scim")).toBe("#settings?scope=workspace&tab=integrations");
    expect(getPostLoginRedirectHash("#discovery?q=risk")).toBe("#search?q=risk");
    expect(getPostLoginRedirectHash("#settings?panel=workspace-access-identity")).toBe("#settings?scope=workspace&tab=permissions");
    expect(getPostLoginRedirectHash("#personal-settings")).toBe("#personal-settings");
    expect(getPostLoginRedirectHash("#organization-settings?panel=profile")).toBe("#organization-settings?panel=profile");
    expect(getPostLoginRedirectHash(`#settings?tab=notifications&spaceId=${libraryId}`)).toBe(
      `#settings?tab=notifications&spaceId=${libraryId}`,
    );
    expect(getPostLoginRedirectHash(`#settings?scope=library&tab=collections&spaceId=${libraryId}`)).toBe(
      `#settings?scope=library&tab=collections&spaceId=${libraryId}`,
    );
    expect(getPostLoginRedirectHash("#settings?scope=organization&tab=overview")).toBe("#settings?scope=organization&tab=overview");
    expect(getPostLoginRedirectHash(`#editor?documentId=${documentId}`)).toBe(`#editor?documentId=${documentId}`);
    expect(getPostLoginRedirectHash("#forgot-password")).toBe("#home");
    expect(getPostLoginRedirectHash("")).toBe("#home");
  });
});
