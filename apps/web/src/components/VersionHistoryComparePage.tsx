import {
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  Clock3,
  Download,
  FileText,
  RotateCcw,
  Scale,
  UserRound,
} from "lucide-react";
import { useEffect, useMemo, useState, type CSSProperties, type ReactNode } from "react";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import {
  compareDocumentVersions,
  getDocument,
  getDocumentVersion,
  getDocumentVersions,
  restoreDocumentVersion,
  type CompareDocumentVersionsResponse,
  type DocumentVersionCompareSegmentDto,
  type DocumentVersionCompareTokenDto,
  type DocumentVersionSummaryDto,
  type KnowledgeDocumentDto,
} from "../lib/appApi";
import { formatApiOperationError, getConfiguredApiBaseUrl, isUuid } from "../lib/apiClient";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

const versionComparePatternStyle = {
  "--version-compare-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--version-compare-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

type VersionHistoryLoadState = "error" | "idle" | "loading" | "ready" | "unconfigured";
type VersionCompareViewMode = "side-by-side" | "unified";
type DiffCellKind = "added" | "modified" | "removed" | "unchanged";
type SideBySideDiffRow = {
  kind: string;
  leftKind: DiffCellKind;
  leftText: string;
  leftTokens: DocumentVersionCompareTokenDto[];
  rightKind: DiffCellKind;
  rightText: string;
  rightTokens: DocumentVersionCompareTokenDto[];
};

export function VersionHistoryComparePage() {
  const documentId = getDocumentIdFromCurrentHash();
  const editorHref = documentId ? `#editor?documentId=${encodeURIComponent(documentId)}` : "#editor";
  const apiConfigured = Boolean(getConfiguredApiBaseUrl());
  const [document, setDocument] = useState<KnowledgeDocumentDto | null>(null);
  const [versions, setVersions] = useState<DocumentVersionSummaryDto[]>([]);
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null);
  const [compareResult, setCompareResult] = useState<CompareDocumentVersionsResponse | null>(null);
  const [loadState, setLoadState] = useState<VersionHistoryLoadState>("idle");
  const [error, setError] = useState<string | null>(null);
  const [operation, setOperation] = useState<"compare" | "download" | "restore" | null>(null);
  const [operationMessage, setOperationMessage] = useState<{ tone: "error" | "success"; text: string } | null>(null);
  const [viewMode, setViewMode] = useState<VersionCompareViewMode>("side-by-side");

  useEffect(() => {
    if (!documentId) {
      setLoadState("error");
      setError("Open version history from a document so the document context is available.");
      return;
    }

    if (!apiConfigured) {
      setLoadState("unconfigured");
      setError("Configure the document API before loading live version history.");
      return;
    }

    const controller = new AbortController();
    setLoadState("loading");
    setError(null);
    setOperationMessage(null);

    Promise.all([getDocument(documentId, controller.signal), getDocumentVersions(documentId, controller.signal)])
      .then(([documentResponse, versionsResponse]) => {
        if (controller.signal.aborted) {
          return;
        }

        const orderedVersions = [...versionsResponse.versions].sort((left, right) => right.versionNo - left.versionNo);
        setDocument(documentResponse.document);
        setVersions(orderedVersions);
        setSelectedVersionId((currentId) =>
          currentId && orderedVersions.some((version) => version.id === currentId) ? currentId : orderedVersions[0]?.id ?? null,
        );
        setLoadState("ready");
      })
      .catch((loadError: unknown) => {
        if (controller.signal.aborted) {
          return;
        }

        setDocument(null);
        setVersions([]);
        setSelectedVersionId(null);
        setLoadState("error");
        setError(formatVersionApiError(loadError, "Version history could not be loaded."));
      });

    return () => controller.abort();
  }, [apiConfigured, documentId]);

  useEffect(() => {
    if (!apiConfigured || !documentId || !selectedVersionId) {
      setCompareResult(null);
      return;
    }

    const controller = new AbortController();
    setOperation("compare");

    compareDocumentVersions(
      documentId,
      {
        from: { type: "version", versionId: selectedVersionId },
        to: { type: "draft", versionId: null },
      },
      controller.signal,
    )
      .then((response) => {
        if (!controller.signal.aborted) {
          setCompareResult(response);
        }
      })
      .catch((compareError: unknown) => {
        if (!controller.signal.aborted) {
          setCompareResult(null);
          setOperationMessage({
            tone: "error",
            text: formatVersionApiError(compareError, "Selected version could not be compared with the current draft."),
          });
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setOperation(null);
        }
      });

    return () => controller.abort();
  }, [apiConfigured, documentId, selectedVersionId]);

  const selectedVersion = useMemo(
    () => versions.find((version) => version.id === selectedVersionId) ?? versions[0] ?? null,
    [selectedVersionId, versions],
  );

  const handleRestoreSelectedVersion = async () => {
    if (!documentId || !document || !selectedVersion || operation) {
      return;
    }

    setOperation("restore");
    setOperationMessage(null);

    try {
      const response = await restoreDocumentVersion(documentId, selectedVersion.id, { baseRevision: document.revision });
      setDocument(response.document);
      setOperationMessage({ tone: "success", text: `Restored ${response.restoredFrom.label} to the current draft.` });
    } catch (restoreError) {
      setOperationMessage({
        tone: "error",
        text: formatVersionApiError(restoreError, "Selected version could not be restored."),
      });
    } finally {
      setOperation(null);
    }
  };

  const handleDownloadSelectedVersion = async () => {
    if (!documentId || !selectedVersion || operation) {
      return;
    }

    setOperation("download");
    setOperationMessage(null);

    try {
      const response = await getDocumentVersion(documentId, selectedVersion.id);
      const payload = JSON.stringify(response, null, 2);
      const blob = new Blob([payload], { type: "application/json" });
      const url = URL.createObjectURL(blob);
      const link = window.document.createElement("a");
      link.href = url;
      link.download = `${sanitizeDownloadName(document?.title ?? "document")}-${sanitizeDownloadName(selectedVersion.label)}.json`;
      link.click();
      URL.revokeObjectURL(url);
      setOperationMessage({ tone: "success", text: `Downloaded ${selectedVersion.label}.` });
    } catch (downloadError) {
      setOperationMessage({
        tone: "error",
        text: formatVersionApiError(downloadError, "Selected version could not be downloaded."),
      });
    } finally {
      setOperation(null);
    }
  };

  return (
    <main className="version-compare-shell flex h-screen flex-col overflow-hidden" style={versionComparePatternStyle}>
      <WorkspaceHomeTopBar />
      <div className="version-compare-body min-h-0 flex-1 overflow-hidden">
        <VersionHistoryPanel
          loadState={loadState}
          selectedVersionId={selectedVersion?.id ?? null}
          versions={versions}
          onSelectVersion={setSelectedVersionId}
        />

        <section className="version-compare-main editor-scrollbar min-w-0 overflow-y-auto">
          <div className="version-compare-main-inner">
            <VersionCompareHeader
              documentTitle={document?.title}
              editorHref={editorHref}
              selectedVersion={selectedVersion}
              viewMode={viewMode}
              onViewModeChange={setViewMode}
            />

            {loadState === "loading" ? (
              <VersionStateMessage>Loading live version history...</VersionStateMessage>
            ) : loadState === "unconfigured" || loadState === "error" ? (
              <VersionStateMessage tone="error">{error}</VersionStateMessage>
            ) : versions.length === 0 ? (
              <VersionStateMessage>No published versions yet. Publish a version from the editor toolbar.</VersionStateMessage>
            ) : (
              <VersionDiffPanel
                compareResult={compareResult}
                document={document}
                operation={operation}
                selectedVersion={selectedVersion}
                viewMode={viewMode}
              />
            )}
          </div>

          <DiffLegend compareResult={compareResult} />
        </section>

        <VersionDetailsPanel
          compareResult={compareResult}
          document={document}
          editorHref={editorHref}
          operation={operation}
          operationMessage={operationMessage}
          selectedVersion={selectedVersion}
          onDownloadSelectedVersion={handleDownloadSelectedVersion}
          onRestoreSelectedVersion={handleRestoreSelectedVersion}
        />
      </div>
    </main>
  );
}

