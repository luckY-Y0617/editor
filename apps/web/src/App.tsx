import { useEffect, useState } from "react";
import { DocumentSharePermissionsPage } from "./components/DocumentSharePermissionsPage";
import { KnowledgeEditorPage } from "./components/KnowledgeEditorPage";
import { LibrariesPage } from "./components/LibrariesPage";
import { NorthstarLoginPage } from "./components/NorthstarLoginPage";
import { SearchCommandPalette } from "./components/SearchCommandPalette";
import { VersionHistoryComparePage } from "./components/VersionHistoryComparePage";
import { WorkspaceHomePage } from "./components/WorkspaceHomePage";
import { OrganizationSettingsPage, PersonalSettingsPage, WorkspaceSettingsPage } from "./components/WorkspaceSettingsPage";
import { WorkspaceUpdatesPage } from "./components/WorkspaceUpdatesPage";
import { getStoredAccessToken, subscribeToAuthChanges } from "./lib/apiClient";
import {
  createSearchHash,
  getCanonicalHashRedirect,
  getEditorDocumentIdFromHash,
  getHashRoute,
  getSearchFiltersFromHash,
  getSettingsRouteTarget,
} from "./lib/hashRouting";

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
  const [lastContentHash, setLastContentHash] = useState(() => getSearchContentHash(window.location.hash));

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
  const isSearchOverlayRoute = route === "#search";
  const contentHash = isSearchOverlayRoute ? getSearchContentHash(lastContentHash) : hash;
  const contentRoute = getHashRoute(contentHash);
  const searchFilters = getSearchFiltersFromHash(hash);

  useEffect(() => {
    if (!isSearchOverlayRoute) {
      setLastContentHash(getSearchContentHash(hash));
    }
  }, [hash, isSearchOverlayRoute]);

  useEffect(() => {
    const openSearchFromKeyboard = (event: KeyboardEvent) => {
      if (event.defaultPrevented || event.key.toLowerCase() !== "k" || (!event.metaKey && !event.ctrlKey)) {
        return;
      }

      event.preventDefault();
      window.location.hash = createSearchHash();
    };

    window.addEventListener("keydown", openSearchFromKeyboard);
    return () => window.removeEventListener("keydown", openSearchFromKeyboard);
  }, []);

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

  const closeSearch = () => {
    window.location.hash = getSearchContentHash(lastContentHash);
  };

  const page = renderPage(contentRoute, contentHash);

  return (
    <>
      {page}
      {isSearchOverlayRoute ? (
        <SearchCommandPalette
          folderId={searchFilters.folderId}
          folderTitle={searchFilters.folderTitle}
          initialQuery={searchFilters.q ?? ""}
          libraryId={searchFilters.libraryId}
          onClose={closeSearch}
        />
      ) : null}
    </>
  );
}

function renderPage(route: string, hash: string) {
  if (route === "#editor") {
    return <KnowledgeEditorPage requestedDocumentId={getEditorDocumentIdFromHash(hash)} />;
  }

  if (route === "#home" || route === "#dashboard") {
    return <WorkspaceHomePage />;
  }

  if (route === "#libraries") {
    return <LibrariesPage />;
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

function getSearchContentHash(hash: string) {
  const normalizedHash = hash || "#home";
  const route = getHashRoute(normalizedHash);

  if (route === "#search" || route === "#discovery" || route === "#") {
    return "#home";
  }

  return normalizedHash;
}
