import { describe, expect, test } from "../test/harness";
import {
  displayLanguageStorageKey,
  getDisplayLanguageOptions,
  getStoredDisplayLocale,
  normalizeDisplayLocale,
  setStoredDisplayLocale,
  t,
} from "./i18n";

describe("i18n", () => {
  test("falls back to English when no browser storage is available", () => {
    const previousWindow = globalThis.window;
    delete (globalThis as { window?: Window }).window;

    try {
      expect(getStoredDisplayLocale()).toBe("en");
    } finally {
      (globalThis as { window?: Window }).window = previousWindow;
    }
  });

  test("reads and writes display language from localStorage", () => {
    const storage = createMemoryStorage();
    const previousWindow = globalThis.window;
    (globalThis as { window?: unknown }).window = {
      addEventListener() {},
      dispatchEvent() { return true; },
      localStorage: storage,
      removeEventListener() {},
    };

    try {
      setStoredDisplayLocale("zh-CN");

      expect(storage.getItem(displayLanguageStorageKey)).toBe("zh-CN");
      expect(getStoredDisplayLocale()).toBe("zh-CN");
    } finally {
      (globalThis as { window?: Window }).window = previousWindow;
    }
  });

  test("normalizes invalid locale values to English", () => {
    expect(normalizeDisplayLocale("fr")).toBe("en");
    expect(normalizeDisplayLocale(null)).toBe("en");
    expect(normalizeDisplayLocale("zh-CN")).toBe("zh-CN");
  });

  test("falls back to English translation when locale is invalid", () => {
    expect(t(normalizeDisplayLocale("bad"), "nav.settings")).toBe("Settings");
    expect(t("en", "settings.organizationHeading")).toBe("Organization Settings Assessment");
    expect(t("en", "settings.contractReadiness")).toBe("Contract readiness");
    expect(t("en", "settings.readiness.missingContract")).toBe("Missing contract");
    expect(t("en", "settings.proposedEndpoint")).toBe("Proposed endpoint");
    expect(t("en", "settings.proposedNotImplemented")).toBe("Proposed only. Not implemented or live-backed.");
    expect(t("en", "settings.risk.medium")).toBe("Medium");
    expect(t("en", "settings.recommendedFirstSlice")).toBe("Recommended First Slice");
    expect(t("en", "settings.status.notExposed")).toBe("Not exposed");
    expect(t("en", "settings.editOrganizationProfile")).toBe("Edit organization profile");
    expect(t("en", "settings.saveChanges")).toBe("Save changes");
    expect(t("en", "settings.slugHelp")).toContain("does not enable organization URL routing");
    expect(t("en", "share.createInternalLink")).toBe("Create internal link");
    expect(t("en", "share.internalLinkCreated")).toContain("Internal share link created");
    expect(t("en", "share.linkRevoked")).toBe("Share link revoked.");
    expect(t("en", "settings.informationArchitecture")).toBe("Settings architecture");
    expect(t("en", "settings.workspaceNotificationPreference")).toBe("Workspace notification default");
    expect(t("en", "settings.inventoryWorkspaceNotifications")).toBe("Workspace notification preferences");
    expect(t("en", "settings.recommendedSettingsClosure")).toBe("Recommended settings closure");
    expect(t("en", "settings.workspaceProfileReadOnlyHelp")).toContain("no workspace profile update API contract");
    expect(t("en", "settings.libraryOperationsSurfaceHelp")).toContain("Settings shows summary and links only");
    expect(t("en", "settings.resourceShareSurfaceHelp")).toContain("public-link behavior is unchanged");
    expect(t("en", "settings.centerHeading")).toBe("Settings");
    expect(t("en", "settings.accessIdentity")).toBe("Access & identity");
    expect(t("en", "settings.membersInventory")).toBe("Members inventory");
    expect(t("en", "settings.scopeDeferred")).toBe("Deferred");
    expect(t("en", "settings.personalSettingsHeading")).toBe("Personal Settings");
    expect(t("en", "settings.organizationSettingsHeading")).toBe("Organization Settings");
    expect(t("en", "topbar.workspaceSwitcher")).toBe("Workspace switcher");
    expect(t("en", "topbar.personalSettings")).toBe("Personal settings");
    expect(t("en", "topbar.organizationSettings")).toBe("Organization settings");
    expect(t("en", "topbar.workspaceSwitchingDeferred")).toContain("not supported");
    expect(t("zh-CN", "nav.settings")).toBe("设置");
    expect(t("zh-CN", "settings.libraryHeading")).toBe("资料库设置");
    expect(t("zh-CN", "settings.editOrganizationProfile")).toBe("编辑组织资料");
    expect(t("zh-CN", "settings.profileUpdated")).toBe("资料已更新");
    expect(t("zh-CN", "share.createInternalLink")).toBe("\u521b\u5efa\u5185\u90e8\u94fe\u63a5");
    expect(t("zh-CN", "share.linkRevoked")).toBe("\u5206\u4eab\u94fe\u63a5\u5df2\u64a4\u9500\u3002");
    expect(t("zh-CN", "settings.workspaceNotificationWatched")).toBe("\u5173\u6ce8\u5de5\u4f5c\u533a");
    expect(t("zh-CN", "settings.recommendedSettingsClosure")).toBe("\u5efa\u8bae\u7684\u8bbe\u7f6e\u6536\u53e3");
    expect(t("zh-CN", "settings.resourceShareSurfaceHelp")).toContain("public-link");
    expect(t("zh-CN", "settings.centerHeading")).toBe("\u8bbe\u7f6e");
    expect(t("zh-CN", "settings.accessIdentity")).toBe("\u8bbf\u95ee\u4e0e\u8eab\u4efd");
    expect(t("zh-CN", "settings.personalSettingsHeading")).toBe("\u4e2a\u4eba\u8bbe\u7f6e");
    expect(t("zh-CN", "settings.organizationSettingsHeading")).toBe("\u7ec4\u7ec7\u8bbe\u7f6e");
    expect(t("zh-CN", "topbar.workspaceSwitcher")).toBe("\u5de5\u4f5c\u533a\u5207\u6362\u5668");
  });

  test("exposes supported display language options", () => {
    expect(getDisplayLanguageOptions()).toEqual([
      { label: "English", locale: "en" },
      { label: "简体中文", locale: "zh-CN" },
    ]);
  });
});

function createMemoryStorage(): Storage {
  const values = new Map<string, string>();
  return {
    clear() {
      values.clear();
    },
    getItem(key: string) {
      return values.get(key) ?? null;
    },
    key(index: number) {
      return Array.from(values.keys())[index] ?? null;
    },
    get length() {
      return values.size;
    },
    removeItem(key: string) {
      values.delete(key);
    },
    setItem(key: string, value: string) {
      values.set(key, value);
    },
  };
}
