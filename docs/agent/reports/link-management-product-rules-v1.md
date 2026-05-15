# Link Management Product Rules V1

## Status

- Readiness rules updated after code verification.
- Link-management implementation now includes protected inventory, per-link
  pause/resume, access analytics, and authenticated audited full-URL copy.
- Remaining rules below define the accepted V1 behavior and the deferred items
  that still must not be represented as implemented.

## Assessment

The proposed link-management direction is broadly suitable for Northstar
because it fits the existing permission module shape:

- `share_links` already owns share-link identity, audience, role, creator,
  expiry, revocation, token hash, optional public password hash, and resource
  association.
- `permission_audit_events` already owns immutable management audit for
  permission mutations.
- dedicated public-link endpoints already preserve the anonymous access
  boundary.
- the document Share drawer is the right surface for ordinary document sharing;
  a centralized link inventory is appropriate as a workspace access/security
  management surface.

The Share drawer and link-management surfaces must not collapse different
concepts into one list:

- people, groups, links, and inherited access are separate access sources;
- "current access" means a permission summary, not live viewers or presence;
- link rows must be labelled as links and must not be shown as duplicate
  Library/Folder rows;
- library/folder terminology is UI copy only. Backend resource types remain
  `document` and `collection`.

The proposal needs adjustments before implementation:

- Do not add a generic `resources` table only for link management. Keep
  `resource_type` plus `resource_id` and resolve against existing documents and
  collections.
- Do not implement `edit` share links in the current baseline. Existing share
  links support `viewer` and `commenter`; public links are `viewer` only.
- Do not reconstruct full token-bearing URLs from list/detail metadata.
  Existing-link full URL copy is allowed only through the authenticated audited
  copy endpoint backed by protected token ciphertext.
- Per-link pause/resume is implemented. Resource-policy mismatch remains a
  separate `policy_paused` status.
- Store access analytics separately from immutable management audit.

Accepted token copy strategy:

- `share_links.token_hash` remains the lookup boundary.
- `share_links.token_ciphertext` stores a protected token value for authorized
  audited copy only.
- `POST /api/v1/permissions/share-links/{shareLinkId}/copy` is authenticated,
  authorizes manage access, writes `share_link.copy_requested`, and returns a
  token-bearing URL without returning token hashes, passwords, password hashes,
  or password proofs.
- Legacy links without ciphertext are reissued during copy: the backend rotates
  to a new raw token, stores the new hash plus ciphertext, and marks the copy
  response `reissued = true`.

## Accepted Target Rules

- Create generates a stable `share_links.id` and a high-entropy raw token.
- Raw token-bearing URLs are shown and copyable immediately after create or by
  an authorized audited copy request.
- List/detail views show token-free metadata only.
- Existing-link full-URL copy is supported through the audited copy endpoint;
  the frontend must not reconstruct a token-bearing URL from metadata.
- Status is derived as active, expired, revoked, or policy-paused unless a later
  migration adds per-link pause fields.
- Management uses central effective permission and action keys.
- Owner/admin-style manage-permissions authority can manage all links for a
  resource.
- Share-only users may be limited to their own links in a future explicit
  behavior change.
- Viewers and public visitors can access valid links but cannot manage links.
- Management mutations write `permission_audit_events`.
- Access events and rollups use future share-link access analytics tables.
- Unknown-token failures must not store raw token material or reveal token
  existence.

## Target Docs Updated

- `docs/PERMISSION_SYSTEM_CONTRACT.md`: canonical backend/data/API/security
  target rules for link management, access analytics, and token limits.
- `apps/web/FRONTEND_API_CONTRACT.md`: frontend placement, inventory, detail
  drawer, copy/reveal, and risk display target rules.
- `docs/agent/05-validation-protocol.md`: browser QA must not use in-browser or
  in-app Browser automation.
- `AGENTS.md`: root control entry now records the browser QA rule.

## Implementation Notes For Future Agents

- Add migrations only after re-reading the permission/data-model skills and
  inspecting current EF entities/configurations.
- Keep new APIs under `/api/v1` and return the Northstar error envelope.
- Do not return EF entities.
- Do not expose raw tokens, token hashes, password hashes, or password proof in
  DTOs, logs, audit, analytics, or UI state after the create response.
- Add focused backend tests for ownership limits, update/pause semantics,
  access analytics writes, public-link non-widening, and token secrecy.
- Add focused frontend tests for create-time-only copy, token-free list/detail
  rows, filters, detail drawer states, and disabled/restricted actions.

## Not Implemented

- centralized link inventory API/UI
- update role/expiry endpoint
- per-link pause/resume
- copy event API
- access event/stat tables
- risk scoring
- sensitive-document copy restriction
- editable share links
- reveal-link behavior
- live viewer/presence display in the Share drawer
