import { History, PanelRightClose, PanelRightOpen } from "lucide-react";
import { AtlasIcon } from "./AtlasIcon";
import { DocumentReaderSurface } from "./DocumentReaderSurface";
import { DocumentMeta } from "./DocumentMeta";
import { DocumentTitleEditor } from "./DocumentTitleEditor";
import { TiptapEditor } from "./TiptapEditor";
import checkCircleIcon from "../assets/svg/icons/check-circle.svg";
import chevronRightIcon from "../assets/svg/icons/chevron-right.svg";
import compassEmblemIcon from "../assets/svg/brand/compass-emblem.svg";
import shareIcon from "../assets/svg/icons/share.svg";
import type { JSONContent } from "@tiptap/react";
import type {
  CommentFocusRequest,
  CommentRuntimeAnchorState,
  CommentThread,
  OutlineFocusRequest,
  PendingCommentComposer,
  TiptapContentChange,
  TiptapContentStats,
} from "../types/editor";

type EditorCanvasProps = {
  activeCommentThreadId?: string | null;
  commentFocusRequest?: CommentFocusRequest | null;
  commentThreads?: CommentThread[];
  content: JSONContent;
  documentId: string;
  documentStatusLabel: string;
  folderHref: string;
  folderTitle: string;
  isContentEmpty: boolean;
  isCommentComposerOpen?: boolean;
  isSidePanelCollapsed?: boolean;
  libraryHref: string;
  libraryName: string;
  shareHref: string;
  title: string;
  tags?: string[];
  textLength: number;
  saveStatusLabel: string;
  updatedAtLabel: string;
  version?: string | null;
  outlineFocusRequest: OutlineFocusRequest | null;
  onCommentRuntimeStateChange?: (runtimeState: CommentRuntimeAnchorState) => void;
  onOpenCommentComposer?: (composer: PendingCommentComposer) => void;
  onOpenShare?: () => void;
  onOpenVersions?: () => void;
  onPublishVersion?: () => void;
  onSelectCommentThread?: (threadId: string) => void;
  onTitleChange: (title: string) => void;
  onToggleSidePanel?: () => void;
  onUnpublishVersion?: () => void;
  versionOperation?: "compare" | "publish" | "restore" | "unpublish" | null;
  onContentChange: (change: TiptapContentChange) => void;
  onContentStatsChange: (stats: TiptapContentStats) => void;
};

