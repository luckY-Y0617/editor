import {
  CalendarDays,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Copy,
  Edit3,
  Eye,
  FileText,
  Link2,
  LockKeyhole,
  MoreHorizontal,
  Plus,
  Save,
  ShieldCheck,
  Tag,
  Trash2,
  UserPlus,
  UserRound,
  UsersRound,
  X,
} from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import { AtlasIcon } from "./AtlasIcon";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import {
  createApiHeaders,
  getConfiguredApiBaseUrl,
  getConfiguredWorkspaceId,
  isUuid,
} from "../lib/apiClient";
import {
  createDocumentShareLink,
  getBootstrap,
  getDocumentShareLinks,
  revokeShareLink,
  type BootstrapResponse,
  type CreateShareLinkResponse,
  type ShareLinkAudience,
  type ShareLinkDto,
  type ShareLinkRole,
} from "../lib/appApi";
import {
  createShareLinkRequest,
  createWorkspaceShareLinkRequest,
  getShareLinkCapability,
  resolveShareTarget,
  toAbsoluteShareUrl,
  toSharePermissionMutationError,
  type ShareTargetResolution,
} from "../lib/documentShareLinksModel";
import { getShareDocumentIdFromHash } from "../lib/hashRouting";
import {
  permissionRoleSummaries,
  recentAccessEvents,
  workspaceAccessMembers,
  type EffectiveDocumentAccess,
  type RecentAccessEvent,
  type ShareDocumentContext,
  type WorkspaceAccessMember,
  type WorkspaceRole,
} from "../data/sharePermissionsData";
import compassMarkUrl from "../assets/svg/decorative/compass-mark-small.svg";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