function VersionHistoryPanel({
  loadState,
  selectedVersionId,
  versions,
  onSelectVersion,
}: {
  loadState: VersionHistoryLoadState;
  selectedVersionId: string | null;
  versions: DocumentVersionSummaryDto[];
  onSelectVersion: (versionId: string) => void;
}) {
  return (
    <aside className="version-compare-history editor-scrollbar overflow-y-auto">
      <div className="version-compare-ruler" aria-hidden="true">
        <span>N 90</span>
        <span>N 60</span>
        <span>N 30</span>
        <span>0</span>
        <span>S 30</span>
        <span>S 60</span>
        <span>S 90</span>
      </div>
      <div className="version-compare-history-inner">
        <h2>Version History</h2>
        <button className="version-compare-filter" disabled title="Version filtering is not exposed by the current backend contract" type="button">
          All Versions
          <ChevronDown className="h-4 w-4" />
        </button>

        <div className="version-compare-version-list" aria-label="Version timeline">
          {versions.map((item) => (
            <VersionHistoryRow
              isSelected={item.id === selectedVersionId}
              item={item}
              key={item.id}
              onSelectVersion={onSelectVersion}
            />
          ))}
        </div>

        {loadState === "ready" && versions.length === 0 ? (
          <p className="version-compare-muted-copy">No versions have been published for this document.</p>
        ) : null}
      </div>
    </aside>
  );
}

