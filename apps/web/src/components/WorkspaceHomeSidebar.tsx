import {
  Bell,
  Home,
  Layers3,
  Library,
  Search,
  Settings,
  UsersRound,
} from "lucide-react";
import { AtlasIcon } from "./AtlasIcon";
import { t, useDisplayLanguage } from "../lib/i18n";
import compassMarkUrl from "../assets/svg/decorative/compass-mark-small.svg";

type WorkspaceHomeSidebarProps = {
  activeItem?: WorkspaceNavItemId | null;
  collectionsTitle?: string;
  currentLibraryCollections?: WorkspaceSidebarCollection[];
  showCollections?: boolean;
};

type WorkspaceNavItemId = "home" | "libraries" | "search" | "settings" | "updates" | "members";

type WorkspaceSidebarCollection = {
  displayTitle: string;
  documentCount: number;
  href: string;
  id: string;
};

const workspaceNavItems = [
  { id: "home" as const, labelKey: "nav.home" as const, icon: Home, href: "#home" },
  { id: "libraries" as const, labelKey: "nav.libraries" as const, icon: Library, href: "#libraries" },
  { id: "search" as const, labelKey: "nav.search" as const, icon: Search, href: "#search" },
  { id: "updates" as const, labelKey: "nav.updates" as const, icon: Bell, href: "#updates" },
  { id: "members" as const, labelKey: "nav.members" as const, icon: UsersRound, href: "#workspace-members" },
  { id: "settings" as const, labelKey: "nav.settings" as const, icon: Settings, href: "#settings" },
];

export function WorkspaceHomeSidebar({
  activeItem = "home",
  collectionsTitle = "Current Library Folders",
  currentLibraryCollections = [],
  showCollections = true,
}: WorkspaceHomeSidebarProps) {
  const { locale } = useDisplayLanguage();
  const resolvedCollectionsTitle = collectionsTitle === "Current Library Folders"
    ? t(locale, "nav.currentLibraryCollections")
    : collectionsTitle;

  return (
    <aside className="workspace-home-sidebar hidden h-full w-[320px] shrink-0 overflow-hidden border-r border-[var(--ns-border)] md:flex md:flex-col">
      <div className="workspace-home-ruler" aria-hidden="true">
        <span>N 90</span>
        <span>N 60</span>
        <span>N 30</span>
        <span>0</span>
        <span>S 30</span>
        <span>S 60</span>
        <span>S 90</span>
      </div>

      <div className="workspace-home-sidebar-content editor-scrollbar relative z-10 flex min-h-0 flex-1 flex-col overflow-y-auto px-6 py-6 pl-[72px]">
        <div className="mb-5 text-[11px] font-semibold uppercase tracking-normal text-[var(--ns-slate-700)]">
          {t(locale, "nav.workspace")}
        </div>

        <nav className="space-y-1" aria-label="Workspace navigation">
          {workspaceNavItems.map((item) => {
            const Icon = item.icon;
            const label = t(locale, item.labelKey);

            return (
              <a
                aria-current={activeItem === item.id ? "page" : undefined}
                className={[
                  "workspace-home-nav-item",
                  activeItem === item.id ? "is-active" : "",
                ].join(" ")}
                href={item.href}
                key={item.id}
                title={label}
              >
                <Icon className="h-4 w-4 shrink-0" />
                <span className="min-w-0 flex-1 truncate">{label}</span>
              </a>
            );
          })}
        </nav>

        {showCollections ? (
          <section className="mt-8 border-t border-[var(--ns-border)] pt-5">
            <div className="mb-3 text-[11px] font-semibold uppercase tracking-normal text-[var(--ns-slate-700)]">
              {resolvedCollectionsTitle}
            </div>
            <div className="space-y-1.5">
              {currentLibraryCollections.length > 0 ? (
                currentLibraryCollections.map((collection) => (
                  <a
                    className="workspace-home-starred-row"
                    href={collection.href}
                    key={collection.id}
                    title={collection.displayTitle}
                  >
                    <Layers3 className="h-3.5 w-3.5 shrink-0 text-[var(--ns-slate-500)]" />
                    <span className="min-w-0 flex-1 truncate">{collection.displayTitle}</span>
                    <span className="shrink-0 tabular-nums text-[var(--ns-slate-500)]">
                      {collection.documentCount}
                    </span>
                  </a>
                ))
              ) : (
                <div className="text-xs leading-5 text-[var(--ns-slate-500)]">{t(locale, "nav.noCurrentLibraryCollections")}</div>
              )}
            </div>
          </section>
        ) : null}

        <div className="mt-auto pt-10 text-center text-[var(--ns-slate-500)]">
          <AtlasIcon className="mx-auto h-20 w-20 opacity-35" src={compassMarkUrl} />
          <div className="mt-4 text-xs">47&deg;36&apos;36&quot;N&nbsp;&nbsp;122&deg;19&apos;48&quot;W</div>
        </div>
      </div>
    </aside>
  );
}
