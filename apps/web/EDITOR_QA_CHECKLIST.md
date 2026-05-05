# Editor QA Checklist

## 1. 基础编辑

- 中文输入正常，输入法候选和上屏不丢字。
- 英文输入正常。
- 回车换行正常。
- 撤销 / 重做正常。
- 复制粘贴普通文本正常。

## 2. 块类型

- Paragraph 可以输入和恢复。
- H2 / H3 可以切换，视觉层级正确。
- Bullet list 可以输入、换行和退出。
- Ordered list 可以输入、换行和退出。
- Task list 可以输入、勾选、换行和退出。
- Blockquote 可以输入和退出。
- Code block 可以输入多行代码。

## 3. Inline Formatting

- Bold 可以添加和取消。
- Italic 可以添加和取消。
- Strike 可以添加和取消。
- Inline code 可以添加和取消。
- Highlight 可以添加和取消。
- Link 可以添加、编辑、移除。
- Clear inline formatting 不删除文本，只清除选区 mark。

## 4. 图片

- 可以通过 URL 插入图片。
- 输入非完整 URL 时会自动补全 `https://`。
- 图片不会撑破编辑区。
- 切换文档后再切回，图片仍保留在前端内存内容中。

## 5. 表格

- 可以插入默认 3x3 表格。
- 单元格可以输入内容。
- 可以添加行 / 删除行。
- 可以添加列 / 删除列。
- 可以删除表格。
- 表格不会撑破编辑区，小屏下可横向滚动。

## 6. Outline

- H2 / H3 自动生成右侧 outline。
- 修改标题后 outline 同步更新。
- 删除标题后 outline 同步更新。
- 点击 outline 可以聚焦定位到正文标题附近。

## 7. 文档状态

- 可以新建文档。
- 可以切换文档。
- 标题编辑正常，标题独立于 Tiptap 正文。
- 正文编辑正常。
- Mock 保存状态会随标题 / 正文变化更新。
- 字数和阅读时间展示随正文变化更新。

## 8. 菜单

- BubbleMenu 在选中文本或链接附近显示，命令正常。
- FloatingMenu 不会在普通段落按 Enter 产生的新空段落中自动弹出。
- FloatingMenu 在文档完全为空时显示自然，不干扰直接输入。
- FloatingMenu 可以通过空段落输入 `/` 触发，点击菜单项后不会残留 `/`。
- FloatingMenu 不在表格、代码块、列表、任务列表中显示。
- Link Popover 可以保存、取消、移除链接。
- Image Popover 可以插入和取消。
- Toolbar active / disabled 状态正常。

## 9. 边界场景

- 在表格中输入正常。
- 在代码块中输入正常。
- 在列表中输入正常。
- 在任务列表中输入和勾选正常。
- 切换文档后 undo / redo 不串到其他文档。
- 空文档 placeholder 正常。
- 插入表格 / 图片后空文档提示不误显示。

## 10. DragHandle

- Paragraph 附近显示轻量拖拽把手，并可基础拖拽。
- 鼠标从正文 block 平滑移向 DragHandle 时，handle 不闪烁、不提前消失。
- DragHandle 自身 hover 时保持可见，并能稳定点击。
- Heading 附近显示轻量拖拽把手，并可基础拖拽。
- Blockquote 附近显示轻量拖拽把手，并可基础拖拽。
- Code block 附近显示轻量拖拽把手，并可基础拖拽。
- List / task list 附近只验证官方默认拖拽行为是否可接受，不要求 listItem 精细拖拽。
- Image 附近只验证官方默认拖拽行为是否可接受，不测试 resize、caption 或选中态。
- Table 附近只验证官方默认拖拽行为是否可接受，不测试行列拖拽。
- 点击 DragHandle 可以打开轻量 Block Handle Menu 骨架。
- Block Handle Menu 只展示当前块类型和 disabled 占位操作，不执行删除、复制、转换或插入。
- 拖拽时 Block Handle Menu 不应打开，已打开时拖拽会关闭。
- 点击编辑器其他区域或按 Escape 可以关闭 Block Handle Menu。
- BubbleMenu 仍正常显示和执行命令。
- FloatingMenu 仍正常显示和执行命令。
- Link Popover 仍能保存、取消、移除链接。
- Image 插入仍正常。
- Table 插入和基础行列操作仍正常。
- Outline 仍能随拖拽后的 heading 顺序更新。
- 切换文档后 DragHandle 仍可用，undo / redo 不串到其他文档。
- 暂不处理嵌套列表、表格内部、图片编辑等高级拖拽场景。

