import { describe, expect, test } from "../test/harness";
import {
  buildPublicShareReaderUrl,
  extractShareTokenFromApiUrl,
  getPublicSharePasswordHeader,
  getPublicShareReadEndpoint,
  publicShareCodeUsesOnlyAllowedEndpoints,
  toUserFacingShareUrl,
  publicShareUnavailableMessage,
  toPublicShareFailureState,
} from "./publicShareModel";

describe("publicShareModel", () => {
  test("selects public document and collection read endpoints only", () => {
    expect(getPublicShareReadEndpoint("document")).toBe("document");
    expect(getPublicShareReadEndpoint("collection")).toBe("collection");
  });

  test("keeps failure states generic for external users", () => {
    expect(toPublicShareFailureState({ hasPassword: true, passwordSubmitted: false })).toEqual({
      canRetryWithPassword: true,
      message: publicShareUnavailableMessage,
    });
    expect(toPublicShareFailureState({ hasPassword: true, passwordSubmitted: true })).toEqual({
      canRetryWithPassword: true,
      message: "This link is unavailable or has expired.",
    });
  });

  test("keeps password proof in the transient request header shape", () => {
    expect(getPublicSharePasswordHeader(" open ")).toEqual({ "X-Share-Link-Password": "open" });
    expect(getPublicSharePasswordHeader(" ")).toEqual({});
  });

  test("builds canonical frontend public reader URLs from create response tokens", () => {
    expect(buildPublicShareReaderUrl("public-token_123", "http://localhost:5173")).toBe(
      "http://localhost:5173/#public/share-links/public-token_123",
    );
    expect(
      toUserFacingShareUrl(
        "/api/v1/public/share-links/backend-token/resolve",
        "created-token",
        "public",
        "https://api.example.com/api/v1",
        "https://app.example.com",
      ),
    ).toBe("https://app.example.com/#public/share-links/created-token");
  });

  test("converts public API resolve URLs to frontend public reader URLs", () => {
    expect(
      toUserFacingShareUrl(
        "/api/v1/public/share-links/public-token_123/resolve",
        null,
        "public",
        "https://api.example.com/api/v1",
        "https://app.example.com",
      ),
    ).toBe("https://app.example.com/#public/share-links/public-token_123");
    expect(
      toUserFacingShareUrl(
        "/api/v1/share-links/public-token_123/resolve",
        null,
        "public",
        "https://api.example.com/api/v1",
        "https://app.example.com",
      ),
    ).toBe("https://app.example.com/#public/share-links/public-token_123");
    expect(extractShareTokenFromApiUrl("https://api.example.com/api/v1/public/share-links/public-token_123/document")).toBe(
      "public-token_123",
    );
  });

  test("keeps already frontend public route URLs and ignores non-share URLs", () => {
    const frontendUrl = "https://app.example.com/#public/share-links/public-token_123";
    expect(toUserFacingShareUrl(frontendUrl, null, "public", "https://api.example.com/api/v1", "https://app.example.com")).toBe(
      frontendUrl,
    );
    expect(extractShareTokenFromApiUrl("/api/v1/documents/public-token_123")).toBe(null);
    expect(toUserFacingShareUrl("/api/v1/documents/public-token_123", null, "public", "https://api.example.com/api/v1", "https://app.example.com")).toBe(
      "https://api.example.com/api/v1/documents/public-token_123",
    );
  });

  test("does not put password material in generated public URLs", () => {
    const url = toUserFacingShareUrl(
      "/api/v1/public/share-links/public-token_123/resolve?password=secret",
      null,
      "public",
      "https://api.example.com/api/v1",
      "https://app.example.com",
    );
    expect(url).toBe("https://app.example.com/#public/share-links/public-token_123");
    expect(url.includes("secret")).toBe(false);
    expect(url.includes("password")).toBe(false);
  });

  test("public share code model rejects protected API widening patterns", () => {
    expect(
      publicShareCodeUsesOnlyAllowedEndpoints(
        "/public/share-links/token/resolve /public/share-links/token/document /public/share-links/token/collection",
      ),
    ).toBe(true);
    expect(publicShareCodeUsesOnlyAllowedEndpoints("/public/share-links/token/document /documents/doc-id/comments")).toBe(false);
  });
});
