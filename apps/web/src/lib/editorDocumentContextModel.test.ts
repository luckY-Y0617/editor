import { describe, expect, test } from "../test/harness";
import {
  createEditorDocumentContextPanelModel,
  formatDocumentStatus,
} from "./editorDocumentContextModel";
import type { DocumentActivityResponse, DocumentContextResponse } from "./appApi";

const relatedDocumentId = "11111111-1111-4111-8111-111111111111";
const backlinkDocumentId = "22222222-2222-4222-8222-222222222222";

describe("editorDocumentContextModel", () => {
  test("maps document context and activity API responses into editor panel rows", () => {
    const model = createEditorDocumentContextPanelModel(createContext(), createActivity());

    expect(model.relatedDocuments).toEqual([
      {
        id: relatedDocumentId,
        code: "01.001",
        href: `#editor?documentId=${relatedDocumentId}`,
        title: "Mission",
      },
    ]);
    expect(model.versionTrail).toEqual([
      {
        id: "version-1",
        author: "Alice Kim",
        date: "May 14, 2024",
        status: "Published",
        version: "3.2",
      },
    ]);
    expect(model.backlinks).toEqual([
      {
        id: backlinkDocumentId,
        code: "02.001",
        excerpt: "Principle 1 informs the decision route.",
        href: `#editor?documentId=${backlinkDocumentId}`,
        title: "Decision Framework",
      },
    ]);
    expect(model.activity).toEqual([
      {
        actorName: "Alice Kim",
        actionLabel: "updated",
        documentTitle: "Mission",
        href: `#editor?documentId=${relatedDocumentId}`,
        id: "activity-1",
        date: "May 15, 2024",
        detail: "Alice Kim updated Mission.",
        title: "Mission",
      },
    ]);
  });

  test("keeps document activity readable when backend sends generic details", () => {
    const model = createEditorDocumentContextPanelModel(null, {
      items: [
        {
          actor: null,
          date: "2024-05-15T10:00:00.000Z",
          detail: "Updated content.",
          document: {
            id: relatedDocumentId,
            title: "Mission",
          },
          id: "activity-generic",
          title: "document.updated",
        },
      ],
    });

    expect(model.activity[0]).toMatchObject({
      actorName: "Unknown user",
      detail: "Unknown user updated Mission.",
      documentTitle: "Mission",
      href: `#editor?documentId=${relatedDocumentId}`,
    });
  });

  test("groups repeated generic document update rows for display", () => {
    const model = createEditorDocumentContextPanelModel(null, {
      items: Array.from({ length: 4 }, (_, index) => ({
        actor: {
          id: "alice",
          name: "Alice Kim",
        },
        date: `2024-05-15T10:0${3 - index}:00.000Z`,
        detail: "Updated content.",
        document: {
          id: relatedDocumentId,
          title: "Mission",
        },
        id: `activity-update-${index}`,
        title: "document.updated",
      })),
    });

    expect(model.activity.length).toBe(1);
    expect(model.activity[0]).toMatchObject({
      actorName: "Alice Kim",
      detail: "Alice Kim updated Mission 4 times. 4 updates grouped.",
      documentTitle: "Mission",
      href: `#editor?documentId=${relatedDocumentId}`,
      id: "activity-update-0:grouped-4",
    });
  });

  test("does not group comment or access activity into document update noise", () => {
    const model = createEditorDocumentContextPanelModel(null, {
      items: [
        {
          actor: {
            id: "alice",
            name: "Alice Kim",
          },
          date: "2024-05-15T10:03:00.000Z",
          detail: "Updated content.",
          document: {
            id: relatedDocumentId,
            title: "Mission",
          },
          id: "activity-update-1",
          title: "document.updated",
        },
        {
          actor: {
            id: "alice",
            name: "Alice Kim",
          },
          date: "2024-05-15T10:02:00.000Z",
          detail: "Comment created.",
          document: {
            id: relatedDocumentId,
            title: "Mission",
          },
          id: "activity-comment",
          title: "comment.created",
        },
        {
          actor: {
            id: "alice",
            name: "Alice Kim",
          },
          date: "2024-05-15T10:01:00.000Z",
          detail: "Updated content.",
          document: {
            id: relatedDocumentId,
            title: "Mission",
          },
          id: "activity-update-2",
          title: "document.updated",
        },
      ],
    });

    expect(model.activity.map((row) => row.id)).toEqual([
      "activity-update-1",
      "activity-comment",
      "activity-update-2",
    ]);
  });

  test("normalizes backend status values without changing API contract fields", () => {
    expect(formatDocumentStatus("in_review")).toBe("In Review");
    expect(formatDocumentStatus("published")).toBe("Published");
    expect(formatDocumentStatus(null)).toBe("Draft");
  });
});

function createContext(): DocumentContextResponse {
  return {
    backlinks: [
      {
        code: "02.001",
        excerpt: "Principle 1 informs the decision route.",
        id: backlinkDocumentId,
        title: "Decision Framework",
      },
    ],
    relatedDocuments: [
      {
        code: "01.001",
        id: relatedDocumentId,
        title: "Mission",
      },
    ],
    versionTrail: [
      {
        author: "Alice Kim",
        date: "2024-05-14T10:00:00.000Z",
        id: "version-1",
        status: "published",
        version: "3.2",
      },
    ],
  };
}

function createActivity(): DocumentActivityResponse {
  return {
    items: [
      {
        actor: {
          id: "alice",
          name: "Alice Kim",
        },
        date: "2024-05-15T10:00:00.000Z",
        detail: "Updated content.",
        document: {
          id: relatedDocumentId,
          title: "Mission",
        },
        id: "activity-1",
        title: "Document updated",
      },
    ],
  };
}
