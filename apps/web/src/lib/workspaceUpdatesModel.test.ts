import { describe, expect, test } from "../test/harness";
import type { PermissionNotificationDto } from "./appApi";
import {
  filterWorkspaceNotifications,
  getNotificationActionHref,
  getNotificationKind,
  getNotificationPreferenceStatusLabel,
  getNotificationStatusLabel,
  getWorkspaceUpdatesTabFromHash,
  getWorkspaceUpdatesTabLabel,
  toNotificationPreferenceResourceRows,
  toWorkspaceNotification,
} from "./workspaceUpdatesModel";

const documentId = "11111111-1111-4111-8111-111111111111";
const collectionId = "22222222-2222-4222-8222-222222222222";

describe("workspaceUpdatesModel", () => {
  test("classifies notification types users can scan", () => {
    expect(getNotificationKind("access_request.created")).toBe("access");
    expect(getNotificationKind("permission.grant_created")).toBe("grant");
    expect(getNotificationKind("group.member_added")).toBe("grant");
    expect(getNotificationKind("share_link.created")).toBe("sharing");
    expect(getNotificationKind("email_invite.revoked")).toBe("sharing");
    expect(getNotificationKind("permission.grant_expiring")).toBe("expiry");
    expect(getNotificationKind("group.member_expired")).toBe("expiry");
    expect(getNotificationKind("unknown")).toBe("permission");
  });

  test("normalizes safe notification targets", () => {
    expect(getNotificationActionHref(createNotification({ actionUrl: `#editor?documentId=${documentId}` }))).toBe(
      `#editor?documentId=${documentId}`,
    );
    expect(getNotificationActionHref(createNotification({ actionUrl: "javascript:alert(1)", resourceId: documentId }))).toBe(
      `#share?documentId=${documentId}`,
    );
    expect(
      getNotificationActionHref(createNotification({ resourceId: collectionId, resourceType: "collection" })),
    ).toBe(`#libraries?collectionId=${collectionId}`);
    expect(getNotificationActionHref(createNotification({ resourceId: documentId, type: "permission.grant_created" }))).toBe(
      `#share?documentId=${documentId}`,
    );
    expect(getNotificationActionHref(createNotification({ type: "permission.grant_created" }))).toBe(
      "#settings?scope=workspace&tab=permissions",
    );
  });

  test("maps live DTOs into update rows", () => {
    const row = toWorkspaceNotification(createNotification({ readAt: null, type: "access_request.created" }));

    expect(row).toMatchObject({
      actionHref: "#settings?scope=workspace&tab=permissions",
      actionLabel: "Review",
      kind: "access",
      messagePrefix: "requested access",
      subject: "Access requested",
      unread: true,
    });
  });

  test("filters live notifications by user-facing tabs", () => {
    const notifications = [
      createNotification({ id: "access", type: "access_request.created" }),
      createNotification({ id: "grant", type: "permission.grant_updated" }),
      createNotification({ id: "sharing", type: "email_invite.created" }),
      createNotification({ id: "expiry", readAt: null, type: "permission.grant_expiring" }),
    ];

    expect(filterWorkspaceNotifications(notifications, "unread").map((notification) => notification.id)).toEqual([
      "expiry",
    ]);
    expect(filterWorkspaceNotifications(notifications, "access").map((notification) => notification.id)).toEqual([
      "access",
    ]);
    expect(filterWorkspaceNotifications(notifications, "grants").map((notification) => notification.id)).toEqual([
      "grant",
    ]);
    expect(filterWorkspaceNotifications(notifications, "sharing").map((notification) => notification.id)).toEqual([
      "sharing",
    ]);
    expect(filterWorkspaceNotifications(notifications, "expiry").map((notification) => notification.id)).toEqual([
      "expiry",
    ]);
    expect(getWorkspaceUpdatesTabLabel("grants")).toBe("Grants & groups");
    expect(getWorkspaceUpdatesTabFromHash("#updates?tab=access")).toBe("access");
    expect(getWorkspaceUpdatesTabFromHash("#notifications?tab=sharing")).toBe("sharing");
    expect(getWorkspaceUpdatesTabFromHash("#notifications?tab=documents")).toBe("all");
    expect(getWorkspaceUpdatesTabFromHash("#updates?tab=unknown")).toBe("all");
  });

  test("maps live watched and muted preference resources", () => {
    const rows = toNotificationPreferenceResourceRows([
      createPreference({ id: "pref-doc", resourceId: documentId, resourceType: "document", watched: true }),
      createPreference({ id: "pref-collection", resourceId: collectionId, resourceType: "collection", muted: true }),
      createPreference({ id: "pref-empty", watched: false, muted: false }),
    ]);

    expect(rows).toEqual([
      {
        href: `#editor?documentId=${documentId}`,
        id: "pref-doc",
        label: "Document 11111111...1111",
        resourceType: "document",
        state: "watched",
        updatedAt: "2024-02-03T00:00:00.000Z",
      },
      {
        href: `#libraries?collectionId=${collectionId}`,
        id: "pref-collection",
        label: "Folder 22222222...2222",
        resourceType: "collection",
        state: "muted",
        updatedAt: "2024-02-03T00:00:00.000Z",
      },
    ]);
  });

  test("labels unavailable notification states honestly", () => {
    expect(getNotificationStatusLabel("unconfigured")).toContain("access & sharing");
    expect(getNotificationStatusLabel("error")).toBe("Notification API unavailable");
    expect(getNotificationPreferenceStatusLabel("error")).toBe("Notification preferences API unavailable");
  });
});

function createNotification(overrides: Partial<PermissionNotificationDto> = {}): PermissionNotificationDto {
  return {
    accessRequestId: null,
    actionUrl: null,
    actorUserId: null,
    body: "Needs review",
    createdAt: "2024-02-01T00:00:00.000Z",
    id: "notification-1",
    permissionGrantId: null,
    readAt: "2024-02-02T00:00:00.000Z",
    recipientUserId: "user-1",
    resourceId: null,
    resourceType: "document",
    title: "Access requested",
    type: "permission.grant_created",
    workspaceId: "workspace-1",
    ...overrides,
  };
}

function createPreference(overrides: Partial<import("./appApi").PermissionNotificationPreferenceDto> = {}) {
  return {
    createdAt: "2024-02-01T00:00:00.000Z",
    id: "preference-1",
    muted: false,
    resourceId: null,
    resourceType: null,
    updatedAt: "2024-02-03T00:00:00.000Z",
    userId: "user-1",
    watched: false,
    workspaceId: "workspace-1",
    ...overrides,
  };
}
