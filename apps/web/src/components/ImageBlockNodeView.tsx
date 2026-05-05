import { NodeViewWrapper, type NodeViewProps } from "@tiptap/react";
import { AlertCircle, FileImage, UploadCloud } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import type { ChangeEvent, DragEvent, MouseEvent as ReactMouseEvent } from "react";
import {
  clampImageBlockWidth,
  DEFAULT_IMAGE_BLOCK_WIDTH,
  normalizeImageBlockAlign,
  normalizeImageBlockAttributes,
  type ImageBlockAttributes,
} from "../extensions/ImageBlock";

const MAX_IMAGE_FILES = 3;
const MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024;
const SUPPORTED_IMAGE_TYPES = new Set(["image/png", "image/jpeg", "image/webp", "image/gif"]);
const SUPPORTED_IMAGE_EXTENSIONS = new Set(["png", "jpg", "jpeg", "webp", "gif"]);
const IMAGE_RESIZE_STATE_EVENT = "knowledge-image-resize-state-change";

type ResizeSide = "left" | "right";

type PreparedImage = ImageBlockAttributes;

export function ImageBlockNodeView({
  editor,
  getPos,
  node,
  selected,
  updateAttributes,
}: NodeViewProps) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const blockRef = useRef<HTMLDivElement | null>(null);
  const isMountedRef = useRef(true);
  const resizeCleanupRef = useRef<(() => void) | null>(null);
  const resizeAnimationFrameRef = useRef<number | null>(null);
  const resizeLatestWidthRef = useRef<number | null>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [isResizing, setIsResizing] = useState(false);
  const [draftWidth, setDraftWidth] = useState<number | null>(null);
  const [uploadError, setUploadError] = useState("");
  const [imageLoadFailed, setImageLoadFailed] = useState(false);

  const attrs = normalizeImageBlockAttributes(node.attrs);
  const hasImage = Boolean(attrs.src);
  const width = clampImageBlockWidth(attrs.width);
  const renderedWidth = draftWidth ?? width;
  const align = normalizeImageBlockAlign(attrs.align);

  const getCurrentPosition = useCallback(() => {
    const position = getPos();

    return typeof position === "number" ? position : null;
  }, [getPos]);

  const selectCurrentNode = useCallback(() => {
    const position = getCurrentPosition();

    if (position !== null && !editor.isDestroyed) {
      editor.commands.setNodeSelection(position);
    }
  }, [editor, getCurrentPosition]);

  const insertExtraImageBlocks = useCallback(
    (insertAt: number, images: PreparedImage[]) => {
      if (images.length === 0 || editor.isDestroyed) {
        return;
      }

      editor
        .chain()
        .focus()
        .insertContentAt(
          insertAt,
          images.map((image) => ({
            type: "imageBlock",
            attrs: image,
          })),
        )
        .run();
    },
    [editor],
  );

  const handleFiles = useCallback(
    async (fileList: FileList | File[]) => {
      const files = Array.from(fileList);

      if (files.length === 0) {
        return;
      }

      selectCurrentNode();
      setUploadError("");
      setIsUploading(true);

      try {
        const { files: validFiles, message } = validateImageFiles(files);

        if (message) {
          setUploadError(message);
        }

        if (validFiles.length === 0) {
          return;
        }

        const position = getCurrentPosition();
        const insertAt = position === null ? null : position + node.nodeSize;
        const preparedImages: PreparedImage[] = [];

        for (const file of validFiles) {
          const src = await uploadImage(file);

          if (!isMountedRef.current || editor.isDestroyed) {
            return;
          }

          preparedImages.push({
            src,
            alt: file.name,
            title: file.name,
            width: DEFAULT_IMAGE_BLOCK_WIDTH,
            align,
          });
        }

        const [firstImage, ...extraImages] = preparedImages;

        if (!firstImage) {
          return;
        }

        if (!isMountedRef.current || editor.isDestroyed) {
          return;
        }

        setImageLoadFailed(false);
        updateAttributes(firstImage);

        if (insertAt !== null) {
          insertExtraImageBlocks(insertAt, extraImages);
        }
      } catch {
        if (isMountedRef.current) {
          setUploadError("图片读取失败，请重试。");
        }
      } finally {
        if (isMountedRef.current) {
          setIsUploading(false);
          setIsDragOver(false);
        }
      }
    },
    [align, getCurrentPosition, insertExtraImageBlocks, node.nodeSize, selectCurrentNode, updateAttributes],
  );

  const handleInputChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      void handleFiles(event.currentTarget.files ?? []);
      event.currentTarget.value = "";
    },
    [handleFiles],
  );

  const openFilePicker = useCallback(
    (event: ReactMouseEvent<HTMLButtonElement>) => {
      event.preventDefault();
      event.stopPropagation();
      selectCurrentNode();
      inputRef.current?.click();
    },
    [selectCurrentNode],
  );

  const handleDrop = useCallback(
    (event: DragEvent<HTMLButtonElement>) => {
      event.preventDefault();
      event.stopPropagation();
      void handleFiles(event.dataTransfer.files);
    },
    [handleFiles],
  );

  const handleDragOver = useCallback((event: DragEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.stopPropagation();
    setIsDragOver(true);
  }, []);

  const handleDragLeave = useCallback((event: DragEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.stopPropagation();
    setIsDragOver(false);
  }, []);

  const startResize = useCallback(
    (side: ResizeSide, event: ReactMouseEvent<HTMLButtonElement>) => {
      event.preventDefault();
      event.stopPropagation();

      const block = blockRef.current;

      if (!block) {
        return;
      }

      selectCurrentNode();

      const blockRect = block.getBoundingClientRect();
      const startX = event.clientX;
      const startWidth = renderedWidth;
      const containerWidth = Math.max(1, blockRect.width);
      const resizeMultiplier = align === "center" ? 2 : 1;

      resizeCleanupRef.current?.();
      resizeLatestWidthRef.current = startWidth;
      setDraftWidth(startWidth);
      setIsResizing(true);
      document.body.classList.add("knowledge-image-resizing-global");
      notifyImageResizeState(true);

      const commitDraftWidth = () => {
        resizeAnimationFrameRef.current = null;

        if (resizeLatestWidthRef.current !== null) {
          setDraftWidth(resizeLatestWidthRef.current);
        }
      };

      const handleMouseMove = (moveEvent: MouseEvent) => {
        moveEvent.preventDefault();
        moveEvent.stopPropagation();

        const deltaPercent = ((moveEvent.clientX - startX) / containerWidth) * 100;
        const widthDelta = deltaPercent * resizeMultiplier;
        const nextWidth =
          side === "left" ? clampImageBlockWidth(startWidth - widthDelta) : clampImageBlockWidth(startWidth + widthDelta);

        resizeLatestWidthRef.current = nextWidth;

        if (resizeAnimationFrameRef.current === null) {
          resizeAnimationFrameRef.current = window.requestAnimationFrame(commitDraftWidth);
        }
      };

      const cleanupResize = () => {
        window.removeEventListener("mousemove", handleMouseMove);
        window.removeEventListener("mouseup", finishResize);

        if (resizeAnimationFrameRef.current !== null) {
          window.cancelAnimationFrame(resizeAnimationFrameRef.current);
          resizeAnimationFrameRef.current = null;
        }

        document.body.classList.remove("knowledge-image-resizing-global");
        notifyImageResizeState(false);
        resizeCleanupRef.current = null;
      };

      const finishResize = (finishEvent?: MouseEvent) => {
        finishEvent?.preventDefault();
        finishEvent?.stopPropagation();

        const finalWidth = clampImageBlockWidth(resizeLatestWidthRef.current ?? startWidth);

        cleanupResize();
        updateAttributes({ width: finalWidth });
        setDraftWidth(null);
        resizeLatestWidthRef.current = null;
        setIsResizing(false);
      };

      resizeCleanupRef.current = () => {
        cleanupResize();

        if (isMountedRef.current) {
          setDraftWidth(null);
          setIsResizing(false);
        }

        resizeLatestWidthRef.current = null;
      };
      window.addEventListener("mousemove", handleMouseMove);
      window.addEventListener("mouseup", finishResize);
    },
    [align, renderedWidth, selectCurrentNode, updateAttributes],
  );

  useEffect(
    () => () => {
      isMountedRef.current = false;
      resizeCleanupRef.current?.();
    },
    [],
  );

  return (
    <NodeViewWrapper
      ref={blockRef}
      className={[
        "knowledge-image-block",
        hasImage ? "knowledge-image-block-filled" : "knowledge-image-block-empty",
        selected ? "knowledge-image-selected" : "",
        isDragOver ? "knowledge-image-upload-dragging" : "",
        isResizing ? "knowledge-image-resizing" : "",
      ].join(" ")}
      contentEditable={false}
      data-align={align}
      onClick={(event: ReactMouseEvent<HTMLDivElement>) => {
        event.stopPropagation();
        selectCurrentNode();
      }}
      onMouseDown={(event: ReactMouseEvent<HTMLDivElement>) => {
        if (event.target instanceof Element && event.target.closest(".knowledge-image-resize-handle")) {
          return;
        }

        selectCurrentNode();
      }}
    >
      <div className="knowledge-image-frame" style={{ width: hasImage ? `${renderedWidth}%` : "100%" }}>
        {hasImage ? (
          imageLoadFailed ? (
            <div className="knowledge-image-load-error" role="status">
              <AlertCircle aria-hidden="true" className="h-4 w-4" />
              <span>图片加载失败</span>
            </div>
          ) : (
            <img
              alt={attrs.alt ?? ""}
              className="knowledge-image-view"
              draggable={false}
              onError={() => setImageLoadFailed(true)}
              onLoad={() => setImageLoadFailed(false)}
              src={attrs.src ?? undefined}
            />
          )
        ) : (
          <button
            aria-label="点击上传或拖拽图片到这里"
            className="knowledge-image-upload"
            disabled={isUploading}
            onClick={openFilePicker}
            onDragEnter={handleDragOver}
            onDragLeave={handleDragLeave}
            onDragOver={handleDragOver}
            onDrop={handleDrop}
            type="button"
          >
            <span className="knowledge-image-upload-icon">
              <FileImage aria-hidden="true" className="h-7 w-7" />
              <span className="knowledge-image-upload-badge">
                <UploadCloud aria-hidden="true" className="h-3.5 w-3.5" />
              </span>
            </span>
            <span className="knowledge-image-upload-title">
              {isUploading ? "正在读取图片..." : "点击上传或拖拽图片到这里"}
            </span>
            <span className="knowledge-image-upload-hint">最多 3 张，每张 5MB</span>
            {uploadError ? (
              <span className="knowledge-image-upload-error">
                <AlertCircle aria-hidden="true" className="h-3.5 w-3.5" />
                {uploadError}
              </span>
            ) : null}
          </button>
        )}
        {hasImage && selected ? (
          <>
            <button
              aria-label="向左调整图片宽度"
              className="knowledge-image-resize-handle knowledge-image-resize-handle-left"
              onMouseDown={(event) => startResize("left", event)}
              type="button"
            />
            <button
              aria-label="向右调整图片宽度"
              className="knowledge-image-resize-handle knowledge-image-resize-handle-right"
              onMouseDown={(event) => startResize("right", event)}
              type="button"
            />
          </>
        ) : null}
      </div>
      {hasImage && uploadError ? (
        <div className="knowledge-image-block-message" role="status">
          <AlertCircle aria-hidden="true" className="h-3.5 w-3.5" />
          {uploadError}
        </div>
      ) : null}
      <input
        accept="image/png,image/jpeg,image/webp,image/gif"
        className="knowledge-image-file-input"
        multiple
        onChange={handleInputChange}
        ref={inputRef}
        type="file"
      />
    </NodeViewWrapper>
  );
}

