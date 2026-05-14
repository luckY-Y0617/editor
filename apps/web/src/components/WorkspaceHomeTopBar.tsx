import { Bell, CheckCircle2, Download, LoaderCircle, PencilLine, Upload } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { AtlasIcon } from "./AtlasIcon";
import type { SaveStatus } from "../hooks/useMockAutoSave";
import { getCurrentUser, getSecurityState, logout, type AuthSecurityStateResponse, type MeResponse } from "../lib/authClient";
import { getConfiguredApiBaseUrl, getConfiguredWorkspaceId, getStoredAccessToken } from "../lib/apiClient";
import { createOrganizationSettingsHash, createPersonalSettingsHash, createSearchHash } from "../lib/hashRouting";
import { t, useDisplayLanguage } from "../lib/i18n";
import { toWorkspaceSwitcherModel } from "../lib/workspaceShellModel";
import chevronDownIcon from "../assets/svg/icons/chevron-down.svg";
import compassEmblemIcon from "../assets/svg/brand/compass-emblem.svg";
import searchIcon from "../assets/svg/icons/search.svg";

type WorkspaceHomeTopBarProps = {
  activeItem?: "updates";
  canExportJson?: boolean;
  canImportJson?: boolean;
  contextHref?: string;
  contextLabel?: string;
  contextTitle?: string;
  onExportJson?: () => void;
  onImportJsonFile?: (file: File) => void;
  onSearch?: (query: string) => void;
  saveStatus?: SaveStatus;
  saveStatusLabel?: string;
  searchValue?: string;
  searchPlaceholder?: string;
  searchHref?: string;
  transferMessage?: { type: "success" | "error"; text: string } | null;
};

