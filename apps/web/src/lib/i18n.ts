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
  | "settings.accessIdentity"
  | "settings.assessment"
  | "settings.categoryAndDigest"
  | "settings.categoryPreferences"
  | "settings.collectionOrder"
  | "settings.collectionSummary"
  | "settings.collections"
  | "settings.contractReadiness"
  | "settings.createdAt"
  | "settings.currentRole"
  | "settings.currentLibraryLabel"
  | "settings.currentLibrarySummary"
  | "settings.currentSpaceId"
  | "settings.currentSource"
  | "settings.dateFormat"
  | "settings.defaultDateFormat"
  | "settings.defaultNumberFormat"
  | "settings.defaultTimezone"
  | "settings.developer"
  | "settings.displayLanguageHelp"
  | "settings.documents"
  | "settings.documentPermissions"
  | "settings.documentPermissionsManagedFromDocument"
  | "settings.documentSummary"
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
  | "settings.centerHeading"
  | "settings.centerHeadingReady"
  | "settings.contextSummary"
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
  | "settings.libraryPermissionsBoundaryHelp"
  | "settings.manageInLibrary"
  | "settings.members"
  | "settings.membersInventory"
  | "settings.name"
  | "settings.newDocumentInLibrary"
  | "settings.noCollections"
  | "settings.noDocuments"
  | "settings.noRowsReturned"
  | "settings.notFound"
  | "settings.notifications"
  | "settings.numberFormat"
  | "settings.openLibrary"
  | "settings.openWorkspaceMembers"
  | "settings.openWorkspaceNotifications"
  | "settings.organizationAssessment"
  | "settings.organizationAssessmentHelp"
  | "settings.organizationHeading"
  | "settings.organizationHeadingReady"
  | "settings.organizationSettingsHeading"
  | "settings.organizationSettingsReady"
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
  | "settings.preferences"
  | "settings.personalSettingsHeading"
  | "settings.personalSettingsReady"
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
  | "settings.backendStatus"
  | "settings.frontendStatus"
  | "settings.informationArchitecture"
  | "settings.inventoryConflictMarked"
  | "settings.inventoryDefer"
  | "settings.inventoryHalfFinished"
  | "settings.inventoryKeep"
  | "settings.inventoryLibraryOperations"
  | "settings.inventoryLiveMutation"
  | "settings.inventoryLiveRead"
  | "settings.inventoryMissing"
  | "settings.inventoryMove"
  | "settings.inventoryOrganizationMembers"
  | "settings.inventoryOrganizationProfile"
  | "settings.inventoryPersonalLanguage"
  | "settings.inventoryReadOnly"
  | "settings.inventoryRemoveAction"
  | "settings.inventoryResourceShare"
  | "settings.inventoryShouldMove"
  | "settings.inventorySplit"
  | "settings.inventoryStatic"
  | "settings.inventorySystemSettings"
  | "settings.inventoryWorkspaceMembers"
  | "settings.inventoryWorkspaceNotifications"
  | "settings.inventoryWorkspaceProfile"
  | "settings.inventoryWorkspaceProvisioning"
  | "settings.accessRequests"
  | "settings.accessRequestsManagedInUpdates"
  | "settings.backendPermissionChecked"
  | "settings.boundary"
  | "settings.groups"
  | "settings.lastOwnerProtected"
  | "settings.libraryOperations"
  | "settings.libraryOperationsSurfaceHelp"
  | "settings.membersSurfaceHelp"
  | "settings.membersManagedInWorkspaceSettings"
  | "settings.notificationBoundaryHelp"
  | "settings.openMembersSurface"
  | "settings.personalPreferenceLocalHelp"
  | "settings.recommendedSettingsClosure"
  | "settings.resourcePreferences"
  | "settings.resourceShare"
  | "settings.resourceShareSurfaceHelp"
  | "settings.roleBoundaries"
  | "settings.selectedCapabilities"
  | "settings.shareLinksPublicLinks"
  | "settings.sortOrder"
  | "settings.viewerManagementDisabled"
  | "settings.workspaceProfile"
  | "settings.workspaceProfileReadOnlyHelp"
  | "settings.loadingNotificationPreferences"
  | "settings.notificationPreferenceApiFailed"
  | "settings.notificationPreferenceForbidden"
  | "settings.notificationPreferenceHelp"
  | "settings.preferenceUpdated"
  | "settings.preferenceUpdateUnavailable"
  | "settings.recommendation"
  | "settings.savingPreference"
  | "settings.scope"
  | "settings.scopePersonal"
  | "settings.scopeResource"
  | "settings.scopeDeferred"
  | "settings.secondaryNavigation"
  | "settings.workspaceNotificationDefault"
  | "settings.workspaceNotificationMuted"
  | "settings.workspaceNotificationPreference"
  | "settings.workspaceNotificationWatched"
  | "share.clipboardBlocked"
  | "share.clipboardUnavailable"
  | "share.createInternalLink"
  | "share.internalLinkCreated"
  | "share.linkCreated"
  | "share.linkRevoked"
  | "share.nothingToCopy"
  | "share.openFromDocument"
  | "share.operationInProgress"
  | "share.shareApiUnavailable"
  | "share.tokenCopied"
  | "share.urlCopied"
  | "topbar.account"
  | "topbar.apiNotConfigured"
  | "topbar.currentWorkspace"
  | "topbar.importJson"
  | "topbar.exportJson"
  | "topbar.libraries"
  | "topbar.mfaEnabled"
  | "topbar.mfaNotEnabled"
  | "topbar.organizationSettings"
  | "topbar.personalSettings"
  | "topbar.runSearch"
  | "topbar.saved"
  | "topbar.searchNorthstar"
  | "topbar.signOut"
  | "topbar.signInRequired"
  | "topbar.workspaceListUnavailable"
  | "topbar.workspaceSwitcher"
  | "topbar.workspaceSwitchingDeferred"
  | "topbar.workspaceHome"
  | "updates.accessRequests"
  | "updates.all"
  | "updates.expiry"
  | "updates.expiring"
  | "updates.failed"
  | "updates.failedInvites"
  | "updates.grantsGroups"
  | "updates.invites"
  | "updates.access"
  | "updates.links"
  | "updates.mutedFolders"
  | "updates.pendingReview"
  | "updates.sharing"
  | "updates.totalNotifications"
  | "updates.unread"
  | "updates.unreadLabel"
  | "updates.watchedDocuments"
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
    "common.languageChinese": "\u7b80\u4f53\u4e2d\u6587",
    "common.loading": "Loading",
    "common.notConnected": "Not connected",
    "common.no": "No",
    "common.open": "Open",
    "common.retry": "Retry",
    "common.unavailable": "Unavailable",
    "common.waiting": "Waiting",
    "common.workspace": "Workspace",
    "common.yes": "Yes",
    "home.activeConversations": "Access and sharing updates",
    "home.allActivity": "All activity",
    "home.goodMorning": "Good morning, {workspaceName}",
    "home.logNote": "Log a note",
    "home.moreActions": "More actions",
    "home.newDecision": "New decision",
    "home.newDocument": "New document",
    "home.notificationDigest": "Access notification digest",
    "home.quickActions": "Quick actions",
    "home.recentConversationsAndDecisions": "Access and sharing context",
    "home.recentlyTouched": "Recently touched",
    "home.requestAccess": "Request access",
    "home.shareUpdate": "Share update",
    "home.teamActivity": "Recent document activity",
    "home.today": "Today",
    "home.topContributors": "Top contributors",
    "home.waitingOnYou": "Waiting on you",
    "home.workspaceMembers": "Workspace members",
    "home.workspaceSignals": "Workspace signals",
    "library.allCollections": "All folders",
    "library.collection": "Folder",
    "library.collectionName": "Folder name",
    "library.createCollection": "Create folder",
    "library.createCollectionDescription": "Create a live-backed folder in the selected library.",
    "library.creating": "Creating",
    "library.deleteCollection": "Delete folder",
    "library.deleteDocument": "Delete document",
    "library.deleteEmptyCollectionDescription": "This empty folder will be removed from the selected library.",
    "library.deleteNonEmptyCollectionDescription": "{count} documents are still in this folder. The API will block deletion until it is empty.",
    "library.documents": "Documents",
    "library.library": "Library",
    "library.moveCollectionDown": "Move {title} down",
    "library.moveCollectionUp": "Move {title} up",
    "library.newCollection": "New folder",
    "library.newDocument": "New Document",
    "library.noCollections": "No folders are available in this library. Create a folder before adding documents.",
    "library.noDocuments": "No documents are available in this library. Select a folder, then create one.",
    "library.renameCollection": "Rename folder",
    "library.renameCollectionDescription": "Rename updates the folder title for everyone in this workspace.",
    "library.saveName": "Save name",
    "library.searchPlaceholder": "Search documents and folders",
    "library.spaceSettings": "Library Settings",
    "library.working": "Working...",
    "nav.currentLibraryCollections": "Current Library Folders",
    "nav.home": "Home",
    "nav.libraries": "Libraries",
    "nav.members": "Members",
    "nav.noCurrentLibraryCollections": "No current library folders",
    "nav.search": "Search",
    "nav.settings": "Settings",
    "nav.updates": "Access & Sharing",
    "nav.workspace": "Workspace",
    "settings.availableLibraries": "Available Libraries",
    "settings.archiveWorkspace": "Archive workspace",
    "settings.archiveWorkspaceDeferredReason": "Workspace archive/delete semantics are deferred for organization scope.",
    "settings.accessIdentity": "Permissions",
    "settings.assessment": "Assessment",
    "settings.categoryAndDigest": "Category And Digest",
    "settings.categoryPreferences": "Category preferences",
    "settings.collectionOrder": "Folder order",
    "settings.collectionSummary": "Folder summary",
    "settings.collections": "Folders",
    "settings.contractReadiness": "Contract readiness",
    "settings.createdAt": "Created at",
    "settings.currentRole": "Current role",
    "settings.currentLibraryLabel": "Current library",
    "settings.currentLibrarySummary": "Current library summary",
    "settings.currentSpaceId": "Current library ID",
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
    "settings.documentPermissions": "Document permissions",
    "settings.documentPermissionsManagedFromDocument": "Open Advanced permissions from the current document. Library Settings does not manage document grants.",
    "settings.documentSummary": "Document summary",
    "settings.email": "Email",
    "settings.drafts": "Drafts",
    "settings.centerHeading": "Settings",
    "settings.centerHeadingReady": "Workspace, personal, and organization settings are grouped by ownership and task surface.",
    "settings.contextSummary": "Settings context",
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
    "settings.libraryPermissionsBoundaryHelp": "This page explains ownership boundaries only. Workspace membership stays in Settings; document grants stay with each document.",
    "settings.manageInLibrary": "Manage in Library",
    "settings.members": "Members",
    "settings.membersInventory": "Members inventory",
    "settings.name": "Name",
    "settings.newDocumentInLibrary": "New document in Library",
    "settings.noCollections": "No folders are available for this library.",
    "settings.noDocuments": "No documents are available for this library.",
    "settings.noRowsReturned": "No rows returned",
    "settings.notFound": "Not found",
    "settings.notifications": "Notifications",
    "settings.numberFormat": "Number format",
    "settings.openLibrary": "Open Library",
    "settings.openWorkspaceMembers": "Open Workspace Members",
    "settings.openWorkspaceNotifications": "Open Workspace Notifications",
    "settings.organizationAssessment": "Organization Assessment",
    "settings.organizationAssessmentHelp": "Assessment keeps broader organization mutations deferred while profile rename is owner-gated.",
    "settings.organizationHeading": "Organization Settings Assessment",
    "settings.organizationHeadingReady": "Organization profile is live-backed. Profile rename is owner-gated; broader administration remains deferred.",
    "settings.organizationSettingsHeading": "Organization Settings",
    "settings.organizationSettingsReady": "Organization profile and inventory live above the current workspace. Organization members and provisioning mutations remain deferred.",
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
    "settings.preferences": "Preferences",
    "settings.personalSettingsHeading": "Personal Settings",
    "settings.personalSettingsReady": "Personal preferences apply to this browser session and are separate from workspace administration.",
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
    "settings.spaceId": "Library ID",
    "settings.spaceSettingsEntry": "Library Settings Entry",
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
    "settings.backendStatus": "Backend status",
    "settings.frontendStatus": "Frontend status",
    "settings.informationArchitecture": "Settings architecture",
    "settings.inventoryConflictMarked": "Conflict-marked",
    "settings.inventoryDefer": "Defer",
    "settings.inventoryHalfFinished": "Half-finished",
    "settings.inventoryKeep": "Keep in Settings",
    "settings.inventoryLibraryOperations": "Library operations",
    "settings.inventoryLiveMutation": "Live mutation",
    "settings.inventoryLiveRead": "Live read",
    "settings.inventoryMissing": "Missing contract",
    "settings.inventoryMove": "Move to task surface",
    "settings.inventoryOrganizationMembers": "Organization members inventory",
    "settings.inventoryOrganizationProfile": "Organization profile",
    "settings.inventoryPersonalLanguage": "Language and region",
    "settings.inventoryReadOnly": "Read-only",
    "settings.inventoryRemoveAction": "Remove action affordance",
    "settings.inventoryResourceShare": "Resource share",
    "settings.inventoryShouldMove": "Should move",
    "settings.inventorySplit": "Split into personal settings",
    "settings.inventoryStatic": "Static",
    "settings.inventorySystemSettings": "System / instance settings",
    "settings.inventoryWorkspaceMembers": "Workspace members",
    "settings.inventoryWorkspaceNotifications": "Workspace notification preferences",
    "settings.inventoryWorkspaceProfile": "Workspace profile",
    "settings.inventoryWorkspaceProvisioning": "Workspace provisioning",
    "settings.accessRequests": "Access requests",
    "settings.accessRequestsManagedInUpdates": "Access request notifications are reviewed in Updates when the backend sends them.",
    "settings.backendPermissionChecked": "Mutations remain checked by the backend permission service.",
    "settings.boundary": "Boundary",
    "settings.groups": "Groups",
    "settings.lastOwnerProtected": "Protected by existing backend constraints.",
    "settings.libraryOperations": "Library operations",
    "settings.libraryOperationsSurfaceHelp": "Routine folder and document work stays in Libraries; Settings shows summary and links only.",
    "settings.membersManagedInWorkspaceSettings": "Workspace members are managed in Workspace Settings > Members.",
    "settings.membersSurfaceHelp": "Member list, add member, role update, remove member, busy states, and backend errors stay on the Members surface.",
    "settings.notificationBoundaryHelp": "Current backend support covers resource watch or mute preferences plus access, sharing, and permission notifications. Ordinary document edit activity stays in Activity, not Updates.",
    "settings.openMembersSurface": "Open Members surface",
    "settings.personalPreferenceLocalHelp": "This is a personal browser preference shown here until a Personal Settings route exists.",
    "settings.recommendedSettingsClosure": "Recommended settings closure",
    "settings.resourcePreferences": "Resource preferences",
    "settings.resourceShare": "Resource share",
    "settings.resourceShareSurfaceHelp": "Share links remain resource-level actions from document or folder context; public-link behavior is unchanged here.",
    "settings.roleBoundaries": "Role boundaries",
    "settings.selectedCapabilities": "Selected capabilities",
    "settings.shareLinksPublicLinks": "Share links / public links",
    "settings.sortOrder": "sort",
    "settings.viewerManagementDisabled": "Management actions are disabled by the existing capability model.",
    "settings.workspaceProfile": "Workspace profile",
    "settings.workspaceProfileReadOnlyHelp": "Workspace profile updates remain read-only because no workspace profile update API contract is exposed in the inspected backend.",
    "settings.loadingNotificationPreferences": "Loading notification preferences.",
    "settings.notificationPreferenceApiFailed": "Notification preference API failed.",
    "settings.notificationPreferenceForbidden": "Notification preference access is unavailable for this user.",
    "settings.notificationPreferenceHelp": "Choose the current workspace default for access, sharing, and permission notifications. Resource-specific watch and mute state remains listed below.",
    "settings.preferenceUpdated": "Preference updated.",
    "settings.preferenceUpdateUnavailable": "API and workspace id are required to update notification preferences.",
    "settings.recommendation": "Recommendation",
    "settings.savingPreference": "Saving preference...",
    "settings.scope": "Scope",
    "settings.scopeDeferred": "Deferred",
    "settings.scopePersonal": "Personal",
    "settings.scopeResource": "Resource",
    "settings.secondaryNavigation": "Settings sections",
    "settings.workspaceNotificationDefault": "Default",
    "settings.workspaceNotificationMuted": "Mute workspace",
    "settings.workspaceNotificationPreference": "Workspace notification default",
    "settings.workspaceNotificationWatched": "Watch workspace",
    "share.clipboardBlocked": "Clipboard access was blocked. Copy the link manually.",
    "share.clipboardUnavailable": "Clipboard is unavailable in this browser.",
    "share.createInternalLink": "Create internal link",
    "share.internalLinkCreated": "Internal share link created. Copy the URL or token now.",
    "share.linkCreated": "Share link created.",
    "share.linkRevoked": "Share link revoked.",
    "share.nothingToCopy": "Nothing to copy yet.",
    "share.openFromDocument": "Open Share from a document to manage live permission links.",
    "share.operationInProgress": "Finish the current share operation first.",
    "share.shareApiUnavailable": "Share API unavailable for this document.",
    "share.tokenCopied": "Token copied.",
    "share.urlCopied": "URL copied.",
    "topbar.account": "Northstar account",
    "topbar.apiNotConfigured": "API not configured",
    "topbar.currentWorkspace": "Current workspace",
    "topbar.exportJson": "Export workspace JSON",
    "topbar.importJson": "Import workspace JSON",
    "topbar.libraries": "Libraries",
    "topbar.mfaEnabled": "MFA enabled",
    "topbar.mfaNotEnabled": "MFA not enabled",
    "topbar.organizationSettings": "Organization settings",
    "topbar.personalSettings": "Personal settings",
    "topbar.runSearch": "Run search",
    "topbar.saved": "Saved",
    "topbar.searchNorthstar": "Search Northstar",
    "topbar.signOut": "Sign out",
    "topbar.signInRequired": "Sign in required",
    "topbar.workspaceListUnavailable": "Workspace list unavailable for this session.",
    "topbar.workspaceSwitcher": "Workspace switcher",
    "topbar.workspaceSwitchingDeferred": "Workspace switching is visible but not supported by this frontend route yet.",
    "topbar.workspaceHome": "Workspace Home",
    "updates.access": "Access / approvals",
    "updates.accessRequests": "Access requests",
    "updates.all": "All",
    "updates.expiry": "Expiry",
    "updates.expiring": "Expiring",
    "updates.failed": "Failed",
    "updates.failedInvites": "Failed invites",
    "updates.grantsGroups": "Grants & groups",
    "updates.invites": "Invites",
    "updates.links": "Links",
    "updates.heading": "Review access requests, grants, group access, share links, email invites, and expiry alerts.",
    "updates.latest": "Latest",
    "updates.markAllRead": "Mark all read",
    "updates.mutedFolders": "Muted folders",
    "updates.newestFirst": "Newest first",
    "updates.noNotifications": "No notifications",
    "updates.noNotificationsForFilter": "No notifications match this filter",
    "updates.pendingReview": "Pending review",
    "updates.shown": "{count} shown",
    "updates.sharing": "Sharing links & invites",
    "updates.summary": "Summary",
    "updates.title": "Access & Sharing",
    "updates.totalNotifications": "{count} total notifications",
    "updates.unread": "Unread",
    "updates.unreadLabel": "unread",
    "updates.watchedDocuments": "Watched documents",
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
    "common.languageChinese": "\u7b80\u4f53\u4e2d\u6587",
    "common.loading": "加载中",
    "common.notConnected": "未连接",
    "common.no": "否",
    "common.open": "打开",
    "common.retry": "重试",
    "common.unavailable": "不可用",
    "common.waiting": "等待中",
    "common.workspace": "工作区",
    "common.yes": "是",
    "home.activeConversations": "\u8bbf\u95ee\u4e0e\u5206\u4eab\u66f4\u65b0",
    "home.allActivity": "全部动态",
    "home.goodMorning": "早上好，{workspaceName}",
    "home.logNote": "记录笔记",
    "home.moreActions": "更多操作",
    "home.newDecision": "新建决策",
    "home.newDocument": "新建文档",
    "home.notificationDigest": "\u8bbf\u95ee\u901a\u77e5\u6458\u8981",
    "home.quickActions": "快捷操作",
    "home.recentConversationsAndDecisions": "\u8bbf\u95ee\u4e0e\u5206\u4eab\u4e0a\u4e0b\u6587",
    "home.recentlyTouched": "最近触达",
    "home.requestAccess": "请求访问",
    "home.shareUpdate": "分享更新",
    "home.teamActivity": "最近文档动态",
    "home.today": "今天",
    "home.topContributors": "主要贡献者",
    "home.waitingOnYou": "待你处理",
    "home.workspaceMembers": "工作区成员",
    "home.workspaceSignals": "工作区信号",
    "library.allCollections": "\u5168\u90e8\u6587\u4ef6\u5939",
    "library.collection": "\u6587\u4ef6\u5939",
    "library.collectionName": "\u6587\u4ef6\u5939\u540d\u79f0",
    "library.createCollection": "\u521b\u5efa\u6587\u4ef6\u5939",
    "library.createCollectionDescription": "\u5728\u5f53\u524d\u8d44\u6599\u5e93\u4e2d\u521b\u5efa\u771f\u5b9e\u540e\u7aef\u652f\u6301\u7684\u6587\u4ef6\u5939\u3002",
    "library.creating": "创建中",
    "library.deleteCollection": "\u5220\u9664\u6587\u4ef6\u5939",
    "library.deleteDocument": "删除文档",
    "library.deleteEmptyCollectionDescription": "\u8fd9\u4e2a\u7a7a\u6587\u4ef6\u5939\u5c06\u4ece\u5f53\u524d\u8d44\u6599\u5e93\u79fb\u9664\u3002",
    "library.deleteNonEmptyCollectionDescription": "\u8fd9\u4e2a\u6587\u4ef6\u5939\u4e2d\u4ecd\u6709 {count} \u4e2a\u6587\u6863\u3002API \u4f1a\u963b\u6b62\u5220\u9664\uff0c\u76f4\u5230\u6587\u4ef6\u5939\u4e3a\u7a7a\u3002",
    "library.documents": "文档",
    "library.library": "资料库",
    "library.moveCollectionDown": "下移 {title}",
    "library.moveCollectionUp": "上移 {title}",
    "library.newCollection": "\u65b0\u5efa\u6587\u4ef6\u5939",
    "library.newDocument": "新建文档",
    "library.noCollections": "\u5f53\u524d\u8d44\u6599\u5e93\u6682\u65e0\u6587\u4ef6\u5939\u3002\u8bf7\u5148\u521b\u5efa\u6587\u4ef6\u5939\uff0c\u518d\u6dfb\u52a0\u6587\u6863\u3002",
    "library.noDocuments": "\u5f53\u524d\u8d44\u6599\u5e93\u6682\u65e0\u6587\u6863\u3002\u9009\u62e9\u4e00\u4e2a\u6587\u4ef6\u5939\u540e\u521b\u5efa\u6587\u6863\u3002",
    "library.renameCollection": "\u91cd\u547d\u540d\u6587\u4ef6\u5939",
    "library.renameCollectionDescription": "\u91cd\u547d\u540d\u4f1a\u66f4\u65b0\u6b64\u5de5\u4f5c\u533a\u6240\u6709\u4eba\u770b\u5230\u7684\u6587\u4ef6\u5939\u6807\u9898\u3002",
    "library.saveName": "保存名称",
    "library.searchPlaceholder": "\u641c\u7d22\u6587\u6863\u548c\u6587\u4ef6\u5939",
    "library.spaceSettings": "\u8d44\u6599\u5e93\u8bbe\u7f6e",
    "library.working": "处理中...",
    "nav.currentLibraryCollections": "\u5f53\u524d\u8d44\u6599\u5e93\u6587\u4ef6\u5939",
    "nav.home": "主页",
    "nav.libraries": "资料库",
    "nav.members": "成员",
    "nav.noCurrentLibraryCollections": "\u6682\u65e0\u5f53\u524d\u8d44\u6599\u5e93\u6587\u4ef6\u5939",
    "nav.search": "搜索",
    "nav.settings": "设置",
    "nav.updates": "访问与分享",
    "nav.workspace": "工作区",
    "settings.availableLibraries": "可用资料库",
    "settings.archiveWorkspace": "归档工作区",
    "settings.archiveWorkspaceDeferredReason": "组织范围的工作区归档 / 删除语义暂未定义。",
    "settings.accessIdentity": "\u6743\u9650",
    "settings.assessment": "评估",
    "settings.categoryAndDigest": "分类与摘要",
    "settings.categoryPreferences": "分类偏好",
    "settings.collectionOrder": "\u6587\u4ef6\u5939\u987a\u5e8f",
    "settings.collections": "\u6587\u4ef6\u5939",
    "settings.contractReadiness": "合同就绪度",
    "settings.createdAt": "创建时间",
    "settings.currentRole": "当前角色",
    "settings.currentLibraryLabel": "当前资料库",
    "settings.currentSpaceId": "\u5f53\u524d\u8d44\u6599\u5e93 ID",
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
    "settings.centerHeading": "\u8bbe\u7f6e",
    "settings.centerHeadingReady": "\u5de5\u4f5c\u533a\u3001\u4e2a\u4eba\u548c\u7ec4\u7ec7\u8bbe\u7f6e\u5df2\u6309\u5f52\u5c5e\u8fb9\u754c\u548c\u4efb\u52a1\u8868\u9762\u5206\u7ec4\u3002",
    "settings.contextSummary": "\u8bbe\u7f6e\u4e0a\u4e0b\u6587",
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
    "settings.membersInventory": "\u6210\u5458\u6e05\u5355",
    "settings.name": "名称",
    "settings.newDocumentInLibrary": "在资料库中新建文档",
    "settings.noCollections": "\u6b64\u8d44\u6599\u5e93\u6682\u65e0\u6587\u4ef6\u5939\u3002",
    "settings.noDocuments": "此资料库暂无文档。",
    "settings.noRowsReturned": "没有返回数据",
    "settings.notFound": "未找到",
    "settings.notifications": "通知",
    "settings.numberFormat": "数字格式",
    "settings.openLibrary": "打开资料库",
    "settings.openWorkspaceMembers": "打开工作区成员",
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
    "settings.organizationSettingsHeading": "\u7ec4\u7ec7\u8bbe\u7f6e",
    "settings.organizationSettingsReady": "\u7ec4\u7ec7\u8d44\u6599\u548c\u6e05\u5355\u5c5e\u4e8e\u5f53\u524d\u5de5\u4f5c\u533a\u4e4b\u4e0a\u7684\u7ba1\u7406\u8fb9\u754c\u3002\u7ec4\u7ec7\u6210\u5458\u548c\u5de5\u4f5c\u533a\u5f00\u901a\u53d8\u66f4\u4ecd\u6682\u7f13\u3002",
    "settings.orgSsoScimOwnership": "SSO / SCIM 归属",
    "settings.orgWorkspaceProvisioning": "工作区开通",
    "settings.organizationReadOnlyHelp": "组织资料重命名仅限拥有者。其他组织变更仍然暂缓。",
    "settings.organizationWorkspaces": "组织工作区",
    "settings.overview": "概览",
    "settings.permissions": "权限",
    "settings.plan": "套餐",
    "settings.preferences": "\u504f\u597d",
    "settings.personalSettingsHeading": "\u4e2a\u4eba\u8bbe\u7f6e",
    "settings.personalSettingsReady": "\u4e2a\u4eba\u504f\u597d\u53ea\u5e94\u7528\u4e8e\u5f53\u524d\u6d4f\u89c8\u5668\u4f1a\u8bdd\uff0c\u4e0e\u5de5\u4f5c\u533a\u7ba1\u7406\u5206\u5f00\u3002",
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
    "settings.spaceId": "\u8d44\u6599\u5e93 ID",
    "settings.spaceSettingsEntry": "\u8d44\u6599\u5e93\u8bbe\u7f6e\u5165\u53e3",
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
    "settings.backendStatus": "\u540e\u7aef\u72b6\u6001",
    "settings.frontendStatus": "\u524d\u7aef\u72b6\u6001",
    "settings.informationArchitecture": "\u8bbe\u7f6e\u4fe1\u606f\u67b6\u6784",
    "settings.inventoryConflictMarked": "\u6709\u51b2\u7a81\u6807\u8bb0",
    "settings.inventoryDefer": "\u6682\u7f13",
    "settings.inventoryHalfFinished": "\u534a\u6210\u54c1",
    "settings.inventoryKeep": "\u4fdd\u7559\u5728\u8bbe\u7f6e",
    "settings.inventoryLibraryOperations": "\u8d44\u6599\u5e93\u64cd\u4f5c",
    "settings.inventoryLiveMutation": "\u53ef\u771f\u5b9e\u53d8\u66f4",
    "settings.inventoryLiveRead": "\u53ef\u771f\u5b9e\u8bfb\u53d6",
    "settings.inventoryMissing": "\u7f3a\u5c11\u5408\u540c",
    "settings.inventoryMove": "\u79fb\u5230\u4efb\u52a1\u754c\u9762",
    "settings.inventoryOrganizationMembers": "\u7ec4\u7ec7\u6210\u5458\u6e05\u5355",
    "settings.inventoryOrganizationProfile": "\u7ec4\u7ec7\u8d44\u6599",
    "settings.inventoryPersonalLanguage": "\u8bed\u8a00\u4e0e\u533a\u57df",
    "settings.inventoryReadOnly": "\u53ea\u8bfb",
    "settings.inventoryRemoveAction": "\u79fb\u9664\u64cd\u4f5c\u5165\u53e3",
    "settings.inventoryResourceShare": "\u8d44\u6e90\u5206\u4eab",
    "settings.inventoryShouldMove": "\u5e94\u8fc1\u51fa",
    "settings.inventorySplit": "\u62c6\u5230\u4e2a\u4eba\u8bbe\u7f6e",
    "settings.inventoryStatic": "\u9759\u6001",
    "settings.inventorySystemSettings": "\u7cfb\u7edf / \u5b9e\u4f8b\u8bbe\u7f6e",
    "settings.inventoryWorkspaceMembers": "\u5de5\u4f5c\u533a\u6210\u5458",
    "settings.inventoryWorkspaceNotifications": "\u5de5\u4f5c\u533a\u901a\u77e5\u504f\u597d",
    "settings.inventoryWorkspaceProfile": "\u5de5\u4f5c\u533a\u8d44\u6599",
    "settings.inventoryWorkspaceProvisioning": "\u5de5\u4f5c\u533a\u5f00\u901a",
    "settings.accessRequests": "\u8bbf\u95ee\u8bf7\u6c42",
    "settings.accessRequestsManagedInUpdates": "\u540e\u7aef\u53d1\u51fa\u8bbf\u95ee\u8bf7\u6c42\u901a\u77e5\u65f6\uff0c\u5728 Updates \u4e2d\u5904\u7406\u3002",
    "settings.backendPermissionChecked": "\u53d8\u66f4\u4ecd\u7531\u540e\u7aef\u6743\u9650\u670d\u52a1\u68c0\u67e5\u3002",
    "settings.boundary": "\u8fb9\u754c",
    "settings.collectionSummary": "\u6587\u4ef6\u5939\u6458\u8981",
    "settings.currentLibrarySummary": "\u5f53\u524d\u8d44\u6599\u5e93\u6458\u8981",
    "settings.documentPermissions": "\u6587\u6863\u6743\u9650",
    "settings.documentPermissionsManagedFromDocument": "\u8bf7\u4ece\u5f53\u524d\u6587\u6863\u6253\u5f00\u9ad8\u7ea7\u6743\u9650\uff1b\u8d44\u6599\u5e93\u8bbe\u7f6e\u4e0d\u7ba1\u7406\u6587\u6863\u6388\u6743\u3002",
    "settings.documentSummary": "\u6587\u6863\u6458\u8981",
    "settings.groups": "\u7ec4",
    "settings.lastOwnerProtected": "\u73b0\u6709\u540e\u7aef\u7ea6\u675f\u4fdd\u62a4\u6700\u540e\u4e00\u4f4d\u62e5\u6709\u8005\u3002",
    "settings.libraryOperations": "\u8d44\u6599\u5e93\u64cd\u4f5c",
    "settings.libraryOperationsSurfaceHelp": "\u65e5\u5e38\u6587\u4ef6\u5939\u548c\u6587\u6863\u5de5\u4f5c\u4fdd\u7559\u5728\u8d44\u6599\u5e93\u754c\u9762\uff1b\u8bbe\u7f6e\u4ec5\u663e\u793a\u6458\u8981\u548c\u94fe\u63a5\u3002",
    "settings.libraryPermissionsBoundaryHelp": "\u6b64\u9875\u53ea\u8bf4\u660e\u6743\u9650\u5f52\u5c5e\u8fb9\u754c\u3002\u5de5\u4f5c\u533a\u6210\u5458\u5728 Settings \u7ba1\u7406\uff1b\u6587\u6863\u6388\u6743\u5728\u6587\u6863\u4e0a\u4e0b\u6587\u7ba1\u7406\u3002",
    "settings.membersManagedInWorkspaceSettings": "\u5de5\u4f5c\u533a\u6210\u5458\u5728 Workspace Settings > Members \u4e2d\u7ba1\u7406\u3002",
    "settings.membersSurfaceHelp": "\u6210\u5458\u5217\u8868\u3001\u6dfb\u52a0\u6210\u5458\u3001\u89d2\u8272\u66f4\u65b0\u3001\u79fb\u9664\u6210\u5458\u3001\u5fd9\u788c\u72b6\u6001\u548c\u540e\u7aef\u9519\u8bef\u90fd\u4fdd\u7559\u5728\u6210\u5458\u754c\u9762\u3002",
    "settings.notificationBoundaryHelp": "\u5f53\u524d\u540e\u7aef\u652f\u6301\u8d44\u6e90\u5173\u6ce8\u6216\u9759\u97f3\u504f\u597d\uff0c\u4ee5\u53ca\u8bbf\u95ee\u3001\u5206\u4eab\u548c\u6743\u9650\u901a\u77e5\u3002\u666e\u901a\u6587\u6863\u7f16\u8f91\u52a8\u6001\u4fdd\u7559\u5728 Activity\uff0c\u4e0d\u8fdb\u5165 Updates\u3002",
    "settings.openMembersSurface": "\u6253\u5f00\u6210\u5458\u754c\u9762",
    "settings.personalPreferenceLocalHelp": "\u8fd9\u662f\u4e2a\u4eba\u6d4f\u89c8\u5668\u504f\u597d\uff1b\u5728\u72ec\u7acb\u4e2a\u4eba\u8bbe\u7f6e\u8def\u7531\u5b58\u5728\u524d\u6682\u65f6\u653e\u5728\u8fd9\u91cc\u3002",
    "settings.recommendedSettingsClosure": "\u5efa\u8bae\u7684\u8bbe\u7f6e\u6536\u53e3",
    "settings.resourcePreferences": "\u8d44\u6e90\u504f\u597d",
    "settings.resourceShare": "\u8d44\u6e90\u5206\u4eab",
    "settings.resourceShareSurfaceHelp": "\u5206\u4eab\u94fe\u63a5\u4ecd\u662f\u6587\u6863\u6216\u6587\u4ef6\u5939\u4e0a\u4e0b\u6587\u4e2d\u7684\u8d44\u6e90\u7ea7\u64cd\u4f5c\uff1b\u8fd9\u91cc\u4e0d\u6539\u53d8 public-link \u884c\u4e3a\u3002",
    "settings.roleBoundaries": "\u89d2\u8272\u8fb9\u754c",
    "settings.selectedCapabilities": "\u9009\u4e2d\u80fd\u529b",
    "settings.shareLinksPublicLinks": "\u5206\u4eab\u94fe\u63a5 / public links",
    "settings.sortOrder": "\u6392\u5e8f",
    "settings.viewerManagementDisabled": "\u73b0\u6709\u80fd\u529b\u6a21\u578b\u4f1a\u7981\u7528\u7ba1\u7406\u64cd\u4f5c\u3002",
    "settings.workspaceProfile": "\u5de5\u4f5c\u533a\u8d44\u6599",
    "settings.workspaceProfileReadOnlyHelp": "\u5df2\u68c0\u67e5\u7684\u540e\u7aef\u6ca1\u6709\u66b4\u9732\u5de5\u4f5c\u533a\u8d44\u6599\u66f4\u65b0 API \u5408\u540c\uff0c\u56e0\u6b64\u5de5\u4f5c\u533a\u8d44\u6599\u4fdd\u6301\u53ea\u8bfb\u3002",
    "settings.loadingNotificationPreferences": "\u6b63\u5728\u52a0\u8f7d\u901a\u77e5\u504f\u597d\u3002",
    "settings.notificationPreferenceApiFailed": "\u901a\u77e5\u504f\u597d API \u8bf7\u6c42\u5931\u8d25\u3002",
    "settings.notificationPreferenceForbidden": "\u5f53\u524d\u7528\u6237\u65e0\u6cd5\u8bbf\u95ee\u901a\u77e5\u504f\u597d\u3002",
    "settings.notificationPreferenceHelp": "\u9009\u62e9\u5f53\u524d\u5de5\u4f5c\u533a\u7684\u8bbf\u95ee\u3001\u5206\u4eab\u548c\u6743\u9650\u901a\u77e5\u9ed8\u8ba4\u72b6\u6001\u3002\u8d44\u6e90\u7ea7\u5173\u6ce8\u548c\u9759\u97f3\u72b6\u6001\u4fdd\u7559\u5728\u4e0b\u65b9\u5217\u8868\u3002",
    "settings.preferenceUpdated": "\u504f\u597d\u5df2\u66f4\u65b0\u3002",
    "settings.preferenceUpdateUnavailable": "\u9700\u8981 API \u548c\u5de5\u4f5c\u533a ID \u624d\u80fd\u66f4\u65b0\u901a\u77e5\u504f\u597d\u3002",
    "settings.recommendation": "\u5efa\u8bae",
    "settings.savingPreference": "\u6b63\u5728\u4fdd\u5b58\u504f\u597d...",
    "settings.scope": "\u8303\u56f4",
    "settings.scopeDeferred": "\u6682\u7f13",
    "settings.scopePersonal": "\u4e2a\u4eba",
    "settings.scopeResource": "\u8d44\u6e90",
    "settings.secondaryNavigation": "\u8bbe\u7f6e\u5206\u7ec4",
    "settings.workspaceNotificationDefault": "\u9ed8\u8ba4",
    "settings.workspaceNotificationMuted": "\u9759\u97f3\u5de5\u4f5c\u533a",
    "settings.workspaceNotificationPreference": "\u5de5\u4f5c\u533a\u901a\u77e5\u9ed8\u8ba4\u503c",
    "settings.workspaceNotificationWatched": "\u5173\u6ce8\u5de5\u4f5c\u533a",
    "share.clipboardBlocked": "\u526a\u8d34\u677f\u8bbf\u95ee\u88ab\u963b\u6b62\uff0c\u8bf7\u624b\u52a8\u590d\u5236\u94fe\u63a5\u3002",
    "share.clipboardUnavailable": "\u5f53\u524d\u6d4f\u89c8\u5668\u4e0d\u652f\u6301\u526a\u8d34\u677f\u3002",
    "share.createInternalLink": "\u521b\u5efa\u5185\u90e8\u94fe\u63a5",
    "share.internalLinkCreated": "\u5185\u90e8\u5206\u4eab\u94fe\u63a5\u5df2\u521b\u5efa\uff0c\u8bf7\u73b0\u5728\u590d\u5236 URL \u6216 token\u3002",
    "share.linkCreated": "\u5206\u4eab\u94fe\u63a5\u5df2\u521b\u5efa\u3002",
    "share.linkRevoked": "\u5206\u4eab\u94fe\u63a5\u5df2\u64a4\u9500\u3002",
    "share.nothingToCopy": "\u6682\u65e0\u53ef\u590d\u5236\u5185\u5bb9\u3002",
    "share.openFromDocument": "\u8bf7\u4ece\u6587\u6863\u6253\u5f00\u5206\u4eab\uff0c\u4ee5\u7ba1\u7406\u5b9e\u65f6\u6743\u9650\u94fe\u63a5\u3002",
    "share.operationInProgress": "\u8bf7\u5148\u5b8c\u6210\u5f53\u524d\u5206\u4eab\u64cd\u4f5c\u3002",
    "share.shareApiUnavailable": "\u5f53\u524d\u6587\u6863\u4e0d\u53ef\u7528\u5206\u4eab API\u3002",
    "share.tokenCopied": "Token \u5df2\u590d\u5236\u3002",
    "share.urlCopied": "URL \u5df2\u590d\u5236\u3002",
    "topbar.account": "Northstar 账户",
    "topbar.apiNotConfigured": "API 未配置",
    "topbar.currentWorkspace": "\u5f53\u524d\u5de5\u4f5c\u533a",
    "topbar.exportJson": "导出工作区 JSON",
    "topbar.importJson": "导入工作区 JSON",
    "topbar.libraries": "资料库",
    "topbar.mfaEnabled": "已启用 MFA",
    "topbar.mfaNotEnabled": "未启用 MFA",
    "topbar.organizationSettings": "\u7ec4\u7ec7\u8bbe\u7f6e",
    "topbar.personalSettings": "\u4e2a\u4eba\u8bbe\u7f6e",
    "topbar.runSearch": "执行搜索",
    "topbar.saved": "已保存",
    "topbar.searchNorthstar": "搜索 Northstar",
    "topbar.signOut": "\u9000\u51fa\u767b\u5f55",
    "topbar.signInRequired": "需要登录",
    "topbar.workspaceListUnavailable": "\u5f53\u524d\u4f1a\u8bdd\u65e0\u6cd5\u8bfb\u53d6\u5de5\u4f5c\u533a\u5217\u8868\u3002",
    "topbar.workspaceSwitcher": "\u5de5\u4f5c\u533a\u5207\u6362\u5668",
    "topbar.workspaceSwitchingDeferred": "\u5de5\u4f5c\u533a\u5207\u6362\u53ef\u89c1\uff0c\u4f46\u5f53\u524d\u524d\u7aef\u8def\u7531\u5c1a\u4e0d\u652f\u6301\u771f\u5b9e\u5207\u6362\u3002",
    "topbar.workspaceHome": "工作区主页",
    "updates.access": "访问 / 审批",
    "updates.accessRequests": "访问请求",
    "updates.all": "全部",
    "updates.expiry": "过期",
    "updates.expiring": "即将过期",
    "updates.failed": "失败",
    "updates.failedInvites": "邀请失败",
    "updates.grantsGroups": "授权与组",
    "updates.invites": "邀请",
    "updates.links": "链接",
    "updates.heading": "查看访问请求、权限授权、组访问、分享链接、邮件邀请和过期提醒。",
    "updates.latest": "最新",
    "updates.markAllRead": "全部标为已读",
    "updates.mutedFolders": "已静音文件夹",
    "updates.newestFirst": "最新优先",
    "updates.noNotifications": "暂无通知",
    "updates.noNotificationsForFilter": "没有符合当前筛选的通知",
    "updates.pendingReview": "待处理",
    "updates.shown": "显示 {count} 条",
    "updates.sharing": "分享链接与邀请",
    "updates.summary": "摘要",
    "updates.title": "访问与分享",
    "updates.totalNotifications": "共 {count} 条通知",
    "updates.unread": "未读",
    "updates.unreadLabel": "未读",
    "updates.watchedDocuments": "已关注文档",
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
  tab: "access" | "all" | "expiry" | "failed" | "grants" | "invites" | "links" | "sharing" | "unread",
) {
  switch (tab) {
    case "unread":
      return t(locale, "updates.unread");
    case "access":
      return t(locale, "updates.accessRequests");
    case "grants":
      return t(locale, "updates.grantsGroups");
    case "sharing":
      return t(locale, "updates.sharing");
    case "links":
      return t(locale, "updates.links");
    case "invites":
      return t(locale, "updates.invites");
    case "expiry":
      return t(locale, "updates.expiry");
    case "failed":
      return t(locale, "updates.failed");
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