## 11. LocalStorage Persistence

- 编辑标题后刷新页面，标题仍保留。
- 编辑正文后刷新页面，正文仍保留。
- 新建文档后刷新页面，新文档仍保留。
- 切换到某个文档后刷新页面，仍打开该文档。
- 插入图片后刷新页面，图片仍保留。
- 插入表格后刷新页面，表格仍保留。
- 添加链接 / 高亮 / 对齐后刷新页面，格式仍保留。
- localStorage 数据损坏或无法解析时，页面 fallback 到 mock 初始数据且不崩溃。

## 12. JSON Import / Export

- 可以导出 JSON 文件。
- 导出的 JSON 包含 `documents`、`activeDocumentId`、`exportVersion`、`exportedAt`。
- 修改文档后导出，文件内容包含最新标题和 content。
- 导入刚导出的 JSON 后，文档恢复正常。
- 导入 `activeDocumentId` 无效的 JSON 时，自动选择第一篇文档。
- 导入非法 JSON 时，页面不崩溃，并显示轻量错误提示。
- 导入成功后刷新页面，数据仍保留。
- 导入包含图片、表格、链接、高亮、对齐的文档后，内容仍保留。

## 13. Input Experience Extensions

- List Keymap 保持启用：bullet list / ordered list 的 Enter、Backspace、Delete、Tab、Shift+Tab 行为正常。
- Task list 的 Enter、Backspace、Delete、Shift+Tab 行为正常；不额外开启嵌套 task item 优化。
- Gapcursor 保持启用：image、table、code block、blockquote 前后可以自然定位光标。
- Trailing Node 保持启用：文档末尾是 image / table / code block / blockquote 时，末尾仍有 paragraph 可继续输入。
- DragHandle 拖拽后，光标和后续输入行为自然。
- 这些能力来自 Tiptap 官方 StarterKit / extensions，不使用自定义 ProseMirror 插件。

## 14. Editor Experience Extensions

- Dropcursor uses the official StarterKit dropcursor configuration; dragging paragraph, heading, blockquote, and codeBlock shows a subtle drop line.
- Dragging with Dropcursor visible must not show BubbleMenu, FloatingMenu, or BlockMenu residue.
- Dragging a heading still updates Outline after drop.
- Focus uses the official Focus extension with `knowledge-focused-block`; paragraph and heading show only a subtle current-block cue.
- Focus styling stays light in blockquote and codeBlock, and does not add heavy styling to table/list/task list contexts.
- CharacterCount uses the official CharacterCount extension; the right-side document count updates after typing, deleting, switching documents, and importing JSON.
- CharacterCount is not persisted separately; it is derived from current editor content.
- CodeBlockLowlight uses the official CodeBlockLowlight extension; existing codeBlock JSON content still renders and remains editable.
- CodeBlockLowlight highlights registered languages: plaintext, javascript/js/jsx, typescript/ts/tsx, json, bash/sh/shell/zsh, sql, csharp/cs, css, html/xml.
- FloatingMenu codeBlock insertion, BlockMenu conversion to codeBlock, Clear Formatting, undo/redo, and Chinese input still work after CodeBlockLowlight is enabled.
- CodeBlockLowlight keeps StarterKit `codeBlock: false`, registers once, and uses only the necessary lowlight languages.
- CodeBlockLowlight uses official tab indentation; pressing Tab inside codeBlock should keep focus in the editor and insert indentation.
- CodeBlockLowlight multiline paste should preserve line breaks and visible indentation.
- CodeBlockLowlight paragraph / heading / blockquote conversion to codeBlock should not drop text.
- Clear Formatting inside CodeBlockLowlight should preserve the codeBlock and only clear inline marks.
- CodeBlockLowlight content should persist through localStorage refresh and JSON import / export.
- CodeBlockLowlight should remain compatible with DragHandle, BlockMenu, FloatingMenu, CharacterCount, Focus, and Details.

