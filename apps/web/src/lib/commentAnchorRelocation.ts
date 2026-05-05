import type { Node as ProseMirrorNode } from "@tiptap/pm/model";
import type { CommentAnchorV1, EditorSelectionRange } from "../types/editor";
import {
  getLevenshteinDistance,
  getNormalizedSimilarity,
  matchCommentAnchorText,
  normalizePlainTextV1,
} from "./commentAnchorMatching";

export type AnchorRelocationResult = {
  status: "active" | "stale" | "orphaned";
  confidence: "exact" | "high" | "medium" | "low" | "none";
  reason:
    | "pm_snapshot_valid"
    | "block_offset_match"
    | "block_exact_quote"
    | "block_fuzzy_quote"
    | "document_exact_quote"
    | "document_fuzzy_quote"
    | "ambiguous_match"
    | "missing_block"
    | "invalid_anchor"
    | "empty_quote"
    | "no_match";
  range?: EditorSelectionRange;
  similarity?: number;
  editDistance?: number;
  candidates?: number;
};

type TextblockLocation = {
  node: ProseMirrorNode;
  pos: number;
  from: number;
  to: number;
  blockId?: string;
};

type IndexedChar = {
  char: string;
  from: number;
  to: number;
};

type SearchIndex = {
  text: string;
  chars: IndexedChar[];
};

type RelocationCandidate = {
  range: EditorSelectionRange;
  text: string;
  similarity: number;
  editDistance: number;
  status: "active" | "stale";
  confidence: "exact" | "high" | "medium" | "low";
};

type CandidateReason =
  | "block_offset_match"
  | "block_exact_quote"
  | "block_fuzzy_quote"
  | "document_exact_quote"
  | "document_fuzzy_quote";

const FUZZY_MIN_SIMILARITY = 0.5;
const FUZZY_LENGTH_WINDOW = 0.3;

export function relocateCommentAnchor(
  doc: ProseMirrorNode,
  anchor: CommentAnchorV1,
): AnchorRelocationResult {
  const normalizedExact = getAnchorNormalizedExact(anchor);

  if (anchor.schema !== "northstar.commentAnchor.v1" || anchor.kind !== "tiptap.textRange") {
    return orphaned("invalid_anchor");
  }

  if (!isValidAnchorRange(anchor.pm)) {
    return orphaned("invalid_anchor");
  }

  if (!normalizedExact) {
    return orphaned("empty_quote");
  }

  const snapshotCandidate = getSnapshotCandidate(doc, anchor, normalizedExact);

  if (snapshotCandidate?.status === "active") {
    return candidateToResult("pm_snapshot_valid", snapshotCandidate, 1);
  }

  const textblocks = collectTextblocks(doc);
  const startBlockId = anchor.block.start.blockId;
  const endBlockId = anchor.block.end.blockId;
  const matchedStartBlock = startBlockId
    ? textblocks.find((textblock) => textblock.blockId === startBlockId) ?? null
    : null;
  const matchedEndBlock = endBlockId
    ? textblocks.find((textblock) => textblock.blockId === endBlockId) ?? null
    : null;
  const blockMissing = Boolean(startBlockId && !matchedStartBlock);
  let bestBlockCandidate: AnchorRelocationResult | null = null;

  if (matchedStartBlock) {
    bestBlockCandidate = getBlockOffsetResult(doc, anchor, matchedStartBlock, normalizedExact);

    const exactBlockResult = getUniqueExactResult(
      "block_exact_quote",
      buildTextblockSearchIndex(matchedStartBlock),
      normalizedExact,
      anchor.pm.from,
    );

    if (exactBlockResult) {
      return exactBlockResult;
    }

    const fuzzyBlockResult = getFuzzyResult(
      "block_fuzzy_quote",
      buildTextblockSearchIndex(matchedStartBlock),
      normalizedExact,
      anchor.pm.from,
      true,
    );

    if (fuzzyBlockResult) {
      if (fuzzyBlockResult.status === "active" || !bestBlockCandidate) {
        bestBlockCandidate = fuzzyBlockResult;
      } else if (bestBlockCandidate.status !== "active") {
        bestBlockCandidate = getStrongerResult(bestBlockCandidate, fuzzyBlockResult, anchor.pm.from);
      }
    }

    if (
      startBlockId &&
      endBlockId &&
      startBlockId !== endBlockId &&
      matchedEndBlock &&
      matchedStartBlock.pos < matchedEndBlock.pos
    ) {
      const crossBlockResult = getCrossTextblockResult(
        doc,
        anchor,
        matchedStartBlock,
        matchedEndBlock,
        normalizedExact,
      );

      if (crossBlockResult?.status === "active") {
        return crossBlockResult;
      }

      if (crossBlockResult && (!bestBlockCandidate || bestBlockCandidate.status !== "active")) {
        bestBlockCandidate = getStrongerResult(bestBlockCandidate, crossBlockResult, anchor.pm.from);
      }
    }
  }

  if (bestBlockCandidate) {
    return bestBlockCandidate;
  }

  const documentIndex = buildDocumentSearchIndex(textblocks);
  const exactDocumentResult = getUniqueExactResult(
    "document_exact_quote",
    documentIndex,
    normalizedExact,
    anchor.pm.from,
  );

  if (exactDocumentResult) {
    return exactDocumentResult;
  }

  const fuzzyDocumentResult = getFuzzyResult(
    "document_fuzzy_quote",
    documentIndex,
    normalizedExact,
    anchor.pm.from,
    false,
  );

  if (fuzzyDocumentResult) {
    return fuzzyDocumentResult;
  }

  if (!startBlockId && snapshotCandidate) {
    return candidateToResult("pm_snapshot_valid", snapshotCandidate, 1);
  }

  return orphaned(blockMissing ? "missing_block" : "no_match");
}

