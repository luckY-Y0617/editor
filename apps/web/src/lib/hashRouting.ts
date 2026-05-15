import { isUuid } from "./apiClient";

export type ParsedHashRoute = {
  params: URLSearchParams;
  route: string;
};

export type WorkspaceSettingsTab =
  | "advanced"
  | "collections"
  | "developer"
  | "documents"
  | "general"
  | "integrations"
  | "members"
  | "notifications"
  | "overview"
  | "permissions"
  | "plan"
  | "security"
  | "workspaces"
  | "assessment";

export type WorkspaceSettingsScope = "library" | "organization" | "workspace";

export type SettingsPanelId =
  | "deferred-developer"
  | "deferred-plan"
  | "organization-members"
  | "organization-profile"
  | "organization-workspaces"
  | "personal-preferences"
  | "workspace-access-identity"
  | "workspace-general"
  | "workspace-integrations"
  | "workspace-members"
  | "workspace-notifications"
  | "workspace-permissions"
  | "workspace-security";

export type SettingsRouteTarget = "organization" | "personal" | "workspace";

export type OrganizationSettingsPanel = "members" | "profile" | "workspaces";

const workspaceSettingsTabs = new Set<WorkspaceSettingsTab>([
  "general",
  "integrations",
  "members",
  "notifications",
  "permissions",
  "security",
]);

const librarySettingsTabs = new Set<WorkspaceSettingsTab>([
  "advanced",
  "collections",
  "documents",
  "general",
  "notifications",
  "permissions",
]);

const organizationSettingsTabs = new Set<WorkspaceSettingsTab>(["assessment", "members", "overview", "workspaces"]);

const postLoginRoutes = new Set([
  "#home",
  "#dashboard",
  "#libraries",
  "#access-sharing",
  "#editor",
  "#search",
  "#discovery",
  "#settings",
  "#personal-settings",
  "#organization-settings",
  "#versions",
  "#compare",
  "#version-history",
  "#share",
  "#permissions",
  "#workspace-members",
  "#members",
  "#permission-admin",
  "#workspace-groups",
  "#groups",
  "#scim",
  "#updates",
  "#notifications",
  "#links",
  "#sharing",
]);

const publicSharePathPrefix = "/public/share-links/";

export function parseHashRoute(hash: string): ParsedHashRoute {
  const routeInput = normalizeRouteInput(hash);
  const normalizedHash = routeInput.startsWith("#") ? routeInput : `#${routeInput}`;
  const queryStart = normalizedHash.indexOf("?");
  const rawRoute = queryStart >= 0 ? normalizedHash.slice(0, queryStart) : normalizedHash;
  const query = queryStart >= 0 ? normalizedHash.slice(queryStart + 1) : "";
  const params = new URLSearchParams(query);
  const publicShareToken = getPublicShareTokenFromRoute(rawRoute);

  if (publicShareToken) {
    params.set("token", publicShareToken);
    return {
      params,
      route: "#public/share-links",
    };
  }

  return {
    params,
    route: rawRoute || "#",
  };
}

export function getHashRoute(hash: string) {
  return parseHashRoute(hash).route;
}

export function getPostLoginRedirectHash(hash: string) {
  const normalizedHash = hash?.trim();
  if (!normalizedHash) {
    return "#home";
  }

  const route = getHashRoute(normalizedHash);
  if (!postLoginRoutes.has(route)) {
    return "#home";
  }

  return getCanonicalHashRedirect(normalizedHash) ?? normalizedHash;
}

export function getEditorDocumentIdFromHash(hash: string) {
  const { params, route } = parseHashRoute(hash);

  if (route !== "#editor") {
    return null;
  }

  const documentId = params.get("documentId");
  return documentId && isUuid(documentId) ? documentId : null;
}

export function getShareDocumentIdFromHash(hash: string) {
  const { params, route } = parseHashRoute(hash);

  if (route !== "#share" && route !== "#permissions") {
    return null;
  }

  const documentId = params.get("documentId");
  return documentId && isUuid(documentId) ? documentId : null;
}

export function createEditorHash(documentId?: string | null) {
  return documentId && isUuid(documentId) ? `#editor?documentId=${encodeURIComponent(documentId)}` : "#editor";
}