## 15. Paste Experience QA

- Web HTML paste: headings, paragraphs, and links should preserve basic structure and update Outline / CharacterCount.
- Plain Markdown paste is intentionally treated as plain text; do not expect Markdown parsing without a dedicated official Markdown flow.
- IDE code paste as plain text should preserve line breaks and visible indentation reasonably, but should not auto-convert to codeBlock without paste handling.
- HTML table paste should use official table parsing when possible; poor source HTML compatibility is recorded rather than fixed with custom handlers.
- Rich text paste from Word / Feishu / Yuque-like sources should preserve safe basic marks and blocks, while unsupported inline styles may be dropped.
- Pasting should not leave BubbleMenu or FloatingMenu stuck open.

## 16. Details Blocks

- FloatingMenu can insert an official Details block.
- Details summary can be edited.
- Details content can be expanded, collapsed, and edited.
- Chinese input works in Details summary and content.
- Details content accepts basic child content such as list, link, and highlight.
- Users can continue typing after a Details block.
- Document switching preserves Details content in memory.
- Refreshing after localStorage save preserves Details content.
- JSON export / import preserves Details content.
- DragHandle, BubbleMenu, FloatingMenu, Outline, CharacterCount, undo/redo, and CodeBlockLowlight still work after Details is enabled.

## 17. Keyboard Shortcut Experience QA

- Ctrl/Cmd+B toggles bold on the current selection without changing block structure.
- Ctrl/Cmd+I toggles italic on the current selection without changing block structure.
- Ctrl/Cmd+Z undoes the latest edit in the current document.
- Ctrl+Y and Ctrl/Cmd+Shift+Z redo the latest undone edit where the platform/browser supports them.
- Paragraph Enter creates a new paragraph.
- Shift+Enter inserts a soft line break.
- Bullet list and ordered list Enter create a new list item; Enter on an empty item exits the list.
- Bullet list and ordered list Tab / Shift+Tab indent and outdent through the official ListKeymap behavior.
- Task list Enter creates a new task item and can exit from an empty task item.
- Task list Tab / Shift+Tab should not corrupt the task list structure; nested task behavior remains official-default only.
- Blockquote Enter behavior is acceptable if content remains editable and undo/redo is stable.
- CodeBlockLowlight Enter stays inside the code block.
- CodeBlockLowlight Tab inserts indentation and keeps focus in the editor through official `enableTabIndentation`.
- Table cell Enter / Tab should keep the table structure intact; advanced spreadsheet-like behavior is not required.
- Table cell Enter may create multiple paragraphs in one cell; cell paragraph spacing should remain compact and readable.
- Plain paragraph Tab and Shift+Tab should not move focus to toolbar/sidebar controls when no official editor keymap consumes the event.
- Typing `/` only opens FloatingMenu in an empty paragraph.
- Normal paragraph Enter must not open FloatingMenu.
- Dragging a block must keep BubbleMenu and FloatingMenu hidden.
- Escape closes BlockMenu.
- Escape closes Link Popover without changing document content.
- Escape closes Image Popover without inserting an image.
- Escape should not leave the editor in a broken focus state.
- Chinese IME composition and committed Chinese text should not be intercepted by menu shortcut logic.
- Undo/redo should remain usable after edits in Link, Image, Table, CodeBlock, and Details content.
- Document switching should not leave stale shortcut/menu state behind.
- localStorage persistence and JSON import/export should remain unaffected by keyboard shortcut behavior.
- No custom ProseMirror keymap, Plugin, Decoration, NodeView, or schema change is required for this QA pass.

## 18. Block Handle Exclusive Mode

