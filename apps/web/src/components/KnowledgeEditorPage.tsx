import { useCallback, useEffect, useRef, useState, type CSSProperties } from "react";
import { EditorCanvas } from "./EditorCanvas";
import { DocumentShareDrawer } from "./DocumentShareDrawer";
import { EditorSidebar } from "./EditorSidebar";
import { OutlinePanel } from "./OutlinePanel";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { useMockAutoSave } from "../hooks/useMockAutoSave";
import {
  cloneContent,
  createEmptyDocumentContent,
  knowledgeFolders,
} from "../data/knowledgeDocuments";
import { loadKnowledgeEditorState, saveKnowledgeEditorState } from "../storage/knowledgeStorage";
import { exportKnowledgeState, parseKnowledgeImport } from "../utils/knowledgeTransfer";
import {
  compareDocumentVersions,
  createDocument,
  deleteDocumentAttachment,
  getBootstrap,
  getDocument,
  getDocumentAttachments,
  getDocumentActivity,
  getDocumentContext,
  getDocumentVersions,
  publishDocumentVersion,
  restoreDocumentVersion,
  unpublishDocumentVersion,
  updateDocument,
  type CompareDocumentVersionsResponse,
  type DocumentActivityResponse,
  type DocumentAttachmentDto,
  type DocumentContextResponse,
  type DocumentVersionsResponse,
  type KnowledgeDocumentDto,
  type KnowledgeDocumentSummaryDto,
  type KnowledgeFolderDto,
} from "../lib/appApi";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import {
  createEditorVersionTrailRowsFromVersions,
  createEditorDocumentContextPanelModel,
  formatDocumentStatus,
  type EditorContextLoadStatus,
} from "../lib/editorDocumentContextModel";
import {
  formatEditorOperationError,
  getApiSaveStatusLabel,
  toTopBarSaveStatus,
  type ApiSaveStatus,
  type EditorApiLoadStatus,
} from "../lib/editorReliabilityModel";
import {
  documentAttachmentChangedEvent,
  type DocumentAttachmentChangedEventDetail,
  uploadDocumentAttachment,
  validateDocumentAttachmentFiles,
} from "../lib/documentFilesModel";
import { createLibrariesHash, createSearchHash, createShareHash } from "../lib/hashRouting";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";
import type { AnchorMatchResult } from "../lib/commentAnchorMatching";
import type { AnchorRelocationResult } from "../lib/commentAnchorRelocation";
import {
  beginCommentLoad,
  beginThreadLifecycleAction,
  createThreadLifecycleActionState,
  finishCommentLoadFailure,
  finishCommentLoadSuccess,
  finishThreadLifecycleActionFailure,
  finishThreadLifecycleActionSuccess,
  getCommentLoadState,
  markComposerSubmitFailed,
  markComposerSubmitting,
  type CommentLoadStatesByDocumentId,
} from "../lib/commentProductionState";
import { createCommentRepository } from "../lib/commentRepository";
import {
  prependThreadForDocument,
  replaceThreadForDocument,
  selectCommentThreadsForDocument,
  setThreadsForDocument,
} from "../lib/commentThreadState";
import type {
  CommentAnchorStatus,
  CommentFocusRequest,
  CommentRuntimeAnchorState,
  CommentThread,
  CreateCommentThreadRequest,
  KnowledgeDocument,
  KnowledgeFolder,
  OutlineFocusRequest,
  OutlineItem,
  PendingCommentComposer,
  TiptapContentChange,
  TiptapContentStats,
} from "../types/editor";

type TransferMessage = {
  type: "success" | "error";
  text: string;
};

type DocumentAttachmentLoadState = "demo" | "error" | "forbidden" | "idle" | "loading" | "ready";
type DocumentVersionLoadState = "demo" | "error" | "forbidden" | "idle" | "loading" | "ready";
type DocumentVersionOperation = "compare" | "publish" | "restore" | "unpublish" | null;

const EMPTY_COMMENT_THREADS: CommentThread[] = [];
const EMPTY_COMMENT_MATCH_RESULTS: Record<string, AnchorMatchResult> = {};
const EMPTY_COMMENT_RELOCATION_RESULTS: Record<string, AnchorRelocationResult> = {};

