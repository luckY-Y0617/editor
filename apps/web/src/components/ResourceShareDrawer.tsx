import { Copy, Link2, Loader2, ShieldCheck, X } from "lucide-react";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  copyShareLinkManagementUrl,
  createResourceShareLink,
  getResourceShareLinks,
  type CreateShareLinkResponse,
  type ShareLinkContentProtectionDto,
  type ShareLinkDto,
} from "../lib/appApi";
import { ApiClientError, getConfiguredApiBaseUrl } from "../lib/apiClient";
import {
  getExistingShareLinkCopyCapability,
  getPublicSharePasswordHint,
  getPublicSharePolicyWarnings,
  getShareLinkGovernanceHint,
  getShareLinkScopeLabel,
  toSharePermissionMutationError,
} from "../lib/documentShareLinksModel";
import {
  createPublicResourceShareLinkRequest,
  getContainerPublicSharePolicy,
  getPublicResourceShareDisabledReason,
  getResourceShareTitle,
  type ResourceShareType,
} from "../lib/resourceShareLinksModel";
import { getContentProtectionLabels, toUserFacingShareUrl } from "../lib/publicShareModel";

export type ResourceShareTarget = {
  resourceId: string;
  resourceTitle: string;
  resourceType: ResourceShareType;
};

type ResourceShareDrawerProps = {
  isOpen: boolean;
  onClose: () => void;
  target: ResourceShareTarget | null;
};

type ResourceShareStatus = "error" | "forbidden" | "idle" | "loading" | "ready" | "unconfigured";

const defaultContainerContentProtection: ShareLinkContentProtectionDto = {
  disableDownload: true,
  disablePrint: false,
  disableCopy: false,
  watermarkEnabled: false,
  watermarkText: "Public link",
};