- Hovering a block shows the lightweight `+` and drag handle controls.
- Clicking the drag handle opens BlockMenu and enters an exclusive block UI state.
- While BlockMenu is open, BubbleMenu, FloatingMenu, Link Popover, and Image Popover should be hidden or closed.
- While BlockMenu is open, moving over other document text should not switch the active block handle target.
- While BlockMenu is open, hovering another block should not visually move or auto-show that other block's DragHandle.
- Clicking outside BlockMenu should close only the menu and should not pass the pointer event through to the editor content underneath.
- Escape closes BlockMenu.
- Starting a drag from the drag handle closes BlockMenu before drag interaction continues.
- Clicking `+` does not open BlockMenu.
- Clicking `+` inserts a new paragraph below the current block, focuses it, and writes `/` so the existing FloatingMenu slash path can appear.
- The `+` flow should not implement a separate slash-command system or a current-block conversion menu.
- Paragraph, heading, blockquote, codeBlock, image, table, and details should use the same insert-below intent where the official DragHandle target allows a safe top-level insert position.
- BlockMenu remains limited to existing safe conversion and disabled placeholder actions; no delete, copy, insert, or AI command is enabled.

## 18. Empty Document And Table Cell Polish QA

- 新建文档后，空文档提示不拥挤，placeholder 是主要写作提示。
- 新建文档后可以直接输入文字，输入不会被 FloatingMenu 或 Toolbar 打断。
- 在空段落输入 `/` 后 FloatingMenu 出现。
- 普通段落按 Enter 后，FloatingMenu 不应自动弹出。
- FloatingMenu 视觉保持轻量，不遮挡 Toolbar，不抢正文注意力。
- 中文输入和 IME 上屏不受空文档提示影响。
- Table cell 内按 Enter 仍创建新的 paragraph。
- Table cell 内多段内容间距自然，不像正文大段落块间距。
- Table cell 内空段落不显示额外 placeholder 干扰。
- 表格添加行 / 删除行 / 添加列 / 删除列 / 删除表格仍正常。
- Clear Formatting 行为边界待方案确认后再改命令逻辑。
- codeBlock 取消 / 转换策略待方案确认后再改命令逻辑。
- table 删除危险操作待方案确认后再改交互逻辑。

## 19. Table Danger Buttons And CodeBlock Conversion QA

- 表格内光标激活时，表格操作组仍正常出现。
- 添加行 / 添加列保持普通按钮样式和原有行为。
- 删除行 / 删除列 / 删除表格的 title 和 aria-label 清楚。
- 删除行 / 删除列 / 删除表格使用轻量危险样式，但不出现确认弹窗。
- 点击删除行 / 删除列 / 删除表格的行为与之前一致。
- 删除表格相关操作后，undo 可以恢复。
- codeBlock 打开 BlockMenu 后，转换为段落 / 二级标题 / 三级标题 / 引用不可用。
- codeBlock 打开 BlockMenu 后，有轻量说明提示代码块暂不支持转换为普通文本。
- paragraph / heading / blockquote 仍可通过 BlockMenu 转换为 codeBlock。
- table / image / list / taskList / details 等复杂节点继续禁用 BlockMenu 转换。
- codeBlock 拖拽仍正常，Outline、BubbleMenu、FloatingMenu 与 DragHandle 互斥逻辑不受影响。

## 20. Clear Formatting Safe Behavior QA

- paragraph：清除 bold / italic / link / highlight 后仍是 paragraph，并恢复左对齐。
- heading：清除格式后变成 paragraph，文本保留，并恢复左对齐。
- blockquote：清除 marks 后仍是 quote，不拆 quote。
- codeBlock：清除后仍是 codeBlock，换行和缩进保留。
- bullet list / ordered list：清除后列表结构和层级保留。
- taskList：清除后 task list 结构和 checkbox 状态保留。
- table：清除后 table / row / cell 不被拆，行列不被删除。
- details：清除后 details / summary / content 结构保留，仍可展开 / 收起。
- image：清除格式 no-op，不删除图片，不修改图片。
- Clear Formatting 不调用 `clearNodes()` 破坏复杂块结构。
- Clear Formatting 操作后 undo / redo 正常。
- Clear Formatting 操作前后中文输入不受影响。

