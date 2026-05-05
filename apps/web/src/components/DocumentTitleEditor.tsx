import { ChangeEvent, useEffect, useRef } from "react";

type DocumentTitleEditorProps = {
  value: string;
  onChange: (value: string) => void;
};

export function DocumentTitleEditor({ value, onChange }: DocumentTitleEditorProps) {
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  // Title is intentionally separate from Tiptap content at this stage.
  useEffect(() => {
    const textarea = textareaRef.current;

    if (!textarea) {
      return;
    }

    textarea.style.height = "0px";
    textarea.style.height = `${textarea.scrollHeight}px`;
  }, [value]);

  const handleChange = (event: ChangeEvent<HTMLTextAreaElement>) => {
    onChange(event.target.value);
  };

  return (
    <textarea
      aria-label="Document title"
      className="atlas-title-editor block w-full resize-none overflow-hidden border-0 bg-transparent p-0 text-[3.2rem] leading-[1.13] tracking-normal text-[var(--ns-navy-950)] outline-none placeholder:text-[var(--ns-stone-300)] focus:placeholder:text-[var(--ns-stone-300)] sm:text-[3.45rem]"
      onBlur={() => {
        const trimmedTitle = value.trim();

        if (trimmedTitle !== value) {
          onChange(trimmedTitle);
        }
      }}
      onChange={handleChange}
      placeholder="Untitled Field Note"
      ref={textareaRef}
      rows={1}
      value={value}
    />
  );
}