function VersionHistoryRow({
  isSelected,
  item,
  onSelectVersion,
}: {
  isSelected: boolean;
  item: DocumentVersionSummaryDto;
  onSelectVersion: (versionId: string) => void;
}) {
  const author = formatRealAuthor(item.createdBy);

  return (
    <button
      className={["version-compare-history-row", isSelected ? "is-compared" : ""].join(" ")}
      onClick={() => onSelectVersion(item.id)}
      title={`Compare ${item.label} with current draft`}
      type="button"
    >
      <span className="version-compare-dot" aria-hidden="true" />
      <div className="min-w-0 text-left">
        <div className="version-compare-history-title">
          <span>{item.label}</span>
          {isSelected ? <strong>Currently viewing</strong> : null}
          <span>{formatVersionDate(item.publishedAt ?? item.createdAt)}</span>
        </div>
        {author ? <p>{author}</p> : null}
      </div>
      <span className={["version-compare-status-text", item.versionType.toLowerCase() === "published" ? "" : "is-draft"].join(" ")}>
        {formatVersionType(item.versionType)}
      </span>
    </button>
  );
}

function VersionCompareHeader({
  documentTitle,
  editorHref,
  selectedVersion,
  viewMode,
  onViewModeChange,
}: {
  documentTitle?: string;
  editorHref: string;
  selectedVersion: DocumentVersionSummaryDto | null;
  viewMode: VersionCompareViewMode;
  onViewModeChange: (mode: VersionCompareViewMode) => void;
}) {
  return (
    <header className="version-compare-heading">
      <div className="version-compare-status-strip">
        <Scale className="h-4 w-4" />
        <span>
          Comparing <strong>{selectedVersion?.label ?? "selected version"}</strong> with <strong>Current draft</strong>
        </span>
      </div>
      <nav className="version-compare-inline-breadcrumbs" aria-label="Version document location">
        <a href={editorHref}>Atlas Library</a>
        <span aria-hidden="true">/</span>
        <a href={editorHref}>{documentTitle?.trim() || "Document"}</a>
        <span aria-hidden="true">/</span>
        <span>Version History</span>
      </nav>
      <div className="version-compare-heading-main">
        <div className="min-w-0">
          <h1>Version history</h1>
        </div>
      </div>
      <div className="version-compare-heading-tools">
        <div className="version-compare-mode" aria-label="Compare view mode">
          <button
            className={viewMode === "side-by-side" ? "is-active" : ""}
            onClick={() => onViewModeChange("side-by-side")}
            title="Side-by-side compare view"
            type="button"
          >
            Side-by-side
          </button>
          <button
            className={viewMode === "unified" ? "is-active" : ""}
            onClick={() => onViewModeChange("unified")}
            title="Unified diff view"
            type="button"
          >
            Unified diff
          </button>
        </div>
        <DiffInlineLegend />
      </div>
    </header>
  );
}

