import { mergeAttributes, Node } from "@tiptap/core";
import { ReactNodeViewRenderer } from "@tiptap/react";
import { ImageBlockNodeView } from "../components/ImageBlockNodeView";

export type ImageBlockAlign = "left" | "center" | "right";
export const DEFAULT_IMAGE_BLOCK_WIDTH = 85;
export const IMAGE_BLOCK_MIN_WIDTH = 30;
export const IMAGE_BLOCK_MAX_WIDTH = 100;

export type ImageBlockAttributes = {
  src: string | null;
  alt: string | null;
  title: string | null;
  width: number;
  align: ImageBlockAlign;
};

declare module "@tiptap/core" {
  interface Commands<ReturnType> {
    imageBlock: {
      setImageBlock: (attrs?: Partial<ImageBlockAttributes>) => ReturnType;
    };
  }
}

export const ImageBlock = Node.create({
  name: "imageBlock",

  group: "block",

  atom: true,

  selectable: true,

  draggable: true,

  addAttributes() {
    return {
      src: {
        default: null,
        parseHTML: (element) => getImageElement(element)?.getAttribute("src") ?? null,
      },
      alt: {
        default: null,
        parseHTML: (element) => getImageElement(element)?.getAttribute("alt") ?? null,
      },
      title: {
        default: null,
        parseHTML: (element) => getImageElement(element)?.getAttribute("title") ?? null,
      },
      width: {
        default: DEFAULT_IMAGE_BLOCK_WIDTH,
        parseHTML: (element) =>
          clampImageBlockWidth(Number(element.getAttribute("data-width") ?? DEFAULT_IMAGE_BLOCK_WIDTH)),
      },
      align: {
        default: "center",
        parseHTML: (element) => normalizeImageBlockAlign(element.getAttribute("data-align")),
      },
    };
  },

  parseHTML() {
    return [
      {
        tag: 'figure[data-type="image-block"]',
      },
    ];
  },

  renderHTML({ HTMLAttributes, node }) {
    const src = typeof node.attrs.src === "string" ? node.attrs.src : null;
    const alt = typeof node.attrs.alt === "string" ? node.attrs.alt : null;
    const width = clampImageBlockWidth(Number(node.attrs.width ?? DEFAULT_IMAGE_BLOCK_WIDTH));
    const align = normalizeImageBlockAlign(node.attrs.align);

    const figureAttrs = mergeAttributes(HTMLAttributes, {
      "data-type": "image-block",
      "data-width": String(width),
      "data-align": align,
      class: "knowledge-image-block-html",
    });

    if (!src) {
      return ["figure", figureAttrs];
    }

    return [
      "figure",
      figureAttrs,
      [
        "img",
        {
          src,
          alt: alt ?? undefined,
          style: `width: ${width}%;`,
        },
      ],
    ];
  },

  addCommands() {
    return {
      setImageBlock:
        (attrs = {}) =>
        ({ commands }) =>
          commands.insertContent({
            type: this.name,
            attrs: normalizeImageBlockAttributes(attrs),
          }),
    };
  },

  addNodeView() {
    return ReactNodeViewRenderer(ImageBlockNodeView);
  },
});

export function normalizeImageBlockAttributes(
  attrs: Partial<ImageBlockAttributes> = {},
): ImageBlockAttributes {
  return {
    src: typeof attrs.src === "string" && attrs.src.trim().length > 0 ? attrs.src : null,
    alt: typeof attrs.alt === "string" && attrs.alt.trim().length > 0 ? attrs.alt : null,
    title: typeof attrs.title === "string" && attrs.title.trim().length > 0 ? attrs.title : null,
    width: clampImageBlockWidth(Number(attrs.width ?? DEFAULT_IMAGE_BLOCK_WIDTH)),
    align: normalizeImageBlockAlign(attrs.align),
  };
}

export function clampImageBlockWidth(width: number) {
  if (!Number.isFinite(width)) {
    return DEFAULT_IMAGE_BLOCK_WIDTH;
  }

  return Math.min(IMAGE_BLOCK_MAX_WIDTH, Math.max(IMAGE_BLOCK_MIN_WIDTH, Math.round(width)));
}

export function normalizeImageBlockAlign(value: unknown): ImageBlockAlign {
  if (value === "left" || value === "right" || value === "center") {
    return value;
  }

  return "center";
}

function getImageElement(element: HTMLElement) {
  if (element instanceof HTMLImageElement) {
    return element;
  }

  return element.querySelector("img");
}
