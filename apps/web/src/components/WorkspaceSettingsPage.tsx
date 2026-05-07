import {
  Bell,
  BookOpen,
  Boxes,
  ChevronRight,
  CircleAlert,
  Database,
  FileText,
  KeyRound,
  Layers3,
  LockKeyhole,
  PencilLine,
  Plug,
  Save,
  Settings,
  ShieldCheck,
  UsersRound,
  X,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError, getConfiguredApiBaseUrl, getConfiguredWorkspaceId } from "../lib/apiClient";
import { getSecurityState, type AuthSecurityStateResponse } from "../lib/authClient";
import {
  getBootstrap,
  getOrganizationMembers,
  getOrganizationProfile,
  getSpaceMap,
  getWorkspaceNotificationPreferences,
  updateOrganizationProfile,
  type BootstrapResponse,
  type KnowledgeMapResponse,
  type OrganizationMembersResponse,
  type OrganizationProfileResponse,
  type UpdateOrganizationProfileRequest,
  type PermissionNotificationPreferenceDto,
} from "../lib/appApi";
import {
  createSettingsHash,
  getSettingsFiltersFromHash,
  type WorkspaceSettingsScope,
  type WorkspaceSettingsTab,
} from "../lib/hashRouting";
import {
  applyOrganizationReadApiStatus,
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
  toWorkspaceGeneralSettings,
  type OrganizationReadApiStatus,
  type WorkspaceSettingsStatus,
} from "../lib/workspaceSettingsModel";
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
  const { locale, setLocale } = useDisplayLanguage();
  const [hash, setHash] = useState(window.location.hash);
  const filters = getSettingsFiltersFromHash(hash);
  const bootstrap = useSettingsBootstrap();
  const workspaceId = bootstrap.data?.workspace.id ?? getConfiguredWorkspaceId();
  const selectedSpaceId = getSelectedSpaceId(bootstrap.data, filters.spaceId);
  const map = useSettingsSpaceMap(selectedSpaceId, bootstrap.status);
  const preferences = useSettingsNotificationPreferences(workspaceId);
  const security = useSettingsSecurityState();
  const organization = useSettingsOrganizationData(bootstrap.data?.workspace.organizationId ?? null);
  const tabRows = useMemo(
    () => createWorkspaceSettingsTabRows(filters.tab, { scope: filters.scope, spaceId: selectedSpaceId }),
    [filters.scope, filters.tab, selectedSpaceId],
  );
  const general = bootstrap.data ? toWorkspaceGeneralSettings(bootstrap.data, filters.spaceId) : null;
  const spaceSummary = bootstrap.data ? toSpaceSettingsSummary(bootstrap.data, map.data, filters.spaceId) : null;
  const libraryGeneral = bootstrap.data ? toLibraryGeneralSettings(bootstrap.data, map.data, selectedSpaceId) : null;
  const libraryCollections = useMemo(() => toLibrarySettingsCollectionRows(map.data, selectedSpaceId), [map.data, selectedSpaceId]);
  const libraryDocuments = useMemo(() => toLibrarySettingsDocumentSummary(map.data, selectedSpaceId), [map.data, selectedSpaceId]);
  const libraryNotifications = useMemo(
    () => toLibraryNotificationPreferenceRows(preferences.preferences, map.data, preferences.status, selectedSpaceId),
    [map.data, preferences.preferences, preferences.status, selectedSpaceId],
  );
  const settingsBoundaries = useMemo(() => toSettingsBoundaryRows(selectedSpaceId), [selectedSpaceId]);
  const organizationAssessmentRows = useMemo(
    () => applyOrganizationReadApiStatus(toOrganizationSettingsAssessmentRows(), {
      members: organization.membersStatus,
      profile: organization.profileStatus,
    }),
    [organization.membersStatus, organization.profileStatus],
  );
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
    <main className="workspace-settings-shell permission-admin-shell flex h-screen flex-col overflow-hidden" style={settingsPatternStyle}>
      <WorkspaceHomeTopBar />
      <div className="permission-admin-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar activeItem="settings" showCollections={false} />
        <section className="permission-admin-feed workspace-settings-feed editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="workspace-home-mobile-nav md:hidden" aria-label="Workspace navigation">
            <a href="#home">{t(locale, "nav.home")}</a>
            <a href="#libraries">{t(locale, "nav.libraries")}</a>
            <a href="#search">{t(locale, "nav.search")}</a>
            <a href="#updates">{t(locale, "nav.updates")}</a>
            <a href="#workspace-members">{t(locale, "nav.members")}</a>
            <a aria-current="page" href="#settings">{t(locale, "nav.settings")}</a>
          </div>
          <div className="permission-admin-feed-inner workspace-settings-inner">
            <header className="permission-admin-heading">
              <div className="min-w-0">
                <h1>{getSettingsScopeHeading(locale, filters.scope)}</h1>
                <p>{getSettingsHeadingStatus(bootstrap.status, locale, filters.scope)}</p>
              </div>
              <div className="permission-admin-heading-metrics" aria-label="Settings summary">
                <SettingsMetric
                  label={t(locale, "common.workspace")}
                  value={(filters.scope === "library" ? libraryGeneral?.workspaceName : general?.workspaceName) ?? statusLabel(bootstrap.status, locale)}
                />
                <SettingsMetric
                  label={t(locale, "common.currentLibrary")}
                  value={(filters.scope === "library" ? libraryGeneral?.spaceName : general?.activeSpaceName) ?? statusLabel(map.status, locale)}
                />
                <SettingsMetric label={t(locale, "common.api")} value={getConfiguredApiBaseUrl() ? t(locale, "common.configured") : t(locale, "common.notConnected")} />
              </div>
            </header>

            <nav className="workspace-settings-scope-switch" aria-label="Settings scope">
              <a
                className={filters.scope === "workspace" ? "is-active" : ""}
                href={createSettingsHash({ scope: "workspace", tab: "general" })}
              >
                {t(locale, "settings.scopeWorkspace")}
              </a>
              <a
                className={filters.scope === "library" ? "is-active" : ""}
                href={createSettingsHash({ scope: "library", spaceId: selectedSpaceId, tab: "general" })}
              >
                {t(locale, "settings.scopeLibrary")}
              </a>
              <a
                className={filters.scope === "organization" ? "is-active" : ""}
                href={createSettingsHash({ scope: "organization", tab: "overview" })}
              >
                {t(locale, "settings.scopeOrganization")}
              </a>
            </nav>

            <nav className="permission-admin-tabs workspace-settings-tabs" aria-label="Workspace settings tabs">
              {tabRows.map((tab) => {
                const tabLabel = getSettingsTabDisplayLabel(locale, tab.id);
                return tab.disabled ? (
                  <button disabled key={tab.id} title={`${tabLabel}: ${getSettingsStatusDisplayLabel(locale, tab.status)}`} type="button">
                    {tabLabel}
                  </button>
                ) : (
                  <a
                    className={filters.tab === tab.id ? "is-active" : ""}
                    href={tab.href}
                    key={tab.id}
                    title={`${tabLabel}: ${getSettingsStatusDisplayLabel(locale, tab.status)}`}
                  >
                    {tabLabel}
                  </a>
                );
              })}
            </nav>

            <SettingsTabStatusGrid locale={locale} tabs={tabRows} />

            {filters.scope === "workspace" && filters.tab === "general" ? (
              <GeneralSettingsTab
                general={general}
                locale={locale}
                mapStatus={map.status}
                setLocale={setLocale}
                spaceSummary={spaceSummary}
                status={bootstrap.status}
              />
            ) : null}
            {filters.scope === "workspace" && filters.tab === "members" ? <MembersSettingsTab /> : null}
            {filters.scope === "workspace" && filters.tab === "notifications" ? <NotificationsSettingsTab preferences={preferences} /> : null}
            {filters.scope === "workspace" && filters.tab === "permissions" ? <PermissionsSettingsTab /> : null}
            {filters.scope === "workspace" && filters.tab === "integrations" ? <IntegrationsSettingsTab /> : null}
            {filters.scope === "workspace" && filters.tab === "security" ? <SecuritySettingsTab security={security} /> : null}
            {filters.scope === "workspace" && (filters.tab === "plan" || filters.tab === "developer") ? (
              <DeferredSettingsTab tab={filters.tab} />
            ) : null}
            {filters.scope === "library" && filters.tab === "general" ? (
              <LibraryGeneralSettingsTab general={libraryGeneral} mapStatus={map.status} status={bootstrap.status} />
            ) : null}
            {filters.scope === "library" && filters.tab === "collections" ? (
              <LibraryCollectionsSettingsTab
                libraryHref={libraryGeneral?.libraryHref ?? "#libraries"}
                mapStatus={map.status}
                rows={libraryCollections}
              />
            ) : null}
            {filters.scope === "library" && filters.tab === "documents" ? (
              <LibraryDocumentsSettingsTab
                libraryHref={libraryGeneral?.libraryHref ?? "#libraries"}
                mapStatus={map.status}
                summary={libraryDocuments}
              />
            ) : null}
            {filters.scope === "library" && filters.tab === "permissions" ? <LibraryPermissionsSettingsTab /> : null}
            {filters.scope === "library" && filters.tab === "notifications" ? (
              <LibraryNotificationsSettingsTab
                preferenceStatus={preferences.status}
                rows={libraryNotifications}
              />
            ) : null}
            {filters.scope === "library" && filters.tab === "advanced" ? <LibraryAdvancedSettingsTab /> : null}
            {filters.scope === "organization" && filters.tab === "overview" ? (
              <OrganizationOverviewTab
                capability={organizationProfileEditCapability}
                errorMessage={organization.profileMutationError}
                model={organizationOverview}
                onSave={organization.updateProfile}
                state={organizationOverviewState}
              />
            ) : null}
            {filters.scope === "organization" && filters.tab === "workspaces" ? (
              <OrganizationWorkspacesTab rows={organizationWorkspaces} state={organizationWorkspacesState} />
            ) : null}
            {filters.scope === "organization" && filters.tab === "members" ? (
              <OrganizationMembersTab rows={organizationMembers} state={organizationMembersState} />
            ) : null}
            {filters.scope === "organization" && filters.tab === "assessment" ? (
              <OrganizationAssessmentTab boundaries={settingsBoundaries} rows={organizationAssessmentRows} />
            ) : null}
          </div>
        </section>
      </div>
    </main>
  );
}

