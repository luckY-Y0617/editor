import { useEffect, useState } from "react";
import type { HomeQuickActionRow } from "./workspaceHomeModel";

export type DisplayLocale = "en" | "zh-CN";

export const displayLanguageStorageKey = "northstar.displayLanguage";
export const displayLanguageEventName = "northstar:display-language-changed";

type TranslationKey =
  | "common.api"
  | "common.cancel"
  | "common.close"
  | "common.configured"
  | "common.currentLibrary"
  | "common.deferred"
  | "common.displayLanguage"
  | "common.forbidden"
  | "common.languageEnglish"
  | "common.languageChinese"
  | "common.loading"
  | "common.notConnected"
  | "common.no"
  | "common.open"
  | "common.retry"
  | "common.unavailable"
  | "common.waiting"
  | "common.workspace"
  | "common.yes"
  | "home.activeConversations"
  | "home.allActivity"
  | "home.goodMorning"
  | "home.newDecision"
  | "home.newDocument"
  | "home.logNote"
  | "home.moreActions"
  | "home.notificationDigest"
  | "home.quickActions"
  | "home.recentConversationsAndDecisions"
  | "home.recentlyTouched"
  | "home.requestAccess"
  | "home.shareUpdate"
  | "home.teamActivity"
  | "home.today"
  | "home.topContributors"
  | "home.waitingOnYou"
  | "home.workspaceMembers"
  | "home.workspaceSignals"
  | "library.allCollections"
  | "library.collection"
  | "library.collectionName"
  | "library.createCollection"
  | "library.createCollectionDescription"
  | "library.creating"
  | "library.deleteCollection"
  | "library.deleteDocument"
  | "library.deleteEmptyCollectionDescription"
  | "library.deleteNonEmptyCollectionDescription"
  | "library.documents"
  | "library.library"
  | "library.moveCollectionDown"
  | "library.moveCollectionUp"
  | "library.newCollection"
  | "library.newDocument"
  | "library.noCollections"
  | "library.noDocuments"
  | "library.renameCollection"
  | "library.renameCollectionDescription"
  | "library.saveName"
  | "library.searchPlaceholder"
  | "library.spaceSettings"
  | "library.working"
  | "nav.currentLibraryCollections"
  | "nav.home"
  | "nav.libraries"
  | "nav.members"
  | "nav.noCurrentLibraryCollections"
  | "nav.search"
  | "nav.settings"
  | "nav.updates"
  | "nav.workspace"
  | "settings.availableLibraries"
  | "settings.archiveWorkspace"
  | "settings.archiveWorkspaceDeferredReason"
  | "settings.assessment"
  | "settings.categoryAndDigest"
  | "settings.categoryPreferences"
  | "settings.collectionOrder"
  | "settings.collections"
  | "settings.contractReadiness"
  | "settings.createdAt"
  | "settings.currentRole"
  | "settings.currentLibraryLabel"
  | "settings.currentSpaceId"
  | "settings.currentSource"
  | "settings.dateFormat"
  | "settings.defaultDateFormat"
  | "settings.defaultNumberFormat"
  | "settings.defaultTimezone"
  | "settings.developer"
  | "settings.displayLanguageHelp"
  | "settings.documents"
  | "settings.email"
  | "settings.emailDigest"
  | "settings.editOrganizationProfile"
  | "settings.error"
  | "settings.flows"
  | "settings.general"
  | "settings.forbidden"
  | "settings.advanced"
  | "settings.archived"
  | "settings.dangerZone"
  | "settings.drafts"
  | "settings.heading"
  | "settings.headingReady"
  | "settings.implementationDependencies"
  | "settings.implementationRisk"
  | "settings.integrations"
  | "settings.languageAndRegion"
  | "settings.languageLocalBacked"
  | "settings.libraryHeading"
  | "settings.libraryLevelPermissions"
  | "settings.libraryNotificationsHelp"
  | "settings.manageInLibrary"
  | "settings.members"
  | "settings.name"
  | "settings.newDocumentInLibrary"
  | "settings.noCollections"
  | "settings.noDocuments"
  | "settings.noRowsReturned"
  | "settings.notFound"
  | "settings.notifications"
  | "settings.numberFormat"
  | "settings.openLibrary"
  | "settings.openWorkspaceNotifications"
  | "settings.organizationAssessment"
  | "settings.organizationAssessmentHelp"
  | "settings.organizationHeading"
  | "settings.organizationHeadingReady"
  | "settings.organizationLiveReadModel"
  | "settings.liveReadBacked"
  | "settings.organizationId"
  | "settings.organizationName"
  | "settings.organizationProfileEditHelp"
  | "settings.orgAuditLog"
  | "settings.orgBillingPlan"
  | "settings.orgDataRetention"
  | "settings.orgDomains"
  | "settings.orgGlobalMembers"
  | "settings.orgProfile"
  | "settings.organizationSlug"
  | "settings.orgSsoScimOwnership"
  | "settings.orgWorkspaceProvisioning"
  | "settings.organizationReadOnlyHelp"
  | "settings.organizationWorkspaces"
  | "settings.overview"
  | "settings.permissions"
  | "settings.plan"
  | "settings.position"
  | "settings.proposedDto"
  | "settings.proposedEndpoint"
  | "settings.proposedNotImplemented"
  | "settings.published"
  | "settings.profileUpdated"
  | "settings.recentDocuments"
  | "settings.readiness.deferred"
  | "settings.readiness.missingContract"
  | "settings.readiness.partial"
  | "settings.readiness.reusable"
  | "settings.recommendedFirstSlice"
  | "settings.recommendedFirstSliceReason"
  | "settings.recommendedPriority"
  | "settings.requiredBackendContract"
  | "settings.requiredFrontendSurface"
  | "settings.risk.high"
  | "settings.risk.low"
  | "settings.risk.medium"
  | "settings.security"
  | "settings.securityNotes"
  | "settings.saveChanges"
  | "settings.slug"
  | "settings.slugHelp"
  | "settings.scopeLibrary"
  | "settings.scopeOrganization"
  | "settings.scopeSystem"
  | "settings.scopeWorkspace"
  | "settings.settingsBoundaries"
  | "settings.spaceId"
  | "settings.spaceSettingsEntry"
  | "settings.statusLabel"
  | "settings.status.assessment"
  | "settings.status.deferred"
  | "settings.status.live"
  | "settings.status.notExposed"
  | "settings.status.reused"
  | "settings.status.unavailable"
  | "settings.systemBoundaryHelp"
  | "settings.timezone"
  | "settings.totalDocuments"
  | "settings.update"
  | "settings.updateDeferred"
  | "settings.updatedAt"
  | "settings.visibleWorkspaces"
  | "settings.watchedAndMuted"
  | "settings.workspaceCount"
  | "settings.workspaceId"
  | "settings.workspaceProvisioning"
  | "settings.workspaceSwitching"
  | "settings.workspaceSwitchingDeferred"
  | "settings.workspaceTitle"
  | "settings.workspaces"
  | "settings.openWorkspaceSettings"
  | "settings.createWorkspace"
  | "settings.createWorkspaceDeferredReason"
  | "settings.currentWorkspace"
  | "settings.memberManagement"
  | "settings.inviteMember"
  | "settings.inviteMemberDeferredReason"
  | "settings.liveReadOnly"
  | "settings.loading"
  | "settings.removeMember"
  | "settings.changeRole"
  | "settings.changeRoleDeferredReason"
  | "settings.readRule"
  | "settings.readOnlyStatus"
  | "settings.renameUnavailable"
  | "settings.ownerRequired"
  | "settings.removeMemberDeferredReason"
  | "settings.unconfigured"
  | "topbar.account"
  | "topbar.apiNotConfigured"
  | "topbar.importJson"
  | "topbar.exportJson"
  | "topbar.libraries"
  | "topbar.mfaEnabled"
  | "topbar.mfaNotEnabled"
  | "topbar.runSearch"
  | "topbar.saved"
  | "topbar.searchNorthstar"
  | "topbar.signInRequired"
  | "topbar.workspaceHome"
  | "updates.all"
  | "updates.comments"
  | "updates.documentChanges"
  | "updates.general"
  | "updates.access"
  | "updates.mentions"
  | "updates.unread"
  | "updates.heading"
  | "updates.latest"
  | "updates.markAllRead"
  | "updates.newestFirst"
  | "updates.noNotifications"
  | "updates.noNotificationsForFilter"
  | "updates.shown"
  | "updates.summary"
  | "updates.title";