const sharePatternStyle = {
  "--share-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--share-route-pattern": `url(${routePatternUrl})`,
  "--share-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

const shareBreadcrumbs = ["Atlas Library", "01. Foundations", "Mission & Vision", "Share"];
const shareTabs = ["People", "Groups", "Access Settings"];

type PermissionApiStatus = "unconfigured" | "loading" | "ready" | "forbidden" | "error";
type PermissionGrantSubjectType = "user" | "group";
type PermissionLinkMode = "disabled" | "internal" | "external";
type PermissionInheritanceMode = "inherit" | "restricted";

type PermissionPolicyDto = {
  inheritanceMode: string;
  linkMode: string;
  defaultLinkRole: string | null;
};

type PermissionGrantDto = {
  id: string;
  subjectType: string;
  subjectId: string;
  roleKey: string;
  grantedBy: string | null;
  grantedAt: string;
  expiresAt: string | null;
  reason: string | null;
};

type EffectivePermissionResponse = {
  allowedActions: string[];
  effectiveRole: string | null;
  source: string;
  inheritanceMode: string;
};

type ResourcePermissionsResponse = {
  resourceType: string;
  resourceId: string;
  policy: PermissionPolicyDto;
  grants: PermissionGrantDto[];
  effectiveAccess: EffectivePermissionResponse;
  inheritedFrom: string;
  availableRoles: string[];
};

type WorkspaceGroupDto = {
  id: string;
  workspaceId: string;
  name: string;
  description: string | null;
  type: string;
  isArchived: boolean;
  externalProvider?: string | null;
  externalGroupId?: string | null;
  externalSyncedAt?: string | null;
  membersCount: number;
};

type WorkspaceGroupsResponse = {
  groups: WorkspaceGroupDto[];
};

type AccessRequestDto = {
  id: string;
  requesterId: string;
  subjectType: string;
  subjectId: string;
  requestedRole: string;
  reason: string | null;
  status: string;
  createdAt: string;
};

type AccessRequestsResponse = {
  requests: AccessRequestDto[];
};

type EmailInviteDto = {
  id: string;
  workspaceId: string;
  resourceType: string;
  resourceId: string;
  email: string;
  roleKey: "viewer" | "commenter";
  status: "pending" | "accepted" | "revoked" | "expired";
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

type EmailInvitesResponse = {
  invites: EmailInviteDto[];
};

type CreateEmailInviteResponse = {
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

type RequestAccessStatus = "idle" | "submitting" | "pending" | "error";
type GrantDraft = {
  subjectId: string;
  roleKey: string;
  expiresAt: string;
  reason: string;
};

type PolicyDraft = {
  inheritanceMode: PermissionInheritanceMode;
  linkMode: PermissionLinkMode;
  defaultLinkRole: ShareLinkRole;
};

export function DocumentSharePermissionsPage() {
  const [activeTab, setActiveTab] = useState(shareTabs[0]);
  const shareTarget = useSharePermissionTarget();
  const permissionApiDocumentId = shareTarget.documentId;
  const permissionApiWorkspaceId = shareTarget.workspaceId;
  const { permissions, reload: reloadPermissions, status } = useResourcePermissions(permissionApiDocumentId);
  const { groups, status: groupStatus } = useWorkspaceGroups(permissionApiWorkspaceId);
  const {
    requests: accessRequests,
    reviewError,
    reviewRequest,
    reviewingRequestId,
    status: requestStatus,
  } = useResourceAccessRequests(permissionApiDocumentId);
  const requestAccess = useRequestAccess(permissionApiDocumentId);
  const permissionMutations = usePermissionMutations(permissionApiDocumentId, reloadPermissions);
  const shareLinks = useShareLinks(permissionApiDocumentId, reloadPermissions);
  const emailInvites = useEmailInvites(permissionApiDocumentId, reloadPermissions);
  const isForbidden = status === "forbidden";
  const primaryDisabled = isForbidden
    ? !requestAccess.canRequest || requestAccess.status === "submitting" || requestAccess.status === "pending"
    : status !== "ready";

  return (
    <main className="share-permissions-shell flex h-screen flex-col overflow-hidden" style={sharePatternStyle}>
      <WorkspaceHomeTopBar />
      <ShareBreadcrumbs />
      <div className="share-permissions-body min-h-0 flex-1 overflow-hidden">
        <DocumentContextPanel context={shareTarget.documentContext} resolution={shareTarget.resolution} status={shareTarget.status} />
        <section className="share-permissions-main editor-scrollbar min-w-0 overflow-y-auto">
          <div className="share-permissions-main-inner">
            <header className="share-permissions-heading">
              <div className="min-w-0">
                <h1>Share &amp; Permissions</h1>
                <p>Manage workspace-based access for this document.</p>
              </div>
              <button
                className="share-permissions-primary-action"
                disabled={primaryDisabled}
                onClick={() => {
                  if (isForbidden) {
                    requestAccess.request();
                    return;
                  }

                  setActiveTab("People");
                }}
                title={isForbidden ? "Request viewer access" : "Create a user or group document grant"}
                type="button"
              >
                <UserPlus className="h-4 w-4" />
                {isForbidden ? requestAccessButtonLabel(requestAccess.status) : "Add Grant"}
              </button>
            </header>

            <nav className="share-permissions-tabs" aria-label="Share sections">
              {shareTabs.map((tab) => (
                <button
                  className={activeTab === tab ? "is-active" : ""}
                  key={tab}
                  onClick={() => setActiveTab(tab)}
                  title={tab}
                  type="button"
                >
                  {tab}
                </button>
              ))}
            </nav>

            <PermissionApiSummary permissions={permissions} status={status} />

            {activeTab === "Groups" ? (
              <WorkspaceGroupsPanel
                groups={groups}
                groupStatus={groupStatus}
                mutations={permissionMutations}
                permissions={permissions}
                permissionStatus={status}
              />
            ) : activeTab === "Access Settings" ? (
              <AccessSettingsPanel mutations={permissionMutations} permissions={permissions} status={status} />
            ) : (
              <PeoplePermissionsPanel mutations={permissionMutations} permissions={permissions} status={status} />
            )}
          </div>
        </section>
        <ShareSettingsPanel
          accessRequests={accessRequests}
          permissions={permissions}
          requestAccess={requestAccess}
          requestStatus={requestStatus}
          reviewError={reviewError}
          reviewRequest={reviewRequest}
          reviewingRequestId={reviewingRequestId}
          emailInvites={emailInvites}
          shareLinks={shareLinks}
          status={status}
        />
      </div>
    </main>
  );
}

function useSharePermissionTarget() {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const configuredDocument = useMemo(() => getConfiguredPermissionDocumentTarget(), []);
  const configuredWorkspaceId = useMemo(() => getConfiguredPermissionWorkspaceId(), []);
  const [bootstrap, setBootstrap] = useState<BootstrapResponse | null>(null);
  const [bootstrapStatus, setBootstrapStatus] = useState<PermissionApiStatus>(() =>
    apiBaseUrl ? "loading" : "unconfigured",
  );

  useEffect(() => {
    if (!apiBaseUrl) {
      setBootstrap(null);
      setBootstrapStatus("unconfigured");
      return;
    }

    const controller = new AbortController();
    setBootstrapStatus("loading");
    getBootstrap(controller.signal)
      .then((response) => {
        setBootstrap(response);
        setBootstrapStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setBootstrap(null);
        setBootstrapStatus(isForbiddenError(error) ? "forbidden" : "error");
      });

    return () => controller.abort();
  }, [apiBaseUrl]);

  const resolution = resolveShareTarget({
    apiConfigured: Boolean(apiBaseUrl),
    bootstrapDocumentId: bootstrap?.activeDocumentId,
    bootstrapWorkspaceId: bootstrap?.workspace.id,
    configuredDocumentId: configuredDocument.documentId,
    configuredDocumentSource: configuredDocument.source,
    configuredWorkspaceId,
  });
  const needsBootstrapForTarget = Boolean(apiBaseUrl && !configuredDocument.documentId);
  const status: PermissionApiStatus = !apiBaseUrl
    ? "unconfigured"
    : needsBootstrapForTarget && bootstrapStatus === "loading"
      ? "loading"
      : resolution.documentId
        ? "ready"
        : bootstrapStatus === "forbidden" || bootstrapStatus === "error"
          ? bootstrapStatus
          : "unconfigured";

  return {
    documentContext: createShareDocumentContext(bootstrap, resolution, status),
    documentId: resolution.documentId,
    resolution,
    status,
    workspaceId: resolution.workspaceId,
  };
}

function ShareBreadcrumbs() {
  return (
    <header className="share-permissions-breadcrumb-row">
      <nav className="share-permissions-breadcrumbs" aria-label="Document location">
        {shareBreadcrumbs.map((breadcrumb, index) => {
          const isLast = index === shareBreadcrumbs.length - 1;

          return (
            <span className={isLast ? "is-current" : ""} key={breadcrumb}>
              <a href={isLast ? "#share" : "#editor"}>{breadcrumb}</a>
              {!isLast ? <ChevronRight className="h-4 w-4" aria-hidden="true" /> : null}
            </span>
          );
        })}
      </nav>
    </header>
  );
}

function DocumentContextPanel({
  context,
  resolution,
  status,
}: {
  context: ShareDocumentContext;
  resolution: ShareTargetResolution;
  status: PermissionApiStatus;
}) {
  return (
    <aside className="share-permissions-context editor-scrollbar overflow-y-auto">
      <div className="share-permissions-ruler" aria-hidden="true">
        <span>N 60</span>
        <span>N 40</span>
        <span>N 20</span>
        <span>0</span>
        <span>S 20</span>
        <span>S 40</span>
      </div>
      <div className="share-permissions-context-inner">
        <h2>Document Context</h2>
        <section className="share-permissions-document-card">
          <span className="share-permissions-document-icon">
            <FileText className="h-6 w-6" />
          </span>
          <div className="min-w-0">
            <h3>{context.title}</h3>
            <p>{context.location}</p>
            <StatusBadge label={context.status} />
          </div>
        </section>

        <ContextMeta icon={<UserRound className="h-4 w-4" />} label="Owner">
          <Avatar initials={context.owner.initials} tone="green" />
          {context.owner.name}
        </ContextMeta>
        <ContextMeta icon={<CalendarDays className="h-4 w-4" />} label="Updated">
          {context.updatedAt}
        </ContextMeta>

        <dl className="share-permissions-context-stats">
          <div>
            <dt>Version</dt>
            <dd>{context.version}</dd>
          </div>
          <div>
            <dt>Readers</dt>
            <dd>{context.readers}</dd>
          </div>
          <div>
            <dt>Tags</dt>
            <dd>
              {context.tags.length ? context.tags.map((tag) => (
                <span key={tag}>{tag}</span>
              )) : <span>{statusLabel(status)}</span>}
              <button title="Tag management is planned for a later phase" type="button">
                <Plus className="h-3.5 w-3.5" />
              </button>
            </dd>
          </div>
        </dl>
        <div className="share-permissions-context-note">
          <strong>Share target</strong>
          <span>{resolution.documentId ? `${resolution.source} document` : resolution.reason ?? statusLabel(status)}</span>
        </div>

        <div className="share-permissions-context-map" aria-hidden="true">
          <AtlasIcon className="h-20 w-20 opacity-30" src={compassMarkUrl} />
        </div>
      </div>
    </aside>
  );
}

function ContextMeta({ children, icon, label }: { children: ReactNode; icon: ReactNode; label: string }) {
  return (
    <section className="share-permissions-context-meta">
      <h3>{label}</h3>
      <div>
        {icon}
        {children}
      </div>
    </section>
  );
}

function PeoplePermissionsPanel({
  mutations,
  permissions,
  status,
}: {
  mutations: ReturnType<typeof usePermissionMutations>;
  permissions: ResourcePermissionsResponse | null;
  status: PermissionApiStatus;
}) {
  const userGrants = permissions?.grants.filter((grant) => grant.subjectType === "user") ?? [];

  return (
    <>
      <CreateGrantPanel mutations={mutations} permissions={permissions} status={status} subjectType="user" />
      <section className="share-permissions-section">
        <div className="share-permissions-section-title">
          <h2>Scoped Grants</h2>
          <span>{userGrants.length}</span>
        </div>
        <ScopedGrantTable grants={userGrants} mutations={mutations} permissions={permissions} status={status} />
      </section>

      <section className="share-permissions-section">
        <div className="share-permissions-section-title">
          <h2>Workspace Members</h2>
          <span>{workspaceAccessMembers.length}</span>
        </div>
        <WorkspaceAccessTable />
      </section>

      <section className="share-permissions-section">
        <h2>Permission Roles</h2>
        <div className="share-permissions-role-list">
          {permissionRoleSummaries.map((role) => (
            <article className="share-permissions-role-row" key={role.id}>
              <span className={["share-permissions-role-icon", `is-${role.id}`].join(" ")}>
                {getRoleIcon(role.id)}
              </span>
              <div className="min-w-0">
                <h3>{role.label}</h3>
                <p>{role.description}</p>
              </div>
              <span className={["share-permissions-access-badge", getAccessClass(role.access)].join(" ")}>
                {role.access}
              </span>
              <ChevronDown className="h-4 w-4 text-[var(--ns-slate-700)]" />
            </article>
          ))}
        </div>
      </section>
    </>
  );
}

function WorkspaceGroupsPanel({
  groups,
  groupStatus,
  mutations,
  permissions,
  permissionStatus,
}: {
  groups: WorkspaceGroupDto[] | null;
  groupStatus: PermissionApiStatus;
  mutations: ReturnType<typeof usePermissionMutations>;
  permissions: ResourcePermissionsResponse | null;
  permissionStatus: PermissionApiStatus;
}) {
  const groupGrants = permissions?.grants.filter((grant) => grant.subjectType === "group") ?? [];
  const groupsById = new Map((groups ?? []).map((group) => [group.id, group]));

  return (
    <>
      <CreateGrantPanel
        groups={groups}
        groupStatus={groupStatus}
        mutations={mutations}
        permissions={permissions}
        status={permissionStatus}
        subjectType="group"
      />
      <section className="share-permissions-section">
        <div className="share-permissions-section-title">
          <h2>Workspace Groups</h2>
          <span>{groups?.length ?? 0}</span>
        </div>
        {!groups ? (
          <div className="share-permissions-empty-state">{statusLabel(groupStatus)}</div>
        ) : groups.length === 0 ? (
          <div className="share-permissions-empty-state">No workspace groups</div>
        ) : (
          <div className="share-permissions-grants-list">
            {groups.map((group) => (
              <article className="share-permissions-grant-row" key={group.id}>
                <span className="share-permissions-member-name">
                  <Avatar initials={group.name.slice(0, 1).toUpperCase()} tone="green" />
                  <strong>{group.name}</strong>
                </span>
                <span
                  className={["share-permissions-access-badge", isExternalGroup(group) ? "is-manage" : "is-view"].join(" ")}
                  title={group.externalProvider ? `Managed by ${group.externalProvider}` : "Local workspace group"}
                >
                  {group.externalProvider ?? formatPermissionValue(group.type)}
                </span>
                <span>{group.membersCount} members</span>
                <button
                  className="share-permissions-icon-button"
                  disabled
                  title={isExternalGroup(group) ? "IAM-managed groups are read-only" : "Manage group membership in workspace member tools"}
                  type="button"
                >
                  <MoreHorizontal className="h-4 w-4" />
                </button>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="share-permissions-section">
        <div className="share-permissions-section-title">
          <h2>Group Grants</h2>
          <span>{groupGrants.length}</span>
        </div>
        {!permissions ? (
          <div className="share-permissions-empty-state">{statusLabel(permissionStatus)}</div>
        ) : groupGrants.length === 0 ? (
          <div className="share-permissions-empty-state">No direct group grants on this document</div>
        ) : (
          <ScopedGrantTable
            grants={groupGrants}
            groupsById={groupsById}
            mutations={mutations}
            permissions={permissions}
            status={permissionStatus}
          />
        )}
      </section>
    </>
  );
}

function isExternalGroup(group: WorkspaceGroupDto) {
  return Boolean(group.externalProvider || group.externalGroupId);
}

function WorkspaceAccessTable() {
  return (
    <div className="share-permissions-table">
      <div className="share-permissions-table-head">
        <span>Name</span>
        <span>Email</span>
        <span>Workspace Role</span>
        <span>Effective Access</span>
        <span>Added</span>
        <span>Actions</span>
      </div>
      {workspaceAccessMembers.map((member) => (
        <WorkspaceAccessRow key={member.id} member={member} />
      ))}
    </div>
  );
}

function WorkspaceAccessRow({ member }: { member: WorkspaceAccessMember }) {
  return (
    <article className="share-permissions-table-row">
      <span className="share-permissions-member-name">
        <Avatar initials={member.initials} tone={member.avatarTone} />
        <strong>{member.name}</strong>
      </span>
      <span className="min-w-0 truncate">{member.email}</span>
      <button
        className="share-permissions-role-select"
        disabled
        title="Workspace role mutation is deferred to the member management API"
        type="button"
      >
        {formatRole(member.role)}
        <ChevronDown className="h-3.5 w-3.5" />
      </button>
      <span className={["share-permissions-access-badge", getAccessClass(member.effectiveAccess)].join(" ")}>
        {member.effectiveAccess}
      </span>
      <span>{member.addedAt}</span>
      <button aria-label={`More actions for ${member.name}`} className="share-permissions-icon-button" type="button">
        <MoreHorizontal className="h-4 w-4" />
      </button>
    </article>
  );
}

function PermissionApiSummary({
  permissions,
  status,
}: {
  permissions: ResourcePermissionsResponse | null;
  status: PermissionApiStatus;
}) {
  return (
    <section className="share-permissions-section">
      <div className="share-permissions-api-summary">
        <PermissionMetric label="Policy" value={permissions?.policy.inheritanceMode ?? statusLabel(status)} />
        <PermissionMetric label="Inherited From" value={permissions?.inheritedFrom ?? "workspace"} />
        <PermissionMetric label="Effective Role" value={permissions?.effectiveAccess.effectiveRole ?? "unknown"} />
        <PermissionMetric label="Source" value={permissions?.effectiveAccess.source ?? "workspace"} />
      </div>
    </section>
  );
}

function PermissionMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="share-permissions-api-metric">
      <span>{label}</span>
      <strong>{formatPermissionValue(value)}</strong>
    </div>
  );
}

function AccessSettingsPanel({
  mutations,
  permissions,
  status,
}: {
  mutations: ReturnType<typeof usePermissionMutations>;
  permissions: ResourcePermissionsResponse | null;
  status: PermissionApiStatus;
}) {
  const [draft, setDraft] = useState<PolicyDraft>({
    inheritanceMode: "inherit",
    linkMode: "disabled",
    defaultLinkRole: "viewer",
  });
  const canMutate = status === "ready" && Boolean(permissions) && mutations.canUse;

  useEffect(() => {
    if (!permissions) {
      return;
    }

    setDraft({
      inheritanceMode: normalizeInheritanceMode(permissions.policy.inheritanceMode),
      linkMode: normalizeLinkMode(permissions.policy.linkMode),
      defaultLinkRole: normalizeShareLinkRole(permissions.policy.defaultLinkRole),
    });
  }, [permissions]);

  const savePolicy = () => {
    if (!canMutate) {
      return;
    }

    void mutations.updatePolicy({
      inheritanceMode: draft.inheritanceMode,
      linkMode: draft.linkMode,
      defaultLinkRole: draft.linkMode === "disabled" ? null : draft.defaultLinkRole,
    });
  };

  return (
    <>
      <section className="share-permissions-section">
        <div className="share-permissions-section-title">
          <h2>Access Settings</h2>
          <span>{permissions ? "3" : "0"}</span>
        </div>
        <div className="share-permissions-form">
          <div className="share-permissions-form-grid">
            <div className="share-permissions-field-row">
              <label htmlFor="permission-inheritance-mode">Inheritance</label>
              <select
                disabled={!canMutate}
                id="permission-inheritance-mode"
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    inheritanceMode: normalizeInheritanceMode(event.target.value),
                  }))
                }
                value={draft.inheritanceMode}
              >
                <option value="inherit">Inherit parent access</option>
                <option value="restricted">Restrict to direct grants</option>
              </select>
            </div>
            <div className="share-permissions-field-row">
              <label htmlFor="permission-link-mode">Link Mode</label>
              <select
                disabled={!canMutate}
                id="permission-link-mode"
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    linkMode: normalizeLinkMode(event.target.value),
                  }))
                }
                value={draft.linkMode}
              >
                <option value="disabled">Disabled</option>
                <option value="internal">Internal</option>
                <option value="external">External</option>
              </select>
            </div>
            <div className="share-permissions-field-row">
              <label htmlFor="permission-default-link-role">Default Link Role</label>
              <select
                disabled={!canMutate || draft.linkMode === "disabled"}
                id="permission-default-link-role"
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    defaultLinkRole: normalizeShareLinkRole(event.target.value),
                  }))
                }
                value={draft.defaultLinkRole}
              >
                <option value="viewer">Viewer</option>
                <option value="commenter">Commenter</option>
              </select>
            </div>
            <div className="share-permissions-policy-boundary">
              <strong>Public links</strong>
              <span title="Public links are created only through the share-link API">Share-link API only</span>
            </div>
          </div>
          <div className="share-permissions-action-row">
            <button
              className="share-permissions-inline-action"
              disabled={!canMutate || mutations.operation === "updating-policy"}
              onClick={savePolicy}
              title={!canMutate ? statusLabel(status) : "Save access settings"}
              type="button"
            >
              <Save className="h-3.5 w-3.5" />
              {mutations.operation === "updating-policy" ? "Saving" : "Save access settings"}
            </button>
            {mutations.error ? <span className="share-permissions-inline-status is-error">{mutations.error}</span> : null}
            {mutations.message ? <span className="share-permissions-inline-status">{mutations.message}</span> : null}
          </div>
        </div>
      </section>

      <section className="share-permissions-section">
        <div className="share-permissions-api-summary">
          <PermissionMetric label="Current Policy" value={permissions?.policy.inheritanceMode ?? statusLabel(status)} />
          <PermissionMetric label="Current Link Mode" value={permissions?.policy.linkMode ?? "unknown"} />
          <PermissionMetric label="Default Link Role" value={permissions?.policy.defaultLinkRole ?? "none"} />
          <PermissionMetric label="Effective Role" value={permissions?.effectiveAccess.effectiveRole ?? "unknown"} />
        </div>
      </section>
    </>
  );
}

