import {
  Bell,
  BookOpen,
  Boxes,
  Building2,
  ChevronRight,
  CircleAlert,
  Code2,
  Database,
  FileText,
  Inbox,
  KeyRound,
  Layers3,
  Link2,
  LockKeyhole,
  PencilLine,
  Plug,
  Puzzle,
  Save,
  ScrollText,
  Settings,
  ShieldCheck,
  UserCheck,
  UsersRound,
  X,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl, getConfiguredWorkspaceId } from "../lib/apiClient";
import { getCurrentUser, getSecurityState, type AuthSecurityStateResponse } from "../lib/authClient";
import {
  getBootstrap,
  getOrganizationMembers,
  getOrganizationProfile,
  getSpaceMap,
  getWorkspaceAuditLog,
  getWorkspaceNotificationPreferences,
  updateWorkspaceNotificationPreference,
  updateOrganizationProfile,
  type BootstrapResponse,
  type KnowledgeMapResponse,
  type OrganizationMembersResponse,
  type OrganizationProfileResponse,
  type UpdateOrganizationProfileRequest,
  type PermissionNotificationPreferenceDto,
  type WorkspaceAuditEventDto,
  type WorkspaceAuditLogResponse,
} from "../lib/appApi";
import {
  createSettingsHash,
  getOrganizationSettingsPanelFromHash,
  getSettingsFiltersFromHash,
  type WorkspaceSettingsTab,
} from "../lib/hashRouting";
import {
  applyOrganizationReadApiStatus,
  createOrganizationSettingsNavGroups,
  createPersonalSettingsNavGroups,
  createWorkspaceSettingsTabRows,
  getOrganizationMemberManagementActions,
  getOrganizationWorkspaceProvisioningActions,
  getRecommendedOrganizationSettingsSlice,
  prepareOrganizationProfileUpdateRequest,
  toOrganizationSettingsAssessmentRows,
  toOrganizationMemberInventoryRows,
  toOrganizationOverviewModel,
  toOrganizationProfileEditCapability,
  toOrganizationReadSurfaceState,
  toOrganizationWorkspaceInventoryRows,
  toSettingsBoundaryRows,
  toLibraryGeneralSettings,
  toLibraryNotificationPreferenceRows,
  toLibrarySettingsCollectionRows,
  toLibrarySettingsDocumentSummary,
  toNotificationSettingsModel,
  toSecuritySettingsRows,
  toSpaceSettingsSummary,
  toSettingsCapabilityInventoryRows,
  toWorkspaceNotificationPreferenceModel,
  prepareWorkspaceNotificationPreferenceRequest,
  toWorkspaceNotificationPreferenceMutationError,
  toWorkspaceGeneralSettings,
  type SettingsNavGroup,
  type OrganizationReadApiStatus,
  type WorkspaceNotificationPreferenceMode,
  type WorkspaceNotificationPreferenceMutationStatus,
  type WorkspaceSettingsStatus,
} from "../lib/workspaceSettingsModel";
import {
  PermissionAdminApiError,
  addWorkspaceMember,
  createScimToken,
  getConfiguredPermissionAdminApiBaseUrl,
  getScimResourceTypes,
  getScimSchemas,
  getScimServiceProviderConfig,
  getScimTokens,
  getWorkspaceGroups as getPermissionWorkspaceGroups,
  getWorkspaceMembers as getPermissionWorkspaceMembers,
  removeWorkspaceMember,
  revokeScimToken,
  updateWorkspaceMemberRole,
  type CreateScimTokenResponse,
  type ScimListResponse,
  type ScimResourceTypeDto,
  type ScimSchemaDto,
  type ScimServiceProviderConfigResponse,
  type ScimTokenDto,
  type WorkspaceGroupDto,
  type WorkspaceMemberDto,
} from "../lib/permissionAdminApi";
import {
  getCurrentWorkspaceRole,
  getMemberActionCapability,
  getRoleChangeOptions,
  toPermissionMutationError,
  type CurrentWorkspaceRole,
} from "../lib/permissionAdminModel";
import { getDisplayLanguageOptions, t, type DisplayLocale, useDisplayLanguage } from "../lib/i18n";
import type { NotificationApiStatus } from "../lib/workspaceUpdatesModel";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

type SettingsDataStatus = "error" | "forbidden" | "idle" | "loading" | "ready" | "unconfigured";
type SecurityStatus = "error" | "forbidden" | "loading" | "ready" | "unconfigured";