type Messages = Record<TranslationKey, string>;

const messages: Record<DisplayLocale, Messages> = {
  en: {
    "common.api": "API",
    "common.cancel": "Cancel",
    "common.close": "Close",
    "common.configured": "Configured",
    "common.currentLibrary": "Current Library",
    "common.deferred": "Deferred",
    "common.displayLanguage": "Display language",
    "common.forbidden": "Forbidden",
    "common.languageEnglish": "English",
    "common.languageChinese": "简体中文",
    "common.loading": "Loading",
    "common.notConnected": "Not connected",
    "common.no": "No",
    "common.open": "Open",
    "common.retry": "Retry",
    "common.unavailable": "Unavailable",
    "common.waiting": "Waiting",
    "common.workspace": "Workspace",
    "common.yes": "Yes",
    "home.activeConversations": "Active conversations",
    "home.allActivity": "All activity",
    "home.goodMorning": "Good morning, {workspaceName}",
    "home.logNote": "Log a note",
    "home.moreActions": "More actions",
    "home.newDecision": "New decision",
    "home.newDocument": "New document",
    "home.notificationDigest": "Notification digest",
    "home.quickActions": "Quick actions",
    "home.recentConversationsAndDecisions": "Recent conversations and decisions",
    "home.recentlyTouched": "Recently touched",
    "home.requestAccess": "Request access",
    "home.shareUpdate": "Share update",
    "home.teamActivity": "Team activity",
    "home.today": "Today",
    "home.topContributors": "Top contributors",
    "home.waitingOnYou": "Waiting on you",
    "home.workspaceMembers": "Workspace members",
    "home.workspaceSignals": "Workspace signals",
    "library.allCollections": "All collections",
    "library.collection": "Collection",
    "library.collectionName": "Collection name",
    "library.createCollection": "Create collection",
    "library.createCollectionDescription": "Create a live-backed collection in the selected library.",
    "library.creating": "Creating",
    "library.deleteCollection": "Delete collection",
    "library.deleteDocument": "Delete document",
    "library.deleteEmptyCollectionDescription": "This empty collection will be removed from the selected library.",
    "library.deleteNonEmptyCollectionDescription": "{count} documents are still in this collection. The API will block deletion until it is empty.",
    "library.documents": "Documents",
    "library.library": "Library",
    "library.moveCollectionDown": "Move {title} down",
    "library.moveCollectionUp": "Move {title} up",
    "library.newCollection": "New collection",
    "library.newDocument": "New Document",
    "library.noCollections": "No collections are available in this library. Create a collection before adding documents.",
    "library.noDocuments": "No documents are available in this library. Select a collection, then create one.",
    "library.renameCollection": "Rename collection",
    "library.renameCollectionDescription": "Rename updates the collection title for everyone in this workspace.",
    "library.saveName": "Save name",
    "library.searchPlaceholder": "Search documents and collections",
    "library.spaceSettings": "Space Settings",
    "library.working": "Working...",
    "nav.currentLibraryCollections": "Current Library Collections",
    "nav.home": "Home",
    "nav.libraries": "Libraries",
    "nav.members": "Members",
    "nav.noCurrentLibraryCollections": "No current library collections",
    "nav.search": "Search",
    "nav.settings": "Settings",
    "nav.updates": "Updates",
    "nav.workspace": "Workspace",
    "settings.availableLibraries": "Available Libraries",
    "settings.archiveWorkspace": "Archive workspace",
    "settings.archiveWorkspaceDeferredReason": "Workspace archive/delete semantics are deferred for organization scope.",
    "settings.assessment": "Assessment",
    "settings.categoryAndDigest": "Category And Digest",
    "settings.categoryPreferences": "Category preferences",
    "settings.collectionOrder": "Collection order",
    "settings.collections": "Collections",
    "settings.contractReadiness": "Contract readiness",
    "settings.createdAt": "Created at",
    "settings.currentRole": "Current role",
    "settings.currentLibraryLabel": "Current library",
    "settings.currentSpaceId": "Current space ID",
    "settings.currentSource": "Current source",
    "settings.currentWorkspace": "Current workspace",
    "settings.dateFormat": "Date format",
    "settings.defaultDateFormat": "Workspace default; full format controls are deferred.",
    "settings.defaultNumberFormat": "Workspace default; full number controls are deferred.",
    "settings.defaultTimezone": "Browser local time; workspace timezone controls are deferred.",
    "settings.advanced": "Advanced",
    "settings.archived": "Archived",
    "settings.dangerZone": "Danger Zone",
    "settings.developer": "Developer",
    "settings.displayLanguageHelp": "Changes apply immediately to this browser session.",
    "settings.documents": "Documents",
    "settings.email": "Email",
    "settings.drafts": "Drafts",
    "settings.emailDigest": "Email digest",
    "settings.editOrganizationProfile": "Edit organization profile",
    "settings.error": "Error",
    "settings.flows": "Security Flows",
    "settings.general": "General",
    "settings.forbidden": "Forbidden",
    "settings.heading": "Workspace Settings",
    "settings.headingReady": "Workspace-level settings backed by current workspace data.",
    "settings.implementationDependencies": "Implementation dependencies",
    "settings.implementationRisk": "Implementation risk",
    "settings.integrations": "Integrations",
    "settings.languageAndRegion": "Language & Region",
    "settings.languageLocalBacked": "Local user preference stored in this browser. No backend preference API is used.",
    "settings.libraryHeading": "Library Settings",
    "settings.libraryLevelPermissions": "Library-level permissions are deferred until a dedicated backend contract exists.",
    "settings.libraryNotificationsHelp": "Only watched or muted resources that match this library can be shown from the current preferences API.",
    "settings.manageInLibrary": "Manage in Library",
    "settings.members": "Members",
    "settings.name": "Name",
    "settings.newDocumentInLibrary": "New document in Library",
    "settings.noCollections": "No collections are available for this library.",
    "settings.noDocuments": "No documents are available for this library.",
    "settings.noRowsReturned": "No rows returned",
    "settings.notFound": "Not found",
    "settings.notifications": "Notifications",
    "settings.numberFormat": "Number format",
    "settings.openLibrary": "Open Library",
    "settings.openWorkspaceNotifications": "Open Workspace Notifications",
    "settings.organizationAssessment": "Organization Assessment",
    "settings.organizationAssessmentHelp": "Assessment keeps broader organization mutations deferred while profile rename is owner-gated.",
    "settings.organizationHeading": "Organization Settings Assessment",
    "settings.organizationHeadingReady": "Organization profile is live-backed. Profile rename is owner-gated; broader administration remains deferred.",
    "settings.organizationLiveReadModel": "Organization Live Read Model",
    "settings.liveReadBacked": "Live-backed by the organization read-only API.",
    "settings.liveReadOnly": "Read-only live data",
    "settings.loading": "Loading",
    "settings.organizationId": "Organization ID",
    "settings.organizationName": "Organization name",
    "settings.organizationProfileEditHelp": "Owners can update the profile name and backend slug. Other organization mutations remain deferred.",
    "settings.orgAuditLog": "Audit log",
    "settings.orgBillingPlan": "Billing / Plan",
    "settings.orgDataRetention": "Data retention",
    "settings.orgDomains": "Domains",
    "settings.orgGlobalMembers": "Global members",
    "settings.orgProfile": "Organization profile",
    "settings.organizationSlug": "Organization slug",
    "settings.orgSsoScimOwnership": "SSO / SCIM ownership",
    "settings.orgWorkspaceProvisioning": "Workspace provisioning",
    "settings.organizationReadOnlyHelp": "Organization profile rename is owner-gated. Other organization mutations remain deferred.",
    "settings.organizationWorkspaces": "Organization Workspaces",
    "settings.overview": "Overview",
    "settings.permissions": "Permissions",
    "settings.plan": "Plan",
    "settings.position": "Position",
    "settings.proposedDto": "Proposed DTO",
    "settings.proposedEndpoint": "Proposed endpoint",
    "settings.proposedNotImplemented": "Proposed only. Not implemented or live-backed.",
    "settings.published": "Published",
    "settings.profileUpdated": "Profile updated",
    "settings.recentDocuments": "Recent documents",
    "settings.readiness.deferred": "Deferred",
    "settings.readiness.missingContract": "Missing contract",
    "settings.readiness.partial": "Partial",
    "settings.readiness.reusable": "Reusable",
    "settings.recommendedFirstSlice": "Recommended First Slice",
    "settings.recommendedFirstSliceReason": "Reason",
    "settings.recommendedPriority": "Recommended priority",
    "settings.requiredBackendContract": "Required backend contract",
    "settings.requiredFrontendSurface": "Required frontend surface",
    "settings.risk.high": "High",
    "settings.risk.low": "Low",
    "settings.risk.medium": "Medium",
    "settings.security": "Security",
    "settings.securityNotes": "Security notes",
    "settings.saveChanges": "Save changes",
    "settings.slug": "Slug",
    "settings.slugHelp": "The slug is stored on the organization profile. It does not enable organization URL routing in this slice.",
    "settings.scopeLibrary": "Current Library",
    "settings.scopeOrganization": "Organization",
    "settings.scopeSystem": "System / Instance",
    "settings.scopeWorkspace": "Workspace",
    "settings.settingsBoundaries": "Settings Boundaries",
    "settings.spaceId": "Space ID",
    "settings.spaceSettingsEntry": "Space Settings Entry",
    "settings.statusLabel": "Status",
    "settings.status.assessment": "Assessment",
    "settings.status.deferred": "Deferred",
    "settings.status.live": "Live-backed",
    "settings.status.notExposed": "Not exposed",
    "settings.status.reused": "Reuses existing surface",
    "settings.status.unavailable": "Unavailable",
    "settings.systemBoundaryHelp": "System / instance settings belong to deployment and operations, and are not exposed in this workspace UI.",
    "settings.timezone": "Timezone",
    "settings.totalDocuments": "Total documents",
    "settings.update": "Update",
    "settings.updateDeferred": "Deferred: no workspace update API is exposed in this frontend flow",
    "settings.updatedAt": "Updated at",
    "settings.visibleWorkspaces": "Visible workspaces",
    "settings.watchedAndMuted": "Watched and muted resources",
    "settings.workspaceCount": "Workspace count",
    "settings.workspaceId": "Workspace ID",
    "settings.workspaceProvisioning": "Workspace provisioning",
    "settings.workspaceSwitching": "Workspace switching",
    "settings.workspaceSwitchingDeferred": "Workspace switching deferred",
    "settings.workspaceTitle": "Workspace",
    "settings.workspaces": "Workspaces",
    "settings.openWorkspaceSettings": "Open Workspace Settings",
    "settings.createWorkspace": "Create workspace",
    "settings.createWorkspaceDeferredReason": "Organization workspace provisioning has no mutation contract in this slice.",
    "settings.memberManagement": "Member management",
    "settings.inviteMember": "Invite member",
    "settings.inviteMemberDeferredReason": "Organization-level invite flow is not implemented in this read-only slice.",
    "settings.removeMember": "Remove member",
    "settings.removeMemberDeferredReason": "Organization-level removal needs last-owner and workspace membership safety rules.",
    "settings.changeRole": "Change role",
    "settings.changeRoleDeferredReason": "Organization-level role changes need a separate permission contract.",
    "settings.readRule": "Read rule",
    "settings.readOnlyStatus": "Read-only status",
    "settings.renameUnavailable": "Rename unavailable",
    "settings.ownerRequired": "Owner required / insufficient permission",
    "settings.unconfigured": "Unconfigured",
    "topbar.account": "Northstar account",
    "topbar.apiNotConfigured": "API not configured",
    "topbar.exportJson": "Export workspace JSON",
    "topbar.importJson": "Import workspace JSON",
    "topbar.libraries": "Libraries",
    "topbar.mfaEnabled": "MFA enabled",
    "topbar.mfaNotEnabled": "MFA not enabled",
    "topbar.runSearch": "Run search",
    "topbar.saved": "Saved",
    "topbar.searchNorthstar": "Search Northstar",
    "topbar.signInRequired": "Sign in required",
    "topbar.workspaceHome": "Workspace Home",
    "updates.access": "Access / approvals",
    "updates.all": "All",
    "updates.comments": "Comments",
    "updates.documentChanges": "Document changes",
    "updates.general": "General",
    "updates.heading": "Track comments, mentions, document changes, and workspace activity.",
    "updates.latest": "Latest",
    "updates.markAllRead": "Mark all read",
    "updates.mentions": "Mentions",
    "updates.newestFirst": "Newest first",
    "updates.noNotifications": "No notifications",
    "updates.noNotificationsForFilter": "No notifications match this filter",
    "updates.shown": "{count} shown",
    "updates.summary": "Summary",
    "updates.title": "Updates",
    "updates.unread": "Unread",
  },
  "zh-CN": {
    "common.api": "API",
    "common.cancel": "取消",
    "common.close": "关闭",
    "common.configured": "已配置",
    "common.currentLibrary": "当前资料库",
    "common.deferred": "暂未支持",
    "common.displayLanguage": "显示语言",
    "common.forbidden": "无权限",
    "common.languageEnglish": "English",
    "common.languageChinese": "简体中文",
    "common.loading": "加载中",
    "common.notConnected": "未连接",
    "common.no": "否",
    "common.open": "打开",
    "common.retry": "重试",
    "common.unavailable": "不可用",
    "common.waiting": "等待中",
    "common.workspace": "工作区",
    "common.yes": "是",
    "home.activeConversations": "活跃讨论",
    "home.allActivity": "全部动态",
    "home.goodMorning": "早上好，{workspaceName}",
    "home.logNote": "记录笔记",
    "home.moreActions": "更多操作",
    "home.newDecision": "新建决策",
    "home.newDocument": "新建文档",
    "home.notificationDigest": "通知摘要",
    "home.quickActions": "快捷操作",
    "home.recentConversationsAndDecisions": "最近讨论与决策",
    "home.recentlyTouched": "最近触达",
    "home.requestAccess": "请求访问",
    "home.shareUpdate": "分享更新",
    "home.teamActivity": "团队动态",
    "home.today": "今天",
    "home.topContributors": "主要贡献者",
    "home.waitingOnYou": "待你处理",
    "home.workspaceMembers": "工作区成员",
    "home.workspaceSignals": "工作区信号",
    "library.allCollections": "全部集合",
    "library.collection": "集合",
    "library.collectionName": "集合名称",
    "library.createCollection": "创建集合",
    "library.createCollectionDescription": "在当前资料库中创建真实后端支持的集合。",
    "library.creating": "创建中",
    "library.deleteCollection": "删除集合",
    "library.deleteDocument": "删除文档",
    "library.deleteEmptyCollectionDescription": "这个空集合将从当前资料库移除。",
    "library.deleteNonEmptyCollectionDescription": "这个集合中仍有 {count} 个文档。API 会阻止删除，直到集合为空。",
    "library.documents": "文档",
    "library.library": "资料库",
    "library.moveCollectionDown": "下移 {title}",
    "library.moveCollectionUp": "上移 {title}",
    "library.newCollection": "新建集合",
    "library.newDocument": "新建文档",
    "library.noCollections": "当前资料库暂无集合。请先创建集合，再添加文档。",
    "library.noDocuments": "当前资料库暂无文档。选择一个集合后创建文档。",
    "library.renameCollection": "重命名集合",
    "library.renameCollectionDescription": "重命名会更新此工作区所有人看到的集合标题。",
    "library.saveName": "保存名称",
    "library.searchPlaceholder": "搜索文档和集合",
    "library.spaceSettings": "空间设置",
    "library.working": "处理中...",
    "nav.currentLibraryCollections": "当前资料库集合",
    "nav.home": "主页",
    "nav.libraries": "资料库",
    "nav.members": "成员",
    "nav.noCurrentLibraryCollections": "暂无当前资料库集合",
    "nav.search": "搜索",
    "nav.settings": "设置",
    "nav.updates": "更新",
    "nav.workspace": "工作区",
    "settings.availableLibraries": "可用资料库",
    "settings.archiveWorkspace": "归档工作区",
    "settings.archiveWorkspaceDeferredReason": "组织范围的工作区归档 / 删除语义暂未定义。",
    "settings.assessment": "评估",
    "settings.categoryAndDigest": "分类与摘要",
    "settings.categoryPreferences": "分类偏好",
    "settings.collectionOrder": "集合顺序",
    "settings.collections": "集合",
    "settings.contractReadiness": "合同就绪度",
    "settings.createdAt": "创建时间",
    "settings.currentRole": "当前角色",
    "settings.currentLibraryLabel": "当前资料库",
    "settings.currentSpaceId": "当前空间 ID",
    "settings.currentSource": "当前来源",
    "settings.currentWorkspace": "当前工作区",
    "settings.dateFormat": "日期格式",
    "settings.defaultDateFormat": "使用工作区默认值；完整格式设置暂未支持。",
    "settings.defaultNumberFormat": "使用工作区默认值；完整数字格式设置暂未支持。",
    "settings.defaultTimezone": "使用浏览器本地时间；工作区时区设置暂未支持。",
    "settings.advanced": "高级",
    "settings.archived": "已归档",
    "settings.dangerZone": "危险区",
    "settings.developer": "开发者",
    "settings.displayLanguageHelp": "切换后会立即应用到当前浏览器会话。",
    "settings.documents": "文档",
    "settings.email": "邮箱",
    "settings.drafts": "草稿",
    "settings.emailDigest": "邮件摘要",
    "settings.editOrganizationProfile": "编辑组织资料",
    "settings.error": "错误",
    "settings.flows": "安全流程",
    "settings.general": "通用",
    "settings.forbidden": "无权访问",
    "settings.heading": "工作区设置",
    "settings.headingReady": "当前工作区数据支持的工作区级设置。",
    "settings.implementationDependencies": "Implementation dependencies",
    "settings.implementationRisk": "Implementation risk",
    "settings.integrations": "集成",
    "settings.languageAndRegion": "语言和区域",
    "settings.languageLocalBacked": "这是保存在当前浏览器中的本地用户偏好，不使用后端偏好 API。",
    "settings.libraryHeading": "资料库设置",
    "settings.libraryLevelPermissions": "资料库级权限需要专门后端合同，本轮暂不支持。",
    "settings.libraryNotificationsHelp": "当前偏好 API 只能显示与此资料库匹配的已关注或已静音资源。",
    "settings.manageInLibrary": "在资料库中管理",
    "settings.members": "成员",
    "settings.name": "名称",
    "settings.newDocumentInLibrary": "在资料库中新建文档",
    "settings.noCollections": "此资料库暂无集合。",
    "settings.noDocuments": "此资料库暂无文档。",
    "settings.noRowsReturned": "没有返回数据",
    "settings.notFound": "未找到",
    "settings.notifications": "通知",
    "settings.numberFormat": "数字格式",
    "settings.openLibrary": "打开资料库",
    "settings.openWorkspaceNotifications": "打开工作区通知设置",
    "settings.organizationAssessment": "组织设置评估",
    "settings.organizationAssessmentHelp": "评估仍保留更大的组织变更为暂缓状态，profile 重命名仅限拥有者。",
    "settings.organizationHeading": "组织设置评估",
    "settings.organizationHeadingReady": "组织资料已接入实时后端。profile 重命名仅限拥有者，更大的管理能力仍暂缓。",
    "settings.organizationLiveReadModel": "组织实时只读模型",
    "settings.liveReadBacked": "由组织只读 API 实时支持。",
    "settings.liveReadOnly": "真实只读数据",
    "settings.loading": "加载中",
    "settings.organizationId": "组织 ID",
    "settings.organizationName": "组织名称",
    "settings.organizationProfileEditHelp": "拥有者可以更新组织名称和后端 slug，其他组织变更仍然暂缓。",
    "settings.orgAuditLog": "审计日志",
    "settings.orgBillingPlan": "账单 / 套餐",
    "settings.orgDataRetention": "数据保留",
    "settings.orgDomains": "域名",
    "settings.orgGlobalMembers": "全局成员",
    "settings.orgProfile": "组织资料",
    "settings.organizationSlug": "组织 slug",
    "settings.orgSsoScimOwnership": "SSO / SCIM 归属",
    "settings.orgWorkspaceProvisioning": "工作区开通",
    "settings.organizationReadOnlyHelp": "组织资料重命名仅限拥有者。其他组织变更仍然暂缓。",
    "settings.organizationWorkspaces": "组织工作区",
    "settings.overview": "概览",
    "settings.permissions": "权限",
    "settings.plan": "套餐",
    "settings.position": "位置",
    "settings.proposedDto": "Proposed DTO",
    "settings.proposedEndpoint": "Proposed endpoint",
    "settings.proposedNotImplemented": "Proposed only. Not implemented or live-backed.",
    "settings.published": "已发布",
    "settings.profileUpdated": "资料已更新",
    "settings.recentDocuments": "最近文档",
    "settings.readiness.deferred": "暂缓",
    "settings.readiness.missingContract": "缺少合同",
    "settings.readiness.partial": "部分可用",
    "settings.readiness.reusable": "可复用",
    "settings.recommendedFirstSlice": "建议第一切片",
    "settings.recommendedFirstSliceReason": "原因",
    "settings.recommendedPriority": "建议优先级",
    "settings.requiredBackendContract": "所需后端合同",
    "settings.requiredFrontendSurface": "所需前端界面",
    "settings.risk.high": "High",
    "settings.risk.low": "Low",
    "settings.risk.medium": "Medium",
    "settings.security": "安全",
    "settings.securityNotes": "安全说明",
    "settings.saveChanges": "保存更改",
    "settings.slug": "Slug",
    "settings.slugHelp": "slug 只保存为组织资料字段，本轮不会启用完整组织 URL 路由。",
    "settings.scopeLibrary": "当前资料库",
    "settings.scopeOrganization": "组织",
    "settings.scopeSystem": "系统 / 实例",
    "settings.scopeWorkspace": "工作区",
    "settings.settingsBoundaries": "设置边界",
    "settings.spaceId": "空间 ID",
    "settings.spaceSettingsEntry": "空间设置入口",
    "settings.statusLabel": "状态",
    "settings.status.assessment": "评估中",
    "settings.status.deferred": "暂未支持",
    "settings.status.live": "真实数据支持",
    "settings.status.notExposed": "不暴露",
    "settings.status.reused": "复用现有界面",
    "settings.status.unavailable": "不可用",
    "settings.systemBoundaryHelp": "系统 / 实例设置属于部署和运维层，不在当前工作区 UI 中暴露。",
    "settings.timezone": "时区",
    "settings.totalDocuments": "文档总数",
    "settings.update": "更新",
    "settings.updateDeferred": "暂未支持：当前前端流程没有暴露工作区更新 API",
    "settings.updatedAt": "更新时间",
    "settings.visibleWorkspaces": "可见工作区",
    "settings.watchedAndMuted": "已关注和已静音资源",
    "settings.workspaceCount": "工作区数量",
    "settings.workspaceId": "工作区 ID",
    "settings.workspaceProvisioning": "工作区开通",
    "settings.workspaceSwitching": "工作区切换",
    "settings.workspaceSwitchingDeferred": "工作区切换暂未支持",
    "settings.workspaceTitle": "工作区",
    "settings.workspaces": "工作区",
    "settings.openWorkspaceSettings": "打开工作区设置",
    "settings.createWorkspace": "创建工作区",
    "settings.createWorkspaceDeferredReason": "本只读切片没有组织级工作区开通变更合同。",
    "settings.memberManagement": "成员管理",
    "settings.inviteMember": "邀请成员",
    "settings.inviteMemberDeferredReason": "本只读切片未实现组织级邀请流程。",
    "settings.removeMember": "移除成员",
    "settings.removeMemberDeferredReason": "组织级移除需要 last-owner 和工作区成员关系安全规则。",
    "settings.changeRole": "更改角色",
    "settings.changeRoleDeferredReason": "组织级角色变更需要单独的权限合同。",
    "settings.readRule": "读取规则",
    "settings.readOnlyStatus": "只读状态",
    "settings.renameUnavailable": "暂不能重命名",
    "settings.ownerRequired": "需要拥有者权限 / 权限不足",
    "settings.unconfigured": "未配置",
    "topbar.account": "Northstar 账户",
    "topbar.apiNotConfigured": "API 未配置",
    "topbar.exportJson": "导出工作区 JSON",
    "topbar.importJson": "导入工作区 JSON",
    "topbar.libraries": "资料库",
    "topbar.mfaEnabled": "已启用 MFA",
    "topbar.mfaNotEnabled": "未启用 MFA",
    "topbar.runSearch": "执行搜索",
    "topbar.saved": "已保存",
    "topbar.searchNorthstar": "搜索 Northstar",
    "topbar.signInRequired": "需要登录",
    "topbar.workspaceHome": "工作区主页",
    "updates.access": "访问 / 审批",
    "updates.all": "全部",
    "updates.comments": "评论",
    "updates.documentChanges": "文档变更",
    "updates.general": "常规",
    "updates.heading": "跟踪评论、提及、文档变更和工作区动态。",
    "updates.latest": "最新",
    "updates.markAllRead": "全部标为已读",
    "updates.mentions": "提及",
    "updates.newestFirst": "最新优先",
    "updates.noNotifications": "暂无通知",
    "updates.noNotificationsForFilter": "没有符合当前筛选的通知",
    "updates.shown": "显示 {count} 条",
    "updates.summary": "摘要",
    "updates.title": "更新",
    "updates.unread": "未读",
  },
};

