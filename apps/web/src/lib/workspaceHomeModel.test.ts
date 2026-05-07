import { describe, expect, test } from "../test/harness";
import type { BootstrapResponse } from "./appApi";
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
          date: "2024-02-04T10:00:00.000Z",
          detail: "Mission was updated.",
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
      href: `#editor?documentId=${activeDocumentId}`,
      title: "document.updated",
    });
    expect(model.contributorRows[0]).toMatchObject({
      initials: "AK",
      name: "Alice Kim",
      role: "owner",
    });
    expect(model.updateRows[0]).toMatchObject({
      href: "#share",
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
        href: "#share",
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
      title: "document.updated",
    });
    expect(model.recentDecisionRows.map((row) => row.title)).toEqual(["Access approved", "Grant expiring"]);
    expect(model.insightRows.map((row) => row.id)).toEqual([
      "total-documents",
      "updated-30-days",
      "draft-documents",
      "published-documents",
    ]);
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