const settingsPatternStyle = {
  "--permission-admin-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--permission-admin-route-pattern": `url(${routePatternUrl})`,
  "--permission-admin-topographic-pattern": `url(${topographicPatternUrl})`,
  "--workspace-home-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--workspace-home-route-pattern": `url(${routePatternUrl})`,
  "--workspace-home-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

export function WorkspaceSettingsPage() {
  const { locale } = useDisplayLanguage();
  const [hash, setHash] = useState(window.location.hash);
  const filters = getSettingsFiltersFromHash(hash);
  const bootstrap = useSettingsBootstrap();
  const workspaceId = bootstrap.data?.workspace.id ?? getConfiguredWorkspaceId();
  const selectedSpaceId = getSelectedSpaceId(bootstrap.data, filters.spaceId);
  const map = useSettingsSpaceMap(selectedSpaceId, bootstrap.status);
  const preferences = useSettingsNotificationPreferences(workspaceId);
  const members = useSettingsWorkspaceMembers(workspaceId);
  const groups = useSettingsWorkspaceGroups(workspaceId);
  const currentRole = useSettingsCurrentWorkspaceRole(workspaceId ?? null);
  const memberMutations = useSettingsMemberMutations(members.apiBaseUrl, workspaceId ?? null, members.reload);
  const scimDiscovery = useSettingsScimDiscovery(workspaceId ?? null);
  const scimTokens = useSettingsScimTokens(workspaceId ?? null);
  const security = useSettingsSecurityState();
  const activeDashboardTab = useMemo(
    () => getWorkspaceSettingsDashboardActiveTab(filters),
    [filters.panel, filters.scope, filters.tab],
  );
  const audit = useSettingsWorkspaceAudit(workspaceId ?? null, activeDashboardTab === "security");
  const general = bootstrap.data ? toWorkspaceGeneralSettings(bootstrap.data, filters.spaceId) : null;
  const spaceSummary = bootstrap.data ? toSpaceSettingsSummary(bootstrap.data, map.data, filters.spaceId) : null;

  useEffect(() => {
    const syncHash = () => setHash(window.location.hash);
    window.addEventListener("hashchange", syncHash);
    return () => window.removeEventListener("hashchange", syncHash);
  }, []);

  return (
    <SettingsPageFrame activeSidebarItem="settings">
      <header className="permission-admin-heading workspace-settings-dashboard-heading">
        <div className="min-w-0">
          <h1>{t(locale, "settings.heading")}</h1>
          <p>{getWorkspaceSettingsDashboardIntro(locale)}</p>
        </div>
      </header>

      <WorkspaceSettingsDashboardTabs activeTab={activeDashboardTab} locale={locale} />

      <div className="workspace-settings-dashboard-content">
        {activeDashboardTab === "general" ? (
          <GeneralSettingsTab
            general={general}
            locale={locale}
            mapStatus={map.status}
            spaceSummary={spaceSummary}
            status={bootstrap.status}
          />
        ) : null}
        {activeDashboardTab === "notifications" ? (
          <NotificationsSettingsTab preferences={preferences} workspaceId={workspaceId ?? null} />
        ) : null}
        {activeDashboardTab === "members" ? (
          <WorkspaceMembersSettingsTab
            currentRole={currentRole}
            members={members}
            mutations={memberMutations}
          />
        ) : null}
        {activeDashboardTab === "permissions" ? (
          <WorkspacePermissionsSettingsTab
            bootstrapStatus={bootstrap.status}
            groups={groups}
            mapStatus={map.status}
            members={members}
            security={security}
            spaceSummary={spaceSummary}
          />
        ) : null}
        {activeDashboardTab === "integrations" ? (
          <IntegrationsSettingsTab
            discovery={scimDiscovery}
            scimTokens={scimTokens}
            workspaceId={workspaceId ?? null}
          />
        ) : null}
        {activeDashboardTab === "security" ? <SecuritySettingsTab audit={audit} security={security} /> : null}
      </div>
    </SettingsPageFrame>
  );
}

type WorkspaceSettingsDashboardTab =
  | "general"
  | "integrations"
  | "members"
  | "notifications"
  | "permissions"
  | "security";

type WorkspaceSettingsDashboardTabRow = {
  href: string | null;
  id: WorkspaceSettingsDashboardTab;
  label: string;
};

function WorkspaceSettingsDashboardTabs({
  activeTab,
  locale,
}: {
  activeTab: WorkspaceSettingsDashboardTab;
  locale: DisplayLocale;
}) {
  const tabs: WorkspaceSettingsDashboardTabRow[] = [
    { href: createSettingsHash({ scope: "workspace", tab: "general" }), id: "general", label: t(locale, "settings.general") },
    { href: createSettingsHash({ scope: "workspace", tab: "notifications" }), id: "notifications", label: t(locale, "settings.notifications") },
    { href: createSettingsHash({ scope: "workspace", tab: "members" }), id: "members", label: t(locale, "settings.members") },
    { href: createSettingsHash({ scope: "workspace", tab: "permissions" }), id: "permissions", label: t(locale, "settings.permissions") },
    { href: createSettingsHash({ scope: "workspace", tab: "security" }), id: "security", label: t(locale, "settings.security") },
    { href: createSettingsHash({ scope: "workspace", tab: "integrations" }), id: "integrations", label: t(locale, "settings.integrations") },
  ];

  return (
    <nav className="workspace-settings-dashboard-tabs" aria-label={t(locale, "settings.secondaryNavigation")}>
      {tabs.map((tab) => {
        const className = [
          activeTab === tab.id ? "is-active" : "",
          tab.href ? "" : "is-disabled",
        ].filter(Boolean).join(" ");

        return tab.href ? (
          <a aria-current={activeTab === tab.id ? "page" : undefined} className={className} href={tab.href} key={tab.id}>
            {tab.label}
          </a>
        ) : (
          <button aria-disabled="true" className={className} disabled key={tab.id} title={getSettingsStatusDisplayLabel(locale, "deferred")} type="button">
            {tab.label}
          </button>
        );
      })}
    </nav>
  );
}

function WorkspacePermissionsSettingsTab({
  bootstrapStatus,
  groups,
  mapStatus,
  members,
  security,
  spaceSummary,
}: {
  bootstrapStatus: SettingsDataStatus;
  groups: ReturnType<typeof useSettingsWorkspaceGroups>;
  mapStatus: SettingsDataStatus;
  members: ReturnType<typeof useSettingsWorkspaceMembers>;
  security: ReturnType<typeof useSettingsSecurityState>;
  spaceSummary: ReturnType<typeof toSpaceSettingsSummary>;
}) {
  const { locale } = useDisplayLanguage();
  const activeGroups = groups.groups.filter((group) => !group.isArchived).length;
  const roleBreakdown = getWorkspaceRoleBreakdown(members.members);
  const memberCount = members.status === "ready" ? formatPeopleCount(locale, members.members.length) : statusLabel(members.status, locale);
  const groupCount = groups.status === "ready" ? formatItemCount(locale, activeGroups) : statusLabel(groups.status, locale);
  const workspaceValue = bootstrapStatus === "ready" ? (locale === "zh-CN" ? "正常运行" : "Running") : statusLabel(bootstrapStatus, locale);
  const libraryValue = mapStatus === "ready" && spaceSummary ? formatItemCount(locale, spaceSummary.collectionCount) : statusLabel(mapStatus, locale);
  const securityValue = security.status === "ready" ? (security.state?.stepUpRequiredForHighRiskActions ? "Step-up enabled" : "Standard") : statusLabel(security.status, locale);

  return (
    <section className="workspace-settings-dashboard" aria-label={t(locale, "settings.permissions")}>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.permissions")} />
        <div className="permission-admin-summary-grid workspace-settings-status-grid">
          <SettingsMetric label="Workspace" value={workspaceValue} />
          <SettingsMetric label={t(locale, "settings.members")} value={memberCount} />
          <SettingsMetric label={t(locale, "settings.groups")} value={groupCount} />
          <SettingsMetric label="Role buckets" value={formatItemCount(locale, roleBreakdown.activeRoleBuckets)} />
          <SettingsMetric label="Library folders" value={libraryValue} />
          <SettingsMetric label={t(locale, "settings.security")} value={securityValue} />
        </div>
        <p className="workspace-settings-note">Workspace Settings owns workspace member, group, security, and integration administration. Daily document sharing stays in the Editor Share drawer; document Advanced permissions stays document-scoped.</p>
      </section>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.groups")} />
        <SettingsCard icon={<UsersRound className="h-4 w-4" />} status={groups.status === "ready" ? "live" : "unavailable"} title={t(locale, "settings.groups")}>
          <p className="workspace-settings-note">Workspace groups are managed here as part of the workspace permission boundary. Organization members remain outside this workspace-scoped surface.</p>
          <WorkspaceGroupsSummary groups={groups.groups} status={groups.status} />
        </SettingsCard>
      </section>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.shareLinksPublicLinks")} />
        <SettingsCard icon={<LockKeyhole className="h-4 w-4" />} status="reused" title={t(locale, "settings.shareLinksPublicLinks")}>
          <p className="workspace-settings-note">Public links are created only through dedicated share-link API. This tab shows the boundary; daily document sharing remains in the Editor Share drawer.</p>
          <DefinitionList
            rows={[
              ["Workspace members", "Managed in the Members tab."],
              ["Workspace groups", "Managed in this Permissions tab."],
              ["Direct document grants", "Managed from document Advanced permissions."],
              ["Access requests", "Reviewed from Updates access requests."],
              ["Workspace inheritance", "Shown here as the workspace permission boundary."],
            ]}
          />
        </SettingsCard>
      </section>
    </section>
  );
}

function WorkspaceDashboardHealthCard({
  chip,
  description,
  icon,
  title,
  value,
}: {
  chip: { label: string; tone: "info" | "success" | "warning" };
  description: string;
  icon: ReactNode;
  title: string;
  value: string;
}) {
  return (
    <article className="workspace-settings-health-card">
      <span className="workspace-settings-dashboard-icon">{icon}</span>
      <div className="min-w-0">
        <span className="workspace-settings-card-eyebrow">{title}</span>
        <strong title={value}>{value}</strong>
        <p>{description}</p>
      </div>
      <WorkspaceSettingsChip label={chip.label} tone={chip.tone} />
    </article>
  );
}

function WorkspaceDashboardFeatureCard({
  actionHref,
  actionLabel,
  chip,
  description,
  icon,
  stats,
  title,
}: {
  actionHref: string;
  actionLabel: string;
  chip?: { label: string; tone: "info" | "success" | "warning" };
  description: string;
  icon: ReactNode;
  stats: Array<{ label: string; value: string }>;
  title: string;
}) {
  return (
    <article className="workspace-settings-feature-card">
      <header>
        <span className="workspace-settings-dashboard-icon">{icon}</span>
        <div className="min-w-0">
          <h2>{title}</h2>
          <p>{description}</p>
        </div>
      </header>
      <dl className="workspace-settings-kpi-grid">
        {stats.map((stat) => (
          <div key={`${stat.label}-${stat.value}`}>
            <dt>{stat.label}</dt>
            <dd title={stat.value}>{stat.value}</dd>
          </div>
        ))}
      </dl>
      <div className="workspace-settings-feature-footer">
        <a href={actionHref}>
          {actionLabel}
          <ChevronRight className="h-4 w-4" />
        </a>
        {chip ? <WorkspaceSettingsChip label={chip.label} tone={chip.tone} /> : null}
      </div>
    </article>
  );
}

function WorkspaceSettingsChip({
  label,
  tone,
}: {
  label: string;
  tone: "info" | "success" | "warning";
}) {
  return <span className={`workspace-settings-chip is-${tone}`}>{label}</span>;
}

function getWorkspaceSettingsDashboardActiveTab(filters: ReturnType<typeof getSettingsFiltersFromHash>): WorkspaceSettingsDashboardTab {
  switch (filters.panel) {
    case "workspace-general":
      return "general";
    case "workspace-integrations":
      return "integrations";
    case "workspace-members":
      return "members";
    case "workspace-notifications":
      return "notifications";
    case "workspace-permissions":
      return "permissions";
    case "workspace-security":
      return "security";
    case "workspace-access-identity":
      return "permissions";
    default:
      break;
  }

  switch (filters.tab) {
    case "integrations":
      return "integrations";
    case "members":
      return "members";
    case "notifications":
      return "notifications";
    case "permissions":
      return "permissions";
    case "security":
      return "security";
    case "general":
    default:
      return "general";
  }
}

function getWorkspaceSettingsDashboardIntro(locale: DisplayLocale) {
  return locale === "zh-CN"
    ? "\u7ba1\u7406\u5de5\u4f5c\u533a\u6210\u5458\u3001\u6743\u9650\u3001\u5b89\u5168\u4e0e\u96c6\u6210\u8bbe\u7f6e\u3002"
    : "Manage workspace members, permissions, security, and integrations.";
}

function getWorkspaceDashboardText(locale: DisplayLocale, key: string) {
  const zh = locale === "zh-CN";
  const copy: Record<string, [string, string]> = {
    accessRequestsHelp: ["查看外部或内部成员的访问申请与审批状态。", "Review external and internal member access requests and approvals."],
    admins: ["管理员", "Admins"],
    apiHelp: ["API 访问已启用与管理。", "API access is enabled and managed."],
    auditHelp: ["快速查看权限分布与近期活动摘要。", "Quickly review permission distribution and recent activity summaries."],
    auditOverview: ["审计与权限概览", "Audit & Permission Overview"],
    billing: ["账单", "Billing"],
    boundaryRules: ["边界规则", "Boundary rules"],
    currentLibraryFolders: ["当前资料库文件夹", "Current library folders"],
    customRoles: ["自定义角色", "Custom roles"],
    documentScoped: ["按文档管理", "Document scoped"],
    editors: ["编辑者", "Editors"],
    enabled: ["已启用", "Enabled"],
    enabledIntegrations: ["已启用集成", "Enabled integrations"],
    fromUpdates: ["见更新页", "See updates"],
    groupTotal: ["组总数", "Groups"],
    groupsHelp: ["按团队或职能管理成员分组与访问权限。", "Manage member groups and access by team or function."],
    handledThisMonth: ["本月已处理", "Handled this month"],
    highRiskOps: ["高风险操作", "High-risk operations"],
    integrationsHelp: ["查看并管理集成。", "Review and manage integrations."],
    manageMembers: ["管理成员", "Manage members"],
    managedInPermissions: ["权限页管理", "Managed in permissions"],
    medium: ["中等", "Medium"],
    memberTotal: ["成员总数", "Members"],
    membersHelp: ["查看与管理工作区成员、分配角色与权限。", "View and manage workspace members, roles, and permissions."],
    membersInGroups: ["受组管理的成员", "Members in groups"],
    needsReview: ["需要审核", "Needs review"],
    normal: ["正常", "Normal"],
    notVerified: ["未接入统计", "Not instrumented"],
    pendingInvites: ["待处理邀请", "Pending invites"],
    pendingRequests: ["待处理请求", "Pending requests"],
    publicLinks: ["公开链接", "Public links"],
    roleBoundary: ["角色边界", "Role boundaries"],
    roleBoundaryHelp: ["定义角色及数据、内容、操作等访问边界。", "Define role access boundaries for data, content, and operations."],
    running: ["正常运行", "Running"],
    securityHelp: ["符合默认安全策略。", "Matches the default security policy."],
    securityLevel: ["安全级别", "Security level"],
    shareLinksHelp: ["创建与管理对外分享链接及其访问策略。", "Create and manage external share links and access policy."],
    standard: ["标准", "Standard"],
    stepUpEnabled: ["已启用", "Enabled"],
    viewAuditLog: ["查看审计日志", "View audit log"],
    viewers: ["查看者", "Viewers"],
    viewRules: ["查看规则", "View rules"],
    workspaceStatus: ["工作区状态", "Workspace status"],
    workspaceStatusHelp: ["所有核心服务运行正常。", "All core services are running."],
  };

  const entry = copy[key];
  if (!entry) {
    return key;
  }

  return zh ? entry[0] : entry[1];
}

function formatPeopleCount(locale: DisplayLocale, count: number) {
  return locale === "zh-CN" ? `${count} \u4eba` : `${count} people`;
}

function formatItemCount(locale: DisplayLocale, count: number) {
  return locale === "zh-CN" ? `${count} \u4e2a` : `${count}`;
}

function getWorkspaceRoleBreakdown(members: WorkspaceMemberDto[]) {
  const roleBuckets = new Set<string>();
  let admins = 0;
  let editors = 0;
  let viewers = 0;

  members.forEach((member) => {
    const role = member.role.toLowerCase();
    roleBuckets.add(role);
    if (role === "owner" || role === "admin") {
      admins += 1;
    } else if (role === "editor") {
      editors += 1;
    } else {
      viewers += 1;
    }
  });

  return {
    activeRoleBuckets: roleBuckets.size,
    admins,
    editors,
    viewers,
  };
}

export function PersonalSettingsPage() {
  const { locale, setLocale } = useDisplayLanguage();
  const settingsNavGroups = useMemo(() => createPersonalSettingsNavGroups(), []);

  return (
    <SettingsPageFrame activeSidebarItem={null}>
      <header className="permission-admin-heading">
        <div className="min-w-0">
          <h1>{t(locale, "settings.personalSettingsHeading")}</h1>
          <p>{t(locale, "settings.personalSettingsReady")}</p>
        </div>
        <div className="permission-admin-heading-metrics workspace-settings-context-strip" aria-label={t(locale, "settings.contextSummary")}>
          <SettingsMetric label={t(locale, "settings.scopePersonal")} value={t(locale, "settings.preferences")} />
          <SettingsMetric label={t(locale, "common.api")} value={t(locale, "common.notConnected")} />
        </div>
      </header>
      <div className="workspace-settings-layout">
        <SettingsSecondaryNav
          activePanelId="personal-preferences"
          groups={settingsNavGroups}
          locale={locale}
        />
        <div className="workspace-settings-detail">
          <PersonalPreferencesPanel locale={locale} setLocale={setLocale} />
        </div>
      </div>
    </SettingsPageFrame>
  );
}

export function OrganizationSettingsPage() {
  const { locale } = useDisplayLanguage();
  const [hash, setHash] = useState(window.location.hash);
  const bootstrap = useSettingsBootstrap();
  const organization = useSettingsOrganizationData(bootstrap.data?.workspace.organizationId ?? null);
  const activeOrganizationPanel = getOrganizationSettingsPanelFromHash(hash);
  const activePanelId = activeOrganizationPanel === "members"
    ? "organization-members"
    : activeOrganizationPanel === "workspaces"
      ? "organization-workspaces"
      : "organization-profile";
  const settingsNavGroups = useMemo(() => createOrganizationSettingsNavGroups(), []);
  const organizationOverview = useMemo(() => toOrganizationOverviewModel(organization.profile), [organization.profile]);
  const organizationWorkspaces = useMemo(
    () => toOrganizationWorkspaceInventoryRows(organization.profile, bootstrap.data?.workspace.id ?? null),
    [bootstrap.data?.workspace.id, organization.profile],
  );
  const organizationMembers = useMemo(() => toOrganizationMemberInventoryRows(organization.members), [organization.members]);
  const organizationOverviewState = useMemo(
    () => toOrganizationReadSurfaceState("overview", organization.profileStatus, Boolean(organizationOverview)),
    [organization.profileStatus, organizationOverview],
  );
  const organizationWorkspacesState = useMemo(
    () => toOrganizationReadSurfaceState("workspaces", organization.profileStatus, organizationWorkspaces.length > 0),
    [organization.profileStatus, organizationWorkspaces.length],
  );
  const organizationMembersState = useMemo(
    () => toOrganizationReadSurfaceState("members", organization.membersStatus, organizationMembers.length > 0),
    [organization.membersStatus, organizationMembers.length],
  );
  const organizationProfileEditCapability = useMemo(
    () => toOrganizationProfileEditCapability(
      organization.profile,
      organization.profileStatus,
      organization.profileMutationStatus,
    ),
    [organization.profile, organization.profileMutationStatus, organization.profileStatus],
  );

  useEffect(() => {
    const syncHash = () => setHash(window.location.hash);
    window.addEventListener("hashchange", syncHash);
    return () => window.removeEventListener("hashchange", syncHash);
  }, []);

  return (
    <SettingsPageFrame activeSidebarItem={null}>
      <header className="permission-admin-heading">
        <div className="min-w-0">
          <h1>{t(locale, "settings.organizationSettingsHeading")}</h1>
          <p>{t(locale, "settings.organizationSettingsReady")}</p>
        </div>
        <div className="permission-admin-heading-metrics workspace-settings-context-strip" aria-label={t(locale, "settings.contextSummary")}>
          <SettingsMetric
            label={t(locale, "settings.scopeOrganization")}
            value={organizationOverview?.name ?? statusLabel(organization.profileStatus === "not-found" ? "error" : organization.profileStatus, locale)}
          />
          <SettingsMetric label={t(locale, "common.api")} value={getConfiguredApiBaseUrl() ? t(locale, "common.configured") : t(locale, "common.notConnected")} />
        </div>
      </header>

      <div className="workspace-settings-layout">
        <SettingsSecondaryNav
          activePanelId={activePanelId}
          groups={settingsNavGroups}
          locale={locale}
        />
        <div className="workspace-settings-detail">
          {activePanelId === "organization-profile" ? (
            <OrganizationOverviewTab
              capability={organizationProfileEditCapability}
              errorMessage={organization.profileMutationError}
              model={organizationOverview}
              onSave={organization.updateProfile}
              state={organizationOverviewState}
            />
          ) : null}
          {activePanelId === "organization-workspaces" ? (
            <OrganizationWorkspacesTab rows={organizationWorkspaces} state={organizationWorkspacesState} />
          ) : null}
          {activePanelId === "organization-members" ? (
            <OrganizationMembersTab rows={organizationMembers} state={organizationMembersState} />
          ) : null}
        </div>
      </div>
    </SettingsPageFrame>
  );
}

function SettingsPageFrame({
  activeSidebarItem,
  children,
}: {
  activeSidebarItem: "home" | "libraries" | "settings" | "updates" | null;
  children: ReactNode;
}) {
  const { locale } = useDisplayLanguage();
  return (
    <main className="workspace-settings-shell permission-admin-shell flex h-screen flex-col overflow-hidden" style={settingsPatternStyle}>
      <WorkspaceHomeTopBar />
      <div className="permission-admin-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar activeItem={activeSidebarItem} showCollections={false} />
        <section className="permission-admin-feed workspace-settings-feed editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="workspace-home-mobile-nav md:hidden" aria-label="Workspace navigation">
            <a href="#home">{t(locale, "nav.home")}</a>
            <a href="#libraries">{t(locale, "nav.libraries")}</a>
            <a href="#updates">{t(locale, "nav.updates")}</a>
            <a aria-current={activeSidebarItem === "settings" ? "page" : undefined} href="#settings">{t(locale, "nav.settings")}</a>
          </div>
          <div className="permission-admin-feed-inner workspace-settings-inner">
            {children}
          </div>
        </section>
      </div>
    </main>
  );
}

function SettingsSecondaryNav({
  activePanelId,
  groups,
  locale,
}: {
  activePanelId: string;
  groups: SettingsNavGroup[];
  locale: DisplayLocale;
}) {
  return (
    <aside className="workspace-settings-secondary-nav" aria-label={t(locale, "settings.secondaryNavigation")}>
      {groups.map((group) => (
        <div className="workspace-settings-secondary-group" key={group.id}>
          <strong>{getSettingsSectionLabel(locale, group.id)}</strong>
          <div>
            {group.items.map((item) => {
              const label = getSettingsPanelLabel(locale, item.id);
              return (
                <a
                  aria-current={activePanelId === item.id ? "page" : undefined}
                  className={activePanelId === item.id ? "is-active" : ""}
                  href={item.href}
                  key={item.id}
                  title={`${label}: ${getSettingsStatusDisplayLabel(locale, item.status)}`}
                >
                  <span>{label}</span>
                  <small>{getSettingsStatusDisplayLabel(locale, item.status)}</small>
                </a>
              );
            })}
          </div>
        </div>
      ))}
    </aside>
  );
}

function PersonalPreferencesPanel({
  locale,
  setLocale,
}: {
  locale: DisplayLocale;
  setLocale: (locale: DisplayLocale) => void;
}) {
  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.preferences")} />
      <div className="workspace-settings-card-grid is-two">
        <LanguageRegionCard locale={locale} setLocale={setLocale} />
      </div>
    </section>
  );
}

