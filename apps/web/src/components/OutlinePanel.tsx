import {
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  CircleOff,
  MessageSquare,
  Pin,
  RotateCcw,
  Send,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { AtlasIcon } from "./AtlasIcon";
import { activityTimeline, backlinks, relatedDocuments, versionTrail } from "../data/editorMockData";
import chevronRightSvg from "../assets/svg/icons/chevron-right.svg";
import fileIcon from "../assets/svg/icons/file.svg";
import linkIcon from "../assets/svg/icons/link.svg";
import type { AnchorMatchResult } from "../lib/commentAnchorMatching";
import type { AnchorRelocationResult } from "../lib/commentAnchorRelocation";
import { isCommentBodySubmittable } from "../lib/commentComposerModel";
import type { CommentLoadState } from "../lib/commentProductionState";
import type {
  EditorActivityRow,
  EditorBacklinkRow,
  EditorContextLoadStatus,
  EditorRelatedDocumentRow,
  EditorVersionTrailRow,
} from "../lib/editorDocumentContextModel";
import type {
  CommentThread,
  CreateCommentThreadRequest,
  KnowledgeDocument,
  OutlineItem,
  PendingCommentComposer,
} from "../types/editor";

type OutlinePanelProps = {
  activeCommentThreadId?: string | null;
  activeDocument: KnowledgeDocument;
  activityRows?: EditorActivityRow[];
  backlinksRows?: EditorBacklinkRow[];
  commentLifecycleErrors?: Record<string, string>;
  commentLifecyclePending?: Record<string, true>;
  commentLoadState?: CommentLoadState;
  commentMatchResults?: Record<string, AnchorMatchResult>;
  commentRelocationResults?: Record<string, AnchorRelocationResult>;
  commentThreads?: CommentThread[];
  contextLoadStatus?: EditorContextLoadStatus;
  documentStatusLabel: string;
  folderHref: string;
  folderTitle: string;
  libraryHref: string;
  libraryName: string;
  outlineItems: OutlineItem[];
  pendingCommentComposer?: PendingCommentComposer | null;
  relatedDocumentRows?: EditorRelatedDocumentRow[];
  saveStatusLabel: string;
  shareHref: string;
  textLength: number;
  updatedAtLabel: string;
  versionTrailRows?: EditorVersionTrailRow[];
  onAddCommentMessage?: (threadId: string, body: string) => Promise<void> | void;
  onCancelPendingComment?: () => void;
  onCommentThreadClick?: (thread: CommentThread) => void;
  onCreateCommentThread?: (request: CreateCommentThreadRequest) => void;
  onOpenShare?: () => void;
  onPendingCommentBodyChange?: (body: string) => void;
  onOutlineItemClick: (item: OutlineItem) => void;
  onReopenCommentThread?: (threadId: string) => void;
  onRetryLoadComments?: () => void;
  onResolveCommentThread?: (threadId: string) => void;
};

type OverviewTab = "overview" | "comments" | "info" | "activity";
type CollapsibleSection = "documentMap" | "versionTrail" | "backlinks";

export function OutlinePanel({
  activeCommentThreadId,
  activeDocument,
  activityRows,
  backlinksRows,
  commentLifecycleErrors = {},
  commentLifecyclePending = {},
  commentLoadState = { status: "idle" },
  commentMatchResults = {},
  commentRelocationResults = {},
  commentThreads = [],
  contextLoadStatus = "demo",
  documentStatusLabel,
  folderHref,
  folderTitle,
  libraryHref,
  libraryName,
  onAddCommentMessage,
  onCancelPendingComment,
  onCommentThreadClick,
  onCreateCommentThread,
  onOpenShare,
  onPendingCommentBodyChange,
  onOutlineItemClick,
  onReopenCommentThread,
  onRetryLoadComments,
  onResolveCommentThread,
  outlineItems,
  pendingCommentComposer,
  relatedDocumentRows,
  saveStatusLabel,
  shareHref,
  textLength,
  updatedAtLabel,
  versionTrailRows,
}: OutlinePanelProps) {
  const [activeTab, setActiveTab] = useState<OverviewTab>("overview");
  const [collapsedSections, setCollapsedSections] = useState<Record<CollapsibleSection, boolean>>({
    documentMap: false,
    versionTrail: false,
    backlinks: false,
  });
  const readingMinutes = Math.max(1, Math.ceil(textLength / 850));
  const tags = activeDocument.tags?.length ? activeDocument.tags : [];
  const displayedActivityRows = activityRows ?? activityTimeline.map(toDemoActivityRow);
  const displayedBacklinkRows = backlinksRows ?? backlinks.map(toDemoBacklinkRow);
  const displayedRelatedDocuments = relatedDocumentRows ?? relatedDocuments.map(toDemoRelatedDocumentRow);
  const displayedVersionTrailRows = versionTrailRows ?? versionTrail.map(toDemoVersionTrailRow);
  const contextIsError = contextLoadStatus === "error";
  const contextIsLoading = contextLoadStatus === "loading";

  useEffect(() => {
    if (activeCommentThreadId || pendingCommentComposer) {
      setActiveTab("comments");
    }
  }, [activeCommentThreadId, pendingCommentComposer]);

  const toggleSection = (section: CollapsibleSection) => {
    setCollapsedSections((currentSections) => ({
      ...currentSections,
      [section]: !currentSections[section],
    }));
  };

  return (
    <aside className="atlas-overview-panel hidden h-full w-[324px] shrink-0 overflow-y-auto border-l border-[var(--ns-border)] bg-[rgba(248,244,236,0.95)] xl:block">
      <div className="atlas-tabs sticky top-0 z-10 grid h-14 grid-cols-[1fr_1fr_1fr_1fr_auto] border-b border-[var(--ns-border)] bg-[rgba(248,244,236,0.96)] text-[11px] font-semibold uppercase tracking-normal text-[var(--ns-slate-700)]">
        <button
          className={["atlas-tab", activeTab === "overview" ? "is-active" : ""].join(" ")}
          onClick={() => setActiveTab("overview")}
          type="button"
        >
          Overview
        </button>
        <button
          className={["atlas-tab", activeTab === "comments" ? "is-active" : ""].join(" ")}
          onClick={() => setActiveTab("comments")}
          type="button"
        >
          Comments
        </button>
        <button
          className={["atlas-tab", activeTab === "info" ? "is-active" : ""].join(" ")}
          onClick={() => setActiveTab("info")}
          type="button"
        >
          Info
        </button>
        <button
          className={["atlas-tab", activeTab === "activity" ? "is-active" : ""].join(" ")}
          onClick={() => setActiveTab("activity")}
          type="button"
        >
          Activity
        </button>
        <button className="grid w-11 place-items-center text-[var(--ns-slate-500)]" title="Pin panel" type="button">
          <Pin className="h-3.5 w-3.5" />
        </button>
      </div>

      <div className="px-5 py-5">
        {activeTab === "overview" ? (
          <>
            <PanelSection
              collapsed={collapsedSections.documentMap}
              onToggle={() => toggleSection("documentMap")}
              title="Document Map"
            >
              {outlineItems.length > 0 ? (
                <nav className="space-y-1" aria-label="Document map">
                  {outlineItems.map((item, index) => (
                    <button
                      className={["atlas-outline-item", index === 0 ? "is-active" : ""].join(" ")}
                      key={item.id}
                      onClick={() => onOutlineItemClick(item)}
                      style={{ paddingLeft: `${10 + Math.max(0, item.level - 2) * 16}px` }}
                      title={item.text}
                      type="button"
                    >
                      <span className="w-5 shrink-0 text-right tabular-nums">{index + 1}.</span>
                      <span className="min-w-0 flex-1 truncate">{item.text}</span>
                    </button>
                  ))}
                </nav>
              ) : (
                <div className="border border-dashed border-[var(--ns-border)] bg-white/45 px-3 py-4 text-sm leading-6 text-[var(--ns-slate-500)]">
                  Headings appear here as the document takes shape.
                </div>
              )}
            </PanelSection>

            <PanelSection action="View All" actionHref="#search" title="Related Documents">
              {contextIsLoading ? (
                <ContextStateMessage>Loading related documents...</ContextStateMessage>
              ) : contextIsError ? (
                <ContextStateMessage tone="error">Document context could not be loaded.</ContextStateMessage>
              ) : displayedRelatedDocuments.length > 0 ? (
                <div className="space-y-2">
                  {displayedRelatedDocuments.map((document) => (
                    <a className="atlas-related-item" href={document.href} key={document.id} title={document.title}>
                      <AtlasIcon className="h-4 w-4 text-[var(--ns-slate-500)]" src={fileIcon} />
                      <span className="font-semibold text-[var(--ns-navy-800)]">{document.code}</span>
                      <span className="min-w-0 flex-1 truncate">{document.title}</span>
                    </a>
                  ))}
                </div>
              ) : (
                <ContextStateMessage>No related documents yet.</ContextStateMessage>
              )}
            </PanelSection>

            <PanelSection
              collapsed={collapsedSections.versionTrail}
              onToggle={() => toggleSection("versionTrail")}
              title="Version Trail"
            >
              {contextIsLoading ? (
                <ContextStateMessage>Loading version trail...</ContextStateMessage>
              ) : contextIsError ? (
                <ContextStateMessage tone="error">Version trail could not be loaded.</ContextStateMessage>
              ) : displayedVersionTrailRows.length > 0 ? (
                <VersionTrail items={displayedVersionTrailRows} />
              ) : (
                <ContextStateMessage>No versions yet.</ContextStateMessage>
              )}
            </PanelSection>

            <PanelSection
              collapsed={collapsedSections.backlinks}
              count={displayedBacklinkRows.length}
              onToggle={() => toggleSection("backlinks")}
              title="Backlinks"
            >
              {contextIsLoading ? (
                <ContextStateMessage>Loading backlinks...</ContextStateMessage>
              ) : contextIsError ? (
                <ContextStateMessage tone="error">Backlinks could not be loaded.</ContextStateMessage>
              ) : displayedBacklinkRows.length > 0 ? (
                <div className="space-y-3">
                  {displayedBacklinkRows.map((item) => (
                    <a className="atlas-backlink" href={item.href} key={item.id} title={item.title}>
                      <div className="flex items-center gap-2">
                        <AtlasIcon className="h-4 w-4 text-[var(--ns-slate-500)]" src={linkIcon} />
                        <span className="font-semibold text-[var(--ns-blue-600)]">{item.code}</span>
                        <span className="min-w-0 flex-1 truncate text-[var(--ns-navy-800)]">{item.title}</span>
                        <AtlasIcon className="h-4 w-4 text-[var(--ns-slate-500)]" src={chevronRightSvg} />
                      </div>
                      <p className="mt-2 line-clamp-2 text-left text-xs leading-5 text-[var(--ns-slate-700)]">
                        {item.excerpt}
                      </p>
                    </a>
                  ))}
                </div>
              ) : (
                <ContextStateMessage>No backlinks yet.</ContextStateMessage>
              )}
            </PanelSection>
          </>
        ) : null}

        {activeTab === "comments" ? (
          <CommentsPanel
            activeCommentThreadId={activeCommentThreadId}
            commentLifecycleErrors={commentLifecycleErrors}
            commentLifecyclePending={commentLifecyclePending}
            commentLoadState={commentLoadState}
            commentMatchResults={commentMatchResults}
            commentRelocationResults={commentRelocationResults}
            commentThreads={commentThreads}
            onCancelPendingComment={onCancelPendingComment}
            onAddCommentMessage={onAddCommentMessage}
            onCommentThreadClick={onCommentThreadClick}
            onCreateCommentThread={onCreateCommentThread}
            onPendingCommentBodyChange={onPendingCommentBodyChange}
            onReopenCommentThread={onReopenCommentThread}
            onRetryLoadComments={onRetryLoadComments}
            onResolveCommentThread={onResolveCommentThread}
            pendingCommentComposer={pendingCommentComposer}
          />
        ) : null}

        {activeTab === "info" ? (
          <>
            <PanelSection title="Document Info">
              <dl className="atlas-info-list">
                <InfoRow label="Document">
                  <span title={activeDocument.title}>{activeDocument.title || "Untitled Field Note"}</span>
                </InfoRow>
                <InfoRow label="Library">
                  <a href={libraryHref} title={`Open ${libraryName}`}>
                    {libraryName}
                  </a>
                </InfoRow>
                <InfoRow label="Folder">
                  <a href={folderHref} title={`Open ${folderTitle}`}>
                    {folderTitle}
                  </a>
                </InfoRow>
                <InfoRow label="Status">
                  <span className="inline-flex items-center gap-2">
                    <span className="h-1.5 w-1.5 rounded-full bg-[#3f8c86]" />
                    {documentStatusLabel}
                  </span>
                </InfoRow>
                <InfoRow label="Owner">{activeDocument.owner?.name ?? "Unassigned"}</InfoRow>
                <InfoRow label="Last updated">{updatedAtLabel}</InfoRow>
                <InfoRow label="Version">{activeDocument.version?.trim() || "Draft"}</InfoRow>
                <InfoRow label="Word count">{textLength.toLocaleString("en-US")}</InfoRow>
                <InfoRow label="Reading time">{readingMinutes} min</InfoRow>
                <InfoRow label="Save status">{saveStatusLabel}</InfoRow>
              </dl>
            </PanelSection>

            <PanelSection title="Access">
              <div className="atlas-info-actions">
                <button onClick={onOpenShare} type="button">
                  Open Share drawer
                </button>
                <a href={shareHref}>Advanced permissions</a>
              </div>
            </PanelSection>

            <PanelSection title="Tags">
              {tags.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {tags.map((tag) => (
                    <span className="atlas-info-tag" key={tag}>
                      {tag}
                    </span>
                  ))}
                </div>
              ) : (
                <p className="text-xs leading-5 text-[var(--ns-slate-500)]">No tags</p>
              )}
            </PanelSection>
          </>
        ) : null}

        {activeTab === "activity" ? (
          <PanelSection title="Activity Trail">
            {contextIsLoading ? (
              <ContextStateMessage>Loading activity...</ContextStateMessage>
            ) : contextIsError ? (
              <ContextStateMessage tone="error">Activity could not be loaded.</ContextStateMessage>
            ) : displayedActivityRows.length > 0 ? (
              <div className="atlas-activity-list">
                {displayedActivityRows.map((item, index) => (
                  <a className="atlas-activity-item" href={item.href} key={item.id} title={item.detail}>
                    <span className={["atlas-version-dot", index === 0 ? "is-active" : ""].join(" ")} />
                    <div className="min-w-0">
                      <div className="flex items-center gap-2 text-xs">
                        <span className="min-w-0 flex-1 truncate font-semibold text-[var(--ns-navy-800)]">
                          {item.actorName}
                          <span className="font-normal text-[var(--ns-slate-600)]"> {item.actionLabel} </span>
                          {item.documentTitle}
                        </span>
                        <span className="shrink-0 text-[var(--ns-slate-500)]">{item.date}</span>
                      </div>
                      <p className="mt-1 text-xs leading-5 text-[var(--ns-slate-700)]">{item.detail}</p>
                    </div>
                  </a>
                ))}
              </div>
            ) : (
              <ContextStateMessage>No activity yet.</ContextStateMessage>
            )}
          </PanelSection>
        ) : null}
      </div>
    </aside>
  );
}

