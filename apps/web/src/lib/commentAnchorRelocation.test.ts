import { describe, expect, test } from "../test/harness";
import { commentTestSchema, createTestCommentAnchor, findTextRange } from "../test/commentTestUtils";
import { relocateCommentAnchor } from "./commentAnchorRelocation";

describe("commentAnchorRelocation", () => {
  test("same blockId with text inserted before anchor relocates to the quote", () => {
    const doc = docFromParagraphs([{ text: "alpha new beta gamma", blockId: "blk_anchor0001" }]);
    const anchor = createTestCommentAnchor({
      exact: "beta",
      range: findTextRange("alpha beta gamma", "beta"),
      startBlockId: "blk_anchor0001",
      endBlockId: "blk_anchor0001",
    });
    const result = relocateCommentAnchor(doc, anchor);

    expect(result.status).toBe("active");
    expect(result.confidence).toBe("exact");
    expect(result.reason).toBe("block_exact_quote");
    expect(textAt(doc, result.range)).toBe("beta");
  });

  test("same blockId with minor text edit returns active high confidence", () => {
    const doc = docFromParagraphs([{ text: "anchor phrasf", blockId: "blk_anchor0002" }]);
    const anchor = createTestCommentAnchor({
      exact: "anchor phrase",
      range: { from: 1, to: 14 },
      startBlockId: "blk_anchor0002",
      endBlockId: "blk_anchor0002",
    });
    const result = relocateCommentAnchor(doc, anchor);

    expect(result.status).toBe("active");
    expect(result.confidence).toBe("high");
    expect(textAt(doc, result.range)).toBe("anchor phrasf");
  });

  test("same blockId with major text edit returns stale", () => {
    const doc = docFromParagraphs([{ text: "anchor island", blockId: "blk_anchor0003" }]);
    const anchor = createTestCommentAnchor({
      exact: "anchor phrase",
      range: { from: 1, to: 14 },
      startBlockId: "blk_anchor0003",
      endBlockId: "blk_anchor0003",
    });
    const result = relocateCommentAnchor(doc, anchor);

    expect(result.status).toBe("stale");
    expect(result.reason).toBe("block_offset_match");
    expect(textAt(doc, result.range)).toBe("anchor island");
  });

  test("missing blockId uses unique document-level exact quote fallback", () => {
    const doc = docFromParagraphs([
      { text: "intro text", blockId: "blk_intro0001" },
      { text: "prefix unique quote suffix", blockId: "blk_body00001" },
    ]);
    const anchor = createTestCommentAnchor({
      exact: "unique quote",
      range: { from: 1, to: 5 },
      startBlockId: "blk_missing01",
      endBlockId: "blk_missing01",
    });
    const result = relocateCommentAnchor(doc, anchor);

    expect(result.status).toBe("active");
    expect(result.confidence).toBe("exact");
    expect(result.reason).toBe("document_exact_quote");
    expect(textAt(doc, result.range)).toBe("unique quote");
  });

  test("ambiguous document-level exact quote becomes orphaned", () => {
    const doc = docFromParagraphs([
      { text: "repeat quote", blockId: "blk_repeat001" },
      { text: "repeat quote", blockId: "blk_repeat002" },
    ]);
    const anchor = createTestCommentAnchor({
      exact: "repeat quote",
      range: { from: 1, to: 3 },
    });
    const result = relocateCommentAnchor(doc, anchor);

    expect(result.status).toBe("orphaned");
    expect(result.reason).toBe("ambiguous_match");
    expect(result.candidates).toBe(2);
  });

  test("deleted block becomes orphaned when no safe fallback exists", () => {
    const doc = docFromParagraphs([{ text: "surviving text", blockId: "blk_survive01" }]);
    const anchor = createTestCommentAnchor({
      exact: "deleted quote",
      range: { from: 1, to: 5 },
      startBlockId: "blk_deleted01",
      endBlockId: "blk_deleted01",
    });
    const result = relocateCommentAnchor(doc, anchor);

    expect(result.status).toBe("orphaned");
    expect(result.reason).toBe("missing_block");
  });

  test("old anchors without blockId use document quote fallback", () => {
    const doc = docFromParagraphs([
      { text: "intro text", blockId: "blk_intro0002" },
      { text: "old anchor quote lives here", blockId: "blk_body00002" },
    ]);
    const anchor = createTestCommentAnchor({
      exact: "old anchor quote",
      range: { from: 1, to: 5 },
    });
    const result = relocateCommentAnchor(doc, anchor);

    expect(result.status).toBe("active");
    expect(result.reason).toBe("document_exact_quote");
    expect(textAt(doc, result.range)).toBe("old anchor quote");
  });

  test("cross-textblock anchors are reconstructed conservatively when both blocks still exist", () => {
    const doc = docFromParagraphs([
      { text: "alpha", blockId: "blk_cross001" },
      { text: "beta", blockId: "blk_cross002" },
    ]);
    const anchor = createTestCommentAnchor({
      exact: "alpha beta",
      range: { from: 1, to: 3 },
      startBlockId: "blk_cross001",
      endBlockId: "blk_cross002",
    });
    anchor.block.start.textOffset = 0;
    anchor.block.end.path = [1];
    anchor.block.end.textOffset = 4;
    const result = relocateCommentAnchor(doc, anchor);

    expect(result.status).toBe("active");
    expect(result.confidence).toBe("exact");
    expect(textAt(doc, result.range)).toBe("alpha beta");
  });
});

function docFromParagraphs(paragraphs: Array<{ text: string; blockId: string }>) {
  return commentTestSchema.node(
    "doc",
    null,
    paragraphs.map((paragraph) =>
      commentTestSchema.node(
        "paragraph",
        { blockId: paragraph.blockId },
        paragraph.text ? [commentTestSchema.text(paragraph.text)] : undefined,
      ),
    ),
  );
}

function textAt(doc: ReturnType<typeof docFromParagraphs>, range?: { from: number; to: number }) {
  if (!range) {
    return "";
  }

  return doc.textBetween(range.from, range.to, " ");
}