function GeneralSettingsTab({
  general,
  locale,
  mapStatus,
  spaceSummary,
  status,
}: {
  general: ReturnType<typeof toWorkspaceGeneralSettings> | null;
  locale: DisplayLocale;
  mapStatus: SettingsDataStatus;
  spaceSummary: ReturnType<typeof toSpaceSettingsSummary>;
  status: SettingsDataStatus;
}) {
  if (!general) {
    return (
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.general")} />
        <SettingsEmptyState label={getUnavailableLabel(status, t(locale, "settings.heading"), locale)} />
      </section>
    );
  }

  return (
    <>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.general")} />
        <div className="workspace-settings-card-grid is-two">
          <SettingsCard
            icon={<Settings className="h-4 w-4" />}
            status="live"
            title={t(locale, "settings.workspaceTitle")}
          >
            <DefinitionList
              rows={[
                [t(locale, "settings.name"), general.workspaceName],
                [t(locale, "settings.workspaceId"), general.workspaceId],
                [t(locale, "settings.currentLibraryLabel"), general.activeSpaceName],
                [t(locale, "settings.update"), t(locale, "settings.updateDeferred")],
              ]}
            />
            <p className="workspace-settings-note">{t(locale, "settings.workspaceProfileReadOnlyHelp")}</p>
          </SettingsCard>
          <SettingsCard
            icon={<Boxes className="h-4 w-4" />}
            status="reused"
            title={t(locale, "settings.currentLibrarySummary")}
          >
            {spaceSummary ? (
              <>
                <DefinitionList
                  rows={[
                    [t(locale, "settings.name"), spaceSummary.spaceName],
                    [t(locale, "settings.spaceId"), spaceSummary.spaceId],
                    [t(locale, "nav.currentLibraryCollections"), String(spaceSummary.collectionCount)],
                    [t(locale, "library.documents"), String(spaceSummary.documentCount)],
                    ["Map status", statusLabel(mapStatus, locale)],
                  ]}
                />
                <p className="workspace-settings-note">{t(locale, "settings.libraryOperationsSurfaceHelp")}</p>
                <div className="workspace-settings-card-actions">
                  <a href={spaceSummary.libraryHref}>{t(locale, "settings.openLibrary")}</a>
                </div>
              </>
            ) : (
              <SettingsEmptyState label={getUnavailableLabel(mapStatus, t(locale, "settings.currentLibrarySummary"), locale)} />
            )}
          </SettingsCard>
          <SettingsCard
            icon={<BookOpen className="h-4 w-4" />}
            status="live"
            title={t(locale, "settings.availableLibraries")}
          >
            <div className="workspace-settings-link-list">
              {general.spaces.map((space) => (
                <a href={space.href} key={space.id} title={space.name}>
                  <span>
                    <strong>{space.name}</strong>
                    <small>{space.isCurrent ? t(locale, "settings.currentLibraryLabel") : t(locale, "settings.manageInLibrary")}</small>
                  </span>
                  <ChevronRight className="h-4 w-4" />
                </a>
              ))}
            </div>
          </SettingsCard>
        </div>
      </section>
    </>
  );
}

function LanguageRegionCard({
  locale,
  setLocale,
}: {
  locale: DisplayLocale;
  setLocale: (locale: DisplayLocale) => void;
}) {
  return (
    <SettingsCard
      icon={<Database className="h-4 w-4" />}
      status="live"
      title={t(locale, "settings.languageAndRegion")}
    >
      <div className="workspace-settings-language-panel">
        <label>
          <span>{t(locale, "common.displayLanguage")}</span>
          <select
            className="workspace-settings-language-select"
            onChange={(event) => setLocale(event.currentTarget.value as DisplayLocale)}
            value={locale}
          >
            {getDisplayLanguageOptions().map((option) => (
              <option key={option.locale} value={option.locale}>
                {option.locale === "zh-CN" ? t(locale, "common.languageChinese") : t(locale, "common.languageEnglish")}
              </option>
            ))}
          </select>
        </label>
        <p className="workspace-settings-note">{t(locale, "settings.displayLanguageHelp")}</p>
        <p className="workspace-settings-note">{t(locale, "settings.languageLocalBacked")}</p>
        <p className="workspace-settings-note">{t(locale, "settings.personalPreferenceLocalHelp")}</p>
      </div>
      <DefinitionList
        rows={[
          [t(locale, "settings.timezone"), t(locale, "settings.defaultTimezone")],
          [t(locale, "settings.dateFormat"), t(locale, "settings.defaultDateFormat")],
          [t(locale, "settings.numberFormat"), t(locale, "settings.defaultNumberFormat")],
        ]}
      />
    </SettingsCard>
  );
}

function LibraryGeneralSettingsTab({
  general,
  mapStatus,
  status,
}: {
  general: ReturnType<typeof toLibraryGeneralSettings> | null;
  mapStatus: SettingsDataStatus;
  status: SettingsDataStatus;
}) {
  const { locale } = useDisplayLanguage();

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.general")} />
      {general ? (
        <div className="workspace-settings-card-grid is-two">
          <SettingsCard icon={<BookOpen className="h-4 w-4" />} status="live" title={general.spaceName}>
            <DefinitionList
              rows={[
                [t(locale, "settings.name"), general.spaceName],
                [t(locale, "settings.spaceId"), general.spaceId],
                [t(locale, "settings.workspaceTitle"), general.workspaceName],
                [t(locale, "settings.currentLibraryLabel"), general.isCurrentLibrary ? t(locale, "common.configured") : t(locale, "common.waiting")],
                [t(locale, "settings.update"), t(locale, "settings.updateDeferred")],
                ["Map status", statusLabel(mapStatus, locale)],
              ]}
            />
            <div className="workspace-settings-card-actions">
              <a href={general.libraryHref}>{t(locale, "settings.openLibrary")}</a>
            </div>
          </SettingsCard>
          <SettingsCard icon={<Layers3 className="h-4 w-4" />} status="live" title={t(locale, "settings.collections")}>
            <DefinitionList
              rows={[
                [t(locale, "nav.currentLibraryCollections"), String(general.collectionCount)],
                [t(locale, "settings.totalDocuments"), String(general.documentCount)],
                [t(locale, "settings.manageInLibrary"), t(locale, "settings.status.reused")],
              ]}
            />
            <div className="workspace-settings-card-actions">
              <a href={general.libraryHref}>{t(locale, "settings.manageInLibrary")}</a>
            </div>
          </SettingsCard>
        </div>
      ) : (
        <SettingsEmptyState label={getUnavailableLabel(status, t(locale, "settings.libraryHeading"), locale)} />
      )}
    </section>
  );
}

function LibraryCollectionsSettingsTab({
  libraryHref,
  mapStatus,
  rows,
}: {
  libraryHref: string;
  mapStatus: SettingsDataStatus;
  rows: ReturnType<typeof toLibrarySettingsCollectionRows>;
}) {
  const { locale } = useDisplayLanguage();

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.collections")} />
      <SettingsCard icon={<Layers3 className="h-4 w-4" />} status={mapStatus === "ready" ? "reused" : "unavailable"} title={t(locale, "settings.collectionSummary")}>
        <p className="workspace-settings-note">{t(locale, "settings.libraryOperationsSurfaceHelp")}</p>
        {mapStatus === "ready" && rows.length > 0 ? (
          <div className="workspace-settings-link-list">
            {rows.map((row) => (
              <a href={row.href} key={row.id} title={row.title}>
                <span>
                  <strong>{row.title}</strong>
                  <small>
                    {t(locale, "settings.position")} {row.position} | {row.documentCount} {t(locale, "library.documents")} | {t(locale, "settings.sortOrder")} {row.sortOrder}
                  </small>
                </span>
                <ChevronRight className="h-4 w-4" />
              </a>
            ))}
          </div>
        ) : (
          <SettingsEmptyState label={mapStatus === "ready" ? t(locale, "settings.noCollections") : getUnavailableLabel(mapStatus, t(locale, "settings.collections"), locale)} />
        )}
        <div className="workspace-settings-card-actions">
          <a href={libraryHref}>{t(locale, "settings.manageInLibrary")}</a>
        </div>
      </SettingsCard>
    </section>
  );
}

function LibraryDocumentsSettingsTab({
  libraryHref,
  mapStatus,
  summary,
}: {
  libraryHref: string;
  mapStatus: SettingsDataStatus;
  summary: ReturnType<typeof toLibrarySettingsDocumentSummary>;
}) {
  const { locale } = useDisplayLanguage();

  return (
    <>
      <section className="permission-admin-section">
        <div className="permission-admin-summary-grid workspace-settings-status-grid">
          <SettingsMetric label={t(locale, "settings.totalDocuments")} value={String(summary.totalDocuments)} />
          <SettingsMetric label={t(locale, "settings.drafts")} value={String(summary.draftCount)} />
          <SettingsMetric label={t(locale, "settings.published")} value={String(summary.publishedCount)} />
          <SettingsMetric label={t(locale, "settings.archived")} value={String(summary.archivedCount)} />
        </div>
      </section>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.recentDocuments")} />
        <SettingsCard icon={<FileText className="h-4 w-4" />} status={mapStatus === "ready" ? "reused" : "unavailable"} title={t(locale, "settings.documentSummary")}>
          <p className="workspace-settings-note">{t(locale, "settings.libraryOperationsSurfaceHelp")}</p>
          {mapStatus === "ready" && summary.recentDocuments.length > 0 ? (
            <div className="workspace-settings-link-list">
              {summary.recentDocuments.map((document) => (
                <a href={document.href} key={document.id} title={document.title}>
                  <span>
                    <strong>{document.title}</strong>
                    <small>{document.collectionTitle} | {document.status} | {new Date(document.updatedAt).toLocaleDateString(locale)}</small>
                  </span>
                  <ChevronRight className="h-4 w-4" />
                </a>
              ))}
            </div>
          ) : (
            <SettingsEmptyState label={mapStatus === "ready" ? t(locale, "settings.noDocuments") : getUnavailableLabel(mapStatus, t(locale, "settings.documents"), locale)} />
          )}
          <div className="workspace-settings-card-actions">
            <a href={libraryHref}>{t(locale, "settings.openLibrary")}</a>
          </div>
        </SettingsCard>
      </section>
    </>
  );
}

function LibraryPermissionsSettingsTab() {
  const { locale } = useDisplayLanguage();
  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.permissions")} />
      <div className="workspace-settings-card-grid is-two">
        <SettingsCard icon={<ShieldCheck className="h-4 w-4" />} status="reused" title={t(locale, "settings.roleBoundaries")}>
          <p className="workspace-settings-note">{t(locale, "settings.libraryPermissionsBoundaryHelp")}</p>
          <DefinitionList
            rows={[
              [t(locale, "settings.members"), t(locale, "settings.membersManagedInWorkspaceSettings")],
              [t(locale, "settings.documentPermissions"), t(locale, "settings.documentPermissionsManagedFromDocument")],
              [t(locale, "settings.accessRequests"), t(locale, "settings.accessRequestsManagedInUpdates")],
            ]}
          />
          <div className="workspace-settings-card-actions">
            <a href={createSettingsHash({ scope: "workspace", tab: "members" })}>{t(locale, "settings.openWorkspaceMembers")}</a>
            <a href="#updates?tab=access">{t(locale, "settings.openWorkspaceNotifications")}</a>
          </div>
        </SettingsCard>
        <SettingsCard icon={<LockKeyhole className="h-4 w-4" />} status="deferred" title={t(locale, "settings.libraryHeading")}>
          <p className="workspace-settings-note">{t(locale, "settings.libraryLevelPermissions")}</p>
        </SettingsCard>
      </div>
    </section>
  );
}

function LibraryNotificationsSettingsTab({
  preferenceStatus,
  rows,
}: {
  preferenceStatus: NotificationApiStatus;
  rows: ReturnType<typeof toLibraryNotificationPreferenceRows>;
}) {
  const { locale } = useDisplayLanguage();
  const watchedRows = rows.filter((row) => row.state === "watched");
  const mutedRows = rows.filter((row) => row.state === "muted");

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.notifications")} />
      <div className="workspace-settings-card-grid is-two">
        <SettingsCard icon={<Bell className="h-4 w-4" />} status={preferenceStatus === "ready" ? "live" : "unavailable"} title={t(locale, "settings.watchedAndMuted")}>
          <p className="workspace-settings-note">{notificationStatusLabel(preferenceStatus)}</p>
          <p className="workspace-settings-note">{t(locale, "settings.libraryNotificationsHelp")}</p>
          <PreferenceResourceRows label="Watched" rows={watchedRows} />
          <PreferenceResourceRows label="Muted" rows={mutedRows} />
        </SettingsCard>
        <SettingsCard icon={<CircleAlert className="h-4 w-4" />} status="deferred" title={t(locale, "settings.categoryAndDigest")}>
          <DefinitionList
            rows={[
              [t(locale, "settings.categoryPreferences"), getSettingsStatusDisplayLabel(locale, "deferred")],
              [t(locale, "settings.emailDigest"), getSettingsStatusDisplayLabel(locale, "deferred")],
            ]}
          />
          <div className="workspace-settings-card-actions">
            <a href={createSettingsHash({ scope: "workspace", tab: "notifications" })}>{t(locale, "settings.openWorkspaceNotifications")}</a>
          </div>
        </SettingsCard>
      </div>
    </section>
  );
}

function LibraryAdvancedSettingsTab() {
  const { locale } = useDisplayLanguage();
  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.advanced")} />
      <SettingsCard icon={<CircleAlert className="h-4 w-4" />} status="deferred" title={t(locale, "settings.dangerZone")}>
        <DefinitionList
          rows={[
            ["Archive / delete library", getSettingsStatusDisplayLabel(locale, "deferred")],
            ["Export / import", getSettingsStatusDisplayLabel(locale, "deferred")],
            ["Link sharing", "Use existing permission/share surfaces only"],
          ]}
        />
      </SettingsCard>
    </section>
  );
}

