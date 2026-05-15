import {
  AlertTriangle,
  ChevronLeft,
  ChevronRight,
  Copy,
  FileText,
  Filter,
  MoreHorizontal,
  Pause,
  Play,
  RefreshCw,
  Search,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import {
  copyShareLinkManagementUrl,
  getShareLinkManagementDetail,
  getShareLinkAccessEvents,
  getShareLinkStats,
  listShareLinkManagement,
  pauseShareLinkManagement,
  resumeShareLinkManagement,
  revokeShareLink,
  type LinkManagementDto,
  type LinkManagementListResponse,
  type ShareLinkAccessEventsResponse,
  type ShareLinkAccessStatsResponse,
  type ShareLinkAudience,
  type ShareLinkRole,
} from "../lib/appApi";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import {
  filterLinksByTab,
  getCopyShareLinkLabel,
  getAccessEventDisplayRows,
  getSourceBreakdownRows,
  getTrendTotals,
  getLinkManagementActionState,
  getLinkManagementDisplay,
  toLinkManagementMutationError,
  toLinkManagementQuery,
  type LinkManagementFilterTab,
} from "../lib/linkManagementModel";
import { toUserFacingShareUrl } from "../lib/publicShareModel";

type LinkManagementStatus = "error" | "forbidden" | "idle" | "loading" | "ready" | "unconfigured";
type LinkAnalyticsStatus = "error" | "forbidden" | "idle" | "loading" | "ready" | "unconfigured";

const tabs: Array<{ id: LinkManagementFilterTab; label: string }> = [
  { id: "all", label: "全部" },
  { id: "active", label: "活跃" },
  { id: "expiring", label: "即将过期" },
  { id: "revoked", label: "已撤销" },
  { id: "high-risk", label: "高风险" },
];

const pageSizes = [10, 20, 50];

export function WorkspaceLinkManagementPanel({ workspaceId }: { workspaceId: string | null }) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [tab, setTab] = useState<LinkManagementFilterTab>("all");
  const [q, setQ] = useState("");
  const [roleKey, setRoleKey] = useState<ShareLinkRole | "">("");
  const [statusFilter, setStatusFilter] = useState("");
  const [audience, setAudience] = useState<ShareLinkAudience | "">("");
  const [resourceType, setResourceType] = useState("");
  const [offset, setOffset] = useState(0);
  const [limit, setLimit] = useState(20);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [list, setList] = useState<LinkManagementListResponse | null>(null);
  const [detail, setDetail] = useState<LinkManagementDto | null>(null);
  const [stats, setStats] = useState<ShareLinkAccessStatsResponse | null>(null);
  const [events, setEvents] = useState<ShareLinkAccessEventsResponse | null>(null);
  const [status, setStatus] = useState<LinkManagementStatus>(() => (apiBaseUrl ? "idle" : "unconfigured"));
  const [detailStatus, setDetailStatus] = useState<LinkManagementStatus>("idle");
  const [statsStatus, setStatsStatus] = useState<LinkAnalyticsStatus>("idle");
  const [eventsStatus, setEventsStatus] = useState<LinkAnalyticsStatus>("idle");
  const [eventsOffset, setEventsOffset] = useState(0);
  const [error, setError] = useState("");
  const [statsError, setStatsError] = useState("");
  const [eventsError, setEventsError] = useState("");
  const [operation, setOperation] = useState<string | null>(null);
  const [operationError, setOperationError] = useState("");
  const [operationMessage, setOperationMessage] = useState("");
  const [copiedLinkId, setCopiedLinkId] = useState<string | null>(null);

  const query = useMemo(
    () => toLinkManagementQuery({
      audience,
      limit,
      offset,
      q,
      resourceType,
      roleKey,
      status: statusFilter,
      tab,
      workspaceId,
    }),
    [audience, limit, offset, q, resourceType, roleKey, statusFilter, tab, workspaceId],
  );

  const loadList = () => {
    if (!apiBaseUrl) {
      setList(null);
      setStatus("unconfigured");
      return undefined;
    }

    if (!workspaceId) {
      setList(null);
      setStatus("idle");
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    setError("");
    void listShareLinkManagement(query, controller.signal)
      .then((response) => {
        setList(response);
        setStatus("ready");
      })
      .catch((loadError: unknown) => {
        if (controller.signal.aborted || (loadError instanceof DOMException && loadError.name === "AbortError")) {
          return;
        }

        setList(null);
        setStatus(loadError instanceof ApiClientError && (loadError.status === 401 || loadError.status === 403) ? "forbidden" : "error");
        setError(toLinkManagementMutationError(loadError, "链接管理列表加载失败。"));
      });

    return () => controller.abort();
  };

  useEffect(loadList, [apiBaseUrl, query, workspaceId]);

  useEffect(() => {
    if (!selectedId || !apiBaseUrl) {
      setDetail(null);
      setDetailStatus(selectedId ? "unconfigured" : "idle");
      return;
    }

    const controller = new AbortController();
    setDetailStatus("loading");
    void getShareLinkManagementDetail(selectedId, controller.signal)
      .then((response) => {
        setDetail(response);
        setDetailStatus("ready");
      })
      .catch((loadError: unknown) => {
        if (controller.signal.aborted || (loadError instanceof DOMException && loadError.name === "AbortError")) {
          return;
        }

        setDetail(null);
        setDetailStatus(loadError instanceof ApiClientError && (loadError.status === 401 || loadError.status === 403) ? "forbidden" : "error");
        setOperationError(toLinkManagementMutationError(loadError, "链接详情加载失败。"));
      });

    return () => controller.abort();
  }, [apiBaseUrl, selectedId]);

  const loadAnalytics = (shareLinkId: string, offsetValue = 0) => {
    if (!apiBaseUrl) {
      setStats(null);
      setEvents(null);
      setStatsStatus("unconfigured");
      setEventsStatus("unconfigured");
      return undefined;
    }

    const controller = new AbortController();
    setStatsStatus("loading");
    setEventsStatus("loading");
    setStatsError("");
    setEventsError("");
    void getShareLinkStats(shareLinkId, controller.signal)
      .then((response) => {
        setStats(response);
        setStatsStatus("ready");
      })
      .catch((loadError: unknown) => {
        if (controller.signal.aborted || (loadError instanceof DOMException && loadError.name === "AbortError")) {
          return;
        }

        setStats(null);
        setStatsStatus(loadError instanceof ApiClientError && (loadError.status === 401 || loadError.status === 403) ? "forbidden" : "error");
        setStatsError(toLinkManagementMutationError(loadError, "访问统计加载失败。"));
      });

    void getShareLinkAccessEvents(shareLinkId, { limit: 10, offset: offsetValue }, controller.signal)
      .then((response) => {
        setEvents(response);
        setEventsStatus("ready");
      })
      .catch((loadError: unknown) => {
        if (controller.signal.aborted || (loadError instanceof DOMException && loadError.name === "AbortError")) {
          return;
        }

        setEvents(null);
        setEventsStatus(loadError instanceof ApiClientError && (loadError.status === 401 || loadError.status === 403) ? "forbidden" : "error");
        setEventsError(toLinkManagementMutationError(loadError, "访问审计加载失败。"));
      });

    return () => controller.abort();
  };

  useEffect(() => {
    if (!selectedId) {
      setStats(null);
      setEvents(null);
      setStatsStatus("idle");
      setEventsStatus("idle");
      setEventsOffset(0);
      return undefined;
    }

    return loadAnalytics(selectedId, eventsOffset);
  }, [apiBaseUrl, selectedId, eventsOffset]);

  const rows = useMemo(() => filterLinksByTab(list?.links ?? [], tab), [list?.links, tab]);
  const totalCount = list?.totalCount ?? rows.length;
  const pageIndex = Math.floor(offset / limit) + 1;
  const canPrevious = offset > 0;
  const canNext = list ? list.hasMore || offset + limit < list.totalCount : false;

  const refresh = async () => {
    setOperationMessage("");
    if (apiBaseUrl && workspaceId) {
      try {
        setList(await listShareLinkManagement(query));
        setStatus("ready");
      } catch (refreshError) {
        setStatus(refreshError instanceof ApiClientError && (refreshError.status === 401 || refreshError.status === 403) ? "forbidden" : "error");
        setError(toLinkManagementMutationError(refreshError, "链接管理列表加载失败。"));
      }
    }

    if (selectedId) {
      setDetailStatus("loading");
      try {
        setDetail(await getShareLinkManagementDetail(selectedId));
        setDetailStatus("ready");
        const [nextStats, nextEvents] = await Promise.all([
          getShareLinkStats(selectedId),
          getShareLinkAccessEvents(selectedId, { limit: 10, offset: eventsOffset }),
        ]);
        setStats(nextStats);
        setEvents(nextEvents);
        setStatsStatus("ready");
        setEventsStatus("ready");
      } catch (refreshError) {
        setDetailStatus(refreshError instanceof ApiClientError && (refreshError.status === 401 || refreshError.status === 403) ? "forbidden" : "error");
      }
    }
  };

  const runOperation = async (
    name: string,
    action: () => Promise<unknown>,
    success: string,
    options: { refreshAfter?: boolean; copiedLinkId?: string | null } = {},
  ) => {
    setOperation(name);
    setOperationError("");
    setOperationMessage("");
    try {
      await action();
      setOperationMessage(success);
      setCopiedLinkId(options.copiedLinkId ?? null);
      if (options.refreshAfter ?? true) {
        await refresh();
      }
    } catch (mutationError) {
      setOperationError(toLinkManagementMutationError(mutationError, "链接操作失败。"));
    } finally {
      setOperation(null);
    }
  };

  return (
    <section className="link-management-shell" aria-label="链接管理">
      <div className="link-management-main">
        <header className="link-management-header">
          <div>
            <h2>链接管理 / Access & Sharing</h2>
            <p>面向工作区管理员的集中治理视图。日常文档分享仍在文档 Share Drawer 中完成。</p>
          </div>
          <div className="link-management-header-actions">
            <button
              className="permission-admin-primary-action"
              disabled
              title="创建文档或文件夹链接请先在对应资源的 Share Drawer 中操作。"
              type="button"
            >
              从资源创建
            </button>
            <button className="permission-admin-icon-button" onClick={() => void refresh()} type="button">
              <RefreshCw className="h-4 w-4" />
              刷新
            </button>
          </div>
        </header>

        <div className="link-management-toolbar">
          <nav className="link-management-tabs" aria-label="链接筛选">
            {tabs.map((item) => (
              <button
                aria-pressed={tab === item.id}
                className={tab === item.id ? "is-active" : ""}
                key={item.id}
                onClick={() => {
                  setOffset(0);
                  setTab(item.id);
                }}
                type="button"
              >
                {item.label}
              </button>
            ))}
          </nav>
          <label className="link-management-search">
            <Search className="h-4 w-4" />
            <input
              onChange={(event) => {
                setOffset(0);
                setQ(event.currentTarget.value);
              }}
              placeholder="搜索资源名称、链接 ID、创建人..."
              type="search"
              value={q}
            />
          </label>
          <select onChange={(event) => setRoleKey(event.currentTarget.value as ShareLinkRole | "")} value={roleKey}>
            <option value="">权限</option>
            <option value="viewer">可查看</option>
            <option value="commenter">可评论</option>
          </select>
          <select onChange={(event) => setStatusFilter(event.currentTarget.value)} value={statusFilter}>
            <option value="">状态</option>
            <option value="active">活跃</option>
            <option value="paused">已暂停</option>
            <option value="policy_paused">策略暂停</option>
            <option value="expired">已过期</option>
            <option value="revoked">已撤销</option>
          </select>
          <select onChange={(event) => setAudience(event.currentTarget.value as ShareLinkAudience | "")} value={audience}>
            <option value="">更多筛选</option>
            <option value="workspace">工作区成员</option>
            <option value="external">外部访客</option>
            <option value="public">公开链接</option>
          </select>
          <select onChange={(event) => setResourceType(event.currentTarget.value)} value={resourceType}>
            <option value="">资源类型</option>
            <option value="document">文档</option>
            <option value="collection">文件夹</option>
          </select>
          <Filter className="h-4 w-4 link-management-filter-icon" aria-hidden="true" />
        </div>

        {status !== "ready" ? (
          <LinkManagementState status={status} error={error} onRetry={() => void refresh()} />
        ) : rows.length === 0 ? (
          <LinkManagementEmpty tab={tab} />
        ) : (
          <LinkManagementTable
            copiedLinkId={copiedLinkId}
            links={rows}
            onCopy={(link) => void runOperation(
              `copy-${link.id}`,
              () => copyShareUrl(link, apiBaseUrl),
              "已复制访问链接。",
              { copiedLinkId: link.id, refreshAfter: false },
            )}
            onSelect={(link) => setSelectedId(link.id)}
          />
        )}

        <footer className="link-management-pagination">
          <span>共 {totalCount} 条</span>
          <select
            aria-label="page size"
            onChange={(event) => {
              setLimit(Number(event.currentTarget.value));
              setOffset(0);
            }}
            value={limit}
          >
            {pageSizes.map((size) => (
              <option key={size} value={size}>{size} 条/页</option>
            ))}
          </select>
          <button disabled={!canPrevious} onClick={() => setOffset(Math.max(0, offset - limit))} type="button">
            <ChevronLeft className="h-4 w-4" />
          </button>
          <span className="link-management-page-index">{pageIndex}</span>
          <button disabled={!canNext} onClick={() => setOffset(offset + limit)} type="button">
            <ChevronRight className="h-4 w-4" />
          </button>
        </footer>
      </div>

      {selectedId ? (
        <div className="link-management-modal-backdrop" onClick={() => {
          setSelectedId(null);
          setDetail(null);
          setStats(null);
          setEvents(null);
          setOperationError("");
          setOperationMessage("");
        }}>
          <LinkDetailDrawer
            detail={detail}
            detailStatus={detailStatus}
            copiedLinkId={copiedLinkId}
            events={events}
            eventsStatus={eventsStatus}
            eventsError={eventsError}
            eventsOffset={eventsOffset}
            operation={operation}
            operationError={operationError}
            operationMessage={operationMessage}
            stats={stats}
            statsStatus={statsStatus}
            statsError={statsError}
            onClose={() => {
              setSelectedId(null);
              setDetail(null);
              setStats(null);
              setEvents(null);
              setOperationError("");
              setOperationMessage("");
            }}
            onEventsPage={(nextOffset) => setEventsOffset(Math.max(0, nextOffset))}
            onRetryAnalytics={() => {
              if (selectedId) {
                loadAnalytics(selectedId, eventsOffset);
              }
            }}
            onCopy={(link) => void runOperation(
              `copy-${link.id}`,
              () => copyShareUrl(link, apiBaseUrl),
              "已复制访问链接。",
              { copiedLinkId: link.id, refreshAfter: false },
            )}
            onPause={(link) => void runOperation(`pause-${link.id}`, () => pauseShareLinkManagement(link.id, { reason: "Paused from link management UI." }), "链接已暂停。")}
            onResume={(link) => void runOperation(`resume-${link.id}`, () => resumeShareLinkManagement(link.id), "已提交恢复。若链接已过期，恢复后仍可能不可访问。")}
            onRevoke={(link) => void runOperation(`revoke-${link.id}`, () => revokeShareLink(link.id), "链接已撤销，历史元数据保留。")}
          />
        </div>
      ) : null}
    </section>
  );
}

function LinkManagementTable({
  copiedLinkId,
  links,
  onCopy,
  onSelect,
}: {
  copiedLinkId: string | null;
  links: LinkManagementDto[];
  onCopy: (link: LinkManagementDto) => void;
  onSelect: (link: LinkManagementDto) => void;
}) {
  return (
    <div className="permission-admin-table link-management-table">
      <div className="permission-admin-table-head is-link-management">
        <span>资源</span>
        <span>链接 ID</span>
        <span>范围</span>
        <span>权限</span>
        <span>创建人</span>
        <span>状态</span>
        <span>有效期</span>
        <span>最近访问</span>
        <span>访问次数</span>
        <span>风险</span>
        <span>操作</span>
      </div>
      {links.map((link) => {
        const display = getLinkManagementDisplay(link);
        const actionState = getLinkManagementActionState(link, null);
        const isCopied = copiedLinkId === link.id;
        return (
          <article className="permission-admin-table-row is-link-management" key={link.id} onClick={() => onSelect(link)}>
            <span className="permission-admin-identity">
              <span className="permission-admin-avatar is-secure"><FileText className="h-4 w-4" /></span>
              <span className="min-w-0">
                <strong title={link.resourceTitle ?? link.resourceId}>{link.resourceTitle ?? "未命名资源"}</strong>
                <small>{formatResourceType(link.resourceType)} / {shortId(link.resourceId)}</small>
              </span>
            </span>
            <span className="permission-admin-cell-text" title={link.id}>{shortId(link.id)}</span>
            <StatusPill className="is-muted" label={formatAudience(link.audience)} />
            <StatusPill className="is-muted" label={formatRole(link.roleKey)} />
            <span className="permission-admin-cell-text" title={link.createdByDisplayName ?? link.createdBy ?? undefined}>
              {link.createdByDisplayName ?? shortId(link.createdBy ?? "")}
            </span>
            <StatusPill className={`is-${display.statusClassName}`} label={display.statusLabel} />
            <span className="permission-admin-cell-text">{formatExpiry(link.expiresAt, link.revokedAt)}</span>
            <span className="permission-admin-cell-text">{formatNullableDateTime(link.lastAccessedAt)}</span>
            <span className="permission-admin-cell-text">{formatCount(link.accessCount)}</span>
            <StatusPill
              className={display.risk === "high" ? "is-danger" : display.risk === "attention" || display.risk === "expiring" ? "is-warning" : display.risk === "normal" ? "is-active" : "is-muted"}
              label={display.riskLabel}
            />
            <span className="permission-admin-row-actions">
              <button
                className="permission-admin-icon-button"
                disabled={!actionState.canCopyLink}
                onClick={(event) => {
                  event.stopPropagation();
                  if (actionState.canCopyLink) {
                    onCopy(link);
                  }
                }}
                title={isCopied ? "已复制" : actionState.canCopyLink ? getCopyShareLinkLabel() : actionState.disabledReason ?? "当前链接不可复制。"}
                type="button"
              >
                <Copy className="h-4 w-4" />
                <span className="sr-only">{isCopied ? "已复制" : getCopyShareLinkLabel()}</span>
              </button>
              <button
                className="permission-admin-icon-button"
                onClick={(event) => {
                  event.stopPropagation();
                  onSelect(link);
                }}
                title="打开详情"
                type="button"
              >
                <MoreHorizontal className="h-4 w-4" />
              </button>
            </span>
          </article>
        );
      })}
    </div>
  );
}

function LinkDetailDrawer({
  copiedLinkId,
  detail,
  detailStatus,
  events,
  eventsError,
  eventsOffset,
  eventsStatus,
  operation,
  operationError,
  operationMessage,
  onClose,
  onCopy,
  onEventsPage,
  onPause,
  onResume,
  onRetryAnalytics,
  onRevoke,
  stats,
  statsError,
  statsStatus,
}: {
  copiedLinkId: string | null;
  detail: LinkManagementDto | null;
  detailStatus: LinkManagementStatus;
  events: ShareLinkAccessEventsResponse | null;
  eventsError: string;
  eventsOffset: number;
  eventsStatus: LinkAnalyticsStatus;
  operation: string | null;
  operationError: string;
  operationMessage: string;
  onClose: () => void;
  onCopy: (link: LinkManagementDto) => void;
  onEventsPage: (offset: number) => void;
  onPause: (link: LinkManagementDto) => void;
  onResume: (link: LinkManagementDto) => void;
  onRetryAnalytics: () => void;
  onRevoke: (link: LinkManagementDto) => void;
  stats: ShareLinkAccessStatsResponse | null;
  statsError: string;
  statsStatus: LinkAnalyticsStatus;
}) {
  const [confirmRevoke, setConfirmRevoke] = useState(false);
  const actionState = getLinkManagementActionState(detail, operation);
  const isCopied = Boolean(detail && copiedLinkId === detail.id);

  useEffect(() => {
    setConfirmRevoke(false);
  }, [detail?.id]);

  if (detailStatus !== "ready" || !detail) {
    return (
      <aside className="link-management-drawer" aria-label="链接详情" onClick={(event) => event.stopPropagation()} role="dialog" aria-modal="true">
        <header>
          <div>
            <h3>链接详情</h3>
            <p>{detailStatus === "loading" ? "正在加载链接元数据。" : "链接详情不可用。"}</p>
          </div>
          <button className="permission-admin-icon-button" onClick={onClose} type="button"><X className="h-4 w-4" /></button>
        </header>
      </aside>
    );
  }

  const display = getLinkManagementDisplay(detail);

  return (
    <aside className="link-management-drawer" aria-label="链接详情" onClick={(event) => event.stopPropagation()} role="dialog" aria-modal="true">
      <header>
        <div className="min-w-0">
          <h3>链接详情</h3>
          <p title={detail.resourceTitle ?? detail.resourceId}>{detail.resourceTitle ?? "未命名资源"}</p>
        </div>
        <div className="link-management-drawer-actions">
          <button className="permission-admin-icon-button" disabled={!actionState.canCopyLink} onClick={() => onCopy(detail)} title={isCopied ? "已复制" : getCopyShareLinkLabel()} type="button">
            <Copy className="h-4 w-4" />
            {isCopied ? "已复制" : "复制链接"}
          </button>
          {actionState.pauseLabel === "恢复" ? (
            <button className="permission-admin-icon-button" disabled={!actionState.canResume} onClick={() => onResume(detail)} title={actionState.disabledReason ?? "恢复链接"} type="button">
              <Play className="h-4 w-4" />
              恢复
            </button>
          ) : (
            <button className="permission-admin-icon-button" disabled={!actionState.canPause} onClick={() => onPause(detail)} title={actionState.disabledReason ?? "暂停链接"} type="button">
              <Pause className="h-4 w-4" />
              暂停
            </button>
          )}
          <button
            className="permission-admin-icon-button is-danger"
            disabled={!actionState.canRevoke}
            onClick={() => {
              if (!confirmRevoke) {
                setConfirmRevoke(true);
                return;
              }
              onRevoke(detail);
            }}
            title={actionState.disabledReason ?? (confirmRevoke ? "确认撤销链接" : "撤销链接")}
            type="button"
          >
            <Trash2 className="h-4 w-4" />
            {confirmRevoke ? "确认撤销" : "撤销"}
          </button>
          <button className="permission-admin-icon-button" onClick={onClose} title="关闭" type="button"><X className="h-4 w-4" /></button>
        </div>
      </header>

      {actionState.disabledReason ? <p className="permission-admin-inline-status">{actionState.disabledReason}</p> : null}
      {operationMessage ? <p className="permission-admin-inline-status">{operationMessage}</p> : null}
      {operationError ? <p className="permission-admin-inline-status is-error" role="alert">{operationError}</p> : null}

      <dl className="link-management-detail-list">
        <DefinitionItem label="链接 ID" value={detail.id} />
        <DefinitionItem label="资源" value={detail.resourceTitle ?? detail.resourceId} />
        <DefinitionItem label="资源 ID" value={detail.resourceId} />
        <DefinitionItem label="资源类型" value={formatResourceType(detail.resourceType)} />
        <DefinitionItem label="创建人" value={detail.createdByDisplayName ?? detail.createdBy ?? "-"} />
        <DefinitionItem label="创建时间" value={formatDateTime(detail.createdAt)} />
        <DefinitionItem label="权限" value={formatRole(detail.roleKey)} />
        <DefinitionItem label="范围" value={`${formatAudience(detail.audience)}${detail.subjectEmail ? ` / ${detail.subjectEmail}` : ""}`} />
        <DefinitionItem label="状态" value={display.statusLabel} />
        <DefinitionItem label="策略状态" value={formatPolicyState(detail.policyState, detail.linkMode)} />
        <DefinitionItem label="密码保护" value={detail.hasPassword ? "有密码" : "无密码"} />
        <DefinitionItem label="过期时间" value={detail.expiresAt ? formatDateTime(detail.expiresAt) : "永不过期"} />
        <DefinitionItem label="最近访问" value={formatNullableDateTime(stats?.lastAccessedAt ?? detail.lastAccessedAt)} />
        <DefinitionItem label="访问次数" value={formatCount(stats?.accessCount ?? detail.accessCount)} />
        <DefinitionItem label="唯一访客" value={formatCount(stats?.uniqueVisitorCount ?? detail.uniqueVisitorCount)} />
      </dl>

      <TrendPanel error={statsError} onRetry={onRetryAnalytics} stats={stats} status={statsStatus} />
      <SourceBreakdownPanel error={statsError} onRetry={onRetryAnalytics} stats={stats} status={statsStatus} />
      <AccessEventsPanel
        error={eventsError}
        events={events}
        offset={eventsOffset}
        onPage={onEventsPage}
        onRetry={onRetryAnalytics}
        status={eventsStatus}
      />
    </aside>
  );
}

function TrendPanel({
  error,
  onRetry,
  stats,
  status,
}: {
  error: string;
  onRetry: () => void;
  stats: ShareLinkAccessStatsResponse | null;
  status: LinkAnalyticsStatus;
}) {
  const trend = stats?.trend ?? [];
  const totals = getTrendTotals(trend);
  const max = Math.max(...trend.map((point) => point.successCount + point.failCount), 1);
  const displayTrend = trend.length > 0
    ? trend
    : Array.from({ length: 7 }, (_, index) => ({
        date: new Date(Date.now() - (6 - index) * 24 * 60 * 60 * 1000).toISOString().slice(0, 10),
        failCount: 0,
        successCount: 0,
      }));

  return (
    <section className="link-management-analytics-panel">
      <AnalyticsPanelHeader error={error} onRetry={onRetry} status={status} title="近 7 天访问趋势" />
      {status === "ready" ? (
        <div className="link-management-trend" aria-label="近 7 天访问趋势">
          {displayTrend.map((point) => {
            const total = point.successCount + point.failCount;
            return (
              <div className={`link-management-trend-bar${totals.totalCount <= 0 ? " is-empty" : ""}`} key={point.date} title={`${point.date}: 成功 ${point.successCount}, 失败 ${point.failCount}`}>
                <span style={{ height: `${Math.max(8, (total / max) * 100)}%` }} />
                <small>{point.date.slice(5)}</small>
              </div>
            );
          })}
          {totals.totalCount <= 0 ? <p className="link-management-chart-empty">暂无访问</p> : null}
        </div>
      ) : null}
    </section>
  );
}

function SourceBreakdownPanel({
  error,
  onRetry,
  stats,
  status,
}: {
  error: string;
  onRetry: () => void;
  stats: ShareLinkAccessStatsResponse | null;
  status: LinkAnalyticsStatus;
}) {
  const rows = getSourceBreakdownRows(stats?.sourceBreakdown);

  return (
    <section className="link-management-analytics-panel">
      <AnalyticsPanelHeader error={error} onRetry={onRetry} status={status} title="访问来源" />
      {status === "ready" && rows.length > 0 ? (
        <div className="link-management-source-list">
          {rows.map((row) => (
            <div key={row.source}>
              <span>{row.label}</span>
              <strong>{formatCount(row.count)}</strong>
              <small>{Number(row.percentage).toFixed(1)}%</small>
            </div>
          ))}
        </div>
      ) : status === "ready" ? (
        <div className="link-management-source-list is-empty">
          <div>
            <span>暂无访问来源</span>
            <strong>0</strong>
            <small>0.0%</small>
          </div>
        </div>
      ) : null}
    </section>
  );
}

function AccessEventsPanel({
  error,
  events,
  offset,
  onPage,
  onRetry,
  status,
}: {
  error: string;
  events: ShareLinkAccessEventsResponse | null;
  offset: number;
  onPage: (offset: number) => void;
  onRetry: () => void;
  status: LinkAnalyticsStatus;
}) {
  const rows = getAccessEventDisplayRows(events?.events);
  const limit = events?.limit ?? 10;

  return (
    <section className="link-management-analytics-panel">
      <AnalyticsPanelHeader error={error} onRetry={onRetry} status={status} title="最近访问审计" />
      {status === "ready" && rows.length > 0 ? (
        <>
          <div className="link-management-events">
            {rows.map((row) => (
              <article key={row.id}>
                <strong>{formatDateTime(row.time)}</strong>
                <span>{row.actor} / {row.actorType} / {row.type} / {row.result}</span>
                <small>IP {row.ip} · {row.device} · {row.failure}</small>
              </article>
            ))}
          </div>
          <div className="link-management-events-pagination">
            <button disabled={offset <= 0} onClick={() => onPage(Math.max(0, offset - limit))} type="button">上一页</button>
            <span>{offset + 1}-{Math.min(offset + rows.length, events?.totalCount ?? rows.length)} / {events?.totalCount ?? rows.length}</span>
            <button disabled={!events?.hasMore} onClick={() => onPage(offset + limit)} type="button">下一页</button>
          </div>
        </>
      ) : status === "ready" ? (
        <p>暂无真实访问审计记录。</p>
      ) : null}
    </section>
  );
}

function AnalyticsPanelHeader({
  error,
  onRetry,
  status,
  title,
}: {
  error: string;
  onRetry: () => void;
  status: LinkAnalyticsStatus;
  title: string;
}) {
  return (
    <header className="link-management-analytics-header">
      <h4>{title}</h4>
      {status === "loading" ? <span>加载中</span> : null}
      {status === "forbidden" ? <span>无权限</span> : null}
      {status === "error" ? <button className="permission-admin-icon-button" onClick={onRetry} title={error || "重试"} type="button">重试</button> : null}
    </header>
  );
}

function LinkManagementState({ error, onRetry, status }: { error: string; onRetry: () => void; status: LinkManagementStatus }) {
  const label = status === "unconfigured"
    ? "当前环境未配置 API base URL，无法加载链接管理。"
    : status === "forbidden"
      ? "你没有权限查看链接管理。"
      : status === "loading"
        ? "正在加载链接管理列表。"
        : status === "idle"
          ? "等待 workspace 上下文。"
          : error || "链接管理加载失败。";

  return (
    <div className="permission-admin-empty-state">
      <AlertTriangle className="h-4 w-4" />
      <span>{label}</span>
      {status === "error" ? <button className="permission-admin-icon-button" onClick={onRetry} type="button">重试</button> : null}
    </div>
  );
}

function LinkManagementEmpty({ tab }: { tab: LinkManagementFilterTab }) {
  return (
    <div className="permission-admin-empty-state">
      <AlertTriangle className="h-4 w-4" />
      <span>{tab === "high-risk" ? "高风险检测依赖访问分析 API，本轮不伪造异常访问。" : "当前筛选没有返回链接。"}</span>
    </div>
  );
}

function DefinitionItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd title={value}>{value}</dd>
    </div>
  );
}

