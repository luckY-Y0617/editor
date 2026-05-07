import { describe, expect, test } from "../test/harness";
import { toWorkspaceSwitcherModel } from "./workspaceShellModel";

const currentWorkspaceId = "11111111-1111-4111-8111-111111111111";
const otherWorkspaceId = "22222222-2222-4222-8222-222222222222";

describe("workspaceShellModel", () => {
  test("uses the configured workspace as the current workspace switcher label", () => {
    const model = toWorkspaceSwitcherModel([
      { id: otherWorkspaceId, name: "Research", role: "viewer" },
      { id: currentWorkspaceId, name: "Northstar", role: "owner" },
    ], currentWorkspaceId);

    expect(model.currentName).toBe("Northstar");
    expect(model.currentRole).toBe("owner");
    expect(model.rows.map((row) => [row.name, row.isCurrent, row.disabled])).toEqual([
      ["Research", false, true],
      ["Northstar", true, false],
    ]);
    expect(model.rows[0]?.disabledReason).toBe("Workspace switching is deferred.");
  });

  test("does not fake workspace switching when no current workspace can be resolved", () => {
    const model = toWorkspaceSwitcherModel([], null, "Workspace");

    expect(model.currentName).toBe("Workspace");
    expect(model.currentRole).toBe("");
    expect(model.rows).toEqual([]);
  });
});
