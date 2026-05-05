# Permission System Completion Readiness

## Summary

- Permission backend readiness is high for core workspace RBAC, scoped document/collection grants, effective permission checks, groups, access requests, audit, share links, email invites, IAM sync foundations, and permission notifications.
- Completion is blocked by conflict-marked public-link behavior: public collection links, public `linkMode`, public-link source drift, and anonymous access boundaries must not be silently resolved.
- Missing or deferred pieces remain: notification preferences / watched / muted persistence, share-link and invite notification fan-out, group grant fan-out, SCIM server endpoints, real IdP login support, and MFA/recent-auth enforcement.
- Existing tests cover many current permission behaviors, including public-link boundary tests, but no validation commands were run in this investigation.
- Code modified: No application, backend, frontend, test, migration, package, or project code was changed.

## Scope

- Investigation only.
- Backend root inspected: `services/api`.
- Permission system readiness was classified from current code inspection where inspected.
- Public-link conflicts were preserved and not resolved.
- Frontend implementation was not inspected; frontend workflow readiness is therefore backend-API readiness only unless stated otherwise.

## Docs Read

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/03-required-reading-order.md`
- `docs/agent/04-implementation-protocol.md`
- `docs/agent/05-validation-protocol.md`
- `docs/agent/06-final-report-format.md`
- `docs/agent/skills/permissions.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/backend-clean-architecture.md`
- `docs/agent/skills/data-model-migrations.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers`
- `services/api/src/Northstar.Application/Security`
- `services/api/src/Northstar.Application/Workspaces`
- `services/api/src/Northstar.Application/DependencyInjection.cs`
- `services/api/src/Northstar.Contracts/Security`
- `services/api/src/Northstar.Contracts/Workspaces`
- `services/api/src/Northstar.Domain/Security`
- `services/api/src/Northstar.Domain/Workspaces`
- `services/api/src/Northstar.Domain/Users`
- `services/api/src/Northstar.Infrastructure/Security`
- `services/api/src/Northstar.Infrastructure/Workspaces`
- `services/api/src/Northstar.Infrastructure/Persistence/NorthstarDbContext.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations`
- `services/api/src/Northstar.Infrastructure/Persistence/Migrations`
- `services/api/src/Northstar.Infrastructure/DependencyInjection.cs`
- `services/api/tests`

## Component Status Matrix

| Component | Status | Evidence | Notes |
|---|---|---|---|
| Workspace RBAC | implemented | `WorkspaceMembersService`, `WorkspaceAccessService`, `WorkspaceMember`, `WorkspacesController` member routes | Includes last-owner protections in service/tests. |
| Scoped document and collection permissions | implemented | `ResourceAccessPolicy`, `ResourceAccessGrant`, `ResourcePermissionManagementService`, `EffectivePermissionService` | Handles document/collection policies, inheritance, restricted mode, direct and group grants. |
| Effective permission service | implemented | `EffectivePermissionService`, `EffectivePermissionQueryService`, `IScopedResourceAccessService` | Central service is wired in `Northstar.Application/DependencyInjection.cs`. |
| Groups and group memberships | implemented | `WorkspaceGroupService`, `WorkspaceGroup`, `WorkspaceGroupMember`, `WorkspacesController` group routes | Static groups implemented; IAM dynamic groups are read-only through normal group APIs. |
| Direct grants and temporary grants | implemented | `ResourcePermissionManagementService`, `ResourceAccessGrant.ExpiresAt`, `PermissionExpiryNotificationProcessor` | Expired/revoked grants are ignored; expiry notification processor exists. |
| Access requests | implemented | `AccessRequestService`, `AccessRequest`, `PermissionsController` access request routes | Create/review/cancel/list workflow exists with audit and notifications. |
| Permission notifications | implemented | `PermissionNotificationService`, `NotificationsController`, `PermissionNotification` | Read/list/read-all operations exist. |
| Notification preferences / watched / muted persistence | missing | Not found during inspection for preference, watched, or muted entities/services/routes | Documented as deferred; no schema found. |
| Share links | implemented | `ShareLinkService`, `ShareLinksController`, `PermissionsController` share-link routes, `ShareLink` | Authenticated workspace/external share-link flows exist. |
| External authenticated links | implemented | `ShareLinkService.ResolveShareLinkAsync`, `EffectivePermissionService` external share-link source | Requires authenticated user email match and external link mode. |
| Email invites | partially implemented | `EmailInviteService`, `ResourceEmailInvite`, `NoopEmailInviteDeliveryService` | Backend workflow and token handling exist; production provider delivery and fan-out remain missing. |
| Public document links | conflict-marked | `PublicShareLinksController.GetDocument`, `ShareLinkService.GetPublicShareDocumentAsync`, `PermissionPublicShareOptions` | Code exists behind options, but public-link behavior is conflict-marked in governance docs. |
| Public collection links | conflict-marked | `PublicShareLinksController.GetCollection`, `ShareLinkService.GetPublicShareCollectionAsync`, `AddPublicCollectionLinksAndLinkPasswordsPhase11` migration | Code exists, but public collection links are explicitly conflict-marked. |
| `linkMode = public` | conflict-marked | `ShareLinkService.EnsureLinkPolicyAsync` can set public; `ResourcePermissionManagementService.UpdatePolicyAsync` rejects public policy patch | Conflicting behavior is documented and not resolved here. |
| Public anonymous access boundary | conflict-marked | `[AllowAnonymous] PublicShareLinksController`; `ShareLinkService.GetActivePublicLinkAsync` checks feature flag, public audience, role, expiry, password, policy mode | Boundary exists in code but remains conflict-marked. |
| Invite/share-link notification fan-out | missing | `KnowledgeApiTests.Phase11DeferredBoundaries_AreDocumentedAndDoNotEmitShareInviteFanout`; not found in `ShareLinkService` or `EmailInviteService` | Deferred by docs and code tests. |
| Group grant fan-out | missing | `ResourcePermissionManagementService.AddGrantNotificationAsync` only emits for `SubjectTypes.User` | Group membership notifications exist, but group grant fan-out is absent. |
| SCIM server endpoints | documented only | `docs/PERMISSION_SYSTEM_CONTRACT.md`; not found during code search for SCIM/Scim routes or services | IAM sync exists, but SCIM server API is absent. |
| IAM sync foundations | implemented | `IamSyncService`, `IIamSyncRepository`, `EfIamSyncRepository`, `WorkspacesController` `POST workspaces/{workspaceId}/iam/sync` | Creates/maps external users/groups/members and audits changes. |
| Real IdP login UI/backend support | partially implemented | `User.ExternalProvider`, `User.ExternalSubjectId`, `IamSyncService` | External identity mapping exists; real IdP login flow not found. |
| MFA/recent-auth enforcement | missing | Not found during search for MFA/recent-auth/recent auth state | Contract marks as deferred. |
| Audit requirements | implemented | `PermissionAuditService`, `PermissionAuditEvent`, audit writes in management/share/invite/access/IAM/group services | Audit repository and migrations exist. |
| Token hashing and raw-token exposure rules | implemented | `ShareLinkTokenService`, `ShareLink.TokenHash`, `ResourceEmailInvite.TokenHash`, API tests for storing only token hash | Raw tokens are returned at creation/resolve URLs, not stored in entity snapshots inspected. |
| Frontend permission mutation workflow readiness | not verified | Frontend code was not inspected in this backend readiness task | Backend APIs exist for mutation workflows; UI readiness is outside inspected scope. |
| Tests and migration coverage | partially implemented | `KnowledgeApiTests`, `EffectivePermissionServiceTests`, `AccessRequestServiceTests`, `ResourceAccessPermissionTests`, migration tests | Strong existing coverage, but no tests for missing preference/fan-out/SCIM/MFA implementations. |

## Implemented Pieces

- Workspace RBAC and member management: `WorkspaceMembersService`, `WorkspaceAccessService`, `WorkspaceMember`, `WorkspacesController`.
- Effective permission calculation: `EffectivePermissionService` centralizes workspace, collection, document, direct grant, group grant, share-link, email-invite, inheritance, and restricted-mode behavior.
- Scoped resource policy and grant management: `ResourcePermissionManagementService`, `ResourceAccessPolicy`, `ResourceAccessGrant`.
- Workspace groups and group membership management: `WorkspaceGroupService`, `WorkspaceGroup`, `WorkspaceGroupMember`.
- Access requests: `AccessRequestService`, `AccessRequest`, controller routes for create/list/review/cancel.
- Permission notifications: `PermissionNotificationService`, `NotificationsController`, `PermissionNotification`.
- Audit logging: `PermissionAuditService`, `PermissionAuditEvent`, `PermissionAuditActions`.
- Share links: `ShareLinkService`, `ShareLink`, `ShareLinksController`, share-link routes under `PermissionsController`.
- Email invites: `EmailInviteService`, `ResourceEmailInvite`, delivery-status fields, invite accept/revoke/resolve routes.
- IAM sync foundations: `IamSyncService`, `IIamSyncRepository`, `EfIamSyncRepository`, `WorkspacesController` IAM sync route.
- Token hashing: `ShareLinkTokenService`, token hash fields on `ShareLink` and `ResourceEmailInvite`.

## Partially Implemented Pieces

- Email invites are partially implemented because the backend workflow exists, but infrastructure uses `NoopEmailInviteDeliveryService`; production SMTP/provider delivery remains deferred.
- Real IdP support is partially implemented because external identity fields and IAM mapping exist, but real login/IdP auth flow was not found.
- Permission tests are partially complete because current implemented behavior has broad coverage, but missing/deferred features do not have implementation tests.
- Public link behavior has code paths, but readiness is conflict-marked rather than fully implemented because governance docs contain unresolved public-link conflicts.

## Missing Pieces

- Notification preferences / watched / muted persistence.
- Share-link and invite notification fan-out.
- Group grant notification fan-out.
- MFA/recent-auth backend state and enforcement.
- SCIM server endpoints.
- Real IdP login flow.
- Production invite delivery provider beyond the no-op delivery service.
- Frontend permission mutation workflow verification in this round.

## Documented Only Pieces

- Public SCIM 2.0 server endpoints are documented but not found in code.
- MFA/recent-auth enforcement is documented as deferred but not found in code.
- Notification preferences / watched / muted are documented as deferred but not found in code.
- Full frontend mutation workflow readiness is documented as deferred/not complete, but frontend code was not inspected.

## Conflict-Marked Areas

- Public collection links: code exists, but governance docs mark the behavior as unresolved.
- Public `linkMode`: code can set public through public share-link creation, while policy patch rejects `public`; this conflict was preserved.
- Public-link source conflicts: `docs/PERMISSION_SYSTEM_CONTRACT.md` and governance docs contain contradictory public collection/public link-mode statements; this report does not choose a source.
- Public anonymous access boundary: anonymous public share-link routes exist; whether and how far they should reach remains conflict-marked.
- Backend phase vs permission phase mismatch: permissions appear implemented through Phase 11 while backend project state says mainline Phase 5 and current target Phase 6 files; this report does not change status.
- README/code verification drift: not resolved in this investigation.
- PostgreSQL smoke gap: not resolved; smoke was not run.

## Data Model And Migration Findings

- `NorthstarDbContext` includes permission-related `DbSet`s for `ResourceAccessPolicies`, `ResourceAccessGrants`, `PermissionAuditEvents`, `WorkspaceGroups`, `WorkspaceGroupMembers`, `AccessRequests`, `PermissionNotifications`, `ShareLinks`, and `ResourceEmailInvites`.
- EF configurations found: `ResourceAccessPolicyConfiguration`, `ResourceAccessGrantConfiguration`, `PermissionAuditEventConfiguration`, `WorkspaceGroupConfiguration`, `WorkspaceGroupMemberConfiguration`, `AccessRequestConfiguration`, `PermissionNotificationConfiguration`, `ShareLinkConfiguration`, `ResourceEmailInviteConfiguration`, `UserConfiguration`, and `WorkspaceMemberConfiguration`.
- Permission migrations found:
  - `20260428061730_AddAuthWorkspacePermissionsPhase4.cs`
  - `20260429154618_AddResourceAccessPoliciesPhase2.cs`
  - `20260429163812_AddPermissionAuditEventsPhase3.cs`
  - `20260430002845_AddWorkspaceGroupsPhase4.cs`
  - `20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5.cs`
  - `20260430014036_AddPermissionExpiryNotificationsPhase6.cs`
  - `20260430052705_AddInternalShareLinksPhase7.cs`
  - `20260430063226_AddIamSyncPhase8.cs`
  - `20260430070347_AddExternalShareLinksAndEmailInvitesPhase9.cs`
  - `20260430162513_AddPublicShareLinksAndInviteDeliveryPhase10.cs`
  - `20260430165840_AddPublicCollectionLinksAndLinkPasswordsPhase11.cs`
- User external identity fields exist through IAM sync support.
- No notification preference, watched, muted, SCIM, MFA, or recent-auth tables/entities were found during inspection.

## API And Contract Findings

- `PermissionsController` exposes effective permission, resource policy/grant management, share links, email invites, audit, and access request routes.
- `ShareLinksController` exposes authenticated share-link token resolve route.
- `PublicShareLinksController` exposes anonymous public share-link resolve/document/collection routes.
- `NotificationsController` exposes notification list, mark-read, and mark-all-read routes.
- `WorkspacesController` exposes workspace members, workspace groups, group members, and IAM sync routes.
- Contracts exist under `Northstar.Contracts.Security` for effective permission, policy/grants, share links, public share links, email invites, access requests, audit, and notifications.
- Contracts exist under `Northstar.Contracts.Workspaces` for workspace members, workspace groups, and IAM sync.
- SCIM, MFA/recent-auth, notification preferences, watched, and muted API contracts were not found during inspection.

## Application / Domain / Infrastructure Findings

- API: Controllers are thin and delegate to Application services; `[Authorize]` is used for protected controllers and `[AllowAnonymous]` only on public share-link routes inspected.
- Contracts: DTOs are in `Northstar.Contracts`; EF entities are not exposed directly in controller signatures inspected.
- Application: Orchestration sits in services such as `EffectivePermissionService`, `ResourcePermissionManagementService`, `AccessRequestService`, `ShareLinkService`, `EmailInviteService`, `WorkspaceGroupService`, and `IamSyncService`.
- Domain: Permission entities and invariants live under `Northstar.Domain.Security`, `Northstar.Domain.Workspaces`, and `Northstar.Domain.Users`.
- Infrastructure: EF repositories, EF configurations, migrations, no-op invite delivery, and expiry notification hosted processing live under `Northstar.Infrastructure`.
- Dependency direction appears consistent in inspected files: Application depends on abstractions, Infrastructure supplies EF/provider implementations.

## Permission And Security Findings

- Server-side authorization is enforced through `WorkspaceAccessService`, `IScopedResourceAccessService`, and `EffectivePermissionService`.
- Role grant elevation is constrained by `PermissionCatalog.GetRank` checks in permission management, share-link, email-invite, and access-request approval code.
- Expired/revoked grants and inactive group memberships are ignored in effective permission tests and service logic.
- Last-owner downgrade/removal protection exists in `WorkspaceMembersService` and is covered by API tests.
- Token hashing is implemented for share links and email invites; tests cover raw-token storage boundaries.
- Public anonymous access is isolated to public share-link routes but remains conflict-marked by governance docs.

## Notification Findings

- Permission notification persistence and read-state operations exist.
- Notifications are emitted for user grants, access requests, access request review, group membership changes, and expiry events.
- Notification preferences, watched, and muted persistence were not found.
- Share-link/invite notification fan-out was not found and is explicitly covered as deferred by `KnowledgeApiTests.Phase11DeferredBoundaries_AreDocumentedAndDoNotEmitShareInviteFanout`.
- Group grant fan-out was not found; `ResourcePermissionManagementService` notification creation is user-subject-only.

## Public Link Findings

- Code exists for public share-link creation behind `PermissionPublicShareOptions.Enabled`.
- Anonymous public routes exist for resolving public links and reading public document/collection content.
- Public links require viewer role, expiry, and optional password proof; password hash storage exists.
- Public share-link resolution validates public audience, expected resource type, active state, policy `LinkMode`, and password proof.
- Protected paths are tested not to broaden from public links in API tests.
- Despite code evidence, public document links, public collection links, public `linkMode`, and anonymous boundary decisions remain conflict-marked.

## SCIM / IAM / IdP Findings

- IAM sync foundations are implemented through `IamSyncService`, `IIamSyncRepository`, `EfIamSyncRepository`, and `POST workspaces/{workspaceId}/iam/sync`.
- IAM sync can map external users and groups, create workspace members, sync group memberships, and audit changes.
- Public SCIM server endpoints were not found.
- Real IdP login support was not found beyond external identity mapping fields.

## MFA / Recent Auth Findings

- No MFA/recent-auth entities, services, controllers, contracts, migrations, or tests were found during inspection.
- MFA/recent-auth remains a missing/deferred permission-system completion area.

## Frontend Workflow Readiness

- Frontend code was not inspected in this backend-focused readiness task.
- Backend APIs needed for permission mutation workflows mostly exist: workspace members, groups, policy/grants, access requests, share links, email invites, notifications, and IAM sync.
- Readiness of actual frontend mutation UI remains not verified.

## Test Findings

- Tests found for effective permission behavior: `EffectivePermissionServiceTests`.
- Tests found for access requests: `AccessRequestServiceTests` and API access request tests in `KnowledgeApiTests`.
- Tests found for domain permission invariants: `ResourceAccessPermissionTests`.
- Tests found for workspace access and catalog behavior: `WorkspaceAccessServiceTests`, `PermissionCatalogTests`.
- API tests in `KnowledgeApiTests` cover permission management, temporary grants, share links, external links, public links, email invites, group grants, IAM sync, restricted documents, search/export filtering, notifications, expiry notifications, and migration shape.
- PostgreSQL smoke test file exists: `PostgreSqlSmokeTests.cs`; it was not run.
- Missing test coverage corresponds to missing implementation areas: notification preferences/watched/muted, share-link/invite notification fan-out, group grant fan-out, SCIM endpoints, real IdP login, and MFA/recent-auth.
- No tests were run in this investigation.

## Validation Not Run

- `dotnet restore`: not run; investigation-only task.
- `dotnet build`: not run; investigation-only task.
- `dotnet test`: not run; investigation-only task.
- PostgreSQL smoke: not run; investigation-only task.
- frontend build/test: not run; investigation-only task.

## Recommended Implementation Order

1. Resolve public-link behavior by explicit user decision before changing public collection, public `linkMode`, or anonymous access behavior.
2. Implement notification preferences / watched / muted persistence.
3. Implement share-link and invite notification fan-out.
4. Implement group grant fan-out.
5. Implement MFA/recent-auth backend state and enforcement.
6. Implement SCIM endpoint skeleton only after IAM sync contract boundaries are confirmed.
7. Add focused tests for each completed permission area and rerun validation commands later.

## Smallest Safe Next Step

resolve public-link behavior by user decision

## Open Questions

- Should public collection links remain supported, be feature-flagged only, or be disabled until a later phase?
- Should `linkMode = public` be patchable through policy APIs, only set by public share-link creation, or not supported?
- What exact anonymous public-link boundary is intended for bootstrap/map/search/export/list/context/activity/comments/attachments/files/version APIs?
- What notification preference model is required: global, workspace-scoped, resource-scoped watched/muted, or all three?
- Which production invite delivery provider should replace the no-op service?
- What SCIM subset is required for the first server endpoint skeleton?
- What MFA/recent-auth actions require step-up authorization?
- What PostgreSQL smoke connection should be used when validation is later requested?
