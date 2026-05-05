import {
  AlertTriangle,
  Copy,
  Database,
  KeyRound,
  LockKeyhole,
  RefreshCw,
  ServerCog,
  ShieldCheck,
  Trash2,
  UserPlus,
  UsersRound,
  X,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { AtlasIcon } from "./AtlasIcon";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
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
import compassMarkUrl from "../assets/svg/decorative/compass-mark-small.svg";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

type PermissionAdminTab = "members" | "groups" | "scim";
type WorkspaceRoleOption = "admin" | "editor" | "viewer";

const memberRoleOptions: WorkspaceRoleOption[] = ["admin", "editor", "viewer"];
const permissionAdminWorkspaceId = getConfiguredPermissionAdminWorkspaceId();
const permissionAdminPatternStyle = {
  "--permission-admin-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--permission-admin-route-pattern": `url(${routePatternUrl})`,
  "--permission-admin-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

export function PermissionAdminSurfacesPage({ initialTab = "members" }: { initialTab?: PermissionAdminTab }) {
  const [activeTab, setActiveTab] = useState<PermissionAdminTab>(initialTab);
  const members = useWorkspaceMembers(permissionAdminWorkspaceId);
  const groups = useWorkspaceGroups(permissionAdminWorkspaceId);
  const scimDiscovery = useScimDiscovery(permissionAdminWorkspaceId);
  const scimTokens = useScimTokens(permissionAdminWorkspaceId);
  const memberMutations = useMemberMutations(members.apiBaseUrl, permissionAdminWorkspaceId, members.reload);

  useEffect(() => {
    setActiveTab(initialTab);
  }, [initialTab]);

  return (
    <main className="permission-admin-shell flex h-screen flex-col overflow-hidden" style={permissionAdminPatternStyle}>
      <WorkspaceHomeTopBar />
      <div className="permission-admin-body flex min-h-0 flex-1 overflow-hidden">
        <PermissionAdminSidebar activeTab={activeTab} />
        <section className="permission-admin-feed editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="permission-admin-feed-inner">
            <PermissionAdminHeader
              activeTab={activeTab}
              groups={groups.groups}
              members={members.members}
              workspaceId={permissionAdminWorkspaceId}
            />
            <PermissionAdminTabs activeTab={activeTab} onChange={setActiveTab} />
            <ConnectionSummary
              apiBaseUrl={members.apiBaseUrl}
              groupStatus={groups.status}
              memberStatus={members.status}
              scimStatus={scimDiscovery.status}
              tokenStatus={scimTokens.status}
              workspaceId={permissionAdminWorkspaceId}
            />

            {activeTab === "groups" ? (
              <WorkspaceGroupsPanel groups={groups.groups} status={groups.status} />
            ) : activeTab === "scim" ? (
              <ScimManagementPanel
                discovery={scimDiscovery}
                scimTokens={scimTokens}
                workspaceId={permissionAdminWorkspaceId}
              />
            ) : (
              <WorkspaceMembersPanel
                groups={groups.groups}
                memberMutations={memberMutations}
                members={members.members}
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

function PermissionAdminSidebar({ activeTab }: { activeTab: PermissionAdminTab }) {
  const navItems = [
    { id: "members" as const, href: "#workspace-members", label: "Members", icon: UsersRound },
    { id: "groups" as const, href: "#workspace-groups", label: "Groups", icon: ShieldCheck },
    { id: "scim" as const, href: "#scim", label: "SCIM", icon: ServerCog },
  ];

  return (
    <aside className="permission-admin-sidebar hidden h-full w-[320px] shrink-0 overflow-hidden md:flex md:flex-col">
      <div className="permission-admin-ruler" aria-hidden="true">
        <span>N 90</span>
        <span>N 60</span>
        <span>N 30</span>
        <span>0</span>
        <span>S 30</span>
        <span>S 60</span>
      </div>
      <div className="permission-admin-sidebar-inner editor-scrollbar">
        <nav aria-label="Permission admin navigation">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <a
                aria-current={activeTab === item.id ? "page" : undefined}
                className={["permission-admin-nav-item", activeTab === item.id ? "is-active" : ""].join(" ")}
                href={item.href}
                key={item.id}
                title={item.label}
              >
                <Icon className="h-4 w-4 shrink-0" />
                <span>{item.label}</span>
                {activeTab === item.id ? <span className="permission-admin-active-dot" /> : null}
              </a>
            );
          })}
          <a className="permission-admin-nav-item" href="#permissions" title="Document permissions">
            <LockKeyhole className="h-4 w-4 shrink-0" />
            <span>Document Share</span>
          </a>
        </nav>

        <div className="permission-admin-sidebar-note">
          <AtlasIcon className="mx-auto h-20 w-20 opacity-35" src={compassMarkUrl} />
          <p>Workspace-scoped admin surfaces use existing protected APIs only.</p>
        </div>
      </div>
    </aside>
  );
}

function PermissionAdminHeader({
  activeTab,
  groups,
  members,
  workspaceId,
}: {
  activeTab: PermissionAdminTab;
  groups: WorkspaceGroupDto[] | null;
  members: WorkspaceMemberDto[] | null;
  workspaceId: string | null;
}) {
  const activeMembers = members?.filter((member) => member.status === "active").length ?? 0;
  const activeGroups = groups?.filter((group) => !group.isArchived).length ?? 0;
  const heading = activeTab === "scim" ? "SCIM Management" : activeTab === "groups" ? "Workspace Groups" : "Workspace Members";
  const description =
    activeTab === "scim"
      ? "Manage SCIM discovery and workspace-scoped bearer tokens."
      : "Manage workspace access and inspect group-backed permission sources.";

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
  activeTab: PermissionAdminTab;
  onChange: (tab: PermissionAdminTab) => void;
}) {
  const tabs: Array<{ id: PermissionAdminTab; label: string; href: string }> = [
    { id: "members", label: "Members", href: "#workspace-members" },
    { id: "groups", label: "Groups", href: "#workspace-groups" },
    { id: "scim", label: "SCIM", href: "#scim" },
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

function ConnectionSummary({
  apiBaseUrl,
  groupStatus,
  memberStatus,
  scimStatus,
  tokenStatus,
  workspaceId,
}: {
  apiBaseUrl: string;
  groupStatus: PermissionAdminApiStatus;
  memberStatus: PermissionAdminApiStatus;
  scimStatus: PermissionAdminApiStatus;
  tokenStatus: PermissionAdminApiStatus;
  workspaceId: string | null;
}) {
  return (
    <section className="permission-admin-section">
      <div className="permission-admin-summary-grid">
        <PermissionMetric label="API" value={apiBaseUrl ? "Configured" : "Not connected"} />
        <PermissionMetric label="Workspace ID" value={workspaceId ? "Configured" : "Missing"} />
        <PermissionMetric label="Members" value={statusLabel(memberStatus)} />
        <PermissionMetric label="Groups" value={statusLabel(groupStatus)} />
        <PermissionMetric label="SCIM" value={statusLabel(scimStatus)} />
        <PermissionMetric label="SCIM Tokens" value={statusLabel(tokenStatus)} />
      </div>
    </section>
  );
}

function WorkspaceMembersPanel({
  groups,
  memberMutations,
  members,
  reloadGroups,
  reloadMembers,
  status,
}: {
  groups: WorkspaceGroupDto[] | null;
  memberMutations: ReturnType<typeof useMemberMutations>;
  members: WorkspaceMemberDto[] | null;
  reloadGroups: () => AbortController | undefined;
  reloadMembers: () => AbortController | undefined;
  status: PermissionAdminApiStatus;
}) {
  return (
    <>
      <AddMemberPanel mutations={memberMutations} status={status} />
      <section className="permission-admin-section">
        <SectionTitle count={members?.length ?? 0} title="Workspace Members">
          <button className="permission-admin-icon-button" onClick={() => reloadMembers()} title="Refresh members" type="button">
            <RefreshCw className="h-4 w-4" />
          </button>
        </SectionTitle>
        <WorkspaceMembersTable members={members} mutations={memberMutations} status={status} />
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
  mutations,
  status,
}: {
  mutations: ReturnType<typeof useMemberMutations>;
  status: PermissionAdminApiStatus;
}) {
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<WorkspaceRoleOption>("viewer");
  const canMutate = status === "ready" && mutations.canUse;
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
      <SectionTitle count={memberRoleOptions.length} title="Add Member" />
      <div className="permission-admin-form">
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
            title={!canMutate ? statusLabel(status) : undefined}
            type="button"
          >
            <UserPlus className="h-4 w-4" />
            {mutations.operation === "add-member" ? "Adding" : "Add Member"}
          </button>
        </div>
        <MutationStatus error={mutations.error} message={mutations.message} />
      </div>
    </section>
  );
}

function WorkspaceMembersTable({
  members,
  mutations,
  status,
}: {
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
      {members.map((member) => (
        <article className="permission-admin-table-row is-members" key={member.userId}>
          <span className="permission-admin-identity">
            <Avatar initials={getInitials(member.displayName || member.email || member.userId)} tone={member.role} />
            <span className="min-w-0">
              <strong title={member.displayName || member.userId}>{member.displayName || "Unnamed user"}</strong>
              <small>{shortId(member.userId)}</small>
            </span>
          </span>
          <span className="permission-admin-cell-text">{member.email ?? "No email"}</span>
          <WorkspaceRoleSelect
            disabled={!mutations.canUse || mutations.operation === member.userId}
            member={member}
            onChange={(role) => mutations.updateRole(member.userId, role)}
          />
          <StatusPill label={member.status} />
          <span className="permission-admin-cell-text">{member.joinedAt ? formatDateTime(member.joinedAt) : "Unknown"}</span>
          <span className="permission-admin-row-actions">
            <button
              className="permission-admin-icon-button is-danger"
              disabled={!mutations.canUse || mutations.operation === member.userId}
              onClick={() => remove(member)}
              title={confirmRemoveUserId === member.userId ? "Confirm member removal" : "Remove member"}
              type="button"
            >
              {confirmRemoveUserId === member.userId ? "Confirm" : <Trash2 className="h-4 w-4" />}
            </button>
            <button className="permission-admin-icon-button" disabled title="Reactivate API is not available" type="button">
              <RefreshCw className="h-4 w-4" />
            </button>
          </span>
        </article>
      ))}
    </div>
  );
}

function WorkspaceRoleSelect({
  disabled,
  member,
  onChange,
}: {
  disabled: boolean;
  member: WorkspaceMemberDto;
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
        setDraftRole(nextRole);
        void onChange(nextRole);
      }}
      title={member.role === "owner" ? "Owner can be downgraded only if backend ownership rules allow it" : "Update role"}
      value={draftRole}
    >
      {member.role === "owner" ? (
        <option disabled value="owner">
          Owner
        </option>
      ) : null}
      {memberRoleOptions.map((role) => (
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
  if (!groups) {
    return (
      <section className="permission-admin-section">
        <SectionTitle title="Workspace Groups" />
        <EmptyState status={status} />
      </section>
    );
  }

  return (
    <section className="permission-admin-section">
      <SectionTitle count={groups.length} title="Workspace Groups" />
      {groups.length === 0 ? (
        <EmptyState label="No workspace groups" />
      ) : (
        <div className="permission-admin-table">
          <div className="permission-admin-table-head is-group-detail">
            <span>Name</span>
            <span>State</span>
            <span>External Source</span>
            <span>Members</span>
            <span>Updated</span>
            <span>Actions</span>
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
                {group.externalProvider ? (
                  <>
                    {group.externalProvider}
                    <small>{group.externalGroupId ?? "No external id"}</small>
                  </>
                ) : (
                  "Local"
                )}
              </span>
              <span className="permission-admin-cell-text">{group.membersCount}</span>
              <span className="permission-admin-cell-text">{formatDateTime(group.updatedAt)}</span>
              <span className="permission-admin-row-actions">
                <button
                  className="permission-admin-icon-button"
                  disabled
                  title={
                    group.externalProvider
                      ? "IAM-managed groups are read-only in local group APIs"
                      : "Group mutation UI is outside V1"
                  }
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
  );
}

function ScimManagementPanel({
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
        <SectionTitle title="SCIM Endpoint" />
        <div className="permission-admin-endpoint">
          <ServerCog className="h-5 w-5" />
          <code>{`/api/v1/workspaces/${workspaceId ?? "{workspaceId}"}/scim/v2`}</code>
        </div>
      </section>

      <section className="permission-admin-section">
        <SectionTitle title="Discovery" />
        <ScimDiscoveryPanel discovery={discovery} />
      </section>

      <section className="permission-admin-section">
        <SectionTitle count={scimTokens.tokens?.length ?? 0} title="SCIM Bearer Tokens">
          <button className="permission-admin-icon-button" onClick={() => scimTokens.reload()} title="Refresh SCIM tokens" type="button">
            <RefreshCw className="h-4 w-4" />
          </button>
        </SectionTitle>
        <ScimTokenManager scimTokens={scimTokens} />
      </section>

      <section className="permission-admin-section">
        <SectionTitle title="Provisioning Capabilities" />
        <ScimCapabilitySummary discovery={discovery} />
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
  if (error instanceof PermissionAdminApiError && (error.status === 401 || error.status === 403)) {
    return "You do not have permission to change this workspace.";
  }

  if (error instanceof PermissionAdminApiError && error.status === 409) {
    return "The requested change conflicts with current workspace state.";
  }

  return fallback;
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