function VersionDiffPanel({
  compareResult,
  document,
  operation,
  selectedVersion,
  viewMode,
}: {
  compareResult: CompareDocumentVersionsResponse | null;
  document: KnowledgeDocumentDto | null;
  operation: "compare" | "download" | "restore" | null;
  selectedVersion: DocumentVersionSummaryDto | null;
  viewMode: VersionCompareViewMode;
}) {
  if (operation === "compare" && !compareResult) {
    return <VersionStateMessage>Comparing selected version with the current draft...</VersionStateMessage>;
  }

  if (!selectedVersion) {
    return <VersionStateMessage>Select a version to compare with the current draft.</VersionStateMessage>;
  }

  if (!compareResult) {
    return <VersionStateMessage>Compare output is unavailable for the selected version.</VersionStateMessage>;
  }

  if (viewMode === "side-by-side") {
    return <SideBySideDiff compareResult={compareResult} document={document} selectedVersion={selectedVersion} />;
  }

  return (
    <article className="version-compare-document-card is-unified">
      <header className="version-compare-document-meta">
        <div className="min-w-0">
          <span className="version-compare-version-number">{compareResult.summary.fromLabel}</span>
          <span>to {compareResult.summary.toLabel}</span>
          <p>
            {compareResult.summary.textChanged
              ? `${compareResult.summary.addedSegments} additions, ${compareResult.summary.removedSegments} removals`
              : "No text changes detected"}
          </p>
        </div>
        <StatusBadge status={formatVersionType(selectedVersion.versionType)} />
      </header>

      <div className="version-compare-document-copy">
        <h2>{selectedVersion.label}</h2>
        <h3>Compared with current draft</h3>
        <div className="version-compare-rule" aria-hidden="true" />
        <div className="version-compare-unified-diff">
          {getMeaningfulCompareLines(compareResult).length > 0 ? (
            <UnifiedDiffLines compareResult={compareResult} />
          ) : compareResult.segments.length > 0 ? (
            compareResult.segments.map((segment, index) => <DiffSegment key={`${segment.kind}-${index}`} segment={segment} />)
          ) : (
            <p className="version-compare-diff-paragraph">No textual diff segments were returned by the backend.</p>
          )}
        </div>
      </div>
    </article>
  );
}

function SideBySideDiff({
  compareResult,
  document,
  selectedVersion,
}: {
  compareResult: CompareDocumentVersionsResponse;
  document: KnowledgeDocumentDto | null;
  selectedVersion: DocumentVersionSummaryDto;
}) {
  const rows = buildSideBySideRows(compareResult);
  const updatedAt = document?.updatedAt ? formatVersionDateTime(document.updatedAt) : "Current draft";

  return (
    <article className="version-compare-side-by-side-card">
      <div className="version-compare-side-header">
        <div className="version-compare-side-header-spacer" aria-hidden="true" />
        <div>
          <strong>{compareResult.summary.fromLabel || selectedVersion.label}</strong>
          <span>
            {formatVersionType(selectedVersion.versionType)} · {formatVersionDateTime(selectedVersion.publishedAt ?? selectedVersion.createdAt)}
          </span>
        </div>
        <div>
          <strong>{compareResult.summary.toLabel || "Current draft"}</strong>
          <span>{updatedAt}</span>
        </div>
      </div>
      <div className="version-compare-side-grid">
        {rows.map((row, index) => (
          <div className="version-compare-side-row" key={`${row.kind}-${index}`}>
            <div className="version-compare-line-number">{index + 1}</div>
            <DiffCell kind={row.leftKind} text={row.leftText} tokens={row.leftTokens} />
            <DiffCell kind={row.rightKind} text={row.rightText} tokens={row.rightTokens} />
          </div>
        ))}
      </div>
    </article>
  );
}

function DiffCell({
  kind,
  text,
  tokens,
}: {
  kind: DiffCellKind;
  text: string;
  tokens: DocumentVersionCompareTokenDto[];
}) {
  return (
    <div className={["version-compare-side-cell", kind !== "unchanged" ? `is-${kind}` : ""].join(" ")}>
      {kind !== "unchanged" ? <span aria-hidden="true">{kind === "added" ? "+" : kind === "removed" ? "-" : "~"}</span> : <span aria-hidden="true" />}
      <p>{renderInlineDiffTokens(tokens, text, kind)}</p>
    </div>
  );
}