function OrganizationOverviewTab({
  capability,
  errorMessage,
  model,
  onSave,
  state,
}: {
  capability: ReturnType<typeof toOrganizationProfileEditCapability>;
  errorMessage: string;
  model: ReturnType<typeof toOrganizationOverviewModel>;
  onSave: (request: UpdateOrganizationProfileRequest) => Promise<boolean>;
  state: ReturnType<typeof toOrganizationReadSurfaceState>;
}) {
  const { locale } = useDisplayLanguage();
  const [isEditing, setIsEditing] = useState(false);
  const [draftName, setDraftName] = useState(model?.name ?? "");
  const [draftSlug, setDraftSlug] = useState(model?.slug ?? "");
  const [clientErrors, setClientErrors] = useState<{ name?: string; slug?: string }>({});
  const isSaving = capability.mutationStatus === "saving";
  const nameErrorId = "organization-profile-name-error";
  const slugErrorId = "organization-profile-slug-error";
  const slugHelpId = "organization-profile-slug-help";

  useEffect(() => {
    if (!isEditing) {
      setDraftName(model?.name ?? "");
      setDraftSlug(model?.slug ?? "");
      setClientErrors({});
    }
  }, [isEditing, model?.name, model?.slug]);

  const startEdit = () => {
    if (!model || !capability.canEditOrganizationProfile) {
      return;
    }

    setDraftName(model.name);
    setDraftSlug(model.slug);
    setClientErrors({});
    setIsEditing(true);
  };

  const cancelEdit = () => {
    setDraftName(model?.name ?? "");
    setDraftSlug(model?.slug ?? "");
    setClientErrors({});
    setIsEditing(false);
  };

  const saveEdit = async () => {
    const prepared = prepareOrganizationProfileUpdateRequest({ name: draftName, slug: draftSlug });
    if (!prepared.isValid) {
      setClientErrors(prepared.errors);
      setDraftName(prepared.request.name);
      setDraftSlug(prepared.request.slug);
      return;
    }

    setClientErrors({});
    setDraftName(prepared.request.name);
    setDraftSlug(prepared.request.slug);
    const saved = await onSave(prepared.request);
    if (saved) {
      setIsEditing(false);
    }
  };

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.overview")} />
      {model ? (
        <div className="workspace-settings-card-grid is-two">
          <SettingsCard icon={<Database className="h-4 w-4" />} status="live" title={model.name}>
            <div className="workspace-settings-profile-summary">
              <div>
                <span className="workspace-settings-profile-eyebrow">{t(locale, "settings.orgProfile")}</span>
                <strong>{model.name}</strong>
                <p id={slugHelpId}>{t(locale, "settings.slugHelp")}</p>
              </div>
              <code className="workspace-settings-profile-slug">{model.slug}</code>
            </div>
            <dl className="workspace-settings-profile-meta">
              <div>
                <dt>{t(locale, "settings.organizationId")}</dt>
                <dd>{model.id}</dd>
              </div>
              <div>
                <dt>{t(locale, "settings.statusLabel")}</dt>
                <dd>{model.status}</dd>
              </div>
              <div>
                <dt>{t(locale, "settings.visibleWorkspaces")}</dt>
                <dd>{model.visibleWorkspaceCount}</dd>
              </div>
              <div>
                <dt>{t(locale, "settings.updatedAt")}</dt>
                <dd>{model.updatedAt}</dd>
              </div>
            </dl>
            <div className="workspace-settings-card-actions">
              {capability.canEditOrganizationProfile ? (
                <button
                  className="workspace-settings-inline-button"
                  disabled={isSaving}
                  onClick={startEdit}
                  type="button"
                >
                  <PencilLine className="h-4 w-4" />
                  {t(locale, "settings.editOrganizationProfile")}
                </button>
              ) : (
                <button
                  className="workspace-settings-inline-button"
                  disabled
                  title={getOrganizationProfileEditDisabledReason(locale, capability.editDisabledReason)}
                  type="button"
                >
                  <PencilLine className="h-4 w-4" />
                  {t(locale, "settings.renameUnavailable")}
                </button>
              )}
            </div>
            {!capability.canEditOrganizationProfile ? (
              <p className="workspace-settings-note">{getOrganizationProfileEditDisabledReason(locale, capability.editDisabledReason)}</p>
            ) : null}
            {isEditing ? (
              <div className="workspace-settings-inline-edit-panel" aria-busy={isSaving}>
                <label>
                  <span>{t(locale, "settings.organizationName")}</span>
                  <input
                    aria-describedby={clientErrors.name ? nameErrorId : undefined}
                    aria-invalid={clientErrors.name ? "true" : "false"}
                    disabled={isSaving}
                    onChange={(event) => setDraftName(event.currentTarget.value)}
                    value={draftName}
                  />
                  {clientErrors.name ? <small id={nameErrorId}>{clientErrors.name}</small> : null}
                </label>
                <label>
                  <span>{t(locale, "settings.organizationSlug")}</span>
                  <input
                    aria-describedby={clientErrors.slug ? slugErrorId : slugHelpId}
                    aria-invalid={clientErrors.slug ? "true" : "false"}
                    disabled={isSaving}
                    onChange={(event) => setDraftSlug(event.currentTarget.value)}
                    value={draftSlug}
                  />
                  {clientErrors.slug ? <small id={slugErrorId}>{clientErrors.slug}</small> : null}
                </label>
                <p className="workspace-settings-note">{t(locale, "settings.slugHelp")}</p>
                {errorMessage && capability.mutationStatus === "error" ? (
                  <p className="workspace-settings-inline-error" role="alert">{errorMessage}</p>
                ) : null}
                {capability.mutationStatus === "success" ? (
                  <p className="workspace-settings-inline-success" aria-live="polite">{t(locale, "settings.profileUpdated")}</p>
                ) : null}
                <div className="workspace-settings-card-actions">
                  <button
                    className="workspace-settings-inline-button"
                    disabled={isSaving}
                    onClick={saveEdit}
                    type="button"
                  >
                    <Save className="h-4 w-4" />
                    {isSaving ? t(locale, "library.working") : t(locale, "settings.saveChanges")}
                  </button>
                  <button
                    className="workspace-settings-inline-button is-secondary"
                    disabled={isSaving}
                    onClick={cancelEdit}
                    type="button"
                  >
                    <X className="h-4 w-4" />
                    {t(locale, "common.cancel")}
                  </button>
                </div>
              </div>
            ) : capability.mutationStatus === "success" ? (
              <p className="workspace-settings-inline-success" aria-live="polite">{t(locale, "settings.profileUpdated")}</p>
            ) : null}
          </SettingsCard>
          <SettingsCard icon={<ShieldCheck className="h-4 w-4" />} status="live" title={t(locale, "settings.readRule")}>
            <DefinitionList rows={[[t(locale, "settings.readRule"), model.readRule]]} />
            <p className="workspace-settings-note">{t(locale, "settings.organizationProfileEditHelp")}</p>
          </SettingsCard>
        </div>
      ) : (
        <SettingsEmptyState label={`${t(locale, "settings.orgProfile")}: ${getOrganizationReadSurfaceStatusLabel(locale, state)}`} />
      )}
    </section>
  );
}

function OrganizationWorkspacesTab({
  rows,
  state,
}: {
  rows: ReturnType<typeof toOrganizationWorkspaceInventoryRows>;
  state: ReturnType<typeof toOrganizationReadSurfaceState>;
}) {
  const { locale } = useDisplayLanguage();
  const provisioningActions = getOrganizationWorkspaceProvisioningActions();

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.organizationWorkspaces")} />
      {rows.length > 0 ? (
        <div className="workspace-settings-card-grid is-three">
          {rows.map((row) => (
            <SettingsCard icon={<Boxes className="h-4 w-4" />} key={row.id} status={row.switchStatus} title={row.name}>
              <DefinitionList
                rows={[
                  [t(locale, "settings.currentWorkspace"), row.isCurrentWorkspace ? t(locale, "common.yes") : t(locale, "common.no")],
                  [t(locale, "settings.workspaceId"), row.id],
                  [t(locale, "settings.slug"), row.slug],
                  [t(locale, "settings.currentRole"), row.currentUserRole],
                  [t(locale, "settings.currentSpaceId"), row.currentSpaceId],
                  [t(locale, "settings.createdAt"), row.createdAt],
                  [t(locale, "settings.workspaceSwitching"), row.switchStatusReason],
                ]}
              />
              <div className="workspace-settings-card-actions">
                {row.settingsHref ? <a href={row.settingsHref}>{t(locale, "settings.openWorkspaceSettings")}</a> : <span>{t(locale, "settings.workspaceSwitchingDeferred")}</span>}
              </div>
            </SettingsCard>
          ))}
          <SettingsCard icon={<LockKeyhole className="h-4 w-4" />} status="deferred" title={t(locale, "settings.workspaceProvisioning")}>
            <DefinitionList
              rows={provisioningActions.map((action) => [
                getOrganizationDeferredActionLabel(locale, action.label),
                `${getSettingsStatusDisplayLabel(locale, action.status)}: ${getOrganizationDeferredActionReason(locale, action.label)}`,
              ])}
            />
          </SettingsCard>
        </div>
      ) : (
        <SettingsEmptyState label={`${t(locale, "settings.organizationWorkspaces")}: ${getOrganizationReadSurfaceStatusLabel(locale, state)}`} />
      )}
    </section>
  );
}

function OrganizationMembersTab({
  rows,
  state,
}: {
  rows: ReturnType<typeof toOrganizationMemberInventoryRows>;
  state: ReturnType<typeof toOrganizationReadSurfaceState>;
}) {
  const { locale } = useDisplayLanguage();
  const memberActions = getOrganizationMemberManagementActions();

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.orgGlobalMembers")} />
      {rows.length > 0 ? (
        <div className="workspace-settings-card-grid is-three">
          {rows.map((row) => (
            <SettingsCard icon={<UsersRound className="h-4 w-4" />} key={row.id} status="live" title={row.displayName}>
              <DefinitionList
                rows={[
                  [t(locale, "settings.email"), row.email],
                  [t(locale, "settings.statusLabel"), row.status],
                  [t(locale, "settings.workspaceCount"), String(row.workspaceCount)],
                  [
                    t(locale, "settings.workspaces"),
                    row.workspaces
                      .map((workspace) => `${workspace.workspaceName} (${workspace.role}/${workspace.status}, ${workspace.joinedAt})`)
                      .join(", "),
                  ],
                ]}
              />
            </SettingsCard>
          ))}
          <SettingsCard icon={<LockKeyhole className="h-4 w-4" />} status="deferred" title={t(locale, "settings.memberManagement")}>
            <DefinitionList
              rows={memberActions.map((action) => [
                getOrganizationDeferredActionLabel(locale, action.label),
                `${getSettingsStatusDisplayLabel(locale, action.status)}: ${getOrganizationDeferredActionReason(locale, action.label)}`,
              ])}
            />
          </SettingsCard>
        </div>
      ) : (
        <SettingsEmptyState label={`${t(locale, "settings.orgGlobalMembers")}: ${getOrganizationReadSurfaceStatusLabel(locale, state)}`} />
      )}
    </section>
  );
}

function OrganizationAssessmentTab({
  boundaries,
  rows,
}: {
  boundaries: ReturnType<typeof toSettingsBoundaryRows>;
  rows: ReturnType<typeof toOrganizationSettingsAssessmentRows>;
}) {
  const { locale } = useDisplayLanguage();
  const recommendedSlice = getRecommendedOrganizationSettingsSlice();

  return (
    <>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.settingsBoundaries")} />
        <div className="workspace-settings-card-grid is-two">
          {boundaries.map((boundary) => (
            boundary.href ? (
              <SettingsLinkCard
                href={boundary.href}
                icon={getBoundaryIcon(boundary.id)}
                key={boundary.id}
                status={boundary.status}
                title={getSettingsBoundaryLabel(locale, boundary.id)}
              />
            ) : (
              <SettingsCard
                icon={getBoundaryIcon(boundary.id)}
                key={boundary.id}
                status={boundary.status}
                title={getSettingsBoundaryLabel(locale, boundary.id)}
              >
                <p className="workspace-settings-note">{t(locale, "settings.systemBoundaryHelp")}</p>
              </SettingsCard>
            )
          ))}
        </div>
      </section>

      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.organizationAssessment")} />
        <div className="workspace-settings-card-grid is-three">
          {rows.map((row) => (
            <SettingsCard
              icon={<Boxes className="h-4 w-4" />}
              key={row.id}
              status={row.status}
              title={getOrganizationCapabilityLabel(locale, row.id)}
            >
              <DefinitionList
                rows={[
                  [t(locale, "settings.contractReadiness"), getOrganizationReadinessLabel(locale, row.readiness)],
                  [t(locale, "settings.currentSource"), row.currentSource],
                  [t(locale, "settings.requiredBackendContract"), row.requiredBackendContract],
                  [t(locale, "settings.requiredFrontendSurface"), row.requiredFrontendSurface],
                  [t(locale, "settings.proposedEndpoint"), row.proposedEndpoint],
                  [t(locale, "settings.proposedDto"), row.proposedDto],
                  [t(locale, "settings.implementationRisk"), getImplementationRiskLabel(locale, row.implementationRisk)],
                  [t(locale, "settings.implementationDependencies"), row.implementationDependencies],
                  [t(locale, "settings.securityNotes"), row.securityNotes],
                  [t(locale, "settings.recommendedPriority"), row.recommendedPriority],
                ]}
              />
                <p className="workspace-settings-note">
                  {row.status === "live" ? t(locale, "settings.liveReadBacked") : t(locale, "settings.proposedNotImplemented")}
                </p>
              {row.href ? (
                <div className="workspace-settings-card-actions">
                  <a href={row.href}>{t(locale, "common.open")}</a>
                </div>
              ) : null}
            </SettingsCard>
          ))}
        </div>
      </section>

      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.recommendedFirstSlice")} />
        <SettingsCard
          icon={<ShieldCheck className="h-4 w-4" />}
          status="assessment"
          title={recommendedSlice.title}
        >
          <DefinitionList
            rows={[
              [t(locale, "settings.currentSource"), recommendedSlice.capabilityIds.map((id) => getOrganizationCapabilityLabel(locale, id)).join(", ")],
              [t(locale, "settings.recommendedFirstSliceReason"), recommendedSlice.reason],
            ]}
          />
          <p className="workspace-settings-note">{t(locale, "settings.organizationAssessmentHelp")}</p>
        </SettingsCard>
      </section>
    </>
  );
}

function NotificationsSettingsTab({
  preferences,
  workspaceId,
}: {
  preferences: ReturnType<typeof useSettingsNotificationPreferences>;
  workspaceId: string | null;
}) {
  const { locale } = useDisplayLanguage();
  const model = useMemo(
    () => toNotificationSettingsModel(preferences.preferences, preferences.status),
    [preferences.preferences, preferences.status],
  );
  const workspacePreference = useMemo(
    () => toWorkspaceNotificationPreferenceModel(
      preferences.preferences,
      preferences.status,
      preferences.mutationStatus,
    ),
    [preferences.mutationStatus, preferences.preferences, preferences.status],
  );
  const watchedRows = model.resourceRows.filter((row) => row.state === "watched");
  const mutedRows = model.resourceRows.filter((row) => row.state === "muted");
  const setWorkspacePreference = (mode: WorkspaceNotificationPreferenceMode) => {
    const request = prepareWorkspaceNotificationPreferenceRequest(workspaceId, mode);
    if (!request) {
      return;
    }

    void preferences.updatePreference(request);
  };

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.notifications")} />
      <div className="workspace-settings-card-grid is-two">
        <SettingsCard
          icon={<Bell className="h-4 w-4" />}
          status={workspacePreference.canUpdate ? "live" : "unavailable"}
          title={t(locale, "settings.workspaceNotificationPreference")}
        >
          <p className="workspace-settings-note">{t(locale, "settings.notificationPreferenceHelp")}</p>
          <div className="workspace-settings-segmented-control" role="group" aria-label={t(locale, "settings.workspaceNotificationPreference")}>
            {(["default", "watched", "muted"] as const).map((mode) => (
              <button
                aria-pressed={workspacePreference.mode === mode}
                className={workspacePreference.mode === mode ? "is-active" : ""}
                disabled={!workspacePreference.canUpdate || workspacePreference.mutationStatus === "saving"}
                key={mode}
                onClick={() => setWorkspacePreference(mode)}
                type="button"
              >
                {getWorkspaceNotificationPreferenceModeLabel(locale, mode)}
              </button>
            ))}
          </div>
          {workspacePreference.updatedAt ? (
            <p className="workspace-settings-note">{t(locale, "settings.updatedAt")}: {new Date(workspacePreference.updatedAt).toLocaleString(locale)}</p>
          ) : null}
          {!workspacePreference.canUpdate ? (
            <p className="workspace-settings-note">{getWorkspaceNotificationPreferenceDisabledReasonLabel(locale, workspacePreference.disabledReason)}</p>
          ) : null}
          {preferences.mutationStatus === "saving" ? (
            <p className="workspace-settings-inline-status" aria-live="polite">{t(locale, "settings.savingPreference")}</p>
          ) : null}
          {preferences.mutationStatus === "success" ? (
            <p className="workspace-settings-inline-success" aria-live="polite">{t(locale, "settings.preferenceUpdated")}</p>
          ) : null}
          {preferences.mutationStatus === "error" && preferences.mutationError ? (
            <p className="workspace-settings-inline-error" role="alert">{preferences.mutationError}</p>
          ) : null}
        </SettingsCard>
        <SettingsCard
          icon={<Bell className="h-4 w-4" />}
          status={model.preferenceStatus === "ready" ? "live" : "unavailable"}
          title={t(locale, "settings.resourcePreferences")}
        >
          <p className="workspace-settings-note">{notificationStatusLabel(model.preferenceStatus)}</p>
          <PreferenceResourceRows label="Watched" rows={watchedRows} />
          <PreferenceResourceRows label="Muted" rows={mutedRows} />
        </SettingsCard>
        <SettingsCard icon={<CircleAlert className="h-4 w-4" />} status="deferred" title={t(locale, "settings.categoryAndDigest")}>
          <DefinitionList
            rows={[
              [t(locale, "settings.categoryPreferences"), getSettingsStatusDisplayLabel(locale, model.categoryPreferenceStatus)],
              [t(locale, "settings.emailDigest"), getSettingsStatusDisplayLabel(locale, model.emailDigestStatus)],
              [t(locale, "settings.boundary"), t(locale, "settings.notificationBoundaryHelp")],
            ]}
          />
        </SettingsCard>
      </div>
    </section>
  );
}