## 21. Notion-like Block Controls QA

## 22. Slash FloatingMenu And Focus Visual QA

- Typing `/` in an empty paragraph shows the FloatingMenu.
- The slash FloatingMenu should read as a lightweight block insert menu, not a dense toolbar.
- FloatingMenu items have stable height, natural icon/text spacing, and aligned Chinese labels.
- FloatingMenu hover and active states stay subtle and avoid saturated blue fills.
- Clicking `+` inserts a slash paragraph and the existing FloatingMenu appears normally.
- FloatingMenu should not cover the top toolbar.
- FloatingMenu and BlockMenu should not appear at the same time.
- Paragraph focus should not look like an input box.
- Multiline paragraph focus should not create a strong rounded rectangle around the text.
- Heading focus should not weaken or distort heading hierarchy.
- Blockquote focus should not create a second strong left line.
- CodeBlock focus should not reduce code readability or fight the code block border.
- Table focus should not alter table borders.
- Details focus should not compete with the details container border.
- List and taskList focus should not affect indentation or checkbox alignment.
- The document body should not show a full-height vertical focus line.

## 23. Table Inline Controls v1 QA

- Focusing inside a table shows one right-side `+` control and one bottom `+` control.
- Moving the selection outside the table hides the inline table controls.
- With multiple tables, only the active selection table shows inline controls.
- Clicking the right-side `+` runs the official add-column-after behavior.
- Clicking the bottom `+` runs the official add-row-after behavior.
- Undo can revert an added column or added row.
- Deleting the table hides the inline controls with no stale floating controls left behind.
- Scrolling or resizing keeps controls aligned with the active table well enough for v1.
- BlockMenu exclusive mode hides table inline controls.
- DragHandle block dragging hides table inline controls.
- BubbleMenu and FloatingMenu should not conflict with table inline controls.
- Chinese input inside table cells remains normal.
- Table Enter and Tab keep official Tiptap table behavior.
- Toolbar table insert and row/column/delete actions remain available as fallback controls.

- 普通正文左侧没有整页贯穿式竖线。
- blockquote 左侧线仅作用于 quote 本身。
- Focus 当前块提示不形成整页竖线。
- hover 到可操作 block 附近时出现 `+` 和 `::`。
- 鼠标从正文移向 `+` / `::` 时控件不闪烁消失。
- `+` 可以点击，且只打开 InsertMenu。
- `::` 可以点击，且打开 BlockMenu。
- `::` 仍可拖拽当前块，拖拽能力来自官方 DragHandleReact。
- 控件不遮挡正文文本，不影响正文点击和文本选择。
- 点击 `+` 打开 InsertMenu。
- 点击外部关闭 InsertMenu。
- 按 Escape 关闭 InsertMenu。
- paragraph 下方可插入 paragraph。
- paragraph 下方可插入 H2。
- heading 下方可插入 H3。
- blockquote 下方可插入 paragraph。
- codeBlock 下方可插入 paragraph。
- 插入后光标进入新插入块。
- 插入 H2 / H3 后 Outline 更新。
- 插入操作可以通过 undo 撤销。
- table / image / list / taskList / details 等复杂节点上插入 disabled。
- 点击 `::` 打开 BlockMenu。
- BlockMenu 不显示 pos 或 node.type.name 调试信息。
- BlockMenu 当前块类型使用中文显示。
- BlockMenu 菜单分组清楚。
- paragraph / heading / blockquote 安全转换正常。
- paragraph / heading / blockquote 可转换为 codeBlock。
- codeBlock 转换为 paragraph / heading / quote disabled。
- table / image / list / taskList / details 等复杂节点转换 disabled。
- 复制节点 / 复制到剪贴板 / 复制块链接 / 询问 AI / 删除均为 disabled 占位，不执行真实功能。
- BlockMenu / InsertMenu 打开后 BubbleMenu 不出现。
- BlockMenu / InsertMenu 打开后 FloatingMenu 不出现。
- 点击正文关闭菜单。
- 点击 Sidebar / Topbar / RightPanel 关闭菜单。
- 按 Escape 关闭菜单。
- 拖拽 `::` 时菜单关闭且不残留。
- 菜单关闭后编辑器恢复正常输入、选中、拖拽。

