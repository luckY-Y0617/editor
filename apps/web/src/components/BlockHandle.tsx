export function BlockHandle() {
  return (
    <div className="pointer-events-auto absolute -left-14 top-1 hidden items-center gap-1 opacity-0 transition group-hover/block:opacity-100 md:flex">
      <button
        className="grid h-7 w-7 place-items-center rounded-md border border-[#dde3da] bg-white text-base leading-none text-[#657066] shadow-sm transition hover:border-[#cfd7cb] hover:bg-[#f5f7f3]"
        aria-label="插入新块"
      >
        +
      </button>
      <button
        className="grid h-7 w-7 place-items-center rounded-md border border-[#dde3da] bg-white text-xs font-semibold tracking-[-0.02em] text-[#7a8379] shadow-sm transition hover:border-[#cfd7cb] hover:bg-[#f5f7f3]"
        aria-label="移动块"
      >
        ::
      </button>
    </div>
  );
}
