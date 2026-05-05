import { useCallback, useEffect, useMemo, useRef, useState } from "react";

export type SaveStatus = "saved" | "editing" | "saving" | "created";

export function useMockAutoSave() {
  const initialDate = useMemo(() => new Date(), []);
  const [saveStatus, setSaveStatus] = useState<SaveStatus>("saved");
  const [updatedAt, setUpdatedAt] = useState<Date>(initialDate);
  const [savedAt, setSavedAt] = useState<Date>(initialDate);
  const editingTimerRef = useRef<number>();
  const savingTimerRef = useRef<number>();

  const clearTimers = useCallback(() => {
    window.clearTimeout(editingTimerRef.current);
    window.clearTimeout(savingTimerRef.current);
  }, []);

  const markDirty = useCallback(() => {
    const changedAt = new Date();

    clearTimers();
    setUpdatedAt(changedAt);
    setSaveStatus("editing");

    editingTimerRef.current = window.setTimeout(() => {
      setSaveStatus("saving");

      savingTimerRef.current = window.setTimeout(() => {
        const savedDate = new Date();

        setSavedAt(savedDate);
        setUpdatedAt(savedDate);
        setSaveStatus("saved");
      }, 650);
    }, 550);
  }, [clearTimers]);

  const resetSaved = useCallback(
    (date: Date) => {
      clearTimers();
      setUpdatedAt(date);
      setSavedAt(date);
      setSaveStatus("saved");
    },
    [clearTimers],
  );

  const markCreated = useCallback(
    (date = new Date()) => {
      clearTimers();
      setUpdatedAt(date);
      setSavedAt(date);
      setSaveStatus("created");
    },
    [clearTimers],
  );

  useEffect(() => clearTimers, [clearTimers]);

  return {
    markCreated,
    saveStatus,
    saveStatusLabel: getSaveStatusLabel(saveStatus, savedAt),
    shortSaveStatusLabel: getShortSaveStatusLabel(saveStatus),
    updatedAtLabel: saveStatus === "saved" ? formatUpdatedAt(updatedAt) : "刚刚",
    markDirty,
    resetSaved,
  };
}

function getSaveStatusLabel(status: SaveStatus, savedAt: Date) {
  if (status === "created") {
    return "已创建";
  }

  if (status === "editing") {
    return "编辑中";
  }

  if (status === "saving") {
    return "保存中";
  }

  return `已保存于 ${formatClockTime(savedAt)}`;
}

function getShortSaveStatusLabel(status: SaveStatus) {
  if (status === "created") {
    return "已创建";
  }

  if (status === "editing") {
    return "编辑中";
  }

  if (status === "saving") {
    return "保存中";
  }

  return "已保存";
}

function formatClockTime(date: Date) {
  return new Intl.DateTimeFormat("zh-CN", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}

function formatUpdatedAt(date: Date) {
  const now = new Date();
  const sameDay =
    date.getFullYear() === now.getFullYear() &&
    date.getMonth() === now.getMonth() &&
    date.getDate() === now.getDate();

  if (sameDay) {
    return `今天 ${formatClockTime(date)}`;
  }

  return new Intl.DateTimeFormat("zh-CN", {
    month: "numeric",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}
