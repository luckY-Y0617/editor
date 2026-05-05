export type AnchorMatchResult = {
  status: "active" | "stale" | "orphaned";
  confidence: "exact" | "high" | "medium" | "low" | "none";
  reason:
    | "exact_match"
    | "minor_text_change"
    | "major_text_change"
    | "empty_range"
    | "invalid_range"
    | "quote_not_found";
  similarity?: number;
  editDistance?: number;
};

type MatchCommentAnchorTextInput = {
  mappedText: string;
  normalizedExact: string;
  rangeValid: boolean;
};

const HIGH_SIMILARITY_THRESHOLD = 0.85;
const MEDIUM_SIMILARITY_THRESHOLD = 0.5;
const SMALL_EDIT_DISTANCE_THRESHOLD = 2;
const TINY_TEXT_LENGTH = 3;

export function matchCommentAnchorText({
  mappedText,
  normalizedExact,
  rangeValid,
}: MatchCommentAnchorTextInput): AnchorMatchResult {
  if (!rangeValid) {
    return {
      status: "orphaned",
      confidence: "none",
      reason: "invalid_range",
    };
  }

  const originalText = normalizePlainTextV1(normalizedExact);
  const currentText = normalizePlainTextV1(mappedText);

  if (!originalText) {
    return {
      status: "orphaned",
      confidence: "none",
      reason: "quote_not_found",
    };
  }

  if (!currentText) {
    return {
      status: "orphaned",
      confidence: "none",
      reason: "empty_range",
    };
  }

  const editDistance = getLevenshteinDistance(originalText, currentText);
  const similarity = getNormalizedSimilarity(originalText, currentText, editDistance);

  if (similarity === 1) {
    return {
      status: "active",
      confidence: "exact",
      reason: "exact_match",
      similarity,
      editDistance,
    };
  }

  if (
    originalText.length > TINY_TEXT_LENGTH &&
    (editDistance <= SMALL_EDIT_DISTANCE_THRESHOLD || similarity >= HIGH_SIMILARITY_THRESHOLD)
  ) {
    return {
      status: "active",
      confidence: "high",
      reason: "minor_text_change",
      similarity,
      editDistance,
    };
  }

  if (similarity >= MEDIUM_SIMILARITY_THRESHOLD) {
    return {
      status: "stale",
      confidence: "medium",
      reason: "major_text_change",
      similarity,
      editDistance,
    };
  }

  return {
    status: "stale",
    confidence: "low",
    reason: "major_text_change",
    similarity,
    editDistance,
  };
}

export function normalizePlainTextV1(text: string) {
  return text.replace(/\s+/g, " ").trim();
}

export function getNormalizedSimilarity(leftText: string, rightText: string, editDistance?: number) {
  const maxLength = Math.max(leftText.length, rightText.length);

  if (maxLength === 0) {
    return 1;
  }

  const distance = editDistance ?? getLevenshteinDistance(leftText, rightText);

  return roundSimilarity(Math.max(0, 1 - distance / maxLength));
}

export function getLevenshteinDistance(leftText: string, rightText: string) {
  if (leftText === rightText) {
    return 0;
  }

  if (leftText.length === 0) {
    return rightText.length;
  }

  if (rightText.length === 0) {
    return leftText.length;
  }

  const previousRow = Array.from({ length: rightText.length + 1 }, (_, index) => index);
  const currentRow = new Array<number>(rightText.length + 1);

  for (let leftIndex = 1; leftIndex <= leftText.length; leftIndex += 1) {
    currentRow[0] = leftIndex;

    for (let rightIndex = 1; rightIndex <= rightText.length; rightIndex += 1) {
      const substitutionCost = leftText[leftIndex - 1] === rightText[rightIndex - 1] ? 0 : 1;

      currentRow[rightIndex] = Math.min(
        currentRow[rightIndex - 1] + 1,
        previousRow[rightIndex] + 1,
        previousRow[rightIndex - 1] + substitutionCost,
      );
    }

    for (let index = 0; index < previousRow.length; index += 1) {
      previousRow[index] = currentRow[index];
    }
  }

  return previousRow[rightText.length];
}

function roundSimilarity(value: number) {
  return Math.round(value * 1000) / 1000;
}