function StatusPill({ className = "", label }: { className?: string; label: string }) {
  return <span className={["permission-admin-status-pill", className].filter(Boolean).join(" ")}>{label}</span>;
}

async function copyShareUrl(link: LinkManagementDto, apiBaseUrl: string | null) {
  if (!apiBaseUrl) {
    throw new Error("当前环境未配置 API base URL。");
  }

  const response = await copyShareLinkManagementUrl(link.id, {
    copiedValueType: "share_url",
    reason: "Copied share URL from link management UI.",
  });
  const appOrigin = typeof window !== "undefined" ? window.location.origin : "";
  await navigator.clipboard?.writeText(toUserFacingShareUrl(response.url, null, link.audience, apiBaseUrl, appOrigin));
}

function formatRole(role: string) {
  if (role === "viewer") {
    return "可查看";
  }

  if (role === "commenter") {
    return "可评论";
  }

  return role;
}

function formatAudience(audience: string) {
  if (audience === "workspace") {
    return "工作区成员";
  }

  if (audience === "external") {
    return "外部访客";
  }

  if (audience === "public") {
    return "公开链接";
  }

  return audience;
}

function formatPolicyState(policyState: string | null | undefined, linkMode: string | null | undefined) {
  const suffix = linkMode ? ` / ${linkMode}` : "";
  if (policyState === "matching") {
    return `策略匹配${suffix}`;
  }

  if (policyState === "disabled") {
    return `策略已禁用${suffix}`;
  }

  if (policyState === "mismatch") {
    return `策略不匹配${suffix}`;
  }

  if (policyState === "missing") {
    return "策略缺失";
  }

  return policyState ? `${policyState}${suffix}` : "-";
}

function formatResourceType(resourceType: string) {
  return resourceType === "collection" ? "文件夹" : resourceType === "document" ? "文档" : resourceType;
}

function formatExpiry(expiresAt: string | null, revokedAt: string | null) {
  if (revokedAt) {
    return "已撤销";
  }

  return expiresAt ? formatDate(expiresAt) : "永不过期";
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString("zh-CN");
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString("zh-CN", {
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

function formatNullableDateTime(value: string | null | undefined) {
  return value ? formatDateTime(value) : "未访问";
}

function formatCount(value: number | null | undefined) {
  return new Intl.NumberFormat("zh-CN").format(value ?? 0);
}

function shortId(value: string) {
  if (!value) {
    return "-";
  }

  return value.length <= 12 ? value : `${value.slice(0, 8)}`;
}
