import { AlertCircle, ChevronLeft, ChevronRight, FileText, Folder, LockKeyhole, ShieldCheck } from "lucide-react";
import { FormEvent, type CSSProperties, type ReactNode, useCallback, useEffect, useMemo, useState } from "react";
import { DocumentReaderSurface, ReadonlyDocumentContent } from "./DocumentReaderSurface";
import {
  getPublicShareCollection,
  getPublicShareDocument,
  getPublicShareTree,
  getScopedPublicShareDocument,
  resolvePublicShareLink,
  type PublicShareCollectionResponse,
  type PublicShareDocumentResponse,
  type PublicShareTreeNodeDto,
  type PublicShareTreeResponse,
  type ResolvePublicShareLinkResponse,
} from "../lib/appApi";
import {
  getPublicShareReadEndpoint,
  getContentProtectionLabels,
  derivePublicShareKnowledgeBaseState,
  getPublicShareScopeLabel,
  getPublicShareTreeDepth,
  getPublicShareSafeStatusLabel,
  normalizeContentProtection,
  publicShareUnavailableMessage,
  toPublicShareFailureState,
} from "../lib/publicShareModel";
import type { ShareLinkContentProtectionDto } from "../lib/appApi";

type PublicSharePageProps = {
  token: string | null;
};

type PublicShareLoadState =
  | { status: "failed"; canRetryWithPassword: boolean }
  | { status: "loading" }
  | { response: PublicShareCollectionResponse; status: "collection" }
  | { response: PublicShareDocumentResponse; status: "document" }
  | { document: PublicShareDocumentResponse | null; status: "scope"; tree: PublicShareTreeResponse }
  | { link: ResolvePublicShareLinkResponse; status: "password" };

export function PublicSharePage({ token }: PublicSharePageProps) {
  const [password, setPassword] = useState("");
  const [passwordAttempt, setPasswordAttempt] = useState<string | null>(null);
  const [loadState, setLoadState] = useState<PublicShareLoadState>({ status: "loading" });

  const loadShare = useCallback(
    async (passwordProof: string | null, signal: AbortSignal) => {
      if (!token) {
        setLoadState({ status: "failed", canRetryWithPassword: false });
        return;
      }

      setLoadState({ status: "loading" });

      try {
        const requestOptions = { password: passwordProof, signal };
        const link = await resolvePublicShareLink(token, requestOptions);

        if (link.hasPassword && !passwordProof) {
          setLoadState({ link, status: "password" });
          return;
        }

        const readEndpoint = getPublicShareReadEndpoint(link.resourceType);
        if (readEndpoint === "tree") {
          const tree = await getPublicShareTree(token, requestOptions);
          const firstDocument = tree.nodes.find((node) => node.type === "document");
          const document = firstDocument
            ? await getScopedPublicShareDocument(token, firstDocument.id, requestOptions)
            : null;
          setLoadState({ document, status: "scope", tree });
          return;
        }

        const response = await getPublicShareDocument(token, requestOptions);
        setLoadState({ response, status: "document" });
      } catch {
        const failure = toPublicShareFailureState({
          hasPassword: true,
          passwordSubmitted: Boolean(passwordProof),
        });
        setLoadState({ status: "failed", canRetryWithPassword: failure.canRetryWithPassword });
      }
    },
    [token],
  );

  useEffect(() => {
    const controller = new AbortController();
    void loadShare(passwordAttempt, controller.signal);

    return () => controller.abort();
  }, [loadShare, passwordAttempt]);

  const submitPassword = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPasswordAttempt(password);
  };

  if (loadState.status === "document") {
    return <PublicShareDocumentPage response={loadState.response} />;
  }

  if (loadState.status === "collection") {
    return <PublicShareCollectionPage response={loadState.response} />;
  }

  if (loadState.status === "scope") {
    return (
      <PublicShareScopePage
        initialDocument={loadState.document}
        passwordProof={passwordAttempt}
        token={token}
        tree={loadState.tree}
      />
    );
  }

  const showPasswordForm = loadState.status === "password" || (loadState.status === "failed" && loadState.canRetryWithPassword);
  return (
    <PublicShareShell>
      <section className="public-share-state">
        {showPasswordForm ? (
          <>
            <div className="public-share-state-icon">
              <LockKeyhole aria-hidden="true" />
            </div>
            <h1>Password required</h1>
            <p>{loadState.status === "failed" ? publicShareUnavailableMessage : "Enter the password supplied with this shared link."}</p>
            <form className="public-share-password-form" onSubmit={submitPassword}>
              <label>
                <span>Link password</span>
                <input
                  autoComplete="current-password"
                  autoFocus
                  onChange={(event) => setPassword(event.currentTarget.value)}
                  type="password"
                  value={password}
                />
              </label>
              <button disabled={!password.trim()} type="submit">
                Open link
              </button>
            </form>
          </>
        ) : (
          <>
            <div className="public-share-state-icon is-muted">
              <AlertCircle aria-hidden="true" />
            </div>
            <h1>{loadState.status === "loading" ? "Opening shared content" : publicShareUnavailableMessage}</h1>
            <p>
              {loadState.status === "loading"
                ? "Checking this public share link."
                : "Ask the sender for a fresh link if you still need access."}
            </p>
          </>
        )}
      </section>
    </PublicShareShell>
  );
}

