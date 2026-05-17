import {
  Copy,
  Eye,
  Globe2,
  Link2,
  Loader2,
  LockKeyhole,
  Pause,
  Play,
  ShieldCheck,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  copyShareLinkManagementUrl,
  createDocumentEmailInvite,
  createDocumentPermissionGrant,
  createDocumentShareLink,
  getDocumentEmailInvites,
  getDocumentResourcePermissions,
  getDocumentShareLinks,
  getResourcePermissions,
  getResourceShareLinks,
  getWorkspaceMembers,
  pauseShareLinkManagement,
  resumeShareLinkManagement,
  revokeDocumentPermissionGrant,
  revokeEmailInvite,
  revokeShareLink,
  updateDocumentPermissionGrant,
  type CreateEmailInviteResponse,
  type CreateShareLinkResponse,
  type EmailInviteDto,
  type PermissionGrantDto,
  type ResourcePermissionsResponse,
  type ShareLinkContentProtectionDto,
  type ShareLinkAudience,
  type ShareLinkDto,
  type ShareLinkRole,
  type WorkspaceMemberDto,
} from "../lib/appApi";
import { ApiClientError, getConfiguredApiBaseUrl, isUuid } from "../lib/apiClient";
import {
  getWorkspaceGroups as getPermissionWorkspaceGroups,
  type WorkspaceGroupDto,
} from "../lib/permissionAdminApi";
import {
  createShareLinkRequest,
  defaultPublicSharePolicy,
  getPublicSharePasswordHint,
  getPublicSharePolicyWarnings,
  getDocumentShareLinkStatus,
  getExistingShareLinkCopyCapability,
  getShareLinkGovernanceHint,
  getShareLinkScopeLabel,
  getShareDrawerInviteDisabledReason,
  getShareDrawerLinkDisabledReason,
  toAbsoluteShareUrl,
  toSharePermissionMutationError,
  type DocumentShareLinkStatus,
  type ShareDrawerApiStatus,
} from "../lib/documentShareLinksModel";
import { createShareHash } from "../lib/hashRouting";
import { toUserFacingShareUrl } from "../lib/publicShareModel";
import type { KnowledgeDocument } from "../types/editor";

type ShareDrawerStatus = ShareDrawerApiStatus;
type InviteRole = "commenter" | "editor" | "viewer";
type LinkScope = "invited" | "public" | "workspace";

type DocumentShareDrawerProps = {
  document: KnowledgeDocument;
  isOpen: boolean;
  libraryId?: string | null;
  onClose: () => void;
  workspaceId: string | null;
};

const inviteRoles: Array<{ label: string; value: InviteRole }> = [
  { label: "Can view", value: "viewer" },
  { label: "Can comment", value: "commenter" },
  { label: "Can edit", value: "editor" },
];

const linkRoles: Array<{ label: string; value: ShareLinkRole }> = [
  { label: "Viewer", value: "viewer" },
  { label: "Commenter", value: "commenter" },
];