function DiffSegment({ segment }: { segment: DocumentVersionCompareSegmentDto }) {
  const tone = segment.kind === "added" || segment.kind === "removed" ? segment.kind : undefined;
  const marker = segment.kind === "added" ? "+" : segment.kind === "removed" ? "-" : " ";

  return (
    <>
      {splitDisplayText(segment.text).map((text, index) => (
        <p className={["version-compare-diff-line", tone ? `is-${tone}` : ""].join(" ")} key={`${segment.kind}-${index}`}>
          <span aria-hidden="true">{marker}</span>
          <DiffToken tone={tone}>{text}</DiffToken>
        </p>
      ))}
    </>
  );
}

function UnifiedDiffLines({ compareResult }: { compareResult: CompareDocumentVersionsResponse }) {
  const lines = getMeaningfulCompareLines(compareResult);

  return (
    <>
      {lines.map((line, index) => {
        if (line.kind === "modified") {
          return (
            <div className="version-compare-modified-pair" key={`modified-${index}`}>
              <UnifiedDiffLine kind="removed" marker="-" text={line.leftText ?? ""} tokens={line.leftTokens} />
              <UnifiedDiffLine kind="added" marker="+" text={line.rightText ?? ""} tokens={line.rightTokens} />
            </div>
          );
        }

        if (line.kind === "added") {
          return <UnifiedDiffLine kind="added" marker="+" key={`added-${index}`} text={line.rightText ?? ""} tokens={line.rightTokens} />;
        }

        if (line.kind === "removed") {
          return <UnifiedDiffLine kind="removed" marker="-" key={`removed-${index}`} text={line.leftText ?? ""} tokens={line.leftTokens} />;
        }

        return <UnifiedDiffLine kind="unchanged" marker=" " key={`unchanged-${index}`} text={line.leftText ?? line.rightText ?? ""} tokens={line.leftTokens} />;
      })}
    </>
  );
}

function UnifiedDiffLine({
  kind,
  marker,
  text,
  tokens,
}: {
  kind: DiffCellKind;
  marker: string;
  text: string;
  tokens: DocumentVersionCompareTokenDto[];
}) {
  return (
    <p className={["version-compare-diff-line", kind !== "unchanged" ? `is-${kind}` : ""].join(" ")}>
      <span aria-hidden="true">{marker}</span>
      <span>{renderInlineDiffTokens(tokens, text, kind)}</span>
    </p>
  );
}

function DiffToken({ children, tone }: { children: ReactNode; tone?: "added" | "removed" }) {
  return <span className={["version-compare-diff-token", tone ? `is-${tone}` : ""].join(" ")}>{children}</span>;
}

function DiffLegend({ compareResult }: { compareResult: CompareDocumentVersionsResponse | null }) {
  return (
    <footer className="version-compare-legend">
      <span>
        <i className="is-added" />
        Added
      </span>
      <span>
        <i className="is-removed" />
        Removed
      </span>
      <span>
        <i className="is-modified" />
        Modified
      </span>
      <strong>
        {compareResult
          ? `${compareResult.summary.addedSegments} additions, ${compareResult.summary.removedSegments} removals, ${compareResult.summary.wordCountDelta} word delta.`
          : "Live compare output loads after a version is selected."}
      </strong>
    </footer>
  );
}

function DiffInlineLegend() {
  return (
    <div className="version-compare-inline-legend" aria-label="Diff legend">
      <span>
        <i className="is-added" />
        Added
      </span>
      <span>
        <i className="is-removed" />
        Removed
      </span>
      <span>
        <i className="is-modified" />
        Modified
      </span>
    </div>
  );
}