function PublicShareDocumentPage({ response }: { response: PublicShareDocumentResponse }) {
  const { document } = response;
  const protection = normalizeContentProtection(response.link.contentProtection);

  return (
    <PublicShareShell protection={protection} scopeLabel="Document" scopeTitle={document.title}>
      <DocumentReaderSurface
        bodyClassName="public-share-document-body"
        kicker="Public read-only document"
        metadata={
          <PublicShareMetadata
            status={document.status}
            tags={document.tags}
            updatedAt={document.updatedAt}
          />
        }
        title={document.title}
      >
        <div className="public-share-resource-type">
          <FileText aria-hidden="true" />
          <span>Document</span>
        </div>
        <PublicShareProtectedContent protection={protection}>
          <ReadonlyDocumentContent content={document.content} />
        </PublicShareProtectedContent>
      </DocumentReaderSurface>
    </PublicShareShell>
  );
}

function PublicShareCollectionPage({ response }: { response: PublicShareCollectionResponse }) {
  const { collection } = response;
  const protection = normalizeContentProtection(response.link.contentProtection);
  const documents = [...collection.documents].sort((left, right) => left.sortOrder - right.sortOrder || left.title.localeCompare(right.title));

  return (
    <PublicShareShell protection={protection} scopeLabel="Collection" scopeTitle={collection.title}>
      <DocumentReaderSurface
        bodyClassName="public-share-collection-body"
        kicker="Public read-only folder"
        metadata={<p className="public-share-updated atlas-document-meta">Updated {formatPublicShareDate(collection.updatedAt)}</p>}
        title={collection.title}
      >
        <div className="public-share-resource-type">
          <Folder aria-hidden="true" />
          <span>Folder summary</span>
        </div>
        <section className="public-share-collection-list" aria-label="Shared folder documents">
          {documents.length ? (
            documents.map((document) => (
              <div className="public-share-collection-row" key={document.id}>
                <div>
                  <strong>{document.title}</strong>
                  <span>Updated {formatPublicShareDate(document.updatedAt)}</span>
                </div>
                <span className="public-share-status">{getPublicShareSafeStatusLabel(document.status)}</span>
                {document.tags.length ? (
                  <div className="public-share-tags">
                    {document.tags.slice(0, 4).map((tag) => (
                      <span key={tag}>{tag}</span>
                    ))}
                  </div>
                ) : null}
              </div>
            ))
          ) : (
            <p className="public-share-empty">No documents are visible in this shared folder.</p>
          )}
        </section>
      </DocumentReaderSurface>
    </PublicShareShell>
  );
}

