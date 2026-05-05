# AI Editor Guardrails

本项目是一个基于 Tiptap 的知识库编辑器，目标是逐步实现接近 Notion / 语雀 / AFFiNE 的现代块编辑体验。

重要说明：

- 最终体验目标不是一个简单富文本编辑器。
- 最终形态应当接近成熟知识库产品：块级编辑、块级拖拽、块操作菜单、快捷插入、稳定大纲、图片、表格、代码块、折叠块、后端保存、后续可扩展 AI / 评论 / 版本历史。
- 但是，实现路线必须分阶段推进。
- 任何阶段都不允许为了“像 Notion”而直接自研复杂 ProseMirror 底层。
- 复杂能力必须先做方案评审，再做最小实现，再做 QA。

---

## 1. 总体开发原则

### 1.1 优先级原则

开发优先级如下：

1. Tiptap 官方扩展
2. Tiptap 官方 command
3. Tiptap 官方 React 组件
4. 普通 React state / props
5. CSS / Tailwind 样式
6. 轻量工具函数
7. Tiptap extension 的轻量扩展
8. ProseMirror 底层能力

ProseMirror 底层能力只能作为最后手段。

---

### 1.2 默认禁止事项

除非用户明确批准，否则不要使用以下能力：

- 自定义 ProseMirror Plugin
- Decoration / DecorationSet
- 自定义 NodeView
- ReactNodeViewRenderer
- NodeViewContent
- 手写 transaction
- 手写 drag / drop 逻辑
- 自定义 nodeRange 计算
- 自定义 schema 结构
- 包裹每个 block DOM
- DOM query 定位 block
- mousemove 全局监听实现 block hover
- 自定义 listItem 独立拖拽
- 表格内部拖拽
- 图片 resize / caption 的自研实现
- 完整 Notion block tree
- 自研协同编辑
- 自研复杂粘贴清洗系统

如果某个功能必须使用上述能力，必须先停止写代码，只输出方案和风险评估。

---

### 1.3 每轮开发前必须回答

每次写代码前，先回答：

1. 本轮只解决什么问题？
2. 是否属于当前阶段目标？
3. 是否可以使用 Tiptap 官方扩展 / command / React 组件完成？
4. 是否需要新增依赖？
5. 是否会改 schema？
6. 是否会改变 JSONContent 结构？
7. 是否会影响已有文档兼容性？
8. 是否会影响中文输入？
9. 是否会影响 undo / redo？
10. 是否会影响 selection？
11. 是否会影响 list / taskList / table / codeBlock？
12. 是否会影响 DragHandle / BubbleMenu / FloatingMenu / BlockMenu？
13. 是否需要自定义 ProseMirror Plugin / Decoration / NodeView？
14. 是否有更小的实现方案？
15. 如果失败，如何回滚？

没有完成上述判断，不要直接写代码。

---

### 1.4 每轮开发后必须输出

每轮完成后必须输出：

1. 修改了哪些文件
2. 新增了哪些依赖
3. 是否新增 Tiptap 扩展
4. 是否改 schema
5. 是否改变 JSONContent 结构
6. 是否使用自定义 ProseMirror Plugin / Decoration / NodeView
7. 是否手写 transaction
8. 是否影响 DragHandle / BubbleMenu / FloatingMenu / BlockMenu
9. 是否影响中文输入、selection、undo / redo
10. 如何验证本轮功能
11. 哪些边界暂不处理
12. npm run build 是否通过

---

## 2. 项目最终体验目标

本项目最终希望实现的编辑器体验：

### 2.1 写作体验

- 打开页面后，中间编辑区是核心
- 标题、meta、toolbar、正文排版稳定
- 用户可以自然输入、换行、粘贴、插入块
- 普通写作时工具不打扰
- 需要操作时，BubbleMenu / FloatingMenu / BlockMenu 可以出现
- 空文档体验自然
- 长文档阅读和编辑不压迫

---

### 2.2 块级体验

最终目标接近 Notion / 语雀：

- 每个主要 block 有轻量 DragHandle
- 支持基础块拖拽
- 支持块操作菜单
- 支持块类型转换
- 支持在当前块下方插入基础块
- 后续可扩展块删除、复制
- 后续可评估块级缩进
- 后续可评估块级唯一 ID
- 后续可评估块级评论和引用

但是：