function RoleSelect({
  disabled,
  id,
  onChange,
  roles,
  value,
}: {
  disabled: boolean;
  id: string;
  onChange: (roleKey: string) => void;
  roles: string[];
  value: string;
}) {
  return (
    <div className="share-permissions-field-row">
      <label htmlFor={id}>Role</label>
      <select disabled={disabled || roles.length === 0} id={id} onChange={(event) => onChange(event.target.value)} value={value}>
        {roles.map((role) => (
          <option key={role} value={role}>
            {formatPermissionValue(role)}
          </option>
        ))}
      </select>
    </div>
  );
}

function CreateGrantPanel({
  groups,
  groupStatus,
  mutations,
  permissions,
  status,
  subjectType,
}: {
  groups?: WorkspaceGroupDto[] | null;
  groupStatus?: PermissionApiStatus;
  mutations: ReturnType<typeof usePermissionMutations>;
  permissions: ResourcePermissionsResponse | null;
  status: PermissionApiStatus;
  subjectType: PermissionGrantSubjectType;
}) {
  const availableRoles = getAvailableRoles(permissions);
  const [draft, setDraft] = useState<GrantDraft>(() => ({
    subjectId: "",
    roleKey: availableRoles[0] ?? "viewer",
    expiresAt: "",
    reason: "",
  }));
  const groupOptions = (groups ?? []).filter((group) => !group.isArchived);
  const isGroupGrant = subjectType === "group";
  const canMutate = status === "ready" && Boolean(permissions) && mutations.canUse;
  const subjectId = isGroupGrant ? draft.subjectId || groupOptions[0]?.id || "" : draft.subjectId.trim();
  const isValid = (isGroupGrant ? Boolean(subjectId) : isUuid(subjectId)) && availableRoles.includes(draft.roleKey);

  useEffect(() => {
    if (!availableRoles.includes(draft.roleKey) && availableRoles.length > 0) {
      setDraft((current) => ({ ...current, roleKey: availableRoles[0] }));
    }
  }, [availableRoles, draft.roleKey]);

  const submit = () => {
    if (!canMutate || !isValid) {
      return;
    }

    void mutations.createGrant({
      subjectType,
      subjectId,
      roleKey: draft.roleKey,
      expiresAt: toApiDateTime(draft.expiresAt),
      reason: draft.reason.trim() || null,
    }).then((created) => {
      if (created) {
        setDraft((current) => ({
          ...current,
          subjectId: "",
          expiresAt: "",
          reason: "",
        }));
      }
    });
  };

  return (
    <section className="share-permissions-section">
      <div className="share-permissions-section-title">
        <h2>{isGroupGrant ? "Add Group Grant" : "Add User Grant"}</h2>
        <span>{availableRoles.length}</span>
      </div>
      <div className="share-permissions-form">
        <div className="share-permissions-form-grid">
          <div className="share-permissions-field-row">
            <label htmlFor={`${subjectType}-grant-subject`}>{isGroupGrant ? "Group" : "User ID"}</label>
            {isGroupGrant ? (
              <select
                disabled={!canMutate || groupStatus !== "ready" || groupOptions.length === 0}
                id={`${subjectType}-grant-subject`}
                onChange={(event) => setDraft((current) => ({ ...current, subjectId: event.target.value }))}
                value={draft.subjectId || groupOptions[0]?.id || ""}
              >
                {groupOptions.map((group) => (
                  <option key={group.id} value={group.id}>
                    {group.name}
                  </option>
                ))}
              </select>
            ) : (
              <input
                disabled={!canMutate}
                id={`${subjectType}-grant-subject`}
                onChange={(event) => setDraft((current) => ({ ...current, subjectId: event.target.value }))}
                placeholder="User UUID"
                type="text"
                value={draft.subjectId}
              />
            )}
          </div>
          <RoleSelect
            disabled={!canMutate}
            id={`${subjectType}-grant-role`}
            onChange={(roleKey) => setDraft((current) => ({ ...current, roleKey }))}
            roles={availableRoles}
            value={draft.roleKey}
          />
          <div className="share-permissions-field-row">
            <label htmlFor={`${subjectType}-grant-expiry`}>Expiry</label>
            <input
              disabled={!canMutate}
              id={`${subjectType}-grant-expiry`}
              onChange={(event) => setDraft((current) => ({ ...current, expiresAt: event.target.value }))}
              type="datetime-local"
              value={draft.expiresAt}
            />
          </div>
          <div className="share-permissions-field-row">
            <label htmlFor={`${subjectType}-grant-reason`}>Reason</label>
            <input
              disabled={!canMutate}
              id={`${subjectType}-grant-reason`}
              onChange={(event) => setDraft((current) => ({ ...current, reason: event.target.value }))}
              placeholder="Optional"
              type="text"
              value={draft.reason}
            />
          </div>
        </div>
        <div className="share-permissions-action-row">
          <button
            className="share-permissions-inline-action"
            disabled={!canMutate || !isValid || mutations.operation === "creating-grant"}
            onClick={submit}
            title={!canMutate ? statusLabel(status) : undefined}
            type="button"
          >
            <Plus className="h-3.5 w-3.5" />
            {mutations.operation === "creating-grant" ? "Creating" : `Create ${isGroupGrant ? "group" : "user"} grant`}
          </button>
          {mutations.error ? <span className="share-permissions-inline-status is-error">{mutations.error}</span> : null}
          {mutations.message ? <span className="share-permissions-inline-status">{mutations.message}</span> : null}
        </div>
      </div>
    </section>
  );
}

