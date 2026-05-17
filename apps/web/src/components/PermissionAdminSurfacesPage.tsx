import {
  AlertTriangle,
  Copy,
  Database,
  KeyRound,
  RefreshCw,
  ServerCog,
  ShieldCheck,
  Trash2,
  UserPlus,
  UsersRound,
  X,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { ApiClientError } from "../lib/apiClient";
import { getCurrentUser } from "../lib/authClient";
import { getBootstrap, type BootstrapResponse } from "../lib/appApi";
import {
  PermissionAdminApiError,
  addWorkspaceMember,
  createScimToken,
  getConfiguredPermissionAdminApiBaseUrl,
  getConfiguredPermissionAdminWorkspaceId,
  getScimResourceTypes,
  getScimSchemas,
  getScimServiceProviderConfig,
  getScimTokens,
  getWorkspaceGroups,
  getWorkspaceMembers,
  removeWorkspaceMember,
  revokeScimToken,
  updateWorkspaceMemberRole,
  type PermissionAdminApiStatus,
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
  getMemberLifecycleGuard,
  getMemberActionCapability,
  getRoleChangeOptions,
  addExistingUserRoleOptions,
  isSupportedWorkspaceMemberRole,
  getWorkspaceGroupDetailRows,
  getWorkspaceGroupReadOnlyReason,
  getWorkspaceGroupSourceLabel,
  resolvePermissionAdminWorkspace,
  toPermissionMutationError,
  type CurrentWorkspaceRole,
  type MembersTeamsTab,
  type WorkspaceMemberRole,
} from "../lib/permissionAdminModel";
import { t, useDisplayLanguage } from "../lib/i18n";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

type WorkspaceRoleOption = Exclude<WorkspaceMemberRole, "owner">;

const memberRoleOptions: WorkspaceRoleOption[] = addExistingUserRoleOptions.filter((role) => role !== "owner") as WorkspaceRoleOption[];
const permissionAdminPatternStyle = {
  "--permission-admin-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--permission-admin-route-pattern": `url(${routePatternUrl})`,
  "--permission-admin-topographic-pattern": `url(${topographicPatternUrl})`,
  "--workspace-home-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--workspace-home-route-pattern": `url(${routePatternUrl})`,
  "--workspace-home-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

export function PermissionAdminSurfacesPage({ initialTab = "members" }: { initialTab?: MembersTeamsTab }) {
  const { locale } = useDisplayLanguage();
  const [activeTab, setActiveTab] = useState<MembersTeamsTab>(initialTab);
  const workspaceContext = usePermissionAdminWorkspaceContext();
  const permissionAdminWorkspaceId = workspaceContext.resolution.workspaceId;
  const members = useWorkspaceMembers(permissionAdminWorkspaceId);
  const groups = useWorkspaceGroups(permissionAdminWorkspaceId);
  const scimDiscovery = useScimDiscovery(permissionAdminWorkspaceId);
  const scimTokens = useScimTokens(permissionAdminWorkspaceId);
  const currentWorkspaceIdentity = useCurrentWorkspaceIdentity(permissionAdminWorkspaceId);
  const memberMutations = useMemberMutations(members.apiBaseUrl, permissionAdminWorkspaceId, members.reload);
  const activeSidebarItem = activeTab === "teams" ? "groups" : "members";

  useEffect(() => {
    setActiveTab(initialTab);
  }, [initialTab]);

  return (
    <main className="permission-admin-shell flex h-screen flex-col overflow-hidden" style={permissionAdminPatternStyle}>
      <WorkspaceHomeTopBar />
      <div className="permission-admin-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar activeItem={activeSidebarItem} showCollections={false} />
        <section className="permission-admin-feed editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="workspace-home-mobile-nav md:hidden" aria-label="Workspace navigation">
            <a href="#home">{t(locale, "nav.home")}</a>
            <a href="#libraries">{t(locale, "nav.libraries")}</a>
            <a href="#access-sharing">{t(locale, "nav.updates")}</a>
            <a aria-current={activeSidebarItem === "members" ? "page" : undefined} href="#members">{t(locale, "nav.members")}</a>
            <a aria-current={activeSidebarItem === "groups" ? "page" : undefined} href="#groups">{t(locale, "nav.groups")}</a>
            <a href="#settings">{t(locale, "nav.settings")}</a>
          </div>
          <div className="permission-admin-feed-inner">
            <PermissionAdminHeader
              activeTab={activeTab}
              groups={groups.groups}
              members={members.members}
              workspaceId={permissionAdminWorkspaceId}
            />
            <PermissionAdminTabs activeTab={activeTab} onChange={setActiveTab} />
            {activeTab === "teams" ? (
              <WorkspaceGroupsPanel groups={groups.groups} status={groups.status} />
            ) : activeTab === "directory" ? (
              <DirectorySyncPanel
                discovery={scimDiscovery}
                scimTokens={scimTokens}
                workspaceId={permissionAdminWorkspaceId}
              />
            ) : activeTab === "summary" ? (
              <PermissionSummaryPanel
                groups={groups.groups}
                groupStatus={groups.status}
                members={members.members}
                memberStatus={members.status}
              />
            ) : (
              <WorkspaceMembersPanel
                groups={groups.groups}
                memberMutations={memberMutations}
                members={members.members}
                currentRole={currentWorkspaceIdentity.role}
                currentUserId={currentWorkspaceIdentity.userId}
                reloadGroups={groups.reload}
                reloadMembers={members.reload}
                status={members.status}
              />
            )}
          </div>
        </section>
      </div>
    </main>
  );
}

function PermissionAdminHeader({
  activeTab,
  groups,
  members,
  workspaceId,
}: {
  activeTab: MembersTeamsTab;
  groups: WorkspaceGroupDto[] | null;
  members: WorkspaceMemberDto[] | null;
  workspaceId: string | null;
}) {
  const activeMembers = members?.filter((member) => member.status === "active").length ?? 0;
  const activeGroups = groups?.filter((group) => !group.isArchived).length ?? 0;
  const heading =
    activeTab === "teams" ? "Groups" : activeTab === "directory" ? "Directory Sync" : activeTab === "summary" ? "Permissions Summary" : "Members";
  const description =
    activeTab === "teams"
      ? "View workspace groups and directory-managed group sources."
      : activeTab === "directory"
        ? "Check directory sync discovery and token status."
        : activeTab === "summary"
          ? "Review identity and permission ownership boundaries."
          : "Manage workspace members and role lifecycle.";

  return (
    <header className="permission-admin-heading">
      <div className="min-w-0">
        <h1>{heading}</h1>
        <p>{description}</p>
      </div>
      <div className="permission-admin-heading-metrics" aria-label="Workspace admin summary">
        <PermissionMetric label="Workspace" value={workspaceId ? shortId(workspaceId) : "Not connected"} />
        <PermissionMetric label="Active Members" value={String(activeMembers)} />
        <PermissionMetric label="Active Groups" value={String(activeGroups)} />
      </div>
    </header>
  );
}

function PermissionAdminTabs({
  activeTab,
  onChange,
}: {
  activeTab: MembersTeamsTab;
  onChange: (tab: MembersTeamsTab) => void;
}) {
  const tabs: Array<{ id: MembersTeamsTab; label: string; href: string }> = [
    { id: "members", label: "Members", href: "#members" },
    { id: "teams", label: "Groups", href: "#groups" },
    { id: "directory", label: "Directory Sync", href: "#members?tab=directory" },
    { id: "summary", label: "Permissions Summary", href: "#members?tab=summary" },
  ];

  return (
    <nav className="permission-admin-tabs" aria-label="Permission admin sections">
      {tabs.map((tab) => (
        <a
          className={activeTab === tab.id ? "is-active" : ""}
          href={tab.href}
          key={tab.id}
          onClick={() => onChange(tab.id)}
          title={tab.label}
        >
          {tab.label}
        </a>
      ))}
    </nav>
  );
}

function usePermissionAdminWorkspaceContext() {
  const apiBaseUrl = useMemo(() => getConfiguredPermissionAdminApiBaseUrl(), []);
  const configuredWorkspaceId = useMemo(() => getConfiguredPermissionAdminWorkspaceId(), []);
  const [bootstrap, setBootstrap] = useState<BootstrapResponse | null>(null);
  const [status, setStatus] = useState<PermissionAdminApiStatus>(() =>
    apiBaseUrl && !configuredWorkspaceId ? "loading" : apiBaseUrl ? "ready" : "unconfigured",
  );

  useEffect(() => {
    if (!apiBaseUrl) {
      setBootstrap(null);
      setStatus("unconfigured");
      return undefined;
    }

    if (configuredWorkspaceId) {
      setBootstrap(null);
      setStatus("ready");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getBootstrap(controller.signal)
      .then((response) => {
        setBootstrap(response);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setBootstrap(null);
        setStatus(error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, configuredWorkspaceId]);

  return {
    apiBaseUrl,
    resolution: resolvePermissionAdminWorkspace({
      apiConfigured: Boolean(apiBaseUrl),
      bootstrapWorkspaceId: bootstrap?.workspace.id ?? null,
      configuredWorkspaceId,
    }),
    status,
  };
}

function WorkspaceMembersPanel({
  currentRole,
  currentUserId,
  groups,
  memberMutations,
  members,
  reloadGroups,
  reloadMembers,
  status,
}: {
  currentRole: CurrentWorkspaceRole;
  currentUserId: string | null;
  groups: WorkspaceGroupDto[] | null;
  memberMutations: ReturnType<typeof useMemberMutations>;
  members: WorkspaceMemberDto[] | null;
  reloadGroups: () => AbortController | undefined;
  reloadMembers: () => AbortController | undefined;
  status: PermissionAdminApiStatus;
}) {
  return (
    <>
      <AddMemberPanel currentRole={currentRole} mutations={memberMutations} status={status} />
      <section className="permission-admin-section">
        <SectionTitle count={members?.length ?? 0} title="Workspace Members">
          <button className="permission-admin-icon-button" onClick={() => reloadMembers()} title="Refresh members" type="button">
            <RefreshCw className="h-4 w-4" />
          </button>
        </SectionTitle>
        <WorkspaceMembersTable
          currentRole={currentRole}
          currentUserId={currentUserId}
          members={members}
          mutations={memberMutations}
          status={status}
        />
      </section>

      <section className="permission-admin-section">
        <SectionTitle count={groups?.length ?? 0} title="Group Sources">
          <button className="permission-admin-icon-button" onClick={() => reloadGroups()} title="Refresh groups" type="button">
            <RefreshCw className="h-4 w-4" />
          </button>
        </SectionTitle>
        <WorkspaceGroupSummary groups={groups} />
      </section>
    </>
  );
}

function AddMemberPanel({
  currentRole,
  mutations,
  status,
}: {
  currentRole: CurrentWorkspaceRole;
  mutations: ReturnType<typeof useMemberMutations>;
  status: PermissionAdminApiStatus;
}) {
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<WorkspaceRoleOption>("viewer");
  const addCapability = getMemberActionCapability({
    action: "add-member",
    apiConfigured: mutations.canUse,
    currentRole,
    operation: mutations.operation,
    status,
  });
  const canMutate = addCapability.canUse;
  const isValid = email.trim().includes("@") && memberRoleOptions.includes(role);

  const submit = () => {
    if (!canMutate || !isValid) {
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
    <section className="permission-admin-section">
      <SectionTitle count={memberRoleOptions.length} title="Add Existing User" />
      <div className="permission-admin-form">
        <p className="permission-admin-inline-status">Member changes are governed by workspace role permissions.</p>
        <div className="permission-admin-form-grid is-member">
          <Field label="Email" htmlFor="workspace-member-email">
            <input
              disabled={!canMutate}
              id="workspace-member-email"
              onChange={(event) => setEmail(event.target.value)}
              placeholder="user@example.com"
              type="email"
              value={email}
            />
          </Field>
          <Field label="Workspace Role" htmlFor="workspace-member-role">
            <select
              disabled={!canMutate}
              id="workspace-member-role"
              onChange={(event) => setRole(event.target.value as WorkspaceRoleOption)}
              value={role}
            >
              {memberRoleOptions.map((roleOption) => (
                <option key={roleOption} value={roleOption}>
                  {formatPermissionValue(roleOption)}
                </option>
              ))}
            </select>
          </Field>
          <div className="permission-admin-policy-note">
            <strong>Owner assignment</strong>
            <span>Not exposed in this frontend surface.</span>
          </div>
          <button
            className="permission-admin-primary-action"
            disabled={!canMutate || !isValid || mutations.operation === "add-member"}
            onClick={submit}
            title={!canMutate ? addCapability.reason ?? statusLabel(status) : "Add an existing user to this workspace"}
            type="button"
          >
            <UserPlus className="h-4 w-4" />
            {mutations.operation === "add-member" ? "Adding" : "Add Existing User"}
          </button>
        </div>
        <MutationStatus error={mutations.error} message={mutations.message} />
      </div>
    </section>
  );
}

function WorkspaceMembersTable({
  currentRole,
  currentUserId,
  members,
  mutations,
  status,
}: {
  currentRole: CurrentWorkspaceRole;
  currentUserId: string | null;
  members: WorkspaceMemberDto[] | null;
  mutations: ReturnType<typeof useMemberMutations>;
  status: PermissionAdminApiStatus;
}) {
  const [confirmRemoveUserId, setConfirmRemoveUserId] = useState<string | null>(null);

  if (!members) {
    return <EmptyState status={status} />;
  }

  if (members.length === 0) {
    return <EmptyState label="No workspace members" />;
  }

  const remove = (member: WorkspaceMemberDto) => {
    if (confirmRemoveUserId !== member.userId) {
      setConfirmRemoveUserId(member.userId);
      return;
    }

    void mutations.removeMember(member.userId).then((removed) => {
      if (removed) {
        setConfirmRemoveUserId(null);
      }
    });
  };

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
          currentUserId,
          member,
          members,
          operation: mutations.operation,
          status,
        });
        const removeCapability = getMemberActionCapability({
          action: "remove-member",
          apiConfigured: mutations.canUse,
          currentRole,
          currentUserId,
          member,
          members,
          operation: mutations.operation,
          status,
        });
        const confirmRemove = confirmRemoveUserId === member.userId;
        return (
          <article className="permission-admin-table-row is-members" key={member.userId}>
            <span className="permission-admin-identity">
              <Avatar initials={getInitials(member.displayName || member.email || member.userId)} tone={member.role} />
              <span className="min-w-0">
                <strong title={member.displayName || member.userId}>
                  {member.displayName || "Unnamed user"}
                  {currentUserId === member.userId ? " (You)" : ""}
                </strong>
                <small>{shortId(member.userId)}</small>
              </span>
            </span>
            <span className="permission-admin-cell-text">{member.email ?? "No email"}</span>
            <WorkspaceRoleSelect
              disabled={!updateCapability.canUse}
              disabledReason={updateCapability.reason}
              member={member}
              members={members}
              currentUserId={currentUserId}
              onChange={(role) => mutations.updateRole(member.userId, role)}
            />
            <StatusPill label={member.status} />
            <span className="permission-admin-cell-text">{member.joinedAt ? formatDateTime(member.joinedAt) : "Unknown"}</span>
            <span className="permission-admin-row-actions">
              <button
                className="permission-admin-icon-button is-danger"
                disabled={!removeCapability.canUse}
                onClick={() => remove(member)}
                title={
                  !removeCapability.canUse
                    ? removeCapability.reason ?? "Member removal is unavailable"
                    : confirmRemove
                      ? `Confirm removing ${member.displayName || member.email || member.userId}. They will lose workspace access.`
                      : "Remove member"
                }
                type="button"
              >
                {confirmRemove ? "Confirm" : <Trash2 className="h-4 w-4" />}
              </button>
              {confirmRemove ? (
                <small className="permission-admin-cell-text">
                  Remove {member.displayName || member.email || member.userId}; workspace access will be revoked.
                </small>
              ) : null}
              <button className="permission-admin-icon-button" disabled title="Reactivate API is not available" type="button">
                <RefreshCw className="h-4 w-4" />
              </button>
            </span>
          </article>
        );
      })}
    </div>
  );
}

function WorkspaceRoleSelect({
  currentUserId,
  disabled,
  disabledReason,
  member,
  members,
  onChange,
}: {
  currentUserId: string | null;
  disabled: boolean;
  disabledReason: string | null;
  member: WorkspaceMemberDto;
  members: WorkspaceMemberDto[] | null;
  onChange: (role: string) => Promise<WorkspaceMemberDto | null>;
}) {
  const [draftRole, setDraftRole] = useState(member.role);

  useEffect(() => {
    setDraftRole(member.role);
  }, [member.role]);

  return (
    <select
      className="permission-admin-role-select"
      disabled={disabled}
      onChange={(event) => {
        const nextRole = event.target.value;
        const guard = getMemberLifecycleGuard({
          action: "update-role",
          currentUserId,
          member,
          members,
          nextRole,
        });
        if (!guard.canUse || !isSupportedWorkspaceMemberRole(nextRole)) {
          setDraftRole(member.role);
          return;
        }

        if (guard.requiresConfirmation && !window.confirm(`${guard.reason} Change ${member.displayName || member.email || member.userId} to ${formatPermissionValue(nextRole)}?`)) {
          setDraftRole(member.role);
          return;
        }

        setDraftRole(nextRole);
        void onChange(nextRole).then((updated) => {
          if (!updated) {
            setDraftRole(member.role);
          }
        });
      }}
      title={
        disabled
          ? disabledReason ?? "Role update is unavailable"
          : member.role === "owner"
            ? "Owner role changes require confirmation and backend validation"
            : "Update role"
      }
      value={draftRole}
    >
      {getRoleChangeOptions(member, members).map((role) => (
        <option key={role} value={role}>
          {formatPermissionValue(role)}
        </option>
      ))}
    </select>
  );
}

function WorkspaceGroupSummary({ groups }: { groups: WorkspaceGroupDto[] | null }) {
  if (!groups) {
    return <EmptyState label="Groups not loaded" />;
  }

  if (groups.length === 0) {
    return <EmptyState label="No workspace groups" />;
  }

  return (
    <div className="permission-admin-table">
      <div className="permission-admin-table-head is-groups">
        <span>Name</span>
        <span>Type</span>
        <span>Source</span>
        <span>Members</span>
      </div>
      {groups.slice(0, 5).map((group) => (
        <article className="permission-admin-table-row is-groups" key={group.id}>
          <span className="permission-admin-identity">
            <Avatar initials={getInitials(group.name)} tone={group.externalProvider ? "external" : "group"} />
            <span className="min-w-0">
              <strong title={group.name}>{group.name}</strong>
              <small>{shortId(group.id)}</small>
            </span>
          </span>
          <StatusPill label={group.isArchived ? "archived" : group.type} />
          <span className="permission-admin-cell-text">
            {group.externalProvider ? `${group.externalProvider} / ${group.externalGroupId ?? "external"}` : "Local"}
          </span>
          <span className="permission-admin-cell-text">{group.membersCount}</span>
        </article>
      ))}
    </div>
  );
}

function WorkspaceGroupsPanel({
  groups,
  status,
}: {
  groups: WorkspaceGroupDto[] | null;
  status: PermissionAdminApiStatus;
}) {
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);

  if (!groups) {
    return (
      <section className="permission-admin-section">
        <SectionTitle title="Workspace Groups" />
        <EmptyState status={status} />
      </section>
    );
  }

  const selectedGroup = groups.find((group) => group.id === selectedGroupId) ?? groups[0] ?? null;

  return (
    <>
      <section className="permission-admin-section">
        <SectionTitle count={groups.length} title="Groups" />
        {groups.length === 0 ? (
          <EmptyState label="No workspace groups" />
        ) : (
          <div className="permission-admin-table">
            <div className="permission-admin-table-head is-group-detail">
              <span>Name</span>
              <span>State</span>
              <span>Source</span>
              <span>Members</span>
              <span>Updated</span>
              <span>Detail</span>
            </div>
            {groups.map((group) => (
              <article className="permission-admin-table-row is-group-detail" key={group.id}>
                <span className="permission-admin-identity">
                  <Avatar initials={getInitials(group.name)} tone={group.externalProvider ? "external" : "group"} />
                  <span className="min-w-0">
                    <strong title={group.name}>{group.name}</strong>
                    <small>{group.description ?? shortId(group.id)}</small>
                  </span>
                </span>
                <StatusPill label={group.isArchived ? "archived" : group.type} />
                <span className="permission-admin-cell-text">
                  {getWorkspaceGroupSourceLabel(group)}
                  <small>{group.externalGroupId ?? getWorkspaceGroupReadOnlyReason(group)}</small>
                </span>
                <span className="permission-admin-cell-text">{group.membersCount}</span>
                <span className="permission-admin-cell-text">{formatDateTime(group.updatedAt)}</span>
                <span className="permission-admin-row-actions">
                  <button
                    className="permission-admin-icon-button"
                    onClick={() => setSelectedGroupId(group.id)}
                    title="View group detail"
                    type="button"
                  >
                    <ShieldCheck className="h-4 w-4" />
                  </button>
                </span>
              </article>
            ))}
          </div>
        )}
      </section>
      {selectedGroup ? (
        <section className="permission-admin-section">
          <SectionTitle title="Group Detail" />
          <div className="permission-admin-form">
            <div className="permission-admin-policy-note">
              <strong>{selectedGroup.name}</strong>
              <span>{getWorkspaceGroupReadOnlyReason(selectedGroup)}</span>
            </div>
            <div className="permission-admin-table">
              <div className="permission-admin-table-head is-capability">
                <span>Field</span>
                <span>Value</span>
                <span>Boundary</span>
              </div>
              {getWorkspaceGroupDetailRows(selectedGroup).map(([label, value]) => (
                <article className="permission-admin-table-row is-capability" key={label}>
                  <span className="permission-admin-cell-text">{label}</span>
                  <span className="permission-admin-cell-text">{value}</span>
                  <span className="permission-admin-cell-text">
                    {label === "Members" ? "Member detail API is not exposed here." : "Existing group list metadata"}
                  </span>
                </article>
              ))}
            </div>
          </div>
        </section>
      ) : null}
    </>
  );
}

