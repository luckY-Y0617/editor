import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        northstar: {
          sidebar: "var(--northstar-sidebar)",
          primary: "var(--northstar-primary)",
          accent: "var(--northstar-accent)",
          border: "var(--northstar-border)",
          page: "var(--northstar-page)",
          text: "var(--northstar-text)",
          muted: "var(--northstar-text-muted)",
        },
      },
      fontFamily: {
        sans: [
          "Inter",
          "ui-sans-serif",
          "system-ui",
          "-apple-system",
          "BlinkMacSystemFont",
          "Segoe UI",
          "Microsoft YaHei",
          "sans-serif",
        ],
      },
      boxShadow: {
        soft: "0 12px 40px rgba(31, 41, 55, 0.06)",
        northstar: "0 18px 50px rgba(7, 23, 55, 0.12)",
      },
      borderRadius: {
        northstar: "10px",
      },
    },
  },
  plugins: [],
} satisfies Config;