function ScopedGrantTable({
  grants,
  groupsById,
  mutations,
  permissions,
  status,
}: {
  grants: PermissionGrantDto[];
  groupsById?: Map<string, WorkspaceGroupDto>;
  mutations: ReturnType<typeof usePermissionMutations>;
  permissions: ResourcePermissionsResponse | null;
  status: PermissionApiStatus;
}) {
  const [editingGrantId, setEditingGrantId] = useState<string | null>(null);
  const [editDraft, setEditDraft] = useState<GrantDraft>({
    subjectId: "",
    roleKey: "viewer",
    expiresAt: "",
    reason: "",
  });
  const [confirmRevokeGrantId, setConfirmRevokeGrantId] = useState<string | null>(null);
  const availableRoles = getAvailableRoles(permissions);
  const canMutate = status === "ready" && Boolean(permissions) && mutations.canUse;

  if (!permissions) {
    return <div className="share-permissions-empty-state">{statusLabel(status)}</div>;
  }

  if (grants.length === 0) {
    return <div className="share-permissions-empty-state">No direct grants on this document</div>;
  }

  const startEdit = (grant: PermissionGrantDto) => {
    setEditingGrantId(grant.id);
    setConfirmRevokeGrantId(null);
    setEditDraft({
      subjectId: grant.subjectId,
      roleKey: grant.roleKey,
      expiresAt: toDateTimeLocalValue(grant.expiresAt),
      reason: grant.reason ?? "",
    });
  };

  const saveEdit = (grant: PermissionGrantDto) => {
    if (!canMutate || !availableRoles.includes(editDraft.roleKey)) {
      return;
    }

    void mutations.updateGrant(grant.id, {
      roleKey: editDraft.roleKey,
      expiresAt: toApiDateTime(editDraft.expiresAt),
      reason: editDraft.reason.trim() || null,
    }).then((updated) => {
      if (updated) {
        setEditingGrantId(null);
      }
    });
  };

  const revoke = (grant: PermissionGrantDto) => {
    if (confirmRevokeGrantId !== grant.id) {
      setConfirmRevokeGrantId(grant.id);
      setEditingGrantId(null);
      return;
    }

    void mutations.revokeGrant(grant.id, grant.reason).then((revoked) => {
      if (revoked) {
        setConfirmRevokeGrantId(null);
      }
    });
  };

  return (
    <div className="share-permissions-grants-list">
      {grants.map((grant) => {
        const group = grant.subjectType === "group" ? groupsById?.get(grant.subjectId) : undefined;
        const isEditing = editingGrantId === grant.id;
        const label = group?.name ?? grant.subjectId;
        const initials = grant.subjectType === "group" ? (group?.name ?? "G").slice(0, 1).toUpperCase() : "U";

        return (
          <article className={["share-permissions-grant-row", isEditing ? "is-editing" : ""].join(" ")} key={grant.id}>
            <span className="share-permissions-member-name">
              <Avatar initials={initials} tone={grant.subjectType === "group" ? "green" : "blue"} />
              <strong title={label}>{label}</strong>
            </span>
            {isEditing ? (
              <>
                <RoleSelect
                  disabled={!canMutate}
                  id={`edit-grant-role-${grant.id}`}
                  onChange={(roleKey) => setEditDraft((current) => ({ ...current, roleKey }))}
                  roles={availableRoles}
                  value={editDraft.roleKey}
                />
                <div className="share-permissions-field-row is-compact">
                  <label htmlFor={`edit-grant-expiry-${grant.id}`}>Expiry</label>
                  <input
                    disabled={!canMutate}
                    id={`edit-grant-expiry-${grant.id}`}
                    onChange={(event) => setEditDraft((current) => ({ ...current, expiresAt: event.target.value }))}
                    type="datetime-local"
                    value={editDraft.expiresAt}
                  />
                </div>
                <div className="share-permissions-row-actions">
                  <button
                    className="share-permissions-icon-button"
                    disabled={!canMutate || mutations.operation === grant.id}
                    onClick={() => saveEdit(grant)}
                    title="Save grant"
                    type="button"
                  >
                    <Save className="h-4 w-4" />
                  </button>
                  <button
                    className="share-permissions-icon-button"
                    onClick={() => setEditingGrantId(null)}
                    title="Cancel"
                    type="button"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              </>
            ) : (
              <>
                <span>{formatPermissionValue(grant.roleKey)}</span>
                <span>{grant.expiresAt ? formatDate(grant.expiresAt) : "No expiry"}</span>
                <div className="share-permissions-row-actions">
                  <button
                    className="share-permissions-icon-button"
                    disabled={!canMutate}
                    onClick={() => startEdit(grant)}
                    title="Edit grant"
                    type="button"
                  >
                    <Edit3 className="h-4 w-4" />
                  </button>
                  <button
                    className="share-permissions-icon-button is-danger"
                    disabled={!canMutate || mutations.operation === grant.id}
                    onClick={() => revoke(grant)}
                    title={confirmRevokeGrantId === grant.id ? "Confirm revoke" : "Revoke grant"}
                    type="button"
                  >
                    {confirmRevokeGrantId === grant.id ? "Confirm" : <Trash2 className="h-4 w-4" />}
                  </button>
                </div>
              </>
            )}
          </article>
        );
      })}
    </div>
  );
}