export function createShareHash(documentId?: string | null) {
  return documentId && isUuid(documentId) ? `#share?documentId=${encodeURIComponent(documentId)}` : "#share";
}

export function createDocumentAdvancedPermissionsHash(documentId?: string | null) {
  return createShareHash(documentId);
}

export function createWorkspaceMembersHash() {
  return createSettingsHash({ scope: "workspace", tab: "members" });
}

export function createWorkspacePermissionsHash(): string {
  return createSettingsHash({ scope: "workspace", tab: "permissions" });
}

export function createWorkspaceIntegrationsHash() {
  return createSettingsHash({ scope: "workspace", tab: "integrations" });
}

export function createWorkspaceLinkManagementHash() {
  return "#access-sharing";
}

export function createWorkspaceUpdatesHash(options: { tab?: string | null } = {}) {
  const tab = options.tab?.trim();
  return tab ? `#updates?tab=${encodeURIComponent(tab)}` : "#updates";
}

export function createPublicShareHash(token?: string | null) {
  const normalizedToken = normalizePublicShareToken(token);
  return normalizedToken ? `#public/share-links/${encodeURIComponent(normalizedToken)}` : "#public/share-links";
}

export function getPublicShareTokenFromHash(hash: string) {
  const { params, route } = parseHashRoute(hash);

  if (route !== "#public/share-links") {
    return null;
  }

  return normalizePublicShareToken(params.get("token"));
}

export function createLibrariesHash(options: { collectionId?: string | null; libraryId?: string | null } = {}) {
  const params = new URLSearchParams();

  if (options.libraryId && isUuid(options.libraryId)) {
    params.set("libraryId", options.libraryId);
  }

  if (options.collectionId && isUuid(options.collectionId)) {
    params.set("collectionId", options.collectionId);
  }

  const query = params.toString();
  return query ? `#libraries?${query}` : "#libraries";
}

export function getLibrariesFiltersFromHash(hash: string) {
  const { params, route } = parseHashRoute(hash);
  if (route !== "#libraries") {
    return {
      collectionId: null,
      libraryId: null,
    };
  }

  const collectionId = params.get("collectionId");
  const libraryId = params.get("libraryId");

  return {
    collectionId: collectionId && isUuid(collectionId) ? collectionId : null,
    libraryId: libraryId && isUuid(libraryId) ? libraryId : null,
  };
}

export function createSearchHash(options: {
  folderId?: string | null;
  folderTitle?: string | null;
  libraryId?: string | null;
  q?: string | null;
} = {}) {
  const params = new URLSearchParams();

  if (options.libraryId && isUuid(options.libraryId)) {
    params.set("libraryId", options.libraryId);
  }

  if (options.folderId && isUuid(options.folderId)) {
    params.set("folderId", options.folderId);
  }

  if (options.folderTitle) {
    params.set("folderTitle", options.folderTitle);
  }

  if (options.q) {
    params.set("q", options.q);
  }

  const query = params.toString();
  return query ? `#search?${query}` : "#search";
}

const settingsPanelIds = new Set<SettingsPanelId>([
  "deferred-developer",
  "deferred-plan",
  "organization-members",
  "organization-profile",
  "organization-workspaces",
  "personal-preferences",
  "workspace-access-identity",
  "workspace-general",
  "workspace-integrations",
  "workspace-members",
  "workspace-notifications",
  "workspace-permissions",
  "workspace-security",
]);

export function createSettingsHash(options: {
  panel?: SettingsPanelId | null;
  scope?: WorkspaceSettingsScope | null;
  spaceId?: string | null;
  tab?: WorkspaceSettingsTab | null;
} = {}): string {
  if (options.panel === "workspace-access-identity") {
    return createWorkspacePermissionsHash();
  }

  if (options.panel === "deferred-developer" || options.panel === "deferred-plan") {
    return createSettingsHash({ scope: "workspace" });
  }

  const params = new URLSearchParams();
  const scope = options.scope === "library" ? "library" : options.scope === "organization" ? "organization" : options.scope === "workspace" ? "workspace" : null;
  const tabs = scope === "library" ? librarySettingsTabs : scope === "organization" ? organizationSettingsTabs : workspaceSettingsTabs;

  if (options.panel && settingsPanelIds.has(options.panel)) {
    params.set("panel", options.panel);
  }

  if (scope) {
    params.set("scope", scope);
  }

  if (options.tab && tabs.has(options.tab) && (options.tab !== "general" || scope === "library")) {
    params.set("tab", options.tab);
  }

  if (options.spaceId && isUuid(options.spaceId)) {
    params.set("spaceId", options.spaceId);
  }

  const query = params.toString();
  return query ? `#settings?${query}` : "#settings";
}

