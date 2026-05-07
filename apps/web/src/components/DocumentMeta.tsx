export function DocumentMeta({
  updatedAtLabel,
  version,
}: {
  tags?: string[];
  updatedAtLabel: string;
  version?: string | null;
}) {
  const versionLabel = version?.trim() || "Draft";

  return (
    <div className="atlas-document-meta flex items-center justify-end gap-2 text-sm text-[var(--ns-slate-700)]">
      <span>Updated {updatedAtLabel}</span>
      <span className="text-[var(--ns-stone-300)]" aria-hidden="true">
        {"\u00B7"}
      </span>
      <span>Ver. {versionLabel}</span>
    </div>
  );
}
