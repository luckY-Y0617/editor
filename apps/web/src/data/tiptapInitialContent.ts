import type { JSONContent } from "@tiptap/react";

export const tiptapInitialContent: JSONContent = {
  type: "doc",
  content: [
    {
      type: "paragraph",
      content: [
        {
          type: "text",
          text: "这个页面用于沉淀知识库编辑器的第一阶段体验：以写作为中心，保持足够的空间感，同时让文档树、大纲和辅助工具自然地围绕正文工作。",
        },
      ],
    },
    {
      type: "heading",
      attrs: { level: 2 },
      content: [{ type: "text", text: "设计原则" }],
    },
    {
      type: "paragraph",
      content: [
        {
          type: "text",
          text: "正文区域不应该像表单，也不应该像传统后台中的内容面板。它更接近一个可持续写作的文档画布，周围的信息密度需要服务于当前文档，而不是抢占注意力。",
        },
      ],
    },
    {
      type: "blockquote",
      content: [
        {
          type: "paragraph",
          content: [
            {
              type: "text",
              text: "好的知识库编辑器应该让结构存在，但不喧宾夺主；让工具随时可用，但默认保持安静。",
            },
          ],
        },
      ],
    },
    {
      type: "heading",
      attrs: { level: 2 },
      content: [{ type: "text", text: "基础编辑能力" }],
    },
    {
      type: "taskList",
      content: [
        {
          type: "taskItem",
          attrs: { checked: true },
          content: [
            {
              type: "paragraph",
              content: [{ type: "text", text: "完成真实 Tiptap EditorContent 接入" }],
            },
          ],
        },
        {
          type: "taskItem",
          attrs: { checked: true },
          content: [
            {
              type: "paragraph",
              content: [{ type: "text", text: "保留中间写作区域的留白和文档感" }],
            },
          ],
        },
        {
          type: "taskItem",
          attrs: { checked: false },
          content: [
            {
              type: "paragraph",
              content: [{ type: "text", text: "后续再评估官方 DragHandle、FloatingMenu、BubbleMenu" }],
            },
          ],
        },
      ],
    },
    {
      type: "bulletList",
      content: [
        {
          type: "listItem",
          content: [
            {
              type: "paragraph",
              content: [{ type: "text", text: "支持段落、标题、引用、代码块和列表" }],
            },
          ],
        },
        {
          type: "listItem",
          content: [
            {
              type: "paragraph",
              content: [{ type: "text", text: "支持加粗、斜体、删除线和行内代码" }],
            },
          ],
        },
        {
          type: "listItem",
          content: [
            {
              type: "paragraph",
              content: [{ type: "text", text: "当前不包含拖拽、slash command、AI 或协同编辑" }],
            },
          ],
        },
      ],
    },
    {
      type: "codeBlock",
      attrs: { language: "tsx" },
      content: [
        {
          type: "text",
          text: "const editorStage = {\n  editor: 'Tiptap EditorContent',\n  features: ['basic marks', 'lists', 'tasks', 'blockquote', 'codeBlock'],\n  customProseMirrorPlugin: false,\n};",
        },
      ],
    },
  ],
};