export function DocumentShareDrawer({ document, isOpen, libraryId, onClose, workspaceId }: DocumentShareDrawerProps) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [permissions, setPermissions] = useState<ResourcePermissionsResponse | null>(null);
  const [collectionPermissions, setCollectionPermissions] = useState<ResourcePermissionsResponse | null>(null);
  const [members, setMembers] = useState<WorkspaceMemberDto[] | null>(null);
  const [groups, setGroups] = useState<WorkspaceGroupDto[] | null>(null);
  const [links, setLinks] = useState<ShareLinkDto[] | null>(null);
  const [broaderLinks, setBroaderLinks] = useState<ShareLinkDto[]>([]);
  const [invites, setInvites] = useState<EmailInviteDto[] | null>(null);
  const [status, setStatus] = useState<ShareDrawerStatus>("idle");
  const [memberStatus, setMemberStatus] = useState<ShareDrawerStatus>("idle");
  const [operation, setOperation] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [inviteValue, setInviteValue] = useState("");
  const [inviteRole, setInviteRole] = useState<InviteRole>("editor");
  const [linkScope, setLinkScope] = useState<LinkScope>("workspace");
  const [linkRole, setLinkRole] = useState<ShareLinkRole>("viewer");
  const [linkExpiresAt, setLinkExpiresAt] = useState("");
  const [passwordEnabled, setPasswordEnabled] = useState(false);
  const [publicPassword, setPublicPassword] = useState("");
  const [contentProtection, setContentProtection] = useState<ShareLinkContentProtectionDto>({
    disableDownload: true,
    disablePrint: false,
    disableCopy: false,
    watermarkEnabled: false,
    watermarkText: "Public link",
  });
  const [createdLink, setCreatedLink] = useState<CreateShareLinkResponse | null>(null);
  const [createdInvite, setCreatedInvite] = useState<CreateEmailInviteResponse | null>(null);

  const peopleGrants = permissions?.grants.filter((grant) => grant.subjectType === "user") ?? [];
  const groupGrants = permissions?.grants.filter((grant) => grant.subjectType === "group") ?? [];
  const visibleInvites = invites?.filter((invite) => invite.status === "pending" || invite.status === "accepted") ?? [];
  const memberById = useMemo(() => new Map((members ?? []).map((member) => [member.userId, member])), [members]);
  const groupById = useMemo(() => new Map((groups ?? []).map((group) => [group.id, group])), [groups]);
  const canUse = status === "ready" && Boolean(apiBaseUrl);
  const canMutate = canUse && !operation;
  const collectionId = document.folderId?.trim() || null;
  const currentLibraryId = libraryId?.trim() || null;
  const normalizedInvite = inviteValue.trim();
  const inviteIsEmail = isEmail(normalizedInvite);
  const selectedMember = findInviteMember(normalizedInvite, members ?? []);
  const availableGrantRoles = useMemo(() => new Set((permissions?.availableRoles ?? ["commenter", "viewer"]).filter(isInviteRole)), [permissions]);
  const isDirectMemberInvite = Boolean(selectedMember || isUuid(normalizedInvite));
  const inviteRoleOptions = useMemo(
    () =>
      inviteRoles.filter((role) => {
        if (isDirectMemberInvite) {
          return availableGrantRoles.has(role.value);
        }

        return role.value === "viewer" || role.value === "commenter";
      }),
    [availableGrantRoles, isDirectMemberInvite],
  );
  const publicExpiryIso = toApiDateTime(linkExpiresAt);
  const inviteDisabledReason = getShareDrawerInviteDisabledReason({
    apiConfigured: Boolean(apiBaseUrl),
    availableRoles: availableGrantRoles,
    inviteIsEmail,
    isDirectMemberInvite,
    memberStatus,
    operation,
    selectedRole: inviteRole,
    status,
    value: normalizedInvite,
  });
  const publicPasswordHint = linkScope === "public" ? getPublicSharePasswordHint("document", defaultPublicSharePolicy) : null;
  const effectivePasswordEnabled = passwordEnabled || Boolean(publicPasswordHint);
  const linkDisabledReason = getShareDrawerLinkDisabledReason({
    apiConfigured: Boolean(apiBaseUrl),
    collectionId,
    contentProtection,
    expiresAt: publicExpiryIso,
    libraryId: currentLibraryId,
    linkScope,
    operation,
    password: publicPassword,
    passwordEnabled: effectivePasswordEnabled,
    policy: defaultPublicSharePolicy,
    publicScope: "document",
    status,
  });
  const canSendInvite = !inviteDisabledReason && inviteRoleOptions.length > 0;
  const canCreateLink = !linkDisabledReason;
  const appOrigin = typeof window !== "undefined" ? window.location.origin : "";
  const generatedUrl = createdLink
    ? toUserFacingShareUrl(createdLink.url, createdLink.token, createdLink.link.audience, apiBaseUrl, appOrigin)
    : "";

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (!apiBaseUrl) {
      setStatus("unconfigured");
      setMemberStatus("unconfigured");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    setMemberStatus(workspaceId ? "loading" : "unconfigured");
    setMessage(null);
    setError(null);

    void Promise.all([
      getDocumentResourcePermissions(document.id, controller.signal),
      getDocumentShareLinks(document.id, controller.signal),
      getDocumentEmailInvites(document.id, controller.signal),
      collectionId
        ? Promise.all([
            getResourcePermissions("collection", collectionId, controller.signal),
            getResourceShareLinks("collection", collectionId, controller.signal),
          ])
        : Promise.resolve(null),
      currentLibraryId
        ? getResourceShareLinks("library", currentLibraryId, controller.signal).catch(() => null)
        : Promise.resolve(null),
    ])
      .then(([nextPermissions, nextLinks, nextInvites, collectionShareState, libraryShareState]) => {
        setPermissions(nextPermissions);
        setCollectionPermissions(collectionShareState?.[0] ?? null);
        setLinks(nextLinks.links);
        setBroaderLinks([...(collectionShareState?.[1].links ?? []), ...(libraryShareState?.links ?? [])]);
        setInvites(nextInvites.invites);
        setStatus("ready");
      })
      .catch((value: unknown) => {
        if (value instanceof DOMException && value.name === "AbortError") {
          return;
        }

        setStatus(isForbiddenError(value) ? "forbidden" : "error");
        setError(toDrawerError(value, "Unable to load share settings."));
      });

    if (workspaceId) {
      void getWorkspaceMembers(workspaceId, controller.signal)
        .then((body) => {
          setMembers(body.members);
          setMemberStatus("ready");
        })
        .catch((value: unknown) => {
          if (value instanceof DOMException && value.name === "AbortError") {
            return;
          }

          setMembers(null);
          setMemberStatus(isForbiddenError(value) ? "forbidden" : "error");
        });

      void getPermissionWorkspaceGroups(apiBaseUrl, workspaceId, controller.signal)
        .then((body) => {
          setGroups(body.groups);
        })
        .catch((value: unknown) => {
          if (value instanceof DOMException && value.name === "AbortError") {
            return;
          }

          setGroups(null);
        });
    }

    return () => controller.abort();
  }, [apiBaseUrl, collectionId, currentLibraryId, document.id, isOpen, workspaceId]);

  useEffect(() => {
    if (inviteRoleOptions.some((role) => role.value === inviteRole)) {
      return;
    }

    setInviteRole(inviteRoleOptions[0]?.value ?? "viewer");
  }, [inviteRole, inviteRoleOptions]);

  if (!isOpen) {
    return null;
  }

  const reloadShareState = async () => {
    const [nextPermissions, nextLinks, nextInvites, collectionShareState, libraryShareState] = await Promise.all([
      getDocumentResourcePermissions(document.id),
      getDocumentShareLinks(document.id),
      getDocumentEmailInvites(document.id),
      collectionId
        ? Promise.all([
            getResourcePermissions("collection", collectionId),
            getResourceShareLinks("collection", collectionId),
          ])
        : Promise.resolve(null),
      currentLibraryId ? getResourceShareLinks("library", currentLibraryId).catch(() => null) : Promise.resolve(null),
    ]);
    setPermissions(nextPermissions);
    setCollectionPermissions(collectionShareState?.[0] ?? null);
    setLinks(nextLinks.links);
    setBroaderLinks([...(collectionShareState?.[1].links ?? []), ...(libraryShareState?.links ?? [])]);
    setInvites(nextInvites.invites);
    setStatus("ready");
  };

  const runOperation = async (operationKey: string, callback: () => Promise<void>) => {
    if (operation) {
      return;
    }

    setOperation(operationKey);
    setMessage(null);
    setError(null);
    try {
      await callback();
    } catch (value) {
      setError(toDrawerError(value, "Operation failed. Retry after the backend session is healthy."));
    } finally {
      setOperation(null);
    }
  };

  const sendInvite = () => {
    void runOperation("invite", async () => {
      if (inviteDisabledReason) {
        setError(inviteDisabledReason);
        return;
      }

      if (!normalizedInvite) {
        return;
      }

      if (selectedMember || isUuid(normalizedInvite)) {
        if (!availableGrantRoles.has(inviteRole)) {
          setError("Selected role is not available for direct member grants.");
          return;
        }

        const subjectId = selectedMember?.userId ?? normalizedInvite;
        await createDocumentPermissionGrant(document.id, {
          expiresAt: null,
          reason: "Shared from document drawer.",
          roleKey: inviteRole,
          subjectId,
          subjectType: "user",
        });
        setInviteValue("");
        setMessage("Member access updated.");
        await reloadShareState();
        return;
      }

      if (!inviteIsEmail) {
        setError(memberStatus === "ready" ? "No matching workspace member found. Enter an email address or user UUID." : "Member search is unavailable. Enter an email invite.");
        return;
      }

      if (inviteRole === "editor") {
        setError("Email invites support viewer or commenter access only.");
        return;
      }

      const created = await createDocumentEmailInvite(document.id, {
        email: normalizedInvite.toLowerCase(),
        expiresAt: defaultInviteExpiry(),
        roleKey: inviteRole,
      });
      setCreatedInvite({ ...created, url: toAbsoluteShareUrl(created.url, apiBaseUrl) });
      setInviteValue("");
      setMessage("Invite created. The invite URL is shown once.");
      await reloadShareState();
    });
  };

  const createLink = () => {
    void runOperation("link", async () => {
      if (linkDisabledReason) {
        setError(linkDisabledReason);
        return;
      }

      if (linkScope === "invited") {
        setMessage("Invitation only mode does not create a share link.");
        return;
      }

      const audience: ShareLinkAudience = linkScope === "public" ? "public" : "workspace";
      const request = createShareLinkRequest({
        audience,
        contentProtection: linkScope === "public" ? contentProtection : null,
        expiresAt: linkScope === "public" ? publicExpiryIso : toApiDateTime(linkExpiresAt),
        password: linkScope === "public" && effectivePasswordEnabled ? publicPassword : null,
        roleKey: linkRole,
        subjectEmail: null,
      });
      const created = await createDocumentShareLink(document.id, request);
      setCreatedLink(created);
      setPublicPassword("");
      setMessage("Share link created. The token-bearing URL is shown once.");
      await reloadShareState();
    });
  };

  const copyGeneratedLink = () => {
    void copyValue(generatedUrl, setMessage, setError);
  };

  const copyExistingLink = (link: ShareLinkDto) => {
    const linkStatus = getDocumentShareLinkStatus(
      link,
      getPolicyLinkModeForShareLink(link, permissions?.policy.linkMode, collectionPermissions?.policy.linkMode),
    );
    const disabledReason = getExistingShareLinkCopyCapability({
      apiConfigured: Boolean(apiBaseUrl),
      canManage: canMutate,
      copyEndpointAvailable: true,
      operation,
      status: linkStatus,
    });

    void runOperation(`copy:${link.id}`, async () => {
      if (disabledReason) {
        setError(disabledReason);
        return;
      }

      const response = await copyShareLinkManagementUrl(link.id, {
        copiedValueType: "share_url",
        reason: "Audited copy from document share drawer.",
      });
      await copyValue(
        toUserFacingShareUrl(response.url, null, link.audience, apiBaseUrl, appOrigin),
        setMessage,
        setError,
      );
      setMessage(response.reissued ? "Audited copy complete. A new URL was issued by the backend." : "Audited copy complete.");
    });
  };

  const advancedHref = createShareHash(document.id);
  const inheritedSource = getInheritedAccessSource(permissions);

  return (
    <div className="document-share-drawer-overlay" role="presentation">
      <aside aria-label="Share document" className="document-share-drawer" role="dialog">
        <header className="document-share-drawer-header">
          <div className="min-w-0">
            <h2>Share</h2>
            <p title={document.title}>{document.title || "Untitled Field Note"}</p>
          </div>
          <button aria-label="Close share drawer" onClick={onClose} title="Close" type="button">
            <X className="h-5 w-5" />
          </button>
        </header>

        <div className="document-share-drawer-body editor-scrollbar">
          {status === "loading" ? <DrawerStatus icon={<Loader2 className="h-4 w-4 animate-spin" />} label="Loading share settings..." /> : null}
          {status === "unconfigured" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label="Share API is not configured." /> : null}
          {status === "forbidden" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label="You do not have permission to manage this document's sharing." /> : null}
          {status === "error" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label={error ?? "Share settings failed to load."} /> : null}

          <section className="document-share-section">
            <h3>Invite people</h3>
            <div className="document-share-invite-row">
              <input
                disabled={!canMutate}
                onChange={(event) => setInviteValue(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter" && canSendInvite) {
                    sendInvite();
                  }
                }}
                placeholder="@member, email, or user UUID"
                title={!canMutate ? inviteDisabledReason ?? undefined : "Search workspace members or enter an email invite."}
                type="text"
                value={inviteValue}
              />
              <select
                disabled={!canMutate || inviteRoleOptions.length === 0}
                onChange={(event) => setInviteRole(event.target.value as InviteRole)}
                title={inviteRoleOptions.length === 0 ? inviteDisabledReason ?? "No roles are available." : "Choose document access role."}
                value={inviteRole}
              >
                {inviteRoleOptions.map((role) => (
                  <option key={role.value} value={role.value}>
                    {role.label}
                  </option>
                ))}
              </select>
              <button className="document-share-primary" disabled={!canSendInvite} onClick={sendInvite} title={inviteDisabledReason ?? "Send invite or grant access"} type="button">
                {operation === "invite" ? "Sending" : "Send"}
              </button>
            </div>
            {members?.length ? (
              <div className="document-share-token-row">
                {members.slice(0, 2).map((member) => (
                  <button
                    disabled={!canMutate}
                    key={member.userId}
                    onClick={() => setInviteValue(member.email ?? member.displayName)}
                    title={`Invite ${member.displayName}`}
                    type="button"
                  >
                    @{member.displayName}
                  </button>
                ))}
              </div>
            ) : null}
            <p className="document-share-help">
              Workspace users become direct grants. Email addresses create email invites. This does not create live presence, comments mentions, or workspace membership.
            </p>
            {memberStatus === "loading" ? (
              <p className="document-share-help">Loading workspace members. You can still prepare an email invite.</p>
            ) : null}
            {memberStatus === "forbidden" || memberStatus === "error" || memberStatus === "unconfigured" ? (
              <p className="document-share-help is-warning">Member search is unavailable or forbidden. Email invites remain available when the share API is ready.</p>
            ) : null}
            {createdInvite ? (
              <GeneratedSecret
                label="Invite link"
                onCopy={() => void copyValue(createdInvite.url, setMessage, setError)}
                value={createdInvite.url}
              />
            ) : null}
          </section>

          <section className="document-share-section">
            <h3>Share link</h3>
            <div className="document-share-copy-row">
              <input readOnly value={generatedUrl || "Create a link to show its URL once"} />
              <button disabled={!generatedUrl} onClick={copyGeneratedLink} type="button">
                <Copy className="h-4 w-4" />
                Copy
              </button>
            </div>
            <div className="document-share-segmented" role="group" aria-label="Share mode">
              <button className={linkScope === "invited" ? "is-active" : ""} onClick={() => setLinkScope("invited")} type="button">
                Invitation only
              </button>
              <button className={linkScope === "workspace" ? "is-active" : ""} onClick={() => setLinkScope("workspace")} type="button">
                Internal
              </button>
              <button className={linkScope === "public" ? "is-active" : ""} onClick={() => setLinkScope("public")} type="button">
                Public
              </button>
            </div>
            <p className="document-share-help">
              Invitation only creates no link. Internal creates a workspace link. Public creates a viewer-only link for this document through the dedicated share-link API and requires a future expiry.
            </p>
            <label className="document-share-select-row">
              <span>
                <Globe2 className="h-4 w-4" />
                Sharing object
              </span>
              <select
                disabled
                onChange={() => undefined}
                title="Document Share Drawer creates document links only."
                value="document"
              >
                {[{ disabled: false, label: "Current document", value: "document" }].map((option) => (
                  <option disabled={option.disabled} key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            {linkScope === "public" ? (
              <p className="document-share-help">
                Public link V1 supports viewer only. Collection and Library public links are managed from Access & Sharing or the container share settings.
              </p>
            ) : null}
            <label className="document-share-select-row">
              <span>
                <Eye className="h-4 w-4" />
                Link role
              </span>
              <select
                disabled={!canMutate || linkScope === "invited" || linkScope === "public"}
                onChange={(event) => setLinkRole(event.target.value as ShareLinkRole)}
                title={linkScope === "public" ? "Public links are viewer-only." : linkScope === "invited" ? "No link role is needed." : "Share links support viewer or commenter only."}
                value={linkScope === "public" ? "viewer" : linkRole}
              >
                {linkRoles.map((role) => (
                  <option key={role.value} value={role.value}>
                    {role.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="document-share-select-row">
              <span>
                <Globe2 className="h-4 w-4" />
                Expiry
              </span>
              <input
                disabled={!canMutate || linkScope === "invited"}
                min={toDateTimeLocalValue(new Date(Date.now() + 60_000).toISOString())}
                onChange={(event) => setLinkExpiresAt(event.target.value)}
                type="datetime-local"
                value={linkExpiresAt}
                title={linkScope === "public" ? "Public links require a future expiry time." : "Optional expiry for this share link."}
              />
            </label>
            <label className="document-share-select-row">
              <span>
                <LockKeyhole className="h-4 w-4" />
                Password
              </span>
              <span className="document-share-password-control">
                <button
                  aria-pressed={effectivePasswordEnabled}
                  className={effectivePasswordEnabled ? "is-on" : ""}
                  disabled={!canMutate || linkScope !== "public" || Boolean(publicPasswordHint)}
                  onClick={() => setPasswordEnabled((current) => !current)}
                  type="button"
                />
                <input
                  autoComplete="new-password"
                  disabled={!canMutate || linkScope !== "public" || !effectivePasswordEnabled}
                  onChange={(event) => setPublicPassword(event.target.value)}
                  placeholder={publicPasswordHint ? "Required by enterprise policy" : "Public links only"}
                  type="password"
                  value={publicPassword}
                />
              </span>
            </label>
            {publicPasswordHint ? <p className="document-share-help is-warning">{publicPasswordHint}</p> : null}
            {linkScope === "public" ? (
              <div className="document-share-protection-options" aria-label="Content protection">
                <label>
                  <input
                    checked={contentProtection.disableDownload}
                    onChange={(event) => setContentProtection((current) => ({ ...current, disableDownload: event.currentTarget.checked }))}
                    type="checkbox"
                  />
                  绂佹涓嬭浇
                </label>
                <label>
                  <input
                    checked={contentProtection.disablePrint}
                    onChange={(event) => setContentProtection((current) => ({ ...current, disablePrint: event.currentTarget.checked }))}
                    type="checkbox"
                  />
                  绂佹鎵撳嵃
                </label>
                <label>
                  <input
                    checked={contentProtection.disableCopy}
                    onChange={(event) => setContentProtection((current) => ({ ...current, disableCopy: event.currentTarget.checked }))}
                    type="checkbox"
                  />
                  绂佹澶嶅埗
                </label>
                <label>
                  <input
                    checked={contentProtection.watermarkEnabled}
                    onChange={(event) => setContentProtection((current) => ({ ...current, watermarkEnabled: event.currentTarget.checked }))}
                    type="checkbox"
                  />
                  鏄剧ず姘村嵃
                </label>
              </div>
            ) : null}
            <button className="document-share-link-action" disabled={!canCreateLink} onClick={createLink} title={linkDisabledReason ?? "Create share link"} type="button">
              <Link2 className="h-4 w-4" />
              {operation === "link" ? "Creating" : linkScope === "public" ? "Create public link" : linkScope === "workspace" ? "Create internal link" : "Invitation only"}
            </button>
            {linkScope === "public" && !publicExpiryIso ? (
              <p className="document-share-help is-warning">Public links require a future expiry. The drawer does not set public access through generic policy mutation.</p>
            ) : null}
          </section>

          <section className="document-share-section">
            <h3>Who can access</h3>
            <p className="document-share-help">
              Permission summary by source. This is not a live viewers or current presence list.
            </p>
            <div className="document-share-access-list">
              <AccessGroup title="Owner">
                <AccessRow
                  initials={initials(document.owner?.name ?? "Owner")}
                  label={document.owner?.name ?? "Document owner"}
                  role="Owner"
                  tone="gold"
                />
              </AccessGroup>

              <AccessGroup title="People">
                {peopleGrants.length || visibleInvites.length ? (
                  <>
                    {peopleGrants.map((grant) => (
                      <GrantAccessRow
                        availableRoles={availableGrantRoles}
                        canMutate={canMutate}
                        grant={grant}
                        key={grant.id}
                        member={memberById.get(grant.subjectId)}
                        onRevoke={() =>
                          void runOperation(`grant:${grant.id}`, async () => {
                            await revokeDocumentPermissionGrant(document.id, grant.id, "Revoked from document drawer.");
                            setMessage("Access revoked.");
                            await reloadShareState();
                          })
                        }
                        onRoleChange={(roleKey) =>
                          void runOperation(`grant:${grant.id}`, async () => {
                            await updateDocumentPermissionGrant(document.id, grant.id, { expiresAt: grant.expiresAt, reason: grant.reason, roleKey });
                            setMessage("Access updated.");
                            await reloadShareState();
                          })
                        }
                      />
                    ))}
                    {visibleInvites.map((invite) => (
                      <InviteAccessRow
                        canMutate={canMutate}
                        invite={invite}
                        key={invite.id}
                        onRevoke={() =>
                          void runOperation(`invite:${invite.id}`, async () => {
                            await revokeEmailInvite(invite.id);
                            setMessage("Email invite revoked.");
                            await reloadShareState();
                          })
                        }
                      />
                    ))}
                  </>
                ) : (
                  <AccessEmpty label="No direct people grants or active email invites." />
                )}
              </AccessGroup>

              <AccessGroup title="Groups">
                {groupGrants.length ? (
                  groupGrants.map((grant) => (
                    <GroupAccessRow
                      grant={grant}
                      group={groupById.get(grant.subjectId)}
                      key={grant.id}
                    />
                  ))
                ) : (
                  <AccessEmpty label="No direct group grants." />
                )}
              </AccessGroup>

              <AccessGroup title="Links">
                {links?.length ? (
                  links.map((link) => (
                    <LinkAccessRow
                      apiConfigured={Boolean(apiBaseUrl)}
                      canMutate={canMutate}
                      key={link.id}
                      link={link}
                      operation={operation}
                      policyLinkMode={getPolicyLinkModeForShareLink(link, permissions?.policy.linkMode, collectionPermissions?.policy.linkMode)}
                      onCopy={() => copyExistingLink(link)}
                      onPause={() =>
                        void runOperation(`pause:${link.id}`, async () => {
                          await pauseShareLinkManagement(link.id, { reason: "Paused from document share drawer." });
                          setMessage("Share link paused.");
                          await reloadShareState();
                        })
                      }
                      onResume={() =>
                        void runOperation(`resume:${link.id}`, async () => {
                          await resumeShareLinkManagement(link.id);
                          setMessage("Share link resumed.");
                          await reloadShareState();
                        })
                      }
                      onRevoke={() =>
                        void runOperation(`link:${link.id}`, async () => {
                          await revokeShareLink(link.id);
                          setMessage("Share link revoked.");
                          await reloadShareState();
                        })
                      }
                    />
                  ))
                ) : (
                  <AccessEmpty label="No document share links returned by the API." />
                )}
              </AccessGroup>

              <AccessGroup title="Related broader links">
                {broaderLinks.length ? (
                  <>
                    <p className="document-share-help">
                      This document may also be accessible through broader Collection / Library links. Manage broader links from Access & Sharing or the corresponding Collection / Library settings.
                    </p>
                    {broaderLinks.map((link) => (
                      <BroaderLinkAccessRow key={link.id} link={link} />
                    ))}
                  </>
                ) : (
                  <AccessEmpty label="No broader Collection or Library links were returned." />
                )}
              </AccessGroup>

              <AccessGroup title="Inherited access">
                {inheritedSource ? (
                  <AccessRow
                    initials={inheritedSource.initials}
                    label={inheritedSource.label}
                    role={`${formatRoleLabel(permissions?.effectiveAccess.effectiveRole ?? "viewer")} / ${formatPolicyLabel(permissions?.policy.inheritanceMode ?? "inherit")}`}
                    tone="gray"
                  />
                ) : (
                  <AccessEmpty label="No workspace or collection inheritance source returned." />
                )}
              </AccessGroup>
            </div>
          </section>
        </div>

        <footer className="document-share-drawer-footer">
          <a href={advancedHref} title="Open document-scoped advanced permissions">
            Advanced permissions
          </a>
          <span className={error ? "is-error" : ""}>{error ?? message}</span>
          <button onClick={onClose} type="button">
            Cancel
          </button>
          <button className="document-share-primary" onClick={onClose} type="button">
            Done
          </button>
        </footer>
      </aside>
    </div>
  );
}

function DrawerStatus({ icon, label }: { icon: ReactNode; label: string }) {
  return (
    <div className="document-share-status">
      {icon}
      {label}
    </div>
  );
}

function GeneratedSecret({ label, onCopy, value }: { label: string; onCopy: () => void; value: string }) {
  return (
    <div className="document-share-generated">
      <span>{label}: {value}</span>
      <button onClick={onCopy} type="button">
        <Copy className="h-4 w-4" />
        Copy
      </button>
    </div>
  );
}

function AccessGroup({ children, title }: { children: ReactNode; title: string }) {
  return (
    <div className="document-share-access-group">
      <h4>{title}</h4>
      {children}
    </div>
  );
}

function AccessEmpty({ label }: { label: string }) {
  return <p className="document-share-access-empty">{label}</p>;
}

function GrantAccessRow({
  availableRoles,
  canMutate,
  grant,
  member,
  onRevoke,
  onRoleChange,
}: {
  availableRoles: Set<InviteRole>;
  canMutate: boolean;
  grant: PermissionGrantDto;
  member?: WorkspaceMemberDto;
  onRevoke: () => void;
  onRoleChange: (roleKey: InviteRole) => void;
}) {
  const label = member?.displayName ?? member?.email ?? grant.subjectId;
  return (
    <AccessRow
      action={
        <>
          <select disabled={!canMutate} onChange={(event) => onRoleChange(event.target.value as InviteRole)} value={normalizeInviteRole(grant.roleKey)}>
            {inviteRoles.filter((role) => availableRoles.has(role.value)).map((role) => (
              <option key={role.value} value={role.value}>
                {role.label}
              </option>
            ))}
          </select>
          <button disabled={!canMutate} onClick={onRevoke} title="Revoke access" type="button">
            <Trash2 className="h-4 w-4" />
          </button>
        </>
      }
      initials={initials(label)}
      label={label}
      role={formatRoleLabel(grant.roleKey)}
      tone="blue"
    />
  );
}

function InviteAccessRow({ canMutate, invite, onRevoke }: { canMutate: boolean; invite: EmailInviteDto; onRevoke: () => void }) {
  return (
    <AccessRow
      action={
        invite.status === "pending" ? (
          <button disabled={!canMutate} onClick={onRevoke} title="Revoke email invite" type="button">
            <Trash2 className="h-4 w-4" />
          </button>
        ) : null
      }
      initials="EM"
      label={invite.email}
      role={`${formatRoleLabel(invite.roleKey)} / invite ${invite.status}`}
      tone="blue"
    />
  );
}

function GroupAccessRow({ grant, group }: { grant: PermissionGrantDto; group?: WorkspaceGroupDto }) {
  const isIamManaged = Boolean(group?.externalProvider || group?.externalGroupId);
  const source = isIamManaged
    ? `IAM-managed${group?.externalProvider ? ` / ${group.externalProvider}` : ""}`
    : "local group";

  return (
    <AccessRow
      initials="GR"
      label={group?.name ?? `Group ${shortId(grant.subjectId)}`}
      role={`${formatRoleLabel(grant.roleKey)} / ${source} / read-only`}
      tone="gray"
    />
  );
}

function getPolicyLinkModeForShareLink(link: ShareLinkDto, documentLinkMode?: string | null, collectionLinkMode?: string | null) {
  if (link.resourceType === "library" && link.audience === "public") {
    return "public";
  }

  return link.resourceType === "collection" ? collectionLinkMode : documentLinkMode;
}

function LinkAccessRow({
  apiConfigured,
  canMutate,
  link,
  operation,
  policyLinkMode,
  onCopy,
  onPause,
  onResume,
  onRevoke,
}: {
  apiConfigured: boolean;
  canMutate: boolean;
  link: ShareLinkDto;
  operation: string | null;
  policyLinkMode: string | null | undefined;
  onCopy: () => void;
  onPause: () => void;
  onResume: () => void;
  onRevoke: () => void;
}) {
  const status = getDocumentShareLinkStatus(link, policyLinkMode);
  const copyDisabledReason = getExistingShareLinkCopyCapability({
    apiConfigured,
    canManage: canMutate,
    copyEndpointAvailable: true,
    operation,
    status,
  });
  const canRevoke = canMutate && status !== "revoked";
  const canPause = canMutate && status === "active";
  const canResume = canMutate && status === "paused";
  const label = `${formatAudienceLabel(link.audience)} link`;
  const metadata = [
    getShareLinkGovernanceHint(link),
    ...getPublicSharePolicyWarnings(link, defaultPublicSharePolicy),
    formatRoleLabel(link.roleKey),
    formatExpiryLabel(link.expiresAt),
    formatLinkStatusLabel(status),
    link.audience === "public" ? (link.hasPassword ? "password" : "no password") : null,
  ].filter(Boolean).join(" / ");

  return (
    <AccessRow
      action={
        <>
          <button disabled={Boolean(copyDisabledReason)} onClick={onCopy} title={copyDisabledReason ?? "Audited copy through backend endpoint"} type="button">
            <Copy className="h-4 w-4" />
          </button>
          {status === "paused" ? (
            <button disabled={!canResume} onClick={onResume} title={canResume ? "Resume link" : "Cannot resume this link"} type="button">
              <Play className="h-4 w-4" />
            </button>
          ) : (
            <button disabled={!canPause} onClick={onPause} title={canPause ? "Pause link" : "Cannot pause this link"} type="button">
              <Pause className="h-4 w-4" />
            </button>
          )}
          <button disabled={!canRevoke} onClick={onRevoke} title={canRevoke ? "Revoke link" : "Cannot revoke this link"} type="button">
            <Trash2 className="h-4 w-4" />
          </button>
        </>
      }
      initials={link.audience === "public" ? "PB" : link.audience === "external" ? "EX" : "LK"}
      label={label}
      role={metadata}
      tone={link.audience === "public" ? "blue" : "gray"}
    />
  );
}

function BroaderLinkAccessRow({ link }: { link: ShareLinkDto }) {
  const metadata = [
    getShareLinkGovernanceHint(link),
    ...getPublicSharePolicyWarnings(link, defaultPublicSharePolicy),
    formatRoleLabel(link.roleKey),
    formatExpiryLabel(link.expiresAt),
    link.status ? formatLinkStatusLabel(link.status as DocumentShareLinkStatus) : null,
    link.audience === "public" ? (link.hasPassword ? "password" : "no password") : null,
  ].filter(Boolean).join(" / ");
  const scopeLabel = getShareLinkScopeLabel(link);

  return (
    <AccessRow
      initials={link.resourceType === "library" ? "LB" : "CO"}
      label={`${scopeLabel} ${formatAudienceLabel(link.audience)} link`}
      role={`${metadata} / manage in Access & Sharing`}
      tone="gray"
    />
  );
}

function AccessRow({
  action,
  initials: avatarInitials,
  label,
  role,
  tone,
}: {
  action?: ReactNode;
  initials: string;
  label: string;
  role: string;
  tone: "blue" | "gold" | "gray";
}) {
  return (
    <div className="document-share-access-row">
      <span className={`document-share-avatar is-${tone}`}>{avatarInitials}</span>
      <span className="min-w-0 flex-1">
        <strong title={label}>{label}</strong>
      </span>
      <span className="document-share-access-role">{role}</span>
      {action ? <span className="document-share-row-actions">{action}</span> : null}
    </div>
  );
}

function findInviteMember(value: string, members: WorkspaceMemberDto[]) {
  const normalized = value.replace(/^@/, "").trim().toLowerCase();
  if (!normalized) {
    return null;
  }

  return (
    members.find(
      (member) =>
        member.userId.toLowerCase() === normalized ||
        member.email?.toLowerCase() === normalized ||
        member.displayName.toLowerCase() === normalized,
    ) ?? null
  );
}

function copyValue(value: string, setMessage: (message: string | null) => void, setError: (message: string | null) => void) {
  if (!value) {
    setError("Nothing to copy.");
    return Promise.resolve();
  }

  if (!navigator.clipboard) {
    setError("Clipboard access is unavailable. Copy the value manually.");
    return Promise.resolve();
  }

  return navigator.clipboard
    .writeText(value)
    .then(() => {
      setError(null);
      setMessage("Copied.");
    })
    .catch(() => setError("Clipboard access was blocked. Copy the value manually."));
}

function isEmail(value: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function isForbiddenError(value: unknown) {
  return value instanceof ApiClientError && (value.status === 401 || value.status === 403);
}

function toDrawerError(value: unknown, fallback: string) {
  return toSharePermissionMutationError(value, fallback);
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

function normalizeInviteRole(value: string): InviteRole {
  return value === "editor" || value === "commenter" ? value : "viewer";
}

function isInviteRole(value: string): value is InviteRole {
  return value === "editor" || value === "commenter" || value === "viewer";
}

function formatRoleLabel(value: string) {
  if (value === "owner") {
    return "Owner";
  }

  if (value === "admin") {
    return "Admin";
  }

  if (value === "editor") {
    return "Can edit";
  }

  if (value === "commenter") {
    return "Can comment";
  }

  return "Can view";
}

function formatAudienceLabel(value: string) {
  if (value === "public") {
    return "Public";
  }

  if (value === "external") {
    return "External";
  }

  return "Workspace";
}

function formatPolicyLabel(value: string) {
  return value === "restricted" ? "restricted" : "inherited";
}

function formatExpiryLabel(value: string | null) {
  return value ? `expires ${new Date(value).toLocaleDateString()}` : "no expiry";
}

function formatLinkStatusLabel(status: DocumentShareLinkStatus) {
  return status;
}

function getInheritedAccessSource(permissions: ResourcePermissionsResponse | null) {
  if (!permissions) {
    return null;
  }

  if (permissions.inheritedFrom === "workspace") {
    return { initials: "WS", label: "Workspace inherited access" };
  }

  if (permissions.inheritedFrom === "collection") {
    return { initials: "CO", label: "Collection inherited access" };
  }

  return null;
}

function initials(value: string) {
  const words = value.trim().split(/\s+/).filter(Boolean);
  if (words.length >= 2) {
    return `${words[0][0]}${words[1][0]}`.toUpperCase();
  }

  return (words[0] ?? "U").slice(0, 2).toUpperCase();
}

function shortId(value: string) {
  return value.length <= 12 ? value : value.slice(0, 8);
}
