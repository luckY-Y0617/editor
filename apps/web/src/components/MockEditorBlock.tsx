import { Check } from "lucide-react";
import { type MockEditorBlock as MockEditorBlockType } from "../data/editorMockData";
import { BlockHandle } from "./BlockHandle";

export function MockEditorBlock({ block }: { block: MockEditorBlockType }) {
  return (
    <section className="group/block relative py-1.5">
      <BlockHandle />
      {renderBlock(block)}
    </section>
  );
}

function renderBlock(block: MockEditorBlockType) {
  switch (block.type) {
    case "heading": {
      const classes =
        block.level === 2
          ? "mt-8 text-2xl font-semibold leading-tight text-[#262a27]"
          : "mt-5 text-xl font-semibold leading-snug text-[#303531]";

      return <h2 className={classes}>{block.text}</h2>;
    }
    case "paragraph":
      return <p className="text-[16px] leading-8 text-[#383d39]">{block.text}</p>;
    case "quote":
      return (
        <blockquote className="my-3 border-l-2 border-[#8ab3a2] bg-[#f5f8f3] py-3 pl-4 pr-5 text-[16px] leading-8 text-[#465046]">
          {block.text}
        </blockquote>
      );
    case "taskList":
      return (
        <div className="my-2 space-y-2">
          {block.items.map((item) => (
            <label className="flex items-start gap-3 text-[15px] leading-7 text-[#3f4740]" key={item.text}>
              <span
                className={[
                  "mt-1 grid h-5 w-5 shrink-0 place-items-center rounded border",
                  item.checked
                    ? "border-[#3f7f6d] bg-[#3f7f6d] text-white"
                    : "border-[#c9d2c7] bg-white text-transparent",
                ].join(" ")}
              >
                <Check className="h-3.5 w-3.5" />
              </span>
              <span className={item.checked ? "text-[#7b857c] line-through decoration-[#b8c0b6]" : ""}>
                {item.text}
              </span>
            </label>
          ))}
        </div>
      );
    case "list":
      return (
        <ul className="my-2 list-disc space-y-1.5 pl-6 text-[16px] leading-7 text-[#3d443e] marker:text-[#8da095]">
          {block.items.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      );
    case "code":
      return (
        <figure className="my-4 overflow-hidden rounded-lg border border-[#dfe5dc] bg-[#fbfcf8] shadow-[0_1px_0_rgba(31,41,55,0.03)]">
          <figcaption className="flex h-9 items-center justify-between border-b border-[#e5e9e2] px-3 text-xs text-[#818a80]">
            <span>{block.language}</span>
            <span>mock code block</span>
          </figcaption>
          <pre className="overflow-x-auto p-4 text-[13px] leading-6 text-[#31413b]">
            <code>{block.code}</code>
          </pre>
        </figure>
      );
  }
}