function VersionDetailsPanel({
  compareResult,
  document,
  editorHref,
  operation,
  operationMessage,
  selectedVersion,
  onDownloadSelectedVersion,
  onRestoreSelectedVersion,
}: {
  compareResult: CompareDocumentVersionsResponse | null;
  document: KnowledgeDocumentDto | null;
  editorHref: string;
  operation: "compare" | "download" | "restore" | null;
  operationMessage: { tone: "error" | "success"; text: string } | null;
  selectedVersion: DocumentVersionSummaryDto | null;
  onDownloadSelectedVersion: () => void;
  onRestoreSelectedVersion: () => void;
}) {
  const author = formatRealAuthor(selectedVersion?.createdBy);
  const draftWordCount = selectedVersion && compareResult ? selectedVersion.wordCount + compareResult.summary.wordCountDelta : null;

  return (
    <aside className="version-compare-details editor-scrollbar overflow-y-auto">
      <div className="version-compare-details-inner">
        <h2>Compare Details</h2>
        <section className="version-compare-detail-card">
          <header className="version-compare-detail-title">
            <span>{selectedVersion?.label ?? "No version selected"}</span>
            {selectedVersion ? <StatusBadge status={formatVersionType(selectedVersion.versionType)} /> : null}
          </header>

          <dl className="version-compare-detail-list">
            {author ? (
              <div>
                <dt>
                  <UserRound className="h-4 w-4" />
                  Author
                </dt>
                <dd>{author}</dd>
              </div>
            ) : null}
            <div>
              <dt>
                <Clock3 className="h-4 w-4" />
                Updated
              </dt>
              <dd>{selectedVersion ? formatVersionDate(selectedVersion.publishedAt ?? selectedVersion.createdAt) : "Not available"}</dd>
            </div>
            <div>
              <dt>
                <CheckCircle2 className="h-4 w-4" />
                Status
              </dt>
              <dd>{selectedVersion ? formatVersionType(selectedVersion.versionType) : "Not available"}</dd>
            </div>
          </dl>

          <VersionDetailSection title="Related Document">
            <a className="version-compare-detail-link" href={editorHref}>
              <FileText className="h-4 w-4" />
              {document?.title ?? "Back to document"}
            </a>
          </VersionDetailSection>

          <VersionDetailSection title="Comparing With">
            <p>Current draft{document?.updatedAt ? ` · ${formatVersionDateTime(document.updatedAt)}` : ""}</p>
          </VersionDetailSection>

          <VersionDetailSection title="Version Data Summary">
            <dl className="version-compare-data-summary">
              <div>
                <dt>Words</dt>
                <dd>
                  {selectedVersion ? selectedVersion.wordCount : "-"} {draftWordCount === null ? "" : `-> ${draftWordCount}`}{" "}
                  {compareResult ? <strong>{formatSignedNumber(compareResult.summary.wordCountDelta)}</strong> : null}
                </dd>
              </div>
              <div>
                <dt>Added segments</dt>
                <dd>{compareResult?.summary.addedSegments ?? "-"}</dd>
              </div>
              <div>
                <dt>Removed segments</dt>
                <dd>{compareResult?.summary.removedSegments ?? "-"}</dd>
              </div>
              <div>
                <dt>Blocks</dt>
                <dd title="The current backend compare contract returns text segments, not block-level counts.">Not available</dd>
              </div>
              <div>
                <dt>Links</dt>
                <dd title="The current backend compare contract returns text segments, not link-level counts.">Not available</dd>
              </div>
              <div>
                <dt>Attachments</dt>
                <dd title="The current backend compare contract returns text segments, not attachment-level counts.">Not available</dd>
              </div>
            </dl>
          </VersionDetailSection>
        </section>

        {operationMessage ? (
          <VersionStateMessage tone={operationMessage.tone === "error" ? "error" : "success"}>{operationMessage.text}</VersionStateMessage>
        ) : null}

        <button
          className="version-compare-detail-action is-primary"
          disabled={!selectedVersion || !document || Boolean(operation)}
          onClick={onRestoreSelectedVersion}
          title="Restore selected version into the current draft"
          type="button"
        >
          <RotateCcw className="h-4 w-4" />
          {operation === "restore" ? "Restoring..." : "Restore to draft"}
        </button>
        <button
          className="version-compare-detail-action"
          disabled={!selectedVersion || Boolean(operation)}
          onClick={onDownloadSelectedVersion}
          title="Download the selected immutable version snapshot"
          type="button"
        >
          <Download className="h-4 w-4" />
          {operation === "download" ? "Downloading..." : "Download this version"}
        </button>
        <p className="version-compare-action-note">
          Restoring will replace the current draft with the selected version snapshot.
        </p>
      </div>
    </aside>
  );
}

function VersionDetailSection({ children, title }: { children: ReactNode; title: string }) {
  return (
    <section className="version-compare-detail-section">
      <h3>{title}</h3>
      {children}
    </section>
  );
}