function CommentsPanel({
  activeCommentThreadId,
  onAddCommentMessage,
  commentLifecycleErrors,
  commentLifecyclePending,
  commentLoadState,
  commentMatchResults,
  commentRelocationResults,
  commentThreads,
  onCancelPendingComment,
  onCommentThreadClick,
  onCreateCommentThread,
  onPendingCommentBodyChange,
  onReopenCommentThread,
  onRetryLoadComments,
  onResolveCommentThread,
  pendingCommentComposer,
}: {
  activeCommentThreadId?: string | null;
  commentLifecycleErrors: Record<string, string>;
  commentLifecyclePending: Record<string, true>;
  commentLoadState: CommentLoadState;
  commentMatchResults: Record<string, AnchorMatchResult>;
  commentRelocationResults: Record<string, AnchorRelocationResult>;
  commentThreads: CommentThread[];
  pendingCommentComposer?: PendingCommentComposer | null;
  onAddCommentMessage?: (threadId: string, body: string) => Promise<void> | void;
  onCancelPendingComment?: () => void;
  onCommentThreadClick?: (thread: CommentThread) => void;
  onCreateCommentThread?: (request: CreateCommentThreadRequest) => void;
  onPendingCommentBodyChange?: (body: string) => void;
  onReopenCommentThread?: (threadId: string) => void;
  onRetryLoadComments?: () => void;
  onResolveCommentThread?: (threadId: string) => void;
}) {
  const openCount = commentThreads.filter((thread) => thread.status === "open").length;
  const staleCount = commentThreads.filter((thread) => thread.anchorStatus === "stale").length;
  const orphanedCount = commentThreads.filter((thread) => thread.anchorStatus === "orphaned").length;
  const isLoading = commentLoadState.status === "loading";
  const loadError = commentLoadState.status === "error" ? commentLoadState.error : null;

  return (
    <PanelSection count={commentThreads.length} title="Comment Threads">
      {pendingCommentComposer ? (
        <NewCommentComposer
          composer={pendingCommentComposer}
          onCancel={() => onCancelPendingComment?.()}
          onBodyChange={(body) => onPendingCommentBodyChange?.(body)}
          onSubmit={(body) => {
            onCreateCommentThread?.({
              anchor: pendingCommentComposer.anchor,
              body,
            });
          }}
        />
      ) : null}

      {isLoading ? (
        <div
          className={["border border-[var(--ns-border)] bg-white/45 px-3 py-4 text-sm leading-6 text-[var(--ns-slate-700)]", pendingCommentComposer ? "mt-4" : ""].join(" ")}
          role="status"
        >
          Loading comments...
        </div>
      ) : loadError ? (
        <div
          className={["border border-[rgba(185,77,95,0.24)] bg-[rgba(185,77,95,0.08)] px-3 py-3 text-sm leading-6 text-[#8e3b4a]", pendingCommentComposer ? "mt-4" : ""].join(" ")}
          role="alert"
        >
          <p className="font-semibold">Comments failed to load.</p>
          <p className="mt-1 text-xs leading-5">{loadError}</p>
          <button
            className="mt-3 inline-flex h-7 items-center gap-1.5 border border-[rgba(185,77,95,0.28)] bg-white/70 px-2 text-[11px] font-semibold text-[#8e3b4a] hover:bg-white"
            onClick={onRetryLoadComments}
            type="button"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            Retry
          </button>
        </div>
      ) : commentThreads.length > 0 ? (
        <div className={["space-y-3", pendingCommentComposer ? "mt-4" : ""].join(" ")}>
          <div className="flex items-center gap-2 text-xs leading-5 text-[var(--ns-slate-700)]">
            <MessageSquare className="h-3.5 w-3.5 shrink-0 text-[var(--ns-blue-600)]" />
            <span>{openCount} open</span>
            <span className="text-[var(--ns-stone-300)]" aria-hidden="true">
              {"\u00B7"}
            </span>
            <span>{commentThreads.length - openCount} resolved</span>
            {staleCount || orphanedCount ? (
              <>
                <span className="text-[var(--ns-stone-300)]" aria-hidden="true">
                  {"\u00B7"}
                </span>
                <span>{staleCount} stale</span>
                <span className="text-[var(--ns-stone-300)]" aria-hidden="true">
                  {"\u00B7"}
                </span>
                <span>{orphanedCount} orphaned</span>
              </>
            ) : null}
          </div>

          {commentThreads.map((thread, index) => (
            <CommentThreadItem
              active={thread.id === activeCommentThreadId}
              index={commentThreads.length - index}
              key={thread.id}
              lifecycleError={commentLifecycleErrors[thread.id] ?? null}
              lifecyclePending={Boolean(commentLifecyclePending[thread.id])}
              matchResult={commentMatchResults[thread.id]}
              onClick={() => onCommentThreadClick?.(thread)}
              onReply={onAddCommentMessage ? (body) => onAddCommentMessage(thread.id, body) : undefined}
              onReopen={() => onReopenCommentThread?.(thread.id)}
              onResolve={() => onResolveCommentThread?.(thread.id)}
              relocationResult={commentRelocationResults[thread.id]}
              thread={thread}
            />
          ))}
        </div>
      ) : !pendingCommentComposer ? (
        <div className="border border-dashed border-[var(--ns-border)] bg-white/45 px-3 py-4 text-sm leading-6 text-[var(--ns-slate-500)]">
          No comments yet. Select text and use the comment button to start a thread.
        </div>
      ) : null}
    </PanelSection>
  );
}