export function ResourceShareDrawer({ isOpen, onClose, target }: ResourceShareDrawerProps) {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [status, setStatus] = useState<ResourceShareStatus>("idle");
  const [operation, setOperation] = useState<string | null>(null);
  const [links, setLinks] = useState<ShareLinkDto[]>([]);
  const [createdLink, setCreatedLink] = useState<CreateShareLinkResponse | null>(null);
  const [expiresAt, setExpiresAt] = useState(defaultExpiryDateTimeLocal);
  const [passwordEnabled, setPasswordEnabled] = useState(true);
  const [publicPassword, setPublicPassword] = useState("");
  const [contentProtection, setContentProtection] = useState<ShareLinkContentProtectionDto>(defaultContainerContentProtection);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const policy = target ? getContainerPublicSharePolicy(target.resourceType) : null;
  const expiresAtIso = toApiDateTime(expiresAt);
  const passwordHint = target && policy ? getPublicSharePasswordHint(target.resourceType, policy) : null;
  const effectivePasswordEnabled = passwordEnabled || Boolean(passwordHint);
  const disabledReason = target && policy
    ? getPublicResourceShareDisabledReason(
        {
          contentProtection,
          expiresAt: expiresAtIso,
          password: publicPassword,
          passwordEnabled: effectivePasswordEnabled,
          resourceId: target.resourceId,
          resourceType: target.resourceType,
        },
        policy,
      ) ?? getBaseDisabledReason({ apiConfigured: Boolean(apiBaseUrl), operation, status })
    : "No share target selected.";
  const canCreate = !disabledReason;
  const appOrigin = typeof window !== "undefined" ? window.location.origin : "";
  const generatedUrl = createdLink
    ? toUserFacingShareUrl(createdLink.url, createdLink.token, createdLink.link.audience, apiBaseUrl, appOrigin)
    : "";

  useEffect(() => {
    if (!isOpen || !target) {
      return;
    }

    setCreatedLink(null);
    setError(null);
    setMessage(null);

    if (!apiBaseUrl) {
      setLinks([]);
      setStatus("unconfigured");
      return;
    }

    const controller = new AbortController();
    setStatus("loading");
    void getResourceShareLinks(target.resourceType, target.resourceId, controller.signal)
      .then((response) => {
        setLinks(response.links);
        setStatus("ready");
      })
      .catch((value: unknown) => {
        if (value instanceof DOMException && value.name === "AbortError") {
          return;
        }

        setLinks([]);
        setStatus(isForbiddenError(value) ? "forbidden" : "error");
        setError(toDrawerError(value, "Unable to load share links for this resource."));
      });

    return () => controller.abort();
  }, [apiBaseUrl, isOpen, target?.resourceId, target?.resourceType]);

  if (!isOpen || !target) {
    return null;
  }

  const reloadLinks = async () => {
    const response = await getResourceShareLinks(target.resourceType, target.resourceId);
    setLinks(response.links);
    setStatus("ready");
  };

  const runOperation = async (key: string, callback: () => Promise<void>) => {
    if (operation) {
      return;
    }

    setOperation(key);
    setMessage(null);
    setError(null);
    try {
      await callback();
    } catch (value) {
      setError(toDrawerError(value, "Share-link operation failed."));
    } finally {
      setOperation(null);
    }
  };

  const createPublicLink = () => {
    void runOperation("create", async () => {
      if (disabledReason) {
        setError(disabledReason);
        return;
      }

      const { request, resourceId, resourceType } = createPublicResourceShareLinkRequest({
        contentProtection,
        expiresAt: expiresAtIso,
        password: effectivePasswordEnabled ? publicPassword : null,
        passwordEnabled: effectivePasswordEnabled,
        resourceId: target.resourceId,
        resourceType: target.resourceType,
      });
      const created = await createResourceShareLink(resourceType, resourceId, request);
      setCreatedLink(created);
      setPublicPassword("");
      setMessage("Public viewer link created. The reader URL is shown once.");
      await reloadLinks();
    });
  };

  const copyGeneratedLink = () => {
    void copyValue(generatedUrl, setMessage, setError);
  };

  const copyExistingLink = (link: ShareLinkDto) => {
    const disabled = getExistingShareLinkCopyCapability({
      apiConfigured: Boolean(apiBaseUrl),
      canManage: status === "ready" && !operation,
      copyEndpointAvailable: true,
      operation,
      status: link.revokedAt ? "revoked" : link.expiresAt && Date.parse(link.expiresAt) <= Date.now() ? "expired" : "active",
    });

    void runOperation(`copy:${link.id}`, async () => {
      if (disabled) {
        setError(disabled);
        return;
      }

      const response = await copyShareLinkManagementUrl(link.id, {
        copiedValueType: "share_url",
        reason: `Audited copy from ${target.resourceType} share drawer.`,
      });
      await copyValue(toUserFacingShareUrl(response.url, null, link.audience, apiBaseUrl, appOrigin), setMessage, setError);
      setMessage(response.reissued ? "Audited copy complete. A new URL was issued by the backend." : "Audited copy complete.");
    });
  };

  return (
    <div className="document-share-drawer-overlay" role="presentation">
      <aside aria-label={getResourceShareTitle(target.resourceType, target.resourceTitle)} className="document-share-drawer" role="dialog">
        <header className="document-share-drawer-header">
          <div className="min-w-0">
            <h2>{getResourceShareTitle(target.resourceType, target.resourceTitle)}</h2>
            <p>{target.resourceType === "library" ? "Library public reader scope" : "Folder public reader scope"}</p>
          </div>
          <button aria-label="Close resource share drawer" onClick={onClose} title="Close" type="button">
            <X className="h-5 w-5" />
          </button>
        </header>

        <div className="document-share-drawer-body editor-scrollbar">
          {status === "loading" ? <DrawerStatus icon={<Loader2 className="h-4 w-4 animate-spin" />} label="Loading resource share links..." /> : null}
          {status === "unconfigured" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label="Share API is not configured." /> : null}
          {status === "forbidden" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label="You do not have permission to publish this resource." /> : null}
          {status === "error" ? <DrawerStatus icon={<ShieldCheck className="h-4 w-4" />} label={error ?? "Share settings failed to load."} /> : null}

          <section className="document-share-section">
            <h3>Public viewer link</h3>
            <p className="document-share-help">
              Creates a read-only public link for this {target.resourceType === "library" ? "Library" : "Folder"}. Workspace public sharing is unsupported.
            </p>
            <div className="document-share-select-row">
              <span>Role</span>
              <input disabled value="Viewer" readOnly />
            </div>
            <div className="document-share-select-row">
              <span>Expiry</span>
              <input
                disabled={status !== "ready" || Boolean(operation)}
                onChange={(event) => setExpiresAt(event.currentTarget.value)}
                type="datetime-local"
                value={expiresAt}
              />
            </div>
            <label className="document-share-password-control">
              <span>Password</span>
              <button
                className={effectivePasswordEnabled ? "is-on" : ""}
                disabled={Boolean(passwordHint) || status !== "ready" || Boolean(operation)}
                onClick={() => setPasswordEnabled((current) => !current)}
                type="button"
              >
                {effectivePasswordEnabled ? "On" : "Off"}
              </button>
              <input
                disabled={!effectivePasswordEnabled || status !== "ready" || Boolean(operation)}
                onChange={(event) => setPublicPassword(event.currentTarget.value)}
                placeholder={passwordHint ? "Required by policy" : "Optional access password"}
                type="password"
                value={publicPassword}
              />
            </label>
            {passwordHint ? <p className="document-share-help is-warning">{passwordHint}</p> : null}
            <div className="document-share-protection-options">
              <label>
                <input
                  checked={contentProtection.disableDownload}
                  onChange={(event) => setContentProtection((current) => ({ ...current, disableDownload: event.currentTarget.checked }))}
                  type="checkbox"
                />
                Disable download
              </label>
              <label>
                <input
                  checked={contentProtection.disablePrint}
                  onChange={(event) => setContentProtection((current) => ({ ...current, disablePrint: event.currentTarget.checked }))}
                  type="checkbox"
                />
                Disable print
              </label>
              <label>
                <input
                  checked={contentProtection.disableCopy}
                  onChange={(event) => setContentProtection((current) => ({ ...current, disableCopy: event.currentTarget.checked }))}
                  type="checkbox"
                />
                Limit copy
              </label>
              <label>
                <input
                  checked={contentProtection.watermarkEnabled}
                  onChange={(event) => setContentProtection((current) => ({ ...current, watermarkEnabled: event.currentTarget.checked }))}
                  type="checkbox"
                />
                Watermark
              </label>
            </div>
            <button className="document-share-link-action" disabled={!canCreate} onClick={createPublicLink} title={disabledReason ?? "Create public viewer link"} type="button">
              <Link2 className="h-4 w-4" />
              {operation === "create" ? "Creating" : target.resourceType === "library" ? "Create Library public link" : "Create Folder public link"}
            </button>
            {disabledReason ? <p className="document-share-help is-warning">{disabledReason}</p> : null}
            {createdLink ? (
              <GeneratedSecret label="Public reader URL" onCopy={copyGeneratedLink} value={generatedUrl} />
            ) : null}
          </section>

          <section className="document-share-section">
            <h3>Existing links</h3>
            <p className="document-share-help">
              Existing URLs are copied through the audited backend copy endpoint. Access & Sharing remains the governance center for pause, resume, revoke, risk, stats, and audit.
            </p>
            <div className="document-share-access-list">
              {links.length ? (
                links.map((link) => (
                  <ExistingResourceLink key={link.id} link={link} onCopy={() => copyExistingLink(link)} />
                ))
              ) : (
                <p className="document-share-access-empty">No links returned for this resource.</p>
              )}
            </div>
          </section>
        </div>

        <footer className="document-share-drawer-footer">
          <a href="#access-sharing" title="Open Access & Sharing">
            Access & Sharing
          </a>
          <span className={error ? "is-error" : ""}>{error ?? message}</span>
          <button onClick={onClose} type="button">
            Cancel
          </button>
          <button className="document-share-primary" onClick={onClose} type="button">
            Done
          </button>
        </footer>
      </aside>
    </div>
  );
}