function getAnchorNormalizedExact(anchor: CommentAnchorV1) {
  return normalizePlainTextV1(anchor.quote.normalizedExact || anchor.quote.exact);
}

function getSnapshotCandidate(
  doc: ProseMirrorNode,
  anchor: CommentAnchorV1,
  normalizedExact: string,
): RelocationCandidate | null {
  const range = getSafeRange(doc, anchor.pm);

  if (!range) {
    return null;
  }

  const text = doc.textBetween(range.from, range.to, " ");
  const matchResult = matchCommentAnchorText({
    mappedText: text,
    normalizedExact,
    rangeValid: true,
  });

  if (matchResult.status === "orphaned" || matchResult.confidence === "none") {
    return null;
  }

  return {
    range,
    text,
    status: matchResult.status,
    confidence: matchResult.confidence,
    similarity: matchResult.similarity ?? 1,
    editDistance: matchResult.editDistance ?? 0,
  };
}

function getBlockOffsetResult(
  doc: ProseMirrorNode,
  anchor: CommentAnchorV1,
  textblock: TextblockLocation,
  normalizedExact: string,
): AnchorRelocationResult | null {
  const startOffset = anchor.block.start.textOffset;
  const endOffset = shouldUseAnchorEndOffset(anchor)
    ? anchor.block.end.textOffset
    : startOffset + normalizedExact.length;
  const from = getTextblockPositionAtTextOffset(textblock, startOffset);
  const to = getTextblockPositionAtTextOffset(textblock, endOffset);
  const range = getSafeRange(doc, { from, to });

  if (!range) {
    return null;
  }

  const text = doc.textBetween(range.from, range.to, " ");
  const matchResult = matchCommentAnchorText({
    mappedText: text,
    normalizedExact,
    rangeValid: true,
  });

  if (matchResult.status === "orphaned") {
    return null;
  }

  return {
    status: matchResult.status,
    confidence: matchResult.confidence,
    reason: "block_offset_match",
    range,
    similarity: matchResult.similarity,
    editDistance: matchResult.editDistance,
    candidates: 1,
  };
}