function DirectorySyncPanel({
  discovery,
  scimTokens,
  workspaceId,
}: {
  discovery: ReturnType<typeof useScimDiscovery>;
  scimTokens: ReturnType<typeof useScimTokens>;
  workspaceId: string | null;
}) {
  return (
    <>
      <section className="permission-admin-section">
        <SectionTitle title="Directory Sync" />
        <div className="permission-admin-endpoint">
          <ServerCog className="h-5 w-5" />
          <code>{`/api/v1/workspaces/${workspaceId ?? "{workspaceId}"}/scim/v2`}</code>
        </div>
      </section>

      <section className="permission-admin-section">
        <SectionTitle title="Status" />
        <ScimDiscoveryPanel discovery={discovery} />
      </section>

      <section className="permission-admin-section">
        <SectionTitle count={scimTokens.tokens?.length ?? 0} title="SCIM Tokens">
          <button className="permission-admin-icon-button" onClick={() => scimTokens.reload()} title="Refresh SCIM tokens" type="button">
            <RefreshCw className="h-4 w-4" />
          </button>
        </SectionTitle>
        <ScimTokenReadOnlyList scimTokens={scimTokens} />
      </section>

      <section className="permission-admin-section">
        <SectionTitle title="Provisioning Capabilities" />
        <ScimCapabilitySummary discovery={discovery} />
      </section>
    </>
  );
}

