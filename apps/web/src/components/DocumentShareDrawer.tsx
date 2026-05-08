import {
  Copy,
  Eye,
  Globe2,
  Link2,
  Loader2,
  LockKeyhole,
  ShieldCheck,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  createDocumentEmailInvite,
  createDocumentPermissionGrant,
  createDocumentShareLink,
  getDocumentEmailInvites,
  getDocumentResourcePermissions,
  getDocumentShareLinks,
  getWorkspaceMembers,
  revokeDocumentPermissionGrant,
  revokeEmailInvite,
  revokeShareLink,
  updateDocumentPermissionGrant,
  type CreateEmailInviteResponse,
  type CreateShareLinkResponse,
  type EmailInviteDto,
  type PermissionGrantDto,
  type ResourcePermissionsResponse,
  type ShareLinkAudience,
  type ShareLinkDto,
  type ShareLinkRole,
  type WorkspaceMemberDto,
} from "../lib/appApi";
import { ApiClientError, getConfiguredApiBaseUrl, isUuid } from "../lib/apiClient";
import { createShareLinkRequest, toAbsoluteShareUrl } from "../lib/documentShareLinksModel";
import { createShareHash } from "../lib/hashRouting";
import type { KnowledgeDocument } from "../types/editor";

type ShareDrawerStatus = "error" | "forbidden" | "idle" | "loading" | "ready" | "unconfigured";
type InviteRole = "commenter" | "editor" | "viewer";
type LinkScope = "invited" | "public" | "workspace";

type DocumentShareDrawerProps = {
  document: KnowledgeDocument;
  isOpen: boolean;
  onClose: () => void;
  workspaceId: string | null;
};

const inviteRoles: Array<{ label: string; value: InviteRole }> = [
  { label: "可查看", value: "viewer" },
  { label: "可评论", value: "commenter" },
  { label: "可编辑", value: "editor" },
];

const linkRoles: Array<{ label: string; value: ShareLinkRole }> = [
  { label: "可查看", value: "viewer" },
  { label: "可评论", value: "commenter" },
];