function PublicShareScopePage({
  initialDocument,
  passwordProof,
  token,
  tree,
}: {
  initialDocument: PublicShareDocumentResponse | null;
  passwordProof: string | null;
  token: string | null;
  tree: PublicShareTreeResponse;
}) {
  const [selectedDocumentId, setSelectedDocumentId] = useState(initialDocument?.document.id ?? "");
  const [documentResponse, setDocumentResponse] = useState<PublicShareDocumentResponse | null>(initialDocument);
  const [isLoadingDocument, setIsLoadingDocument] = useState(false);
  const [treeOpen, setTreeOpen] = useState(true);
  const [collapsedCollections, setCollapsedCollections] = useState<ReadonlySet<string>>(() => new Set());
  const model = useMemo(
    () => derivePublicShareKnowledgeBaseState(tree, selectedDocumentId || initialDocument?.document.id),
    [initialDocument?.document.id, selectedDocumentId, tree],
  );
  const protection = normalizeContentProtection(tree.contentProtection ?? initialDocument?.link.contentProtection);

  const openDocument = async (documentId: string) => {
    if (!token || documentId === selectedDocumentId) {
      return;
    }

    setSelectedDocumentId(documentId);
    setIsLoadingDocument(true);
    try {
      setDocumentResponse(await getScopedPublicShareDocument(token, documentId, { password: passwordProof }));
    } catch {
      setDocumentResponse(null);
    } finally {
      setIsLoadingDocument(false);
    }
  };

  const visibleNodes = model.orderedNodes.filter((node) => {
    if (!node.parentId) {
      return true;
    }

    const parents = model.breadcrumb.map((item) => item.id);
    if (parents.includes(node.id)) {
      return true;
    }

    let parentId: string | null = node.parentId;
    while (parentId) {
      if (collapsedCollections.has(parentId)) {
        return false;
      }
      const parent = model.orderedNodes.find((item) => item.id === parentId);
      parentId = parent?.parentId ?? null;
    }

    return true;
  });

  const toggleCollection = (collectionId: string) => {
    setCollapsedCollections((current) => {
      const next = new Set(current);
      if (next.has(collectionId)) {
        next.delete(collectionId);
      } else {
        next.add(collectionId);
      }
      return next;
    });
  };

  return (
    <PublicShareShell protection={protection} scopeLabel={model.scopeLabel} scopeTitle={model.scopeTitle}>
      <div className="public-share-scope-layout">
        <aside className={`public-share-tree${treeOpen ? " is-open" : ""}`} aria-label="Shared public directory">
          <button className="public-share-tree-heading" onClick={() => setTreeOpen((current) => !current)} type="button">
            <Folder aria-hidden="true" />
            <div>
              <strong>{model.scopeTitle}</strong>
              <span>{model.scopeLabel} public knowledge base</span>
            </div>
          </button>
          <div className="public-share-tree-list">
            {visibleNodes.length ? (
              visibleNodes.map((node) =>
                node.type === "document" ? (
                  <button
                    className={node.id === selectedDocumentId ? "is-active" : ""}
                    key={node.id}
                    onClick={() => void openDocument(node.id)}
                    style={{ "--public-share-tree-depth": getPublicShareTreeDepth(model.orderedNodes, node) } as CSSProperties}
                    type="button"
                  >
                    <FileText aria-hidden="true" />
                    <span>{node.title}</span>
                  </button>
                ) : (
                  <button
                    className="public-share-tree-folder"
                    key={node.id}
                    onClick={() => toggleCollection(node.id)}
                    style={{ "--public-share-tree-depth": getPublicShareTreeDepth(model.orderedNodes, node) } as CSSProperties}
                    type="button"
                  >
                    <Folder aria-hidden="true" />
                    <span>{node.title}</span>
                    <span className="public-share-tree-toggle">{collapsedCollections.has(node.id) ? "Show" : "Hide"}</span>
                  </button>
                ),
              )
            ) : (
              <p>{model.emptyState === "empty-scope" ? "This public knowledge base is empty." : "No readable documents are visible."}</p>
            )}
          </div>
        </aside>
        {documentResponse ? (
          <PublicShareDocumentArticle
            breadcrumb={model.breadcrumb}
            nextDocument={model.nextDocument}
            onNavigate={(documentId) => void openDocument(documentId)}
            previousDocument={model.previousDocument}
            response={documentResponse}
            scopeLabel={model.scopeLabel}
            scopeProtection={protection}
          />
        ) : (
          <section className="public-share-empty public-share-scope-empty">
            {isLoadingDocument
              ? "Opening document..."
              : model.documentOrder.length
                ? publicShareUnavailableMessage
                : model.emptyState === "empty-scope"
                  ? "This public knowledge base is empty."
                  : "No readable documents are visible in this shared scope."}
          </section>
        )}
      </div>
    </PublicShareShell>
  );
}

