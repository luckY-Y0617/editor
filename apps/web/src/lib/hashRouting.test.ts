import { describe, expect, test } from "../test/harness";
import {
  createEditorHash,
  createLibrariesHash,
  createSearchHash,
  createSettingsHash,
  getEditorDocumentIdFromHash,
  getHashRoute,
  getLibrariesFiltersFromHash,
  getPostLoginRedirectHash,
  getSearchFiltersFromHash,
  getSettingsFiltersFromHash,
  normalizeInternalActionHash,
  parseHashRoute,
} from "./hashRouting";

const documentId = "11111111-1111-4111-8111-111111111111";
const libraryId = "22222222-2222-4222-8222-222222222222";
const collectionId = "33333333-3333-4333-8333-333333333333";

describe("hashRouting", () => {
  test("parses a hash route and query params", () => {
    const route = parseHashRoute(`#editor?documentId=${documentId}&panel=outline`);

    expect(route.route).toBe("#editor");
    expect(route.params.get("documentId")).toBe(documentId);
    expect(route.params.get("panel")).toBe("outline");
  });

  test("uses the route base for protected route checks", () => {
    expect(getHashRoute(`#editor?documentId=${documentId}`)).toBe("#editor");
  });

  test("returns only valid editor document ids", () => {
    expect(getEditorDocumentIdFromHash(`#editor?documentId=${documentId}`)).toBe(documentId);
    expect(getEditorDocumentIdFromHash("#editor?documentId=not-a-uuid")).toBe(null);
    expect(getEditorDocumentIdFromHash(`#home?documentId=${documentId}`)).toBe(null);
  });

  test("creates editor document hashes only for UUID document ids", () => {
    expect(createEditorHash(documentId)).toBe(`#editor?documentId=${documentId}`);
    expect(createEditorHash("doc-demo")).toBe("#editor");
    expect(createEditorHash(null)).toBe("#editor");
  });

  test("creates and parses folder search hashes", () => {
    const hash = createSearchHash({ folderId: documentId, folderTitle: "Foundations", q: "mission" });

    expect(hash).toBe(`#search?folderId=${documentId}&folderTitle=Foundations&q=mission`);
    expect(getSearchFiltersFromHash(hash)).toEqual({
      folderId: documentId,
      folderTitle: "Foundations",
      q: "mission",
    });
    expect(getSearchFiltersFromHash("#search?folderId=not-a-uuid&folderTitle=Drafts")).toEqual({
      folderId: null,
      folderTitle: "Drafts",
      q: null,
    });
  });

  test("creates and parses libraries hashes", () => {
    const hash = createLibrariesHash({ collectionId, libraryId });

    expect(hash).toBe(`#libraries?libraryId=${libraryId}&collectionId=${collectionId}`);
    expect(getLibrariesFiltersFromHash(hash)).toEqual({
      collectionId,
      libraryId,
    });
    expect(getLibrariesFiltersFromHash("#libraries?libraryId=invalid&collectionId=also-invalid")).toEqual({
      collectionId: null,
      libraryId: null,
    });
    expect(createLibrariesHash({ collectionId: "invalid", libraryId: "invalid" })).toBe("#libraries");
  });

  test("creates and parses workspace settings hashes", () => {
    expect(createSettingsHash()).toBe("#settings");
    expect(createSettingsHash({ tab: "notifications" })).toBe("#settings?tab=notifications");
    expect(createSettingsHash({ spaceId: libraryId, tab: "general" })).toBe(`#settings?spaceId=${libraryId}`);
    expect(createSettingsHash({ spaceId: libraryId, tab: "security" })).toBe(`#settings?tab=security&spaceId=${libraryId}`);
    expect(createSettingsHash({ scope: "workspace", tab: "general" })).toBe("#settings?scope=workspace");
    expect(createSettingsHash({ scope: "library", spaceId: libraryId, tab: "general" })).toBe(
      `#settings?scope=library&tab=general&spaceId=${libraryId}`,
    );
    expect(createSettingsHash({ scope: "library", spaceId: libraryId, tab: "collections" })).toBe(
      `#settings?scope=library&tab=collections&spaceId=${libraryId}`,
    );
    expect(createSettingsHash({ scope: "organization", tab: "overview" })).toBe("#settings?scope=organization&tab=overview");
    expect(createSettingsHash({ scope: "organization", tab: "workspaces" })).toBe("#settings?scope=organization&tab=workspaces");
    expect(createSettingsHash({ scope: "organization", tab: "members" })).toBe("#settings?scope=organization&tab=members");
    expect(createSettingsHash({ scope: "organization", tab: "assessment" })).toBe("#settings?scope=organization&tab=assessment");
    expect(getSettingsFiltersFromHash(`#settings?tab=members&spaceId=${libraryId}`)).toEqual({
      scope: "workspace",
      spaceId: libraryId,
      tab: "members",
    });
    expect(getSettingsFiltersFromHash(`#settings?scope=library&tab=documents&spaceId=${libraryId}`)).toEqual({
      scope: "library",
      spaceId: libraryId,
      tab: "documents",
    });
    expect(getSettingsFiltersFromHash(`#settings?scope=library&tab=members&spaceId=${libraryId}`)).toEqual({
      scope: "library",
      spaceId: libraryId,
      tab: "general",
    });
    expect(getSettingsFiltersFromHash("#settings?scope=organization&tab=overview")).toEqual({
      scope: "organization",
      spaceId: null,
      tab: "overview",
    });
    expect(getSettingsFiltersFromHash("#settings?scope=organization&tab=workspaces")).toEqual({
      scope: "organization",
      spaceId: null,
      tab: "workspaces",
    });
    expect(getSettingsFiltersFromHash("#settings?scope=organization&tab=members")).toEqual({
      scope: "organization",
      spaceId: null,
      tab: "members",
    });
    expect(getSettingsFiltersFromHash("#settings?scope=organization&tab=assessment")).toEqual({
      scope: "organization",
      spaceId: null,
      tab: "assessment",
    });
    expect(getSettingsFiltersFromHash(`#settings?scope=organization&tab=security&spaceId=${libraryId}`)).toEqual({
      scope: "organization",
      spaceId: libraryId,
      tab: "overview",
    });
    expect(getSettingsFiltersFromHash("#settings?tab=unknown&spaceId=invalid")).toEqual({
      scope: "workspace",
      spaceId: null,
      tab: "general",
    });
    expect(getSettingsFiltersFromHash(`#settings?scope=admin&tab=security&spaceId=${libraryId}`)).toEqual({
      scope: "workspace",
      spaceId: libraryId,
      tab: "security",
    });
  });

  test("normalizes action urls to safe internal hashes only", () => {
    expect(normalizeInternalActionHash("#workspace-members")).toBe("#workspace-members");
    expect(normalizeInternalActionHash("#settings?tab=notifications")).toBe("#settings?tab=notifications");
    expect(normalizeInternalActionHash(`#libraries?libraryId=${libraryId}`)).toBe(`#libraries?libraryId=${libraryId}`);
    expect(normalizeInternalActionHash(`#editor?documentId=${documentId}`)).toBe(`#editor?documentId=${documentId}`);
    expect(normalizeInternalActionHash("https://example.com")).toBe("#updates");
    expect(normalizeInternalActionHash("javascript:alert(1)")).toBe("#updates");
    expect(normalizeInternalActionHash("#unknown")).toBe("#updates");
    expect(normalizeInternalActionHash(null)).toBe("#updates");
  });

  test("preserves safe protected target after login", () => {
    expect(getPostLoginRedirectHash("#settings")).toBe("#settings");
    expect(getPostLoginRedirectHash(`#settings?tab=notifications&spaceId=${libraryId}`)).toBe(
      `#settings?tab=notifications&spaceId=${libraryId}`,
    );
    expect(getPostLoginRedirectHash(`#settings?scope=library&tab=collections&spaceId=${libraryId}`)).toBe(
      `#settings?scope=library&tab=collections&spaceId=${libraryId}`,
    );
    expect(getPostLoginRedirectHash("#settings?scope=organization&tab=overview")).toBe("#settings?scope=organization&tab=overview");
    expect(getPostLoginRedirectHash(`#editor?documentId=${documentId}`)).toBe(`#editor?documentId=${documentId}`);
    expect(getPostLoginRedirectHash("#forgot-password")).toBe("#home");
    expect(getPostLoginRedirectHash("")).toBe("#home");
  });
});