function PermissionSummaryPanel({
  groups,
  groupStatus,
  members,
  memberStatus,
}: {
  groups: WorkspaceGroupDto[] | null;
  groupStatus: PermissionAdminApiStatus;
  members: WorkspaceMemberDto[] | null;
  memberStatus: PermissionAdminApiStatus;
}) {
  const activeMembers = members?.filter((member) => member.status === "active").length ?? 0;
  const activeGroups = groups?.filter((group) => !group.isArchived).length ?? 0;
  const externalGroups = groups?.filter((group) => group.externalProvider || group.externalGroupId).length ?? 0;

  return (
    <>
      <section className="permission-admin-section">
        <SectionTitle title="Permissions Summary" />
        <div className="permission-admin-summary-grid">
          <PermissionMetric label="Members" value={memberStatus === "ready" ? String(activeMembers) : statusLabel(memberStatus)} />
          <PermissionMetric label="Groups" value={groupStatus === "ready" ? String(activeGroups) : statusLabel(groupStatus)} />
          <PermissionMetric label="Directory-managed groups" value={groupStatus === "ready" ? String(externalGroups) : statusLabel(groupStatus)} />
          <PermissionMetric label="Editing" value="Advanced Permissions" />
        </div>
      </section>
      <section className="permission-admin-section">
        <SectionTitle title="Access Boundaries" />
        <div className="permission-admin-table">
          <div className="permission-admin-table-head is-capability">
            <span>Surface</span>
            <span>Responsibility</span>
            <span>V1 boundary</span>
          </div>
          {[
            ["Members", "Workspace member lifecycle management.", "No pending invitation lifecycle."],
            ["Groups", "Workspace group visibility and directory source context.", "Group mutation UI is deferred."],
            ["Advanced Permissions", "Document and collection grants for users and groups.", "Opened from resource context."],
            ["Access & Sharing", "Share-link governance and public exposure.", "Share V1 behavior unchanged."],
            ["Settings", "Workspace policy, security, and integrations.", "Members management has moved out."],
          ].map(([surface, responsibility, boundary]) => (
            <article className="permission-admin-table-row is-capability" key={surface}>
              <span className="permission-admin-cell-text">{surface}</span>
              <span className="permission-admin-cell-text">{responsibility}</span>
              <span className="permission-admin-cell-text">{boundary}</span>
            </article>
          ))}
        </div>
      </section>
    </>
  );
}