function getCrossTextblockResult(
  doc: ProseMirrorNode,
  anchor: CommentAnchorV1,
  startBlock: TextblockLocation,
  endBlock: TextblockLocation,
  normalizedExact: string,
): AnchorRelocationResult | null {
  const from = getTextblockPositionAtTextOffset(startBlock, anchor.block.start.textOffset);
  const to = getTextblockPositionAtTextOffset(endBlock, anchor.block.end.textOffset);
  const range = getSafeRange(doc, { from, to });

  if (!range) {
    return orphaned("invalid_anchor");
  }

  const text = doc.textBetween(range.from, range.to, " ");
  const matchResult = matchCommentAnchorText({
    mappedText: text,
    normalizedExact,
    rangeValid: true,
  });

  if (matchResult.status === "orphaned") {
    return null;
  }

  return {
    status: matchResult.status,
    confidence: matchResult.confidence,
    reason: "block_offset_match",
    range,
    similarity: matchResult.similarity,
    editDistance: matchResult.editDistance,
    candidates: 1,
  };
}

function getUniqueExactResult(
  reason: CandidateReason,
  index: SearchIndex,
  normalizedExact: string,
  snapshotFrom: number,
): AnchorRelocationResult | null {
  const candidates = findExactCandidates(index, normalizedExact);

  if (candidates.length === 0) {
    return null;
  }

  if (reason === "document_exact_quote" && candidates.length > 1) {
    return orphaned("ambiguous_match", candidates.length);
  }

  const chosen = chooseBestCandidate(candidates, snapshotFrom);

  if (!chosen) {
    return orphaned("ambiguous_match", candidates.length);
  }

  return candidateToResult(reason, chosen, candidates.length);
}

function getFuzzyResult(
  reason: CandidateReason,
  index: SearchIndex,
  normalizedExact: string,
  snapshotFrom: number,
  allowLowConfidence: boolean,
): AnchorRelocationResult | null {
  const candidates = findFuzzyCandidates(index, normalizedExact).filter(
    (candidate) => allowLowConfidence || candidate.confidence !== "low",
  );

  if (candidates.length === 0) {
    return null;
  }

  const chosen = chooseBestCandidate(candidates, snapshotFrom);

  if (!chosen) {
    return orphaned("ambiguous_match", candidates.length);
  }

  return candidateToResult(reason, chosen, candidates.length);
}

function findExactCandidates(index: SearchIndex, normalizedExact: string): RelocationCandidate[] {
  const candidates: RelocationCandidate[] = [];
  let searchFrom = 0;

  while (searchFrom <= index.text.length) {
    const foundIndex = index.text.indexOf(normalizedExact, searchFrom);

    if (foundIndex < 0) {
      break;
    }

    const candidate = createCandidateFromNormalizedWindow(index, foundIndex, foundIndex + normalizedExact.length);

    if (candidate) {
      candidates.push({
        ...candidate,
        status: "active",
        confidence: "exact",
        similarity: 1,
        editDistance: 0,
      });
    }

    searchFrom = foundIndex + 1;
  }

  return candidates;
}

function findFuzzyCandidates(index: SearchIndex, normalizedExact: string): RelocationCandidate[] {
  if (index.text.length === 0) {
    return [];
  }

  const targetLength = normalizedExact.length;
  const minLength = Math.max(1, Math.floor(targetLength * (1 - FUZZY_LENGTH_WINDOW)));
  const maxLength = Math.min(index.text.length, Math.ceil(targetLength * (1 + FUZZY_LENGTH_WINDOW)) + 2);
  const candidatesByRange = new Map<string, RelocationCandidate>();

  for (let start = 0; start < index.text.length; start += 1) {
    for (let length = minLength; length <= maxLength && start + length <= index.text.length; length += 1) {
      const end = start + length;
      const windowText = index.text.slice(start, end);
      const editDistance = getLevenshteinDistance(normalizedExact, windowText);
      const similarity = getNormalizedSimilarity(normalizedExact, windowText, editDistance);

      if (similarity < FUZZY_MIN_SIMILARITY) {
        continue;
      }

      const matchResult = matchCommentAnchorText({
        mappedText: windowText,
        normalizedExact,
        rangeValid: true,
      });

      if (
        matchResult.status === "orphaned" ||
        matchResult.confidence === "none" ||
        matchResult.confidence === "exact"
      ) {
        continue;
      }

      const candidate = createCandidateFromNormalizedWindow(index, start, end);

      if (!candidate) {
        continue;
      }

      const keyedCandidate: RelocationCandidate = {
        ...candidate,
        status: matchResult.status,
        confidence: matchResult.confidence,
        similarity: matchResult.similarity ?? similarity,
        editDistance: matchResult.editDistance ?? editDistance,
      };
      const key = `${keyedCandidate.range.from}:${keyedCandidate.range.to}`;
      const existingCandidate = candidatesByRange.get(key);

      if (!existingCandidate || compareCandidates(keyedCandidate, existingCandidate, 0) < 0) {
        candidatesByRange.set(key, keyedCandidate);
      }
    }
  }

  return [...candidatesByRange.values()];
}

