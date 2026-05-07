# Organization Settings Contract Discovery v1

## Scope

This report records code-level discovery for Organization Settings readiness. It does not mark organization-level management as implemented, and it does not add backend API, migration, permission semantics, or system/instance settings.

## Existing / Partial / Missing Capabilities

| Capability | Current source found in code | Readiness | Notes |
| --- | --- | --- | --- |
| Organization profile | Workspace bootstrap/profile data exists; no organization or tenant entity/API was found. | missing-contract | Needs an organization read model before UI can be live-backed. |
| Global members | `GET /workspaces/{workspaceId}/members` and the Members surface exist. | partial | Workspace-scoped only; not organization-global. |
| Workspace provisioning | Workspace routes/bootstrap data exist. | partial | No organization-owned provisioning contract was found. |
| Domains | No domain entity, DTO, route, or frontend surface found. | missing-contract | Needs ownership and verification semantics. |
| SSO / SCIM ownership | Workspace-scoped SCIM discovery, users/groups, and token endpoints exist; login OIDC/SAML UI is disabled. | partial | SCIM tokens are workspace-bound, not organization-wide. |
| Audit log | `GET /permissions/audit?workspaceId=...` exists. | partial | Workspace permission audit only; no organization audit stream found. |
| Billing / Plan | Settings Plan is deferred; no billing/plan backend contract found. | deferred | Billing boundary should not be inferred from workspace roles. |
| Data retention | No retention policy entity, DTO, route, or frontend surface found. | missing-contract | Destructive policy semantics must be defined first. |

## Recommended First Slice

Recommended first implementation slice: **Organization profile + global members read-only**.

Rationale: this is the lowest-risk organization layer because it avoids high-risk mutations, defines the ownership boundary, and creates a stable base for later domains, SSO/SCIM ownership, and organization audit.

## Deferred Boundaries

- No Organization Settings mutation is enabled.
- No System / Instance Settings route or implementation is added.
- No organization, billing, audit, domain, SSO, or SCIM backend contract is added.
- No migration is added.
- No Tiptap document JSON schema or runtime UI state persistence is changed.
