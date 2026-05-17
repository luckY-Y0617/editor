import {
  Home,
  Layers3,
  Library,
  MoreHorizontal,
  Settings,
  Share2,
  UsersRound,
} from "lucide-react";
import { AtlasIcon } from "./AtlasIcon";
import { t, useDisplayLanguage } from "../lib/i18n";
import { createWorkspaceNavItems, type WorkspaceNavItemId } from "../lib/workspaceShellModel";
import compassMarkUrl from "../assets/svg/decorative/compass-mark-small.svg";

type WorkspaceHomeSidebarProps = {
  activeItem?: WorkspaceNavItemId | null;
  collectionsTitle?: string;
  currentLibraryCollections?: WorkspaceSidebarCollection[];
  onShareCollection?: (collection: WorkspaceSidebarCollection) => void;
  showCollections?: boolean;
};

type WorkspaceSidebarCollection = {
  displayTitle: string;
  documentCount: number;
  href: string;
  id: string;
  isActive?: boolean;
};

const workspaceNavIconById = {
  groups: Layers3,
  home: Home,
  libraries: Library,
  members: UsersRound,
  settings: Settings,
  sharing: Share2,
  updates: Share2,
} satisfies Record<WorkspaceNavItemId, typeof Home>;

export function WorkspaceHomeSidebar({
  activeItem = "home",
  collectionsTitle = "Current Library Folders",
  currentLibraryCollections = [],
  onShareCollection,
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
          {createWorkspaceNavItems().map((item) => {
            const Icon = workspaceNavIconById[item.id];
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
                  <div
                    className={[
                      "workspace-home-starred-row",
                      collection.isActive ? "is-active" : "",
                      onShareCollection ? "has-action" : "",
                    ].join(" ")}
                    key={collection.id}
                    title={collection.displayTitle}
                  >
                    <a href={collection.href}>
                      <Layers3 className="h-3.5 w-3.5 shrink-0 text-[var(--ns-slate-500)]" />
                      <span className="min-w-0 flex-1 truncate">{collection.displayTitle}</span>
                    </a>
                    <span className="shrink-0 tabular-nums text-[var(--ns-slate-500)]">
                      {collection.documentCount}
                    </span>
                    {onShareCollection ? (
                      <button
                        aria-label={`Share folder ${collection.displayTitle}`}
                        className="workspace-home-starred-action"
                        onClick={() => onShareCollection(collection)}
                        title={`Share folder ${collection.displayTitle}`}
                        type="button"
                      >
                        <MoreHorizontal className="h-3.5 w-3.5" />
                      </button>
                    ) : null}
                  </div>
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
