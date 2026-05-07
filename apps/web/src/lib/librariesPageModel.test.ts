import { describe, expect, test } from "../test/harness";
import type { BootstrapResponse, KnowledgeMapResponse } from "./appApi";
import {
  createCollectionReorderIds,
  createLibrariesPageModel,
  getCollectionIdAfterDelete,
  getPreferredLibraryId,
} from "./librariesPageModel";

const activeLibraryId = "11111111-1111-4111-8111-111111111111";
const secondLibraryId = "22222222-2222-4222-8222-222222222222";
const selectedCollectionId = "33333333-3333-4333-8333-333333333333";
const otherCollectionId = "44444444-4444-4444-8444-444444444444";
const selectedDocumentId = "55555555-5555-4555-8555-555555555555";
const otherDocumentId = "66666666-6666-4666-8666-666666666666";
const archivedDocumentId = "77777777-7777-4777-8777-777777777777";
const reviewDocumentId = "99999999-9999-4999-8999-999999999999";

describe("librariesPageModel", () => {
  test("selects the requested library and filters documents by collection", () => {
    const model = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: selectedCollectionId,
      libraryId: secondLibraryId,
    });

    expect(model.workspaceName).toBe("Atlas Workspace");
    expect(model.activeLibraryId).toBe(secondLibraryId);
    expect(model.activeLibraryName).toBe("Research");
    expect(model.activeCollectionTitle).toBe("Foundations");
    expect(model.libraries.map((library) => library.href)).toEqual([
      `#libraries?libraryId=${activeLibraryId}`,
      `#libraries?libraryId=${secondLibraryId}`,
    ]);
    expect(model.collections[0]).toMatchObject({
      canDelete: false,
      href: `#libraries?libraryId=${secondLibraryId}&collectionId=${selectedCollectionId}`,
      isActive: true,
      sortOrder: 1,
      title: "Foundations",
    });
    expect(model.documents.map((document) => document.id)).toEqual([reviewDocumentId, selectedDocumentId]);
    expect(model.documents.find((document) => document.id === selectedDocumentId)).toMatchObject({
      canArchive: true,
      canRestore: false,
      collectionTitle: "Foundations",
      href: `#editor?documentId=${selectedDocumentId}`,
      isArchived: false,
      title: "Mission",
    });
    expect(model.canCreateDocument).toBe(true);
    expect(model.canCreateCollection).toBe(true);
    expect(model.canRenameActiveCollection).toBe(true);
    expect(model.canDeleteActiveCollection).toBe(false);
    expect(model.canReorderActiveCollectionUp).toBe(false);
    expect(model.canReorderActiveCollectionDown).toBe(true);
    expect(model.createDocumentDisabledReason).toBe(null);
  });

  test("falls back to active library and all documents when filters are unavailable", () => {
    const model = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      libraryId: "88888888-8888-4888-8888-888888888888",
    });

    expect(model.activeLibraryId).toBe(activeLibraryId);
    expect(model.activeCollectionId).toBe(null);
    expect(model.documents.map((document) => document.id)).toEqual([
      reviewDocumentId,
      selectedDocumentId,
      otherDocumentId,
      archivedDocumentId,
    ]);
    expect(model.canCreateDocument).toBe(false);
    expect(model.createDocumentDisabledReason).toBe("Select a folder before creating a document.");
  });

  test("calculates stats from real map data", () => {
    const model = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
    });

    expect(model.stats).toEqual([
      { id: "total-documents", label: "Total Documents", value: "4" },
      { id: "collections", label: "Folders", value: "2" },
      { id: "published-documents", label: "Published", value: "1" },
      { id: "draft-documents", label: "Drafts", value: "1" },
      { id: "archived-documents", label: "Archived", value: "1" },
      { id: "updated-30-days", label: "Updated in 30 days", value: "1" },
    ]);
  });

  test("builds status, tag, and collection options from real documents", () => {
    const model = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
    });

    expect(model.collectionOptions.map((option) => `${option.label}:${option.count}`)).toEqual([
      "Foundations:2",
      "Guides:2",
    ]);
    expect(model.statusOptions.map((option) => `${option.label}:${option.count}`)).toEqual([
      "Archived:1",
      "Draft:1",
      "In Review:1",
      "Published:1",
    ]);
    expect(model.tagOptions.map((option) => `${option.label}:${option.count}`)).toEqual([
      "Guide:3",
      "Process:1",
      "Reference:2",
      "Strategy:2",
    ]);
  });

  test("filters by search query across title, tag, and collection title", () => {
    const titleModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      query: "mission",
    });
    const tagModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      query: "glossary",
    });
    const collectionModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      query: "guides",
    });

    expect(titleModel.documents.map((document) => document.id)).toEqual([selectedDocumentId]);
    expect(tagModel.documents.map((document) => document.id)).toEqual([archivedDocumentId]);
    expect(collectionModel.documents.map((document) => document.id)).toEqual([otherDocumentId, archivedDocumentId]);
  });

  test("filters by real status and tag options only", () => {
    const statusModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      status: "published",
    });
    const invalidStatusModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      status: "missing",
    });
    const tagModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      tag: "guide",
    });

    expect(statusModel.statusFilter).toBe("published");
    expect(statusModel.documents.map((document) => document.id)).toEqual([otherDocumentId]);
    expect(invalidStatusModel.statusFilter).toBe(null);
    expect(invalidStatusModel.documents.length).toBe(4);
    expect(tagModel.tagFilter).toBe("guide");
    expect(tagModel.documents.map((document) => document.id)).toEqual([reviewDocumentId, otherDocumentId, archivedDocumentId]);
  });

  test("keeps archived documents filterable and exposes restore action state", () => {
    const model = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      status: "archived",
    });

    expect(model.statusFilter).toBe("archived");
    expect(model.documents.map((document) => document.id)).toEqual([archivedDocumentId]);
    expect(model.documents[0]).toMatchObject({
      canArchive: false,
      canDelete: true,
      canRestore: true,
      isArchived: true,
      statusTone: "archived",
    });
  });

  test("builds document move options from current collections", () => {
    const model = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      query: "mission",
    });

    expect(model.documents[0].canMove).toBe(true);
    expect(model.documents[0].moveOptions.map((option) => ({
      id: option.id,
      isCurrent: option.isCurrent,
      label: option.label,
    }))).toEqual([
      { id: selectedCollectionId, isCurrent: true, label: "Foundations" },
      { id: otherCollectionId, isCurrent: false, label: "Guides" },
    ]);
  });

  test("reports move unavailable when no other collection exists", () => {
    const map = createMap();
    const model = createLibrariesPageModel(createBootstrap(), {
      documents: [map.documents[1]],
      folders: [map.folders[0]],
    }, {
      collectionId: null,
      libraryId: activeLibraryId,
    });

    expect(model.documents[0].canMove).toBe(false);
    expect(model.documents[0].moveOptions).toEqual([
      {
        count: 2,
        id: selectedCollectionId,
        label: "Foundations",
        isCurrent: true,
      },
    ]);
  });

  test("sorts documents by updated date, title, status, and collection", () => {
    const updatedModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      sortKey: "updatedAt",
    });
    const titleModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      sortKey: "title",
    });
    const statusModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      sortKey: "status",
    });
    const collectionModel = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: null,
      libraryId: activeLibraryId,
      sortKey: "collection",
    });

    expect(updatedModel.documents.map((document) => document.title)).toEqual([
      "Review Plan",
      "Mission",
      "Handbook",
      "Glossary",
    ]);
    expect(titleModel.documents.map((document) => document.title)).toEqual([
      "Glossary",
      "Handbook",
      "Mission",
      "Review Plan",
    ]);
    expect(statusModel.documents.map((document) => document.status)).toEqual([
      "Archived",
      "Draft",
      "In Review",
      "Published",
    ]);
    expect(collectionModel.documents.map((document) => document.title)).toEqual([
      "Mission",
      "Review Plan",
      "Glossary",
      "Handbook",
    ]);
  });

  test("limits visible tags and exposes hidden tag count for document cards", () => {
    const model = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: selectedCollectionId,
      libraryId: activeLibraryId,
      query: "review",
    });

    expect(model.documents[0]).toMatchObject({
      hiddenTagCount: 1,
      statusTone: "neutral",
      visibleTags: ["Process", "Guide", "Strategy"],
    });
  });

  test("reports collection operations from current map state", () => {
    const map = createMap();
    const model = createLibrariesPageModel(createBootstrap(), {
      ...map,
      folders: [
        ...map.folders,
        {
          documentCount: 0,
          id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          sortOrder: 3,
          title: "03. Empty",
        },
      ],
    }, {
      collectionId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      libraryId: activeLibraryId,
    });

    expect(model.activeCollectionTitle).toBe("Empty");
    expect(model.canRenameActiveCollection).toBe(true);
    expect(model.canDeleteActiveCollection).toBe(true);
    expect(model.canReorderActiveCollectionUp).toBe(true);
    expect(model.canReorderActiveCollectionDown).toBe(false);
    expect(model.collections.find((collection) => collection.id === "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa")).toMatchObject({
      canDelete: true,
      sortOrder: 3,
      title: "Empty",
    });
  });

  test("calculates stable collection reorder ids", () => {
    const model = createLibrariesPageModel(createBootstrap(), createMap(), {
      collectionId: selectedCollectionId,
      libraryId: activeLibraryId,
    });

    expect(createCollectionReorderIds(model.collections, selectedCollectionId, "down")).toEqual([
      otherCollectionId,
      selectedCollectionId,
    ]);
    expect(createCollectionReorderIds(model.collections, selectedCollectionId, "up")).toBe(null);
    expect(createCollectionReorderIds(model.collections, null, "down")).toBe(null);
  });

  test("chooses adjacent collection after deleting the active empty collection", () => {
    const map = createMap();
    const emptyCollectionId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    const model = createLibrariesPageModel(createBootstrap(), {
      ...map,
      folders: [
        ...map.folders,
        {
          documentCount: 0,
          id: emptyCollectionId,
          sortOrder: 3,
          title: "03. Empty",
        },
      ],
    }, {
      collectionId: emptyCollectionId,
      libraryId: activeLibraryId,
    });

    expect(getCollectionIdAfterDelete(model.collections, emptyCollectionId)).toBe(otherCollectionId);
    expect(getCollectionIdAfterDelete(model.collections.slice(0, 1), selectedCollectionId)).toBe(null);
  });

  test("reports create document as unavailable when the current library has no collections", () => {
    const model = createLibrariesPageModel(createBootstrap(), { documents: [], folders: [] }, {
      collectionId: null,
      libraryId: activeLibraryId,
    });

    expect(model.hasCollections).toBe(false);
    expect(model.canCreateCollection).toBe(true);
    expect(model.canCreateDocument).toBe(false);
    expect(model.createDocumentDisabledReason).toBe("Select a folder before creating a document.");
  });

  test("chooses a preferred library from requested, active, current, then first library", () => {
    expect(getPreferredLibraryId(createBootstrap(), secondLibraryId)).toBe(secondLibraryId);
    expect(getPreferredLibraryId(createBootstrap(), null)).toBe(activeLibraryId);
    expect(getPreferredLibraryId({ ...createBootstrap(), activeSpaceId: "missing" }, null)).toBe(secondLibraryId);
    expect(getPreferredLibraryId({ ...createBootstrap(), activeSpaceId: "missing", workspace: { ...createBootstrap().workspace, currentSpaceId: "missing" } }, null)).toBe(activeLibraryId);
    expect(getPreferredLibraryId({ ...createBootstrap(), spaces: [] }, null)).toBe(null);
  });
});

