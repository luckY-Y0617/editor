import { useEffect, useState } from "react";
import { DocumentSharePermissionsPage } from "./components/DocumentSharePermissionsPage";
import { KnowledgeEditorPage } from "./components/KnowledgeEditorPage";
import { NorthstarLoginPage } from "./components/NorthstarLoginPage";
import { PermissionAdminSurfacesPage } from "./components/PermissionAdminSurfacesPage";
import { SearchDiscoveryPage } from "./components/SearchDiscoveryPage";
import { VersionHistoryComparePage } from "./components/VersionHistoryComparePage";
import { WorkspaceHomePage } from "./components/WorkspaceHomePage";
import { WorkspaceUpdatesPage } from "./components/WorkspaceUpdatesPage";
import { getStoredAccessToken, subscribeToAuthChanges } from "./lib/apiClient";

const protectedHashes = new Set([
  "#home",
  "#dashboard",
  "#editor",
  "#search",
  "#discovery",
  "#versions",
  "#compare",
  "#version-history",
  "#share",
  "#permissions",
  "#workspace-members",
  "#members",
  "#permission-admin",
  "#workspace-groups",
  "#groups",
  "#scim",
  "#updates",
  "#notifications",
]);

export default function App() {
  const [hash, setHash] = useState(window.location.hash);
  const [isAuthenticated, setIsAuthenticated] = useState(() => Boolean(getStoredAccessToken()));

  useEffect(() => {
    const syncHash = () => setHash(window.location.hash);
    const syncAuthState = () => setIsAuthenticated(Boolean(getStoredAccessToken()));

    window.addEventListener("hashchange", syncHash);
    const unsubscribeFromAuthChanges = subscribeToAuthChanges(syncAuthState);

    return () => {
      window.removeEventListener("hashchange", syncHash);
      unsubscribeFromAuthChanges();
    };
  }, []);

  if (protectedHashes.has(hash) && !isAuthenticated) {
    return <NorthstarLoginPage />;
  }

  if (hash === "#editor") {
    return <KnowledgeEditorPage />;
  }

  if (hash === "#home" || hash === "#dashboard") {
    return <WorkspaceHomePage />;
  }

  if (hash === "#search" || hash === "#discovery") {
    return <SearchDiscoveryPage />;
  }

  if (hash === "#versions" || hash === "#compare" || hash === "#version-history") {
    return <VersionHistoryComparePage />;
  }

  if (hash === "#share" || hash === "#permissions") {
    return <DocumentSharePermissionsPage />;
  }

  if (hash === "#workspace-members" || hash === "#members" || hash === "#permission-admin") {
    return <PermissionAdminSurfacesPage initialTab="members" />;
  }

  if (hash === "#workspace-groups" || hash === "#groups") {
    return <PermissionAdminSurfacesPage initialTab="groups" />;
  }

  if (hash === "#scim") {
    return <PermissionAdminSurfacesPage initialTab="scim" />;
  }

  if (hash === "#updates" || hash === "#notifications") {
    return <WorkspaceUpdatesPage />;
  }

  return <NorthstarLoginPage />;
}