function ExistingResourceLink({ link, onCopy }: { link: ShareLinkDto; onCopy: () => void }) {
  const warnings = getPublicSharePolicyWarnings(link, getContainerPublicSharePolicy(link.resourceType === "library" ? "library" : "collection"));
  const metadata = [
    getShareLinkGovernanceHint(link),
    ...warnings,
    ...getContentProtectionLabels(link.contentProtection).slice(0, 2),
    link.expiresAt ? `expires ${new Date(link.expiresAt).toLocaleDateString()}` : "no expiry",
  ].filter(Boolean).join(" / ");

  return (
    <div className="document-share-access-row">
      <span className="document-share-avatar is-blue">{link.resourceType === "library" ? "LB" : "FO"}</span>
      <span className="min-w-0 flex-1">
        <strong title={link.id}>{getShareLinkScopeLabel(link)} {link.audience} link</strong>
      </span>
      <span className="document-share-access-role">{metadata}</span>
      <span className="document-share-row-actions">
        <button onClick={onCopy} title="Audited copy through backend endpoint" type="button">
          <Copy className="h-4 w-4" />
        </button>
      </span>
    </div>
  );
}

function DrawerStatus({ icon, label }: { icon: ReactNode; label: string }) {
  return (
    <div className="document-share-status">
      {icon}
      {label}
    </div>
  );
}

function GeneratedSecret({ label, onCopy, value }: { label: string; onCopy: () => void; value: string }) {
  return (
    <div className="document-share-generated">
      <span>{label}: {value}</span>
      <button onClick={onCopy} type="button">
        <Copy className="h-4 w-4" />
        Copy
      </button>
    </div>
  );
}

function getBaseDisabledReason(options: { apiConfigured: boolean; operation: string | null; status: ResourceShareStatus }) {
  if (!options.apiConfigured) {
    return "Share API is not configured.";
  }

  if (options.status === "loading" || options.status === "idle") {
    return "Share links are still loading.";
  }

  if (options.status === "unconfigured") {
    return "Share APIs are not connected.";
  }

  if (options.status === "forbidden") {
    return "You do not have permission to publish this resource.";
  }

  if (options.status === "error") {
    return "Share APIs are unavailable. Retry after the backend session is healthy.";
  }

  if (options.operation) {
    return "Another share operation is in progress.";
  }

  return null;
}

function defaultExpiryDateTimeLocal() {
  const date = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);
  date.setSeconds(0, 0);
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

function toApiDateTime(value: string) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function copyValue(value: string, setMessage: (message: string | null) => void, setError: (message: string | null) => void) {
  if (!value) {
    setError("Nothing to copy.");
    return Promise.resolve();
  }

  if (!navigator.clipboard) {
    setError("Clipboard access is unavailable. Copy the value manually.");
    return Promise.resolve();
  }

  return navigator.clipboard
    .writeText(value)
    .then(() => {
      setError(null);
      setMessage("Copied.");
    })
    .catch(() => setError("Clipboard access was blocked. Copy the value manually."));
}

function isForbiddenError(value: unknown) {
  return value instanceof ApiClientError && (value.status === 401 || value.status === 403);
}

function toDrawerError(value: unknown, fallback: string) {
  return toSharePermissionMutationError(value, fallback);
}
