import { Check, Trash2, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";

type LinkEditPopoverProps = {
  className?: string;
  initialHref: string;
  showRemove: boolean;
  onCancel: () => void;
  onRemove: () => void;
  onSubmit: (href: string) => void;
};

export function LinkEditPopover({
  className = "",
  initialHref,
  onCancel,
  onRemove,
  onSubmit,
  showRemove,
}: LinkEditPopoverProps) {
  const [href, setHref] = useState(initialHref);
  const inputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    setHref(initialHref);
  }, [initialHref]);

  useEffect(() => {
    const focusTimer = window.setTimeout(() => inputRef.current?.focus(), 0);

    return () => {
      window.clearTimeout(focusTimer);
    };
  }, []);

  return (
    <form
      className={[
        "flex w-max items-center gap-1 rounded-[var(--northstar-radius-panel)] border border-[var(--northstar-border)] bg-white p-1 shadow-[var(--northstar-shadow-popover)]",
        className,
      ].join(" ")}
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(href);
      }}
    >
      <input
        aria-label="链接地址"
        className="h-7 w-44 rounded-[var(--northstar-radius-control)] border border-transparent bg-[var(--northstar-surface-muted)] px-2 text-xs text-[var(--northstar-text-muted)] outline-none transition placeholder:text-[#9aa8bf] focus:border-[var(--northstar-border-strong)] focus:bg-white"
        onChange={(event) => setHref(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            event.preventDefault();
            onCancel();
          }
        }}
        placeholder="https://example.com"
        ref={inputRef}
        value={href}
      />
      <button
        aria-label="保存链接"
        className="grid h-7 w-7 place-items-center rounded-[var(--northstar-radius-control)] text-[var(--northstar-primary)] transition hover:bg-[var(--northstar-primary-soft)]"
        title="保存链接"
        type="submit"
      >
        <Check className="h-3.5 w-3.5" />
      </button>
      {showRemove ? (
        <button
          aria-label="移除链接"
          className="grid h-7 w-7 place-items-center rounded-[var(--northstar-radius-control)] text-[#8a5d54] transition hover:bg-[#f5ece9]"
          onClick={onRemove}
          title="移除链接"
          type="button"
        >
          <Trash2 className="h-3.5 w-3.5" />
        </button>
      ) : null}
      <button
        aria-label="取消"
        className="grid h-7 w-7 place-items-center rounded-[var(--northstar-radius-control)] text-[var(--northstar-text-subtle)] transition hover:bg-[var(--northstar-surface-muted)]"
        onClick={onCancel}
        title="取消"
        type="button"
      >
        <X className="h-3.5 w-3.5" />
      </button>
    </form>
  );
}
