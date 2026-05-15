import { type CSSProperties, useEffect, useMemo, useState } from "react";
import { WorkspaceHomeSidebar } from "./WorkspaceHomeSidebar";
import { WorkspaceHomeTopBar } from "./WorkspaceHomeTopBar";
import { WorkspaceLinkManagementPanel } from "./WorkspaceLinkManagementPanel";
import { getBootstrap, type BootstrapResponse } from "../lib/appApi";
import { ApiClientError, getConfiguredApiBaseUrl, getConfiguredWorkspaceId } from "../lib/apiClient";
import { t, useDisplayLanguage } from "../lib/i18n";
import coordinatePatternUrl from "../assets/svg/patterns/coordinate-ticks.svg";
import routePatternUrl from "../assets/svg/patterns/route-line.svg";
import topographicPatternUrl from "../assets/svg/patterns/topographic-lines.svg";

type AccessSharingStatus = "error" | "forbidden" | "idle" | "loading" | "ready" | "unconfigured";

const accessSharingPatternStyle = {
  "--permission-admin-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--permission-admin-route-pattern": `url(${routePatternUrl})`,
  "--permission-admin-topographic-pattern": `url(${topographicPatternUrl})`,
  "--workspace-home-coordinate-pattern": `url(${coordinatePatternUrl})`,
  "--workspace-home-route-pattern": `url(${routePatternUrl})`,
  "--workspace-home-topographic-pattern": `url(${topographicPatternUrl})`,
} as CSSProperties;

export function AccessSharingPage() {
  const { locale } = useDisplayLanguage();
  const bootstrap = useAccessSharingBootstrap();
  const workspaceId = bootstrap.data?.workspace.id ?? getConfiguredWorkspaceId();

  return (
    <main className="access-sharing-shell permission-admin-shell flex h-screen flex-col overflow-hidden" style={accessSharingPatternStyle}>
      <WorkspaceHomeTopBar activeItem="sharing" />
      <div className="permission-admin-body flex min-h-0 flex-1 overflow-hidden">
        <WorkspaceHomeSidebar activeItem="sharing" showCollections={false} />
        <section className="access-sharing-content permission-admin-feed editor-scrollbar min-w-0 flex-1 overflow-y-auto">
          <div className="workspace-home-mobile-nav md:hidden" aria-label="Workspace navigation">
            <a href="#home">{t(locale, "nav.home")}</a>
            <a href="#libraries">{t(locale, "nav.libraries")}</a>
            <a aria-current="page" href="#access-sharing">访问与分享</a>
            <a href="#settings">{t(locale, "nav.settings")}</a>
          </div>
          <div className="access-sharing-inner permission-admin-feed-inner">
            <WorkspaceLinkManagementPanel workspaceId={workspaceId ?? null} />
            {bootstrap.status === "error" || bootstrap.status === "forbidden" ? (
              <p className="permission-admin-inline-status is-error" role="alert">
                {bootstrap.error || "工作区上下文加载失败，链接列表可能无法显示。"}
              </p>
            ) : null}
          </div>
        </section>
      </div>
    </main>
  );
}

function useAccessSharingBootstrap() {
  const apiBaseUrl = useMemo(() => getConfiguredApiBaseUrl(), []);
  const [data, setData] = useState<BootstrapResponse | null>(null);
  const [error, setError] = useState("");
  const [status, setStatus] = useState<AccessSharingStatus>(() => (apiBaseUrl ? "idle" : "unconfigured"));

  useEffect(() => {
    if (!apiBaseUrl) {
      setStatus("unconfigured");
      setData(null);
      return undefined;
    }

    const controller = new AbortController();
    setStatus("loading");
    setError("");
    void getBootstrap(controller.signal)
      .then((response) => {
        setData(response);
        setStatus("ready");
      })
      .catch((loadError: unknown) => {
        if (controller.signal.aborted || (loadError instanceof DOMException && loadError.name === "AbortError")) {
          return;
        }

        setData(null);
        setStatus(loadError instanceof ApiClientError && (loadError.status === 401 || loadError.status === 403) ? "forbidden" : "error");
        setError(loadError instanceof Error ? loadError.message : "工作区上下文加载失败。");
      });

    return () => controller.abort();
  }, [apiBaseUrl]);

  return { data, error, status };
}