function WorkspaceMembersSettingsTab({
  currentRole,
  members,
  mutations,
}: {
  currentRole: CurrentWorkspaceRole;
  members: ReturnType<typeof useSettingsWorkspaceMembers>;
  mutations: ReturnType<typeof useSettingsMemberMutations>;
}) {
  const { locale } = useDisplayLanguage();
  const addCapability = getMemberActionCapability({
    action: "add-member",
    apiConfigured: mutations.canUse,
    currentRole,
    operation: mutations.operation,
    status: toPermissionAdminStatus(members.status),
  });

  return (
    <>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.members")} />
        <SettingsCard icon={<UsersRound className="h-4 w-4" />} status={members.status === "ready" ? "live" : "unavailable"} title="Workspace members">
          <p className="workspace-settings-note">Workspace members are managed only in Workspace Settings. Organization members are not shown in this workspace-scoped list.</p>
          <AddWorkspaceMemberInline
            capabilityReason={addCapability.reason}
            canUse={addCapability.canUse}
            mutations={mutations}
          />
          <WorkspaceMembersTable
            currentRole={currentRole}
            members={members.members}
            mutations={mutations}
            status={members.status}
          />
        </SettingsCard>
      </section>
    </>
  );
}

function AddWorkspaceMemberInline({
  capabilityReason,
  canUse,
  mutations,
}: {
  capabilityReason: string | null;
  canUse: boolean;
  mutations: ReturnType<typeof useSettingsMemberMutations>;
}) {
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("viewer");
  const isValid = email.trim().includes("@") && ["admin", "editor", "viewer"].includes(role);

  const submit = () => {
    if (!canUse || !isValid) {
      return;
    }

    void mutations.addMember(email.trim(), role).then((created) => {
      if (created) {
        setEmail("");
        setRole("viewer");
      }
    });
  };

  return (
    <div className="permission-admin-form">
      <div className="permission-admin-form-grid is-member">
        <Field label="Email" htmlFor="workspace-settings-member-email">
          <input
            disabled={!canUse}
            id="workspace-settings-member-email"
            onChange={(event) => setEmail(event.currentTarget.value)}
            placeholder="user@example.com"
            type="email"
            value={email}
          />
        </Field>
        <Field label="Workspace role" htmlFor="workspace-settings-member-role">
          <select
            disabled={!canUse}
            id="workspace-settings-member-role"
            onChange={(event) => setRole(event.currentTarget.value)}
            value={role}
          >
            {["viewer", "editor", "admin"].map((roleOption) => (
              <option key={roleOption} value={roleOption}>
                {formatSettingsValue(roleOption)}
              </option>
            ))}
          </select>
        </Field>
        <div className="permission-admin-policy-note">
          <strong>Disabled reason</strong>
          <span>{canUse ? "Backend last-owner and step-up checks still apply." : capabilityReason ?? "Member management is unavailable."}</span>
        </div>
        <button
          className="permission-admin-primary-action"
          disabled={!canUse || !isValid || mutations.operation === "add-member"}
          onClick={submit}
          title={canUse ? "Add workspace member" : capabilityReason ?? "Member management is unavailable"}
          type="button"
        >
          <UsersRound className="h-4 w-4" />
          {mutations.operation === "add-member" ? "Adding" : "Add member"}
        </button>
      </div>
      <SettingsMutationStatus error={mutations.error} message={mutations.message} />
    </div>
  );
}

function WorkspaceMembersTable({
  currentRole,
  members,
  mutations,
  status,
}: {
  currentRole: CurrentWorkspaceRole;
  members: WorkspaceMemberDto[];
  mutations: ReturnType<typeof useSettingsMemberMutations>;
  status: SettingsDataStatus;
}) {
  const [confirmRemoveUserId, setConfirmRemoveUserId] = useState<string | null>(null);

  if (status !== "ready") {
    return <SettingsEmptyState label={getManagementStatusLabel(status, "workspace members")} />;
  }

  if (members.length === 0) {
    return <SettingsEmptyState label="No workspace members returned by the API." />;
  }

  const permissionStatus = toPermissionAdminStatus(status);

  return (
    <div className="permission-admin-table">
      <div className="permission-admin-table-head is-members">
        <span>Identity</span>
        <span>Email</span>
        <span>Role</span>
        <span>Status</span>
        <span>Joined</span>
        <span>Actions</span>
      </div>
      {members.map((member) => {
        const updateCapability = getMemberActionCapability({
          action: "update-role",
          apiConfigured: mutations.canUse,
          currentRole,
          member,
          operation: mutations.operation,
          status: permissionStatus,
        });
        const removeCapability = getMemberActionCapability({
          action: "remove-member",
          apiConfigured: mutations.canUse,
          currentRole,
          member,
          operation: mutations.operation,
          status: permissionStatus,
        });
        const confirmRemove = confirmRemoveUserId === member.userId;

        return (
          <article className="permission-admin-table-row is-members" key={member.userId}>
            <span className="permission-admin-identity">
              <SettingsAvatar initials={getInitials(member.displayName || member.email || member.userId)} tone={member.role} />
              <span className="min-w-0">
                <strong title={member.displayName || member.userId}>{member.displayName || "Unnamed user"}</strong>
                <small>{shortId(member.userId)}</small>
              </span>
            </span>
            <span className="permission-admin-cell-text">{member.email ?? "No email"}</span>
            <WorkspaceRoleInlineSelect
              disabled={!updateCapability.canUse}
              disabledReason={updateCapability.reason}
              member={member}
              operation={mutations.operation}
              onChange={(role) => mutations.updateRole(member.userId, role)}
            />
            <StatusPill label={member.status} />
            <span className="permission-admin-cell-text">{member.joinedAt ? formatDateTime(member.joinedAt) : "Unknown"}</span>
            <span className="permission-admin-row-actions">
              <button
                className="permission-admin-icon-button is-danger"
                disabled={!removeCapability.canUse}
                onClick={() => {
                  if (!confirmRemove) {
                    setConfirmRemoveUserId(member.userId);
                    return;
                  }

                  void mutations.removeMember(member.userId).then((removed) => {
                    if (removed) {
                      setConfirmRemoveUserId(null);
                    }
                  });
                }}
                title={!removeCapability.canUse ? removeCapability.reason ?? "Member removal is unavailable" : confirmRemove ? "Confirm member removal" : "Remove member"}
                type="button"
              >
                {confirmRemove ? "Confirm" : <X className="h-4 w-4" />}
              </button>
            </span>
          </article>
        );
      })}
    </div>
  );
}

function WorkspaceRoleInlineSelect({
  disabled,
  disabledReason,
  member,
  onChange,
  operation,
}: {
  disabled: boolean;
  disabledReason: string | null;
  member: WorkspaceMemberDto;
  onChange: (role: string) => Promise<WorkspaceMemberDto | null>;
  operation: string | null;
}) {
  const [draftRole, setDraftRole] = useState(member.role);

  useEffect(() => {
    setDraftRole(member.role);
  }, [member.role]);

  return (
    <select
      className="permission-admin-role-select"
      disabled={disabled || operation === member.userId}
      onChange={(event) => {
        const nextRole = event.currentTarget.value;
        setDraftRole(nextRole);
        void onChange(nextRole);
      }}
      title={disabled ? disabledReason ?? "Role update unavailable" : "Change workspace role"}
      value={draftRole}
    >
      {getRoleChangeOptions(member).map((role) => (
        <option key={role} value={role}>
          {formatSettingsValue(role)}
        </option>
      ))}
    </select>
  );
}

function IntegrationsSettingsTab({
  discovery,
  scimTokens,
  workspaceId,
}: {
  discovery: ReturnType<typeof useSettingsScimDiscovery>;
  scimTokens: ReturnType<typeof useSettingsScimTokens>;
  workspaceId: string | null;
}) {
  const { locale } = useDisplayLanguage();
  return (
    <>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.integrations")} />
        <SettingsCard icon={<Plug className="h-4 w-4" />} status={discovery.status === "ready" ? "live" : "unavailable"} title="SCIM endpoint">
          <p className="workspace-settings-note">Workspace-scoped SCIM is managed here. Organization-wide identity ownership remains separate unless a backend contract adds it.</p>
          {discovery.status !== "ready" ? (
            <p className="workspace-settings-note">{getManagementStatusLabel(discovery.status, "SCIM discovery")}</p>
          ) : null}
          {discovery.status === "ready" && workspaceId ? (
            <>
              <div className="permission-admin-endpoint">
                <Plug className="h-5 w-5" />
                <code>{`/api/v1/workspaces/${workspaceId}/scim/v2`}</code>
              </div>
              <ScimDiscoverySummary discovery={discovery} />
            </>
          ) : (
            <SettingsEmptyState label={getManagementStatusLabel(discovery.status, "SCIM discovery")} />
          )}
        </SettingsCard>
      </section>
      <section className="permission-admin-section">
        <SectionTitle title="SCIM bearer tokens" />
        <SettingsCard icon={<KeyRound className="h-4 w-4" />} status={scimTokens.status === "ready" ? "live" : "unavailable"} title="SCIM bearer tokens">
          <ScimTokenManagerInline scimTokens={scimTokens} />
        </SettingsCard>
      </section>
      <section className="permission-admin-section">
        <SettingsCard icon={<Database className="h-4 w-4" />} status="deferred" title="External providers">
          <p className="workspace-settings-note">These providers are shown as unavailable until a workspace-scoped backend contract exists. No setup toggle is enabled from this placeholder.</p>
          <DefinitionList
            rows={[
              ["OIDC / SAML redirect", "Deferred"],
              ["SMTP / production delivery", "Deferred"],
              ["SCIM bulk / complex filters", "Deferred"],
              ["SCIM delete / deactivate", "Deferred"],
            ]}
          />
        </SettingsCard>
      </section>
    </>
  );
}

function WorkspaceGroupsSummary({
  groups,
  status,
}: {
  groups: WorkspaceGroupDto[];
  status: SettingsDataStatus;
}) {
  if (status !== "ready") {
    return <SettingsEmptyState label={getManagementStatusLabel(status, "workspace groups")} />;
  }

  if (groups.length === 0) {
    return <SettingsEmptyState label="No workspace groups returned by the API." />;
  }

  return (
    <div className="permission-admin-table">
      <div className="permission-admin-table-head is-groups">
        <span>Group</span>
        <span>Type</span>
        <span>Members</span>
        <span>Status</span>
      </div>
      {groups.map((group) => (
        <article className="permission-admin-table-row is-groups" key={group.id}>
          <span className="permission-admin-identity">
            <SettingsAvatar initials={getInitials(group.name)} tone="group" />
            <span className="min-w-0">
              <strong title={group.name}>{group.name}</strong>
              <small>{group.description || shortId(group.id)}</small>
            </span>
          </span>
          <span className="permission-admin-cell-text">{formatSettingsValue(group.type)}</span>
          <span className="permission-admin-cell-text">{group.membersCount}</span>
          <StatusPill label={group.isArchived ? "archived" : "active" } />
        </article>
      ))}
    </div>
  );
}

function ScimDiscoverySummary({ discovery }: { discovery: ReturnType<typeof useSettingsScimDiscovery> }) {
  if (discovery.status !== "ready" || !discovery.config || !discovery.resourceTypes || !discovery.schemas) {
    return <SettingsEmptyState label={getManagementStatusLabel(discovery.status, "SCIM discovery")} />;
  }

  return (
    <DefinitionList
      rows={[
        ["Patch", discovery.config.patch.supported ? "Supported" : "Disabled"],
        ["Filter", discovery.config.filter.supported ? `Max ${discovery.config.filter.maxResults}` : "Disabled"],
        ["Bulk", discovery.config.bulk.supported ? "Supported" : "Deferred"],
        ["Schemas", String(discovery.schemas.totalResults)],
        ["Resource types", discovery.resourceTypes.resources.map((resource) => resource.name).join(", ") || "None"],
        ["Auth", discovery.config.authenticationSchemes[0]?.type ?? "Bearer"],
      ]}
    />
  );
}