export function DocumentShareDrawer({ document, isOpen, onClose, workspaceId }: DocumentShareDrawerProps) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [permissions, setPermissions] = useState<ResourcePermissionsResponse | null>(null);
  const [members, setMembers] = useState<WorkspaceMemberDto[] | null>(null);
  const [links, setLinks] = useState<ShareLinkDto[] | null>(null);
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
  const [createdLink, setCreatedLink] = useState<CreateShareLinkResponse | null>(null);
  const [createdInvite, setCreatedInvite] = useState<CreateEmailInviteResponse | null>(null);

  const activeLinks = links?.filter((link) => !link.revokedAt) ?? [];
  const pendingInvites = invites?.filter((invite) => invite.status === "pending") ?? [];
  const memberById = useMemo(() => new Map((members ?? []).map((member) => [member.userId, member])), [members]);
  const canUse = status === "ready" && Boolean(apiBaseUrl);
  const canMutate = canUse && !operation;
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
  const canSendInvite = Boolean(normalizedInvite) && canMutate && inviteRoleOptions.length > 0;
  const publicExpiryIso = toApiDateTime(linkExpiresAt);
  const canCreateLink =
    canMutate &&
    linkScope !== "invited" &&
    (linkScope !== "public" || Boolean(publicExpiryIso && new Date(publicExpiryIso).getTime() > Date.now()));
  const generatedUrl = createdLink ? toAbsoluteShareUrl(createdLink.url, apiBaseUrl) : "";

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
    ])
      .then(([nextPermissions, nextLinks, nextInvites]) => {
        setPermissions(nextPermissions);
        setLinks(nextLinks.links);
        setInvites(nextInvites.invites);
        setStatus("ready");
      })
      .catch((value: unknown) => {
        if (value instanceof DOMException && value.name === "AbortError") {
          return;
        }

        setStatus(isForbiddenError(value) ? "forbidden" : "error");
        setError(toDrawerError(value, "无法加载分享设置。"));
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
    }

    return () => controller.abort();
  }, [apiBaseUrl, document.id, isOpen, workspaceId]);

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
    const [nextPermissions, nextLinks, nextInvites] = await Promise.all([
      getDocumentResourcePermissions(document.id),
      getDocumentShareLinks(document.id),
      getDocumentEmailInvites(document.id),
    ]);
    setPermissions(nextPermissions);
    setLinks(nextLinks.links);
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
      setError(toDrawerError(value, "操作失败，请稍后重试。"));
    } finally {
      setOperation(null);
    }
  };

  const sendInvite = () => {
    void runOperation("invite", async () => {
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
        setMessage("成员权限已更新。");
        await reloadShareState();
        return;
      }

      if (!inviteIsEmail) {
        setError(memberStatus === "ready" ? "未找到匹配成员，请输入成员姓名、邮箱或用户 UUID。" : "成员搜索不可用，请输入 email invite。");
        return;
      }

      if (inviteRole === "editor") {
        setError("当前后端 email invite 不支持可编辑权限，请改用可查看或可评论。");
        return;
      }

      const created = await createDocumentEmailInvite(document.id, {
        email: normalizedInvite.toLowerCase(),
        expiresAt: defaultInviteExpiry(),
        roleKey: inviteRole,
      });
      setCreatedInvite({ ...created, url: toAbsoluteShareUrl(created.url, apiBaseUrl) });
      setInviteValue("");
      setMessage("邀请已发送。");
      await reloadShareState();
    });
  };

  const createLink = () => {
    void runOperation("link", async () => {
      if (linkScope === "invited") {
        setMessage("当前为仅受邀访问，不需要创建分享链接。");
        return;
      }

      const audience: ShareLinkAudience = linkScope === "public" ? "public" : "workspace";
      const created = await createDocumentShareLink(
        document.id,
        createShareLinkRequest({
          audience,
          expiresAt: linkScope === "public" ? publicExpiryIso : toApiDateTime(linkExpiresAt),
          password: linkScope === "public" && passwordEnabled ? publicPassword : null,
          roleKey: linkRole,
          subjectEmail: null,
        }),
      );
      setCreatedLink({ ...created, url: toAbsoluteShareUrl(created.url, apiBaseUrl) });
      setPublicPassword("");
      setMessage("分享链接已创建，请复制链接。");
      await reloadShareState();
    });
  };

  const copyGeneratedLink = () => {
    void copyValue(generatedUrl, setMessage, setError);
  };

  const advancedHref = createShareHash(document.id);

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
          {status === "loading" ? <DrawerStatus icon={<Loader2 className="h-4 w-4 animate-spin" />} label="正在加载分享设置..." /> : null}
          {status === "unconfigured" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label="Share API 未配置。" /> : null}
          {status === "forbidden" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label="你没有管理此文档分享的权限。" /> : null}
          {status === "error" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label={error ?? "分享设置加载失败。"} /> : null}

          <section className="document-share-section">
            <h3>1. 邀请成员</h3>
            <div className="document-share-invite-row">
              <input
                disabled={!canMutate}
                onChange={(event) => setInviteValue(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter" && canSendInvite) {
                    sendInvite();
                  }
                }}
                placeholder="@用户或邮箱"
                type="text"
                value={inviteValue}
              />
              <select disabled={!canMutate || inviteRoleOptions.length === 0} onChange={(event) => setInviteRole(event.target.value as InviteRole)} value={inviteRole}>
                {inviteRoleOptions.map((role) => (
                  <option key={role.value} value={role.value}>
                    {role.label}
                  </option>
                ))}
              </select>
              <button className="document-share-primary" disabled={!canSendInvite} onClick={sendInvite} type="button">
                {operation === "invite" ? "发送中" : "发送邀请"}
              </button>
            </div>
            <div className="document-share-token-row">
              {members?.slice(0, 2).map((member) => (
                <button
                  disabled={!canMutate}
                  key={member.userId}
                  onClick={() => setInviteValue(member.email ?? member.displayName)}
                  type="button"
                >
                  @{member.displayName}
                </button>
              ))}
              <button disabled={!canMutate} onClick={() => setInviteValue("alex@company.com")} type="button">
                alex@company.com
              </button>
            </div>
            <p className="document-share-help">
              通过输入 @ 搜索成员或输入邮箱邀请。评论中的 @ 仅会通知他人，真正 mention notification 不在本轮实现。
            </p>
            {memberStatus === "forbidden" ? (
              <p className="document-share-help is-warning">成员搜索接口当前不可用或权限不足，仍可使用 email invite。</p>
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
            <h3>2. 分享链接</h3>
            <div className="document-share-copy-row">
              <input readOnly value={generatedUrl || "创建链接后会显示一次性 URL"} />
              <button disabled={!generatedUrl} onClick={copyGeneratedLink} type="button">
                <Copy className="h-4 w-4" />
                复制
              </button>
            </div>
            <div className="document-share-segmented" role="group" aria-label="Share link scope">
              <button className={linkScope === "invited" ? "is-active" : ""} onClick={() => setLinkScope("invited")} type="button">
                仅受邀
              </button>
              <button className={linkScope === "workspace" ? "is-active" : ""} onClick={() => setLinkScope("workspace")} type="button">
                内部可见
              </button>
              <button className={linkScope === "public" ? "is-active" : ""} onClick={() => setLinkScope("public")} type="button">
                公开访问
              </button>
            </div>
            <label className="document-share-select-row">
              <span>
                <Eye className="h-4 w-4" />
                链接权限
              </span>
              <select disabled={!canMutate || linkScope === "invited"} onChange={(event) => setLinkRole(event.target.value as ShareLinkRole)} value={linkRole}>
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
                有效期
              </span>
              <input
                disabled={!canMutate || linkScope === "invited"}
                min={toDateTimeLocalValue(new Date(Date.now() + 60_000).toISOString())}
                onChange={(event) => setLinkExpiresAt(event.target.value)}
                type="datetime-local"
                value={linkExpiresAt}
              />
            </label>
            <label className="document-share-select-row">
              <span>
                <LockKeyhole className="h-4 w-4" />
                访问密码
              </span>
              <span className="document-share-password-control">
                <button
                  aria-pressed={passwordEnabled}
                  className={passwordEnabled ? "is-on" : ""}
                  disabled={!canMutate || linkScope !== "public"}
                  onClick={() => setPasswordEnabled((current) => !current)}
                  type="button"
                />
                <input
                  autoComplete="new-password"
                  disabled={!canMutate || linkScope !== "public" || !passwordEnabled}
                  onChange={(event) => setPublicPassword(event.target.value)}
                  placeholder="仅公开链接可用"
                  type="password"
                  value={publicPassword}
                />
              </span>
            </label>
            <button className="document-share-link-action" disabled={!canCreateLink} onClick={createLink} type="button">
              <Link2 className="h-4 w-4" />
              {operation === "link" ? "创建中" : linkScope === "public" ? "创建公开链接" : "应用链接设置"}
            </button>
            {linkScope === "public" && !publicExpiryIso ? (
              <p className="document-share-help is-warning">公开链接按后端策略需要设置未来过期时间；不会通过普通 policy 开启 public。</p>
            ) : null}
          </section>

          <section className="document-share-section">
            <h3>3. 当前访问</h3>
            <div className="document-share-access-list">
              <AccessRow
                initials={initials(document.owner?.name ?? "Owner")}
                label={document.owner?.name ?? "Document Owner"}
                role="所有者"
                tone="gold"
              />
              {permissions?.grants.filter((grant) => grant.subjectType === "user").map((grant) => (
                <GrantAccessRow
                  availableRoles={availableGrantRoles}
                  canMutate={canMutate}
                  grant={grant}
                  key={grant.id}
                  member={memberById.get(grant.subjectId)}
                  onRevoke={() =>
                    void runOperation(`grant:${grant.id}`, async () => {
                      await revokeDocumentPermissionGrant(document.id, grant.id, "Revoked from document drawer.");
                      setMessage("访问权限已撤销。");
                      await reloadShareState();
                    })
                  }
                  onRoleChange={(roleKey) =>
                    void runOperation(`grant:${grant.id}`, async () => {
                      await updateDocumentPermissionGrant(document.id, grant.id, { expiresAt: grant.expiresAt, reason: grant.reason, roleKey });
                      setMessage("访问权限已更新。");
                      await reloadShareState();
                    })
                  }
                />
              ))}
              {activeLinks.map((link) => (
                <AccessRow
                  action={
                    <button
                      disabled={!canMutate || operation === `link:${link.id}`}
                      onClick={() =>
                        void runOperation(`link:${link.id}`, async () => {
                          await revokeShareLink(link.id);
                          setMessage("分享链接已撤销。");
                          await reloadShareState();
                        })
                      }
                      title="撤销链接"
                      type="button"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  }
                  initials={link.audience === "public" ? "PB" : "L"}
                  key={link.id}
                  label={link.audience === "public" ? "公开访问链接" : "Atlas Library"}
                  role={`${formatRoleLabel(link.roleKey)} / ${formatAudienceLabel(link.audience)}`}
                  tone={link.audience === "public" ? "blue" : "gray"}
                />
              ))}
              {pendingInvites.map((invite) => (
                <AccessRow
                  action={
                    <button
                      disabled={!canMutate || operation === `invite:${invite.id}`}
                      onClick={() =>
                        void runOperation(`invite:${invite.id}`, async () => {
                          await revokeEmailInvite(invite.id);
                          setMessage("邮件邀请已撤销。");
                          await reloadShareState();
                        })
                      }
                      title="撤销邀请"
                      type="button"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  }
                  initials="EM"
                  key={invite.id}
                  label={invite.email}
                  role={`${formatRoleLabel(invite.roleKey)} / 邀请中`}
                  tone="blue"
                />
              ))}
              {permissions ? (
                <AccessRow
                  initials="WS"
                  label="Workspace inherited access"
                  role={`${formatRoleLabel(permissions.effectiveAccess.effectiveRole ?? "viewer")} / ${formatPolicyLabel(permissions.policy.inheritanceMode)}`}
                  tone="gray"
                />
              ) : null}
            </div>
          </section>
        </div>

        <footer className="document-share-drawer-footer">
          <a href={advancedHref}>前往 Advanced permissions</a>
          <span className={error ? "is-error" : ""}>{error ?? message}</span>
          <button onClick={onClose} type="button">
            取消
          </button>
          <button className="document-share-primary" onClick={onClose} type="button">
            完成
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
        复制
      </button>
    </div>
  );
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
          <button disabled={!canMutate} onClick={onRevoke} title="撤销访问" type="button">
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
    setError("暂无可复制内容。");
    return Promise.resolve();
  }

  if (!navigator.clipboard) {
    setError("当前浏览器不支持剪贴板，请手动复制。");
    return Promise.resolve();
  }

  return navigator.clipboard
    .writeText(value)
    .then(() => {
      setError(null);
      setMessage("已复制。");
    })
    .catch(() => setError("剪贴板访问被阻止，请手动复制。"));
}

function isEmail(value: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function isForbiddenError(value: unknown) {
  return value instanceof ApiClientError && (value.status === 401 || value.status === 403);
}

function toDrawerError(value: unknown, fallback: string) {
  if (value instanceof ApiClientError && value.message && !value.message.startsWith("API returned ")) {
    return value.message;
  }

  return fallback;
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
    return "所有者";
  }

  if (value === "admin") {
    return "管理员";
  }

  if (value === "editor") {
    return "可编辑";
  }

  if (value === "commenter") {
    return "可评论";
  }

  return "可查看";
}

function formatAudienceLabel(value: string) {
  if (value === "public") {
    return "公开访问";
  }

  if (value === "external") {
    return "外部认证";
  }

  return "内部可见";
}

function formatPolicyLabel(value: string) {
  return value === "restricted" ? "仅直接授权" : "继承访问";
}

function initials(value: string) {
  const words = value.trim().split(/\s+/).filter(Boolean);
  if (words.length >= 2) {
    return `${words[0][0]}${words[1][0]}`.toUpperCase();
  }

  return (words[0] ?? "U").slice(0, 2).toUpperCase();
}