function ScimDiscoveryPanel({ discovery }: { discovery: ReturnType<typeof useScimDiscovery> }) {
  if (!discovery.config || !discovery.resourceTypes || !discovery.schemas) {
    return <EmptyState status={discovery.status} />;
  }

  return (
    <div className="permission-admin-discovery-grid">
      <PermissionMetric label="Patch" value={discovery.config.patch.supported ? "Supported" : "Disabled"} />
      <PermissionMetric label="Filter" value={discovery.config.filter.supported ? `Max ${discovery.config.filter.maxResults}` : "Disabled"} />
      <PermissionMetric label="Bulk" value={discovery.config.bulk.supported ? "Supported" : "Deferred"} />
      <PermissionMetric label="Schemas" value={String(discovery.schemas.totalResults)} />
      <PermissionMetric label="Resources" value={discovery.resourceTypes.resources.map((resource) => resource.name).join(", ")} />
      <PermissionMetric label="Auth" value={discovery.config.authenticationSchemes[0]?.type ?? "Bearer"} />
    </div>
  );
}

function ScimTokenReadOnlyList({ scimTokens }: { scimTokens: ReturnType<typeof useScimTokens> }) {
  if (!scimTokens.tokens) {
    return <EmptyState status={scimTokens.status} />;
  }

  if (scimTokens.tokens.length === 0) {
    return <EmptyState label="No SCIM tokens" />;
  }

  return (
    <div className="permission-admin-table">
      <div className="permission-admin-table-head is-tokens">
        <span>Name</span>
        <span>Status</span>
        <span>Expires</span>
        <span>Last Used</span>
        <span>Management</span>
      </div>
      {scimTokens.tokens.map((token) => (
        <article className="permission-admin-table-row is-tokens" key={token.id}>
          <span className="permission-admin-identity">
            <Avatar initials={getInitials(token.name)} tone="scim" />
            <span className="min-w-0">
              <strong title={token.name}>{token.name}</strong>
              <small>{shortId(token.id)}</small>
            </span>
          </span>
          <StatusPill label={getTokenStatus(token)} />
          <span className="permission-admin-cell-text">{token.expiresAt ? formatDateTime(token.expiresAt) : "No expiry"}</span>
          <span className="permission-admin-cell-text">{token.lastUsedAt ? formatDateTime(token.lastUsedAt) : "Never"}</span>
          <span className="permission-admin-cell-text">
            <a href="#settings?scope=workspace&tab=integrations">Settings Integrations</a>
          </span>
        </article>
      ))}
    </div>
  );
}

