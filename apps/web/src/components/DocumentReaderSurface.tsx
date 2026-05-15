import { mergeAttributes, Node } from "@tiptap/core";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import { Details, DetailsContent, DetailsSummary } from "@tiptap/extension-details";
import Highlight from "@tiptap/extension-highlight";
import ImageExtension from "@tiptap/extension-image";
import Link from "@tiptap/extension-link";
import { Table } from "@tiptap/extension-table";
import TableCell from "@tiptap/extension-table-cell";
import TableHeader from "@tiptap/extension-table-header";
import TableRow from "@tiptap/extension-table-row";
import TaskItem from "@tiptap/extension-task-item";
import TaskList from "@tiptap/extension-task-list";
import TextAlign from "@tiptap/extension-text-align";
import { EditorContent, useEditor, type JSONContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import type { ReactNode } from "react";
import { useMemo } from "react";
import { lowlight } from "../lib/lowlight";

export const documentReaderSurfaceClass =
  "document-reader-surface atlas-document-flow mx-auto min-h-full w-full max-w-[920px] px-8 pb-20 pt-12 sm:px-12 lg:px-[72px]";
export const documentReaderHeaderClass = "document-reader-header atlas-document-header mb-5";
export const documentReaderBodyClass = "document-reader-body";
export const documentReaderTypographyClass = "knowledge-tiptap-content document-reader-typography";

type DocumentReaderSurfaceProps = {
  bodyClassName?: string;
  children: ReactNode;
  className?: string;
  dividerIcon?: ReactNode;
  kicker?: string | null;
  metadata?: ReactNode;
  title?: string;
  titleNode?: ReactNode;
};

export function DocumentReaderSurface({
  bodyClassName = "",
  children,
  className = "",
  dividerIcon,
  kicker,
  metadata,
  title,
  titleNode,
}: DocumentReaderSurfaceProps) {
  return (
    <article className={[documentReaderSurfaceClass, className].filter(Boolean).join(" ")}>
      <header className={documentReaderHeaderClass}>
        {kicker ? <div className="ns-kicker mb-3">{kicker}</div> : null}
        {titleNode ?? <h1 className="document-reader-title">{title}</h1>}
        {dividerIcon ? <div className="atlas-compass-divider mt-4 text-[var(--ns-stone-300)]">{dividerIcon}</div> : null}
        {metadata}
      </header>
      <div className={[documentReaderBodyClass, bodyClassName].filter(Boolean).join(" ")}>{children}</div>
    </article>
  );
}

export function ReadonlyDocumentContent({ content }: { content: JSONContent }) {
  const sanitizedContent = useMemo(() => sanitizeReadonlyDocumentContent(content), [content]);
  const editor = useEditor(
    {
      content: sanitizedContent,
      editable: false,
      editorProps: {
        attributes: {
          class: documentReaderTypographyClass,
          spellcheck: "false",
        },
      },
      extensions: createReadonlyDocumentExtensions(),
    },
    [sanitizedContent],
  );

  return <EditorContent editor={editor} />;
}

const ReadonlyImageBlock = Node.create({
  name: "imageBlock",
  group: "block",
  atom: true,

  addAttributes() {
    return {
      align: { default: "center" },
      alt: { default: null },
      fileId: { default: null },
      src: { default: null },
      title: { default: null },
      width: { default: 85 },
    };
  },

  parseHTML() {
    return [{ tag: 'figure[data-type="image-block"]' }];
  },

  renderHTML({ HTMLAttributes, node }) {
    const src = typeof node.attrs.src === "string" ? node.attrs.src : null;
    const alt = typeof node.attrs.alt === "string" ? node.attrs.alt : "";
    const width = typeof node.attrs.width === "number" ? Math.min(100, Math.max(30, node.attrs.width)) : 85;
    const align = node.attrs.align === "left" || node.attrs.align === "right" ? node.attrs.align : "center";
    const attrs = mergeAttributes(HTMLAttributes, {
      "data-align": align,
      "data-type": "image-block",
      class: "knowledge-image-block-html",
    });

    return src ? ["figure", attrs, ["img", { alt, src, style: `width: ${width}%;` }]] : ["figure", attrs];
  },
});

function createReadonlyDocumentExtensions() {
  return [
    StarterKit.configure({
      codeBlock: false,
      horizontalRule: false,
      link: false,
      underline: false,
    }),
    CodeBlockLowlight.configure({
      defaultLanguage: "plaintext",
      lowlight,
      HTMLAttributes: {
        class: "knowledge-code-block",
      },
    }),
    Details.configure({
      openClassName: "is-open",
      HTMLAttributes: {
        class: "knowledge-details",
      },
    }),
    DetailsSummary.configure({
      HTMLAttributes: {
        class: "knowledge-details-summary",
      },
    }),
    DetailsContent.configure({
      HTMLAttributes: {
        class: "knowledge-details-content",
      },
    }),
    TaskList,
    TaskItem.configure({ nested: true }),
    Highlight.configure({
      multicolor: false,
      HTMLAttributes: {
        class: "knowledge-highlight",
      },
    }),
    TextAlign.configure({
      alignments: ["left", "center", "right"],
      types: ["heading", "paragraph"],
    }),
    ImageExtension.configure({
      allowBase64: false,
      HTMLAttributes: {
        class: "knowledge-image",
      },
      inline: false,
    }),
    ReadonlyImageBlock,
    Table.configure({
      resizable: false,
      HTMLAttributes: {
        class: "knowledge-table",
      },
    }),
    TableRow,
    TableHeader,
    TableCell,
    Link.configure({
      autolink: false,
      enableClickSelection: false,
      linkOnPaste: false,
      openOnClick: true,
      HTMLAttributes: {
        class: "knowledge-link",
        rel: "noopener noreferrer nofollow",
        target: "_blank",
      },
    }),
  ];
}

export function sanitizeReadonlyDocumentContent(content: JSONContent): JSONContent {
  const node = sanitizeReadonlyDocumentNode(content);
  return node ?? { type: "doc", content: [] };
}

function sanitizeReadonlyDocumentNode(node: JSONContent): JSONContent | null {
  if ((node.type === "image" || node.type === "imageBlock") && hasInternalFileReference(node)) {
    return {
      type: "paragraph",
      content: [{ type: "text", text: "File preview is not available in this public view." }],
    };
  }

  return {
    ...node,
    content: node.content?.map(sanitizeReadonlyDocumentNode).filter((child): child is JSONContent => Boolean(child)),
  };
}

function hasInternalFileReference(node: JSONContent) {
  const attrs = node.attrs as Record<string, unknown> | undefined;
  const fileId = attrs?.fileId;
  const src = attrs?.src;

  return (
    (typeof fileId === "string" && fileId.trim().length > 0) ||
    (typeof src === "string" && /\/api\/v1\/files\/[^/]+\/content/i.test(src))
  );
}