function PublicShareDocumentArticle({
  breadcrumb = [],
  nextDocument,
  onNavigate,
  previousDocument,
  response,
  scopeLabel,
  scopeProtection,
}: {
  breadcrumb?: PublicShareTreeNodeDto[];
  nextDocument?: PublicShareTreeNodeDto | null;
  onNavigate?: (documentId: string) => void;
  previousDocument?: PublicShareTreeNodeDto | null;
  response: PublicShareDocumentResponse;
  scopeLabel?: string;
  scopeProtection?: ShareLinkContentProtectionDto;
}) {
  const { document } = response;
  const protection = normalizeContentProtection(response.link.contentProtection ?? scopeProtection);
  const label = scopeLabel ?? getPublicShareScopeLabel(response.link.resourceType);

  return (
    <DocumentReaderSurface
      bodyClassName="public-share-document-body"
      className="public-share-scope-document"
      kicker={`Public read-only ${label.toLowerCase()}`}
      metadata={
        <PublicShareMetadata
          status={document.status}
          tags={document.tags}
          updatedAt={document.updatedAt}
        />
      }
      title={document.title}
    >
      {breadcrumb.length ? (
        <nav className="public-share-breadcrumb" aria-label="Public breadcrumb">
          {breadcrumb.map((item, index) => (
            <span key={`${item.id}:${index}`}>{item.title}</span>
          ))}
        </nav>
      ) : null}
      <div className="public-share-resource-type">
        <FileText aria-hidden="true" />
        <span>Document</span>
      </div>
      <PublicShareProtectedContent protection={protection}>
        <ReadonlyDocumentContent content={document.content} />
      </PublicShareProtectedContent>
      {onNavigate ? (
        <nav className="public-share-prev-next" aria-label="Previous and next public documents">
          <button disabled={!previousDocument} onClick={() => previousDocument && onNavigate(previousDocument.id)} type="button">
            <ChevronLeft aria-hidden="true" />
            <span>{previousDocument?.title ?? "No previous document"}</span>
          </button>
          <button disabled={!nextDocument} onClick={() => nextDocument && onNavigate(nextDocument.id)} type="button">
            <span>{nextDocument?.title ?? "No next document"}</span>
            <ChevronRight aria-hidden="true" />
          </button>
        </nav>
      ) : null}
    </DocumentReaderSurface>
  );
}

function PublicShareProtectedContent({ children, protection }: { children: ReactNode; protection: ShareLinkContentProtectionDto }) {
  return (
    <div
      className={protection.disableCopy ? "public-share-copy-limited" : undefined}
      onCopy={protection.disableCopy ? (event) => event.preventDefault() : undefined}
    >
      {children}
    </div>
  );
}

function PublicShareWatermark({ protection }: { protection: ShareLinkContentProtectionDto }) {
  if (!protection.watermarkEnabled) {
    return null;
  }

  return <div aria-hidden="true" className="public-share-watermark">{protection.watermarkText}</div>;
}

function PublicShareShell({
  children,
  protection,
  scopeLabel = "Document",
  scopeTitle = "Shared content",
}: {
  children: ReactNode;
  protection?: ShareLinkContentProtectionDto;
  scopeLabel?: string;
  scopeTitle?: string;
}) {
  const normalizedProtection = normalizeContentProtection(protection);
  return (
    <main className={[
      "public-share-page",
      normalizedProtection.disablePrint ? "is-print-limited" : "",
    ].filter(Boolean).join(" ")}>
      <PublicShareWatermark protection={normalizedProtection} />
      <header className="public-share-topbar">
        <div>
          <strong title={scopeTitle}>{scopeTitle}</strong>
          <span>{scopeLabel} public read-only share</span>
        </div>
        <span className="public-share-boundary">
          <ShieldCheck aria-hidden="true" />
          View only
        </span>
      </header>
      {getContentProtectionLabels(normalizedProtection).length ? (
        <div className="public-share-protection-strip" aria-label="Content protection policy">
          {getContentProtectionLabels(normalizedProtection).map((label) => (
            <span key={label}>{label}</span>
          ))}
        </div>
      ) : null}
      <div className="public-share-scroll editor-scrollbar">{children}</div>
    </main>
  );
}

function PublicShareMetadata({ status, tags, updatedAt }: { status: string; tags: string[]; updatedAt: string }) {
  return (
    <div className="public-share-meta atlas-document-meta">
      <span className="public-share-status">{getPublicShareSafeStatusLabel(status)}</span>
      <span>Updated {formatPublicShareDate(updatedAt)}</span>
      {tags.map((tag) => (
        <span key={tag}>{tag}</span>
      ))}
    </div>
  );
}

function formatPublicShareDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "not available";
  }

  return new Intl.DateTimeFormat(undefined, {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(date);
}