- 不直接做完整 Notion block tree
- 不直接做复杂父子块结构
- 不直接做 listItem 独立精细拖拽
- 不直接做表格内部拖拽
- 不直接做复杂 drop indicator
- 不直接做完整 slash command

---

### 2.3 知识库体验

- 左侧文档树清晰
- 中间编辑区稳定
- 右侧 Outline / Document Info / AI Assistant 作为辅助
- 文档可以新建、切换、本地保存
- 后续接后端保存 JSONContent
- 后续支持图片上传、版本历史、评论、AI，但必须独立分阶段实现

---

## 3. 阶段规划与验收标准

---

# Phase 0：项目护栏与基线

## 目标

建立开发约束，防止 AI 在编辑器里随意引入复杂底层逻辑。

## 允许做

- 新增 AI_EDITOR_GUARDRAILS.md
- 新增 QA checklist
- 整理已有功能清单
- 标注高风险模块

## 禁止做

- 新增功能
- 修改 schema
- 修改 Tiptap 扩展
- 重构编辑器

## 验收标准

- 项目根目录存在 AI_EDITOR_GUARDRAILS.md
- 有清晰阶段目标
- 有每轮开发前检查项
- 有每轮开发后输出规范
- Codex 后续任务必须遵守该文件

---

# Phase 1：页面 Shell 与知识库布局

## 目标

实现现代知识库编辑器的基础页面结构。

目标布局：

- 左侧 Sidebar 全高固定
- 右侧 Workspace 包含顶部 Navbar
- Navbar 下方分为 Main Editor + Right Panel
- 中间编辑区是视觉中心
- 右侧辅助栏不抢正文注意力

## 允许做

- AppShell
- Sidebar
- TopNavbar
- EditorCanvas
- RightPanel
- 基础文档树 mock
- 基础视觉风格

## 禁止做

- 接入复杂 Tiptap 功能
- DragHandle
- AI
- 后端
- 协同
- 权限
- 评论

## 验收标准

- 页面结构接近现代知识库产品
- 左侧导航清楚
- 中间编辑区主次明确
- 右侧辅助栏稳定
- 不像后台管理系统
- 不像 landing page
- npm run build 通过

---

# Phase 2：Tiptap 基础编辑能力

## 目标

接入真实 Tiptap EditorContent，替换静态 mock 正文。

## 允许做

- StarterKit
- Placeholder
- paragraph
- heading
- bullet list
- ordered list
- task list
- blockquote
- codeBlock
- bold
- italic
- strike
- inline code
- undo / redo
- 基础 toolbar

## 禁止做

- DragHandle
- slash command
- AI
- 自定义 NodeView
- 自定义 ProseMirror Plugin
- 自定义 Decoration
- 自定义 schema

## 验收标准

- 中间正文是真实 Tiptap EditorContent
- 基础输入正常
- 中文输入正常
- 列表、引用、代码块正常
- undo / redo 正常
- 没有复杂 ProseMirror 自定义逻辑

---

# Phase 3：文档标题、保存状态与本地文档模型

## 目标

让编辑器从单页 demo 变成文档编辑原型。

## 允许做

- 独立 React state 管理标题
- mock save status
- updatedAt mock
- 当前文档状态
- 文档切换
- 新建文档
- localStorage 持久化
- JSON import/export

## 禁止做

- 后端接口
- 登录
- 协同编辑
- 评论
- 版本历史
- 云端同步

## 验收标准

- 标题可编辑
- 正文可编辑
- 修改后保存状态变化
- 可以新建文档
- 可以切换文档
- 刷新后 localStorage 能恢复
- JSON 可导入导出
- 不保存 editor 实例 / selection / menu 状态

---

# Phase 4：Outline 与文档辅助信息

## 目标

右侧 Outline 和文档信息基于真实 Tiptap 内容生成。

## 允许做

- 从 heading 节点生成 Outline
- 使用 editor.state.doc.descendants 获取 heading pos
- 点击 Outline 定位
- 字数统计
- 阅读时间估算
- Document Info

## 禁止做

- 持久化 heading pos
- 给 heading 强行写入自定义 id
- URL hash
- 滚动监听高亮
- 自定义 Decoration
- 自定义 Plugin

## 验收标准

- H2 / H3 自动出现在 Outline
- 修改标题后 Outline 更新
- 删除标题后 Outline 更新
- 点击 Outline 能定位
- pos 只作为当前 editor session 临时数据
- 不改变 JSONContent 结构

