import {
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Clock3,
  ExternalLink,
  FileText,
  Link2,
  MoreHorizontal,
  RotateCcw,
  Share2,
  UserRound,
  UsersRound,
} from "lucide-react";
import { type CSSProperties, type ReactNode } from "react";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import {
  selectedVersionDetail,
  versionCompareDocuments,
  versionHistoryItems,
  type VersionCompareDocument,
  type VersionDiffBlock,
  type VersionDiffToken,
  type VersionHistoryItem,
  type VersionStatus,
} from "../data/versionHistoryData";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

const versionComparePatternStyle = {
  "--version-compare-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--version-compare-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

const breadcrumbs = ["Atlas Library", "01. Foundations", "Mission & Vision", "Version History / Compare"];

export function VersionHistoryComparePage() {
  const [leftDocument, rightDocument] = versionCompareDocuments;

  return (
    <main className="version-compare-shell flex h-screen flex-col overflow-hidden" style={versionComparePatternStyle}>
      <WorkspaceHomeTopBar />
      <VersionBreadcrumbs />
      <div className="version-compare-body min-h-0 flex-1 overflow-hidden">
        <VersionHistoryPanel />

        <section className="version-compare-main editor-scrollbar min-w-0 overflow-y-auto">
          <div className="version-compare-main-inner">
            <header className="version-compare-heading">
              <div className="min-w-0">
                <h1>Comparing versions</h1>
                <p>
                  {leftDocument.version} ({leftDocument.date}) <span aria-hidden="true">-&gt;</span>{" "}
                  {rightDocument.version} ({rightDocument.date})
                </p>
              </div>
              <div className="version-compare-mode" aria-label="Compare view mode">
                <button className="is-active" title="Side-by-side compare view" type="button">
                  Side-by-side
                </button>
                <button title="Unified diff will be available after compare API is connected" type="button">
                  Unified diff
                </button>
              </div>
            </header>

            <div className="version-compare-document-grid">
              <VersionDocumentCard document={leftDocument} />
              <VersionDocumentCard document={rightDocument} />
            </div>
          </div>

          <DiffLegend />
        </section>

        <VersionDetailsPanel />
      </div>
    </main>
  );
}

function VersionBreadcrumbs() {
  return (
    <header className="version-compare-breadcrumb-row">
      <nav className="version-compare-breadcrumbs" aria-label="Document location">
        {breadcrumbs.map((breadcrumb, index) => {
          const isLast = index === breadcrumbs.length - 1;

          return (
            <span className={isLast ? "is-current" : ""} key={breadcrumb}>
              <a href={isLast ? "#versions" : "#editor"}>{breadcrumb}</a>
              {!isLast ? <ChevronRight className="h-4 w-4" aria-hidden="true" /> : null}
            </span>
          );
        })}
      </nav>
      <div className="version-compare-breadcrumb-actions">
        <button title="Share version compare" type="button">
          <Share2 className="h-4 w-4" />
          Share
        </button>
        <button aria-label="More version actions" title="More version actions" type="button">
          <MoreHorizontal className="h-5 w-5" />
        </button>
      </div>
    </header>
  );
}

function VersionHistoryPanel() {
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
        <button className="version-compare-filter" title="Filter versions" type="button">
          All Versions
          <ChevronDown className="h-4 w-4" />
        </button>

        <div className="version-compare-version-list" aria-label="Version timeline">
          {versionHistoryItems.map((item) => (
            <VersionHistoryRow item={item} key={item.id} />
          ))}
        </div>

        <button className="version-compare-primary-button" title="Compare selected versions" type="button">
          Compare versions
        </button>
      </div>
    </aside>
  );
}

function VersionHistoryRow({ item }: { item: VersionHistoryItem }) {
  return (
    <article className={["version-compare-history-row", item.isCompared ? "is-compared" : ""].join(" ")}>
      <span className="version-compare-dot" aria-hidden="true" />
      <div className="min-w-0">
        <div className="version-compare-history-title">
          <span>{item.version}</span>
          <span>{item.date}</span>
        </div>
        <p>{item.author}</p>
      </div>
      <span className={["version-compare-status-text", item.status === "Draft" ? "is-draft" : ""].join(" ")}>
        {item.status}
      </span>
    </article>
  );
}

