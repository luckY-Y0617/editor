import { describe, expect, test } from "../test/harness";
import { ApiClientError } from "./apiClient";
import {
  createShareLinkRequest,
  createWorkspaceShareLinkRequest,
  getShareDrawerInviteDisabledReason,
  getShareDrawerLinkDisabledReason,
  getShareLinkCapability,
  resolveShareTarget,
  toAbsoluteShareUrl,
  toDocumentAdvancedRoleRows,
  toSharePermissionMutationError,
} from "./documentShareLinksModel";

describe("documentShareLinksModel", () => {
  test("resolves configured document before bootstrap fallback", () => {
    expect(
      resolveShareTarget({
        apiConfigured: true,
        bootstrapDocumentId: "bootstrap-doc",
        bootstrapWorkspaceId: "bootstrap-workspace",
        configuredDocumentId: "configured-doc",
        configuredDocumentSource: "hash",
        configuredWorkspaceId: "configured-workspace",
      }),
    ).toEqual({
      documentId: "configured-doc",
      reason: null,
      source: "hash",
      workspaceId: "configured-workspace",
    });
  });

  test("falls back to bootstrap active document", () => {
    expect(
      resolveShareTarget({
        apiConfigured: true,
        bootstrapDocumentId: "bootstrap-doc",
        bootstrapWorkspaceId: "bootstrap-workspace",
        configuredDocumentId: null,
        configuredWorkspaceId: null,
      }),
    ).toEqual({
      documentId: "bootstrap-doc",
      reason: null,
      source: "bootstrap",
      workspaceId: "bootstrap-workspace",
    });
  });

  test("keeps missing target honest when API or document is unavailable", () => {
    expect(resolveShareTarget({ apiConfigured: false }).reason).toBe("Share API is not configured.");
    expect(resolveShareTarget({ apiConfigured: true }).reason).toBe(
      "Open Share from a document or configure a share document id.",
    );
  });

  test("derives share link capability from status and operation", () => {
    expect(
      getShareLinkCapability({
        apiConfigured: true,
        documentId: "doc",
        operation: null,
        status: "ready",
      }),
    ).toEqual({ canUse: true, reason: null });

    expect(
      getShareLinkCapability({
        apiConfigured: true,
        documentId: "doc",
        operation: null,
        status: "forbidden",
      }).reason,
    ).toContain("do not have permission");

    expect(
      getShareLinkCapability({
        apiConfigured: true,
        documentId: "doc",
        operation: "creating",
        status: "ready",
      }).reason,
    ).toContain("operation is in progress");
  });

  test("builds trimmed request data without pretending public links are generic policy", () => {
    expect(createWorkspaceShareLinkRequest("viewer")).toEqual({
      audience: "workspace",
      expiresAt: null,
      password: null,
      roleKey: "viewer",
      subjectEmail: null,
    });

    expect(
      createShareLinkRequest({
        audience: "external",
        expiresAt: "2026-06-01T00:00:00.000Z",
        roleKey: "commenter",
        subjectEmail: " Person@Example.COM ",
      }),
    ).toEqual({
      audience: "external",
      expiresAt: "2026-06-01T00:00:00.000Z",
      password: null,
      roleKey: "commenter",
      subjectEmail: "person@example.com",
    });
  });

  test("explains daily share drawer invite disabled reasons", () => {
    expect(
      getShareDrawerInviteDisabledReason({
        apiConfigured: true,
        availableRoles: new Set(["viewer", "commenter"]),
        inviteIsEmail: true,
        isDirectMemberInvite: false,
        memberStatus: "ready",
        operation: null,
        selectedRole: "editor",
        status: "ready",
        value: "person@example.com",
      }),
    ).toBe("Email invites support viewer or commenter access only.");

    expect(
      getShareDrawerInviteDisabledReason({
        apiConfigured: true,
        availableRoles: new Set(["viewer", "commenter"]),
        inviteIsEmail: false,
        isDirectMemberInvite: false,
        memberStatus: "error",
        operation: null,
        selectedRole: "viewer",
        status: "ready",
        value: "@alex",
      }),
    ).toBe("Workspace member search is unavailable. Enter an email address to invite externally.");
  });

  test("explains share link disabled reasons without exposing edit links", () => {
    expect(
      getShareDrawerLinkDisabledReason({
        apiConfigured: true,
        expiresAt: null,
        linkScope: "invited",
        operation: null,
        status: "ready",
      }),
    ).toContain("no share link is needed");

    expect(
      getShareDrawerLinkDisabledReason({
        apiConfigured: true,
        expiresAt: null,
        linkScope: "public",
        operation: null,
        status: "ready",
      }),
    ).toBe("Public links require a future expiry time.");
  });

  test("preserves Northstar API message before generic fallbacks", () => {
    expect(
      toSharePermissionMutationError(
        { message: "Public share links require expiresAt.", status: 400 },
        "Fallback",
      ),
    ).toBe("Public share links require expiresAt.");
    expect(toSharePermissionMutationError({ message: "API returned 403", status: 403 }, "Fallback")).toContain(
      "do not have permission",
    );
    expect(
      toSharePermissionMutationError(
        new ApiClientError(0, "Could not reach API endpoint https://northstar.test/api/v1/share-links. Failed to fetch"),
        "Fallback",
      ),
    ).toBe("Could not reach the share-link API. Check the backend session and retry.");
  });

  test("normalizes generated API resolve paths for copy/display", () => {
    expect(toAbsoluteShareUrl("/api/v1/share-links/token/resolve", "https://localhost:7036/api/v1")).toBe(
      "https://localhost:7036/api/v1/share-links/token/resolve",
    );
  });

  test("models document advanced permission roles from backend available roles", () => {
    expect(toDocumentAdvancedRoleRows(["commenter", "viewer", "editor", "viewer"]).map((row) => row.roleKey)).toEqual([
      "editor",
      "commenter",
      "viewer",
    ]);
    expect(toDocumentAdvancedRoleRows([])).toEqual([
      {
        access: "Can view",
        description: "Can read this document when effective access permits it.",
        label: "Viewer",
        roleKey: "viewer",
      },
    ]);
  });
});