function ScimTokenManager({ scimTokens }: { scimTokens: ReturnType<typeof useScimTokens> }) {
  const [draft, setDraft] = useState({ name: "", expiresAt: "" });
  const [confirmRevokeTokenId, setConfirmRevokeTokenId] = useState<string | null>(null);
  const canMutate = scimTokens.canUse && scimTokens.status !== "forbidden";
  const isValid = draft.name.trim().length > 0;

  const submit = () => {
    if (!canMutate || !isValid) {
      return;
    }

    void scimTokens.createToken({
      name: draft.name.trim(),
      expiresAt: toApiDateTime(draft.expiresAt),
    }).then((created) => {
      if (created) {
        setDraft({ name: "", expiresAt: "" });
      }
    });
  };

  const revoke = (token: ScimTokenDto) => {
    if (confirmRevokeTokenId !== token.id) {
      setConfirmRevokeTokenId(token.id);
      return;
    }

    void scimTokens.revokeToken(token.id).then((revoked) => {
      if (revoked) {
        setConfirmRevokeTokenId(null);
      }
    });
  };

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
              <Copy className="h-4 w-4" />
              Copy
            </button>
            <button onClick={scimTokens.dismissCreated} title="Dismiss one-time SCIM token" type="button">
              <X className="h-4 w-4" />
              Dismiss
            </button>
            <small>Raw token is returned once and is not shown in token lists.</small>
          </div>
        ) : null}
        <div className="permission-admin-form-grid is-scim-token">
          <Field label="Token Name" htmlFor="scim-token-name">
            <input
              disabled={!canMutate}
              id="scim-token-name"
              onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))}
              placeholder="Okta production"
              type="text"
              value={draft.name}
            />
          </Field>
          <Field label="Expiry" htmlFor="scim-token-expiry">
            <input
              disabled={!canMutate}
              id="scim-token-expiry"
              onChange={(event) => setDraft((current) => ({ ...current, expiresAt: event.target.value }))}
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
            type="button"
          >
            <KeyRound className="h-4 w-4" />
            {scimTokens.operation === "create-token" ? "Creating" : "Create Token"}
          </button>
        </div>
        <MutationStatus error={scimTokens.error} message={scimTokens.message} />
      </div>

      {!scimTokens.tokens ? (
        <EmptyState status={scimTokens.status} />
      ) : scimTokens.tokens.length === 0 ? (
        <EmptyState label="No SCIM tokens" />
      ) : (
        <div className="permission-admin-table">
          <div className="permission-admin-table-head is-tokens">
            <span>Name</span>
            <span>Status</span>
            <span>Expires</span>
            <span>Last Used</span>
            <span>Actions</span>
          </div>
          {scimTokens.tokens.map((token) => (
            <article className="permission-admin-table-row is-tokens" key={token.id}>
              <span className="permission-admin-identity">
                <Avatar initials={getInitials(token.name)} tone="scim" />
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
                  onClick={() => revoke(token)}
                  title={confirmRevokeTokenId === token.id ? "Confirm token revoke" : "Revoke token"}
                  type="button"
                >
                  {confirmRevokeTokenId === token.id ? "Confirm" : <Trash2 className="h-4 w-4" />}
                </button>
              </span>
            </article>
          ))}
        </div>
      )}
    </>
  );
}