function NewCommentComposer({
  composer,
  onBodyChange,
  onCancel,
  onSubmit,
}: {
  composer: PendingCommentComposer;
  onBodyChange: (body: string) => void;
  onCancel: () => void;
  onSubmit: (body: string) => void;
}) {
  const body = composer.body ?? "";
  const isSubmitting = Boolean(composer.isSubmitting);
  const canSubmit = isCommentBodySubmittable(body) && !isSubmitting;
  const errorId = `comment-create-error-${composer.documentId}`;

  return (
    <section className="border border-[rgba(15,92,156,0.28)] bg-white/70 p-3 shadow-[0_1px_0_rgba(31,41,55,0.03)]">
      <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-normal text-[var(--ns-blue-600)]">
        <MessageSquare className="h-3.5 w-3.5" />
        New Comment
      </div>
      <blockquote className="mt-2 border-l-2 border-[rgba(15,92,156,0.24)] pl-2 text-xs leading-5 text-[var(--ns-slate-700)]">
        {composer.excerpt}
      </blockquote>
      <textarea
        aria-describedby={composer.error ? errorId : undefined}
        aria-invalid={composer.error ? true : undefined}
        autoFocus
        className="mt-3 min-h-[92px] w-full resize-y border border-[var(--ns-border)] bg-white/80 px-2.5 py-2 text-sm leading-5 text-[var(--ns-navy-800)] outline-none transition placeholder:text-[var(--ns-slate-500)] focus:border-[rgba(15,92,156,0.42)] focus:ring-1 focus:ring-[rgba(15,92,156,0.14)]"
        disabled={isSubmitting}
        onChange={(event) => onBodyChange(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Escape" && !isSubmitting) {
            event.preventDefault();
            onCancel();
          }
        }}
        placeholder="Write a comment..."
        value={body}
      />
      {composer.error ? (
        <p className="mt-2 text-xs leading-5 text-[#8e3b4a]" id={errorId} role="alert">
          {composer.error}
        </p>
      ) : null}
      <div className="mt-2 flex items-center justify-end gap-2">
        <button
          className="inline-flex h-7 items-center gap-1.5 border border-[var(--ns-border)] bg-white/70 px-2 text-[11px] font-semibold text-[var(--ns-slate-700)] hover:border-[rgba(15,92,156,0.28)] hover:text-[var(--ns-blue-600)]"
          disabled={isSubmitting}
          onClick={onCancel}
          type="button"
        >
          <X className="h-3.5 w-3.5" />
          Cancel
        </button>
        <button
          aria-busy={isSubmitting}
          className="inline-flex h-7 items-center gap-1.5 border border-[rgba(15,92,156,0.32)] bg-[rgba(15,92,156,0.08)] px-2 text-[11px] font-semibold text-[var(--ns-blue-600)] disabled:cursor-not-allowed disabled:border-[var(--ns-border)] disabled:bg-white/50 disabled:text-[var(--ns-slate-500)]"
          disabled={!canSubmit}
          onClick={() => {
            if (canSubmit) {
              onSubmit(body);
            }
          }}
          type="button"
        >
          <Send className="h-3.5 w-3.5" />
          {isSubmitting ? "Submitting" : "Comment"}
        </button>
      </div>
    </section>
  );
}