## 24. Table Inline Controls v2 QA

- Hovering inside a table shows the top column handle and left row handle without first clicking the table.
- Clicking inside a table cell no longer shows the old toolbar / navbar table operation group.
- The toolbar no longer shows the old contextual table operation group; inline controls are the table row / column operation entry.
- Hover tracking does not prevent normal `td` / `th` clicks from moving selection into the table.
- Clicking row handle, column handle, or table menu buttons does not drop the table selection unexpectedly.
- Moving the mouse between cells updates the row handle and column handle to the hovered row / column.
- Moving the mouse outside the table hides controls when RowMenu / ColumnMenu is closed.
- With RowMenu / ColumnMenu open, briefly leaving the table does not flicker or lose the menu target.
- The row handle is narrow, single-layer, and no longer overlaps the block DragHandle.
- The block DragHandle stays on the same unified left track whether table controls are visible or hidden, and still drags normal blocks.
- The top column handle does not overlap the previous block.
- The bottom add-row control does not overlap the next block.
- The bottom add-row `+` stays close to the table bottom edge, roughly 6px to 10px below it.
- The table wrapper reserves enough bottom space for the bottom add-row control.
- Moving the mouse from the table bottom edge to the bottom add-row `+` does not make the control disappear.
- The bottom add-row control has a transparent hover bridge between the table and the `+`.
- The bottom add-row `+` does not cover paragraph, heading, list, or another table below.
- Consecutive tables keep each table control near its own table without overlapping the next table.
- Table spacing is solved through table wrapper spacing, not by globally enlarging text block spacing.
- Normal paragraph, heading, and list spacing is not globally enlarged.
- Hovering the last column shows the right-side add-column `+`.
- Hovering a non-last column hides the right-side add-column `+`.
- Hovering the last row shows the bottom add-row `+`.
- Hovering a non-last row hides the bottom add-row `+`.
- RowMenu / ColumnMenu hide the right-side and bottom `+` controls while open.
- RowMenu can insert a row above with the official table command.
- RowMenu can insert a row below with the official table command.
- RowMenu can delete the current row with a light danger style.
- ColumnMenu can insert a column left with the official table command.
- ColumnMenu can insert a column right with the official table command.
- ColumnMenu can delete the current column with a light danger style.
- Undo can recover inserted or deleted rows and columns.
- Multiple tables only operate on the current hovered table or the table owning the open menu.
- Open RowMenu, move the mouse to another table, then run add row after; the command still targets the menu-owned table.
- Open ColumnMenu, move the mouse to another table, then run delete column; the command still targets the menu-owned table.
- Closing RowMenu / ColumnMenu and then hovering another table leaves no stale locked table state.
- Clicking outside RowMenu / ColumnMenu closes the menu without passing the click to the table.
- Escape closes RowMenu / ColumnMenu.
- RowMenu / ColumnMenu hide BubbleMenu, FloatingMenu, and Image Popover while open.
- Text selection or multi-cell selection should not incorrectly show hover controls.
- Scrolling or resizing keeps controls aligned, or safely recalculates / hides stale controls.
- If a saved cell position becomes invalid after document changes, row / column commands are skipped and menus close safely.
- deleteRow / deleteColumn closes the menu and recalculates or hides controls safely.
- Deleting the last row / column follows official table command behavior and must not break the editor.
- Deleting the table removes controls and menus with no stale UI.
- Switching documents removes controls and menus with no stale UI.

## 25. Overlay Priority QA

