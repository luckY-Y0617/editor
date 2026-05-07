import { Plus } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { AtlasIcon } from "./AtlasIcon";
import chevronDownIcon from "../assets/svg/icons/chevron-down.svg";
import chevronRightIcon from "../assets/svg/icons/chevron-right.svg";
import filterSlidersIcon from "../assets/svg/icons/filter-sliders.svg";
import mapIcon from "../assets/svg/icons/map.svg";
import type { KnowledgeDocument, KnowledgeFolder } from "../types/editor";

type EditorSidebarProps = {
  activeDocumentId: string;
  documents: KnowledgeDocument[];
  folders: KnowledgeFolder[];
  libraryHref: string;
  libraryName: string;
  onCreateDocument: () => void;
  onSelectDocument: (documentId: string) => void;
};

export function EditorSidebar({
  activeDocumentId,
  documents,
  folders,
  libraryHref,
  libraryName,
  onCreateDocument,
  onSelectDocument,
}: EditorSidebarProps) {
  const activeFolderId = useMemo(
    () => documents.find((document) => document.id === activeDocumentId)?.folderId,
    [activeDocumentId, documents],
  );
  const [expandedFolderIds, setExpandedFolderIds] = useState<Set<string>>(() =>
    new Set(activeFolderId ? [activeFolderId] : []),
  );

  useEffect(() => {
    if (!activeFolderId) {
      return;
    }

    setExpandedFolderIds((currentFolderIds) => {
      if (currentFolderIds.has(activeFolderId)) {
        return currentFolderIds;
      }

      const nextFolderIds = new Set(currentFolderIds);
      nextFolderIds.add(activeFolderId);
      return nextFolderIds;
    });
  }, [activeFolderId]);

  const handleToggleFolder = (folderId: string, hasDocuments: boolean) => {
    if (!hasDocuments) {
      return;
    }

    setExpandedFolderIds((currentFolderIds) => {
      const nextFolderIds = new Set(currentFolderIds);

      if (nextFolderIds.has(folderId)) {
        nextFolderIds.delete(folderId);
      } else {
        nextFolderIds.add(folderId);
      }

      return nextFolderIds;
    });
  };

  return (
    <aside className="atlas-sidebar hidden h-full w-[396px] shrink-0 overflow-hidden border-r border-[var(--ns-border)] md:flex md:flex-col">
      <div className="atlas-coordinate-ruler" aria-hidden="true">
        <span>N 90</span>
        <span>N 60</span>
        <span>N 30</span>
        <span>0</span>
        <span>S 30</span>
        <span>S 60</span>
      </div>

      <div className="atlas-sidebar-content editor-scrollbar relative z-10 flex min-h-0 flex-1 flex-col overflow-y-auto px-6 py-5 pl-[72px]">
        <div className="mb-5 flex items-center justify-between">
          <div className="text-[11px] font-semibold uppercase tracking-normal text-[var(--ns-slate-700)]">
            Knowledge Map
          </div>
          <div className="flex items-center gap-1.5 text-[var(--ns-navy-800)]">
            <a className="atlas-icon-button" href={libraryHref} title="Library map">
              <AtlasIcon className="h-4 w-4" src={mapIcon} />
            </a>
            <a className="atlas-icon-button" href="#search" title="Search filters">
              <AtlasIcon className="h-4 w-4" src={filterSlidersIcon} />
            </a>
          </div>
        </div>

        <div className="mb-4 flex items-center gap-2 px-1 text-xs font-semibold uppercase tracking-normal text-[var(--ns-navy-800)]">
          <AtlasIcon className="h-4 w-4" src={chevronDownIcon} />
          <a className="min-w-0 flex-1 truncate hover:text-[var(--ns-blue-600)]" href={libraryHref}>
            {libraryName}
          </a>
          <button
            aria-label="Create document"
            className="ml-auto atlas-icon-button"
            onClick={onCreateDocument}
            title="Create document"
            type="button"
          >
            <Plus className="h-3.5 w-3.5" />
          </button>
        </div>

        <nav className="atlas-route-tree flex-1" aria-label="Atlas documents">
          {folders.map((folder) => {
            const folderDocuments = documents.filter((document) => document.folderId === folder.id);
            const isActiveFolder = folder.id === activeFolderId;
            const folderCount = folderDocuments.length;
            const folderNumber = getFolderNumber(folder.title);
            const hasDocuments = folderDocuments.length > 0;
            const expanded = hasDocuments && expandedFolderIds.has(folder.id);

            return (
              <div className="atlas-route-group" key={folder.id}>
                <button
                  aria-disabled={!hasDocuments || undefined}
                  aria-expanded={hasDocuments ? expanded : undefined}
                  className={["atlas-route-folder", isActiveFolder ? "is-active" : ""].join(" ")}
                  onClick={() => handleToggleFolder(folder.id, hasDocuments)}
                  title={hasDocuments ? `${expanded ? "Collapse" : "Expand"} ${folder.title}` : folder.title}
                  type="button"
                >
                  <span className="atlas-route-joint">
                    <span className="atlas-route-node" />
                  </span>
                  {hasDocuments ? (
                    <AtlasIcon
                      className="atlas-route-chevron h-3.5 w-3.5"
                      src={expanded ? chevronDownIcon : chevronRightIcon}
                    />
                  ) : (
                    <span className="h-3.5 w-3.5 shrink-0" />
                  )}
                  <AtlasIcon className="h-4 w-4" src={mapIcon} />
                  <span className="min-w-0 flex-1 truncate">{folder.title}</span>
                  <span className={["atlas-count-badge", isActiveFolder ? "is-emphasized" : ""].join(" ")}>
                    {folderCount}
                  </span>
                </button>

                {expanded ? (
                  <div className="atlas-route-documents">
                    {folderDocuments.map((document, documentIndex) => {
                      const active = document.id === activeDocumentId;
                      const title = document.title.trim() || "Untitled Field Note";

                      return (
                        <button
                          className={["atlas-route-document", active ? "is-active" : ""].join(" ")}
                          key={document.id}
                          onClick={() => onSelectDocument(document.id)}
                          title={title}
                          type="button"
                        >
                          <span className="atlas-route-joint">
                            <span className="atlas-route-node" />
                          </span>
                          <span className="w-9 shrink-0 text-right tabular-nums text-[var(--ns-slate-500)]">
                            {folderNumber}.{documentIndex + 1}
                          </span>
                          <span className="min-w-0 flex-1 truncate">{title}</span>
                        </button>
                      );
                    })}
                  </div>
                ) : null}
              </div>
            );
          })}
        </nav>

        <div className="atlas-map-legend mt-5 border-t border-[var(--ns-border)] pt-5">
          <div className="mb-3 text-[11px] font-semibold uppercase tracking-normal text-[var(--ns-slate-700)]">
            Map Legend
          </div>
          <div className="grid grid-cols-2 gap-x-5 gap-y-2 text-xs text-[var(--ns-slate-700)]">
            <span className="atlas-legend-item">
              <i className="atlas-legend-dot" />
              Folder
            </span>
            <span className="atlas-legend-item">
              <i className="atlas-legend-line" />
              Primary Route
            </span>
            <span className="atlas-legend-item">
              <i className="atlas-legend-dot is-filled" />
              Document
            </span>
            <span className="atlas-legend-item">
              <i className="atlas-legend-line is-dashed" />
              Secondary Route
            </span>
            <span className="atlas-legend-item">
              <i className="atlas-legend-dot is-active" />
              In Progress
            </span>
            <span className="atlas-legend-item">
              <i className="atlas-legend-line is-cross" />
              Cross Reference
            </span>
          </div>
          <div className="mt-7 text-[11px] font-semibold tracking-normal text-[var(--ns-slate-500)]">
            W 122 24'  /  N 37 46'
          </div>
          <div className="atlas-scale mt-2" aria-hidden="true">
            <div className="atlas-scale-line">
              <span />
              <span />
              <span />
              <span />
            </div>
            <div className="atlas-scale-labels">
              <span>0</span>
              <span>25</span>
              <span>50</span>
              <span>75km</span>
            </div>
          </div>
        </div>
      </div>
    </aside>
  );
}

function getFolderNumber(title: string) {
  const match = title.match(/^0?(\d+)/);

  return match ? Number(match[1]) : 0;
}
