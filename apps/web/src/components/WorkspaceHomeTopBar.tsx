import { Bell, CheckCircle2, Download, Upload } from "lucide-react";
import { type FormEvent, useEffect, useState } from "react";
import { AtlasIcon } from "./AtlasIcon";
import { getCurrentUser, getSecurityState, logout, type AuthSecurityStateResponse, type MeResponse } from "../lib/authClient";
import { getConfiguredApiBaseUrl, getStoredAccessToken } from "../lib/apiClient";
import { createSearchHash } from "../lib/hashRouting";
import { t, useDisplayLanguage } from "../lib/i18n";
import chevronDownIcon from "../assets/svg/icons/chevron-down.svg";
import compassEmblemIcon from "../assets/svg/brand/compass-emblem.svg";
import searchIcon from "../assets/svg/icons/search.svg";

type WorkspaceHomeTopBarProps = {
  activeItem?: "updates";
  searchValue?: string;
  searchPlaceholder?: string;
  searchHref?: string;
};

export function WorkspaceHomeTopBar({
  activeItem,
  searchHref = "#search",
  searchPlaceholder,
  searchValue = "",
}: WorkspaceHomeTopBarProps) {
  const { locale } = useDisplayLanguage();
  const auth = useTopBarAuthState();
  const [query, setQuery] = useState(searchValue);
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

  useEffect(() => {
    setQuery(searchValue);
  }, [searchValue]);

  const submitSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const trimmedQuery = query.trim();
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

      <a
        className="hidden h-9 items-center gap-2 px-3 text-sm font-semibold text-[#f5efe4] transition hover:bg-white/[0.07] md:inline-flex"
        href="#libraries"
        title={t(locale, "topbar.libraries")}
      >
        {t(locale, "topbar.libraries")}
        <AtlasIcon className="h-4 w-4 text-[#b9c7d8]" src={chevronDownIcon} />
      </a>

      <form
        className="workspace-home-search hidden min-w-[260px] max-w-[460px] items-center rounded-full border border-white/[0.18] bg-white/[0.07] px-3 text-[#c7d2df] shadow-[inset_0_1px_0_rgba(255,255,255,0.08)] lg:flex"
        onSubmit={submitSearch}
        title={query ? `${t(locale, "nav.search")} ${query}` : t(locale, "topbar.searchNorthstar")}
      >
        <button aria-label={t(locale, "topbar.runSearch")} className="grid h-8 w-5 place-items-center border-0 bg-transparent p-0 text-inherit" type="submit">
          <AtlasIcon className="h-4 w-4" src={searchIcon} />
        </button>
        <input
          aria-label={t(locale, "topbar.searchNorthstar")}
          className="h-8 min-w-0 flex-1 border-0 bg-transparent px-2 text-sm text-white outline-none placeholder:text-[#c7d2df]"
          onChange={(event) => setQuery(event.currentTarget.value)}
          placeholder={resolvedSearchPlaceholder}
          type="search"
          value={query}
        />
        <span className="rounded border border-white/[0.14] bg-white/[0.08] px-1.5 py-0.5 text-[10px] font-semibold text-[#cbd7e4]">
          Cmd K
        </span>
      </form>

      <div className="workspace-home-topbar-actions flex shrink-0 items-center gap-2">
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
          title={t(locale, "topbar.saved")}
        >
          <CheckCircle2 className="h-4 w-4 text-[#dfe8f3]" />
          {t(locale, "topbar.saved")}
        </div>
        <button
          aria-label={t(locale, "topbar.exportJson")}
          className="grid h-9 w-9 place-items-center text-[#dfe8f3] transition hover:bg-white/[0.08]"
          title={t(locale, "topbar.exportJson")}
          type="button"
        >
          <Download className="h-4 w-4" />
        </button>
        <button
          aria-label={t(locale, "topbar.importJson")}
          className="grid h-9 w-9 place-items-center text-[#dfe8f3] transition hover:bg-white/[0.08]"
          title={t(locale, "topbar.importJson")}
          type="button"
        >
          <Upload className="h-4 w-4" />
        </button>
        <button
          className="flex h-9 items-center gap-2 border-l border-white/[0.14] pl-3 text-sm text-[#f5efe4]"
          onClick={auth.signOut}
          title={accountTitle}
          type="button"
        >
          <span className="grid h-8 w-8 place-items-center rounded-full bg-[#efe5d3] text-xs font-semibold text-[var(--ns-navy-900)]">
            {initials}
          </span>
          <AtlasIcon className="h-4 w-4 text-[#b9c7d8]" src={chevronDownIcon} />
        </button>
      </div>
    </header>
  );
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
