import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

declare const process: {
  cwd(): string;
};

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const apiProxyTarget = normalizeApiProxyTarget(
    env.VITE_NORTHSTAR_API_PROXY_TARGET?.trim() || "https://localhost:7036",
  );

  return {
    plugins: [
      {
        name: "serve-root-index",
        configureServer(server) {
          server.middlewares.use((req, _res, next) => {
            if (req.url === "/") {
              req.url = "/index.html";
            }
            next();
          });
        },
      },
      react(),
    ],
    server: {
      proxy: {
        "/api/v1": {
          changeOrigin: true,
          secure: false,
          target: apiProxyTarget,
        },
      },
    },
  };
});

function normalizeApiProxyTarget(value: string) {
  return value.trim().replace(/\/api\/v1\/?$/, "").replace(/\/+$/, "");
}