export function KnowledgeEditorPage({ requestedDocumentId = null }: { requestedDocumentId?: string | null }) {
  const [initialEditorState] = useState(loadKnowledgeEditorState);
  const [isApiMode] = useState(() => Boolean(getConfiguredApiBaseUrl()));
  const [documents, setDocuments] = useState<KnowledgeDocument[]>(initialEditorState.documents);
  const [folders, setFolders] = useState<KnowledgeFolder[]>(knowledgeFolders);
  const [activeDocumentId, setActiveDocumentId] = useState(initialEditorState.activeDocumentId);
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(() => {
    const initialDocument =
      initialEditorState.documents.find((document) => document.id === initialEditorState.activeDocumentId) ??
      initialEditorState.documents[0];

    return initialDocument?.folderId ?? knowledgeFolders[0]?.id ?? null;
  });
  const [editorApiLoadStatus, setEditorApiLoadStatus] = useState<EditorApiLoadStatus>(() =>
    getConfiguredApiBaseUrl() ? "loading" : "demo",
  );
  const [editorApiLoadError, setEditorApiLoadError] = useState<string | null>(null);
  const [editorApiLoadRetryKey, setEditorApiLoadRetryKey] = useState(0);
  const [workspaceName, setWorkspaceName] = useState("Northstar");
  const [workspaceId, setWorkspaceId] = useState<string | null>(null);
  const [activeLibraryId, setActiveLibraryId] = useState<string | null>(null);
  const [activeLibraryName, setActiveLibraryName] = useState("Atlas Library");
  const [documentContextResponse, setDocumentContextResponse] = useState<DocumentContextResponse | null>(null);
  const [documentActivityResponse, setDocumentActivityResponse] = useState<DocumentActivityResponse | null>(null);
  const [documentContextStatus, setDocumentContextStatus] = useState<EditorContextLoadStatus>(() =>
    getConfiguredApiBaseUrl() ? "idle" : "demo",
  );
  const [documentVersionsResponse, setDocumentVersionsResponse] = useState<DocumentVersionsResponse | null>(null);
  const [documentVersionsStatus, setDocumentVersionsStatus] = useState<DocumentVersionLoadState>(() =>
    getConfiguredApiBaseUrl() ? "idle" : "demo",
  );
  const [documentVersionsError, setDocumentVersionsError] = useState<string | null>(null);
  const [documentVersionsReloadKey, setDocumentVersionsReloadKey] = useState(0);
  const [documentVersionCompare, setDocumentVersionCompare] = useState<CompareDocumentVersionsResponse | null>(null);
  const [documentVersionOperation, setDocumentVersionOperation] = useState<DocumentVersionOperation>(null);
  const [documentAttachments, setDocumentAttachments] = useState<DocumentAttachmentDto[]>([]);
  const [documentAttachmentsStatus, setDocumentAttachmentsStatus] = useState<DocumentAttachmentLoadState>(() =>
    getConfiguredApiBaseUrl() ? "idle" : "demo",
  );
  const [documentAttachmentsError, setDocumentAttachmentsError] = useState<string | null>(null);
  const [documentAttachmentRemovePendingId, setDocumentAttachmentRemovePendingId] = useState<string | null>(null);
  const [documentAttachmentUploadStatus, setDocumentAttachmentUploadStatus] = useState<"error" | "idle" | "uploading">("idle");
  const [documentAttachmentUploadError, setDocumentAttachmentUploadError] = useState<string | null>(null);
  const [documentAttachmentsReloadKey, setDocumentAttachmentsReloadKey] = useState(0);
  const [apiSaveStatus, setApiSaveStatus] = useState<ApiSaveStatus>("saved");
  const [isContentEmpty, setIsContentEmpty] = useState(false);
  const [contentTextLength, setContentTextLength] = useState(0);
  const [outlineItems, setOutlineItems] = useState<OutlineItem[]>([]);
  const [outlineFocusRequest, setOutlineFocusRequest] = useState<OutlineFocusRequest | null>(null);
  const [commentThreadsByDocumentId, setCommentThreadsByDocumentId] = useState<Record<string, CommentThread[]>>({});
  const [commentAnchorStatusesByDocumentId, setCommentAnchorStatusesByDocumentId] = useState<
    Record<string, Record<string, CommentAnchorStatus>>
  >({});
  const [commentMatchResultsByDocumentId, setCommentMatchResultsByDocumentId] = useState<
    Record<string, Record<string, AnchorMatchResult>>
  >({});
  const [commentRelocationResultsByDocumentId, setCommentRelocationResultsByDocumentId] = useState<
    Record<string, Record<string, AnchorRelocationResult>>
  >({});
  const [commentLoadStatesByDocumentId, setCommentLoadStatesByDocumentId] =
    useState<CommentLoadStatesByDocumentId>({});
  const [commentLifecycleActionState, setCommentLifecycleActionState] =
    useState(createThreadLifecycleActionState);
  const [activeCommentThreadId, setActiveCommentThreadId] = useState<string | null>(null);
  const [commentFocusRequest, setCommentFocusRequest] = useState<CommentFocusRequest | null>(null);
  const [pendingCommentComposer, setPendingCommentComposer] = useState<PendingCommentComposer | null>(null);
  const [transferMessage, setTransferMessage] = useState<TransferMessage | null>(null);
  const [isShareDrawerOpen, setIsShareDrawerOpen] = useState(false);
  const [isOutlinePanelCollapsed, setIsOutlinePanelCollapsed] = useState(false);
  const activeDocumentIdRef = useRef(activeDocumentId);
  const documentsRef = useRef(documents);
  const commentRepositoryRef = useRef(createCommentRepository());
  const commentLoadRequestIdRef = useRef(0);
  const latestCommentLoadRequestByDocumentIdRef = useRef<Record<string, number>>({});
  const pendingCommentComposerRef = useRef<PendingCommentComposer | null>(null);
  const createThreadInFlightByDocumentIdRef = useRef<Record<string, true>>({});
  const addCommentMessageInFlightByThreadIdRef = useRef<Record<string, true>>({});
  const lifecycleActionInFlightByThreadIdRef = useRef<Record<string, true>>({});
  const apiConflictedDocumentIdsRef = useRef<Record<string, true>>({});
  const apiSaveTimerRef = useRef<number>();
  const saveToStorageTimerRef = useRef<number>();
  const saveStateInitializedRef = useRef(false);
  const {
    markCreated: markMockCreated,
    markDirty: markMockDirty,
    resetSaved: resetMockSaved,
    saveStatus: mockSaveStatus,
    updatedAtLabel: mockUpdatedAtLabel,
  } = useMockAutoSave();
  const activeDocument = documents.find((document) => document.id === activeDocumentId) ?? documents[0];
  const activeCommentThreads = activeDocument
    ? selectCommentThreadsForDocument(
        commentThreadsByDocumentId,
        commentAnchorStatusesByDocumentId,
        activeDocument.id,
      )
    : EMPTY_COMMENT_THREADS;
  const activeCommentMatchResults = activeDocument
    ? commentMatchResultsByDocumentId[activeDocument.id] ?? EMPTY_COMMENT_MATCH_RESULTS
    : EMPTY_COMMENT_MATCH_RESULTS;
  const activeCommentRelocationResults = activeDocument
    ? commentRelocationResultsByDocumentId[activeDocument.id] ?? EMPTY_COMMENT_RELOCATION_RESULTS
    : EMPTY_COMMENT_RELOCATION_RESULTS;
  const activeCommentLoadState = activeDocument
    ? getCommentLoadState(commentLoadStatesByDocumentId, activeDocument.id)
    : { status: "idle" as const };
  const effectiveSaveStatus = isApiMode ? toTopBarSaveStatus(apiSaveStatus) : mockSaveStatus;
  const atlasSaveStatusLabel = isApiMode
    ? getApiSaveStatusLabel(apiSaveStatus)
    : mockSaveStatus === "saving"
      ? "Saving"
      : mockSaveStatus === "editing"
        ? "Editing"
        : mockSaveStatus === "created"
          ? "Created"
          : "Saved";
  const effectiveUpdatedAtLabel = isApiMode
    ? formatDocumentUpdatedAt(activeDocument?.updatedAt)
    : mockUpdatedAtLabel;
  const activeDocumentFolder = activeDocument
    ? folders.find((folder) => folder.id === activeDocument.folderId)
    : null;
  const libraryHref = createLibrariesHash({ libraryId: activeLibraryId });
  const folderHref = createLibrariesHash({
    collectionId: activeDocument?.folderId,
    libraryId: activeLibraryId,
  });
  const shareHref = createShareHash(activeDocument?.id);
  const versionHistoryHref = activeDocument
    ? `#versions?documentId=${encodeURIComponent(activeDocument.id)}`
    : "#versions";
  const documentStatusLabel = formatDocumentStatus(activeDocument?.status);
  const documentContextPanelModel = isApiMode
    ? createEditorDocumentContextPanelModel(documentContextResponse, documentActivityResponse)
    : null;
  const documentVersionTrailRows = isApiMode
    ? createEditorVersionTrailRowsFromVersions(documentVersionsResponse?.versions)
    : null;
  const atlasPatternStyle = {
    "--atlas-coordinate-pattern": `url(${coordinatePatternUrl})`,
    "--atlas-route-pattern": `url(${routePatternUrl})`,
    "--atlas-topographic-pattern": `url(${topographicPatternUrl})`,
  } as CSSProperties;

  useEffect(() => {
    activeDocumentIdRef.current = activeDocumentId;
  }, [activeDocumentId]);

  useEffect(() => {
    documentsRef.current = documents;
  }, [documents]);

  useEffect(() => {
    pendingCommentComposerRef.current = pendingCommentComposer;
  }, [pendingCommentComposer]);

  useEffect(() => {
    if (!isApiMode) {
      return;
    }

    const controller = new AbortController();
    setEditorApiLoadStatus("loading");
    setEditorApiLoadError(null);

    void getBootstrap(controller.signal)
      .then(async (bootstrap) => {
        const targetDocumentId = requestedDocumentId ?? bootstrap.activeDocumentId;
        const activeDocumentResponse = await getDocument(targetDocumentId, controller.signal);

        if (controller.signal.aborted) {
          return;
        }

        const summaryDocuments = bootstrap.documents.map(mapDocumentSummaryDto);
        const activeApiDocument = mapDocumentDto(activeDocumentResponse.document);
        const nextDocuments = upsertDocument(summaryDocuments, activeApiDocument);
        const activeSpace =
          bootstrap.spaces.find((space) => space.id === bootstrap.activeSpaceId) ??
          bootstrap.spaces.find((space) => space.id === bootstrap.workspace.currentSpaceId) ??
          bootstrap.spaces[0];

        setWorkspaceName(bootstrap.workspace.name);
        setWorkspaceId(bootstrap.workspace.id);
        setActiveLibraryId(activeSpace?.id ?? bootstrap.activeSpaceId ?? null);
        setActiveLibraryName(activeSpace?.name ?? "Atlas Library");
        setFolders(bootstrap.folders.map(mapFolderDto));
        setDocuments(nextDocuments);
        documentsRef.current = nextDocuments;
        activeDocumentIdRef.current = activeApiDocument.id;
        setActiveDocumentId(activeApiDocument.id);
        setApiSaveStatus("saved");
        setEditorApiLoadStatus("ready");
      })
      .catch((error) => {
        if (controller.signal.aborted) {
          return;
        }

        setDocuments([]);
        setEditorApiLoadError(formatEditorOperationError(error, "Document API load failed."));
        setEditorApiLoadStatus("error");
      });

    return () => controller.abort();
  }, [editorApiLoadRetryKey, isApiMode, requestedDocumentId]);

  useEffect(() => {
    if (isApiMode) {
      return;
    }

    window.clearTimeout(saveToStorageTimerRef.current);

    saveToStorageTimerRef.current = window.setTimeout(() => {
      saveKnowledgeEditorState({ activeDocumentId, documents });
    }, 300);

    return () => window.clearTimeout(saveToStorageTimerRef.current);
  }, [activeDocumentId, documents, isApiMode]);

  useEffect(() => {
    if (isApiMode || !activeDocument || saveStateInitializedRef.current) {
      return;
    }

    saveStateInitializedRef.current = true;
    resetMockSaved(new Date(activeDocument.updatedAt));
  }, [activeDocument, isApiMode, resetMockSaved]);

  useEffect(() => {
    if (!isApiMode) {
      setDocumentContextStatus("demo");
      setDocumentContextResponse(null);
      setDocumentActivityResponse(null);
      return;
    }

    if (!activeDocument || editorApiLoadStatus !== "ready") {
      setDocumentContextStatus("idle");
      setDocumentContextResponse(null);
      setDocumentActivityResponse(null);
      return;
    }

    const controller = new AbortController();

    setDocumentContextStatus("loading");
    setDocumentContextResponse(null);
    setDocumentActivityResponse(null);

    void Promise.all([
      getDocumentContext(activeDocument.id, controller.signal),
      getDocumentActivity(activeDocument.id, controller.signal),
    ])
      .then(([contextResponse, activityResponse]) => {
        if (controller.signal.aborted) {
          return;
        }

        setDocumentContextResponse(contextResponse);
        setDocumentActivityResponse(activityResponse);
        setDocumentContextStatus("ready");
      })
      .catch(() => {
        if (controller.signal.aborted) {
          return;
        }

        setDocumentContextStatus("error");
      });

    return () => controller.abort();
  }, [activeDocument?.id, editorApiLoadStatus, isApiMode]);

  useEffect(() => {
    if (!isApiMode) {
      setDocumentVersionsResponse(null);
      setDocumentVersionsStatus("demo");
      setDocumentVersionsError(null);
      setDocumentVersionCompare(null);
      return;
    }

    if (!activeDocument || editorApiLoadStatus !== "ready") {
      setDocumentVersionsResponse(null);
      setDocumentVersionsStatus("idle");
      setDocumentVersionsError(null);
      setDocumentVersionCompare(null);
      return;
    }

    const controller = new AbortController();
    setDocumentVersionsStatus("loading");
    setDocumentVersionsError(null);

    void getDocumentVersions(activeDocument.id, controller.signal)
      .then((response) => {
        if (controller.signal.aborted) {
          return;
        }

        setDocumentVersionsResponse(response);
        setDocumentVersionsStatus("ready");
      })
      .catch((error) => {
        if (controller.signal.aborted) {
          return;
        }

        setDocumentVersionsResponse(null);
        setDocumentVersionsStatus(
          error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error",
        );
        setDocumentVersionsError(formatEditorOperationError(error, "Document versions could not be loaded."));
      });

    return () => controller.abort();
  }, [activeDocument?.id, documentVersionsReloadKey, editorApiLoadStatus, isApiMode]);

  useEffect(() => {
    if (!isApiMode) {
      setDocumentAttachments([]);
      setDocumentAttachmentsStatus("demo");
      setDocumentAttachmentsError(null);
      setDocumentAttachmentUploadStatus("idle");
      setDocumentAttachmentUploadError(null);
      return;
    }

    if (!activeDocument || editorApiLoadStatus !== "ready") {
      setDocumentAttachments([]);
      setDocumentAttachmentsStatus("idle");
      setDocumentAttachmentsError(null);
      setDocumentAttachmentUploadStatus("idle");
      setDocumentAttachmentUploadError(null);
      return;
    }

    const controller = new AbortController();
    setDocumentAttachmentsStatus("loading");
    setDocumentAttachmentsError(null);

    void getDocumentAttachments(activeDocument.id, controller.signal)
      .then((response) => {
        if (controller.signal.aborted) {
          return;
        }

        setDocumentAttachments(response.attachments);
        setDocumentAttachmentsStatus("ready");
      })
      .catch((error) => {
        if (controller.signal.aborted) {
          return;
        }

        setDocumentAttachments([]);
        setDocumentAttachmentsStatus(
          error instanceof ApiClientError && (error.status === 401 || error.status === 403) ? "forbidden" : "error",
        );
        setDocumentAttachmentsError(formatEditorOperationError(error, "Document attachments could not be loaded."));
      });

    return () => controller.abort();
  }, [activeDocument?.id, documentAttachmentsReloadKey, editorApiLoadStatus, isApiMode]);

  useEffect(() => {
    if (!isApiMode) {
      return;
    }

    const handleAttachmentChange = (event: Event) => {
      const detail = (event as CustomEvent<DocumentAttachmentChangedEventDetail>).detail;
      if (detail?.documentId && detail.documentId === activeDocumentIdRef.current) {
        setDocumentAttachmentsReloadKey((currentKey) => currentKey + 1);
      }
    };

    window.addEventListener(documentAttachmentChangedEvent, handleAttachmentChange);

    return () => window.removeEventListener(documentAttachmentChangedEvent, handleAttachmentChange);
  }, [isApiMode]);

  useEffect(() => () => window.clearTimeout(apiSaveTimerRef.current), []);

  useEffect(() => {
    if (!transferMessage) {
      return;
    }

    const timer = window.setTimeout(() => setTransferMessage(null), 3200);

    return () => window.clearTimeout(timer);
  }, [transferMessage]);

  const saveDocumentToApi = useCallback(
    async function saveDocumentToApi(documentId: string) {
      if (!isApiMode || apiConflictedDocumentIdsRef.current[documentId]) {
        return;
      }

      const document = documentsRef.current.find((item) => item.id === documentId);
      if (!document || typeof document.revision !== "number") {
        setApiSaveStatus("error");
        setTransferMessage({ type: "error", text: "Document revision is not available." });
        return;
      }

      const submittedSnapshot = createDocumentSaveSnapshot(document);
      setApiSaveStatus("saving");

      try {
        const response = await updateDocument(documentId, {
          baseRevision: document.revision,
          content: document.content,
          tags: document.tags ?? [],
          title: document.title,
        });
        const savedDocument = mapDocumentDto(response.document);
        const latestDocument = documentsRef.current.find((item) => item.id === documentId);
        const hasNewChanges =
          latestDocument !== undefined &&
          createDocumentSaveSnapshot(latestDocument) !== submittedSnapshot;
        const nextDocuments = documentsRef.current.map((currentDocument) => {
          if (currentDocument.id !== documentId) {
            return currentDocument;
          }

          return hasNewChanges
            ? {
                ...currentDocument,
                revision: savedDocument.revision,
                updatedAt: savedDocument.updatedAt,
              }
            : savedDocument;
        });

        documentsRef.current = nextDocuments;
        setDocuments(nextDocuments);

        const { [documentId]: _resolvedConflict, ...remainingConflicts } =
          apiConflictedDocumentIdsRef.current;
        apiConflictedDocumentIdsRef.current = remainingConflicts;

        if (hasNewChanges) {
          setApiSaveStatus("editing");
          window.clearTimeout(apiSaveTimerRef.current);
          apiSaveTimerRef.current = window.setTimeout(() => {
            void saveDocumentToApi(documentId);
          }, 350);
          return;
        }

        setApiSaveStatus("saved");
      } catch (error) {
        if (isConflictError(error)) {
          apiConflictedDocumentIdsRef.current = {
            ...apiConflictedDocumentIdsRef.current,
            [documentId]: true,
          };
          setApiSaveStatus("conflict");
          setTransferMessage({ type: "error", text: "Document changed elsewhere. Reload before saving." });
          return;
        }

        setApiSaveStatus("error");
        setTransferMessage({
          type: "error",
          text: formatEditorOperationError(error, "Save failed."),
        });
      }
    },
    [isApiMode],
  );

  const markDocumentDirty = useCallback(
    (documentId: string) => {
      if (!isApiMode) {
        markMockDirty();
        return;
      }

      if (apiConflictedDocumentIdsRef.current[documentId]) {
        setApiSaveStatus("conflict");
        setTransferMessage({ type: "error", text: "Resolve the document conflict before saving again." });
        return;
      }

      setApiSaveStatus("editing");
      window.clearTimeout(apiSaveTimerRef.current);
      apiSaveTimerRef.current = window.setTimeout(() => {
        void saveDocumentToApi(documentId);
      }, 850);
    },
    [isApiMode, markMockDirty, saveDocumentToApi],
  );

  const loadDocumentFromApi = useCallback(
    async (documentId: string) => {
      if (!isApiMode) {
        return;
      }

      try {
        const response = await getDocument(documentId);
        const apiDocument = mapDocumentDto(response.document);
        const { [documentId]: _resolvedConflict, ...remainingConflicts } =
          apiConflictedDocumentIdsRef.current;

        apiConflictedDocumentIdsRef.current = remainingConflicts;
        setDocuments((currentDocuments) => {
          const nextDocuments = upsertDocument(currentDocuments, apiDocument);
          documentsRef.current = nextDocuments;
          return nextDocuments;
        });
        setApiSaveStatus("saved");
      } catch (error) {
        setApiSaveStatus("error");
        setTransferMessage({
          type: "error",
          text: formatEditorOperationError(error, "Document load failed."),
        });
      }
    },
    [isApiMode],
  );

  const loadCommentThreadsForDocument = useCallback((documentId: string) => {
    const requestId = commentLoadRequestIdRef.current + 1;

    commentLoadRequestIdRef.current = requestId;
    latestCommentLoadRequestByDocumentIdRef.current = {
      ...latestCommentLoadRequestByDocumentIdRef.current,
      [documentId]: requestId,
    };
    setCommentLoadStatesByDocumentId((currentStatesByDocumentId) =>
      beginCommentLoad(currentStatesByDocumentId, documentId, requestId),
    );

    void commentRepositoryRef.current
      .listThreads(documentId)
      .then((threads) => {
        if (
          latestCommentLoadRequestByDocumentIdRef.current[documentId] !== requestId ||
          activeDocumentIdRef.current !== documentId
        ) {
          return;
        }

        setCommentThreadsByDocumentId((currentThreadsByDocumentId) =>
          setThreadsForDocument(currentThreadsByDocumentId, documentId, threads),
        );
        setCommentLoadStatesByDocumentId((currentStatesByDocumentId) =>
          finishCommentLoadSuccess(
            currentStatesByDocumentId,
            documentId,
            requestId,
            activeDocumentIdRef.current,
          ),
        );
      })
      .catch((error) => {
        if (
          latestCommentLoadRequestByDocumentIdRef.current[documentId] !== requestId ||
          activeDocumentIdRef.current !== documentId
        ) {
          return;
        }

        setCommentLoadStatesByDocumentId((currentStatesByDocumentId) =>
          finishCommentLoadFailure(
            currentStatesByDocumentId,
            documentId,
            requestId,
            activeDocumentIdRef.current,
            error,
          ),
        );
      });
  }, []);

  useEffect(() => {
    if (!activeDocument) {
      return;
    }

    loadCommentThreadsForDocument(activeDocument.id);
  }, [activeDocument?.id, loadCommentThreadsForDocument]);

  const handleTitleChange = useCallback(
    (nextTitle: string) => {
      const updatedAt = new Date().toISOString();

      setDocuments((currentDocuments) =>
        currentDocuments.map((document) =>
          document.id === activeDocumentId
            ? {
                ...document,
                title: nextTitle,
                updatedAt,
              }
            : document,
        ),
      );
      markDocumentDirty(activeDocumentId);
    },
    [activeDocumentId, markDocumentDirty],
  );

  const handleContentStatsChange = useCallback((stats: TiptapContentStats) => {
    setIsContentEmpty(stats.isEmpty);
    setContentTextLength(stats.textLength);
    setOutlineItems(stats.outlineItems);
  }, []);

  const handleContentChange = useCallback(
    (change: TiptapContentChange) => {
      const updatedAt = new Date().toISOString();

      setIsContentEmpty(change.isEmpty);
      setContentTextLength(change.textLength);
      setOutlineItems(change.outlineItems);
      setDocuments((currentDocuments) =>
        currentDocuments.map((document) =>
          document.id === activeDocumentId
            ? {
                ...document,
                content: change.content,
                updatedAt,
              }
            : document,
        ),
      );
      markDocumentDirty(activeDocumentId);
    },
    [activeDocumentId, markDocumentDirty],
  );

  const handleOutlineItemClick = useCallback((item: OutlineItem) => {
    setOutlineFocusRequest((currentRequest) => ({
      pos: item.pos,
      requestId: (currentRequest?.requestId ?? 0) + 1,
    }));
  }, []);

  const handleOpenCommentComposer = useCallback((composer: PendingCommentComposer) => {
    setPendingCommentComposer({
      ...composer,
      body: "",
      error: null,
      isSubmitting: false,
    });
    setActiveCommentThreadId(null);
    setCommentFocusRequest(null);
  }, []);

  const handleCancelPendingCommentComposer = useCallback(() => {
    setPendingCommentComposer((currentComposer) =>
      currentComposer?.isSubmitting ? currentComposer : null,
    );
  }, []);

  const handlePendingCommentBodyChange = useCallback((body: string) => {
    setPendingCommentComposer((currentComposer) =>
      currentComposer
        ? {
            ...currentComposer,
            body,
            error: currentComposer.error ? null : currentComposer.error,
          }
        : currentComposer,
    );
  }, []);

  const handleCreateCommentThread = useCallback(async (request: CreateCommentThreadRequest) => {
    const documentId = activeDocumentId;
    const currentComposer = pendingCommentComposerRef.current;

    if (
      request.anchor.documentId !== documentId ||
      !currentComposer ||
      currentComposer.documentId !== documentId ||
      currentComposer.anchor !== request.anchor ||
      createThreadInFlightByDocumentIdRef.current[documentId]
    ) {
      return;
    }

    const submitState = markComposerSubmitting(currentComposer, request.body);

    if (!submitState.accepted) {
      return;
    }

    createThreadInFlightByDocumentIdRef.current = {
      ...createThreadInFlightByDocumentIdRef.current,
      [documentId]: true,
    };
    setPendingCommentComposer((composer) =>
      composer && composer.documentId === documentId && composer.anchor === request.anchor
        ? submitState.composer
        : composer,
    );

    try {
      const thread = await commentRepositoryRef.current.createThread(documentId, {
        anchor: request.anchor,
        body: request.body,
      });

      setCommentThreadsByDocumentId((currentThreadsByDocumentId) =>
        prependThreadForDocument(currentThreadsByDocumentId, documentId, thread),
      );

      if (activeDocumentIdRef.current !== documentId) {
        return;
      }

      setPendingCommentComposer(null);
      setActiveCommentThreadId(thread.id);
      setCommentFocusRequest((currentRequest) => ({
        threadId: thread.id,
        requestId: (currentRequest?.requestId ?? 0) + 1,
      }));
    } catch (error) {
      if (activeDocumentIdRef.current === documentId) {
        setPendingCommentComposer((composer) =>
          composer && composer.documentId === documentId && composer.anchor === request.anchor
            ? markComposerSubmitFailed(composer, request.body, error)
            : composer,
        );
      }
    } finally {
      const { [documentId]: _completedRequest, ...nextRequestsByDocumentId } =
        createThreadInFlightByDocumentIdRef.current;

      createThreadInFlightByDocumentIdRef.current = nextRequestsByDocumentId;
    }
  }, [activeDocumentId]);

  const handleAddCommentMessage = useCallback(async (threadId: string, body: string) => {
    const documentId = activeDocumentId;

    if (addCommentMessageInFlightByThreadIdRef.current[threadId]) {
      return;
    }

    addCommentMessageInFlightByThreadIdRef.current = {
      ...addCommentMessageInFlightByThreadIdRef.current,
      [threadId]: true,
    };

    try {
      const thread = await commentRepositoryRef.current.addMessage(documentId, threadId, { body });

      setCommentThreadsByDocumentId((currentThreadsByDocumentId) =>
        replaceThreadForDocument(currentThreadsByDocumentId, documentId, thread),
      );
    } catch (error) {
      throw error;
    } finally {
      const { [threadId]: _completedRequest, ...nextRequestsByThreadId } =
        addCommentMessageInFlightByThreadIdRef.current;

      addCommentMessageInFlightByThreadIdRef.current = nextRequestsByThreadId;
    }
  }, [activeDocumentId]);

  const handleCommentRuntimeStateChange = useCallback(
    (runtimeState: CommentRuntimeAnchorState) => {
      setCommentMatchResultsByDocumentId((currentMatchResultsByDocumentId) => {
        const currentMatchResults = currentMatchResultsByDocumentId[activeDocumentId] ?? {};

        if (areMatchResultRecordsEqual(currentMatchResults, runtimeState.matchResultByThreadId)) {
          return currentMatchResultsByDocumentId;
        }

        return {
          ...currentMatchResultsByDocumentId,
          [activeDocumentId]: runtimeState.matchResultByThreadId,
        };
      });

      setCommentAnchorStatusesByDocumentId((currentStatusesByDocumentId) => {
        const currentStatuses = currentStatusesByDocumentId[activeDocumentId] ?? {};

        if (areAnchorStatusRecordsEqual(currentStatuses, runtimeState.anchorStatusByThreadId)) {
          return currentStatusesByDocumentId;
        }

        return {
          ...currentStatusesByDocumentId,
          [activeDocumentId]: runtimeState.anchorStatusByThreadId,
        };
      });

      setCommentRelocationResultsByDocumentId((currentRelocationResultsByDocumentId) => {
        const currentRelocationResults = currentRelocationResultsByDocumentId[activeDocumentId] ?? {};

        if (areRelocationResultRecordsEqual(currentRelocationResults, runtimeState.relocationResultByThreadId)) {
          return currentRelocationResultsByDocumentId;
        }

        return {
          ...currentRelocationResultsByDocumentId,
          [activeDocumentId]: runtimeState.relocationResultByThreadId,
        };
      });
    },
    [activeDocumentId],
  );

  const handleSelectCommentThread = useCallback((threadId: string) => {
    setActiveCommentThreadId(threadId);
  }, []);

  const handleCommentThreadClick = useCallback((thread: CommentThread) => {
    setActiveCommentThreadId(thread.id);
    if (thread.anchorStatus !== "orphaned") {
      setCommentFocusRequest((currentRequest) => ({
        threadId: thread.id,
        requestId: (currentRequest?.requestId ?? 0) + 1,
      }));
    }
  }, []);

  const runCommentThreadLifecycleAction = useCallback(async (
    threadId: string,
    action: "resolve" | "reopen",
  ) => {
    if (lifecycleActionInFlightByThreadIdRef.current[threadId]) {
      return;
    }

    const documentId = activeDocumentId;

    lifecycleActionInFlightByThreadIdRef.current = {
      ...lifecycleActionInFlightByThreadIdRef.current,
      [threadId]: true,
    };
    setCommentLifecycleActionState((currentActionState) =>
      beginThreadLifecycleAction(currentActionState, threadId).state,
    );

    try {
      const thread =
        action === "resolve"
          ? await commentRepositoryRef.current.resolveThread(documentId, threadId)
          : await commentRepositoryRef.current.reopenThread(documentId, threadId);

      setCommentThreadsByDocumentId((currentThreadsByDocumentId) =>
        replaceThreadForDocument(currentThreadsByDocumentId, documentId, thread),
      );

      setCommentLifecycleActionState((currentActionState) =>
        finishThreadLifecycleActionSuccess(currentActionState, threadId),
      );
    } catch (error) {
      if (activeDocumentIdRef.current === documentId) {
        setCommentLifecycleActionState((currentActionState) =>
          finishThreadLifecycleActionFailure(currentActionState, threadId, error),
        );
      }
    } finally {
      const { [threadId]: _completedRequest, ...nextRequestsByThreadId } =
        lifecycleActionInFlightByThreadIdRef.current;

      lifecycleActionInFlightByThreadIdRef.current = nextRequestsByThreadId;
    }
  }, [activeDocumentId]);

  const handleResolveCommentThread = useCallback((threadId: string) => {
    void runCommentThreadLifecycleAction(threadId, "resolve");
  }, [runCommentThreadLifecycleAction]);

  const handleReopenCommentThread = useCallback((threadId: string) => {
    void runCommentThreadLifecycleAction(threadId, "reopen");
  }, [runCommentThreadLifecycleAction]);

  const handleSelectDocument = useCallback(
    (documentId: string) => {
      if (documentId === activeDocumentId) {
        return;
      }

      const selectedDocument = documents.find((document) => document.id === documentId);

      if (!selectedDocument) {
        return;
      }

      activeDocumentIdRef.current = documentId;
      setActiveDocumentId(documentId);
      setSelectedFolderId(selectedDocument.folderId);
      setOutlineFocusRequest(null);
      setActiveCommentThreadId(null);
      setCommentFocusRequest(null);
      setPendingCommentComposer(null);
      setCommentLifecycleActionState(createThreadLifecycleActionState());
      if (isApiMode) {
        setApiSaveStatus("saved");
        void loadDocumentFromApi(documentId);
      } else {
        resetMockSaved(new Date(selectedDocument.updatedAt));
      }
    },
    [activeDocumentId, documents, isApiMode, loadDocumentFromApi, resetMockSaved],
  );

  const handleSelectFolder = useCallback((folderId: string) => {
    setSelectedFolderId(folderId);
  }, []);

  const handleCreateDocument = useCallback(() => {
    const now = new Date();
    const folderId =
      (selectedFolderId && folders.some((folder) => folder.id === selectedFolderId) ? selectedFolderId : null) ??
      activeDocument?.folderId ??
      folders[0]?.id ??
      knowledgeFolders[0]?.id ??
      "product";

    if (isApiMode) {
      setApiSaveStatus("saving");
      void createDocument({ folderId, title: "Untitled Field Note" })
        .then((response) => {
          const createdDocument = mapDocumentDto(response.document);
          const nextDocuments = upsertDocument(
            response.map.documents.map(mapDocumentSummaryDto),
            createdDocument,
          );

          setFolders(response.map.folders.map(mapFolderDto));
          setDocuments(nextDocuments);
          documentsRef.current = nextDocuments;
          activeDocumentIdRef.current = createdDocument.id;
          setActiveDocumentId(createdDocument.id);
          setSelectedFolderId(createdDocument.folderId);
          setIsContentEmpty(true);
          setContentTextLength(0);
          setOutlineItems([]);
          setOutlineFocusRequest(null);
          setActiveCommentThreadId(null);
          setCommentFocusRequest(null);
          setPendingCommentComposer(null);
          setCommentLifecycleActionState(createThreadLifecycleActionState());
          setApiSaveStatus("created");
        })
        .catch((error) => {
          setApiSaveStatus("error");
          setTransferMessage({
            type: "error",
            text: formatEditorOperationError(error, "Document create failed."),
          });
        });
      return;
    }

    const newDocument: KnowledgeDocument = {
      id: `doc-${now.getTime()}`,
      title: "Untitled Field Note",
      folderId,
      updatedAt: now.toISOString(),
      tags: [],
      content: createEmptyDocumentContent(),
    };

    setDocuments((currentDocuments) => [newDocument, ...currentDocuments]);
    activeDocumentIdRef.current = newDocument.id;
    setActiveDocumentId(newDocument.id);
    setSelectedFolderId(folderId);
    setIsContentEmpty(true);
    setContentTextLength(0);
    setOutlineItems([]);
    setOutlineFocusRequest(null);
    setActiveCommentThreadId(null);
    setCommentFocusRequest(null);
    setPendingCommentComposer(null);
    setCommentLifecycleActionState(createThreadLifecycleActionState());
    markMockCreated(now);
  }, [activeDocument?.folderId, folders, isApiMode, markMockCreated, selectedFolderId]);

  const handleSearch = useCallback(
    (query: string) => {
      const trimmedQuery = query.trim();

      window.location.hash = createSearchHash({
        folderId: activeDocument?.folderId,
        folderTitle: activeDocumentFolder?.title,
        libraryId: activeLibraryId,
        q: trimmedQuery || null,
      });
    },
    [activeDocument?.folderId, activeDocumentFolder?.title, activeLibraryId],
  );

  const handleExportJson = useCallback(() => {
    exportKnowledgeState({ activeDocumentId, documents });
    setTransferMessage({ type: "success", text: "已导出 JSON" });
  }, [activeDocumentId, documents]);

  const handleImportJsonFile = useCallback(
    async (file: File) => {
      if (isApiMode) {
        setTransferMessage({ type: "error", text: "JSON import is only available in demo mode." });
        return;
      }

      try {
        const importResult = parseKnowledgeImport(await file.text());

        if (!importResult.ok) {
          setTransferMessage({ type: "error", text: importResult.message });
          return;
        }

        window.clearTimeout(saveToStorageTimerRef.current);
        setDocuments(importResult.state.documents);
        documentsRef.current = importResult.state.documents;
        setFolders(knowledgeFolders);
        activeDocumentIdRef.current = importResult.state.activeDocumentId;
        setActiveDocumentId(importResult.state.activeDocumentId);
        setIsContentEmpty(false);
        setContentTextLength(0);
        setOutlineItems([]);
        setOutlineFocusRequest(null);
        setCommentThreadsByDocumentId({});
        setCommentAnchorStatusesByDocumentId({});
        setCommentMatchResultsByDocumentId({});
        setCommentRelocationResultsByDocumentId({});
        setCommentLoadStatesByDocumentId({});
        setActiveCommentThreadId(null);
        setCommentFocusRequest(null);
        setPendingCommentComposer(null);
        setCommentLifecycleActionState(createThreadLifecycleActionState());
        saveKnowledgeEditorState(importResult.state);

        const importedDocument = importResult.state.documents.find(
          (document) => document.id === importResult.state.activeDocumentId,
        );

        resetMockSaved(new Date(importedDocument?.updatedAt ?? Date.now()));
        setTransferMessage({ type: "success", text: "已导入 JSON" });
      } catch {
        setTransferMessage({ type: "error", text: "导入失败，请重试。" });
      }
    },
    [isApiMode, resetMockSaved],
  );

  const applyApiDocument = useCallback((document: KnowledgeDocumentDto) => {
    const mappedDocument = mapDocumentDto(document);
    const nextDocuments = upsertDocument(documentsRef.current, mappedDocument);
    setDocuments(nextDocuments);
    documentsRef.current = nextDocuments;
    activeDocumentIdRef.current = mappedDocument.id;
    setActiveDocumentId(mappedDocument.id);
    setApiSaveStatus("saved");
  }, []);

  const handleRetryLoadDocumentVersions = useCallback(() => {
    setDocumentVersionsReloadKey((currentKey) => currentKey + 1);
  }, []);

  const handleOpenVersions = useCallback(() => {
    const documentId = activeDocumentIdRef.current;
    window.location.hash = documentId ? `#versions?documentId=${encodeURIComponent(documentId)}` : "#versions";
  }, []);

  const handlePublishDocumentVersion = useCallback(async () => {
    const document = documentsRef.current.find((item) => item.id === activeDocumentIdRef.current);
    if (!isApiMode || !document || typeof document.revision !== "number" || documentVersionOperation) {
      return;
    }

    setDocumentVersionOperation("publish");
    setDocumentVersionCompare(null);
    try {
      const response = await publishDocumentVersion(document.id, { baseRevision: document.revision });
      applyApiDocument(response.document);
      setDocumentVersionsReloadKey((currentKey) => currentKey + 1);
      setTransferMessage({ type: "success", text: `Published version ${response.version.label}.` });
    } catch (error) {
      setTransferMessage({
        type: "error",
        text: formatEditorOperationError(error, "Document could not be published."),
      });
    } finally {
      setDocumentVersionOperation(null);
    }
  }, [applyApiDocument, documentVersionOperation, isApiMode]);

  const handleUnpublishDocumentVersion = useCallback(async () => {
    const document = documentsRef.current.find((item) => item.id === activeDocumentIdRef.current);
    if (!isApiMode || !document || typeof document.revision !== "number" || documentVersionOperation) {
      return;
    }

    setDocumentVersionOperation("unpublish");
    setDocumentVersionCompare(null);
    try {
      const response = await unpublishDocumentVersion(document.id, { baseRevision: document.revision });
      applyApiDocument(response.document);
      setDocumentVersionsReloadKey((currentKey) => currentKey + 1);
      setTransferMessage({ type: "success", text: "Document unpublished. Version history was kept." });
    } catch (error) {
      setTransferMessage({
        type: "error",
        text: formatEditorOperationError(error, "Document could not be unpublished."),
      });
    } finally {
      setDocumentVersionOperation(null);
    }
  }, [applyApiDocument, documentVersionOperation, isApiMode]);

  const handleRestoreDocumentVersion = useCallback(
    async (versionId: string) => {
      const document = documentsRef.current.find((item) => item.id === activeDocumentIdRef.current);
      if (!isApiMode || !document || typeof document.revision !== "number" || documentVersionOperation) {
        return;
      }

      setDocumentVersionOperation("restore");
      setDocumentVersionCompare(null);
      try {
        const response = await restoreDocumentVersion(document.id, versionId, { baseRevision: document.revision });
        applyApiDocument(response.document);
        setDocumentVersionsReloadKey((currentKey) => currentKey + 1);
        setTransferMessage({ type: "success", text: `Restored version ${response.restoredFrom.label} to draft.` });
      } catch (error) {
        setTransferMessage({
          type: "error",
          text: formatEditorOperationError(error, "Document version could not be restored."),
        });
      } finally {
        setDocumentVersionOperation(null);
      }
    },
    [applyApiDocument, documentVersionOperation, isApiMode],
  );

  const handleCompareDocumentVersion = useCallback(
    async (versionId: string) => {
      const document = documentsRef.current.find((item) => item.id === activeDocumentIdRef.current);
      if (!isApiMode || !document || documentVersionOperation) {
        return;
      }

      setDocumentVersionOperation("compare");
      try {
        const response = await compareDocumentVersions(document.id, {
          from: { type: "version", versionId },
          to: { type: "draft", versionId: null },
        });
        setDocumentVersionCompare(response);
      } catch (error) {
        setTransferMessage({
          type: "error",
          text: formatEditorOperationError(error, "Document versions could not be compared."),
        });
      } finally {
        setDocumentVersionOperation(null);
      }
    },
    [documentVersionOperation, isApiMode],
  );

  const handleRetryLoadAttachments = useCallback(() => {
    setDocumentAttachmentsReloadKey((currentKey) => currentKey + 1);
  }, []);

  const handleRemoveDocumentAttachment = useCallback(
    async (attachmentId: string) => {
      const documentId = activeDocumentIdRef.current;
      if (!isApiMode || !documentId || documentAttachmentRemovePendingId) {
        return;
      }

      setDocumentAttachmentRemovePendingId(attachmentId);
      try {
        await deleteDocumentAttachment(documentId, attachmentId);
        setDocumentAttachments((currentAttachments) =>
          currentAttachments.filter((attachment) => attachment.id !== attachmentId),
        );
        setTransferMessage({ type: "success", text: "Attachment removed from this document." });
      } catch (error) {
        setTransferMessage({
          type: "error",
          text: formatEditorOperationError(error, "Attachment could not be removed."),
        });
      } finally {
        setDocumentAttachmentRemovePendingId(null);
      }
    },
    [documentAttachmentRemovePendingId, isApiMode],
  );

  const handleUploadDocumentAttachment = useCallback(
    async (files: File[]) => {
      const documentId = activeDocumentIdRef.current;
      if (!isApiMode || !documentId || documentAttachmentUploadStatus === "uploading") {
        return;
      }

      const validation = validateDocumentAttachmentFiles(files);
      if (validation.acceptedFiles.length === 0) {
        setDocumentAttachmentUploadStatus("error");
        setDocumentAttachmentUploadError(validation.message ?? "Choose at least one file before uploading.");
        return;
      }

      setDocumentAttachmentUploadStatus("uploading");
      setDocumentAttachmentUploadError(validation.message);
      try {
        const uploadedAttachments: DocumentAttachmentDto[] = [];
        for (const file of validation.acceptedFiles) {
          uploadedAttachments.push(await uploadDocumentAttachment(documentId, file, { workspaceId }));
        }

        setDocumentAttachments((currentAttachments) => [
          ...uploadedAttachments.filter(
            (attachment) => !currentAttachments.some((currentAttachment) => currentAttachment.id === attachment.id),
          ),
          ...currentAttachments,
        ]);
        setDocumentAttachmentsStatus("ready");
        setTransferMessage({
          type: "success",
          text:
            uploadedAttachments.length === 1
              ? "Attachment uploaded."
              : `${uploadedAttachments.length} attachments uploaded.`,
        });
        setDocumentAttachmentUploadStatus("idle");
      } catch (error) {
        setDocumentAttachmentUploadStatus("error");
        setDocumentAttachmentUploadError(formatEditorOperationError(error, "Attachment could not be uploaded."));
      }
    },
    [documentAttachmentUploadStatus, isApiMode, workspaceId],
  );

  if (isApiMode && editorApiLoadStatus === "loading") {
    return (
      <main className="atlas-shell flex h-screen flex-col overflow-hidden" style={atlasPatternStyle}>
        <div className="grid flex-1 place-items-center bg-[var(--northstar-canvas)] text-sm font-semibold text-[var(--ns-navy-700)]">
          Loading document...
        </div>
      </main>
    );
  }

  if (isApiMode && editorApiLoadStatus === "error") {
    return (
      <main className="atlas-shell flex h-screen flex-col overflow-hidden" style={atlasPatternStyle}>
        <div className="grid flex-1 place-items-center bg-[var(--northstar-canvas)] px-6 text-center text-sm text-[var(--ns-navy-700)]">
          <div className="max-w-md border border-[var(--ns-border)] bg-[rgba(251,248,241,0.9)] p-6 shadow-sm">
            <h1 className="font-serif text-3xl text-[var(--ns-navy-900)]">Document could not be loaded.</h1>
            <p className="mt-3 text-sm leading-6 text-[var(--ns-slate-700)]">
              {editorApiLoadError ?? "Check the backend session and retry."}
            </p>
            <div className="mt-5 flex justify-center gap-3">
              <button
                className="atlas-row-button"
                onClick={() => setEditorApiLoadRetryKey((currentKey) => currentKey + 1)}
                type="button"
              >
                Retry
              </button>
              <a className="atlas-row-button" href={libraryHref}>
                Back to Library
              </a>
            </div>
          </div>
        </div>
      </main>
    );
  }

  if (!activeDocument) {
    return null;
  }

  return (
    <main className="atlas-shell flex h-screen flex-col overflow-hidden" style={atlasPatternStyle}>
      <WorkspaceHomeTopBar
        canImportJson={!isApiMode}
        contextHref={libraryHref}
        contextLabel={activeLibraryName}
        contextTitle="Open Library"
        onExportJson={handleExportJson}
        onImportJsonFile={handleImportJsonFile}
        onSearch={handleSearch}
        saveStatus={effectiveSaveStatus}
        saveStatusLabel={atlasSaveStatusLabel}
        searchPlaceholder={`Search ${workspaceName}`}
        transferMessage={transferMessage}
      />
      <div className="atlas-workspace flex min-h-0 flex-1 overflow-hidden">
        <EditorSidebar
          activeDocumentId={activeDocument.id}
          documents={documents}
          folders={folders}
          libraryHref={libraryHref}
          libraryName={activeLibraryName}
          onCreateDocument={handleCreateDocument}
          onSelectFolder={handleSelectFolder}
          onSelectDocument={handleSelectDocument}
          selectedFolderId={selectedFolderId ?? activeDocument.folderId}
        />
        <section className="flex min-w-0 flex-1 bg-[var(--northstar-canvas)]">
          <EditorCanvas
            activeCommentThreadId={activeCommentThreadId}
            commentFocusRequest={commentFocusRequest}
            commentThreads={activeCommentThreads}
            content={cloneContent(activeDocument.content)}
            documentId={activeDocument.id}
            documentStatusLabel={documentStatusLabel}
            folderHref={folderHref}
            folderTitle={activeDocumentFolder?.title ?? "Folder"}
            isContentEmpty={isContentEmpty}
            isCommentComposerOpen={pendingCommentComposer?.documentId === activeDocument.id}
            isSidePanelCollapsed={isOutlinePanelCollapsed}
            libraryHref={libraryHref}
            libraryName={activeLibraryName}
            onCommentRuntimeStateChange={handleCommentRuntimeStateChange}
            onContentChange={handleContentChange}
            onContentStatsChange={handleContentStatsChange}
            onOpenCommentComposer={handleOpenCommentComposer}
            onOpenShare={() => setIsShareDrawerOpen(true)}
            onOpenVersions={handleOpenVersions}
            onPublishVersion={handlePublishDocumentVersion}
            onSelectCommentThread={handleSelectCommentThread}
            onToggleSidePanel={() => setIsOutlinePanelCollapsed((isCollapsed) => !isCollapsed)}
            onTitleChange={handleTitleChange}
            onUnpublishVersion={handleUnpublishDocumentVersion}
            outlineFocusRequest={outlineFocusRequest}
            saveStatusLabel={atlasSaveStatusLabel}
            shareHref={shareHref}
            tags={activeDocument.tags}
            textLength={contentTextLength}
            title={activeDocument.title}
            updatedAtLabel={effectiveUpdatedAtLabel}
            version={activeDocument.version}
            versionOperation={documentVersionOperation}
          />
          <DocumentShareDrawer
            document={activeDocument}
            isOpen={isShareDrawerOpen}
            libraryId={activeLibraryId}
            onClose={() => setIsShareDrawerOpen(false)}
            workspaceId={workspaceId}
          />
          {isOutlinePanelCollapsed ? null : (
          <OutlinePanel
            activeCommentThreadId={activeCommentThreadId}
            activeDocument={activeDocument}
            activityRows={documentContextPanelModel?.activity}
            backlinksRows={documentContextPanelModel?.backlinks}
            commentLifecycleErrors={commentLifecycleActionState.errorsByThreadId}
            commentLifecyclePending={commentLifecycleActionState.pendingByThreadId}
            commentLoadState={activeCommentLoadState}
            commentMatchResults={activeCommentMatchResults}
            commentRelocationResults={activeCommentRelocationResults}
            commentThreads={activeCommentThreads}
            contextLoadStatus={documentContextStatus}
            documentAttachmentRemovePendingId={documentAttachmentRemovePendingId}
            documentAttachmentUploadError={documentAttachmentUploadError}
            documentAttachmentUploadStatus={documentAttachmentUploadStatus}
            documentAttachments={documentAttachments}
            documentAttachmentsError={documentAttachmentsError}
            documentAttachmentsStatus={documentAttachmentsStatus}
            documentStatusLabel={documentStatusLabel}
            documentVersionCompare={documentVersionCompare}
            documentVersionOperation={documentVersionOperation}
            documentVersionsError={documentVersionsError}
            documentVersionsStatus={documentVersionsStatus}
            folderHref={folderHref}
            folderTitle={activeDocumentFolder?.title ?? "Folder"}
            libraryHref={libraryHref}
            libraryName={activeLibraryName}
            onCompareDocumentVersion={handleCompareDocumentVersion}
            onAddCommentMessage={handleAddCommentMessage}
            onCancelPendingComment={handleCancelPendingCommentComposer}
            onCreateCommentThread={handleCreateCommentThread}
            onCommentThreadClick={handleCommentThreadClick}
            onOpenShare={() => setIsShareDrawerOpen(true)}
            onPendingCommentBodyChange={handlePendingCommentBodyChange}
            onOutlineItemClick={handleOutlineItemClick}
            onPublishDocumentVersion={handlePublishDocumentVersion}
            onReopenCommentThread={handleReopenCommentThread}
            onRestoreDocumentVersion={handleRestoreDocumentVersion}
            onRetryLoadComments={() => loadCommentThreadsForDocument(activeDocument.id)}
            onRetryLoadDocumentAttachments={handleRetryLoadAttachments}
            onRetryLoadDocumentVersions={handleRetryLoadDocumentVersions}
            onRemoveDocumentAttachment={handleRemoveDocumentAttachment}
            onUnpublishDocumentVersion={handleUnpublishDocumentVersion}
            onUploadDocumentAttachment={handleUploadDocumentAttachment}
            onResolveCommentThread={handleResolveCommentThread}
            outlineItems={outlineItems}
            pendingCommentComposer={
              pendingCommentComposer?.documentId === activeDocument.id ? pendingCommentComposer : null
            }
            relatedDocumentRows={documentContextPanelModel?.relatedDocuments}
            saveStatusLabel={atlasSaveStatusLabel}
            shareHref={shareHref}
            versionHistoryHref={versionHistoryHref}
            textLength={contentTextLength}
            updatedAtLabel={effectiveUpdatedAtLabel}
            versionTrailRows={documentVersionTrailRows ?? documentContextPanelModel?.versionTrail}
          />
          )}
        </section>
      </div>
    </main>
  );
}