function ScimTokenManagerInline({ scimTokens }: { scimTokens: ReturnType<typeof useSettingsScimTokens> }) {
  const [draft, setDraft] = useState({ name: "", expiresAt: "" });
  const [confirmRevokeTokenId, setConfirmRevokeTokenId] = useState<string | null>(null);
  const canMutate = scimTokens.canUse && scimTokens.status !== "forbidden";
  const isValid = draft.name.trim().length > 0;

  const submit = () => {
    if (!canMutate || !isValid) {
      return;
    }

    void scimTokens.createToken({
      expiresAt: toApiDateTime(draft.expiresAt),
      name: draft.name.trim(),
    }).then((created) => {
      if (created) {
        setDraft({ name: "", expiresAt: "" });
      }
    });
  };

  if (scimTokens.status !== "ready") {
    return <SettingsEmptyState label={getManagementStatusLabel(scimTokens.status, "SCIM tokens")} />;
  }

  return (
    <>
      <div className="permission-admin-form">
        {scimTokens.created ? (
          <div className="permission-admin-secret-box">
            <span title={scimTokens.created.rawToken}>{scimTokens.created.rawToken}</span>
            <button
              onClick={() => void navigator.clipboard?.writeText(scimTokens.created?.rawToken ?? "")}
              title="Copy one-time SCIM token"
              type="button"
            >
              Copy
            </button>
            <button onClick={scimTokens.dismissCreated} title="Dismiss one-time SCIM token" type="button">
              Dismiss
            </button>
            <small>Raw token is returned once and is not shown in token lists.</small>
          </div>
        ) : null}
        <div className="permission-admin-form-grid is-scim-token">
          <Field label="Token name" htmlFor="workspace-settings-scim-token-name">
            <input
              disabled={!canMutate}
              id="workspace-settings-scim-token-name"
              onChange={(event) => setDraft((current) => ({ ...current, name: event.currentTarget.value }))}
              placeholder="Okta production"
              type="text"
              value={draft.name}
            />
          </Field>
          <Field label="Expiry" htmlFor="workspace-settings-scim-token-expiry">
            <input
              disabled={!canMutate}
              id="workspace-settings-scim-token-expiry"
              onChange={(event) => setDraft((current) => ({ ...current, expiresAt: event.currentTarget.value }))}
              type="datetime-local"
              value={draft.expiresAt}
            />
          </Field>
          <div className="permission-admin-policy-note">
            <strong>Token safety</strong>
            <span>Lists never show raw token or hash.</span>
          </div>
          <button
            className="permission-admin-primary-action"
            disabled={!canMutate || !isValid || scimTokens.operation === "create-token"}
            onClick={submit}
            title={canMutate ? "Create SCIM bearer token" : getManagementStatusLabel(scimTokens.status, "SCIM tokens")}
            type="button"
          >
            <KeyRound className="h-4 w-4" />
            {scimTokens.operation === "create-token" ? "Creating" : "Create token"}
          </button>
        </div>
        <SettingsMutationStatus error={scimTokens.error} message={scimTokens.message} />
      </div>

      {scimTokens.tokens.length === 0 ? (
        <SettingsEmptyState label="No SCIM tokens returned by the API." />
      ) : (
        <div className="permission-admin-table">
          <div className="permission-admin-table-head is-tokens">
            <span>Name</span>
            <span>Status</span>
            <span>Expires</span>
            <span>Last used</span>
            <span>Actions</span>
          </div>
          {scimTokens.tokens.map((token) => {
            const confirmRevoke = confirmRevokeTokenId === token.id;
            return (
              <article className="permission-admin-table-row is-tokens" key={token.id}>
                <span className="permission-admin-identity">
                  <SettingsAvatar initials={getInitials(token.name)} tone="scim" />
                  <span className="min-w-0">
                    <strong title={token.name}>{token.name}</strong>
                    <small>{shortId(token.id)}</small>
                  </span>
                </span>
                <StatusPill label={getTokenStatus(token)} />
                <span className="permission-admin-cell-text">{token.expiresAt ? formatDateTime(token.expiresAt) : "No expiry"}</span>
                <span className="permission-admin-cell-text">{token.lastUsedAt ? formatDateTime(token.lastUsedAt) : "Never"}</span>
                <span className="permission-admin-row-actions">
                  <button
                    className="permission-admin-icon-button is-danger"
                    disabled={!canMutate || Boolean(token.revokedAt) || scimTokens.operation === token.id}
                    onClick={() => {
                      if (!confirmRevoke) {
                        setConfirmRevokeTokenId(token.id);
                        return;
                      }

                      void scimTokens.revokeToken(token.id).then((revoked) => {
                        if (revoked) {
                          setConfirmRevokeTokenId(null);
                        }
                      });
                    }}
                    title={!canMutate ? getManagementStatusLabel(scimTokens.status, "SCIM tokens") : token.revokedAt ? "Token already revoked" : confirmRevoke ? "Confirm token revoke" : "Revoke token"}
                    type="button"
                  >
                    {confirmRevoke ? "Confirm" : <X className="h-4 w-4" />}
                  </button>
                </span>
              </article>
            );
          })}
        </div>
      )}
    </>
  );
}

function SecuritySettingsTab({
  audit,
  security,
}: {
  audit: ReturnType<typeof useSettingsWorkspaceAudit>;
  security: ReturnType<typeof useSettingsSecurityState>;
}) {
  const { locale } = useDisplayLanguage();
  const rows = useMemo(() => toSecuritySettingsRows(security.state, security.status), [security.state, security.status]);

  return (
    <>
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.security")} />
        <div className="workspace-settings-card-grid is-two">
          <SettingsCard icon={<ShieldCheck className="h-4 w-4" />} status={security.status === "ready" ? "live" : "unavailable"} title="Auth State">
            <p className="workspace-settings-note">This is a read-only summary of the current workspace security state. New MFA, recovery, or WebAuthn flows are not enabled from this page.</p>
            <DefinitionList rows={rows.map((row) => [row.label, row.value])} />
          </SettingsCard>
          <SettingsCard icon={<LockKeyhole className="h-4 w-4" />} status="deferred" title={t(locale, "settings.flows")}>
            <DefinitionList
              rows={[
                ["MFA enrollment", "Existing backend flow only; no new UI flow here"],
                ["WebAuthn / SMS / recovery codes", "Deferred"],
                ["Admin recovery/reset", "Deferred"],
              ]}
            />
          </SettingsCard>
        </div>
      </section>

      <section className="permission-admin-section">
        <SectionTitle title="Workspace audit log" />
        <SettingsCard icon={<ScrollText className="h-4 w-4" />} status={audit.status === "ready" ? "live" : "unavailable"} title="Security and access trail">
          <p className="workspace-settings-note">
            Workspace audit tracks administrative, access, sharing, group, invite, and SCIM events. It is separate from document Activity and the Updates notification inbox.
          </p>
          <WorkspaceAuditTable audit={audit} />
        </SettingsCard>
      </section>
    </>
  );
}

function WorkspaceAuditTable({ audit }: { audit: ReturnType<typeof useSettingsWorkspaceAudit> }) {
  if (audit.status !== "ready") {
    return <SettingsEmptyState label={getManagementStatusLabel(audit.status, "workspace audit log")} />;
  }

  if (!audit.data || audit.data.events.length === 0) {
    return <SettingsEmptyState label="No workspace audit events returned by the API." />;
  }

  return (
    <>
      <div className="permission-admin-table">
        <div className="permission-admin-table-head is-audit">
          <span>Action</span>
          <span>Actor</span>
          <span>Resource</span>
          <span>When</span>
        </div>
        {audit.data.events.map((event) => (
          <article className="permission-admin-table-row is-audit" key={event.id}>
            <span className="permission-admin-identity">
              <SettingsAvatar initials={getInitials(formatAuditActor(event))} tone="audit" />
              <span className="min-w-0">
                <strong title={event.action}>{formatSettingsValue(event.action)}</strong>
                <small>{event.subjectType ? `${formatSettingsValue(event.subjectType)} ${shortId(event.subjectId ?? "")}` : "Workspace event"}</small>
              </span>
            </span>
            <span className="permission-admin-cell-text" title={event.actorEmail ?? event.actorId ?? undefined}>
              {formatAuditActor(event)}
            </span>
            <span className="permission-admin-cell-text" title={`${event.resourceType} ${event.resourceId}`}>
              {formatAuditResource(event)}
            </span>
            <span className="permission-admin-cell-text">{formatDateTime(event.createdAt)}</span>
          </article>
        ))}
      </div>
      <p className="workspace-settings-note">
        Showing {audit.data.events.length} of {audit.data.totalCount} events{audit.data.hasMore ? "; use the API for additional pages." : "."}
      </p>
    </>
  );
}

function DeferredSettingsTab({ tab }: { tab: WorkspaceSettingsTab }) {
  const { locale } = useDisplayLanguage();
  const label = getSettingsTabDisplayLabel(locale, tab);
  return (
    <section className="permission-admin-section">
      <SectionTitle title={label} />
      <SettingsEmptyState label={`${label}: ${getSettingsStatusDisplayLabel(locale, "deferred")}`} />
    </section>
  );
}

function SettingsTabStatusGrid({ locale, tabs }: { locale: DisplayLocale; tabs: ReturnType<typeof createWorkspaceSettingsTabRows> }) {
  return (
    <section className="permission-admin-section">
      <div className="permission-admin-summary-grid workspace-settings-status-grid">
        {tabs.map((tab) => (
          <SettingsMetric key={tab.id} label={getSettingsTabDisplayLabel(locale, tab.id)} value={getSettingsStatusDisplayLabel(locale, tab.status)} />
        ))}
      </div>
    </section>
  );
}

function SettingsLinkCard({
  href,
  icon,
  status,
  title,
}: {
  href: string;
  icon: ReactNode;
  status: WorkspaceSettingsStatus;
  title: string;
}) {
  const { locale } = useDisplayLanguage();
  return (
    <SettingsCard icon={icon} status={status} title={title}>
      <div className="workspace-settings-card-actions">
        <a href={href}>{t(locale, "common.open")}</a>
      </div>
    </SettingsCard>
  );
}

function SettingsCard({
  children,
  icon,
  status,
  title,
}: {
  children: ReactNode;
  icon: ReactNode;
  status: WorkspaceSettingsStatus;
  title: string;
}) {
  const { locale } = useDisplayLanguage();
  return (
    <article className="workspace-settings-card">
      <header>
        <span className="workspace-settings-card-icon">{icon}</span>
        <div className="min-w-0">
          <h3>{title}</h3>
          <small>{getSettingsStatusDisplayLabel(locale, status)}</small>
        </div>
      </header>
      {children}
    </article>
  );
}

function PreferenceResourceRows({ label, rows }: { label: string; rows: Array<{ href: string; id: string; label: string }> }) {
  return (
    <div className="workspace-settings-preference-group">
      <strong>{label}</strong>
      {rows.length === 0 ? (
        <p className="workspace-settings-note">No {label.toLowerCase()} resources</p>
      ) : (
        <div className="workspace-settings-link-list">
          {rows.slice(0, 5).map((row) => (
            <a href={row.href} key={row.id} title={row.label}>
              <span>{row.label}</span>
              <ChevronRight className="h-4 w-4" />
            </a>
          ))}
        </div>
      )}
    </div>
  );
}

function DefinitionList({ rows }: { rows: Array<[string, string]> }) {
  return (
    <dl className="workspace-settings-definition-list">
      {rows.map(([label, value]) => (
        <div key={label}>
          <dt>{label}</dt>
          <dd title={value}>{value}</dd>
        </div>
      ))}
    </dl>
  );
}

function SectionTitle({ title }: { title: string }) {
  return (
    <div className="permission-admin-section-title">
      <div>
        <h2>{title}</h2>
      </div>
    </div>
  );
}

function SettingsMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="permission-admin-metric">
      <span>{label}</span>
      <strong title={value}>{value}</strong>
    </div>
  );
}

function SettingsEmptyState({ label }: { label: string }) {
  return (
    <div className="permission-admin-empty-state">
      <CircleAlert className="h-4 w-4" />
      <span>{label}</span>
    </div>
  );
}

function Field({ children, htmlFor, label }: { children: ReactNode; htmlFor: string; label: string }) {
  return (
    <label className="permission-admin-field" htmlFor={htmlFor}>
      <span>{label}</span>
      {children}
    </label>
  );
}

function SettingsMutationStatus({ error, message }: { error?: string | null; message?: string | null }) {
  if (error) {
    return <p className="workspace-settings-inline-error" role="alert">{error}</p>;
  }

  if (message) {
    return <p className="workspace-settings-inline-success" aria-live="polite">{message}</p>;
  }

  return null;
}

function StatusPill({ label }: { label: string }) {
  return <span className="permission-admin-status-pill">{formatSettingsValue(label)}</span>;
}

function SettingsAvatar({ initials, tone }: { initials: string; tone: string }) {
  return <span className={["permission-admin-avatar", getAvatarClass(tone)].join(" ")}>{initials}</span>;
}

