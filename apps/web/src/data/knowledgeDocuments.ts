import type { JSONContent } from "@tiptap/react";
import type { KnowledgeDocument, KnowledgeFolder } from "../types/editor";
import { migrateDocumentContentBlockIds } from "../extensions/BlockIdentity";

const ourPrinciplesContent: JSONContent = {
  type: "doc",
  content: [
    {
      type: "paragraph",
      content: [
        {
          type: "text",
          text: "Our principles are the immutable anchors that guide how we think, decide, and build. They precede strategy, outlast campaigns, and shape culture.",
        },
      ],
    },
    {
      type: "blockquote",
      content: [
        {
          type: "paragraph",
          content: [{ type: "text", text: "Northstar Note" }],
        },
        {
          type: "paragraph",
          content: [
            {
              type: "text",
              text: "These principles are not goals. They are enduring truths that help us navigate complexity and stay aligned when the path is not clear.",
            },
          ],
        },
      ],
    },
    {
      type: "heading",
      attrs: { level: 2 },
      content: [{ type: "text", text: "Clarity over Cleverness" }],
    },
    {
      type: "paragraph",
      content: [
        {
          type: "text",
          text: "We choose clarity in our communication, our designs, and our decisions. Simple is not simplistic. Clarity respects people's time and attention.",
        },
      ],
    },
    {
      type: "heading",
      attrs: { level: 2 },
      content: [{ type: "text", text: "Long-Term Thinking" }],
    },
    {
      type: "paragraph",
      content: [
        {
          type: "text",
          text: "We build with durability in mind. We consider the downstream consequences of today's decisions on our users, our team, and our world.",
        },
      ],
    },
    {
      type: "heading",
      attrs: { level: 2 },
      content: [{ type: "text", text: "Stewardship" }],
    },
    {
      type: "paragraph",
      content: [
        {
          type: "text",
          text: "We are responsible custodians of our resources, our data, and our influence. We act with care, protect what matters, and leave things better than we found them.",
        },
      ],
    },
    {
      type: "heading",
      attrs: { level: 2 },
      content: [{ type: "text", text: "Collaboration" }],
    },
    {
      type: "paragraph",
      content: [
        {
          type: "text",
          text: "The strongest route is rarely drawn by one person. We surface context early, invite critique, and make decisions visible.",
        },
      ],
    },
    {
      type: "heading",
      attrs: { level: 2 },
      content: [{ type: "text", text: "Continuous Learning" }],
    },
    {
      type: "paragraph",
      content: [
        {
          type: "text",
          text: "Every expedition should improve the atlas. We record what changed, what surprised us, and what future teams should know.",
        },
      ],
    },
  ],
};

export const knowledgeFolders: KnowledgeFolder[] = [
  { id: "orientation", title: "00. Orientation" },
  { id: "product", title: "01. Foundations" },
  { id: "research", title: "02. Strategy" },
  { id: "workstreams", title: "03. Workstreams" },
  { id: "guides", title: "04. Guides & Playbooks" },
  { id: "reference", title: "05. Reference" },
  { id: "archive", title: "06. Archives" },
];

export const initialKnowledgeDocuments: KnowledgeDocument[] = [
  {
    id: "doc-editor-experience",
    title: "Our Principles",
    folderId: "product",
    updatedAt: "2024-05-14T10:24:00.000Z",
    tags: ["Foundations", "Principles", "Atlas"],
    content: cloneContent(ourPrinciplesContent),
  },
  {
    id: "doc-writing-flow",
    title: "Mission & Vision",
    folderId: "product",
    updatedAt: "2024-04-28T11:10:00.000Z",
    tags: ["Mission", "Strategy"],
    content: {
      type: "doc",
      content: [
        {
          type: "paragraph",
          content: [
            {
              type: "text",
              text: "The mission explains why the Atlas Library exists and how it helps teams navigate decisions with shared context.",
            },
          ],
        },
        {
          type: "heading",
          attrs: { level: 2 },
          content: [{ type: "text", text: "Library Promise" }],
        },
        {
          type: "bulletList",
          content: [
            {
              type: "listItem",
              content: [{ type: "paragraph", content: [{ type: "text", text: "Make durable knowledge easy to find." }] }],
            },
            {
              type: "listItem",
              content: [{ type: "paragraph", content: [{ type: "text", text: "Keep decisions attached to their original context." }] }],
            },
          ],
        },
      ],
    },
  },
  {
    id: "doc-block-principles",
    title: "Operating System",
    folderId: "product",
    updatedAt: "2024-04-10T14:35:00.000Z",
    tags: ["Operations", "Rituals"],
    content: {
      type: "doc",
      content: [
        {
          type: "heading",
          attrs: { level: 2 },
          content: [{ type: "text", text: "Editorial Rhythm" }],
        },
        {
          type: "paragraph",
          content: [
            {
              type: "text",
              text: "Every material document moves through draft, review, and publication without requiring a separate workflow system.",
            },
          ],
        },
      ],
    },
  },
  {
    id: "doc-glossary",
    title: "Glossary",
    folderId: "product",
    updatedAt: "2024-03-26T09:12:00.000Z",
    tags: ["Language", "Reference"],
    content: {
      type: "doc",
      content: [
        {
          type: "paragraph",
          content: [{ type: "text", text: "Shared terms keep the Atlas Library precise across teams and time zones." }],
        },
        {
          type: "heading",
          attrs: { level: 2 },
          content: [{ type: "text", text: "Core Terms" }],
        },
      ],
    },
  },
  {
    id: "doc-yuque-observation",
    title: "Decision Framework",
    folderId: "research",
    updatedAt: "2024-03-12T09:12:00.000Z",
    tags: ["Decision", "Strategy"],
    content: {
      type: "doc",
      content: [
        {
          type: "paragraph",
          content: [
            {
              type: "text",
              text: "A decision framework turns principles into repeatable evaluation criteria.",
            },
          ],
        },
        {
          type: "heading",
          attrs: { level: 2 },
          content: [{ type: "text", text: "Evaluation Points" }],
        },
        {
          type: "orderedList",
          content: [
            {
              type: "listItem",
              content: [{ type: "paragraph", content: [{ type: "text", text: "Name the tradeoff clearly." }] }],
            },
            {
              type: "listItem",
              content: [{ type: "paragraph", content: [{ type: "text", text: "Record the route not taken." }] }],
            },
          ],
        },
      ],
    },
  },
  {
    id: "doc-communication-guide",
    title: "Communication Guide",
    folderId: "guides",
    updatedAt: "2024-02-20T12:10:00.000Z",
    tags: ["Guide", "Writing"],
    content: {
      type: "doc",
      content: [
        {
          type: "paragraph",
          content: [{ type: "text", text: "Communication guidance keeps updates concise, traceable, and useful after the moment has passed." }],
        },
      ],
    },
  },
];

export function createEmptyDocumentContent(): JSONContent {
  return migrateDocumentContentBlockIds({
    type: "doc",
    content: [{ type: "paragraph" }],
  });
}

export function cloneContent(content: JSONContent): JSONContent {
  return JSON.parse(JSON.stringify(content)) as JSONContent;
}
