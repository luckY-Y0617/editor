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
        id: "activity-1",
        date: "May 15, 2024",
        detail: "Alice Kim updated this document.",
        title: "Document updated",
      },
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
        date: "2024-05-15T10:00:00.000Z",
        detail: "Alice Kim updated this document.",
        id: "activity-1",
        title: "Document updated",
      },
    ],
  };
}
