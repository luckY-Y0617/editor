import { useEffect, useState } from "react";
import { DocumentSharePermissionsPage } from "./components/DocumentSharePermissionsPage";
import { KnowledgeEditorPage } from "./components/KnowledgeEditorPage";
import { LibrariesPage } from "./components/LibrariesPage";
import { NorthstarLoginPage } from "./components/NorthstarLoginPage";
import { SearchDiscoveryPage } from "./components/SearchDiscoveryPage";
import { VersionHistoryComparePage } from "./components/VersionHistoryComparePage";
import { WorkspaceHomePage } from "./components/WorkspaceHomePage";
import { OrganizationSettingsPage, PersonalSettingsPage, WorkspaceSettingsPage } from "./components/WorkspaceSettingsPage";
import { WorkspaceUpdatesPage } from "./components/WorkspaceUpdatesPage";
import { getStoredAccessToken, subscribeToAuthChanges } from "./lib/apiClient";
import { getCanonicalHashRedirect, getEditorDocumentIdFromHash, getHashRoute, getSettingsRouteTarget } from "./lib/hashRouting";

const protectedHashes = new Set([
  "#home",
  "#dashboard",
  "#libraries",
  "#editor",
  "#search",
  "#discovery",
  "#settings",
  "#personal-settings",
  "#organization-settings",
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

  const route = getHashRoute(hash);
  const canonicalHash = getCanonicalHashRedirect(hash);

  useEffect(() => {
    if (canonicalHash && window.location.hash !== canonicalHash) {
      window.location.replace(canonicalHash);
    }
  }, [canonicalHash]);

  if (canonicalHash) {
    return null;
  }

  if (protectedHashes.has(route) && !isAuthenticated) {
    return <NorthstarLoginPage />;
  }

  if (route === "#editor") {
    return <KnowledgeEditorPage requestedDocumentId={getEditorDocumentIdFromHash(hash)} />;
  }

  if (route === "#home" || route === "#dashboard") {
    return <WorkspaceHomePage />;
  }

  if (route === "#libraries") {
    return <LibrariesPage />;
  }

  if (route === "#search" || route === "#discovery") {
    return <SearchDiscoveryPage />;
  }

  if (route === "#personal-settings") {
    return <PersonalSettingsPage />;
  }

  if (route === "#organization-settings") {
    return <OrganizationSettingsPage />;
  }

  if (route === "#settings") {
    const settingsRouteTarget = getSettingsRouteTarget(hash);
    if (settingsRouteTarget === "personal") {
      return <PersonalSettingsPage />;
    }

    if (settingsRouteTarget === "organization") {
      return <OrganizationSettingsPage />;
    }

    return <WorkspaceSettingsPage />;
  }

  if (route === "#versions" || route === "#compare" || route === "#version-history") {
    return <VersionHistoryComparePage />;
  }

  if (route === "#share" || route === "#permissions") {
    return <DocumentSharePermissionsPage />;
  }

  if (route === "#updates" || route === "#notifications") {
    return <WorkspaceUpdatesPage />;
  }

  return <NorthstarLoginPage />;
}