function ShareSettingsPanel({
  accessRequests,
  permissions,
  requestAccess,
  requestStatus,
  reviewError,
  reviewRequest,
  reviewingRequestId,
  emailInvites,
  shareLinks,
  status,
}: {
  accessRequests: AccessRequestDto[] | null;
  permissions: ResourcePermissionsResponse | null;
  requestAccess: ReturnType<typeof useRequestAccess>;
  requestStatus: PermissionApiStatus;
  reviewError: string | null;
  reviewRequest: (request: AccessRequestDto, decision: "approve" | "deny") => void;
  reviewingRequestId: string | null;
  emailInvites: ReturnType<typeof useEmailInvites>;
  shareLinks: ReturnType<typeof useShareLinks>;
  status: PermissionApiStatus;
}) {
  const pendingRequests = accessRequests?.filter((request) => request.status === "pending") ?? [];

  return (
    <aside className="share-permissions-settings editor-scrollbar overflow-y-auto">
      <div className="share-permissions-settings-inner">
        <Panel title="Share links" icon={<Link2 className="h-5 w-5" />}>
          {shareLinks.created ? (
            <div className="share-permissions-generated-link">
              <span>{shareLinks.created.url}</span>
              <button
                onClick={shareLinks.copyGeneratedUrl}
                title="Copy generated link"
                type="button"
              >
                <Copy className="h-4 w-4" />
                URL
              </button>
              <div className="share-permissions-generated-actions">
                <button
                  onClick={shareLinks.copyGeneratedToken}
                  title="Copy one-time token"
                  type="button"
                >
                  Copy token
                </button>
                <button onClick={shareLinks.dismissCreated} title="Dismiss generated token" type="button">
                  Dismiss
                </button>
              </div>
              <small>
                {formatPermissionValue(shareLinks.created.link.audience)} token is returned once and is not shown in
                active link lists.
              </small>
            </div>
          ) : null}
          {!shareLinks.canUse && shareLinks.capabilityReason ? (
            <div className="share-permissions-inline-status is-muted">{shareLinks.capabilityReason}</div>
          ) : null}
          <div className="share-permissions-link-options">
            <button
              className={shareLinks.role === "viewer" ? "is-selected" : ""}
              disabled={!shareLinks.canUse}
              onClick={() => shareLinks.setRole("viewer")}
              type="button"
            >
              <Eye className="h-4 w-4" />
              Viewer
            </button>
            <button
              className={shareLinks.role === "commenter" ? "is-selected" : ""}
              disabled={!shareLinks.canUse}
              onClick={() => shareLinks.setRole("commenter")}
              type="button"
            >
              <UserRound className="h-4 w-4" />
              Commenter
            </button>
          </div>
          <button
            className="share-permissions-inline-action"
            disabled={!shareLinks.canUse || shareLinks.operation === "creating"}
            onClick={shareLinks.createInternalLink}
            title={shareLinks.capabilityReason ?? "Create a workspace-member share link"}
            type="button"
          >
            {shareLinks.operation === "creating" ? "Creating" : "Create internal link"}
          </button>
          <div className="share-permissions-field-row">
            <label htmlFor="external-share-email">External authenticated email</label>
            <input
              disabled={!shareLinks.canUse}
              id="external-share-email"
              onChange={(event) => shareLinks.setExternalEmail(event.target.value)}
              placeholder="person@example.com"
              type="email"
              value={shareLinks.externalEmail}
            />
          </div>
          <button
            className="share-permissions-inline-action"
            disabled={!shareLinks.canUse || !shareLinks.externalEmail.trim() || shareLinks.operation === "creating-external"}
            onClick={shareLinks.createExternalLink}
            type="button"
          >
            {shareLinks.operation === "creating-external" ? "Creating" : "Create external link"}
          </button>
          <div className="share-permissions-public-link-form">
            <div className="share-permissions-public-link-heading">
              <strong>Public document link</strong>
              <small>Viewer only / dedicated share-link API</small>
            </div>
            <div className="share-permissions-public-link-grid">
              <div className="share-permissions-field-row is-compact">
                <label htmlFor="public-share-expiry">Expiry</label>
                <input
                  disabled={!shareLinks.canUse}
                  id="public-share-expiry"
                  min={shareLinks.minimumPublicExpiry}
                  onChange={(event) => shareLinks.setPublicExpiresAt(event.target.value)}
                  type="datetime-local"
                  value={shareLinks.publicExpiresAt}
                />
              </div>
              <div className="share-permissions-field-row is-compact">
                <label htmlFor="public-share-password">Password</label>
                <input
                  autoComplete="new-password"
                  disabled={!shareLinks.canUse}
                  id="public-share-password"
                  onChange={(event) => shareLinks.setPublicPassword(event.target.value)}
                  placeholder="Optional"
                  type="password"
                  value={shareLinks.publicPassword}
                />
              </div>
              <button
                className="share-permissions-inline-action"
                disabled={
                  !shareLinks.canUse ||
                  !shareLinks.canCreatePublicLink ||
                  shareLinks.operation === "creating-public"
                }
                onClick={shareLinks.createPublicLink}
                title={!shareLinks.canCreatePublicLink ? "Future expiry is required" : "Create public document link"}
                type="button"
              >
                <LockKeyhole className="h-4 w-4" />
                {shareLinks.operation === "creating-public" ? "Creating" : "Create public link"}
              </button>
            </div>
          </div>
          {!shareLinks.links ? (
            <div className="share-permissions-empty-state">{statusLabel(shareLinks.status)}</div>
          ) : shareLinks.links.length === 0 ? (
            <div className="share-permissions-empty-state">No active links</div>
          ) : (
            <div className="share-permissions-share-link-list">
              {shareLinks.links.map((link) => (
                <div className="share-permissions-share-link-row" key={link.id}>
                  <span>
                    <strong>
                      {formatPermissionValue(link.roleKey)} / {link.audience}
                    </strong>
                    <small>
                      {formatShareLinkMetadata(link)}
                    </small>
                  </span>
                  <button
                    className={shareLinks.revokeCandidateId === link.id ? "is-danger" : ""}
                    disabled={shareLinks.operation === link.id}
                    onClick={() => {
                      if (shareLinks.revokeCandidateId === link.id) {
                        shareLinks.revokeLink(link.id);
                        return;
                      }

                      shareLinks.requestRevokeLink(link.id);
                    }}
                    title={
                      shareLinks.revokeCandidateId === link.id
                        ? "Click again to revoke this share link"
                        : "Prepare to revoke this share link"
                    }
                    type="button"
                  >
                    {shareLinks.operation === link.id
                      ? "Revoking"
                      : shareLinks.revokeCandidateId === link.id
                        ? "Confirm"
                        : "Revoke"}
                  </button>
                </div>
              ))}
            </div>
          )}
          {shareLinks.error ? <div className="share-permissions-inline-status is-error">{shareLinks.error}</div> : null}
          {shareLinks.message ? <div className="share-permissions-inline-status">{shareLinks.message}</div> : null}
        </Panel>

        <Panel title="Email invites" icon={<UserPlus className="h-5 w-5" />}>
          {emailInvites.created ? (
            <div className="share-permissions-generated-link">
              <span>{emailInvites.created.url}</span>
              <button
                onClick={() => void navigator.clipboard?.writeText(emailInvites.created?.url ?? "")}
                title="Copy generated invite"
                type="button"
              >
                <Copy className="h-4 w-4" />
                URL
              </button>
              <div className="share-permissions-generated-actions">
                <button
                  onClick={() => void navigator.clipboard?.writeText(emailInvites.created?.token ?? "")}
                  title="Copy one-time invite token"
                  type="button"
                >
                  Copy token
                </button>
                <button onClick={emailInvites.dismissCreated} title="Dismiss generated invite" type="button">
                  Dismiss
                </button>
              </div>
              <small>Invite token is returned once. Copy it now; it is not shown in invite lists.</small>
            </div>
          ) : null}
          <div className="share-permissions-field-row">
            <label htmlFor="email-invite-address">Recipient email</label>
            <input
              id="email-invite-address"
              onChange={(event) => emailInvites.setEmail(event.target.value)}
              placeholder="person@example.com"
              type="email"
              value={emailInvites.email}
            />
          </div>
          <div className="share-permissions-link-options">
            <button
              className={emailInvites.role === "viewer" ? "is-selected" : ""}
              onClick={() => emailInvites.setRole("viewer")}
              type="button"
            >
              <Eye className="h-4 w-4" />
              Viewer
            </button>
            <button
              className={emailInvites.role === "commenter" ? "is-selected" : ""}
              onClick={() => emailInvites.setRole("commenter")}
              type="button"
            >
              <UserRound className="h-4 w-4" />
              Commenter
            </button>
          </div>
          <button
            className="share-permissions-inline-action"
            disabled={!emailInvites.canUse || !emailInvites.email.trim() || emailInvites.operation === "creating"}
            onClick={emailInvites.createInvite}
            type="button"
          >
            {emailInvites.operation === "creating" ? "Creating" : "Create invite"}
          </button>
          {!emailInvites.invites ? (
            <div className="share-permissions-empty-state">{statusLabel(emailInvites.status)}</div>
          ) : emailInvites.invites.length === 0 ? (
            <div className="share-permissions-empty-state">No email invites</div>
          ) : (
            <div className="share-permissions-share-link-list">
              {emailInvites.invites.map((invite) => (
                <div className="share-permissions-share-link-row" key={invite.id}>
                  <span>
                    <strong>
                      {invite.email} / {invite.status}
                    </strong>
                    <small>
                      {formatPermissionValue(invite.roleKey)} / expires {formatDate(invite.expiresAt)}
                      {invite.deliveryStatus ? ` / delivery ${invite.deliveryStatus}` : ""}
                    </small>
                  </span>
                  <button
                    disabled={emailInvites.operation === invite.id || invite.status === "revoked" || invite.status === "expired"}
                    onClick={() => emailInvites.revokeInvite(invite.id)}
                    type="button"
                  >
                    Revoke
                  </button>
                </div>
              ))}
            </div>
          )}
          {emailInvites.error ? <div className="share-permissions-inline-status is-error">{emailInvites.error}</div> : null}
        </Panel>

        <Panel title="Inherited workspace access" icon={<UsersRound className="h-5 w-5" />}>
          <div className="share-permissions-toggle-row">
            <p>{permissions ? `Policy ${permissions.policy.inheritanceMode}; source ${permissions.effectiveAccess.source}.` : statusLabel(status)}</p>
            <span aria-hidden="true" />
          </div>
          <a className="share-permissions-text-link" href="#workspace-members" title="Manage workspace members">
            Manage workspace members
          </a>
        </Panel>

        <Panel title="Access Requests" icon={<UserPlus className="h-5 w-5" />}>
          {requestAccess.message ? (
            <div className={["share-permissions-inline-status", requestAccess.status === "error" ? "is-error" : ""].join(" ")}>
              {requestAccess.message}
            </div>
          ) : null}
          {!accessRequests ? (
            <div className="share-permissions-empty-state">
              {statusLabel(requestStatus)}
              <button
                className="share-permissions-inline-action"
                disabled={!requestAccess.canRequest || requestAccess.status === "submitting" || requestAccess.status === "pending"}
                onClick={requestAccess.request}
                type="button"
              >
                {requestAccessButtonLabel(requestAccess.status)}
              </button>
            </div>
          ) : pendingRequests.length === 0 ? (
            <div className="share-permissions-empty-state">No pending access requests</div>
          ) : (
            <div className="share-permissions-recent-list">
              {pendingRequests.map((request) => (
                <div className="share-permissions-recent-row" key={request.id}>
                  <Avatar initials="U" tone="blue" />
                  <span className="min-w-0 flex-1 truncate">
                    <strong>{request.subjectId}</strong> requested {formatPermissionValue(request.requestedRole)}
                  </span>
                  <div className="share-permissions-review-actions">
                    <button
                      disabled={reviewingRequestId === request.id}
                      onClick={() => reviewRequest(request, "approve")}
                      type="button"
                    >
                      Approve
                    </button>
                    <button
                      disabled={reviewingRequestId === request.id}
                      onClick={() => reviewRequest(request, "deny")}
                      type="button"
                    >
                      Deny
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
          {reviewError ? <div className="share-permissions-inline-status is-error">{reviewError}</div> : null}
        </Panel>

        <Panel action="View all" title="Recent Access" icon={<CalendarDays className="h-5 w-5" />}>
          <div className="share-permissions-recent-list">
            {recentAccessEvents.map((event) => (
              <RecentAccessRow event={event} key={event.id} />
            ))}
          </div>
        </Panel>
      </div>
    </aside>
  );
}

function Panel({
  action,
  children,
  icon,
  title,
}: {
  action?: string;
  children: ReactNode;
  icon: ReactNode;
  title: string;
}) {
  return (
    <section className="share-permissions-panel">
      <header>
        <div className="min-w-0">
          {icon}
          <h2>{title}</h2>
        </div>
        {action ? (
          <button className="share-permissions-text-link" title={action} type="button">
            {action}
          </button>
        ) : (
          <ShieldCheck className="h-4 w-4 text-[var(--ns-slate-500)]" />
        )}
      </header>
      {children}
    </section>
  );
}

function RecentAccessRow({ event }: { event: RecentAccessEvent }) {
  return (
    <div className="share-permissions-recent-row">
      <Avatar initials={event.initials} tone={event.avatarTone} />
      <span className="min-w-0 flex-1 truncate">
        <strong>{event.actor}</strong> {event.action}
      </span>
      <span>{event.happenedAt}</span>
    </div>
  );
}

function Avatar({ initials, tone }: { initials: string; tone: WorkspaceAccessMember["avatarTone"] }) {
  return <span className={["share-permissions-avatar", `is-${tone}`].join(" ")}>{initials}</span>;
}

function StatusBadge({ label }: { label: ShareDocumentContext["status"] }) {
  return (
    <span className={["share-permissions-status", label === "Draft" ? "is-draft" : ""].join(" ")}>
      <CheckCircle2 className="h-3.5 w-3.5" />
      {label}
    </span>
  );
}

function formatRole(role: WorkspaceRole) {
  return role.charAt(0).toUpperCase() + role.slice(1);
}

function getAccessClass(access: EffectiveDocumentAccess) {
  if (access === "Can manage") {
    return "is-manage";
  }

  if (access === "Can edit") {
    return "is-edit";
  }

  return "is-view";
}

function getRoleIcon(role: WorkspaceRole) {
  if (role === "owner" || role === "admin") {
    return <ShieldCheck className="h-4 w-4" />;
  }

  if (role === "editor") {
    return <Tag className="h-4 w-4" />;
  }

  return <Eye className="h-4 w-4" />;
}

function usePermissionMutations(documentId: string | null, reloadPermissions: () => AbortController | undefined) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [operation, setOperation] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const canUse = Boolean(apiBaseUrl && documentId);

  const runMutation = async <T,>(
    operationKey: string,
    url: string,
    options: RequestInit,
    successMessage: string,
    parseJson = true,
  ) => {
    if (!apiBaseUrl || !documentId || operation) {
      return null;
    }

    setOperation(operationKey);
    setError(null);
    setMessage(null);

    try {
      const response = await fetch(url, options);
      if (response.status === 401 || response.status === 403) {
        setError("You do not have permission to change this document.");
        return null;
      }

      if (!response.ok) {
        throw new Error(`Permission mutation returned ${response.status}`);
      }

      const body = parseJson ? ((await response.json()) as T) : (true as T);
      setMessage(successMessage);
      reloadPermissions();
      return body;
    } catch {
      setError("Could not save permission changes.");
      return null;
    } finally {
      setOperation(null);
    }
  };

  const createGrant = (request: {
    subjectType: PermissionGrantSubjectType;
    subjectId: string;
    roleKey: string;
    expiresAt: string | null;
    reason: string | null;
  }) => {
    if (!apiBaseUrl || !documentId) {
      return Promise.resolve(null);
    }

    return runMutation<PermissionGrantDto>(
      "creating-grant",
      `${apiBaseUrl}/permissions/resources/document/${documentId}/grants`,
      {
        body: JSON.stringify(request),
        headers: createPermissionHeaders("application/json"),
        method: "POST",
      },
      "Grant created.",
    );
  };

  const updateGrant = (
    grantId: string,
    request: {
      roleKey: string;
      expiresAt: string | null;
      reason: string | null;
    },
  ) => {
    if (!apiBaseUrl || !documentId) {
      return Promise.resolve(null);
    }

    return runMutation<PermissionGrantDto>(
      grantId,
      `${apiBaseUrl}/permissions/resources/document/${documentId}/grants/${grantId}`,
      {
        body: JSON.stringify(request),
        headers: createPermissionHeaders("application/json"),
        method: "PATCH",
      },
      "Grant updated.",
    );
  };

  const revokeGrant = (grantId: string, reason: string | null = null) => {
    if (!apiBaseUrl || !documentId) {
      return Promise.resolve(null);
    }

    return runMutation<boolean>(
      grantId,
      `${apiBaseUrl}/permissions/resources/document/${documentId}/grants/${grantId}`,
      {
        body: JSON.stringify({ reason }),
        headers: createPermissionHeaders("application/json"),
        method: "DELETE",
      },
      "Grant revoked.",
      false,
    );
  };

  const updatePolicy = (request: {
    inheritanceMode: PermissionInheritanceMode;
    linkMode: PermissionLinkMode;
    defaultLinkRole: ShareLinkRole | null;
  }) => {
    if (!apiBaseUrl || !documentId) {
      return Promise.resolve(null);
    }

    return runMutation<ResourcePermissionsResponse>(
      "updating-policy",
      `${apiBaseUrl}/permissions/resources/document/${documentId}/policy`,
      {
        body: JSON.stringify(request),
        headers: createPermissionHeaders("application/json"),
        method: "PATCH",
      },
      "Access settings saved.",
    );
  };

  return {
    canUse,
    createGrant,
    error,
    message,
    operation,
    revokeGrant,
    updateGrant,
    updatePolicy,
  };
}

function useShareLinks(documentId: string | null, reloadPermissions?: () => AbortController | undefined) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [links, setLinks] = useState<ShareLinkDto[] | null>(null);
  const [status, setStatus] = useState<PermissionApiStatus>(() =>
    apiBaseUrl && documentId ? "loading" : "unconfigured",
  );
  const [role, setRole] = useState<ShareLinkRole>("viewer");
  const [externalEmail, setExternalEmail] = useState("");
  const [publicExpiresAt, setPublicExpiresAt] = useState("");
  const [publicPassword, setPublicPassword] = useState("");
  const [created, setCreated] = useState<CreateShareLinkResponse | null>(null);
  const [operation, setOperation] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [revokeCandidateId, setRevokeCandidateId] = useState<string | null>(null);
  const isConfigured = Boolean(apiBaseUrl && documentId);
  const capability = getShareLinkCapability({
    apiConfigured: Boolean(apiBaseUrl),
    documentId,
    operation,
    status,
  });
  const canUse = isConfigured && capability.canUse;
  const publicExpiryIso = toApiDateTime(publicExpiresAt);
  const canCreatePublicLink = Boolean(publicExpiryIso && new Date(publicExpiryIso).getTime() > Date.now());
  const minimumPublicExpiry = toDateTimeLocalValue(new Date(Date.now() + 60_000).toISOString());

  const loadLinks = () => {
    if (!apiBaseUrl || !documentId) {
      setLinks(null);
      setStatus("unconfigured");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getDocumentShareLinks(documentId, controller.signal)
      .then((body) => {
        setLinks(body.links);
        setStatus("ready");
      })
      .catch((errorValue: unknown) => {
        if (errorValue instanceof DOMException && errorValue.name === "AbortError") {
          return;
        }

        setLinks(null);
        setStatus(isForbiddenError(errorValue) ? "forbidden" : "error");
        setError(toSharePermissionMutationError(errorValue, "Could not load share links."));
      });

    return controller;
  };

  useEffect(() => {
    const controller = loadLinks();
    return () => controller?.abort();
  }, [apiBaseUrl, documentId]);

  const createLink = (
    audience: ShareLinkAudience,
    options: { subjectEmail?: string; expiresAt?: string | null; password?: string | null } = {},
  ) => {
    if (!apiBaseUrl || !documentId || !canUse || operation) {
      return;
    }

    if (audience === "public" && (!options.expiresAt || new Date(options.expiresAt).getTime() <= Date.now())) {
      setError("Public links require a future expiry.");
      return;
    }

    setOperation(audience === "public" ? "creating-public" : audience === "external" ? "creating-external" : "creating");
    setError(null);
    setMessage(null);
    void createDocumentShareLink(
      documentId,
      createShareLinkRequest({
        audience,
        expiresAt: options.expiresAt ?? (audience === "external" ? defaultInviteExpiry() : null),
        password: options.password,
        roleKey: role,
        subjectEmail: audience === "external" ? options.subjectEmail : null,
      }),
    )
      .then((body) => {
        setCreated({
          ...body,
          url: toAbsoluteShareUrl(body.url, apiBaseUrl),
        });
        setMessage("Share link created. Copy the URL or token now.");
        setRevokeCandidateId(null);
        if (audience === "external") {
          setExternalEmail("");
        }
        if (audience === "public") {
          setPublicExpiresAt("");
          setPublicPassword("");
        }
        loadLinks();
        reloadPermissions?.();
      })
      .catch((errorValue: unknown) => {
        setError(toSharePermissionMutationError(errorValue, "Could not create link."));
      })
      .finally(() => {
        setOperation(null);
      });
  };

  const createInternalLink = () => {
    if (!apiBaseUrl || !documentId || !canUse || operation) {
      return;
    }

    setOperation("creating");
    setError(null);
    setMessage(null);
    void createDocumentShareLink(documentId, createWorkspaceShareLinkRequest(role))
      .then((body) => {
        setCreated({
          ...body,
          url: toAbsoluteShareUrl(body.url, apiBaseUrl),
        });
        setMessage("Internal share link created. Copy the URL or token now.");
        setRevokeCandidateId(null);
        loadLinks();
        reloadPermissions?.();
      })
      .catch((errorValue: unknown) => {
        setError(toSharePermissionMutationError(errorValue, "Could not create internal link."));
      })
      .finally(() => {
        setOperation(null);
      });
  };
  const createExternalLink = () => createLink("external", { subjectEmail: externalEmail.trim() });
  const createPublicLink = () =>
    createLink("public", {
      expiresAt: publicExpiryIso,
      password: publicPassword.trim() || null,
    });

  const requestRevokeLink = (linkId: string) => {
    if (!apiBaseUrl || !canUse || operation) {
      return;
    }

    setRevokeCandidateId((current) => (current === linkId ? null : linkId));
    setError(null);
    setMessage(null);
  };

  const revokeLink = (linkId: string) => {
    if (!apiBaseUrl || !canUse || operation) {
      return;
    }

    setOperation(linkId);
    setError(null);
    setMessage(null);
    void revokeShareLink(linkId)
      .then(() => {
        setLinks((current) => current?.filter((link) => link.id !== linkId) ?? current);
        setRevokeCandidateId(null);
        setMessage("Share link revoked.");
        reloadPermissions?.();
      })
      .catch((errorValue: unknown) => {
        setError(toSharePermissionMutationError(errorValue, "Could not revoke link."));
      })
      .finally(() => {
        setOperation(null);
      });
  };

  const copyGeneratedUrl = () => copyShareValue(created?.url ?? "", setMessage, setError, "URL copied.");
  const copyGeneratedToken = () => copyShareValue(created?.token ?? "", setMessage, setError, "Token copied.");

  return {
    canUse,
    canCreatePublicLink,
    capabilityReason: capability.reason,
    copyGeneratedToken,
    copyGeneratedUrl,
    createExternalLink,
    createInternalLink,
    createPublicLink,
    created,
    dismissCreated: () => {
      setCreated(null);
      setMessage(null);
    },
    error,
    externalEmail,
    links,
    message,
    minimumPublicExpiry,
    operation,
    publicExpiresAt,
    publicPassword,
    requestRevokeLink,
    revokeLink,
    revokeCandidateId,
    role,
    setExternalEmail,
    setPublicExpiresAt,
    setPublicPassword,
    setRole,
    status,
  };
}

function useEmailInvites(documentId: string | null, reloadPermissions?: () => AbortController | undefined) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [invites, setInvites] = useState<EmailInviteDto[] | null>(null);
  const [status, setStatus] = useState<PermissionApiStatus>(() =>
    apiBaseUrl && documentId ? "loading" : "unconfigured",
  );
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<ShareLinkRole>("viewer");
  const [created, setCreated] = useState<CreateEmailInviteResponse | null>(null);
  const [operation, setOperation] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const canUse = Boolean(apiBaseUrl && documentId);

  const loadInvites = () => {
    if (!apiBaseUrl || !documentId) {
      setInvites(null);
      setStatus("unconfigured");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    void fetch(`${apiBaseUrl}/permissions/resources/document/${documentId}/email-invites`, {
      headers: createPermissionHeaders(),
      signal: controller.signal,
    })
      .then(async (response) => {
        if (response.status === 401 || response.status === 403) {
          setStatus("forbidden");
          return;
        }

        if (!response.ok) {
          throw new Error(`Email invite API returned ${response.status}`);
        }

        const body = (await response.json()) as EmailInvitesResponse;
        setInvites(body.invites);
        setStatus("ready");
      })
      .catch((errorValue: unknown) => {
        if (errorValue instanceof DOMException && errorValue.name === "AbortError") {
          return;
        }

        setInvites(null);
        setStatus("error");
      });

    return controller;
  };

  useEffect(() => {
    const controller = loadInvites();
    return () => controller?.abort();
  }, [apiBaseUrl, documentId]);

  const createInvite = () => {
    if (!apiBaseUrl || !documentId || operation || !email.trim()) {
      return;
    }

    setOperation("creating");
    setError(null);
    void fetch(`${apiBaseUrl}/permissions/resources/document/${documentId}/email-invites`, {
      body: JSON.stringify({
        email: email.trim(),
        roleKey: role,
        expiresAt: defaultInviteExpiry(),
      }),
      headers: createPermissionHeaders("application/json"),
      method: "POST",
    })
      .then(async (response) => {
        if (response.status === 409) {
          setError("An invite is already pending for this email.");
          return;
        }

        if (!response.ok) {
          throw new Error(`Create invite API returned ${response.status}`);
        }

        const body = (await response.json()) as CreateEmailInviteResponse;
        setCreated(body);
        setEmail("");
        loadInvites();
        reloadPermissions?.();
      })
      .catch(() => {
        setError("Could not create invite.");
      })
      .finally(() => {
        setOperation(null);
      });
  };

  const revokeInvite = (inviteId: string) => {
    if (!apiBaseUrl || operation) {
      return;
    }

    setOperation(inviteId);
    setError(null);
    void fetch(`${apiBaseUrl}/permissions/email-invites/${inviteId}`, {
      headers: createPermissionHeaders(),
      method: "DELETE",
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Revoke invite API returned ${response.status}`);
        }

        setInvites((current) =>
          current?.map((invite) =>
            invite.id === inviteId ? { ...invite, status: "revoked", revokedAt: new Date().toISOString() } : invite,
          ) ?? current,
        );
        reloadPermissions?.();
      })
      .catch(() => {
        setError("Could not revoke invite.");
      })
      .finally(() => {
        setOperation(null);
      });
  };

  return {
    canUse,
    createInvite,
    created,
    dismissCreated: () => setCreated(null),
    email,
    error,
    invites,
    operation,
    revokeInvite,
    role,
    setEmail,
    setRole,
    status,
  };
}

function useResourceAccessRequests(documentId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [requests, setRequests] = useState<AccessRequestDto[] | null>(null);
  const [status, setStatus] = useState<PermissionApiStatus>(() =>
    apiBaseUrl && documentId ? "loading" : "unconfigured",
  );
  const [reviewingRequestId, setReviewingRequestId] = useState<string | null>(null);
  const [reviewError, setReviewError] = useState<string | null>(null);

  const loadRequests = () => {
    if (!apiBaseUrl || !documentId) {
      setRequests(null);
      setStatus("unconfigured");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");

    void fetch(`${apiBaseUrl}/permissions/resources/document/${documentId}/access-requests`, {
      headers: createPermissionHeaders(),
      signal: controller.signal,
    })
      .then(async (response) => {
        if (response.status === 401 || response.status === 403) {
          setStatus("forbidden");
          return;
        }

        if (!response.ok) {
          throw new Error(`Access request API returned ${response.status}`);
        }

        const body = (await response.json()) as AccessRequestsResponse;
        setRequests(body.requests);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setRequests(null);
        setStatus("error");
      });

    return controller;
  };

  useEffect(() => {
    const controller = loadRequests();
    return () => controller?.abort();
  }, [apiBaseUrl, documentId]);

  const reviewRequest = (request: AccessRequestDto, decision: "approve" | "deny") => {
    if (!apiBaseUrl) {
      return;
    }

    setReviewingRequestId(request.id);
    setReviewError(null);
    void fetch(`${apiBaseUrl}/permissions/access-requests/${request.id}/review`, {
      body: JSON.stringify({
        decision,
        roleKey: decision === "approve" ? request.requestedRole : null,
        reason: null,
      }),
      headers: createPermissionHeaders("application/json"),
      method: "POST",
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`Review API returned ${response.status}`);
        }

        await response.json();
        loadRequests();
      })
      .catch(() => {
        setReviewError("Could not review this request.");
      })
      .finally(() => {
        setReviewingRequestId(null);
      });
  };

  return { requests, reviewError, reviewRequest, reviewingRequestId, status };
}

function useRequestAccess(documentId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [status, setStatus] = useState<RequestAccessStatus>("idle");
  const [message, setMessage] = useState<string | null>(null);
  const canRequest = Boolean(apiBaseUrl && documentId);

  const request = () => {
    if (!apiBaseUrl || !documentId || status === "submitting" || status === "pending") {
      return;
    }

    setStatus("submitting");
    setMessage(null);
    void fetch(`${apiBaseUrl}/permissions/access-requests`, {
      body: JSON.stringify({
        resourceType: "document",
        resourceId: documentId,
        requestedRole: "viewer",
        reason: null,
      }),
      headers: createPermissionHeaders("application/json"),
      method: "POST",
    })
      .then(async (response) => {
        if (response.status === 409) {
          setStatus("pending");
          setMessage("Access request is already pending.");
          return;
        }

        if (!response.ok) {
          throw new Error(`Request access API returned ${response.status}`);
        }

        await response.json();
        setStatus("pending");
        setMessage("Access request sent.");
      })
      .catch(() => {
        setStatus("error");
        setMessage("Could not request access.");
      });
  };

  return { canRequest, message, request, status };
}

function useResourcePermissions(documentId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [permissions, setPermissions] = useState<ResourcePermissionsResponse | null>(null);
  const [status, setStatus] = useState<PermissionApiStatus>(() =>
    apiBaseUrl && documentId ? "loading" : "unconfigured",
  );

  const loadPermissions = () => {
    if (!apiBaseUrl || !documentId) {
      setPermissions(null);
      setStatus("unconfigured");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");

    void fetch(`${apiBaseUrl}/permissions/resources/document/${documentId}`, {
      headers: createPermissionHeaders(),
      signal: controller.signal,
    })
      .then(async (response) => {
        if (response.status === 401 || response.status === 403) {
          setStatus("forbidden");
          return;
        }

        if (!response.ok) {
          throw new Error(`Permission API returned ${response.status}`);
        }

        const body = (await response.json()) as ResourcePermissionsResponse;
        setPermissions(body);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setPermissions(null);
        setStatus("error");
      });

    return controller;
  };

  useEffect(() => {
    const controller = loadPermissions();
    return () => controller?.abort();
  }, [apiBaseUrl, documentId]);

  return { permissions, reload: loadPermissions, status };
}

function useWorkspaceGroups(workspaceId: string | null) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [groups, setGroups] = useState<WorkspaceGroupDto[] | null>(null);
  const [status, setStatus] = useState<PermissionApiStatus>(() =>
    apiBaseUrl && workspaceId ? "loading" : "unconfigured",
  );

  useEffect(() => {
    if (!apiBaseUrl || !workspaceId) {
      setGroups(null);
      setStatus("unconfigured");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");

    void fetch(`${apiBaseUrl}/workspaces/${workspaceId}/groups`, {
      headers: createPermissionHeaders(),
      signal: controller.signal,
    })
      .then(async (response) => {
        if (response.status === 401 || response.status === 403) {
          setStatus("forbidden");
          return;
        }

        if (!response.ok) {
          throw new Error(`Groups API returned ${response.status}`);
        }

        const body = (await response.json()) as WorkspaceGroupsResponse;
        setGroups(body.groups);
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setGroups(null);
        setStatus("error");
      });

    return () => controller.abort();
  }, [apiBaseUrl, workspaceId]);

  return { groups, status };
}

function getAvailableRoles(permissions: ResourcePermissionsResponse | null) {
  return permissions?.availableRoles.length ? permissions.availableRoles : ["viewer"];
}

function normalizeInheritanceMode(value: string): PermissionInheritanceMode {
  return value === "restricted" ? "restricted" : "inherit";
}

function normalizeLinkMode(value: string | null | undefined): PermissionLinkMode {
  if (value === "internal" || value === "external") {
    return value;
  }

  return "disabled";
}

function normalizeShareLinkRole(value: string | null | undefined): ShareLinkRole {
  return value === "commenter" ? "commenter" : "viewer";
}

function toApiDateTime(value: string) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function toDateTimeLocalValue(value: string | null) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

function defaultInviteExpiry() {
  return new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
}

function getConfiguredPermissionDocumentTarget(): {
  documentId: string | null;
  source: "configured" | "hash" | "query" | null;
} {
  const hashDocumentId = typeof window === "undefined" ? null : getShareDocumentIdFromHash(window.location.hash);
  if (hashDocumentId) {
    return { documentId: hashDocumentId, source: "hash" };
  }

  const params = typeof window === "undefined" ? null : new URLSearchParams(window.location.search);
  const queryDocumentId = params?.get("documentId");
  if (queryDocumentId && isUuid(queryDocumentId)) {
    return { documentId: queryDocumentId, source: "query" };
  }

  const env = (import.meta as ImportMeta & { env?: Record<string, string | undefined> }).env;
  const envDocumentId = env?.VITE_NORTHSTAR_SHARE_DOCUMENT_ID;
  return envDocumentId && isUuid(envDocumentId)
    ? { documentId: envDocumentId, source: "configured" }
    : { documentId: null, source: null };
}

function getConfiguredPermissionWorkspaceId() {
  return getConfiguredWorkspaceId();
}

function createPermissionHeaders(contentType?: string) {
  return createApiHeaders(contentType);
}

function createShareDocumentContext(
  bootstrap: BootstrapResponse | null,
  resolution: ShareTargetResolution,
  status: PermissionApiStatus,
): ShareDocumentContext {
  const document = bootstrap?.documents.find((candidate) => candidate.id === resolution.documentId);
  if (document) {
    const folder = bootstrap?.folders.find((candidate) => candidate.id === document.folderId);
    return {
      location: [bootstrap?.workspace.name, folder?.title].filter(Boolean).join(" / ") || "Current workspace",
      owner: {
        initials: "NS",
        name: "Workspace permissions",
      },
      readers: "Live permission model",
      status: document.status?.toLowerCase() === "published" ? "Published" : "Draft",
      tags: document.tags,
      title: document.title,
      updatedAt: formatDate(document.updatedAt),
      version: "Live",
    };
  }

  if (resolution.documentId) {
    return {
      location: "Document resolved by id",
      owner: {
        initials: "NS",
        name: "Workspace permissions",
      },
      readers: "Live permission model",
      status: "Draft",
      tags: [],
      title: `Document ${resolution.documentId.slice(0, 8)}`,
      updatedAt: statusLabel(status),
      version: "Live",
    };
  }

  return {
    location: resolution.reason ?? statusLabel(status),
    owner: {
      initials: "NS",
      name: "No document selected",
    },
    readers: "Not connected",
    status: "Draft",
    tags: [],
    title: "Share target unavailable",
    updatedAt: statusLabel(status),
    version: "None",
  };
}

function isForbiddenError(error: unknown) {
  return typeof error === "object" &&
    error !== null &&
    "status" in error &&
    ((error as { status?: number }).status === 401 || (error as { status?: number }).status === 403);
}

function statusLabel(status: PermissionApiStatus) {
  if (status === "loading") {
    return "Loading";
  }

  if (status === "forbidden") {
    return "Forbidden";
  }

  if (status === "error") {
    return "Unavailable";
  }

  return "Not connected";
}

function requestAccessButtonLabel(status: RequestAccessStatus) {
  if (status === "submitting") {
    return "Requesting";
  }

  if (status === "pending") {
    return "Pending";
  }

  return "Request Access";
}

function formatPermissionValue(value: string) {
  return value
    .split("_")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function formatShareLinkMetadata(link: ShareLinkDto) {
  const parts = [
    link.subjectEmail,
    link.expiresAt ? `expires ${formatDate(link.expiresAt)}` : "no expiry",
    link.hasPassword ? "password required" : null,
    link.revokedAt ? "revoked" : null,
  ].filter(Boolean);

  return parts.join(" / ");
}

function copyShareValue(
  value: string,
  setMessage: (message: string | null) => void,
  setError: (message: string | null) => void,
  successMessage: string,
) {
  if (!value.trim()) {
    setError("Nothing to copy.");
    setMessage(null);
    return;
  }

  if (!navigator.clipboard?.writeText) {
    setError("Clipboard is unavailable. Select and copy the value manually.");
    setMessage(null);
    return;
  }

  navigator.clipboard.writeText(value)
    .then(() => {
      setMessage(successMessage);
      setError(null);
    })
    .catch(() => {
      setError("Clipboard was blocked. Select and copy the value manually.");
      setMessage(null);
    });
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(value));
}
