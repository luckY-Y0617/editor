import { describe, expect, test } from "../test/harness";
import type { BootstrapResponse, WorkspaceAgendaResponse } from "./appApi";
import { createHomeQuickActions, createLiveWorkspaceHomeModel } from "./workspaceHomeModel";

const activeDocumentId = "11111111-1111-4111-8111-111111111111";
const olderDocumentId = "22222222-2222-4222-8222-222222222222";
const foundationsFolderId = "33333333-3333-4333-8333-333333333333";
const guidesFolderId = "44444444-4444-4444-8444-444444444444";
const activeLibraryId = "55555555-5555-4555-8555-555555555555";

describe("workspaceHomeModel", () => {
  test("maps bootstrap DTOs into live Home rows and counts", () => {
    const model = createLiveWorkspaceHomeModel(createBootstrap(), {
      activityItems: [
        {
          actor: {
            id: "user-1",
            name: "Alice Kim",
          },
          date: "2024-02-04T10:00:00.000Z",
          detail: "Mission was updated.",
          document: {
            id: activeDocumentId,
            title: "Mission",
          },
          id: "activity-1",
          title: "document.updated",
        },
      ],
      members: [
        {
          displayName: "Alice Kim",
          email: "alice@example.com",
          joinedAt: "2024-01-01T00:00:00.000Z",
          role: "owner",
          status: "active",
          userId: "user-1",
        },
      ],
      notifications: [
        {
          actionUrl: "#share",
          body: "Access request approved.",
          createdAt: "2024-02-05T10:00:00.000Z",
          id: "notification-1",
          readAt: null,
          recipientUserId: "user-1",
          title: "Access approved",
          type: "access_request.approved",
          workspaceId: "workspace-1",
        },
        {
          actionUrl: "#updates",
          body: "Grant expires soon.",
          createdAt: "2024-02-06T10:00:00.000Z",
          id: "notification-2",
          readAt: null,
          recipientUserId: "user-1",
          title: "Grant expiring",
          type: "permission.grant_expiring",
          workspaceId: "workspace-1",
        },
      ],
    });

    expect(model.mode).toBe("live");
    expect(model.workspaceName).toBe("Atlas Workspace");
    expect(model.documentsLabel).toBe("2");
    expect(model.collectionsLabel).toBe("2");
    expect(model.librariesLabel).toBe("1");
    expect(model.activeLibraryName).toBe("Operations");
    expect(model.activeLibraryHref).toBe(`#libraries?libraryId=${activeLibraryId}`);
    expect(model.activeDocumentHref).toBe(`#editor?documentId=${activeDocumentId}`);
    expect(model.documentRows.map((row) => row.id)).toEqual([activeDocumentId, olderDocumentId]);
    expect(model.documentRows[0]).toMatchObject({
      folderTitle: "Foundations",
      href: `#editor?documentId=${activeDocumentId}`,
      status: "Draft",
      title: "Mission",
    });
    expect(model.collections.map((collection) => collection.displayTitle)).toEqual(["Foundations", "Guides"]);
    expect(model.collections[0].href).toBe(`#libraries?libraryId=${activeLibraryId}&collectionId=${foundationsFolderId}`);
    expect(model.activityRows[0]).toMatchObject({
      actorName: "Alice Kim",
      detail: "Alice Kim updated this document.",
      href: `#editor?documentId=${activeDocumentId}`,
      title: "Mission",
    });
    expect(model.contributorRows[0]).toMatchObject({
      initials: "AK",
      name: "Alice Kim",
      role: "owner",
    });
    expect(model.updateRows[0]).toMatchObject({
      href: "#settings?scope=workspace&tab=permissions",
      title: "Access approved",
      type: "access_request.approved",
    });
    expect(model.agendaRows).toEqual([]);
    expect(model.waitingRows.map((row) => row.title)).toEqual(["Access approved", "Grant expiring"]);
    expect(model.signalRows).toEqual([
      {
        detail: "drafts in current workspace",
        href: "#libraries",
        id: "signal-drafts",
        label: "draft documents",
        source: "live",
        value: "1",
      },
      {
        detail: "unread workspace notifications",
        href: "#updates",
        id: "signal-unread",
        label: "notifications",
        source: "live",
        value: "2",
      },
      {
        detail: "from permission workflow",
        href: "#updates?tab=access",
        id: "signal-access-requests",
        label: "access requests",
        source: "live",
        value: "1",
      },
    ]);
    expect(model.digestRows.map((row) => row.value)).toEqual(["2", "1", "1"]);
    expect(model.conversationRows[0]).toMatchObject({
      kind: "activity",
      source: "live",
      title: "Mission",
    });
    expect(model.recentDecisionRows.map((row) => row.title)).toEqual(["Access approved", "Grant expiring"]);
    expect(model.insightRows.map((row) => row.id)).toEqual([
      "total-documents",
      "updated-30-days",
      "draft-documents",
      "published-documents",
    ]);
  });

  test("keeps activity readable when older events do not have an actor", () => {
    const model = createLiveWorkspaceHomeModel(createBootstrap(), {
      activityItems: [
        {
          date: "2024-02-04T10:00:00.000Z",
          detail: "Updated content.",
          document: {
            id: activeDocumentId,
            title: "Mission",
          },
          id: "activity-no-actor",
          title: "document.updated",
        },
      ],
    });

    expect(model.activityRows[0]).toMatchObject({
      actorName: undefined,
      detail: "Updated document content.",
      href: `#editor?documentId=${activeDocumentId}`,
      title: "Mission",
    });
  });

  test("groups repeated document update activity rows for display", () => {
    const model = createLiveWorkspaceHomeModel(createBootstrap(), {
      activityItems: Array.from({ length: 6 }, (_, index) => ({
        actor: {
          id: "user-1",
          name: "Alice Kim",
        },
        date: `2024-02-04T10:0${5 - index}:00.000Z`,
        detail: "Updated content.",
        document: {
          id: activeDocumentId,
          title: "Mission",
        },
        id: `activity-update-${index}`,
        title: "document.updated",
      })),
    });

    expect(model.activityRows.length).toBe(1);
    expect(model.activityRows[0]).toMatchObject({
      actorName: "Alice Kim",
      detail: "Alice Kim updated Mission 6 times. 6 updates grouped.",
      href: `#editor?documentId=${activeDocumentId}`,
      id: "activity-update-0:grouped-6",
      title: "Mission",
    });
  });

  test("does not group high-signal or separated activity rows", () => {
    const model = createLiveWorkspaceHomeModel(createBootstrap(), {
      activityItems: [
        {
          actor: {
            id: "user-1",
            name: "Alice Kim",
          },
          date: "2024-02-04T10:05:00.000Z",
          detail: "Updated content.",
          document: {
            id: activeDocumentId,
            title: "Mission",
          },
          id: "activity-update-1",
          title: "document.updated",
        },
        {
          actor: {
            id: "user-1",
            name: "Alice Kim",
          },
          date: "2024-02-04T10:04:00.000Z",
          detail: "Created document.",
          document: {
            id: activeDocumentId,
            title: "Mission",
          },
          id: "activity-created",
          title: "document.created",
        },
        {
          actor: {
            id: "user-1",
            name: "Alice Kim",
          },
          date: "2024-02-04T10:03:00.000Z",
          detail: "Updated content.",
          document: {
            id: activeDocumentId,
            title: "Mission",
          },
          id: "activity-update-2",
          title: "document.updated",
        },
      ],
    });

    expect(model.activityRows.map((row) => row.id)).toEqual([
      "activity-update-1",
      "activity-created",
      "activity-update-2",
    ]);
  });

  test("maps workspace agenda items to document links", () => {
    const model = createLiveWorkspaceHomeModel(createBootstrap(), {
      agenda: createAgenda(),
    });

    expect(model.agendaRows[0]).toMatchObject({
      detail: "30 minutes",
      href: `#editor?documentId=${activeDocumentId}`,
      meta: "Foundations",
      time: "09:00",
      title: "Mission",
    });
  });

  test("falls unsafe update action urls back to workspace updates", () => {
    const model = createLiveWorkspaceHomeModel(createBootstrap(), {
      notifications: [
        {
          actionUrl: "javascript:alert(1)",
          body: null,
          createdAt: "2024-02-05T10:00:00.000Z",
          id: "notification-unsafe",
          readAt: null,
          recipientUserId: "user-1",
          title: "Unsafe URL",
          type: "access_request.created",
          workspaceId: "workspace-1",
        },
      ],
    });

    expect(model.updateRows[0].href).toBe("#updates");
  });

  test("marks unsupported Home quick actions as unavailable", () => {
    const model = createLiveWorkspaceHomeModel(createBootstrap());
    const actions = createHomeQuickActions(model);

    expect(actions.find((action) => action.id === "new-document")).toMatchObject({
      href: `#libraries?libraryId=${activeLibraryId}`,
      isEnabled: true,
    });
    expect(actions.find((action) => action.id === "new-decision")).toMatchObject({
      disabledReason: "Decision workflow is not supported by the current API.",
      isEnabled: false,
    });
    expect(actions.find((action) => action.id === "request-access")?.isEnabled).toBe(false);
    expect(actions.find((action) => action.id === "more-actions")).toMatchObject({
      href: "#updates",
      isEnabled: true,
    });
  });
});