export function EditorCanvas({
  activeCommentThreadId,
  commentFocusRequest,
  commentThreads,
  content,
  documentId,
  documentStatusLabel,
  folderHref,
  folderTitle,
  isContentEmpty,
  isCommentComposerOpen,
  isSidePanelCollapsed = false,
  libraryHref,
  libraryName,
  onCommentRuntimeStateChange,
  onContentChange,
  onContentStatsChange,
  onOpenCommentComposer,
  onOpenShare,
  onOpenVersions,
  onPublishVersion,
  onSelectCommentThread,
  onToggleSidePanel,
  onUnpublishVersion,
  onTitleChange,
  outlineFocusRequest,
  saveStatusLabel,
  shareHref,
  tags,
  textLength,
  title,
  updatedAtLabel,
  version,
  versionOperation = null,
}: EditorCanvasProps) {
  const displayTitle = title.trim() || "Untitled Field Note";
  const readingMinutes = Math.max(1, Math.ceil(textLength / 850));
  const toolbarSlotId = `atlas-toolbar-${documentId.replace(/[^a-zA-Z0-9_-]/g, "-")}`;
  const folderLabel = folderTitle.trim() || "Folder";
  const isPublished = documentStatusLabel.toLowerCase() === "published";
  const publishButtonLabel = versionOperation === "publish" ? "Publishing..." : "Publish version";
  const unpublishButtonLabel = versionOperation === "unpublish" ? "Moving to draft..." : "Move to draft";

  return (
    <section className="atlas-canvas flex h-full w-full min-w-0 flex-1 flex-col">
      <div className="atlas-breadcrumb-row flex min-h-[64px] shrink-0 items-center justify-between border-b border-[var(--ns-border)] bg-[rgba(251,248,241,0.76)] px-8">
        <nav className="flex min-w-0 items-center gap-2 text-sm text-[var(--ns-slate-700)]" aria-label="Breadcrumb">
          <a className="truncate hover:text-[var(--ns-blue-600)]" href={libraryHref}>
            {libraryName}
          </a>
          <AtlasIcon className="h-3.5 w-3.5 text-[var(--ns-slate-500)]" src={chevronRightIcon} />
          <a className="hidden truncate hover:text-[var(--ns-blue-600)] sm:inline" href={folderHref}>
            {folderLabel}
          </a>
          <AtlasIcon className="hidden h-3.5 w-3.5 text-[var(--ns-slate-500)] sm:inline-block" src={chevronRightIcon} />
          <span className="truncate font-semibold text-[var(--ns-navy-900)]">{displayTitle}</span>
        </nav>

        <div className="ml-4 flex shrink-0 items-center gap-2 text-sm text-[var(--ns-navy-800)]">
          <span className="hidden items-center gap-2 border-r border-[var(--ns-border)] pr-3 font-medium sm:inline-flex">
            <span className="h-1.5 w-1.5 rounded-full bg-[#3f8c86]" />
            {documentStatusLabel}
          </span>
          <button
            className="atlas-row-button hidden sm:inline-flex"
            disabled={!onPublishVersion || Boolean(versionOperation)}
            onClick={onPublishVersion}
            title="Publish the current draft as a version"
            type="button"
          >
            {publishButtonLabel}
          </button>
          {isPublished ? (
            <button
              className="atlas-row-button hidden sm:inline-flex"
              disabled={!onUnpublishVersion || Boolean(versionOperation)}
              onClick={onUnpublishVersion}
              title="Move this document back to draft while keeping version history"
              type="button"
            >
              {unpublishButtonLabel}
            </button>
          ) : null}
          <button
            className="atlas-row-button hidden sm:inline-flex"
            disabled={!onOpenVersions}
            onClick={onOpenVersions}
            title="Open version history"
            type="button"
          >
            <History className="h-4 w-4" />
            Versions
          </button>
          <button
            className="atlas-row-button hidden sm:inline-flex"
            onClick={onOpenShare ?? (() => { window.location.hash = shareHref; })}
            title="Share document"
            type="button"
          >
            <AtlasIcon className="h-4 w-4" src={shareIcon} />
            Share
          </button>
          <button
            aria-label={isSidePanelCollapsed ? "Show document panel" : "Collapse document panel"}
            className="atlas-icon-button"
            disabled={!onToggleSidePanel}
            onClick={onToggleSidePanel}
            title={isSidePanelCollapsed ? "Show document panel" : "Collapse document panel"}
            type="button"
          >
            {isSidePanelCollapsed ? <PanelRightOpen className="h-4 w-4" /> : <PanelRightClose className="h-4 w-4" />}
          </button>
        </div>
      </div>

      <div
        aria-label="Editor toolbar"
        className="atlas-toolbar-slot flex shrink-0 items-center"
        id={toolbarSlotId}
      />

      <div className="editor-scrollbar atlas-canvas-scroll min-h-0 flex-1 overflow-y-auto">
        <DocumentReaderSurface
          dividerIcon={
            <>
              <AtlasIcon className="h-5 w-5 text-[var(--ns-stone-300)]" src={compassEmblemIcon} />
            </>
          }
          kicker={getFolderKicker(folderLabel)}
          metadata={
            <DocumentMeta tags={tags} updatedAtLabel={updatedAtLabel} version={version} />
          }
          titleNode={<DocumentTitleEditor onChange={onTitleChange} value={title} />}
        >
          <div className="relative">
            <div className={["bg-transparent", isContentEmpty ? "pt-0" : ""].join(" ")}>
              <TiptapEditor
                activeCommentThreadId={activeCommentThreadId}
                commentFocusRequest={commentFocusRequest}
                commentThreads={commentThreads}
                content={content}
                documentId={documentId}
                focusRequest={outlineFocusRequest}
                isCommentComposerOpen={isCommentComposerOpen}
                // Recreate the editor per document to avoid stale content and history crossing documents.
                key={documentId}
                onCommentRuntimeStateChange={onCommentRuntimeStateChange}
                onContentChange={onContentChange}
                onContentStatsChange={onContentStatsChange}
                onOpenCommentComposer={onOpenCommentComposer}
                onSelectCommentThread={onSelectCommentThread}
                toolbarPortalId={toolbarSlotId}
              />
            </div>
          </div>
        </DocumentReaderSurface>
      </div>

      <footer className="atlas-statusbar flex h-10 shrink-0 items-center gap-3 border-t border-[var(--ns-border)] bg-[rgba(251,248,241,0.88)] px-6 text-xs text-[var(--ns-slate-700)]">
        <span>{textLength.toLocaleString("en-US")} words</span>
        <span className="text-[var(--ns-stone-300)]" aria-hidden="true">
          {"\u00B7"}
        </span>
        <span>{readingMinutes} min read</span>
        <span className="text-[var(--ns-stone-300)]" aria-hidden="true">
          {"\u00B7"}
        </span>
        <span>
          readability{" "}
          <button className="font-semibold text-[var(--ns-blue-600)] underline underline-offset-2" type="button">
            64
          </button>
        </span>
        <span className="ml-auto hidden items-center gap-2 sm:inline-flex">
          <AtlasIcon className="h-4 w-4 text-[#7f9b8f]" src={checkCircleIcon} />
          {saveStatusLabel}
          <span className="text-[var(--ns-slate-500)]">/</span>
          {updatedAtLabel}
        </span>
      </footer>
    </section>
  );
}

function getFolderKicker(folderTitle: string) {
  return folderTitle.replace(/^\d+(?:\.\d+)?\.?\s*/, "") || folderTitle;
}
