import { AlertCircle, FileText, Folder, LockKeyhole, ShieldCheck } from "lucide-react";
import { FormEvent, type ReactNode, useCallback, useEffect, useState } from "react";
import { DocumentReaderSurface, ReadonlyDocumentContent } from "./DocumentReaderSurface";
import {
  getPublicShareCollection,
  getPublicShareDocument,
  resolvePublicShareLink,
  type PublicShareCollectionResponse,
  type PublicShareDocumentResponse,
  type ResolvePublicShareLinkResponse,
} from "../lib/appApi";
import {
  getPublicShareReadEndpoint,
  getPublicShareSafeStatusLabel,
  publicShareUnavailableMessage,
  toPublicShareFailureState,
} from "../lib/publicShareModel";

type PublicSharePageProps = {
  token: string | null;
};

type PublicShareLoadState =
  | { status: "failed"; canRetryWithPassword: boolean }
  | { status: "loading" }
  | { response: PublicShareCollectionResponse; status: "collection" }
  | { response: PublicShareDocumentResponse; status: "document" }
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
        const response =
          readEndpoint === "collection"
            ? await getPublicShareCollection(token, requestOptions)
            : await getPublicShareDocument(token, requestOptions);

        setLoadState(
          link.resourceType === "collection"
            ? { response: response as PublicShareCollectionResponse, status: "collection" }
            : { response: response as PublicShareDocumentResponse, status: "document" },
        );
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

  return (
    <PublicShareShell>
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
        <ReadonlyDocumentContent content={document.content} />
      </DocumentReaderSurface>
    </PublicShareShell>
  );
}

function PublicShareCollectionPage({ response }: { response: PublicShareCollectionResponse }) {
  const { collection } = response;
  const documents = [...collection.documents].sort((left, right) => left.sortOrder - right.sortOrder || left.title.localeCompare(right.title));

  return (
    <PublicShareShell>
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

function PublicShareShell({ children }: { children: ReactNode }) {
  return (
    <main className="public-share-page">
      <header className="public-share-topbar">
        <div>
          <strong>Northstar Atlas Library</strong>
          <span>Public read-only share</span>
        </div>
        <span className="public-share-boundary">
          <ShieldCheck aria-hidden="true" />
          View only
        </span>
      </header>
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
