import type {
  LinkManagementDto,
  LinkManagementListQuery,
  LinkManagementStatus,
  ShareLinkAccessEventDto,
  ShareLinkAccessStatsResponse,
  ShareLinkAccessTrendPointDto,
  ShareLinkSourceBreakdownDto,
  ShareLinkAudience,
  ShareLinkRole,
  UpdateShareLinkRequest,
} from "./appApi";
import { formatApiOperationError } from "./apiClient";

export type LinkManagementFilterTab = "active" | "all" | "expiring" | "high-risk" | "revoked";
export type LinkManagementRisk = "attention" | "empty" | "expiring" | "high" | "normal";

export type LinkManagementDisplay = {
  risk: LinkManagementRisk;
  riskLabel: string;
  statusClassName: "active" | "danger" | "muted";
  statusLabel: string;
};

export type LinkManagementActionState = {
  canCopyLink: boolean;
  canPause: boolean;
  canResume: boolean;
  canRevoke: boolean;
  disabledReason: string | null;
  pauseLabel: "恢复" | "暂停";
};

export type LinkManagementPatchDraft = {
  expiresAt: string;
  roleKey: ShareLinkRole;
};

export type LinkManagementPatchState = {
  errors: {
    expiresAt?: string;
    roleKey?: string;
  };
  isValid: boolean;
  request: UpdateShareLinkRequest;
};

const expiringWindowMs = 1000 * 60 * 60 * 24 * 7;

export function getLinkManagementDisplay(
  link: Pick<LinkManagementDto, "accessCount" | "audience" | "expiresAt" | "externalOrPublicAccessCount" | "hasPassword" | "recentFailCount" | "revokedAt" | "status">,
  now = new Date(),
): LinkManagementDisplay {
  const normalizedStatus = normalizeLinkManagementStatus(link.status, link.revokedAt, link.expiresAt, now);
  const risk = getLinkRisk(link, now);

  return {
    risk,
    riskLabel: getRiskLabel(risk),
    statusClassName: normalizedStatus === "active" ? "active" : normalizedStatus === "revoked" ? "danger" : "muted",
    statusLabel: getStatusLabel(normalizedStatus),
  };
}

export function getLinkRisk(
  link: Pick<LinkManagementDto, "accessCount" | "audience" | "expiresAt" | "externalOrPublicAccessCount" | "hasPassword" | "recentFailCount" | "revokedAt" | "status">,
  now = new Date(),
): LinkManagementRisk {
  const normalizedStatus = normalizeLinkManagementStatus(link.status, link.revokedAt, link.expiresAt, now);
  if ((normalizedStatus === "revoked" || normalizedStatus === "expired") && link.recentFailCount <= 0) {
    return "empty";
  }

  if (isHighRiskLink(link)) {
    return "high";
  }

  if (link.recentFailCount > 0 || normalizedStatus === "paused" || normalizedStatus === "policy_paused" || (link.audience === "public" && !link.hasPassword)) {
    return "attention";
  }

  if (isExpiringSoon(link.expiresAt, now)) {
    return "expiring";
  }

  return "normal";
}

export function getLinkRiskFromStats(
  link: Pick<LinkManagementDto, "accessCount" | "audience" | "expiresAt" | "externalOrPublicAccessCount" | "hasPassword" | "recentFailCount" | "revokedAt" | "status">,
  stats: Pick<ShareLinkAccessStatsResponse, "accessCount" | "sourceBreakdown" | "trend"> | null,
  now = new Date(),
): LinkManagementRisk {
  const statsLike = stats
    ? {
        ...link,
        accessCount: stats.accessCount,
        externalOrPublicAccessCount: getExternalOrPublicSourceCount(stats.sourceBreakdown),
        recentFailCount: getRecentFailCount(stats.trend),
      }
    : link;
  return getLinkRisk(statsLike, now);
}

export function filterLinksByTab(links: LinkManagementDto[], tab: LinkManagementFilterTab, now = new Date()) {
  if (tab === "active") {
    return links.filter((link) => normalizeLinkManagementStatus(link.status, link.revokedAt, link.expiresAt, now) === "active");
  }

  if (tab === "expiring") {
    return links.filter((link) => isExpiringSoon(link.expiresAt, now) && !link.revokedAt);
  }

  if (tab === "revoked") {
    return links.filter((link) => normalizeLinkManagementStatus(link.status, link.revokedAt, link.expiresAt, now) === "revoked");
  }

  if (tab === "high-risk") {
    return links.filter((link) => getLinkRisk(link, now) === "high");
  }

  return links;
}