function VersionStateMessage({ children, tone = "muted" }: { children: ReactNode; tone?: "error" | "muted" | "success" }) {
  return (
    <div className={["version-compare-state", tone === "error" ? "is-error" : tone === "success" ? "is-success" : ""].join(" ")}>
      {tone === "error" ? <AlertTriangle className="h-4 w-4" /> : null}
      <span>{children}</span>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  return (
    <span className={["version-compare-status-badge", status === "Draft" ? "is-draft" : ""].join(" ")}>
      {status}
    </span>
  );
}

function getDocumentIdFromCurrentHash() {
  const query = window.location.hash.split("?")[1] ?? "";
  return new URLSearchParams(query).get("documentId");
}

function formatRealAuthor(value?: string | null) {
  const trimmed = value?.trim();

  if (!trimmed || isUuid(trimmed)) {
    return undefined;
  }

  return trimmed;
}

function formatVersionDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(date);
}

function formatVersionDateTime(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("en-US", {
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    month: "short",
    year: "numeric",
  }).format(date);
}

function formatVersionType(value: string) {
  const normalized = value.trim().toLowerCase();

  if (normalized === "published") {
    return "Published";
  }

  if (normalized === "manual") {
    return "Manual";
  }

  if (normalized === "imported") {
    return "Imported";
  }

  if (normalized === "system") {
    return "System";
  }

  return "Draft";
}

function formatVersionApiError(error: unknown, fallback: string) {
  return formatApiOperationError(error, fallback, {
    forbidden: "You do not have permission to manage document versions.",
    network: "Could not reach the document version API. Check the backend session and retry.",
    unauthorized: "Sign in again to manage document versions.",
    unconfigured: "Document API is not configured for this environment.",
  });
}

function getCompareLines(compareResult: CompareDocumentVersionsResponse) {
  return Array.isArray(compareResult.lines) ? compareResult.lines : [];
}

function getMeaningfulCompareLines(compareResult: CompareDocumentVersionsResponse) {
  return getCompareLines(compareResult).filter((line) => hasMeaningfulCompareText(line.leftText, line.rightText));
}

function buildSideBySideRows(compareResult: CompareDocumentVersionsResponse): SideBySideDiffRow[] {
  const sourceLines = getCompareLines(compareResult);
  const lines = getMeaningfulCompareLines(compareResult);

  if (sourceLines.length > 0) {
    return lines.map((line) => {
      if (line.kind === "added") {
        return {
          kind: "added",
          leftKind: "unchanged",
          leftText: "",
          leftTokens: [],
          rightKind: "added",
          rightText: line.rightText ?? "",
          rightTokens: normalizeCompareTokens(line.rightTokens, line.rightText ?? "", "added"),
        };
      }

      if (line.kind === "removed") {
        return {
          kind: "removed",
          leftKind: "removed",
          leftText: line.leftText ?? "",
          leftTokens: normalizeCompareTokens(line.leftTokens, line.leftText ?? "", "removed"),
          rightKind: "unchanged",
          rightText: "",
          rightTokens: [],
        };
      }

      if (line.kind === "modified") {
        return {
          kind: "modified",
          leftKind: "modified",
          leftText: line.leftText ?? "",
          leftTokens: normalizeCompareTokens(line.leftTokens, line.leftText ?? "", "removed"),
          rightKind: "modified",
          rightText: line.rightText ?? "",
          rightTokens: normalizeCompareTokens(line.rightTokens, line.rightText ?? "", "added"),
        };
      }

      return {
        kind: "unchanged",
        leftKind: "unchanged",
        leftText: line.leftText ?? "",
        leftTokens: normalizeCompareTokens(line.leftTokens, line.leftText ?? "", "unchanged"),
        rightKind: "unchanged",
        rightText: line.rightText ?? line.leftText ?? "",
        rightTokens: normalizeCompareTokens(line.rightTokens, line.rightText ?? line.leftText ?? "", "unchanged"),
      };
    });
  }

  return buildFallbackSideBySideRows(compareResult.segments);
}