function useSettingsBootstrap() {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [data, setData] = useState<BootstrapResponse | null>(null);
  const [status, setStatus] = useState<SettingsDataStatus>(() => (apiBaseUrl ? "loading" : "unconfigured"));

  useEffect(() => {
    if (!apiBaseUrl) {
      setData(null);
      setStatus("unconfigured");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getBootstrap(controller.signal)
      .then((response) => {
        setData(response);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted || (error instanceof DOMException && error.name === "AbortError")) {
          return;
        }

        setData(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [apiBaseUrl]);

  return { data, status };
}

function useSettingsSpaceMap(spaceId: string | null, bootstrapStatus: SettingsDataStatus) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [data, setData] = useState<KnowledgeMapResponse | null>(null);
  const [status, setStatus] = useState<SettingsDataStatus>(() => (apiBaseUrl ? "idle" : "unconfigured"));

  useEffect(() => {
    if (!apiBaseUrl) {
      setData(null);
      setStatus("unconfigured");
      return;
    }

    if (bootstrapStatus !== "ready" || !spaceId) {
      setData(null);
      setStatus(bootstrapStatus === "loading" ? "loading" : "idle");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getSpaceMap(spaceId, controller.signal)
      .then((response) => {
        setData(response);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted || (error instanceof DOMException && error.name === "AbortError")) {
          return;
        }

        setData(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, bootstrapStatus, spaceId]);

  return { data, status };
}

function useSettingsWorkspaceMembers(workspaceId: string | null | undefined) {
  const apiBaseUrl = useMemo(() => getConfiguredPermissionAdminApiBaseUrl(), []);
  const [members, setMembers] = useState<WorkspaceMemberDto[]>([]);
  const [status, setStatus] = useState<SettingsDataStatus>(() => (apiBaseUrl && workspaceId ? "loading" : apiBaseUrl ? "idle" : "unconfigured"));

  const loadMembers = () => {
    if (!apiBaseUrl) {
      setMembers([]);
      setStatus("unconfigured");
      return undefined;
    }

    if (!workspaceId) {
      setMembers([]);
      setStatus("idle");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getPermissionWorkspaceMembers(apiBaseUrl, workspaceId, controller.signal)
      .then((response) => {
        setMembers(response.members);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted || (error instanceof DOMException && error.name === "AbortError")) {
          return;
        }

        setMembers([]);
        setStatus(error instanceof PermissionAdminApiError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return controller;
  };

  useEffect(() => {
    const controller = loadMembers();
    return () => controller?.abort();
  }, [apiBaseUrl, workspaceId]);

  return { apiBaseUrl, members, reload: loadMembers, status };
}

function useSettingsWorkspaceGroups(workspaceId: string | null | undefined) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [groups, setGroups] = useState<WorkspaceGroupDto[]>([]);
  const [status, setStatus] = useState<SettingsDataStatus>(() => (apiBaseUrl && workspaceId ? "loading" : apiBaseUrl ? "idle" : "unconfigured"));

  useEffect(() => {
    if (!apiBaseUrl) {
      setGroups([]);
      setStatus("unconfigured");
      return;
    }

    if (!workspaceId) {
      setGroups([]);
      setStatus("idle");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getPermissionWorkspaceGroups(apiBaseUrl, workspaceId, controller.signal)
      .then((response) => {
        setGroups(response.groups);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted || (error instanceof DOMException && error.name === "AbortError")) {
          return;
        }

        setGroups([]);
        setStatus(error instanceof PermissionAdminApiError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, workspaceId]);

  return { groups, status };
}

function useSettingsCurrentWorkspaceRole(workspaceId: string | null) {
  const [role, setRole] = useState<CurrentWorkspaceRole>("unknown");

  useEffect(() => {
    if (!workspaceId) {
      setRole("unknown");
      return;
    }

    let isActive = true;
    void getCurrentUser()
      .then((response) => {
        if (isActive) {
          setRole(getCurrentWorkspaceRole(response.workspaces, workspaceId));
        }
      })
      .catch(() => {
        if (isActive) {
          setRole("unknown");
        }
      });

    return () => {
      isActive = false;
    };
  }, [workspaceId]);

  return role;
}

function useSettingsMemberMutations(
  apiBaseUrl: string,
  workspaceId: string | null,
  reloadMembers: () => AbortController | undefined,
) {
  const [operation, setOperation] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const canUse = Boolean(apiBaseUrl && workspaceId);

  const addMember = async (email: string, role: string) => {
    if (!apiBaseUrl || !workspaceId || operation) {
      return null;
    }

    setOperation("add-member");
    setError(null);
    setMessage(null);
    try {
      const member = await addWorkspaceMember(apiBaseUrl, workspaceId, email, role);
      setMessage("Member added.");
      reloadMembers();
      return member;
    } catch (errorValue) {
      setError(toPermissionMutationError(errorValue, "Could not add member."));
      return null;
    } finally {
      setOperation(null);
    }
  };

  const updateRole = async (userId: string, role: string) => {
    if (!apiBaseUrl || !workspaceId || operation) {
      return null;
    }

    setOperation(userId);
    setError(null);
    setMessage(null);
    try {
      const member = await updateWorkspaceMemberRole(apiBaseUrl, workspaceId, userId, role);
      setMessage("Member role updated.");
      reloadMembers();
      return member;
    } catch (errorValue) {
      setError(toPermissionMutationError(errorValue, "Could not update member role."));
      return null;
    } finally {
      setOperation(null);
    }
  };

  const removeMember = async (userId: string) => {
    if (!apiBaseUrl || !workspaceId || operation) {
      return false;
    }

    setOperation(userId);
    setError(null);
    setMessage(null);
    try {
      await removeWorkspaceMember(apiBaseUrl, workspaceId, userId);
      setMessage("Member removed.");
      reloadMembers();
      return true;
    } catch (errorValue) {
      setError(toPermissionMutationError(errorValue, "Could not remove member."));
      return false;
    } finally {
      setOperation(null);
    }
  };

  return { addMember, canUse, error, message, operation, removeMember, updateRole };
}

function useSettingsScimDiscovery(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredPermissionAdminApiBaseUrl(), []);
  const [config, setConfig] = useState<ScimServiceProviderConfigResponse | null>(null);
  const [resourceTypes, setResourceTypes] = useState<ScimListResponse<ScimResourceTypeDto> | null>(null);
  const [schemas, setSchemas] = useState<ScimListResponse<ScimSchemaDto> | null>(null);
  const [status, setStatus] = useState<SettingsDataStatus>(() => (apiBaseUrl && workspaceId ? "loading" : apiBaseUrl ? "idle" : "unconfigured"));

  const load = () => {
    if (!apiBaseUrl) {
      setConfig(null);
      setResourceTypes(null);
      setSchemas(null);
      setStatus("unconfigured");
      return undefined;
    }

    if (!workspaceId) {
      setConfig(null);
      setResourceTypes(null);
      setSchemas(null);
      setStatus("idle");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    void Promise.all([
      getScimServiceProviderConfig(apiBaseUrl, workspaceId, controller.signal),
      getScimResourceTypes(apiBaseUrl, workspaceId, controller.signal),
      getScimSchemas(apiBaseUrl, workspaceId, controller.signal),
    ])
      .then(([serviceProviderConfig, resourceTypesResponse, schemasResponse]) => {
        setConfig(serviceProviderConfig);
        setResourceTypes(resourceTypesResponse);
        setSchemas(schemasResponse);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted || (error instanceof DOMException && error.name === "AbortError")) {
          return;
        }

        setConfig(null);
        setResourceTypes(null);
        setSchemas(null);
        setStatus(error instanceof PermissionAdminApiError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return controller;
  };

  useEffect(() => {
    const controller = load();
    return () => controller?.abort();
  }, [apiBaseUrl, workspaceId]);

  return { config, reload: load, resourceTypes, schemas, status };
}

function useSettingsScimTokens(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredPermissionAdminApiBaseUrl(), []);
  const [tokens, setTokens] = useState<ScimTokenDto[]>([]);
  const [created, setCreated] = useState<CreateScimTokenResponse | null>(null);
  const [operation, setOperation] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [status, setStatus] = useState<SettingsDataStatus>(() => (apiBaseUrl && workspaceId ? "loading" : apiBaseUrl ? "idle" : "unconfigured"));
  const canUse = Boolean(apiBaseUrl && workspaceId);

  const load = () => {
    if (!apiBaseUrl) {
      setTokens([]);
      setStatus("unconfigured");
      return undefined;
    }

    if (!workspaceId) {
      setTokens([]);
      setStatus("idle");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getScimTokens(apiBaseUrl, workspaceId, controller.signal)
      .then((body) => {
        setTokens(body.tokens);
        setStatus("ready");
      })
      .catch((errorValue: unknown) => {
        if (controller.signal.aborted || (errorValue instanceof DOMException && errorValue.name === "AbortError")) {
          return;
        }

        setTokens([]);
        setStatus(errorValue instanceof PermissionAdminApiError && (errorValue.status === 401 || errorValue.status === 403) ? "forbidden" : "error");
      });

    return controller;
  };

  useEffect(() => {
    const controller = load();
    return () => controller?.abort();
  }, [apiBaseUrl, workspaceId]);

  const createToken = async (request: { name: string; expiresAt: string | null }) => {
    if (!apiBaseUrl || !workspaceId || operation) {
      return null;
    }

    setOperation("create-token");
    setError(null);
    setMessage(null);
    try {
      const response = await createScimToken(apiBaseUrl, workspaceId, request);
      setCreated(response);
      setMessage("SCIM token created.");
      load();
      return response;
    } catch (errorValue) {
      setError(toPermissionMutationError(errorValue, "Could not create SCIM token."));
      return null;
    } finally {
      setOperation(null);
    }
  };

  const revokeToken = async (tokenId: string) => {
    if (!apiBaseUrl || !workspaceId || operation) {
      return false;
    }

    setOperation(tokenId);
    setError(null);
    setMessage(null);
    try {
      await revokeScimToken(apiBaseUrl, workspaceId, tokenId);
      setTokens((current) =>
        current.map((token) => (token.id === tokenId ? { ...token, revokedAt: new Date().toISOString() } : token)),
      );
      setMessage("SCIM token revoked.");
      return true;
    } catch (errorValue) {
      setError(toPermissionMutationError(errorValue, "Could not revoke SCIM token."));
      return false;
    } finally {
      setOperation(null);
    }
  };

  return {
    canUse,
    createToken,
    created,
    dismissCreated: () => setCreated(null),
    error,
    message,
    operation,
    reload: load,
    revokeToken,
    status,
    tokens,
  };
}

function useSettingsNotificationPreferences(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [preferences, setPreferences] = useState<PermissionNotificationPreferenceDto[]>([]);
  const [status, setStatus] = useState<NotificationApiStatus>(() => (apiBaseUrl && workspaceId ? "loading" : "unconfigured"));
  const [mutationStatus, setMutationStatus] = useState<WorkspaceNotificationPreferenceMutationStatus>("idle");
  const [mutationError, setMutationError] = useState("");

  useEffect(() => {
    if (!apiBaseUrl || !workspaceId) {
      setPreferences([]);
      setStatus("unconfigured");
      setMutationStatus("idle");
      setMutationError("");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getWorkspaceNotificationPreferences(workspaceId, controller.signal)
      .then((body) => {
        setPreferences(body.preferences);
        setStatus("ready");
        setMutationError("");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setPreferences([]);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, workspaceId]);

  const updatePreference = async (request: Parameters<typeof updateWorkspaceNotificationPreference>[0]) => {
    if (!apiBaseUrl || !workspaceId) {
      setMutationStatus("error");
      setMutationError("API and workspace id are required to update notification preferences.");
      return false;
    }

    setMutationStatus("saving");
    setMutationError("");
    try {
      const updated = await updateWorkspaceNotificationPreference(request);
      setPreferences((current) => upsertPreference(current, updated));
      setStatus("ready");
      setMutationStatus("success");
      return true;
    } catch (error) {
      setMutationStatus("error");
      setMutationError(toWorkspaceNotificationPreferenceMutationError(error));
      return false;
    }
  };

  return { mutationError, mutationStatus, preferences, status, updatePreference };
}

function upsertPreference(
  preferences: PermissionNotificationPreferenceDto[],
  updated: PermissionNotificationPreferenceDto,
) {
  const index = preferences.findIndex((preference) => preference.id === updated.id);
  if (index === -1) {
    return [...preferences, updated];
  }

  return preferences.map((preference, preferenceIndex) => (preferenceIndex === index ? updated : preference));
}

function useSettingsSecurityState() {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [state, setState] = useState<AuthSecurityStateResponse | null>(null);
  const [status, setStatus] = useState<SecurityStatus>(() => (apiBaseUrl ? "loading" : "unconfigured"));

  useEffect(() => {
    if (!apiBaseUrl) {
      setState(null);
      setStatus("unconfigured");
      return;
    }

    setStatus("loading");
    let isActive = true;
    void getSecurityState()
      .then((response) => {
        if (isActive) {
          setState(response);
          setStatus("ready");
        }
      })
      .catch((error: unknown) => {
        if (!isActive) {
          return;
        }

        setState(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => {
      isActive = false;
    };
  }, [apiBaseUrl]);

  return { state, status };
}

function useSettingsWorkspaceAudit(workspaceId: string | null, enabled: boolean) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [data, setData] = useState<WorkspaceAuditLogResponse | null>(null);
  const [status, setStatus] = useState<SettingsDataStatus>(() => (apiBaseUrl && workspaceId ? "idle" : apiBaseUrl ? "idle" : "unconfigured"));

  useEffect(() => {
    if (!enabled) {
      return;
    }

    if (!apiBaseUrl) {
      setData(null);
      setStatus("unconfigured");
      return;
    }

    if (!workspaceId) {
      setData(null);
      setStatus("idle");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getWorkspaceAuditLog(workspaceId, { limit: 10 }, controller.signal)
      .then((response) => {
        setData(response);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted || (error instanceof DOMException && error.name === "AbortError")) {
          return;
        }

        setData(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, enabled, workspaceId]);

  return { data, status };
}

function useSettingsOrganizationData(organizationId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [profile, setProfile] = useState<OrganizationProfileResponse | null>(null);
  const [members, setMembers] = useState<OrganizationMembersResponse | null>(null);
  const [profileStatus, setProfileStatus] = useState<OrganizationReadApiStatus>(() => (apiBaseUrl ? "loading" : "unconfigured"));
  const [membersStatus, setMembersStatus] = useState<OrganizationReadApiStatus>(() => (apiBaseUrl ? "loading" : "unconfigured"));
  const [profileMutationStatus, setProfileMutationStatus] = useState<"error" | "idle" | "saving" | "success">("idle");
  const [profileMutationError, setProfileMutationError] = useState("");

  useEffect(() => {
    if (!apiBaseUrl || !organizationId) {
      setProfile(null);
      setMembers(null);
      setProfileStatus("unconfigured");
      setMembersStatus("unconfigured");
      setProfileMutationStatus("idle");
      setProfileMutationError("");
      return;
    }

    const controller = new AbortController();
    setProfileStatus("loading");
    setMembersStatus("loading");
    setProfileMutationStatus("idle");
    setProfileMutationError("");

    void getOrganizationProfile(organizationId, controller.signal)
      .then((response) => {
        setProfile(response);
        setProfileStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setProfile(null);
        setProfileStatus(toOrganizationReadApiStatus(error));
      });

    void getOrganizationMembers(organizationId, controller.signal)
      .then((response) => {
        setMembers(response);
        setMembersStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setMembers(null);
        setMembersStatus(toOrganizationReadApiStatus(error));
      });

    return () => controller.abort();
  }, [apiBaseUrl, organizationId]);

  const updateProfile = async (request: UpdateOrganizationProfileRequest) => {
    if (!apiBaseUrl || !organizationId) {
      setProfileMutationStatus("error");
      setProfileMutationError("API base URL or organization id is not configured.");
      return false;
    }

    setProfileMutationStatus("saving");
    setProfileMutationError("");
    try {
      const response = await updateOrganizationProfile(organizationId, request);
      setProfile(response);
      setProfileStatus("ready");
      setProfileMutationStatus("success");
      return true;
    } catch (error) {
      setProfileMutationStatus("error");
      setProfileMutationError(error instanceof Error ? error.message : "Organization profile update failed.");
      return false;
    }
  };

  return {
    members,
    membersStatus,
    profile,
    profileMutationError,
    profileMutationStatus,
    profileStatus,
    updateProfile,
  };
}

function toOrganizationReadApiStatus(error: unknown): OrganizationReadApiStatus {
  if (error instanceof ApiClientError) {
    if (error.status === 401 || error.status === 403) {
      return "forbidden";
    }

    if (error.status === 404) {
      return "not-found";
    }
  }

  return "error";
}

function getSelectedSpaceId(bootstrap: BootstrapResponse | null, requestedSpaceId: string | null) {
  if (!bootstrap) {
    return requestedSpaceId;
  }

  if (requestedSpaceId && bootstrap.spaces.some((space) => space.id === requestedSpaceId)) {
    return requestedSpaceId;
  }

  return bootstrap.activeSpaceId || bootstrap.workspace.currentSpaceId || (bootstrap.spaces[0]?.id ?? null);
}

function getSettingsCenterHeadingStatus(status: SettingsDataStatus, locale: DisplayLocale) {
  if (status === "ready") {
    return t(locale, "settings.headingReady");
  }

  return getUnavailableLabel(status, t(locale, "settings.heading"), locale);
}

function getSettingsSectionLabel(locale: DisplayLocale, sectionId: string) {
  switch (sectionId) {
    case "deferred":
      return t(locale, "settings.scopeDeferred");
    case "organization":
      return t(locale, "settings.scopeOrganization");
    case "personal":
      return t(locale, "settings.scopePersonal");
    case "workspace":
    default:
      return t(locale, "settings.scopeWorkspace");
  }
}

function getSettingsPanelLabel(locale: DisplayLocale, panelId: string) {
  switch (panelId) {
    case "personal-preferences":
      return t(locale, "settings.preferences");
    case "workspace-notifications":
      return t(locale, "settings.notifications");
    case "workspace-access-identity":
      return t(locale, "settings.permissions");
    case "workspace-members":
      return t(locale, "settings.members");
    case "workspace-permissions":
      return t(locale, "settings.permissions");
    case "workspace-security":
      return t(locale, "settings.security");
    case "workspace-integrations":
      return t(locale, "settings.integrations");
    case "organization-profile":
      return t(locale, "settings.orgProfile");
    case "organization-workspaces":
      return t(locale, "settings.workspaces");
    case "organization-members":
      return t(locale, "settings.membersInventory");
    case "deferred-plan":
      return t(locale, "settings.plan");
    case "deferred-developer":
      return t(locale, "settings.developer");
    case "workspace-general":
    default:
      return t(locale, "settings.general");
  }
}

function getUnavailableLabel(status: SettingsDataStatus, noun: string, locale: DisplayLocale) {
  if (status === "loading") {
    return locale === "zh-CN" ? `\u6b63\u5728\u52a0\u8f7d${noun}\u3002` : `Loading ${noun.toLowerCase()}.`;
  }

  if (status === "forbidden") {
    return locale === "zh-CN" ? `\u5f53\u524d\u4f1a\u8bdd\u4e0d\u53ef\u8bbf\u95ee\uff1a${noun}\u3002` : `${noun} are not available for this session.`;
  }

  if (status === "error") {
    return locale === "zh-CN" ? `${noun} \u65e0\u6cd5\u4ece\u5df2\u914d\u7f6e API \u52a0\u8f7d\u3002` : `${noun} could not be loaded from the configured API.`;
  }

  if (status === "idle") {
    return locale === "zh-CN" ? `${noun} \u6b63\u5728\u7b49\u5f85\u5de5\u4f5c\u533a\u6570\u636e\u3002` : `${noun} are waiting for workspace data.`;
  }

  return locale === "zh-CN" ? `\u914d\u7f6e API \u540e\u52a0\u8f7d${noun}\u3002` : `Configure the API to load ${noun.toLowerCase()}.`;
}

function notificationStatusLabel(status: NotificationApiStatus) {
  if (status === "ready") {
    return "Resource watch and mute preferences are live-backed.";
  }

  if (status === "loading") {
    return "Loading live notification preferences.";
  }

  if (status === "forbidden") {
    return "Notification preference access is unavailable for this session.";
  }

  if (status === "error") {
    return "Notification preferences could not be loaded from the configured API.";
  }

  return "Configure the API and workspace id to load notification preferences.";
}

function getWorkspaceNotificationPreferenceModeLabel(
  locale: DisplayLocale,
  mode: WorkspaceNotificationPreferenceMode,
) {
  if (mode === "watched") {
    return t(locale, "settings.workspaceNotificationWatched");
  }

  if (mode === "muted") {
    return t(locale, "settings.workspaceNotificationMuted");
  }

  return t(locale, "settings.workspaceNotificationDefault");
}

function getWorkspaceNotificationPreferenceDisabledReasonLabel(locale: DisplayLocale, reason: string) {
  if (reason === "Loading notification preferences.") {
    return t(locale, "settings.loadingNotificationPreferences");
  }

  if (reason === "Notification preference access is unavailable for this user.") {
    return t(locale, "settings.notificationPreferenceForbidden");
  }

  if (reason === "Notification preference API failed.") {
    return t(locale, "settings.notificationPreferenceApiFailed");
  }

  return t(locale, "settings.preferenceUpdateUnavailable");
}

function statusLabel(status: SettingsDataStatus, locale: DisplayLocale) {
  if (status === "ready") {
    return locale === "zh-CN" ? "就绪" : "Ready";
  }

  if (status === "loading") {
    return t(locale, "common.loading");
  }

  if (status === "forbidden") {
    return t(locale, "common.forbidden");
  }

  if (status === "error") {
    return t(locale, "common.unavailable");
  }

  if (status === "idle") {
    return t(locale, "common.waiting");
  }

  return t(locale, "common.notConnected");
}

function toPermissionAdminStatus(status: SettingsDataStatus): "error" | "forbidden" | "loading" | "ready" | "unconfigured" {
  return status === "idle" ? "unconfigured" : status;
}

function getManagementStatusLabel(status: SettingsDataStatus, noun: string) {
  if (status === "ready") {
    return `${noun} are ready.`;
  }

  if (status === "loading") {
    return `Loading ${noun}.`;
  }

  if (status === "forbidden") {
    return `${noun} are forbidden for this session.`;
  }

  if (status === "error") {
    return `${noun} could not be loaded from the configured API.`;
  }

  if (status === "idle") {
    return `${noun} are waiting for workspace data.`;
  }

  return `Configure API and workspace id to load ${noun}.`;
}

function shortId(value: string) {
  return value.length <= 12 ? value : `${value.slice(0, 8)}...${value.slice(-4)}`;
}

function formatAuditActor(event: WorkspaceAuditEventDto) {
  if (event.actorName?.trim()) {
    return event.actorName.trim();
  }

  if (event.actorEmail?.trim()) {
    return event.actorEmail.trim();
  }

  return event.actorId ? `User ${shortId(event.actorId)}` : "System";
}

function formatAuditResource(event: WorkspaceAuditEventDto) {
  return `${formatSettingsValue(event.resourceType)} ${shortId(event.resourceId)}`;
}

function getInitials(value: string) {
  const words = value.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) {
    return "NA";
  }

  return words.slice(0, 2).map((word) => word[0]?.toUpperCase() ?? "").join("") || "NA";
}

function getAvatarClass(tone: string) {
  const normalized = tone.toLowerCase();
  if (normalized === "owner" || normalized === "admin" || normalized === "scim") {
    return "is-owner";
  }

  if (normalized === "editor" || normalized === "group") {
    return "is-editor";
  }

  return "is-viewer";
}

function formatSettingsValue(value: string) {
  return value
    .split(/[-_\s]+/)
    .filter(Boolean)
    .map((part) => `${part.slice(0, 1).toUpperCase()}${part.slice(1).toLowerCase()}`)
    .join(" ");
}

function formatDateTime(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}

function toApiDateTime(value: string) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function getTokenStatus(token: ScimTokenDto) {
  if (token.revokedAt) {
    return "revoked";
  }

  if (token.expiresAt && new Date(token.expiresAt).getTime() <= Date.now()) {
    return "expired";
  }

  return "active";
}

function getSettingsTabDisplayLabel(locale: DisplayLocale, tabId: string) {
  switch (tabId) {
    case "advanced":
      return t(locale, "settings.advanced");
    case "collections":
      return t(locale, "settings.collections");
    case "documents":
      return t(locale, "settings.documents");
    case "members":
      return t(locale, "settings.members");
    case "notifications":
      return t(locale, "settings.notifications");
    case "overview":
      return t(locale, "settings.overview");
    case "workspaces":
      return t(locale, "settings.workspaces");
    case "assessment":
      return t(locale, "settings.assessment");
    case "permissions":
      return t(locale, "settings.permissions");
    case "integrations":
      return t(locale, "settings.integrations");
    case "security":
      return t(locale, "settings.security");
    case "plan":
      return t(locale, "settings.plan");
    case "developer":
      return t(locale, "settings.developer");
    case "general":
    default:
      return t(locale, "settings.general");
  }
}

function getBoundaryIcon(id: string) {
  if (id === "workspace") {
    return <Settings className="h-4 w-4" />;
  }

  if (id === "library") {
    return <BookOpen className="h-4 w-4" />;
  }

  if (id === "organization") {
    return <UsersRound className="h-4 w-4" />;
  }

  return <Database className="h-4 w-4" />;
}

function getSettingsBoundaryLabel(locale: DisplayLocale, id: string) {
  switch (id) {
    case "library":
      return t(locale, "settings.scopeLibrary");
    case "organization":
      return t(locale, "settings.scopeOrganization");
    case "system":
      return t(locale, "settings.scopeSystem");
    case "workspace":
    default:
      return t(locale, "settings.scopeWorkspace");
  }
}

function getSettingsInventoryLabel(locale: DisplayLocale, id: string) {
  switch (id) {
    case "personal-language":
      return t(locale, "settings.inventoryPersonalLanguage");
    case "workspace-profile-update":
      return t(locale, "settings.inventoryWorkspaceProfile");
    case "workspace-members":
      return t(locale, "settings.inventoryWorkspaceMembers");
    case "workspace-notification-preferences":
      return t(locale, "settings.inventoryWorkspaceNotifications");
    case "resource-share":
      return t(locale, "settings.inventoryResourceShare");
    case "organization-profile":
      return t(locale, "settings.inventoryOrganizationProfile");
    case "organization-members":
      return t(locale, "settings.inventoryOrganizationMembers");
    case "organization-workspace-provisioning":
      return t(locale, "settings.inventoryWorkspaceProvisioning");
    case "library-collections-documents":
      return t(locale, "settings.inventoryLibraryOperations");
    case "system-instance-settings":
      return t(locale, "settings.inventorySystemSettings");
    default:
      return id;
  }
}

function getSettingsInventoryScopeLabel(
  locale: DisplayLocale,
  scope: ReturnType<typeof toSettingsCapabilityInventoryRows>[number]["scope"],
) {
  if (scope === "personal") {
    return t(locale, "settings.scopePersonal");
  }

  if (scope === "resource") {
    return t(locale, "settings.scopeResource");
  }

  return getSettingsBoundaryLabel(locale, scope);
}

function getSettingsInventoryBackendStatusLabel(
  locale: DisplayLocale,
  status: ReturnType<typeof toSettingsCapabilityInventoryRows>[number]["backendStatus"],
) {
  switch (status) {
    case "conflict-marked":
      return t(locale, "settings.inventoryConflictMarked");
    case "live-mutation":
      return t(locale, "settings.inventoryLiveMutation");
    case "live-read":
      return t(locale, "settings.inventoryLiveRead");
    case "read-only":
      return t(locale, "settings.inventoryReadOnly");
    case "missing":
    default:
      return t(locale, "settings.inventoryMissing");
  }
}

function getSettingsInventoryFrontendStatusLabel(
  locale: DisplayLocale,
  status: ReturnType<typeof toSettingsCapabilityInventoryRows>[number]["frontendStatus"],
) {
  switch (status) {
    case "half-finished":
      return t(locale, "settings.inventoryHalfFinished");
    case "read-only":
      return t(locale, "settings.inventoryReadOnly");
    case "should-move":
      return t(locale, "settings.inventoryShouldMove");
    case "static":
      return t(locale, "settings.inventoryStatic");
    case "deferred":
      return getSettingsStatusDisplayLabel(locale, "deferred");
    case "live":
    default:
      return getSettingsStatusDisplayLabel(locale, "live");
  }
}

function getSettingsInventoryRecommendationLabel(
  locale: DisplayLocale,
  recommendation: ReturnType<typeof toSettingsCapabilityInventoryRows>[number]["recommendation"],
) {
  switch (recommendation) {
    case "defer":
      return t(locale, "settings.inventoryDefer");
    case "move":
      return t(locale, "settings.inventoryMove");
    case "remove-action-affordance":
      return t(locale, "settings.inventoryRemoveAction");
    case "split":
      return t(locale, "settings.inventorySplit");
    case "keep":
    default:
      return t(locale, "settings.inventoryKeep");
  }
}

function toInventoryStatus(status: ReturnType<typeof toSettingsCapabilityInventoryRows>[number]["frontendStatus"]): WorkspaceSettingsStatus {
  if (status === "live") {
    return "live";
  }

  if (status === "should-move" || status === "read-only") {
    return "reused";
  }

  if (status === "deferred") {
    return "deferred";
  }

  return "assessment";
}

function getOrganizationCapabilityLabel(locale: DisplayLocale, id: string) {
  switch (id) {
    case "global-members":
      return t(locale, "settings.orgGlobalMembers");
    case "workspace-provisioning":
      return t(locale, "settings.orgWorkspaceProvisioning");
    case "domains":
      return t(locale, "settings.orgDomains");
    case "sso-scim-ownership":
      return t(locale, "settings.orgSsoScimOwnership");
    case "audit-log":
      return t(locale, "settings.orgAuditLog");
    case "billing-plan":
      return t(locale, "settings.orgBillingPlan");
    case "data-retention":
      return t(locale, "settings.orgDataRetention");
    case "organization-profile":
    default:
      return t(locale, "settings.orgProfile");
  }
}

function getOrganizationReadinessLabel(locale: DisplayLocale, readiness: string) {
  switch (readiness) {
    case "reusable":
      return t(locale, "settings.readiness.reusable");
    case "partial":
      return t(locale, "settings.readiness.partial");
    case "missing-contract":
      return t(locale, "settings.readiness.missingContract");
    case "deferred":
    default:
      return t(locale, "settings.readiness.deferred");
  }
}

function getImplementationRiskLabel(locale: DisplayLocale, risk: string) {
  switch (risk) {
    case "low":
      return t(locale, "settings.risk.low");
    case "medium":
      return t(locale, "settings.risk.medium");
    case "high":
    default:
      return t(locale, "settings.risk.high");
  }
}

function getSettingsStatusDisplayLabel(locale: DisplayLocale, status: WorkspaceSettingsStatus) {
  switch (status) {
    case "assessment":
      return t(locale, "settings.status.assessment");
    case "live":
      return t(locale, "settings.status.live");
    case "reused":
      return t(locale, "settings.status.reused");
    case "deferred":
      return t(locale, "settings.status.deferred");
    case "not-exposed":
      return t(locale, "settings.status.notExposed");
    case "unavailable":
    default:
      return t(locale, "settings.status.unavailable");
  }
}

function getOrganizationReadSurfaceStatusLabel(
  locale: DisplayLocale,
  state: ReturnType<typeof toOrganizationReadSurfaceState>,
) {
  return getOrganizationReadSurfaceDetailLabel(locale, state.title);
}

function getOrganizationReadSurfaceDetailLabel(locale: DisplayLocale, title: string) {
  switch (title) {
    case "Live-backed":
      return t(locale, "settings.liveReadBacked");
    case "No rows returned":
      return t(locale, "settings.noRowsReturned");
    case "Loading":
      return t(locale, "settings.loading");
    case "Unconfigured":
      return t(locale, "settings.unconfigured");
    case "Forbidden":
      return t(locale, "settings.forbidden");
    case "Not found":
      return t(locale, "settings.notFound");
    case "Error":
    default:
      return t(locale, "settings.error");
  }
}

function getOrganizationDeferredActionLabel(locale: DisplayLocale, label: string) {
  switch (label) {
    case "Create workspace":
      return t(locale, "settings.createWorkspace");
    case "Archive workspace":
      return t(locale, "settings.archiveWorkspace");
    case "Invite member":
      return t(locale, "settings.inviteMember");
    case "Remove member":
      return t(locale, "settings.removeMember");
    case "Change role":
      return t(locale, "settings.changeRole");
    default:
      return label;
  }
}

function getOrganizationDeferredActionReason(locale: DisplayLocale, label: string) {
  switch (label) {
    case "Create workspace":
      return t(locale, "settings.createWorkspaceDeferredReason");
    case "Archive workspace":
      return t(locale, "settings.archiveWorkspaceDeferredReason");
    case "Invite member":
      return t(locale, "settings.inviteMemberDeferredReason");
    case "Remove member":
      return t(locale, "settings.removeMemberDeferredReason");
    case "Change role":
      return t(locale, "settings.changeRoleDeferredReason");
    default:
      return getSettingsStatusDisplayLabel(locale, "deferred");
  }
}

function getOrganizationProfileEditDisabledReason(locale: DisplayLocale, reason: string) {
  if (reason === "Owner required / insufficient permission") {
    return t(locale, "settings.ownerRequired");
  }

  return t(locale, "settings.renameUnavailable");
}