---

# Phase 5：BubbleMenu / FloatingMenu / Link / Image / Table

## 目标

补齐基础现代编辑器工具体验。

## 允许做

- 官方 BubbleMenu
- 官方 FloatingMenu
- Link extension
- Link Popover
- Image URL 插入
- Table 基础能力
- Highlight
- TextAlign
- Clear Formatting

## 禁止做

- 完整 slash command
- AI command
- 图片上传
- 图片 resize
- 表格右键菜单
- 表格列宽拖拽
- 表格合并单元格
- 自定义 NodeView
- 自定义 Plugin

## 验收标准

- 选中文本时 BubbleMenu 出现
- 空段落输入 `/` 时 FloatingMenu 出现
- 普通 Enter 后 FloatingMenu 不自动弹出
- 可以添加 / 编辑 / 移除链接
- 可以通过 URL 插入图片
- 可以插入基础表格
- 表格支持基础行列操作
- 不破坏中文输入、undo / redo、selection

---

# Phase 6：官方 DragHandle 与块级入口

## 目标

接入官方 DragHandle，开始形成块级编辑体验。

## 允许做

- 官方 DragHandleReact
- 基础块拖拽
- DragHandle 样式
- Dropcursor
- BlockMenu 骨架
- 当前块识别
- 安全块类型转换

## 禁止做

- 自定义拖拽系统
- 自定义 ProseMirror Plugin
- Decoration
- NodeView
- listItem 独立精细拖拽
- 表格内部拖拽
- 图片 resize
- 完整 block tree
- “+” 插入按钮
- 删除块
- 复制块
- 插入块

## 验收标准

- paragraph / heading / blockquote / codeBlock 基础拖拽可用
- 拖拽时 BubbleMenu / FloatingMenu 不出现
- BlockMenu 打开不影响拖拽
- 拖拽后 Outline 更新
- BlockMenu 能识别当前块
- 只允许 paragraph / heading / blockquote / codeBlock 安全转换
- table / image / list / taskList / details 复杂节点禁用转换

---

# Phase 7：编辑器体验型官方扩展

## 目标

用 Tiptap 官方能力提升写作体验。

## 允许做

- Dropcursor
- Focus
- CharacterCount
- Gapcursor
- Trailing Node
- List Keymap
- CodeBlockLowlight
- Details

## 禁止做

- 复杂 Focus 样式
- 自定义 Drop indicator
- 代码块自定义 NodeView
- 代码复制按钮
- 语言选择器
- 图片上传
- 复杂 Details 状态管理
- 自定义粘贴系统

## 验收标准

- 拖拽时有克制落点提示
- 当前块有轻微 Focus 提示
- 字数统计来自官方 CharacterCount
- CodeBlockLowlight 稳定
- Details 可插入、编辑、折叠、持久化
- 不引入项目自研 ProseMirror 底层逻辑

---

# Phase 8：输入体验与快捷键稳定

## 目标

保证真实写作时键盘行为稳定。

## 允许做

- CodeBlock Tab 缩进
- ListKeymap 官方列表缩进
- Tab 防止焦点逃出编辑器
- Esc 关闭菜单
- Enter / Shift+Enter QA
- 粘贴体验 QA
- 最小 CSS 修复

## 禁止做

- 完整自定义 keymap 系统
- Excel 式表格导航
- TaskItem nested schema 改动
- Markdown paste handler
- 图片粘贴上传
- 复杂 HTML 清洗
- 自定义 Plugin

## 验收标准

- codeBlock 中 Tab 插入缩进
- list 中 Tab / Shift+Tab 走官方缩进
- 普通 paragraph 中 Tab 不缩进，但不逃出编辑器
- taskList 不启用 nested
- table 保持官方默认行为
- 中文输入不受影响
- undo / redo 正常

---

# Phase 9：复杂节点稳定性

## 目标

保证 table / codeBlock / details / image 等复杂节点可用且不互相破坏。

## 允许做

- table cell paragraph 样式微调
- codeBlock QA
- details QA
- image QA
- Focus / DragHandle 在复杂节点上的样式微调

## 禁止做

- 表格高级能力
- codeBlock NodeView
- image resize
- details 深度集成
- 强行转换复杂节点
- 复杂节点删除 / 复制 / 插入

## 验收标准