export function getDisplayLanguageOptions() {
  return [
    { label: messages.en["common.languageEnglish"], locale: "en" as const },
    { label: messages["zh-CN"]["common.languageChinese"], locale: "zh-CN" as const },
  ];
}

export function normalizeDisplayLocale(value: unknown): DisplayLocale {
  return value === "zh-CN" ? "zh-CN" : "en";
}

export function getStoredDisplayLocale(): DisplayLocale {
  const storage = getLocalStorage();
  if (!storage) {
    return "en";
  }

  return normalizeDisplayLocale(storage.getItem(displayLanguageStorageKey));
}

export function setStoredDisplayLocale(locale: DisplayLocale) {
  const normalized = normalizeDisplayLocale(locale);
  getLocalStorage()?.setItem(displayLanguageStorageKey, normalized);
  dispatchDisplayLanguageChange(normalized);
  return normalized;
}

export function subscribeToDisplayLanguageChanges(listener: (locale: DisplayLocale) => void) {
  const win = getWindow();
  if (!win) {
    return () => undefined;
  }

  const handleChange = () => listener(getStoredDisplayLocale());
  win.addEventListener(displayLanguageEventName, handleChange);
  win.addEventListener("storage", handleChange);
  return () => {
    win.removeEventListener(displayLanguageEventName, handleChange);
    win.removeEventListener("storage", handleChange);
  };
}