function mapFolderDto(folder: KnowledgeFolderDto): KnowledgeFolder {
  return {
    id: folder.id,
    title: folder.title,
  };
}

function mapDocumentSummaryDto(document: KnowledgeDocumentSummaryDto): KnowledgeDocument {
  return {
    id: document.id,
    content: createEmptyDocumentContent(),
    folderId: document.folderId,
    sortOrder: document.sortOrder,
    status: document.status,
    tags: [...document.tags],
    title: document.title,
    updatedAt: document.updatedAt,
  };
}

function mapDocumentDto(document: KnowledgeDocumentDto): KnowledgeDocument {
  return {
    id: document.id,
    content: cloneContent(document.content),
    folderId: document.folderId,
    owner: { ...document.owner },
    revision: document.revision,
    sortOrder: document.sortOrder,
    status: document.status,
    tags: [...document.tags],
    title: document.title,
    updatedAt: document.updatedAt,
    version: document.version,
  };
}

function upsertDocument(documents: KnowledgeDocument[], document: KnowledgeDocument) {
  let didReplace = false;
  const nextDocuments = documents.map((currentDocument) => {
    if (currentDocument.id !== document.id) {
      return currentDocument;
    }

    didReplace = true;
    return document;
  });

  return didReplace ? nextDocuments : [document, ...documents];
}

