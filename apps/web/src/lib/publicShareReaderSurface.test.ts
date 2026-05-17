import { describe, expect, test } from "../test/harness";
import { sanitizeReadonlyDocumentContent } from "../components/DocumentReaderSurface";

const nodeFs = "node:fs";
const { readFileSync } = await import(nodeFs);

describe("public share reader surface", () => {
  test("uses the shared document reader surface and readonly renderer", () => {
    const publicSharePageSource = readSource("src/components/PublicSharePage.tsx");
    const editorCanvasSource = readSource("src/components/EditorCanvas.tsx");

    expect(publicSharePageSource.includes("DocumentReaderSurface")).toBe(true);
    expect(publicSharePageSource.includes("ReadonlyDocumentContent")).toBe(true);
    expect(editorCanvasSource.includes("DocumentReaderSurface")).toBe(true);
  });

  test("keeps public reader out of the internal editor shell and protected loaders", () => {
    const source = readSource("src/components/PublicSharePage.tsx");
    const forbiddenInternalSurfaces = [
      "KnowledgeEditorPage",
      "EditorCanvas",
      "EditorSidebar",
      "OutlinePanel",
      "WorkspaceHomeTopBar",
      "DocumentShareDrawer",
      "getBootstrap",
      "getDocumentContext",
      "getDocumentActivity",
      "getDocumentAttachments",
      "getDocumentVersions",
    ];

    expect(forbiddenInternalSurfaces.filter((pattern) => source.includes(pattern))).toEqual([]);
  });

  test("keeps public password proof out of urls and persistent browser storage", () => {
    const source = readSource("src/components/PublicSharePage.tsx");

    expect(source.includes("localStorage")).toBe(false);
    expect(source.includes("sessionStorage")).toBe(false);
    expect(source.includes("window.location")).toBe(false);
    expect(source.includes("URLSearchParams")).toBe(false);
  });

  test("renders content protection behavior without adding public download controls", () => {
    const source = readSource("src/components/PublicSharePage.tsx");

    expect(source.includes("public-share-copy-limited")).toBe(true);
    expect(source.includes("public-share-watermark")).toBe(true);
    expect(source.includes("disablePrint")).toBe(true);
    expect(source.includes("download")).toBe(false);
  });

  test("keeps legacy collection summary endpoint separate from scoped document reading", () => {
    const source = readSource("src/components/PublicSharePage.tsx");
    const collectionPageSource = source.slice(
      source.indexOf("function PublicShareCollectionPage"),
      source.indexOf("function PublicShareScopePage"),
    );

    expect(collectionPageSource.includes("ReadonlyDocumentContent")).toBe(false);
    expect(collectionPageSource.includes("document.content")).toBe(false);
    expect(source.includes("getScopedPublicShareDocument")).toBe(true);
  });

  test("renders public knowledge base navigation without protected app shell", () => {
    const source = readSource("src/components/PublicSharePage.tsx");

    expect(source.includes("derivePublicShareKnowledgeBaseState")).toBe(true);
    expect(source.includes("public-share-breadcrumb")).toBe(true);
    expect(source.includes("public-share-prev-next")).toBe(true);
    expect(source.includes("getScopedPublicShareDocument(token")).toBe(true);
    expect(source.includes("createEditorHash")).toBe(false);
    expect(source.includes("#editor")).toBe(false);
  });

  test("replaces internal file references with unavailable fallback content", () => {
    const sanitized = sanitizeReadonlyDocumentContent({
      type: "doc",
      content: [
        {
          type: "imageBlock",
          attrs: {
            fileId: "file-1",
            src: "/api/v1/files/file-1/content",
          },
        },
      ],
    });

    expect(sanitized).toEqual({
      type: "doc",
      content: [
        {
          type: "paragraph",
          content: [{ type: "text", text: "File preview is not available in this public view." }],
        },
      ],
    });
  });
});

function readSource(path: string) {
  return readFileSync(path, "utf8");
}
