import { describe, expect, test } from "../test/harness";
import type { PermissionNotificationDto } from "./appApi";
import {
  filterWorkspaceNotifications,
  getNotificationActionHref,
  getNotificationKind,
  getNotificationPreferenceStatusLabel,
  getNotificationStatusLabel,
  getWorkspaceUpdatesTabLabel,
  toNotificationPreferenceResourceRows,
  toWorkspaceNotification,
} from "./workspaceUpdatesModel";

const documentId = "11111111-1111-4111-8111-111111111111";
const collectionId = "22222222-2222-4222-8222-222222222222";

describe("workspaceUpdatesModel", () => {
  test("classifies notification types users can scan", () => {
    expect(getNotificationKind("comment.created")).toBe("comment");
    expect(getNotificationKind("mention.created")).toBe("mention");
    expect(getNotificationKind("document.updated")).toBe("document");
    expect(getNotificationKind("access_request.created")).toBe("permission");
    expect(getNotificationKind("unknown")).toBe("system");
  });

  test("normalizes safe notification targets", () => {
    expect(getNotificationActionHref(createNotification({ actionUrl: `#editor?documentId=${documentId}` }))).toBe(
      `#editor?documentId=${documentId}`,
    );
    expect(getNotificationActionHref(createNotification({ actionUrl: "javascript:alert(1)", resourceId: documentId }))).toBe(
      `#editor?documentId=${documentId}`,
    );
    expect(
      getNotificationActionHref(createNotification({ resourceId: collectionId, resourceType: "collection" })),
    ).toBe(`#libraries?collectionId=${collectionId}`);
  });

  test("maps live DTOs into update rows", () => {
    const row = toWorkspaceNotification(createNotification({ readAt: null, type: "access_request.created" }));

    expect(row).toMatchObject({
      actionHref: "#permissions",
      actionLabel: "Review",
      kind: "permission",
      subject: "Access requested",
      unread: true,
    });
  });

  test("filters live notifications by user-facing tabs", () => {
    const notifications = [
      createNotification({ id: "read-comment", readAt: "2024-02-02T00:00:00.000Z", type: "comment.created" }),
      createNotification({ id: "unread-mention", readAt: null, type: "mention.created" }),
      createNotification({ id: "access", type: "access_request.created" }),
      createNotification({ id: "document", type: "document.updated" }),
    ];

    expect(filterWorkspaceNotifications(notifications, "unread").map((notification) => notification.id)).toEqual([
      "unread-mention",
    ]);
    expect(filterWorkspaceNotifications(notifications, "comments").map((notification) => notification.id)).toEqual([
      "read-comment",
    ]);
    expect(filterWorkspaceNotifications(notifications, "access").map((notification) => notification.id)).toEqual([
      "access",
    ]);
    expect(getWorkspaceUpdatesTabLabel("documents")).toBe("Document changes");
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
        label: "Collection 22222222...2222",
        resourceType: "collection",
        state: "muted",
        updatedAt: "2024-02-03T00:00:00.000Z",
      },
    ]);
  });

  test("labels unavailable notification states honestly", () => {
    expect(getNotificationStatusLabel("unconfigured")).toContain("Demo");
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