function createDocumentSaveSnapshot(document: KnowledgeDocument) {
  return JSON.stringify({
    content: document.content,
    tags: document.tags ?? [],
    title: document.title,
  });
}

function isConflictError(error: unknown) {
  return error instanceof ApiClientError && (error.status === 409 || error.code === "CONFLICT");
}

function formatDocumentUpdatedAt(value?: string) {
  if (!value) {
    return "Not saved";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Not saved";
  }

  return new Intl.DateTimeFormat("en", {
    day: "numeric",
    hour: "2-digit",
    hour12: false,
    minute: "2-digit",
    month: "short",
  }).format(date);
}

function areAnchorStatusRecordsEqual(
  leftRecord: Record<string, CommentAnchorStatus>,
  rightRecord: Record<string, CommentAnchorStatus>,
) {
  const leftKeys = Object.keys(leftRecord);
  const rightKeys = Object.keys(rightRecord);

  if (leftKeys.length !== rightKeys.length) {
    return false;
  }

  return leftKeys.every((key) => leftRecord[key] === rightRecord[key]);
}

function areMatchResultRecordsEqual(
  leftRecord: Record<string, AnchorMatchResult>,
  rightRecord: Record<string, AnchorMatchResult>,
) {
  const leftKeys = Object.keys(leftRecord);
  const rightKeys = Object.keys(rightRecord);

  if (leftKeys.length !== rightKeys.length) {
    return false;
  }

  return leftKeys.every((key) => {
    const left = leftRecord[key];
    const right = rightRecord[key];

    return (
      right !== undefined &&
      left.status === right.status &&
      left.confidence === right.confidence &&
      left.reason === right.reason &&
      Object.is(left.similarity, right.similarity) &&
      Object.is(left.editDistance, right.editDistance)
    );
  });
}

function areRelocationResultRecordsEqual(
  leftRecord: Record<string, AnchorRelocationResult>,
  rightRecord: Record<string, AnchorRelocationResult>,
) {
  const leftKeys = Object.keys(leftRecord);
  const rightKeys = Object.keys(rightRecord);

  if (leftKeys.length !== rightKeys.length) {
    return false;
  }

  return leftKeys.every((key) => {
    const left = leftRecord[key];
    const right = rightRecord[key];

    return (
      right !== undefined &&
      left.status === right.status &&
      left.confidence === right.confidence &&
      left.reason === right.reason &&
      Object.is(left.similarity, right.similarity) &&
      Object.is(left.editDistance, right.editDistance) &&
      Object.is(left.candidates, right.candidates) &&
      left.range?.from === right.range?.from &&
      left.range?.to === right.range?.to
    );
  });
}