function CommentThreadItem({
  active,
  index,
  lifecycleError,
  lifecyclePending,
  matchResult,
  onClick,
  onReply,
  onReopen,
  onResolve,
  relocationResult,
  thread,
}: {
  active: boolean;
  index: number;
  lifecycleError?: string | null;
  lifecyclePending?: boolean;
  matchResult?: AnchorMatchResult;
  onClick: () => void;
  onReply?: (body: string) => Promise<void> | void;
  onReopen: () => void;
  onResolve: () => void;
  relocationResult?: AnchorRelocationResult;
  thread: CommentThread;
}) {
  const [replyBody, setReplyBody] = useState("");
  const [replyError, setReplyError] = useState<string | null>(null);
  const [replyIsOpen, setReplyIsOpen] = useState(false);
  const [replyIsSubmitting, setReplyIsSubmitting] = useState(false);
  const runtimeMatch = useMemo(
    () => matchResult ?? createFallbackMatchResult(thread.anchorStatus),
    [matchResult, thread.anchorStatus],
  );
  const createdAt = formatThreadTime(thread.createdAt);
  const anchorStatusMeta = getAnchorStatusMeta(runtimeMatch, relocationResult);
  const AnchorStatusIcon = anchorStatusMeta.icon;
  const canReply = Boolean(onReply) && thread.status === "open";
  const canSubmitReply = isCommentBodySubmittable(replyBody) && !replyIsSubmitting;

  const submitReply = async () => {
    if (!onReply || !canSubmitReply) {
      return;
    }

    setReplyIsSubmitting(true);
    setReplyError(null);
    try {
      await onReply(replyBody);
      setReplyBody("");
      setReplyIsOpen(false);
    } catch (error) {
      setReplyError(error instanceof Error && error.message.trim() ? error.message : "Reply failed. Try again.");
    } finally {
      setReplyIsSubmitting(false);
    }
  };

  return (
    <article
      className={[
        "border bg-white/55 p-3 text-left shadow-[0_1px_0_rgba(31,41,55,0.03)] transition",
        getThreadCardStateClass(runtimeMatch.status),
        active
          ? "border-[rgba(15,92,156,0.34)] ring-1 ring-[rgba(15,92,156,0.16)]"
          : "border-[var(--ns-border)] hover:border-[rgba(15,92,156,0.24)]",
      ].join(" ")}
    >
      <button className="block w-full text-left" onClick={onClick} type="button">
        <div className="flex items-start gap-2">
          <span className="mt-0.5 grid h-5 w-5 shrink-0 place-items-center border border-[var(--ns-border)] bg-[rgba(248,244,236,0.72)] text-[10px] font-bold text-[var(--ns-blue-600)]">
            {index}
          </span>
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-normal">
              <span className={thread.status === "open" ? "text-[var(--ns-blue-600)]" : "text-[var(--ns-slate-500)]"}>
                {thread.status}
              </span>
              <span className="text-[var(--ns-stone-300)]" aria-hidden="true">
                {"\u00B7"}
              </span>
              <span className={["inline-flex items-center gap-1", anchorStatusMeta.className].join(" ")}>
                <AnchorStatusIcon className="h-3 w-3" />
                {anchorStatusMeta.label}
              </span>
              <span className="text-[var(--ns-stone-300)]" aria-hidden="true">
                {"\u00B7"}
              </span>
              <span className="truncate text-[var(--ns-slate-500)]">{createdAt}</span>
            </div>
            <p className="mt-1 line-clamp-2 text-sm font-semibold leading-5 text-[var(--ns-navy-800)]">
              {thread.anchor.display.excerpt}
            </p>
          </div>
        </div>
      </button>

      <div className="atlas-comment-message-list">
        {thread.comments.length > 0 ? (
          thread.comments.map((comment) => (
            <div className="atlas-comment-message" key={comment.id}>
              <div>
                <strong title={comment.author.name}>{comment.author.name}</strong>
                <span>{formatThreadTime(comment.createdAt)}</span>
              </div>
              <p>{comment.body}</p>
            </div>
          ))
        ) : (
          <p className="mt-2 text-xs leading-5 text-[var(--ns-slate-700)]">No comment body</p>
        )}
      </div>

      {runtimeMatch.status !== "active" || runtimeMatch.confidence !== "exact" ? (
        <div className={["mt-2 border px-2 py-1.5 text-xs leading-5", anchorStatusMeta.noticeClassName].join(" ")}>
          {anchorStatusMeta.detail}
        </div>
      ) : null}

      <div className="mt-3 flex items-center justify-between gap-2">
        <span className={["text-[11px] uppercase tracking-normal", anchorStatusMeta.className].join(" ")}>
          {anchorStatusMeta.label}
        </span>
        <span className="flex shrink-0 items-center gap-1.5">
        {canReply ? (
          <button
            className="inline-flex h-7 items-center gap-1.5 border border-[var(--ns-border)] bg-white/70 px-2 text-[11px] font-semibold text-[var(--ns-slate-700)] hover:border-[rgba(15,92,156,0.28)] hover:text-[var(--ns-blue-600)]"
            disabled={replyIsSubmitting || lifecyclePending}
            onClick={() => setReplyIsOpen((isOpen) => !isOpen)}
            type="button"
          >
            Reply
          </button>
        ) : null}
        {thread.status === "open" ? (
          <button
            aria-busy={lifecyclePending}
            className="inline-flex h-7 items-center gap-1.5 border border-[var(--ns-border)] bg-white/70 px-2 text-[11px] font-semibold text-[var(--ns-slate-700)] hover:border-[rgba(15,92,156,0.28)] hover:text-[var(--ns-blue-600)] disabled:cursor-not-allowed disabled:bg-white/45 disabled:text-[var(--ns-slate-500)]"
            disabled={lifecyclePending}
            onClick={onResolve}
            type="button"
          >
            <CheckCircle2 className="h-3.5 w-3.5" />
            {lifecyclePending ? "Resolving" : "Resolve"}
          </button>
        ) : (
          <button
            aria-busy={lifecyclePending}
            className="inline-flex h-7 items-center gap-1.5 border border-[var(--ns-border)] bg-white/70 px-2 text-[11px] font-semibold text-[var(--ns-slate-700)] hover:border-[rgba(15,92,156,0.28)] hover:text-[var(--ns-blue-600)] disabled:cursor-not-allowed disabled:bg-white/45 disabled:text-[var(--ns-slate-500)]"
            disabled={lifecyclePending}
            onClick={onReopen}
            type="button"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            {lifecyclePending ? "Reopening" : "Reopen"}
          </button>
        )}
        </span>
      </div>

      {replyIsOpen ? (
        <div className="atlas-comment-reply">
          <textarea
            disabled={replyIsSubmitting}
            onChange={(event) => setReplyBody(event.target.value)}
            placeholder="Write a reply..."
            value={replyBody}
          />
          {replyError ? <p role="alert">{replyError}</p> : null}
          <div>
            <button disabled={replyIsSubmitting} onClick={() => setReplyIsOpen(false)} type="button">
              Cancel
            </button>
            <button disabled={!canSubmitReply} onClick={() => void submitReply()} type="button">
              {replyIsSubmitting ? "Sending" : "Reply"}
            </button>
          </div>
        </div>
      ) : null}

      {lifecycleError ? (
        <p className="mt-2 border border-[rgba(185,77,95,0.24)] bg-[rgba(185,77,95,0.08)] px-2 py-1.5 text-xs leading-5 text-[#8e3b4a]" role="alert">
          {lifecycleError}
        </p>
      ) : null}
    </article>
  );
}

