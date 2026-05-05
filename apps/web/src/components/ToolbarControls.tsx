import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";

type ToolbarGroupProps = {
  children: ReactNode;
  label?: string;
  contextual?: boolean;
};

type ToolbarButtonProps = {
  label: string;
  icon: LucideIcon;
  active?: boolean;
  disabled?: boolean;
  onClick: () => void;
  shortLabel?: string;
  tone?: "default" | "danger";
  variant?: "icon" | "compact";
};

export function ToolbarGroup({ children, contextual, label }: ToolbarGroupProps) {
  return (
    <div
      aria-label={label}
      className={[
        "inline-flex items-center gap-0.5 shrink-0",
        contextual
          ? "border border-[var(--ns-border)] bg-[rgba(255,255,255,0.48)] px-1 py-0.5"
          : "",
      ].join(" ")}
      role={label ? "group" : undefined}
    >
      {label ? <span className="pl-1 pr-0.5 text-[11px] font-medium text-[var(--ns-slate-500)]">{label}</span> : null}
      {children}
    </div>
  );
}

export function ToolbarButton({
  active,
  disabled,
  icon: Icon,
  label,
  onClick,
  shortLabel,
  tone = "default",
  variant = "icon",
}: ToolbarButtonProps) {
  const isCompact = variant === "compact";
  const dangerClass =
    tone === "danger" && !disabled
      ? "text-[#8f5b57] hover:bg-[#fff1f0] hover:text-[#a34843]"
      : "";

  return (
    <button
      aria-label={label}
      aria-pressed={active ?? undefined}
      className={[
        "inline-flex h-7 items-center justify-center text-[var(--ns-slate-700)] transition duration-150",
        isCompact ? "gap-1 px-2 text-[12px] font-medium leading-none" : "w-6",
        active ? "text-[var(--ns-blue-600)] shadow-[inset_0_-2px_0_var(--ns-blue-600)]" : "",
        disabled
          ? "cursor-not-allowed text-[var(--ns-stone-300)]"
          : dangerClass || "hover:bg-[rgba(15,92,156,0.08)] hover:text-[var(--ns-blue-600)]",
      ].join(" ")}
      disabled={disabled}
      onClick={() => {
        if (!disabled) {
          onClick();
        }
      }}
      onMouseDown={(event) => event.preventDefault()}
      title={label}
      type="button"
    >
      <Icon className={isCompact ? "h-3.5 w-3.5" : "h-4 w-4"} />
      {isCompact ? <span>{shortLabel ?? label}</span> : null}
    </button>
  );
}

export function ToolbarSeparator() {
  return <span className="mx-1.5 h-5 w-px shrink-0 bg-[rgba(216,208,195,0.78)]" />;
}