- table cell 内 Enter 保持文档型表格默认行为
- table cell 内多段文本间距自然
- codeBlock 不被误转成 paragraph
- codeBlock clear formatting 不破坏代码块
- details 能保存 / 恢复 / 导入导出
- image 不撑破编辑区

---

# Phase 10：视觉与产品化收敛

## 目标

实现接近成熟产品的视觉和交互层级。

## 允许做

- Northstar sidebar 星空装饰
- 主题一致性收尾
- Toolbar 密度调整
- Menu 层级调整
- Layout correction
- RightPanel 信息层级优化

## 禁止做

- 视觉探索无限循环
- 同时参考多个冲突截图
- 大改编辑器逻辑
- 为了美观修改 schema / command / keymap
- 把正文区域做成 landing page

## 验收标准

- 左侧 Sidebar 有品牌感但不抢正文
- 中间编辑区是主视觉中心
- 右侧辅助栏稳定克制
- 工具栏不臃肿
- 菜单不互相遮挡
- 不存在明显绿色残留或主题混乱
- 图三风格布局优先

---

# Phase 11：后端数据契约准备

## 目标

在不接后端的情况下，明确未来保存接口和文档数据结构。

## 允许做

- Document DTO 设计
- Save API 方案
- Load API 方案
- Version 字段
- updatedAt / createdAt
- JSONContent 保存结构
- 文件上传接口设计
- 乐观保存策略设计

## 禁止做

- 直接接真实接口
- 改编辑器内容结构
- 引入登录权限
- 版本历史
- 评论
- 协同

## 验收标准

- 有清晰前后端数据契约
- JSONContent 结构明确
- 文件上传和图片节点关系明确
- 保存冲突策略初步明确
- 不影响现有 localStorage mock

---

# Phase 12：后端保存 MVP

## 目标

从本地 mock 过渡到真实后端保存。

## 允许做

- 获取文档
- 保存文档
- 新建文档
- 更新标题
- 更新 content JSON
- 基础错误处理
- 本地 fallback

## 禁止做

- 协同编辑
- 评论
- 版本历史
- 权限复杂化
- 自动合并冲突
- AI

## 验收标准

- 文档可从后端加载
- 修改后可保存到后端
- 刷新后后端数据恢复
- 保存失败有轻量提示
- localStorage 可以作为 fallback 或开发模式保留
- 不改变编辑器内部行为

---

# Phase 13：文件服务与图片上传

## 目标

将 Image URL 能力升级为真实图片上传。

## 允许做

- 接自己的 file-service
- 图片上传按钮
- 粘贴图片上传评估
- 上传进度
- 上传失败提示
- 图片 URL 写入 JSONContent

## 禁止做

- 图片 resize
- 图片 caption
- 图片编辑器
- 裁剪
- 拖拽缩放
- 图片资源打包导出

## 验收标准

- 可以上传图片
- 图片 URL 存入文档
- 刷新后图片存在
- 上传失败不破坏编辑器
- 不引入自定义 image NodeView

---

# Phase 14：BlockMenu 安全扩展

## 目标

逐步扩展块菜单，让其更接近 Notion / 语雀的块操作体验。

## 允许做

按顺序逐步实现：

1. 安全块类型转换
2. 在当前块下方插入 paragraph / heading / quote / codeBlock
3. 删除安全块
4. 复制安全块
5. 可选：基础块级缩进

## 禁止做

- 一轮内同时做插入 / 删除 / 复制
- 对 table / image / list / taskList / details 强行操作
- 完整 block tree
- listItem 独立 handle
- 表格内部拖拽
- 手写复杂 transaction
- 自定义 Plugin / Decoration / NodeView

## 验收标准

- 每个命令只作用于明确支持的安全节点
- 复杂节点 disabled
- 操作后菜单关闭
- 操作后焦点合理
- undo / redo 正常
- 文档 JSONContent 不异常
- 操作失败不会破坏内容

---

# Phase 15：Block Indent v1

## 目标

实现轻量块级缩进，不实现真正 Notion block tree。

## 允许做

- paragraph / heading / blockquote 增加 indent attr
- Tab / Shift+Tab 修改 indent
- Toolbar indent / outdent 可选
- 最大缩进层级限制
- CSS 根据 indent 控制 margin-left

## 禁止做

- 真正父子块结构
- block tree
- 父块折叠子块
- listItem 独立缩进替代官方列表
- table / image / codeBlock / details 缩进
- 自定义复杂 transaction