function formatThreadTime(value: string) {
  try {
    return new Intl.DateTimeFormat("en-US", {
      hour: "2-digit",
      minute: "2-digit",
      month: "short",
      day: "numeric",
    }).format(new Date(value));
  } catch {
    return "now";
  }
}

function getThreadCardStateClass(anchorStatus: CommentThread["anchorStatus"]) {
  if (anchorStatus === "orphaned") {
    return "bg-[rgba(185,77,95,0.07)]";
  }

  if (anchorStatus === "stale") {
    return "bg-[rgba(226,196,104,0.13)]";
  }

  return "";
}

function getAnchorStatusMeta(matchResult: AnchorMatchResult, relocationResult?: AnchorRelocationResult) {
  if (matchResult.status === "orphaned") {
    return {
      icon: CircleOff,
      label: "Anchor lost",
      className: "text-[#b94d5f]",
      noticeClassName: "border-[rgba(185,77,95,0.24)] bg-[rgba(185,77,95,0.08)] text-[#8e3b4a]",
      detail: "The mapped range is no longer valid, so selecting this thread will not move the editor cursor.",
    };
  }

  if (matchResult.status === "stale") {
    const confidence = relocationResult?.confidence ?? matchResult.confidence;
    const isMediumConfidence = confidence === "medium";

    return {
      icon: AlertTriangle,
      label: isMediumConfidence ? "Needs review" : "Anchor may be inaccurate",
      className: "text-[#8b6d1f]",
      noticeClassName: "border-[rgba(181,124,32,0.24)] bg-[rgba(226,196,104,0.14)] text-[#6f571a]",
      detail: "The range still maps, but the current text differs from the original quoted text.",
    };
  }

  if (relocationResult?.status === "active" && relocationResult.confidence === "high") {
    return {
      icon: CheckCircle2,
      label: "Attached - relocated with high confidence",
      className: "text-[var(--ns-blue-600)]",
      noticeClassName: "border-[rgba(15,92,156,0.18)] bg-[rgba(15,92,156,0.06)] text-[var(--ns-blue-600)]",
      detail: "The range was reconstructed from block identity and quote evidence.",
    };
  }

  if (matchResult.confidence === "high") {
    return {
      icon: CheckCircle2,
      label: "Attached - text changed slightly",
      className: "text-[var(--ns-blue-600)]",
      noticeClassName: "border-[rgba(15,92,156,0.18)] bg-[rgba(15,92,156,0.06)] text-[var(--ns-blue-600)]",
      detail: "The range still maps with a minor text change.",
    };
  }

  return {
    icon: CheckCircle2,
    label: "Attached",
    className: "text-[var(--ns-blue-600)]",
    noticeClassName: "border-[var(--ns-border)] bg-white/50 text-[var(--ns-slate-700)]",
    detail: "The mapped range still matches the original quote.",
  };
}