function createBootstrap(): BootstrapResponse {
  return {
    activeDocumentId: selectedDocumentId,
    activeSpaceId: activeLibraryId,
    documents: [],
    folders: [],
    spaces: [
      { id: activeLibraryId, name: "Operations" },
      { id: secondLibraryId, name: "Research" },
    ],
    workspace: {
      currentSpaceId: secondLibraryId,
      id: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
      name: "Atlas Workspace",
      organizationId: "99999999-9999-4999-8999-999999999999",
    },
  };
}

function createMap(): KnowledgeMapResponse {
  return {
    documents: [
      {
        folderId: otherCollectionId,
        id: otherDocumentId,
        sortOrder: 2,
        status: "Published",
        tags: ["Guide"],
        title: "Handbook",
        updatedAt: "2024-01-02T10:00:00.000Z",
      },
      {
        folderId: selectedCollectionId,
        id: selectedDocumentId,
        sortOrder: 1,
        status: "Draft",
        tags: ["Strategy"],
        title: "Mission",
        updatedAt: "2024-02-03T10:00:00.000Z",
      },
      {
        folderId: otherCollectionId,
        id: archivedDocumentId,
        sortOrder: 3,
        status: "Archived",
        tags: ["Guide", "Reference"],
        title: "Glossary",
        updatedAt: "2023-11-05T10:00:00.000Z",
      },
      {
        folderId: selectedCollectionId,
        id: reviewDocumentId,
        sortOrder: 4,
        status: "In Review",
        tags: ["Process", "Guide", "Strategy", "Reference"],
        title: "Review Plan",
        updatedAt: new Date().toISOString(),
      },
    ],
    folders: [
      {
        documentCount: 2,
        id: selectedCollectionId,
        sortOrder: 1,
        title: "01. Foundations",
      },
      {
        documentCount: 2,
        id: otherCollectionId,
        sortOrder: 2,
        title: "02. Guides",
      },
    ],
  };
}