function GeneralSettingsTab({
  general,
  locale,
  mapStatus,
  setLocale,
  spaceSummary,
  status,
}: {
  general: ReturnType<typeof toWorkspaceGeneralSettings> | null;
  locale: DisplayLocale;
  mapStatus: SettingsDataStatus;
  setLocale: (locale: DisplayLocale) => void;
  spaceSummary: ReturnType<typeof toSpaceSettingsSummary>;
  status: SettingsDataStatus;
}) {
  if (!general) {
    return (
      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.general")} />
        <div className="workspace-settings-card-grid is-two">
          <LanguageRegionCard locale={locale} setLocale={setLocale} />
        </div>
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
                    <small>{space.isCurrent ? t(locale, "settings.currentLibraryLabel") : space.id}</small>
                  </span>
                  <ChevronRight className="h-4 w-4" />
                </a>
              ))}
            </div>
          </SettingsCard>
          <LanguageRegionCard locale={locale} setLocale={setLocale} />
        </div>
      </section>

      <section className="permission-admin-section">
        <SectionTitle title={t(locale, "settings.spaceSettingsEntry")} />
        {spaceSummary ? (
          <SettingsCard
            icon={<Boxes className="h-4 w-4" />}
            status="live"
            title={spaceSummary.spaceName}
          >
            <DefinitionList
              rows={[
                [t(locale, "settings.spaceId"), spaceSummary.spaceId],
                [t(locale, "nav.currentLibraryCollections"), String(spaceSummary.collectionCount)],
                [t(locale, "library.documents"), String(spaceSummary.documentCount)],
                [t(locale, "settings.update"), t(locale, "settings.updateDeferred")],
                ["Map status", statusLabel(mapStatus, locale)],
              ]}
            />
            <div className="workspace-settings-card-actions">
              <a href={spaceSummary.libraryHref}>{t(locale, "settings.openLibrary")}</a>
            </div>
          </SettingsCard>
        ) : (
          <SettingsEmptyState label={getUnavailableLabel(mapStatus, t(locale, "settings.spaceSettingsEntry"), locale)} />
        )}
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
      <SettingsCard icon={<Layers3 className="h-4 w-4" />} status={mapStatus === "ready" ? "live" : "unavailable"} title={t(locale, "settings.collectionOrder")}>
        {mapStatus === "ready" && rows.length > 0 ? (
          <div className="workspace-settings-link-list">
            {rows.map((row) => (
              <a href={row.href} key={row.id} title={row.title}>
                <span>
                  <strong>{row.title}</strong>
                  <small>
                    {t(locale, "settings.position")} {row.position} · {row.documentCount} {t(locale, "library.documents")} · sort {row.sortOrder}
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
        <SettingsCard icon={<FileText className="h-4 w-4" />} status={mapStatus === "ready" ? "live" : "unavailable"} title={t(locale, "settings.documents")}>
          {mapStatus === "ready" && summary.recentDocuments.length > 0 ? (
            <div className="workspace-settings-link-list">
              {summary.recentDocuments.map((document) => (
                <a href={document.href} key={document.id} title={document.title}>
                  <span>
                    <strong>{document.title}</strong>
                    <small>{document.collectionTitle} · {document.status} · {new Date(document.updatedAt).toLocaleDateString(locale)}</small>
                  </span>
                  <ChevronRight className="h-4 w-4" />
                </a>
              ))}
            </div>
          ) : (
            <SettingsEmptyState label={mapStatus === "ready" ? t(locale, "settings.noDocuments") : getUnavailableLabel(mapStatus, t(locale, "settings.documents"), locale)} />
          )}
          <div className="workspace-settings-card-actions">
            <a href={libraryHref}>{t(locale, "settings.newDocumentInLibrary")}</a>
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
      <div className="workspace-settings-card-grid is-three">
        <SettingsLinkCard href="#workspace-members" icon={<UsersRound className="h-4 w-4" />} status="reused" title={t(locale, "settings.members")} />
        <SettingsLinkCard href="#permissions" icon={<ShieldCheck className="h-4 w-4" />} status="reused" title="Document Permissions" />
        <SettingsLinkCard href="#updates?tab=access" icon={<Bell className="h-4 w-4" />} status="reused" title="Access Requests" />
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
            <DefinitionList
              rows={[
                [t(locale, "settings.readOnlyStatus"), t(locale, "settings.liveReadOnly")],
                [t(locale, "settings.name"), model.name],
                [t(locale, "settings.organizationId"), model.id],
                [t(locale, "settings.slug"), model.slug],
                [t(locale, "settings.statusLabel"), model.status],
                [t(locale, "settings.visibleWorkspaces"), String(model.visibleWorkspaceCount)],
                [t(locale, "settings.createdAt"), model.createdAt],
                [t(locale, "settings.updatedAt"), model.updatedAt],
              ]}
            />
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
              <div className="workspace-settings-inline-edit-panel">
                <label>
                  <span>{t(locale, "settings.organizationName")}</span>
                  <input
                    disabled={isSaving}
                    onChange={(event) => setDraftName(event.currentTarget.value)}
                    value={draftName}
                  />
                  {clientErrors.name ? <small>{clientErrors.name}</small> : null}
                </label>
                <label>
                  <span>{t(locale, "settings.organizationSlug")}</span>
                  <input
                    disabled={isSaving}
                    onChange={(event) => setDraftSlug(event.currentTarget.value)}
                    value={draftSlug}
                  />
                  {clientErrors.slug ? <small>{clientErrors.slug}</small> : null}
                </label>
                <p className="workspace-settings-note">{t(locale, "settings.slugHelp")}</p>
                {errorMessage && capability.mutationStatus === "error" ? (
                  <p className="workspace-settings-inline-error">{errorMessage}</p>
                ) : null}
                {capability.mutationStatus === "success" ? (
                  <p className="workspace-settings-inline-success">{t(locale, "settings.profileUpdated")}</p>
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
              <p className="workspace-settings-inline-success">{t(locale, "settings.profileUpdated")}</p>
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

function MembersSettingsTab() {
  const { locale } = useDisplayLanguage();
  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.members")} />
      <div className="workspace-settings-card-grid is-two">
        <SettingsCard icon={<UsersRound className="h-4 w-4" />} status="reused" title={t(locale, "settings.members")}>
          <p className="workspace-settings-note">
            Member list, add member, role update, remove member, busy states, and backend error display stay on the existing Members surface.
          </p>
          <div className="workspace-settings-card-actions">
            <a href="#workspace-members">{t(locale, "common.open")} {t(locale, "settings.members")}</a>
          </div>
        </SettingsCard>
        <SettingsCard icon={<ShieldCheck className="h-4 w-4" />} status="reused" title="Role Boundaries">
          <DefinitionList
            rows={[
              ["Viewer", "Management actions disabled by existing capability model"],
              ["Admin / Owner", "Mutations remain checked by backend permission service"],
              ["Last owner", "Protected by existing backend constraints"],
            ]}
          />
        </SettingsCard>
      </div>
    </section>
  );
}

function NotificationsSettingsTab({
  preferences,
}: {
  preferences: ReturnType<typeof useSettingsNotificationPreferences>;
}) {
  const { locale } = useDisplayLanguage();
  const model = useMemo(
    () => toNotificationSettingsModel(preferences.preferences, preferences.status),
    [preferences.preferences, preferences.status],
  );
  const watchedRows = model.resourceRows.filter((row) => row.state === "watched");
  const mutedRows = model.resourceRows.filter((row) => row.state === "muted");

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.notifications")} />
      <div className="workspace-settings-card-grid is-two">
        <SettingsCard icon={<Bell className="h-4 w-4" />} status="live" title="Resource Preferences">
          <p className="workspace-settings-note">{notificationStatusLabel(model.preferenceStatus)}</p>
          <PreferenceResourceRows label="Watched" rows={watchedRows} />
          <PreferenceResourceRows label="Muted" rows={mutedRows} />
        </SettingsCard>
        <SettingsCard icon={<CircleAlert className="h-4 w-4" />} status="deferred" title={t(locale, "settings.categoryAndDigest")}>
          <DefinitionList
            rows={[
              [t(locale, "settings.categoryPreferences"), getSettingsStatusDisplayLabel(locale, model.categoryPreferenceStatus)],
              [t(locale, "settings.emailDigest"), getSettingsStatusDisplayLabel(locale, model.emailDigestStatus)],
              ["Boundary", "No new notification/email backend semantics in this round"],
            ]}
          />
        </SettingsCard>
      </div>
    </section>
  );
}

function PermissionsSettingsTab() {
  const { locale } = useDisplayLanguage();
  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.permissions")} />
      <div className="workspace-settings-card-grid is-three">
        <SettingsLinkCard href="#workspace-members" icon={<UsersRound className="h-4 w-4" />} status="reused" title={t(locale, "settings.members")} />
        <SettingsLinkCard href="#workspace-groups" icon={<Boxes className="h-4 w-4" />} status="reused" title="Groups" />
        <SettingsLinkCard href="#permissions" icon={<ShieldCheck className="h-4 w-4" />} status="reused" title="Document Permissions" />
        <SettingsLinkCard href="#updates?tab=access" icon={<Bell className="h-4 w-4" />} status="reused" title="Access Requests" />
        <SettingsCard icon={<LockKeyhole className="h-4 w-4" />} status="reused" title="Share Links / Public Links">
          <p className="workspace-settings-note">
            Existing share-link surfaces remain the only entry point. Public-link behavior is not changed here.
          </p>
        </SettingsCard>
      </div>
    </section>
  );
}

function IntegrationsSettingsTab() {
  const { locale } = useDisplayLanguage();
  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.integrations")} />
      <div className="workspace-settings-card-grid is-three">
        <SettingsLinkCard href="#scim" icon={<Plug className="h-4 w-4" />} status="reused" title="SCIM" />
        <SettingsCard icon={<Database className="h-4 w-4" />} status="reused" title="IAM Sync">
          <p className="workspace-settings-note">Existing SCIM discovery and token management surface is linked from this tab.</p>
        </SettingsCard>
        <SettingsCard icon={<KeyRound className="h-4 w-4" />} status="deferred" title="External Providers">
          <DefinitionList
            rows={[
              ["OIDC / SAML redirect", "Deferred"],
              ["SMTP / production delivery", "Deferred"],
              ["SCIM bulk / complex filters", "Deferred"],
            ]}
          />
        </SettingsCard>
      </div>
    </section>
  );
}

function SecuritySettingsTab({
  security,
}: {
  security: ReturnType<typeof useSettingsSecurityState>;
}) {
  const { locale } = useDisplayLanguage();
  const rows = useMemo(() => toSecuritySettingsRows(security.state, security.status), [security.state, security.status]);

  return (
    <section className="permission-admin-section">
      <SectionTitle title={t(locale, "settings.security")} />
      <div className="workspace-settings-card-grid is-two">
        <SettingsCard icon={<ShieldCheck className="h-4 w-4" />} status={security.status === "ready" ? "live" : "unavailable"} title="Auth State">
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
        if (error instanceof DOMException && error.name === "AbortError") {
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
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setData(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, bootstrapStatus, spaceId]);

  return { data, status };
}

function useSettingsNotificationPreferences(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [preferences, setPreferences] = useState<PermissionNotificationPreferenceDto[]>([]);
  const [status, setStatus] = useState<NotificationApiStatus>(() => (apiBaseUrl && workspaceId ? "loading" : "unconfigured"));

  useEffect(() => {
    if (!apiBaseUrl || !workspaceId) {
      setPreferences([]);
      setStatus("unconfigured");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getWorkspaceNotificationPreferences(workspaceId, controller.signal)
      .then((body) => {
        setPreferences(body.preferences);
        setStatus("ready");
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

  return { preferences, status };
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

function getSettingsScopeHeading(locale: DisplayLocale, scope: WorkspaceSettingsScope) {
  if (scope === "library") {
    return t(locale, "settings.libraryHeading");
  }

  if (scope === "organization") {
    return t(locale, "settings.organizationHeading");
  }

  return t(locale, "settings.heading");
}

function getSettingsHeadingStatus(status: SettingsDataStatus, locale: DisplayLocale, scope: WorkspaceSettingsScope) {
  if (status === "ready") {
    if (scope === "library") {
      return t(locale, "settings.libraryHeading");
    }

    if (scope === "organization") {
      return t(locale, "settings.organizationHeadingReady");
    }

    return t(locale, "settings.headingReady");
  }

  return getUnavailableLabel(status, getSettingsScopeHeading(locale, scope), locale);
}

function getUnavailableLabel(status: SettingsDataStatus, noun: string, locale: DisplayLocale) {
  if (status === "loading") {
    return locale === "zh-CN" ? `正在加载${noun}。` : `Loading ${noun.toLowerCase()}.`;
  }

  if (status === "forbidden") {
    return locale === "zh-CN" ? `当前会话不可用：${noun}。` : `${noun} are not available for this session.`;
  }

  if (status === "error") {
    return locale === "zh-CN" ? `${noun} 无法从已配置 API 加载。` : `${noun} could not be loaded from the configured API.`;
  }

  if (status === "idle") {
    return locale === "zh-CN" ? `${noun} 正在等待工作区数据。` : `${noun} are waiting for workspace data.`;
  }

  return locale === "zh-CN" ? `配置 API 后加载${noun}。` : `Configure the API to load ${noun.toLowerCase()}.`;
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
