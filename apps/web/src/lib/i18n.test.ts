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
    expect(t("zh-CN", "nav.settings")).toBe("设置");
    expect(t("zh-CN", "settings.libraryHeading")).toBe("资料库设置");
    expect(t("zh-CN", "settings.editOrganizationProfile")).toBe("编辑组织资料");
    expect(t("zh-CN", "settings.profileUpdated")).toBe("资料已更新");
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
