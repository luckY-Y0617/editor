import { describe, expect, test } from "../test/harness";
import {
  createPublicResourceShareLinkRequest,
  getContainerPublicSharePolicy,
  getPublicResourceShareDisabledReason,
  getResourceShareTitle,
} from "./resourceShareLinksModel";

const nodeFs = "node:fs";
const { readFileSync } = await import(nodeFs);

describe("resourceShareLinksModel", () => {
  test("builds library public share create request with fixed viewer role and safe metadata", () => {
    const payload = createPublicResourceShareLinkRequest({
      contentProtection: {
        disableCopy: true,
        disableDownload: true,
        disablePrint: false,
        watermarkEnabled: true,
        watermarkText: "Public link",
      },
      expiresAt: "2026-05-20T00:00:00.000Z",
      password: " open ",
      passwordEnabled: true,
      resourceId: "library-1",
      resourceType: "library",
    });

    expect(payload.resourceType).toBe("library");
    expect(payload.resourceId).toBe("library-1");
    expect(payload.request).toEqual({
      audience: "public",
      contentProtection: {
        disableDownload: true,
        disablePrint: false,
        disableCopy: true,
        watermarkEnabled: true,
        watermarkText: "Public link",
      },
      expiresAt: "2026-05-20T00:00:00.000Z",
      password: "open",
      roleKey: "viewer",
      subjectEmail: null,
    });
  });

  test("builds collection public share create request without workspace scope or secrets in URL state", () => {
    const payload = createPublicResourceShareLinkRequest({
      contentProtection: {
        disableCopy: false,
        disableDownload: true,
        disablePrint: true,
        watermarkEnabled: false,
        watermarkText: "Public link",
      },
      expiresAt: "2026-05-20T00:00:00.000Z",
      password: "folder-pass",
      passwordEnabled: true,
      resourceId: "collection-1",
      resourceType: "collection",
    });

    expect(payload.resourceType).toBe("collection");
    expect(payload.resourceId).toBe("collection-1");
    expect(payload.request.roleKey).toBe("viewer");
    expect(payload.request.audience).toBe("public");
    expect(payload.request.subjectEmail).toBe(null);
    expect(JSON.stringify(payload).includes("workspace")).toBe(false);
  });

  test("shows policy disabled reasons for container contexts", () => {
    expect(
      getPublicResourceShareDisabledReason(
        {
          contentProtection: {
            disableCopy: false,
            disableDownload: true,
            disablePrint: false,
            watermarkEnabled: false,
            watermarkText: "Public link",
          },
          expiresAt: "2026-05-20T00:00:00.000Z",
          password: "open",
          passwordEnabled: true,
          resourceId: "library-1",
          resourceType: "library",
        },
        { ...getContainerPublicSharePolicy("library"), allowLibraryScope: false },
      ),
    ).toBe("Enterprise policy does not allow Library public links.");

    expect(
      getPublicResourceShareDisabledReason({
        contentProtection: {
          disableCopy: false,
          disableDownload: true,
          disablePrint: false,
          watermarkEnabled: false,
          watermarkText: "Public link",
        },
        expiresAt: "2026-05-20T00:00:00.000Z",
        password: "",
        passwordEnabled: true,
        resourceId: "collection-1",
        resourceType: "collection",
      }),
    ).toContain("Collection public links");
  });

  test("places container share entries on Libraries without changing document drawer creation", () => {
    const librariesPage = readFileSync("src/components/LibrariesPage.tsx", "utf8");
    const resourceDrawer = readFileSync("src/components/ResourceShareDrawer.tsx", "utf8");
    const documentDrawer = readFileSync("src/components/DocumentShareDrawer.tsx", "utf8");

    expect(librariesPage.includes("Publish Library")).toBe(true);
    expect(librariesPage.includes("onShareCollection")).toBe(true);
    expect(resourceDrawer.includes("createResourceShareLink(resourceType, resourceId, request)")).toBe(true);
    expect(resourceDrawer.includes("Workspace public sharing is unsupported")).toBe(true);
    expect(documentDrawer.includes("createResourceShareLink")).toBe(false);
    expect(documentDrawer.includes("Related broader links")).toBe(true);
  });

  test("titles resource drawers with explicit resource context", () => {
    expect(getResourceShareTitle("library", "Atlas Library")).toBe('Publish Library "Atlas Library"');
    expect(getResourceShareTitle("collection", "Foundations")).toBe('Share Folder "Foundations"');
  });
});