export function WorkspaceHomeTopBar({
  activeItem,
  canExportJson,
  canImportJson,
  contextHref,
  contextLabel,
  contextTitle,
  onExportJson,
  onImportJsonFile,
  onSearch,
  saveStatus = "saved",
  saveStatusLabel,
  searchHref = "#search",
  searchPlaceholder,
  searchValue = "",
  transferMessage,
}: WorkspaceHomeTopBarProps) {
  const { locale } = useDisplayLanguage();
  const auth = useTopBarAuthState();
  const [isWorkspaceMenuOpen, setIsWorkspaceMenuOpen] = useState(false);
  const [isAccountMenuOpen, setIsAccountMenuOpen] = useState(false);
  const importInputRef = useRef<HTMLInputElement | null>(null);
  const workspaceSwitcher = toWorkspaceSwitcherModel(
    auth.me?.workspaces ?? [],
    getConfiguredWorkspaceId(),
    t(locale, "common.workspace"),
  );
  const resolvedSearchPlaceholder = searchPlaceholder ?? t(locale, "topbar.searchNorthstar");
  const initials = auth.me?.user.displayName
    ? getInitials(auth.me.user.displayName)
    : auth.me?.user.email
      ? getInitials(auth.me.user.email)
      : "NK";
  const accountTitle = auth.me
    ? `${auth.me.user.displayName} - ${auth.securityState?.mfaEnabled ? t(locale, "topbar.mfaEnabled") : t(locale, "topbar.mfaNotEnabled")}`
    : auth.status === "unconfigured"
      ? t(locale, "topbar.apiNotConfigured")
      : auth.status === "unauthenticated"
        ? t(locale, "topbar.signInRequired")
        : t(locale, "topbar.account");
  const canRunExport = Boolean(onExportJson && canExportJson !== false);
  const canRunImport = Boolean(onImportJsonFile && canImportJson !== false);
  const ResolvedSaveIcon =
    saveStatus === "saving" ? LoaderCircle : saveStatus === "editing" || saveStatus === "created" ? PencilLine : CheckCircle2;
  const resolvedSaveStatusLabel = saveStatusLabel ?? t(locale, "topbar.saved");
  const shortcutLabel = getShortcutLabel();

  const openSearch = () => {
    const trimmedQuery = searchValue.trim();
    if (onSearch) {
      onSearch(trimmedQuery);
      return;
    }

    window.location.hash = trimmedQuery ? createSearchHash({ q: trimmedQuery }) : searchHref;
  };

  return (
    <header className="workspace-home-topbar flex h-[60px] shrink-0 items-center gap-4 px-5 text-[var(--ns-paper)]">
      <a className="flex min-w-0 items-center gap-3" href="#home" title={t(locale, "topbar.workspaceHome")}>
        <span className="grid h-8 w-8 place-items-center rounded-full border border-white/20 text-[#f7f2e8]">
          <AtlasIcon className="h-6 w-6" src={compassEmblemIcon} />
        </span>
        <span className="font-serif text-2xl leading-none tracking-normal text-[#fffaf0]">Northstar</span>
      </a>

      <span className="hidden h-8 w-px bg-white/[0.18] md:block" />

      {contextLabel ? (
        <a
          className="workspace-home-context hidden min-w-0 items-center gap-2 px-3 text-sm font-bold text-[#f5efe4] transition hover:bg-white/[0.07] md:inline-flex"
          href={contextHref ?? "#home"}
          title={contextTitle ?? contextLabel}
        >
          <span className="min-w-0 truncate">{contextLabel}</span>
          <AtlasIcon className="h-4 w-4 text-[#b9c7d8]" src={chevronDownIcon} />
        </a>
      ) : (
      <div className="workspace-home-switcher hidden md:block">
        <button
          aria-expanded={isWorkspaceMenuOpen}
          className="workspace-home-switcher-button"
          onClick={() => {
            setIsWorkspaceMenuOpen((current) => !current);
            setIsAccountMenuOpen(false);
          }}
          title={t(locale, "topbar.workspaceSwitcher")}
          type="button"
        >
          <span>{workspaceSwitcher.currentName}</span>
          <AtlasIcon className="h-4 w-4 text-[#b9c7d8]" src={chevronDownIcon} />
        </button>
        {isWorkspaceMenuOpen ? (
          <div className="workspace-home-dropdown workspace-home-workspace-dropdown" role="menu">
            <div className="workspace-home-dropdown-heading">
              <span>{t(locale, "topbar.workspaceSwitcher")}</span>
              <strong>{workspaceSwitcher.currentName}</strong>
            </div>
            {workspaceSwitcher.rows.length > 0 ? (
              <div className="workspace-home-dropdown-list">
                {workspaceSwitcher.rows.map((workspace) => (
                  <a
                    aria-disabled={workspace.disabled ? "true" : undefined}
                    aria-current={workspace.isCurrent ? "page" : undefined}
                    className={workspace.isCurrent ? "is-current" : workspace.disabled ? "is-disabled" : ""}
                    href={workspace.disabled ? "#" : workspace.href}
                    key={workspace.id}
                    onClick={(event) => {
                      if (workspace.disabled) {
                        event.preventDefault();
                        return;
                      }

                      setIsWorkspaceMenuOpen(false);
                    }}
                    role="menuitem"
                    title={workspace.disabled ? t(locale, "topbar.workspaceSwitchingDeferred") : workspace.name}
                  >
                    <span>
                      <strong>{workspace.name}</strong>
                      <small>{workspace.role}</small>
                    </span>
                    <em>{workspace.isCurrent ? t(locale, "topbar.currentWorkspace") : t(locale, "common.deferred")}</em>
                  </a>
                ))}
              </div>
            ) : (
              <p>{t(locale, "topbar.workspaceListUnavailable")}</p>
            )}
          <p>{t(locale, "topbar.workspaceSwitchingDeferred")}</p>
        </div>
      ) : null}
      </div>
      )}

      <button
        aria-keyshortcuts="Control+K Meta+K"
        className="workspace-home-search workspace-home-search-trigger hidden min-w-[260px] max-w-[460px] items-center rounded-full border border-white/[0.18] bg-white/[0.07] px-3 text-[#c7d2df] shadow-[inset_0_1px_0_rgba(255,255,255,0.08)] lg:flex"
        onClick={openSearch}
        title={searchValue ? `${t(locale, "nav.search")} ${searchValue}` : t(locale, "topbar.searchNorthstar")}
        type="button"
      >
        <span className="grid h-8 w-5 place-items-center text-inherit">
          <AtlasIcon className="h-4 w-4" src={searchIcon} />
        </span>
        <span className="workspace-home-search-label min-w-0 flex-1 truncate px-2 text-left text-sm">
          {searchValue || resolvedSearchPlaceholder}
        </span>
        <kbd>{shortcutLabel}</kbd>
      </button>

      <div className="workspace-home-topbar-actions flex shrink-0 items-center gap-2">
        {transferMessage ? (
          <span
            className={[
              "workspace-home-transfer hidden max-w-[220px] truncate rounded-full border px-2.5 py-1 text-xs font-semibold md:inline-block",
              transferMessage.type === "error"
                ? "border-[#e5b8ad]/45 bg-[#7b2f2a]/20 text-[#ffd8d1]"
                : "border-[#b9d6c9]/45 bg-[#1c5c48]/20 text-[#d7f1e3]",
            ].join(" ")}
            title={transferMessage.text}
          >
            {transferMessage.text}
          </span>
        ) : null}
        <a
          className={[
            "workspace-home-topbar-link hidden h-9 items-center gap-2 border-l border-white/[0.14] px-3 text-sm font-semibold text-[#f5efe4] transition hover:bg-white/[0.07] md:inline-flex",
            activeItem === "updates" ? "is-active" : "",
          ].join(" ")}
          href="#updates"
          title={t(locale, "nav.updates")}
        >
          <Bell className="h-4 w-4 text-[#dfe8f3]" />
          {t(locale, "nav.updates")}
        </a>
        <div
          className="hidden h-9 items-center gap-2 border-l border-white/[0.14] px-3 text-sm font-semibold text-[#f5efe4] md:inline-flex"
          title={resolvedSaveStatusLabel}
        >
          <ResolvedSaveIcon className={["h-4 w-4 text-[#dfe8f3]", saveStatus === "saving" ? "animate-spin" : ""].join(" ")} />
          {resolvedSaveStatusLabel}
        </div>
        <button
          aria-label={t(locale, "topbar.exportJson")}
          className="grid h-9 w-9 place-items-center text-[#dfe8f3] transition hover:bg-white/[0.08] disabled:cursor-not-allowed disabled:opacity-45 disabled:hover:bg-transparent"
          disabled={!canRunExport}
          onClick={onExportJson}
          title={canRunExport ? t(locale, "topbar.exportJson") : "Open a document in the editor to export JSON."}
          type="button"
        >
          <Download className="h-4 w-4" />
        </button>
        <button
          aria-label={t(locale, "topbar.importJson")}
          className="grid h-9 w-9 place-items-center text-[#dfe8f3] transition hover:bg-white/[0.08] disabled:cursor-not-allowed disabled:opacity-45 disabled:hover:bg-transparent"
          disabled={!canRunImport}
          onClick={() => importInputRef.current?.click()}
          title={canRunImport ? t(locale, "topbar.importJson") : "Open a document in the editor to import JSON."}
          type="button"
        >
          <Upload className="h-4 w-4" />
        </button>
        {onImportJsonFile ? (
          <input
            ref={importInputRef}
            accept="application/json"
            className="hidden"
            onChange={(event) => {
              const file = event.currentTarget.files?.[0];

              if (file) {
                onImportJsonFile(file);
              }

              event.currentTarget.value = "";
            }}
            type="file"
          />
        ) : null}
        <div className="workspace-home-account-wrapper">
          <button
            aria-expanded={isAccountMenuOpen}
            className="workspace-home-account-button"
            onClick={() => {
              setIsAccountMenuOpen((current) => !current);
              setIsWorkspaceMenuOpen(false);
            }}
            title={accountTitle}
            type="button"
          >
            <span className="grid h-8 w-8 place-items-center rounded-full bg-[#efe5d3] text-xs font-semibold text-[var(--ns-navy-900)]">
              {initials}
            </span>
            <AtlasIcon className="h-4 w-4 text-[#b9c7d8]" src={chevronDownIcon} />
          </button>
          {isAccountMenuOpen ? (
            <div className="workspace-home-dropdown workspace-home-account-dropdown" role="menu">
              <div className="workspace-home-dropdown-heading">
                <span>{t(locale, "topbar.account")}</span>
                <strong>{auth.me?.user.displayName ?? auth.me?.user.email ?? accountTitle}</strong>
              </div>
              <a href={createPersonalSettingsHash()} onClick={() => setIsAccountMenuOpen(false)} role="menuitem">
                {t(locale, "topbar.personalSettings")}
              </a>
              <a href={createOrganizationSettingsHash()} onClick={() => setIsAccountMenuOpen(false)} role="menuitem">
                {t(locale, "topbar.organizationSettings")}
              </a>
              <button onClick={auth.signOut} type="button">
                {t(locale, "topbar.signOut")}
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </header>
  );
}

function getShortcutLabel() {
  return /mac|iphone|ipad|ipod/i.test(window.navigator.platform) ? "⌘K" : "Ctrl K";
}

function useTopBarAuthState() {
  const [me, setMe] = useState<MeResponse | null>(null);
  const [securityState, setSecurityState] = useState<AuthSecurityStateResponse | null>(null);
  const [status, setStatus] = useState<"unconfigured" | "unauthenticated" | "loading" | "ready" | "error">(() => {
    if (!getConfiguredApiBaseUrl()) {
      return "unconfigured";
    }

    return getStoredAccessToken() ? "loading" : "unauthenticated";
  });

  useEffect(() => {
    if (!getConfiguredApiBaseUrl()) {
      setStatus("unconfigured");
      return;
    }

    if (!getStoredAccessToken()) {
      setMe(null);
      setSecurityState(null);
      setStatus("unauthenticated");
      return;
    }

    let cancelled = false;
    setStatus("loading");
    void Promise.all([getCurrentUser(), getSecurityState()])
      .then(([currentUser, currentSecurityState]) => {
        if (cancelled) {
          return;
        }

        setMe(currentUser);
        setSecurityState(currentSecurityState);
        setStatus("ready");
      })
      .catch(() => {
        if (cancelled) {
          return;
        }

        setMe(null);
        setSecurityState(null);
        setStatus("error");
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const signOut = () => {
    if (!getStoredAccessToken()) {
      window.location.hash = "";
      return;
    }

    void logout().finally(() => {
      setMe(null);
      setSecurityState(null);
      setStatus("unauthenticated");
      window.location.hash = "";
    });
  };

  return { me, securityState, signOut, status };
}

function getInitials(value: string) {
  return value
    .split(/[\s@._-]+/)
    .filter(Boolean)
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}