function createCandidateFromNormalizedWindow(
  index: SearchIndex,
  start: number,
  end: number,
): Omit<RelocationCandidate, "status" | "confidence" | "similarity" | "editDistance"> | null {
  const firstChar = index.chars[start];
  const lastChar = index.chars[end - 1];

  if (!firstChar || !lastChar || firstChar.from >= lastChar.to) {
    return null;
  }

  return {
    range: {
      from: firstChar.from,
      to: lastChar.to,
    },
    text: index.text.slice(start, end),
  };
}

function chooseBestCandidate(candidates: RelocationCandidate[], snapshotFrom: number) {
  const sortedCandidates = [...candidates].sort((left, right) => compareCandidates(left, right, snapshotFrom));
  const bestCandidate = sortedCandidates[0];

  if (!bestCandidate) {
    return null;
  }

  const tiedCandidates = sortedCandidates.filter(
    (candidate) => compareCandidates(candidate, bestCandidate, snapshotFrom) === 0,
  );

  return tiedCandidates.length === 1 ? bestCandidate : null;
}

function compareCandidates(left: RelocationCandidate, right: RelocationCandidate, snapshotFrom: number) {
  const leftConfidence = getConfidenceRank(left.confidence);
  const rightConfidence = getConfidenceRank(right.confidence);

  if (leftConfidence !== rightConfidence) {
    return rightConfidence - leftConfidence;
  }

  if (left.similarity !== right.similarity) {
    return right.similarity - left.similarity;
  }

  if (left.editDistance !== right.editDistance) {
    return left.editDistance - right.editDistance;
  }

  const leftDistance = Math.abs(left.range.from - snapshotFrom);
  const rightDistance = Math.abs(right.range.from - snapshotFrom);

  if (leftDistance !== rightDistance) {
    return leftDistance - rightDistance;
  }

  return 0;
}

function getConfidenceRank(confidence: RelocationCandidate["confidence"]) {
  if (confidence === "exact") {
    return 4;
  }

  if (confidence === "high") {
    return 3;
  }

  if (confidence === "medium") {
    return 2;
  }

  return 1;
}

function candidateToResult(
  reason: AnchorRelocationResult["reason"],
  candidate: RelocationCandidate,
  candidates: number,
): AnchorRelocationResult {
  return {
    status: candidate.status,
    confidence: candidate.confidence,
    reason,
    range: candidate.range,
    similarity: candidate.similarity,
    editDistance: candidate.editDistance,
    candidates,
  };
}

function getStrongerResult(
  leftResult: AnchorRelocationResult | null,
  rightResult: AnchorRelocationResult,
  snapshotFrom: number,
) {
  if (!leftResult?.range) {
    return rightResult;
  }

  if (!rightResult.range) {
    return leftResult;
  }

  const leftCandidate = resultToCandidate(leftResult);
  const rightCandidate = resultToCandidate(rightResult);

  return compareCandidates(leftCandidate, rightCandidate, snapshotFrom) <= 0 ? leftResult : rightResult;
}

function resultToCandidate(result: AnchorRelocationResult): RelocationCandidate {
  return {
    range: result.range ?? { from: 0, to: 0 },
    text: "",
    status: result.status === "active" ? "active" : "stale",
    confidence: result.confidence === "none" ? "low" : result.confidence,
    similarity: result.similarity ?? 0,
    editDistance: result.editDistance ?? Number.MAX_SAFE_INTEGER,
  };
}