function buildFallbackSideBySideRows(segments: DocumentVersionCompareSegmentDto[]): SideBySideDiffRow[] {
  const rows: SideBySideDiffRow[] = [];

  for (let index = 0; index < segments.length; index += 1) {
    const segment = segments[index];
    const units = splitDisplayText(segment.text);

    if (segment.kind === "added") {
      rows.push(
        ...units.map((text) => ({
          kind: "added",
          leftKind: "unchanged" as const,
          leftText: "",
          leftTokens: [],
          rightKind: "added" as const,
          rightText: text,
          rightTokens: [{ kind: "added", text }],
        })),
      );
      continue;
    }

    if (segment.kind === "removed") {
      rows.push(
        ...units.map((text) => ({
          kind: "removed",
          leftKind: "removed" as const,
          leftText: text,
          leftTokens: [{ kind: "removed", text }],
          rightKind: "unchanged" as const,
          rightText: "",
          rightTokens: [],
        })),
      );
      continue;
    }

    rows.push(
      ...units.map((text) => ({
        kind: "unchanged",
        leftKind: "unchanged" as const,
        leftText: text,
        leftTokens: [{ kind: "unchanged", text }],
        rightKind: "unchanged" as const,
        rightText: text,
        rightTokens: [{ kind: "unchanged", text }],
      })),
    );
  }

  return rows;
}

function normalizeCompareTokens(
  tokens: DocumentVersionCompareTokenDto[] | undefined,
  fallbackText: string,
  fallbackKind: DocumentVersionCompareTokenDto["kind"],
) {
  if (tokens?.length) {
    return tokens;
  }

  return fallbackText ? [{ kind: fallbackKind, text: fallbackText }] : [];
}

function renderInlineDiffTokens(tokens: DocumentVersionCompareTokenDto[], fallbackText: string, fallbackKind: DiffCellKind) {
  const normalizedTokens = normalizeCompareTokens(tokens, fallbackText, fallbackKind);

  if (normalizedTokens.length === 0) {
    return "\u00A0";
  }

  return normalizedTokens.map((token, index) => {
    const kind = token.kind === "added" || token.kind === "removed" || token.kind === "modified" ? token.kind : undefined;

    return (
      <span className={["version-compare-inline-token", kind ? `is-${kind}` : ""].join(" ")} key={`${token.kind}-${index}`}>
        {token.text}
      </span>
    );
  });
}

function splitDisplayText(value: string) {
  const normalized = value.replace(/\r\n/g, "\n").replace(/\r/g, "\n").replace(/\s+/g, " ").trim();

  if (!normalized) {
    return [];
  }

  const units: string[] = [];
  let current = "";

  for (let index = 0; index < normalized.length; index += 1) {
    current += normalized[index];

    if (shouldSplitDisplayUnit(normalized, index, current.length)) {
      units.push(current.trim());
      current = "";
    }
  }

  if (current.trim()) {
    units.push(current.trim());
  }

  const meaningfulUnits = units.filter((unit) => !isNoiseOnlyText(unit));

  if (meaningfulUnits.length > 0) {
    return meaningfulUnits;
  }

  return isNoiseOnlyText(normalized) ? [] : [normalized];
}

function shouldSplitDisplayUnit(text: string, index: number, length: number) {
  const character = text[index];
  const next = text[index + 1] ?? "";
  const previous = text[index - 1] ?? "";

  if (isStrongDisplayBreak(character)) {
    return true;
  }

  if (character === "." && !(isDigit(previous) && isDigit(next)) && (!next || /\s/.test(next))) {
    return true;
  }

  if (length >= 96 && isSoftDisplayBreak(character)) {
    return true;
  }

  return length >= 150;
}

function isStrongDisplayBreak(character: string) {
  return (
    character === "\u3002" ||
    character === "\uff01" ||
    character === "\uff1f" ||
    character === "\uff1b" ||
    character === "!" ||
    character === "?" ||
    character === ";"
  );
}

function isSoftDisplayBreak(character: string) {
  return (
    character === "\uff0c" ||
    character === "," ||
    character === "\u3001" ||
    character === "\uff1a" ||
    character === ":" ||
    character === ")" ||
    character === "\uff09"
  );
}

function hasMeaningfulCompareText(...values: Array<string | null | undefined>) {
  return values.some((value) => value && !isNoiseOnlyText(value));
}

function isNoiseOnlyText(value: string) {
  return !/[A-Za-z0-9\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]/u.test(value);
}

function isDigit(value: string) {
  return value >= "0" && value <= "9";
}

function formatSignedNumber(value: number) {
  if (value > 0) {
    return `+${value}`;
  }

  return String(value);
}

function sanitizeDownloadName(value: string) {
  return value.trim().replace(/[^a-z0-9._-]+/gi, "-").replace(/^-+|-+$/g, "") || "version";
}