function validateImageFiles(files: File[]) {
  const messages: string[] = [];
  const acceptedFiles: File[] = [];
  const limitedFiles = files.slice(0, MAX_IMAGE_FILES);

  if (files.length > MAX_IMAGE_FILES) {
    messages.push("一次最多插入 3 张，已处理前 3 张。");
  }

  for (const file of limitedFiles) {
    if (!isSupportedImage(file)) {
      messages.push(`${file.name} 不是支持的图片格式。`);
      continue;
    }

    if (file.size > MAX_IMAGE_SIZE_BYTES) {
      messages.push(`${file.name} 超过 5MB。`);
      continue;
    }

    acceptedFiles.push(file);
  }

  return {
    files: acceptedFiles,
    message: messages[0] ?? "",
  };
}

function isSupportedImage(file: File) {
  if (SUPPORTED_IMAGE_TYPES.has(file.type)) {
    return true;
  }

  const extension = file.name.split(".").pop()?.toLowerCase();

  return extension ? SUPPORTED_IMAGE_EXTENSIONS.has(extension) : false;
}

function uploadImage(file: File) {
  // Current adapter stores a base64 data URL in node attrs so demo JSON can retain src.
  // This is not a production storage strategy: base64 significantly increases document size.
  // A real upload adapter should send the file to backend/object storage and return a stable URL.
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = () => {
      if (typeof reader.result === "string") {
        resolve(reader.result);
      } else {
        reject(new Error("Unable to read image file."));
      }
    };

    reader.onerror = () => reject(reader.error ?? new Error("Unable to read image file."));
    reader.readAsDataURL(file);
  });
}

function notifyImageResizeState(isResizing: boolean) {
  window.dispatchEvent(
    new CustomEvent(IMAGE_RESIZE_STATE_EVENT, {
      detail: { isResizing },
    }),
  );
}
