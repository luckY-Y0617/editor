import { describe, expect, test } from "../test/harness";
import { matchCommentAnchorText, normalizePlainTextV1 } from "./commentAnchorMatching";

describe("commentAnchorMatching", () => {
  test("exact match returns active exact", () => {
    const result = matchCommentAnchorText({
      mappedText: "immutable anchors",
      normalizedExact: "immutable anchors",
      rangeValid: true,
    });

    expect(result).toMatchObject({
      status: "active",
      confidence: "exact",
      reason: "exact_match",
      similarity: 1,
      editDistance: 0,
    });
  });

  test("one-character change in reasonably long text remains active high", () => {
    const result = matchCommentAnchorText({
      mappedText: "immutable anchor",
      normalizedExact: "immutable anchors",
      rangeValid: true,
    });

    expect(result.status).toBe("active");
    expect(result.confidence).toBe("high");
    expect(result.reason).toBe("minor_text_change");
    expect(result.editDistance).toBe(1);
  });

  test("one-character change in very short text is conservative", () => {
    const result = matchCommentAnchorText({
      mappedText: "ax",
      normalizedExact: "at",
      rangeValid: true,
    });

    expect(result.status).toBe("stale");
    expect(result.confidence).toBe("medium");
    expect(result.reason).toBe("major_text_change");
  });

  test("major replacement becomes stale", () => {
    const result = matchCommentAnchorText({
      mappedText: "unrelated replacement",
      normalizedExact: "immutable anchors",
      rangeValid: true,
    });

    expect(result.status).toBe("stale");
    expect(result.reason).toBe("major_text_change");
    expect(["medium", "low"]).toContain(result.confidence);
  });

  test("empty mapped text becomes orphaned", () => {
    const result = matchCommentAnchorText({
      mappedText: "   ",
      normalizedExact: "immutable anchors",
      rangeValid: true,
    });

    expect(result).toMatchObject({
      status: "orphaned",
      confidence: "none",
      reason: "empty_range",
    });
  });

  test("invalid range becomes orphaned", () => {
    const result = matchCommentAnchorText({
      mappedText: "immutable anchors",
      normalizedExact: "immutable anchors",
      rangeValid: false,
    });

    expect(result).toMatchObject({
      status: "orphaned",
      confidence: "none",
      reason: "invalid_range",
    });
  });

  test("northstar plain text normalizer collapses whitespace without changing case or punctuation", () => {
    expect(normalizePlainTextV1("  Northstar,\n\tComment   Anchor!  ")).toBe(
      "Northstar, Comment Anchor!",
    );
  });
});