function createFallbackMatchResult(anchorStatus: CommentThread["anchorStatus"]): AnchorMatchResult {
  if (anchorStatus === "orphaned") {
    return {
      status: "orphaned",
      confidence: "none",
      reason: "invalid_range",
    };
  }

  if (anchorStatus === "stale") {
    return {
      status: "stale",
      confidence: "low",
      reason: "major_text_change",
    };
  }

  return {
    status: "active",
    confidence: "exact",
    reason: "exact_match",
    similarity: 1,
    editDistance: 0,
  };
}

function VersionTrail({ items }: { items: EditorVersionTrailRow[] }) {
  return (
    <div className="atlas-version-list">
      {items.map((item, index) => (
        <div className="atlas-version-item" key={item.id}>
          <span className={["atlas-version-dot", index === 0 ? "is-active" : ""].join(" ")} />
          <div className="min-w-0">
            <div className="flex items-center gap-3 text-xs">
              <span className="font-semibold text-[var(--ns-blue-600)]">{item.version}</span>
              <span className="text-[var(--ns-navy-800)]">{item.date}</span>
              <span className="ml-auto text-[var(--ns-blue-600)]">{item.status}</span>
            </div>
            <div className="mt-1 text-xs text-[var(--ns-slate-500)]">{item.author}</div>
          </div>
        </div>
      ))}
    </div>
  );
}

