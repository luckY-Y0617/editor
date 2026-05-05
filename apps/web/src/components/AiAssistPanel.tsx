import { aiActions } from "../data/editorMockData";

export function AiAssistPanel() {
  return (
    <section className="space-y-2">
      {aiActions.map((action) => (
        <button
          className="group w-full rounded-[var(--northstar-radius-panel)] border border-[var(--northstar-border-soft)] bg-white px-3.5 py-3 text-left shadow-[var(--northstar-shadow-card)] transition hover:border-[var(--northstar-border-strong)] hover:bg-[var(--northstar-accent-tint)]"
          key={action.label}
        >
          <div className="flex items-start gap-3">
            <span className="mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[var(--northstar-accent-tint)] text-[var(--northstar-primary)] group-hover:bg-white">
              <action.icon className="h-4 w-4" />
            </span>
            <span>
              <span className="block text-sm font-semibold text-[var(--northstar-text)]">{action.label}</span>
              <span className="mt-0.5 block text-xs leading-5 text-[var(--northstar-text-subtle)]">{action.description}</span>
            </span>
          </div>
        </button>
      ))}
    </section>
  );
}