function VersionDocumentCard({ document }: { document: VersionCompareDocument }) {
  return (
    <article className="version-compare-document-card">
      <header className="version-compare-document-meta">
        <div className="min-w-0">
          <span className="version-compare-version-number">{document.version}</span>
          <span>{document.date}</span>
          <p>{document.author}</p>
        </div>
        <StatusBadge status={document.status} />
      </header>

      <div className="version-compare-paper-divider" aria-hidden="true">
        <span />
      </div>

      <div className="version-compare-document-copy">
        <h2>{document.title}</h2>
        <h3>{document.sectionTitle}</h3>
        <div className="version-compare-rule" aria-hidden="true" />
        {document.blocks.map((block) => (
          <DiffBlock block={block} key={block.id} />
        ))}
      </div>

      <div className="version-compare-paper-divider is-bottom" aria-hidden="true">
        <span />
      </div>
    </article>
  );
}

function DiffBlock({ block }: { block: VersionDiffBlock }) {
  const tokens = block.tokens.map((token, index) => <DiffToken key={`${block.id}-${index}`} token={token} />);

  if (block.kind === "heading") {
    return <h4 className="version-compare-diff-heading">{tokens}</h4>;
  }

  if (block.kind === "bullet") {
    return <li className="version-compare-diff-bullet">{tokens}</li>;
  }

  return <p className="version-compare-diff-paragraph">{tokens}</p>;
}

function DiffToken({ token }: { token: VersionDiffToken }) {
  return (
    <span className={["version-compare-diff-token", token.tone ? `is-${token.tone}` : ""].join(" ")}>
      {token.text}
    </span>
  );
}

function DiffLegend() {
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
      <strong>3 additions, 2 removals, 1 modification.</strong>
      <label className="version-compare-whitespace">
        Show whitespace
        <input aria-label="Show whitespace" readOnly type="checkbox" />
        <span />
      </label>
    </footer>
  );
}

function VersionDetailsPanel() {
  return (
    <aside className="version-compare-details editor-scrollbar overflow-y-auto">
      <div className="version-compare-details-inner">
        <h2>Version Details</h2>
        <section className="version-compare-detail-card">
          <header className="version-compare-detail-title">
            <span>{selectedVersionDetail.version}</span>
            <StatusBadge status={selectedVersionDetail.status} />
          </header>

          <dl className="version-compare-detail-list">
            <div>
              <dt>
                <UserRound className="h-4 w-4" />
                Author
              </dt>
              <dd>{selectedVersionDetail.author}</dd>
            </div>
            <div>
              <dt>
                <Clock3 className="h-4 w-4" />
                Updated
              </dt>
              <dd>{selectedVersionDetail.updatedAt}</dd>
            </div>
            <div>
              <dt>
                <CheckCircle2 className="h-4 w-4" />
                Status
              </dt>
              <dd>{selectedVersionDetail.status}</dd>
            </div>
          </dl>

          <VersionDetailSection title="Summary">
            <p>{selectedVersionDetail.summary}</p>
          </VersionDetailSection>

          <VersionDetailSection title="Related Document">
            <a className="version-compare-detail-link" href="#editor">
              <FileText className="h-4 w-4" />
              {selectedVersionDetail.relatedDocument}
              <ExternalLink className="h-3.5 w-3.5" />
            </a>
          </VersionDetailSection>

          <VersionDetailSection title="Location">
            <p>{selectedVersionDetail.location.join("  /  ")}</p>
          </VersionDetailSection>

          <VersionDetailSection title="Readers">
            <p className="version-compare-readers">
              <UsersRound className="h-4 w-4" />
              {selectedVersionDetail.readers} readers
            </p>
          </VersionDetailSection>

          <VersionDetailSection title="Tags">
            <div className="version-compare-tags">
              {selectedVersionDetail.tags.map((tag) => (
                <span key={tag}>{tag}</span>
              ))}
            </div>
          </VersionDetailSection>

          <VersionDetailSection title="Linked Versions">
            <div className="version-compare-linked-versions">
              <Link2 className="h-4 w-4" />
              {selectedVersionDetail.linkedVersions.map((version) => (
                <a href="#versions" key={version}>
                  {version}
                </a>
              ))}
            </div>
          </VersionDetailSection>
        </section>

        <button
          aria-disabled="true"
          className="version-compare-detail-action is-primary"
          title="Available after backend restore API is connected"
          type="button"
        >
          <RotateCcw className="h-4 w-4" />
          Restore this version
        </button>
        <button
          aria-disabled="true"
          className="version-compare-detail-action"
          title="Available after backend create version API is connected"
          type="button"
        >
          Create new version
        </button>
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

function StatusBadge({ status }: { status: VersionStatus }) {
  return (
    <span className={["version-compare-status-badge", status === "Draft" ? "is-draft" : ""].join(" ")}>
      {status}
    </span>
  );
}
