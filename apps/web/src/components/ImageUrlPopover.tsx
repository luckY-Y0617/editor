import { Check, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";

type ImageUrlPopoverProps = {
  className?: string;
  onCancel: () => void;
  onSubmit: (src: string) => void;
};

export function ImageUrlPopover({ className = "", onCancel, onSubmit }: ImageUrlPopoverProps) {
  const [src, setSrc] = useState("");
  const inputRef = useRef<HTMLInputElement | null>(null);

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
        onSubmit(src);
      }}
    >
      <input
        aria-label="图片 URL"
        className="h-7 w-52 rounded-[var(--northstar-radius-control)] border border-transparent bg-[var(--northstar-surface-muted)] px-2 text-xs text-[var(--northstar-text-muted)] outline-none transition placeholder:text-[#9aa8bf] focus:border-[var(--northstar-border-strong)] focus:bg-white"
        onChange={(event) => setSrc(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            event.preventDefault();
            onCancel();
          }
        }}
        placeholder="https://example.com/image.png"
        ref={inputRef}
        value={src}
      />
      <button
        aria-label="插入图片"
        className="grid h-7 w-7 place-items-center rounded-[var(--northstar-radius-control)] text-[var(--northstar-primary)] transition hover:bg-[var(--northstar-primary-soft)]"
        title="插入图片"
        type="submit"
      >
        <Check className="h-3.5 w-3.5" />
      </button>
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