function buildDocumentSearchIndex(textblocks: TextblockLocation[]): SearchIndex {
  const chars: IndexedChar[] = [];

  textblocks.forEach((textblock, index) => {
    if (index > 0 && chars.length > 0) {
      const lastChar = chars[chars.length - 1];

      chars.push({
        char: " ",
        from: lastChar.to,
        to: lastChar.to,
      });
    }

    chars.push(...collectTextblockChars(textblock));
  });

  return normalizeIndexedChars(chars);
}

function buildTextblockSearchIndex(textblock: TextblockLocation): SearchIndex {
  return normalizeIndexedChars(collectTextblockChars(textblock));
}

function collectTextblockChars(textblock: TextblockLocation): IndexedChar[] {
  const chars: IndexedChar[] = [];

  textblock.node.descendants((node, pos) => {
    if (!node.isText) {
      return true;
    }

    const text = node.text ?? "";
    const absoluteTextStart = textblock.from + pos;

    for (let index = 0; index < text.length; index += 1) {
      chars.push({
        char: text[index],
        from: absoluteTextStart + index,
        to: absoluteTextStart + index + 1,
      });
    }

    return false;
  });

  return chars;
}

function normalizeIndexedChars(chars: IndexedChar[]): SearchIndex {
  const normalizedChars: IndexedChar[] = [];
  let pendingSpace: IndexedChar | null = null;

  for (const char of chars) {
    if (/\s/.test(char.char)) {
      if (normalizedChars.length > 0 && normalizedChars[normalizedChars.length - 1].char !== " ") {
        pendingSpace ??= {
          char: " ",
          from: char.from,
          to: char.to,
        };
      }

      continue;
    }

    if (pendingSpace) {
      normalizedChars.push(pendingSpace);
      pendingSpace = null;
    }

    normalizedChars.push(char);
  }

  return {
    text: normalizedChars.map((char) => char.char).join(""),
    chars: normalizedChars,
  };
}

function collectTextblocks(doc: ProseMirrorNode): TextblockLocation[] {
  const textblocks: TextblockLocation[] = [];

  doc.descendants((node, pos) => {
    if (!node.isTextblock) {
      return true;
    }

    textblocks.push({
      node,
      pos,
      from: pos + 1,
      to: pos + node.nodeSize - 1,
      blockId: typeof node.attrs.blockId === "string" ? node.attrs.blockId : undefined,
    });

    return false;
  });

  return textblocks;
}

function getTextblockPositionAtTextOffset(textblock: TextblockLocation, textOffset: number) {
  const targetOffset = Math.max(0, textOffset);
  let consumedTextLength = 0;
  let bestPosition = textblock.from;

  textblock.node.descendants((node, pos) => {
    if (!node.isText) {
      return true;
    }

    const textLength = node.text?.length ?? 0;
    const absoluteTextStart = textblock.from + pos;

    if (consumedTextLength + textLength >= targetOffset) {
      bestPosition = absoluteTextStart + Math.max(0, targetOffset - consumedTextLength);
      return false;
    }

    consumedTextLength += textLength;
    bestPosition = absoluteTextStart + textLength;
    return false;
  });

  return Math.min(textblock.to, Math.max(textblock.from, bestPosition));
}

function shouldUseAnchorEndOffset(anchor: CommentAnchorV1) {
  const start = anchor.block.start;
  const end = anchor.block.end;

  if (start.blockId && end.blockId) {
    return start.blockId === end.blockId;
  }

  return start.nodeType === end.nodeType && arraysEqual(start.path, end.path);
}

function getSafeRange(doc: ProseMirrorNode, range: EditorSelectionRange): EditorSelectionRange | null {
  if (
    !Number.isInteger(range.from) ||
    !Number.isInteger(range.to) ||
    range.from < 1 ||
    range.to <= range.from ||
    range.to > doc.content.size
  ) {
    return null;
  }

  return range;
}

function isValidAnchorRange(range: EditorSelectionRange) {
  return Number.isInteger(range.from) && Number.isInteger(range.to) && range.from < range.to;
}

function orphaned(
  reason: AnchorRelocationResult["reason"],
  candidates?: number,
): AnchorRelocationResult {
  return {
    status: "orphaned",
    confidence: "none",
    reason,
    ...(candidates === undefined ? {} : { candidates }),
  };
}

function arraysEqual(left: readonly unknown[], right: readonly unknown[]) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}
