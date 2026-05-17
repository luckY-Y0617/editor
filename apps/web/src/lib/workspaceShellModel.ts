import type { AuthWorkspaceDto } from "./authClient";

export type WorkspaceNavItemId = "groups" | "home" | "libraries" | "members" | "settings" | "sharing" | "updates";

export type WorkspaceNavItem = {
  href: string;
  id: WorkspaceNavItemId;
  labelKey: "nav.groups" | "nav.home" | "nav.libraries" | "nav.members" | "nav.settings" | "nav.updates";
};

export type WorkspaceSwitcherRow = {
  disabled: boolean;
  disabledReason: string;
  href: string;
  id: string;
  isCurrent: boolean;
  name: string;
  role: string;
};

export type WorkspaceSwitcherModel = {
  currentName: string;
  currentRole: string;
  rows: WorkspaceSwitcherRow[];
};

export function createWorkspaceNavItems(): WorkspaceNavItem[] {
  return [
    { id: "home", labelKey: "nav.home", href: "#home" },
    { id: "libraries", labelKey: "nav.libraries", href: "#libraries" },
    { id: "sharing", labelKey: "nav.updates", href: "#access-sharing" },
    { id: "members", labelKey: "nav.members", href: "#members" },
    { id: "groups", labelKey: "nav.groups", href: "#groups" },
    { id: "settings", labelKey: "nav.settings", href: "#settings" },
  ];
}

export function toWorkspaceSwitcherModel(
  workspaces: AuthWorkspaceDto[],
  configuredWorkspaceId: string | null,
  fallbackName = "Workspace",
): WorkspaceSwitcherModel {
  const currentWorkspace = getCurrentWorkspace(workspaces, configuredWorkspaceId);

  return {
    currentName: currentWorkspace?.name ?? fallbackName,
    currentRole: currentWorkspace?.role ?? "",
    rows: workspaces.map((workspace) => {
      const isCurrent = currentWorkspace?.id === workspace.id;
      return {
        disabled: !isCurrent,
        disabledReason: isCurrent ? "" : "Workspace switching is deferred.",
        href: isCurrent ? "#home" : "#",
        id: workspace.id,
        isCurrent,
        name: workspace.name,
        role: workspace.role,
      };
    }),
  };
}

function getCurrentWorkspace(workspaces: AuthWorkspaceDto[], configuredWorkspaceId: string | null) {
  return workspaces.find((workspace) => workspace.id === configuredWorkspaceId) ?? workspaces[0] ?? null;
}
