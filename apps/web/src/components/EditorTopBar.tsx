import { Download, LoaderCircle, PencilLine, Upload } from "lucide-react";
import { useRef } from "react";
import { AtlasIcon } from "./AtlasIcon";
import bellDotIcon from "../assets/svg/icons/bell-dot.svg";
import checkCircleIcon from "../assets/svg/icons/check-circle.svg";
import chevronDownIcon from "../assets/svg/icons/chevron-down.svg";
import compassEmblemIcon from "../assets/svg/brand/compass-emblem.svg";
import searchIcon from "../assets/svg/icons/search.svg";
import type { SaveStatus } from "../hooks/useMockAutoSave";

type EditorTopBarProps = {
  title: string;
  saveStatus: SaveStatus;
  saveStatusLabel: string;
  transferMessage?: {
    type: "success" | "error";
    text: string;
  } | null;
  onExportJson: () => void;
  onImportJsonFile: (file: File) => void;
};

export function EditorTopBar({
  onExportJson,
  onImportJsonFile,
  saveStatus,
  saveStatusLabel,
  title,
  transferMessage,
}: EditorTopBarProps) {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const displayTitle = title.trim() || "Untitled Field Note";
  const isSaving = saveStatus === "saving";
  const isEditing = saveStatus === "editing";

  return (
    <header className="atlas-topbar flex h-[60px] shrink-0 items-center gap-4 px-5 text-[var(--ns-paper)]">
      <div className="flex min-w-0 items-center gap-3">
        <div className="flex items-center gap-2.5 pr-3">
          <span className="grid h-8 w-8 place-items-center rounded-full border border-white/20 text-[#f7f2e8]">
            <AtlasIcon className="h-6 w-6" src={compassEmblemIcon} />
          </span>
          <span className="font-serif text-2xl leading-none tracking-normal text-[#fffaf0]">Northstar</span>
        </div>
        <span className="hidden h-8 w-px bg-white/[0.18] md:block" />
        <button
          className="hidden h-9 items-center gap-2 border-l border-transparent px-3 text-sm font-semibold text-[#f5efe4] transition hover:bg-white/[0.07] md:inline-flex"
          title="Atlas Library"
          type="button"
        >
          Atlas Library
          <AtlasIcon className="h-4 w-4 text-[#b9c7d8]" src={chevronDownIcon} />
        </button>
      </div>

      <div className="ml-auto hidden min-w-[260px] max-w-[420px] flex-1 items-center rounded-full border border-white/[0.18] bg-white/[0.07] px-3 text-[#c7d2df] shadow-[inset_0_1px_0_rgba(255,255,255,0.08)] lg:flex">
        <AtlasIcon className="h-4 w-4" src={searchIcon} />
        <input
          aria-label="Search Northstar"
          className="h-8 min-w-0 flex-1 border-0 bg-transparent px-2 text-sm text-white outline-none placeholder:text-[#c7d2df]"
          placeholder="Search Northstar"
          readOnly
          value=""
        />
        <span className="rounded border border-white/[0.14] bg-white/[0.08] px-1.5 py-0.5 text-[10px] font-semibold text-[#cbd7e4]">
          Cmd K
        </span>
      </div>

      <div className="flex shrink-0 items-center gap-2">
        {transferMessage ? (
          <span
            className={[
              "hidden max-w-[180px] truncate border border-white/[0.16] bg-white/[0.08] px-2.5 py-1 text-xs text-[#dfe8f3] lg:inline",
              transferMessage.type === "error" ? "text-[#ffd1ca]" : "",
            ].join(" ")}
            title={transferMessage.text}
          >
            {transferMessage.text}
          </span>
        ) : null}
        <button
          className="hidden h-9 items-center gap-2 border-l border-white/[0.14] px-3 text-sm font-semibold text-[#f5efe4] transition hover:bg-white/[0.07] md:inline-flex"
          title="Updates"
          type="button"
        >
          <AtlasIcon className="h-4 w-4" src={bellDotIcon} />
          Updates
        </button>
        <div
          className="hidden h-9 items-center gap-2 border-l border-white/[0.14] px-3 text-sm font-semibold text-[#f5efe4] md:inline-flex"
          title={saveStatusLabel}
        >
          {isSaving ? (
            <LoaderCircle className="h-4 w-4 animate-spin text-[#cbd7e4]" />
          ) : isEditing ? (
            <PencilLine className="h-4 w-4 text-[#cbd7e4]" />
          ) : (
            <AtlasIcon className="h-4 w-4 text-[#cbd7e4]" src={checkCircleIcon} />
          )}
          {saveStatus === "saved" ? "Saved" : saveStatusLabel}
        </div>
        <button
          aria-label="Export JSON"
          className="grid h-9 w-9 place-items-center text-[#dfe8f3] transition hover:bg-white/[0.08]"
          onClick={onExportJson}
          title="Export JSON"
          type="button"
        >
          <Download className="h-4 w-4" />
        </button>
        <button
          aria-label="Import JSON"
          className="grid h-9 w-9 place-items-center text-[#dfe8f3] transition hover:bg-white/[0.08]"
          onClick={() => fileInputRef.current?.click()}
          title="Import JSON"
          type="button"
        >
          <Upload className="h-4 w-4" />
        </button>
        <input
          accept="application/json,.json"
          className="hidden"
          onChange={(event) => {
            const file = event.target.files?.[0];

            if (file) {
              onImportJsonFile(file);
            }

            event.target.value = "";
          }}
          ref={fileInputRef}
          type="file"
        />
        <button
          className="flex h-9 items-center gap-2 border-l border-white/[0.14] pl-3 text-sm text-[#f5efe4]"
          title={displayTitle}
          type="button"
        >
          <span className="grid h-8 w-8 place-items-center rounded-full bg-[#efe5d3] text-xs font-semibold text-[var(--ns-navy-900)]">
            NK
          </span>
          <AtlasIcon className="h-4 w-4 text-[#b9c7d8]" src={chevronDownIcon} />
        </button>
      </div>
    </header>
  );
}