- Normal block hover shows only the DragHandle controls; BubbleMenu and FloatingMenu do not appear accidentally.
- Selecting non-empty text shows BubbleMenu and suppresses FloatingMenu and DragHandle interference.
- An empty paragraph or slash paragraph can show FloatingMenu, while BubbleMenu remains hidden.
- Hovering a table shows TableInlineControls, and DragHandle does not overlap the row control.
- Opening RowMenu or ColumnMenu hides BubbleMenu, FloatingMenu, ImagePopover, DragHandle, and the table add-row/add-column quick controls.
- Selecting text inside a table can show BubbleMenu when RowMenu / ColumnMenu is closed.
- Selecting text inside a table must not show BubbleMenu while RowMenu / ColumnMenu is open.
- Opening ImagePopover hides BubbleMenu, FloatingMenu, and DragHandle.
- With ImagePopover open, hovering another block must not make DragHandle take over the layer.
- Closing a high-priority overlay restores normal DragHandle, BubbleMenu, and FloatingMenu behavior.
- Reserved SlashMenu and CommentPopover overlay states have clear priority slots and do not affect current behavior while inactive.
- After all overlays are closed, typing, selection, block dragging, table controls, undo, and redo remain normal.
- Overlay priority is controlled through editor state / root classes, not by high z-index forcing that leaves dead clickable regions.

## 26. ImageBlock QA

- Toolbar can insert an empty `imageBlock`.
- FloatingMenu slash flow can insert an empty `imageBlock`.
- Slash-triggered FloatingMenu image command inserts `imageBlock`, not the legacy `image` node.
- Empty `imageBlock` dropzone matches the Northstar light editor style.
- Clicking the dropzone selects a single image.
- Dragging one supported image file onto the dropzone inserts it.
- Selecting 2-3 supported image files inserts multiple `imageBlock` nodes in order.
- Selecting more than 3 files processes only the first 3 and shows a clear lightweight message.
- Selecting a file larger than 5MB shows a clear lightweight message and does not crash.
- Selecting a non-image file shows a clear lightweight message and does not crash.
- Inserted images default to `width: 85`.
- Clicking an inserted image shows a light selected state.
- Left and right resize handles are visible only while the image block is selected.
- Resizing keeps `width` between 30 and 100.
- Backspace / Delete removes a selected `imageBlock`.
- `imageBlock` controls do not overlap or fight the DragHandle.
- `imageBlock` selection does not trigger FloatingMenu or BubbleMenu.
- TableMenu open state still blocks image-related overlays and handles through the existing priority model.
- ImageUrlPopover still works and inserts URL images as `imageBlock`.
- Document JSON keeps `src`, `width`, `align`, `alt`, and `title` in attrs; current local base64 preview can persist only within storage limits and should be replaced by a stable upload URL in production.

## 27. Image Resize QA

- Clicking an inserted image shows left and right resize handles.
- Dragging the right handle to the right makes the image wider.
- Dragging the right handle to the left makes the image narrower.
- Dragging the left handle to the left makes the image wider.
- Dragging the left handle to the right makes the image narrower.
- Image `width` is clamped between 30 and 100.
- Mouseup persists the final `width` to `node.attrs.width`.
- Resizing does not modify `src`, `alt`, `title`, or `align`.
- Resizing does not create text selection.
- DragHandle stays hidden and does not capture events while resizing.
- BubbleMenu and FloatingMenu do not show while resizing.
- Normal DragHandle, BubbleMenu, and FloatingMenu behavior returns after resize ends.
- Repeated resize interactions do not throw errors.
- Deleting an image after resize leaves no stale `mousemove` or `mouseup` listeners.
- `npm run build` passes after resize changes.

## 28. ImageBlock Visual And Default Size QA