export function toLinkManagementQuery(options: {
  audience: ShareLinkAudience | "";
  limit: number;
  offset: number;
  q: string;
  resourceType: string;
  roleKey: ShareLinkRole | "";
  status: string;
  tab: LinkManagementFilterTab;
  workspaceId: string | null;
}): LinkManagementListQuery {
  return {
    audience: options.audience || null,
    limit: options.limit,
    offset: options.offset,
    q: options.q,
    resourceType: options.resourceType || null,
    roleKey: options.roleKey || null,
    status: options.tab === "active" ? "active" : options.tab === "revoked" ? "revoked" : options.status || null,
    workspaceId: options.workspaceId,
  };
}

export function getLinkManagementActionState(link: LinkManagementDto | null, operation: string | null): LinkManagementActionState {
  if (!link) {
    return {
      canCopyLink: false,
      canPause: false,
      canResume: false,
      canRevoke: false,
      disabledReason: "请选择一个链接。",
      pauseLabel: "暂停",
    };
  }

  const status = normalizeLinkManagementStatus(link.status, link.revokedAt, link.expiresAt);
  const busy = Boolean(operation);
  const isPaused = status === "paused";
  const disabledReason = busy
    ? "链接操作正在执行。"
    : status === "revoked"
      ? "已撤销链接只能查看元数据。"
      : status === "expired"
        ? "已过期链接可能在恢复后仍保持过期。"
        : status === "policy_paused"
          ? "资源策略与链接范围不匹配，请在资源权限设置中调整 linkMode。"
        : !link.canManage
          ? "你没有权限管理这个链接。"
          : !link.canPause && !link.canRevoke
            ? "后端未允许对这个链接执行管理操作。"
        : null;

  return {
    canCopyLink: !busy && link.canManage && status !== "revoked" && status !== "expired",
    canPause: !busy && link.canPause && !isPaused && status !== "policy_paused" && status !== "revoked" && status !== "expired",
    canResume: !busy && link.canPause && isPaused,
    canRevoke: !busy && link.canRevoke && status !== "revoked",
    disabledReason,
    pauseLabel: isPaused ? "恢复" : "暂停",
  };
}

export function createLinkPatchDraft(link: LinkManagementDto): LinkManagementPatchDraft {
  return {
    expiresAt: toDateTimeLocalValue(link.expiresAt),
    roleKey: link.audience === "public" ? "viewer" : link.roleKey,
  };
}

export function prepareLinkManagementPatch(link: LinkManagementDto, draft: LinkManagementPatchDraft, now = new Date()): LinkManagementPatchState {
  const roleKey = link.audience === "public" ? "viewer" : draft.roleKey;
  const errors: LinkManagementPatchState["errors"] = {};
  const expiresAt = draft.expiresAt ? new Date(draft.expiresAt) : null;

  if (roleKey !== "viewer" && roleKey !== "commenter") {
    errors.roleKey = "链接权限只能是 viewer 或 commenter。";
  }

  if (link.audience === "public" && roleKey !== "viewer") {
    errors.roleKey = "公开链接只能是 viewer。";
  }

  if (link.audience === "public" && !expiresAt) {
    errors.expiresAt = "公开链接必须设置未来过期时间。";
  }

  if (expiresAt && expiresAt.getTime() <= now.getTime()) {
    errors.expiresAt = "过期时间必须晚于当前时间。";
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
    request: {
      expiresAt: expiresAt ? expiresAt.toISOString() : null,
      reason: "Updated from link management UI.",
      roleKey,
    },
  };
}

export function toLinkManagementMutationError(error: unknown, fallback: string) {
  return formatApiOperationError(error, fallback, {
    forbidden: "你没有权限管理这些链接。",
    network: "无法连接链接管理 API，请检查后端会话后重试。",
    unauthorized: "请重新登录后再管理链接。",
    unconfigured: "当前环境未配置 API base URL。",
  });
}

export function getCopyShareLinkLabel() {
  return "Audited copy link";
}

export function hasForbiddenSecretFields(value: Record<string, unknown>) {
  return ["token", "tokenHash", "token_hash", "passwordHash", "password_hash", "passwordProof", "url"].some((key) => key in value);
}