function ScimCapabilitySummary({ discovery }: { discovery: ReturnType<typeof useScimDiscovery> }) {
  const resources = new Set(discovery.resourceTypes?.resources.map((resource) => resource.name.toLowerCase()) ?? []);
  const rows = [
    { name: "Users list/get/create/update", status: resources.has("user") ? "supported" : "not verified" },
    { name: "Groups list/get/create/update", status: resources.has("group") ? "supported" : "not verified" },
    { name: "Bearer token management", status: "supported" },
    { name: "Bulk operations", status: "deferred" },
    { name: "Complex filters", status: "deferred" },
    { name: "Enterprise extension", status: "deferred" },
    { name: "Delete/deactivate", status: "deferred" },
  ];

  return (
    <div className="permission-admin-table">
      <div className="permission-admin-table-head is-capability">
        <span>Capability</span>
        <span>Status</span>
        <span>Boundary</span>
      </div>
      {rows.map((row) => (
        <article className="permission-admin-table-row is-capability" key={row.name}>
          <span className="permission-admin-cell-text">{row.name}</span>
          <StatusPill label={row.status} />
          <span className="permission-admin-cell-text">
            {row.status === "deferred" ? "Not exposed in frontend V1" : "Existing backend contract"}
          </span>
        </article>
      ))}
    </div>
  );
}