## 验收标准

- paragraph / heading / blockquote 可缩进
- JSONContent 中 indent attr 可保存
- Clear Formatting 可清除 indent
- list 仍走官方缩进
- codeBlock / table / image / details 不受影响
- undo / redo 正常

---

# Phase 16：Slash Command v1

## 目标

实现轻量 slash command，不做完整 Notion 命令系统。

## 允许做

- 输入 `/` 打开命令菜单
- 基础块插入
- heading
- list
- quote
- codeBlock
- image
- table
- details

## 禁止做

- AI command
- 搜索全部命令
- 多级分类
- 最近使用
- 自定义 Suggestion 复杂系统，除非方案评审通过
- 插件化命令系统

## 验收标准

- `/` 只在合理空段落触发
- 普通 Enter 不弹出菜单
- 选择命令后 `/` 被清理
- 插入块正常
- 不影响中文输入
- 不影响 FloatingMenu / BubbleMenu

---

# Phase 17：评论 / 版本 / AI 评估

## 目标

开始评估高级知识库能力，但不直接实现复杂版本。

## 允许做

- 评论方案设计
- 版本历史方案设计
- AI 改写方案设计
- 块 ID 方案评估
- 后端表结构草案

## 禁止做

- 直接实现协同编辑
- 直接做复杂评论锚点
- 直接上 AI 自动改全文
- 直接接 Tiptap Pro / Cloud
- 直接写评论 Decoration

## 验收标准

- 明确哪些能力需要 block id
- 明确哪些能力会影响 JSONContent
- 明确是否需要 UniqueID
- 明确风险
- 先出方案，不写代码

---

# Phase 18：协同编辑评估

## 目标

评估协同编辑是否值得做，以及采用什么方案。

## 允许做

- Yjs 方案评估
- Hocuspocus 方案评估
- Tiptap Collaboration 方案评估
- 自建服务成本评估
- Tiptap Platform 成本评估

## 禁止做

- 直接接协同
- 在当前编辑器里试验协同
- 改现有文档保存结构
- 影响普通单人编辑稳定性

## 验收标准

- 有明确成本分析
- 有服务端要求
- 有数据持久化方案
- 有冲突处理方案
- 有是否实施的判断

---

## 4. 高风险功能审批规则

以下功能必须先输出方案，不能直接写代码：

- Block Indent
- BlockMenu 删除 / 复制 / 插入
- Markdown paste
- 图片上传
- 图片 resize
- 代码块复制按钮
- 语言选择器
- Mention / @文档
- 评论
- 版本历史
- AI 改写
- 协同编辑
- UniqueID
- TableOfContents
- 自定义 NodeView
- 自定义 Plugin
- Decoration
- schema 变更

方案必须包含：

1. 为什么要做
2. 用户价值
3. 可替代方案
4. 是否有官方能力
5. 是否改 schema
6. 是否改 JSONContent
7. 是否影响已有文档
8. 是否影响 undo / redo
9. 是否影响中文输入
10. 如何回滚
11. QA 清单

---

## 5. 当前推荐路线

当前阶段推荐继续做：

1. Editor UX Audit
2. 现有复杂节点稳定性
3. Table / CodeBlock / Details 细节 QA
4. Focus / DragHandle / BlockMenu 层级收敛
5. 后端数据契约设计
6. 后端保存 MVP
7. 图片上传
8. BlockMenu 安全扩展
9. Block Indent v1
10. Slash Command v1

暂时不要做：

- 协同编辑
- 评论
- 版本历史
- AI 自动改写
- 图片 resize
- 完整 block tree
- 完整 Notion clone
- listItem 独立拖拽
- 表格高级能力

---

## 6. 最终目标提醒

本项目最终不是简单富文本编辑器。

最终体验目标是：

- 写作时像 Notion 一样顺畅
- 块操作像语雀 / Notion 一样自然
- 文档组织像知识库产品一样清楚
- 辅助面板提供大纲、信息、AI 能力
- 复杂能力逐步实现，不一次性堆叠
- 保持稳定、可维护、可回退

最终可以接近 Notion，但实现方式必须比 Notion 更克制、更适合本项目。

核心判断标准：

> 能用官方能力解决，就不要写底层。
> 能做小步实现，就不要一步到位。
> 能先做 QA，就不要直接加功能。
> 能先冻结稳定，就不要继续发散。