function ContextStateMessage({
  children,
  tone = "muted",
}: {
  children: ReactNode;
  tone?: "error" | "muted";
}) {
  return (
    <div
      className={[
        "border px-3 py-3 text-sm leading-6",
        tone === "error"
          ? "border-[rgba(185,77,95,0.24)] bg-[rgba(185,77,95,0.08)] text-[#8e3b4a]"
          : "border-dashed border-[var(--ns-border)] bg-white/45 text-[var(--ns-slate-500)]",
      ].join(" ")}
    >
      {children}
    </div>
  );
}

function toDemoRelatedDocumentRow(document: (typeof relatedDocuments)[number], index: number): EditorRelatedDocumentRow {
  return {
    id: `demo-related-${index}`,
    code: document.code,
    href: "#editor",
    title: document.title,
  };
}

function toDemoVersionTrailRow(item: (typeof versionTrail)[number], index: number): EditorVersionTrailRow {
  return {
    id: `demo-version-${index}`,
    author: item.author,
    date: item.date,
    status: item.status,
    version: item.version,
  };
}

function toDemoBacklinkRow(item: (typeof backlinks)[number], index: number): EditorBacklinkRow {
  return {
    id: `demo-backlink-${index}`,
    code: item.code,
    excerpt: item.excerpt,
    href: "#editor",
    title: item.title,
  };
}