function SectionTitle({ children, count, title }: { children?: ReactNode; count?: number; title: string }) {
  return (
    <div className="permission-admin-section-title">
      <div>
        <h2>{title}</h2>
        {typeof count === "number" ? <span>{count}</span> : null}
      </div>
      {children ? <div className="permission-admin-title-actions">{children}</div> : null}
    </div>
  );
}

function Field({
  children,
  htmlFor,
  label,
}: {
  children: ReactNode;
  htmlFor: string;
  label: string;
}) {
  return (
    <div className="permission-admin-field-row">
      <label htmlFor={htmlFor}>{label}</label>
      {children}
    </div>
  );
}

function PermissionMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="permission-admin-metric">
      <span>{label}</span>
      <strong title={value}>{value}</strong>
    </div>
  );
}

function EmptyState({ label, status }: { label?: string; status?: PermissionAdminApiStatus }) {
  const Icon = status === "forbidden" || status === "error" ? AlertTriangle : Database;
  return (
    <div className="permission-admin-empty-state">
      <Icon className="h-4 w-4" />
      <span>{label ?? statusLabel(status ?? "unconfigured")}</span>
    </div>
  );
}

function MutationStatus({ error, message }: { error: string | null; message: string | null }) {
  if (!error && !message) {
    return null;
  }

  return <div className={["permission-admin-inline-status", error ? "is-error" : ""].join(" ")}>{error ?? message}</div>;
}

function StatusPill({ label }: { label: string }) {
  return <span className={["permission-admin-status-pill", getStatusClass(label)].join(" ")}>{formatPermissionValue(label)}</span>;
}

function Avatar({ initials, tone }: { initials: string; tone: string }) {
  return <span className={["permission-admin-avatar", getAvatarClass(tone)].join(" ")}>{initials}</span>;
}

function useWorkspaceMembers(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredPermissionAdminApiBaseUrl(), []);
  const [members, setMembers] = useState<WorkspaceMemberDto[] | null>(null);
  const [status, setStatus] = useState<PermissionAdminApiStatus>(() =>
    apiBaseUrl && workspaceId ? "loading" : "unconfigured",
  );

  const loadMembers = () => {
    if (!apiBaseUrl || !workspaceId) {
      setMembers(null);
      setStatus("unconfigured");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getWorkspaceMembers(apiBaseUrl, workspaceId, controller.signal)
      .then((body) => {
        setMembers(body.members);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setStatus(getErrorStatus(error));
      });

    return controller;
  };

  useEffect(() => {
    const controller = loadMembers();
    return () => controller?.abort();
  }, [apiBaseUrl, workspaceId]);

  return { apiBaseUrl, members, reload: loadMembers, status };
}

function useCurrentWorkspaceIdentity(workspaceId: string | null) {
  const [identity, setIdentity] = useState<{ role: CurrentWorkspaceRole; userId: string | null }>({
    role: "unknown",
    userId: null,
  });

  useEffect(() => {
    if (!workspaceId) {
      setIdentity({ role: "unknown", userId: null });
      return;
    }

    let isActive = true;
    void getCurrentUser()
      .then((response) => {
        if (isActive) {
          setIdentity({
            role: getCurrentWorkspaceRole(response.workspaces, workspaceId),
            userId: response.user.id,
          });
        }
      })
      .catch(() => {
        if (isActive) {
          setIdentity({ role: "unknown", userId: null });
        }
      });

    return () => {
      isActive = false;
    };
  }, [workspaceId]);

  return identity;
}

function useWorkspaceGroups(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredPermissionAdminApiBaseUrl(), []);
  const [groups, setGroups] = useState<WorkspaceGroupDto[] | null>(null);
  const [status, setStatus] = useState<PermissionAdminApiStatus>(() =>
    apiBaseUrl && workspaceId ? "loading" : "unconfigured",
  );

  const loadGroups = () => {
    if (!apiBaseUrl || !workspaceId) {
      setGroups(null);
      setStatus("unconfigured");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getWorkspaceGroups(apiBaseUrl, workspaceId, controller.signal)
      .then((body) => {
        setGroups(body.groups);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setStatus(getErrorStatus(error));
      });

    return controller;
  };

  useEffect(() => {
    const controller = loadGroups();
    return () => controller?.abort();
  }, [apiBaseUrl, workspaceId]);

  return { groups, reload: loadGroups, status };
}

