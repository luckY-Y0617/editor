import { describe, expect, test } from "../test/harness";
import { ApiClientError } from "./apiClient";
import {
  createShareLinkRequest,
  createAccessRequestReviewRequest,
  getAvailableDocumentGrantRoles,
  createWorkspaceShareLinkRequest,
  getDocumentShareLinkStatus,
  getExistingShareLinkCopyCapability,
  getGenericPolicyLinkModeOptions,
  getIamManagedGroupState,
  getInheritanceModeLabel,
  getInheritedSourceLabel,
  getShareDrawerInviteDisabledReason,
  getShareDrawerLinkDisabledReason,
  getShareLinkCapability,
  hasForbiddenAdvancedPermissionSecretFields,
  resolveShareTarget,
  selectAllGrantIds,
  summarizeBatchGrantRevoke,
  toAbsoluteShareUrl,
  toDocumentAdvancedRoleRows,
  toSharePermissionMutationError,
  toggleBatchGrantSelection,
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
        audience: "public",
        expiresAt: "2026-06-01T00:00:00.000Z",
        password: " open ",
        roleKey: "commenter",
        subjectEmail: "person@example.com",
      }),
    ).toEqual({
      audience: "public",
      expiresAt: "2026-06-01T00:00:00.000Z",
      password: "open",
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

  test("derives document share link row status without widening public access", () => {
    const active = {
      audience: "workspace" as const,
      expiresAt: "2026-06-01T00:00:00.000Z",
      revokedAt: null,
    };

    expect(getDocumentShareLinkStatus(active, "internal", new Date("2026-05-14T00:00:00.000Z"))).toBe("active");
    expect(getDocumentShareLinkStatus(active, "disabled", new Date("2026-05-14T00:00:00.000Z"))).toBe("policy-paused");
    expect(getDocumentShareLinkStatus({ ...active, expiresAt: "2026-05-01T00:00:00.000Z" }, "internal", new Date("2026-05-14T00:00:00.000Z"))).toBe("expired");
    expect(getDocumentShareLinkStatus({ ...active, revokedAt: "2026-05-13T00:00:00.000Z" }, "internal", new Date("2026-05-14T00:00:00.000Z"))).toBe("revoked");
    expect(getDocumentShareLinkStatus({ ...active, status: "paused" }, "internal", new Date("2026-05-14T00:00:00.000Z"))).toBe("paused");
  });

  test("allows existing link copy only through approved audited endpoint", () => {
    expect(
      getExistingShareLinkCopyCapability({
        apiConfigured: true,
        canManage: true,
        copyEndpointAvailable: true,
        operation: null,
        status: "active",
      }),
    ).toBe(null);

    expect(
      getExistingShareLinkCopyCapability({
        apiConfigured: true,
        canManage: true,
        copyEndpointAvailable: false,
        operation: null,
        status: "active",
      }),
    ).toContain("approved audited copy endpoint");

    expect(
      getExistingShareLinkCopyCapability({
        apiConfigured: true,
        canManage: true,
        copyEndpointAvailable: true,
        operation: null,
        status: "expired",
      }),
    ).toBe("Expired links cannot be copied.");
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

  test("filters advanced grant roles to backend availableRoles only", () => {
    expect(getAvailableDocumentGrantRoles([" viewer ", "commenter", "viewer", ""])).toEqual(["viewer", "commenter"]);
    expect(toDocumentAdvancedRoleRows(["owner", "admin"]).map((row) => row.roleKey)).toEqual(["owner", "admin"]);
  });

  test("does not expose public as a generic policy link mode", () => {
    expect(getGenericPolicyLinkModeOptions().map((option) => option.value)).toEqual(["disabled", "internal", "external"]);
    expect(getGenericPolicyLinkModeOptions().some((option) => String(option.value) === "public")).toBe(false);
  });

  test("labels inheritance modes and inherited sources", () => {
    expect(getInheritanceModeLabel("inherit")).toBe("Inherits workspace or folder access");
    expect(getInheritanceModeLabel("restricted")).toBe("Restricted to direct document grants");
    expect(getInheritedSourceLabel("workspace")).toBe("Workspace inheritance");
    expect(getInheritedSourceLabel("collection")).toBe("Folder inheritance");
    expect(getInheritedSourceLabel("none")).toBe("No inherited source");
  });

  test("marks IAM-managed groups as read-only with source metadata", () => {
    expect(
      getIamManagedGroupState({
        externalProvider: "okta",
        externalGroupId: "eng",
        isArchived: false,
        membersCount: 12,
      }),
    ).toEqual({
      isArchived: false,
      isManaged: true,
      membersLabel: "12 members",
      readOnlyReason: "IAM-managed group. Local member and group editing is disabled.",
      source: "okta / eng",
    });

    expect(getIamManagedGroupState({ isArchived: true, membersCount: 1 }).readOnlyReason).toBe(null);
  });

  test("models batch selection and batch revoke summaries", () => {
    const selected = toggleBatchGrantSelection(new Set<string>(), "grant-a");
    expect(Array.from(selected)).toEqual(["grant-a"]);
    expect(Array.from(toggleBatchGrantSelection(selected, "grant-a"))).toEqual([]);
    expect(Array.from(selectAllGrantIds([{ id: "grant-a", roleKey: "viewer" }, { id: "grant-b", roleKey: "editor" }]))).toEqual([
      "grant-a",
      "grant-b",
    ]);
    expect(summarizeBatchGrantRevoke({ succeeded: ["grant-a"], failed: [] })).toBe("1 grant revoked.");
  });

  test("summarizes batch partial failures honestly", () => {
    expect(
      summarizeBatchGrantRevoke({
        succeeded: ["grant-a"],
        failed: [{ grantId: "grant-b", reason: "FORBIDDEN" }],
      }),
    ).toBe("1 revoked, 1 failed: grant-b: FORBIDDEN");
  });

  test("shapes access request approval role and expiry from availableRoles", () => {
    expect(
      createAccessRequestReviewRequest(
        {
          decision: "approve",
          expiresAt: "2026-06-01T10:00:00.000Z",
          requestedRole: "admin",
          roleKey: "admin",
        },
        ["viewer", "commenter"],
      ),
    ).toEqual({
      decision: "approve",
      expiresAt: "2026-06-01T10:00:00.000Z",
      reason: null,
      roleKey: "viewer",
    });

    expect(
      createAccessRequestReviewRequest(
        {
          decision: "deny",
          expiresAt: "2026-06-01T10:00",
          requestedRole: "viewer",
        },
        ["viewer"],
      ).roleKey,
    ).toBe(null);
  });

  test("detects token and password fields in advanced permission UI model data", () => {
    expect(hasForbiddenAdvancedPermissionSecretFields({ token: "raw" })).toBe(true);
    expect(hasForbiddenAdvancedPermissionSecretFields({ password_hash: "hash" })).toBe(true);
    expect(hasForbiddenAdvancedPermissionSecretFields({ roleKey: "viewer", subjectId: "user-id" })).toBe(false);
  });
});
