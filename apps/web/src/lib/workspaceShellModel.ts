import type { AuthWorkspaceDto } from "./authClient";

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
