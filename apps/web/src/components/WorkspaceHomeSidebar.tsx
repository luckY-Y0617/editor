import {
  Activity,
  FileText,
  Home,
  Layers3,
  Library,
  Map,
  Settings,
  Star,
  StarOff,
  SwatchBook,
} from "lucide-react";
import { AtlasIcon } from "./AtlasIcon";
import compassMarkUrl from "../assets/svg/decorative/compass-mark-small.svg";
import type { CollectionSpotlight } from "../data/workspaceHomeData";

type WorkspaceHomeSidebarProps = {
  starredCollections: CollectionSpotlight[];
};

const workspaceNavItems = [
  { label: "Home", icon: Home, active: true, href: "#home" },
  { label: "Library", icon: Library, href: "#editor" },
  { label: "Documents", icon: FileText, href: "#editor" },
  { label: "Collections", icon: Layers3, href: "#home" },
  { label: "Maps", icon: Map, href: "#home" },
  { label: "Templates", icon: SwatchBook, href: "#home", deferred: true },
  { label: "Activity", icon: Activity, href: "#home" },
  { label: "Settings", icon: Settings, href: "#home", deferred: true },
];

export function WorkspaceHomeSidebar({ starredCollections }: WorkspaceHomeSidebarProps) {
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
          Workspace
        </div>

        <nav className="space-y-1" aria-label="Workspace navigation">
          {workspaceNavItems.map((item) => {
            const Icon = item.icon;

            return (
              <a
                aria-current={item.active ? "page" : undefined}
                className={[
                  "workspace-home-nav-item",
                  item.active ? "is-active" : "",
                  item.deferred ? "is-deferred" : "",
                ].join(" ")}
                href={item.href}
                key={item.label}
                title={item.deferred ? `${item.label} is planned for a later phase` : item.label}
              >
                <Icon className="h-4 w-4 shrink-0" />
                <span className="min-w-0 flex-1 truncate">{item.label}</span>
              </a>
            );
          })}
        </nav>

        <section className="mt-8 border-t border-[var(--ns-border)] pt-5">
          <div className="mb-3 text-[11px] font-semibold uppercase tracking-normal text-[var(--ns-slate-700)]">
            Starred Collections
          </div>
          <div className="space-y-1.5">
            {starredCollections.length > 0 ? (
              starredCollections.map((collection) => (
                <a
                  className="workspace-home-starred-row"
                  href="#editor"
                  key={collection.id}
                  title={collection.displayTitle}
                >
                  <Star className="h-3.5 w-3.5 shrink-0 text-[var(--ns-slate-500)]" />
                  <span className="min-w-0 flex-1 truncate">{collection.displayTitle}</span>
                  <span className="shrink-0 tabular-nums text-[var(--ns-slate-500)]">
                    {collection.documentCount}
                  </span>
                </a>
              ))
            ) : (
              <div className="flex items-center gap-2 text-xs leading-5 text-[var(--ns-slate-500)]">
                <StarOff className="h-3.5 w-3.5" />
                No starred collections
              </div>
            )}
          </div>
        </section>

        <div className="mt-auto pt-10 text-center text-[var(--ns-slate-500)]">
          <AtlasIcon className="mx-auto h-20 w-20 opacity-35" src={compassMarkUrl} />
          <div className="mt-4 text-xs">47&deg;36&apos;36&quot;N&nbsp;&nbsp;122&deg;19&apos;48&quot;W</div>
        </div>
      </div>
    </aside>
  );
}