- Hovering an image does not show the browser native `title` tooltip.
- `title` attr can remain in document data, but is not rendered as an `img title` attribute.
- Toolbar inserts an empty `imageBlock` with default `width: 85` and `align: center`.
- FloatingMenu inserts an empty `imageBlock` with default `width: 85` and `align: center`.
- Slash-triggered image insertion uses default `width: 85` and `align: center`.
- ImageUrlPopover inserts URL images with default `width: 85` and `align: center`.
- Clicking the dropzone to upload one image displays it at default `width: 85`.
- Dragging one image onto the dropzone displays it at default `width: 85`.
- Uploading 2-3 images creates image blocks where each new image defaults to `width: 85`.
- Existing images with a `width` attr are not overwritten.
- Legacy image blocks without a `width` attr render with fallback `width: 85`.
- Image selected state is clear but visually restrained.
- Unselected images do not show an obvious blue border.
- Resize handles still drag normally.
- Resize still persists the final `width` attr.
- DragHandle, BubbleMenu, and FloatingMenu remain suppressed while resizing.
- `npm run build` passes.

## 30. BlockMenu / DragHandle Lock QA

- Hovering a normal paragraph shows DragHandle.
- Clicking a paragraph DragHandle opens BlockMenu.
- With BlockMenu open, moving the pointer to another paragraph does not show another paragraph DragHandle.
- With BlockMenu open, moving the pointer near headings, lists, images, and tables does not show another DragHandle.
- With BlockMenu open, the menu does not follow pointer movement to another block.
- Clicking a menu item closes BlockMenu and restores normal DragHandle hover.
- Clicking outside closes BlockMenu and restores normal DragHandle hover.
- Pressing Escape closes BlockMenu and restores normal DragHandle hover.
- Opening TableMenu, ImagePopover, or SlashMenu closes BlockMenu.
- After close, the editor root has no block menu lock class residue.
- No `removeChild` NotFoundError occurs.
- `npm run build` passes.

## 29. Overlay CodeBlock And Table Hover QA

- Cursor inside a codeBlock does not show the normal BubbleMenu.
- Selecting text inside a codeBlock does not show the normal BubbleMenu.
- Empty lines inside a codeBlock do not show the FloatingMenu.
- Hovering a normal paragraph shows the DragHandle on the unified left-side track.
- Hovering a heading, quote, or codeBlock shows the DragHandle at the same x position as a paragraph.
- Selecting normal paragraph text outside a codeBlock still shows BubbleMenu.
- Selecting normal text inside a table still shows BubbleMenu unless RowMenu / ColumnMenu is open.
- Hovering a table shows TableControls.
- Hovering a table shows the DragHandle at the same x position as normal blocks.
- The table row control does not overlap the unified DragHandle track.
- Moving from a table into the row or column control keeps the DragHandle visually stable without blocking the control.
- TableControls appearing or disappearing does not move the DragHandle horizontally.
- Moving the pointer from body content to the DragHandle does not hide the DragHandle early.
- Moving from a table to the row or column control does not flicker TableControls.
- Moving from a table to the bottom add-row control does not flicker TableControls.
- Moving from a table to the right add-column control does not flicker TableControls.
- RowMenu / ColumnMenu open state keeps DragHandle hidden and non-interactive.
- DragHandle and TableControls do not overlap or compete for the same area.
- CodeBlock BubbleMenu and FloatingMenu exclusions remain active.
- No `removeChild` NotFoundError occurs during overlay transitions.
- `npm run build` passes.

## 31. Strict Overlay Stability Regression QA

- Typing `/` in an empty paragraph opens the slash-triggered FloatingMenu and adds the `editor-slash-menu-open` root state only while that slash menu is visible.
- A plain empty-document FloatingMenu does not mark the editor as slash-menu open.
- Slash-triggered FloatingMenu visibility suppresses BubbleMenu and DragHandle, then releases both after command execution, Escape/click-away hide, blur, or text deletion.
- Table hover controls clear on editor blur and do not leave stale row/column geometry, hover bridge state, or menu-open root state.
- RowMenu / ColumnMenu still target the locked table after moving the pointer to another table.
- ImageBlock upload promises resolving after node unmount do not call React state setters or update stale node attrs.
- Invalid or missing ImageBlock `width` attrs fall back to `85`, while resize remains clamped to `30` through `100`.
- ImageBlock image CSS does not inherit generic editor image margins or broad `img` styling.
- ImageUrlPopover unmount clears its deferred focus timer.
- `npx tsc --noEmit` and `npm run build` pass after the stability fixes.
