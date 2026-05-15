import {
  buildShareLinkAccessEventsPath,
  buildShareLinkAccessStatsPath,
  buildShareLinkCopyPath,
  buildShareLinkManagementDetailPath,
  buildShareLinkManagementListPath,
  type LinkManagementDto,
} from "./appApi";
import {
  filterLinksByTab,
  getAccessEventDisplayRows,
  getCopyShareLinkLabel,
  getLinkManagementActionState,
  getLinkManagementDisplay,
  getSourceBreakdownRows,
  getTrendTotals,
  hasForbiddenSecretFields,
  prepareLinkManagementPatch,
} from "./linkManagementModel";
import { toUserFacingShareUrl } from "./publicShareModel";
import { describe, expect, test } from "../test/harness";

const now = new Date("2026-05-14T00:00:00.000Z");

describe("linkManagementModel", () => {
  test("models LinkManagementDto without token, token hash, or password hash fields", () => {
    const link = createLink();
    expect(hasForbiddenSecretFields(link as unknown as Record<string, unknown>)).toBe(false);
    expect(Object.keys(link).sort()).toEqual([
      "audience",
      "accessCount",
      "canManage",
      "canPause",
      "canRevoke",
      "canUpdate",
      "createdAt",
      "createdBy",
      "createdByDisplayName",
      "expiresAt",
      "externalOrPublicAccessCount",
      "hasPassword",
      "id",
      "lastAccessedAt",
      "linkMode",
      "pausedAt",
      "pausedBy",
      "pauseReason",
      "policyState",
      "recentFailCount",
      "resourceId",
      "resourceTitle",
      "resourceType",
      "revokedAt",
      "roleKey",
      "status",
      "subjectEmail",
      "uniqueVisitorCount",
      "workspaceId",
    ].sort());
  });

  test("derives normal, expiring, attention, and high risk from real metadata", () => {
    expect(getLinkManagementDisplay(createLink(), now)).toMatchObject({
      riskLabel: "正常",
      statusLabel: "活跃",
    });
    expect(getLinkManagementDisplay(createLink({ expiresAt: "2026-05-20T00:00:00.000Z" }), now)).toMatchObject({
      riskLabel: "即将过期",
      statusLabel: "活跃",
    });
    expect(getLinkManagementDisplay(createLink({ recentFailCount: 1 }), now)).toMatchObject({
      riskLabel: "需关注",
      statusLabel: "活跃",
    });
    expect(getLinkManagementDisplay(createLink({ audience: "public", hasPassword: false }), now)).toMatchObject({
      riskLabel: "需关注",
      statusLabel: "活跃",
    });
    expect(getLinkManagementDisplay(createLink({ recentFailCount: 5 }), now)).toMatchObject({
      riskLabel: "高风险",
      statusLabel: "活跃",
    });
    expect(getLinkManagementDisplay(createLink({ accessCount: 50, audience: "public", externalOrPublicAccessCount: 45 }), now)).toMatchObject({
      riskLabel: "高风险",
      statusLabel: "活跃",
    });
    expect(getLinkManagementDisplay(createLink({ expiresAt: "2026-05-01T00:00:00.000Z" }), now)).toMatchObject({
      riskLabel: "-",
      statusLabel: "已过期",
    });
    expect(getLinkManagementDisplay(createLink({ revokedAt: "2026-05-13T00:00:00.000Z" }), now)).toMatchObject({
      riskLabel: "-",
      statusLabel: "已撤销",
    });
  });

  test("high-risk tab filters only high risk links without mock rows", () => {
    const normal = createLink({ id: "normal" });
    const highByFailure = createLink({ id: "high-fail", recentFailCount: 5 });
    const highByPublicAccess = createLink({ id: "high-public", accessCount: 60, audience: "public", externalOrPublicAccessCount: 55 });

    expect(filterLinksByTab([normal, highByFailure, highByPublicAccess], "high-risk", now).map((link) => link.id)).toEqual([
      "high-fail",
      "high-public",
    ]);
  });

  test("keeps public link role UI viewer-only and requires future expiry", () => {
    const publicLink = createLink({ audience: "public", roleKey: "commenter" });
    const invalid = prepareLinkManagementPatch(publicLink, { expiresAt: "", roleKey: "commenter" }, now);
    expect(invalid.isValid).toBe(false);
    expect(invalid.request.roleKey).toBe("viewer");
    expect(invalid.errors.expiresAt).toBe("公开链接必须设置未来过期时间。");

    const valid = prepareLinkManagementPatch(publicLink, { expiresAt: "2026-06-01T00:00", roleKey: "commenter" }, now);
    expect(valid.isValid).toBe(true);
    expect(valid.request.roleKey).toBe("viewer");
    expect(Date.parse(valid.request.expiresAt ?? "") > now.getTime()).toBe(true);
  });

  test("copy action copy says access link without exposing token wording", () => {
    const label = getCopyShareLinkLabel();
    expect(label).toBe("Audited copy link");
    expect(label.toLowerCase().includes("audit")).toBe(true);
    expect(label.includes("ID")).toBe(false);
    expect(label.toLowerCase().includes("token")).toBe(false);
    expect(label.toLowerCase().includes("reveal")).toBe(false);
  });

  test("existing-link copy converts audited copy endpoint response without metadata reconstruction", () => {
    const link = createLink({
      audience: "public",
      id: "link-metadata-id",
      resourceId: "resource-metadata-id",
    });

    expect(
      toUserFacingShareUrl(
        "/api/v1/share-links/copy-response-token/resolve",
        null,
        link.audience,
        "https://api.example.com/api/v1",
        "https://app.example.com",
      ),
    ).toBe("https://app.example.com/#public/share-links/copy-response-token");
  });

  test("disables revoked, expired, and paused state buttons correctly", () => {
    const revoked = getLinkManagementActionState(createLink({ revokedAt: "2026-05-13T00:00:00.000Z", status: "revoked" }), null);
    expect(revoked.canRevoke).toBe(false);
    expect(revoked.canResume).toBe(false);

    const expired = getLinkManagementActionState(createLink({ expiresAt: "2026-05-01T00:00:00.000Z", status: "expired" }), null);
    expect(expired.canPause).toBe(false);
    expect(expired.canResume).toBe(false);

    const paused = getLinkManagementActionState(createLink({ pausedAt: "2026-05-13T00:00:00.000Z", status: "paused" }), null);
    expect(paused.canPause).toBe(false);
    expect(paused.canResume).toBe(true);
    expect(paused.pauseLabel).toBe("恢复");

    const policyPaused = getLinkManagementActionState(createLink({ status: "policy_paused" }), null);
    expect(policyPaused.canPause).toBe(false);
    expect(policyPaused.canResume).toBe(false);
    expect(policyPaused.disabledReason).toContain("linkMode");
  });

  test("builds API client routes with /api/v1 relative permission paths", () => {
    expect(buildShareLinkManagementListPath({
      audience: "workspace",
      limit: 20,
      offset: 40,
      q: "demo",
      roleKey: "viewer",
      status: "active",
      workspaceId: "00000000-0000-0000-0000-000000000001",
    })).toBe("/permissions/share-links?workspaceId=00000000-0000-0000-0000-000000000001&audience=workspace&roleKey=viewer&status=active&q=demo&offset=40&limit=20");
    expect(buildShareLinkManagementDetailPath("abc 123")).toBe("/permissions/share-links/abc%20123");
    expect(buildShareLinkAccessStatsPath("abc 123")).toBe("/permissions/share-links/abc%20123/stats");
    expect(buildShareLinkAccessEventsPath("abc 123", { eventType: "resolve", limit: 10, offset: 20, result: "fail" })).toBe(
      "/permissions/share-links/abc%20123/access-events?offset=20&limit=10&result=fail&eventType=resolve",
    );
    expect(buildShareLinkCopyPath("abc 123")).toBe("/permissions/share-links/abc%20123/copy");
  });

  test("stats and events helpers handle empty data without fabricated rows", () => {
    expect(getTrendTotals([])).toEqual({ failCount: 0, successCount: 0, totalCount: 0 });
    expect(getSourceBreakdownRows([])).toEqual([]);
    expect(getAccessEventDisplayRows([])).toEqual([]);
  });

  test("stats and access event DTOs do not carry token or password material", () => {
    const stats = {
      accessCount: 1,
      lastAccessedAt: "2026-05-14T00:00:00.000Z",
      lastAccessIp: "127.0.0.1",
      recentWindowDays: 7,
      shareLinkId: "link-1",
      sourceBreakdown: [],
      trend: [],
      uniqueVisitorCount: 1,
    };
    const event = {
      accessedAt: "2026-05-14T00:00:00.000Z",
      accessedBy: null,
      actorDisplayName: null,
      actorType: "anonymous",
      actorUserId: null,
      deviceSummary: null,
      eventType: "resolve",
      failureCategory: null,
      id: "event-1",
      ip: "127.0.0.1",
      occurredAt: "2026-05-14T00:00:00.000Z",
      result: "success",
      shareLinkId: "link-1",
      userAgent: null,
    };

    expect(hasForbiddenSecretFields(stats)).toBe(false);
    expect(hasForbiddenSecretFields(event)).toBe(false);
  });
});

function createLink(overrides: Partial<LinkManagementDto> = {}): LinkManagementDto {
  return {
    audience: "workspace",
    accessCount: 0,
    canManage: true,
    canPause: true,
    canRevoke: true,
    canUpdate: true,
    createdAt: "2026-05-14T00:00:00.000Z",
    createdBy: "user-1",
    createdByDisplayName: "Northstar Owner",
    expiresAt: null,
    externalOrPublicAccessCount: 0,
    hasPassword: false,
    id: "90c43390-0000-0000-0000-000000000001",
    lastAccessedAt: null,
    linkMode: "internal",
    pausedAt: null,
    pausedBy: null,
    pauseReason: null,
    policyState: "enabled",
    recentFailCount: 0,
    resourceId: "11111111-1111-1111-1111-111111111111",
    resourceTitle: "Demo",
    resourceType: "document",
    revokedAt: null,
    roleKey: "viewer",
    status: "active",
    subjectEmail: null,
    uniqueVisitorCount: 0,
    workspaceId: "00000000-0000-0000-0000-000000000001",
    ...overrides,
  };
}