function toDemoActivityRow(item: (typeof activityTimeline)[number], index: number): EditorActivityRow {
  return {
    actionLabel: "updated",
    actorName: item.title,
    documentTitle: item.title,
    href: "#editor",
    id: `demo-activity-${index}`,
    date: item.date,
    detail: item.detail,
    title: item.title,
  };
}

function InfoRow({ children, label }: { children: ReactNode; label: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{children}</dd>
    </div>
  );
}

function PanelSection({
  action,
  actionHref,
  children,
  collapsed,
  count,
  onToggle,
  title,
}: {
  action?: string;
  actionHref?: string;
  children: ReactNode;
  collapsed?: boolean;
  count?: number;
  onToggle?: () => void;
  title: string;
}) {
  const collapsible = Boolean(onToggle);

  return (
    <section className="atlas-panel-section">
      <div className="mb-3 flex items-center justify-between gap-2 text-[11px] font-semibold uppercase tracking-normal text-[var(--ns-navy-800)]">
        {collapsible ? (
          <button
            aria-expanded={!collapsed}
            className="atlas-section-toggle"
            onClick={onToggle}
            type="button"
          >
            <span>{title}</span>
          </button>
        ) : (
          <span>{title}</span>
        )}
        <div className="flex shrink-0 items-center gap-2">
          {action ? (
            <a
              className="text-[10px] font-bold text-[var(--ns-blue-600)] underline underline-offset-2"
              href={actionHref ?? "#search"}
            >
              {action}
            </a>
          ) : null}
          {typeof count === "number" ? (
            <span className="text-xs tracking-normal text-[var(--ns-navy-800)]">{count}</span>
          ) : null}
          {collapsible ? (
            collapsed ? (
              <ChevronRight className="h-3.5 w-3.5 text-[var(--ns-slate-500)]" />
            ) : (
              <ChevronDown className="h-3.5 w-3.5 text-[var(--ns-slate-500)]" />
            )
          ) : !action ? (
            <ChevronDown className="h-3.5 w-3.5 text-[var(--ns-slate-500)]" />
          ) : null}
        </div>
      </div>
      {collapsed ? null : children}
    </section>
  );
}