export function getTrendTotals(trend: ShareLinkAccessTrendPointDto[] | null | undefined) {
  return (trend ?? []).reduce(
    (totals, point) => ({
      failCount: totals.failCount + point.failCount,
      successCount: totals.successCount + point.successCount,
      totalCount: totals.totalCount + point.successCount + point.failCount,
    }),
    { failCount: 0, successCount: 0, totalCount: 0 },
  );
}

export function getSourceBreakdownRows(sourceBreakdown: ShareLinkSourceBreakdownDto[] | null | undefined) {
  return (sourceBreakdown ?? []).map((row) => ({
    ...row,
    label: getSourceLabel(row.source),
  }));
}

export function getAccessEventDisplayRows(events: ShareLinkAccessEventDto[] | null | undefined) {
  return (events ?? []).map((event) => ({
    actor: event.actorDisplayName || event.accessedBy || getActorTypeLabel(event.actorType),
    actorType: getActorTypeLabel(event.actorType),
    device: event.deviceSummary || event.userAgent || "-",
    failure: event.failureCategory || "-",
    id: event.id,
    ip: event.ip || "-",
    rawUserAgent: event.userAgent || "-",
    result: event.result === "success" ? "成功" : "失败",
    time: event.occurredAt || event.accessedAt,
    type: getEventTypeLabel(event.eventType),
  }));
}

export function normalizeLinkManagementStatus(
  status: LinkManagementStatus,
  revokedAt?: string | null,
  expiresAt?: string | null,
  now = new Date(),
) {
  if (revokedAt) {
    return "revoked";
  }

  if (expiresAt && Date.parse(expiresAt) <= now.getTime()) {
    return "expired";
  }

  const normalized = status.trim().toLowerCase();
  if (normalized === "policy_paused" || normalized === "paused" || normalized === "revoked" || normalized === "expired") {
    return normalized;
  }

  return "active";
}

function getStatusLabel(status: ReturnType<typeof normalizeLinkManagementStatus>) {
  if (status === "active") {
    return "活跃";
  }

  if (status === "paused") {
    return "已暂停";
  }

  if (status === "policy_paused") {
    return "策略暂停";
  }

  if (status === "expired") {
    return "已过期";
  }

  if (status === "revoked") {
    return "已撤销";
  }

  return status;
}

function getRiskLabel(risk: LinkManagementRisk) {
  if (risk === "normal") {
    return "正常";
  }

  if (risk === "expiring") {
    return "即将过期";
  }

  if (risk === "attention") {
    return "需关注";
  }

  if (risk === "high") {
    return "高风险";
  }

  return "-";
}

function isHighRiskLink(
  link: Pick<LinkManagementDto, "accessCount" | "audience" | "externalOrPublicAccessCount" | "recentFailCount">,
) {
  if (link.recentFailCount >= 5) {
    return true;
  }

  if (link.accessCount >= 50 && (link.audience === "public" || link.audience === "external")) {
    return true;
  }

  return link.accessCount >= 50 && link.externalOrPublicAccessCount / Math.max(link.accessCount, 1) >= 0.7;
}

function getRecentFailCount(trend: ShareLinkAccessTrendPointDto[]) {
  return getTrendTotals(trend).failCount;
}

function getExternalOrPublicSourceCount(sourceBreakdown: ShareLinkSourceBreakdownDto[]) {
  return sourceBreakdown
    .filter((row) => row.source === "external_authenticated" || row.source === "public_visitor")
    .reduce((sum, row) => sum + row.count, 0);
}

function getSourceLabel(source: string) {
  if (source === "workspace_member") {
    return "工作区成员";
  }

  if (source === "external_authenticated") {
    return "外部认证";
  }

  if (source === "public_visitor") {
    return "公开访客";
  }

  return "未知";
}

function getActorTypeLabel(actorType: string) {
  if (actorType === "authenticated") {
    return "认证用户";
  }

  if (actorType === "anonymous") {
    return "匿名访问";
  }

  return actorType || "未知";
}

function getEventTypeLabel(eventType: string) {
  if (eventType === "resolve") {
    return "解析";
  }

  if (eventType === "access") {
    return "访问";
  }

  if (eventType === "download") {
    return "下载";
  }

  return eventType;
}

function isExpiringSoon(expiresAt: string | null, now: Date) {
  if (!expiresAt) {
    return false;
  }

  const expiresTime = Date.parse(expiresAt);
  const diff = expiresTime - now.getTime();
  return diff > 0 && diff <= expiringWindowMs;
}

function toDateTimeLocalValue(value: string | null) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const pad = (part: number) => String(part).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