export function useDisplayLanguage() {
  const [locale, setLocaleState] = useState<DisplayLocale>(() => getStoredDisplayLocale());

  useEffect(() => subscribeToDisplayLanguageChanges(setLocaleState), []);

  const setLocale = (nextLocale: DisplayLocale) => {
    setLocaleState(setStoredDisplayLocale(nextLocale));
  };

  return { locale, setLocale };
}

export function t(
  locale: DisplayLocale,
  key: TranslationKey,
  values: Record<string, string | number> = {},
) {
  const template = messages[locale][key] ?? messages.en[key] ?? key;
  return template.replace(/\{(\w+)\}/g, (_match, name: string) => String(values[name] ?? ""));
}

export function getQuickActionLabel(locale: DisplayLocale, id: HomeQuickActionRow["id"]) {
  switch (id) {
    case "new-document":
      return t(locale, "home.newDocument");
    case "new-decision":
      return t(locale, "home.newDecision");
    case "request-access":
      return t(locale, "home.requestAccess");
    case "share-update":
      return t(locale, "home.shareUpdate");
    case "log-note":
      return t(locale, "home.logNote");
    case "more-actions":
    default:
      return t(locale, "home.moreActions");
  }
}

export function getWorkspaceUpdatesTabDisplayLabel(
  locale: DisplayLocale,
  tab: "access" | "all" | "comments" | "documents" | "general" | "mentions" | "unread",
) {
  switch (tab) {
    case "unread":
      return t(locale, "updates.unread");
    case "comments":
      return t(locale, "updates.comments");
    case "mentions":
      return t(locale, "updates.mentions");
    case "access":
      return t(locale, "updates.access");
    case "documents":
      return t(locale, "updates.documentChanges");
    case "general":
      return t(locale, "updates.general");
    case "all":
    default:
      return t(locale, "updates.all");
  }
}

function dispatchDisplayLanguageChange(locale: DisplayLocale) {
  const win = getWindow();
  if (!win) {
    return;
  }

  const event = typeof CustomEvent === "function"
    ? new CustomEvent(displayLanguageEventName, { detail: { locale } })
    : typeof Event === "function"
      ? new Event(displayLanguageEventName)
      : ({ type: displayLanguageEventName } as Event);
  win.dispatchEvent(event);
}

function getLocalStorage(): Storage | null {
  try {
    return getWindow()?.localStorage ?? null;
  } catch {
    return null;
  }
}

function getWindow(): Window | null {
  return typeof window === "undefined" ? null : window;
}
