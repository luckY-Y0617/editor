import { ChevronDown, FileText, Folder } from "lucide-react";
import type { KnowledgeDocument, KnowledgeFolder } from "../types/editor";

export function SidebarDocumentTree({
  activeDocumentId,
  documents,
  folders,
  onSelectDocument,
}: {
  activeDocumentId: string;
  documents: KnowledgeDocument[];
  folders: KnowledgeFolder[];
  onSelectDocument: (documentId: string) => void;
}) {
  return (
    <div className="space-y-1">
      {folders.map((folder) => {
        const folderDocuments = documents.filter((document) => document.folderId === folder.id);

        return (
          <div key={folder.id}>
            <div className="flex h-8 items-center gap-1.5 rounded-md px-2 text-sm text-[#c5d2ec]">
              <ChevronDown className="h-3.5 w-3.5 text-[#7f93b8]" />
              <Folder className="h-4 w-4 shrink-0 text-[#8ca1c8]" />
              <span className="truncate text-left">{folder.title}</span>
              <span className="ml-auto text-xs text-[#7f93b8]">{folderDocuments.length}</span>
            </div>
            <div className="space-y-0.5">
              {folderDocuments.map((document) => {
                const active = document.id === activeDocumentId;
                const title = document.title.trim() || "未命名文档";

                return (
                  <button
                    className={[
                      "group flex h-9 w-full items-center gap-1.5 rounded-lg pr-2 text-sm transition",
                      active
                        ? "bg-[rgba(109,140,255,0.18)] text-white shadow-[inset_2px_0_0_var(--northstar-accent),inset_0_0_0_1px_rgba(255,255,255,0.08)]"
                        : "text-[#c1cee7] hover:bg-white/[0.075] hover:text-white",
                    ].join(" ")}
                    key={document.id}
                    onClick={() => onSelectDocument(document.id)}
                    style={{ paddingLeft: 30 }}
                    title={title}
                    type="button"
                  >
                    <FileText
                      className={[
                        "h-4 w-4 shrink-0",
                        active ? "text-[#b8b4ff]" : "text-[#8398bf] group-hover:text-[#aabbe0]",
                      ].join(" ")}
                    />
                    <span className="truncate text-left">{title}</span>
                  </button>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
}