export function createPersonalSettingsHash() {
  return "#personal-settings";
}

export function createOrganizationSettingsHash(options: { panel?: OrganizationSettingsPanel | null } = {}) {
  const params = new URLSearchParams();
  if (options.panel && ["members", "profile", "workspaces"].includes(options.panel)) {
    params.set("panel", options.panel);
  }

  const query = params.toString();
  return query ? `#organization-settings?${query}` : "#organization-settings";
}

export function getSettingsRouteTarget(hash: string): SettingsRouteTarget {
  const { params, route } = parseHashRoute(hash);

  if (route === "#personal-settings") {
    return "personal";
  }

  if (route === "#organization-settings") {
    return "organization";
  }

  if (route !== "#settings") {
    return "workspace";
  }

  const panel = params.get("panel");
  if (panel === "personal-preferences") {
    return "personal";
  }

  if (panel?.startsWith("organization-") || params.get("scope") === "organization") {
    return "organization";
  }

  return "workspace";
}

export function getOrganizationSettingsPanelFromHash(hash: string): OrganizationSettingsPanel {
  const { params, route } = parseHashRoute(hash);

  if (route === "#organization-settings") {
    const panel = params.get("panel");
    if (panel === "members" || panel === "workspaces") {
      return panel;
    }

    return "profile";
  }

  if (route === "#settings") {
    const panel = params.get("panel");
    if (panel === "organization-members") {
      return "members";
    }

    if (panel === "organization-workspaces") {
      return "workspaces";
    }

    if (params.get("scope") === "organization") {
      const tab = params.get("tab");
      if (tab === "members" || tab === "workspaces") {
        return tab;
      }
    }
  }

  return "profile";
}

export function getSettingsFiltersFromHash(hash: string): {
  panel: SettingsPanelId | null;
  scope: WorkspaceSettingsScope;
  spaceId: string | null;
  tab: WorkspaceSettingsTab;
} {
  const { params, route } = parseHashRoute(hash);
  if (route !== "#settings") {
    return {
      panel: null,
      scope: "workspace",
      spaceId: null,
      tab: "general",
    };
  }

  const requestedScope = params.get("scope");
  const scope: WorkspaceSettingsScope = requestedScope === "library" ? "library" : requestedScope === "organization" ? "organization" : "workspace";
  const tabs = scope === "library" ? librarySettingsTabs : scope === "organization" ? organizationSettingsTabs : workspaceSettingsTabs;
  const fallbackTab: WorkspaceSettingsTab = scope === "organization" ? "overview" : "general";
  const panel = params.get("panel");
  const tab = params.get("tab");
  const spaceId = params.get("spaceId");

  return {
    panel: panel && settingsPanelIds.has(panel as SettingsPanelId) ? (panel as SettingsPanelId) : null,
    scope,
    spaceId: spaceId && isUuid(spaceId) ? spaceId : null,
    tab: tab && tabs.has(tab as WorkspaceSettingsTab) ? (tab as WorkspaceSettingsTab) : fallbackTab,
  };
}

export function getSearchFiltersFromHash(hash: string) {
  const { params, route } = parseHashRoute(hash);
  if (route !== "#search" && route !== "#discovery") {
    return {
      folderId: null,
      folderTitle: null,
      libraryId: null,
      q: null,
    };
  }

  const folderId = params.get("folderId");
  const folderTitle = params.get("folderTitle");
  const libraryId = params.get("libraryId");
  const q = params.get("q");

  return {
    folderId: folderId && isUuid(folderId) ? folderId : null,
    folderTitle: folderTitle || null,
    libraryId: libraryId && isUuid(libraryId) ? libraryId : null,
    q: q || null,
  };
}