function useScimDiscovery(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredPermissionAdminApiBaseUrl(), []);
  const [config, setConfig] = useState<ScimServiceProviderConfigResponse | null>(null);
  const [resourceTypes, setResourceTypes] = useState<ScimListResponse<ScimResourceTypeDto> | null>(null);
  const [schemas, setSchemas] = useState<ScimListResponse<ScimSchemaDto> | null>(null);
  const [status, setStatus] = useState<PermissionAdminApiStatus>(() =>
    apiBaseUrl && workspaceId ? "loading" : "unconfigured",
  );

  const load = () => {
    if (!apiBaseUrl || !workspaceId) {
      setConfig(null);
      setResourceTypes(null);
      setSchemas(null);
      setStatus("unconfigured");
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
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setStatus(getErrorStatus(error));
      });

    return controller;
  };

  useEffect(() => {
    const controller = load();
    return () => controller?.abort();
  }, [apiBaseUrl, workspaceId]);

  return { config, reload: load, resourceTypes, schemas, status };
}

function useScimTokens(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredPermissionAdminApiBaseUrl(), []);
  const [tokens, setTokens] = useState<ScimTokenDto[] | null>(null);
  const [created, setCreated] = useState<{ rawToken: string; token: ScimTokenDto } | null>(null);
  const [operation, setOperation] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [status, setStatus] = useState<PermissionAdminApiStatus>(() =>
    apiBaseUrl && workspaceId ? "loading" : "unconfigured",
  );
  const canUse = Boolean(apiBaseUrl && workspaceId);

  const load = () => {
    if (!apiBaseUrl || !workspaceId) {
      setTokens(null);
      setStatus("unconfigured");
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
        if (errorValue instanceof DOMException && errorValue.name === "AbortError") {
          return;
        }

        setStatus(getErrorStatus(errorValue));
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
      setError(toMutationError(errorValue, "Could not create SCIM token."));
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
        current?.map((token) => (token.id === tokenId ? { ...token, revokedAt: new Date().toISOString() } : token)) ?? current,
      );
      setMessage("SCIM token revoked.");
      return true;
    } catch (errorValue) {
      setError(toMutationError(errorValue, "Could not revoke SCIM token."));
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

function useMemberMutations(apiBaseUrl: string, workspaceId: string | null, reloadMembers: () => AbortController | undefined) {
  const [operation, setOperation] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const canUse = Boolean(apiBaseUrl && workspaceId);

  const addMember = async (email: string, role: string) => {
    if (!apiBaseUrl || !workspaceId || operation) {
      return null;
    }
    if (!addExistingUserRoleOptions.includes(role as WorkspaceRoleOption)) {
      setError("Workspace member role is not supported.");
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
      setError(toMutationError(errorValue, "Could not add member."));
      return null;
    } finally {
      setOperation(null);
    }
  };

  const updateRole = async (userId: string, role: string) => {
    if (!apiBaseUrl || !workspaceId || operation) {
      return null;
    }
    if (!isSupportedWorkspaceMemberRole(role)) {
      setError("Workspace member role is not supported.");
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
      setError(toMutationError(errorValue, "Could not update member role."));
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
      setError(toMutationError(errorValue, "Could not remove member."));
      return false;
    } finally {
      setOperation(null);
    }
  };

  return { addMember, canUse, error, message, operation, removeMember, updateRole };
}

function getErrorStatus(error: unknown): PermissionAdminApiStatus {
  if (error instanceof PermissionAdminApiError && (error.status === 401 || error.status === 403)) {
    return "forbidden";
  }

  return "error";
}

function toMutationError(error: unknown, fallback: string) {
  return toPermissionMutationError(error, fallback);
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

function getInitials(value: string) {
  return value
    .trim()
    .split(/\s+/)
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase() || "NS";
}

function getAvatarClass(tone: string) {
  if (tone === "owner" || tone === "admin" || tone === "scim") {
    return "is-secure";
  }

  if (tone === "external" || tone === "group") {
    return "is-green";
  }

  if (tone === "editor") {
    return "is-blue";
  }

  return "is-sand";
}

function getStatusClass(label: string) {
  const normalized = label.toLowerCase();
  if (["active", "supported", "admin", "owner"].includes(normalized)) {
    return "is-active";
  }

  if (["revoked", "expired", "archived", "forbidden", "error"].includes(normalized)) {
    return "is-danger";
  }

  if (["deferred", "viewer", "editor"].includes(normalized)) {
    return "is-muted";
  }

  return "";
}

function toApiDateTime(value: string) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function formatDateTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Unknown";
  }

  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(date);
}

function formatPermissionValue(value: string) {
  return value
    .split("_")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function shortId(value: string) {
  return value.length > 12 ? `${value.slice(0, 8)}...${value.slice(-4)}` : value;
}

function statusLabel(status: PermissionAdminApiStatus) {
  if (status === "loading") {
    return "Loading";
  }

  if (status === "ready") {
    return "Ready";
  }

  if (status === "forbidden") {
    return "Forbidden";
  }

  if (status === "error") {
    return "Unavailable";
  }

  return "Not connected";
}