function createBootstrap(): BootstrapResponse {
  return {
    activeDocumentId,
    activeSpaceId: activeLibraryId,
    documents: [
      {
        folderId: guidesFolderId,
        id: olderDocumentId,
        sortOrder: 2,
        status: "Published",
        tags: ["Guide"],
        title: "Handbook",
        updatedAt: "2024-01-02T10:00:00.000Z",
      },
      {
        folderId: foundationsFolderId,
        id: activeDocumentId,
        sortOrder: 1,
        status: "Draft",
        tags: ["Strategy"],
        title: "Mission",
        updatedAt: "2024-02-03T10:00:00.000Z",
      },
    ],
    folders: [
      {
        documentCount: 1,
        id: guidesFolderId,
        sortOrder: 2,
        title: "02. Guides",
      },
      {
        documentCount: 1,
        id: foundationsFolderId,
        sortOrder: 1,
        title: "01. Foundations",
      },
    ],
    spaces: [{ id: activeLibraryId, name: "Operations" }],
    workspace: {
      currentSpaceId: activeLibraryId,
      id: "workspace-1",
      name: "Atlas Workspace",
      organizationId: "organization-1",
    },
  };
}

function createAgenda(): WorkspaceAgendaResponse {
  return {
    calendarStatus: "workspace",
    date: "2026-05-07",
    today: [
      {
        actionUrl: null,
        calendarStatus: "workspace",
        category: "Foundations",
        connectedToCalendar: false,
        date: "2026-05-07",
        detail: "30 minutes",
        durationMinutes: 30,
        endTime: "09:30",
        id: "agenda-1",
        kind: "document",
        resourceId: activeDocumentId,
        resourceType: "document",
        startTime: "09:00",
        title: "Mission",
      },
    ],
    upcoming: [],
    workspaceId: "workspace-1",
  };
}