export function getCanonicalHashRedirect(hash: string) {
  const normalizedHash = hash?.trim();
  if (!normalizedHash?.startsWith("#")) {
    return null;
  }

  const { params, route } = parseHashRoute(normalizedHash);
  if (route === "#discovery") {
    return createSearchHash({
      folderId: params.get("folderId"),
      folderTitle: params.get("folderTitle"),
      libraryId: params.get("libraryId"),
      q: params.get("q"),
    });
  }

  if (route === "#members" || route === "#workspace-members") {
    return createWorkspaceMembersHash();
  }

  if (route === "#permission-admin" || route === "#workspace-groups" || route === "#groups") {
    return createWorkspacePermissionsHash();
  }

  if (route === "#scim") {
    return createWorkspaceIntegrationsHash();
  }

  if (route === "#links" || route === "#sharing") {
    return createWorkspaceLinkManagementHash();
  }

  if ((route === "#permissions" || route === "#share") && !getShareDocumentIdFromHash(normalizedHash)) {
    return createWorkspacePermissionsHash();
  }

  if (route === "#settings") {
    const panel = params.get("panel");
    if (panel === "workspace-access-identity") {
      return createWorkspacePermissionsHash();
    }

    if (params.get("scope") === "workspace" && params.get("tab") === "links") {
      return createWorkspaceLinkManagementHash();
    }

    if (panel === "deferred-plan" || panel === "deferred-developer") {
      return createSettingsHash({ scope: "workspace" });
    }

    const tab = params.get("tab");
    if (params.get("scope") === "workspace" && (tab === "plan" || tab === "developer")) {
      return createSettingsHash({ scope: "workspace" });
    }
  }

  return null;
}

export function normalizeInternalActionHash(value?: string | null) {
  if (!value) {
    return "#updates";
  }

  const trimmed = value.trim();
  if (!trimmed.startsWith("#")) {
    return "#updates";
  }

  const canonicalHash = getCanonicalHashRedirect(trimmed);
  if (canonicalHash) {
    return canonicalHash;
  }

  const route = getHashRoute(trimmed);
  const allowedRoutes = new Set([
    "#home",
    "#dashboard",
    "#libraries",
    "#access-sharing",
    "#editor",
    "#search",
    "#discovery",
    "#share",
    "#permissions",
    "#settings",
    "#personal-settings",
    "#organization-settings",
    "#workspace-groups",
    "#workspace-members",
    "#members",
    "#permission-admin",
    "#scim",
    "#updates",
    "#notifications",
    "#links",
    "#sharing",
  ]);

  return allowedRoutes.has(route) ? trimmed : "#updates";
}

export function createRouteHashFromLocation(location: Pick<Location, "hash" | "pathname" | "search">) {
  if (location.hash) {
    return location.hash;
  }

  if (location.pathname.startsWith(publicSharePathPrefix)) {
    return `${location.pathname}${location.search}`;
  }

  return "";
}

function getPublicShareTokenFromRoute(route: string) {
  const routeWithoutHash = route.startsWith("#") ? route.slice(1) : route;
  const normalizedRoute = routeWithoutHash.startsWith("/") ? routeWithoutHash : `/${routeWithoutHash}`;

  if (!normalizedRoute.startsWith(publicSharePathPrefix)) {
    return null;
  }

  const encodedToken = normalizedRoute.slice(publicSharePathPrefix.length).split("/")[0];
  return normalizePublicShareToken(safeDecodeURIComponent(encodedToken));
}

function normalizeRouteInput(value: string) {
  const trimmed = value?.trim() ?? "";
  if (/^https?:\/\//i.test(trimmed)) {
    try {
      const url = new URL(trimmed);
      if (url.hash) {
        return url.hash;
      }

      if (url.pathname.startsWith(publicSharePathPrefix)) {
        return `${url.pathname}${url.search}`;
      }
    } catch {
      return trimmed;
    }
  }

  return trimmed;
}

function normalizePublicShareToken(token?: string | null) {
  const trimmed = token?.trim();
  if (!trimmed || trimmed.length > 512 || /[\s?#/\\]/.test(trimmed)) {
    return null;
  }

  return trimmed;
}

function safeDecodeURIComponent(value: string) {
  try {
    return decodeURIComponent(value);
  } catch {
    return "";
  }
}
