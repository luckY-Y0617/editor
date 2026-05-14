# Activity and Notification Boundary Decision

## Status

Accepted product and architecture boundary for the current frontend. Backend aggregation is proposed for a future phase and is not implemented by this decision.

## Scope

- Defines the boundary between Activity, Notification, Audit, and Presence.
- Documents the current Updates contract and future coalescing direction.
- Does not change backend tables, EF entities, migrations, API routes, services, outbox consumers, hosted services, or notification fanout.
- Preserves the backend model: Organization -> Workspace -> Space -> Collection -> Document.
- Preserves the product language: Workspace -> Library -> Folder/Document. Space remains the backend entity behind Library; Collection remains the backend entity behind Folder.

## Decision Summary

- Activity is collaborative context. It answers: who did what, to which object, and when.
- Notification is a high-signal inbox item that requires user handling or explicit awareness.
- Audit is a durable security and administration trail. It is not a user inbox by default.
- Presence is ephemeral collaboration state. It is not a notification and should not be persisted as activity unless a future collaboration-session model explicitly requires it.
- Ordinary document edits, autosaves, and repeated `document.updated` events belong to Activity, not Notification.
- Updates currently remains the workspace access and sharing notification inbox. It should not present Comments, Mentions, Document changes, Versions, or System alerts as primary capabilities until backend contracts and fanout exist.
- Current Home and Editor activity aggregation is a frontend display-layer noise reduction only. It is not the final backend notification or activity aggregation architecture.

## Activity Boundary

Activity is used for Home activity previews, Editor document activity, and document side-panel context. It can include low-signal events, but low-signal events must be grouped, folded, or delayed before display.

Activity can show:

- Ordinary document updates and autosaves.
- Repeated edits grouped by actor, document, action, and time proximity.
- High-signal document context such as publish, title change, move, comment, and share events when available.

Activity should not:

- Require immediate user action.
- Be treated as unread inbox work.
- Be stored in Tiptap document JSON.
- Become the same system as permission notifications.

## Notification Boundary

Notification is for events users need to handle or clearly know about. Current Updates aligns to the existing permission notification contract:

- `access_request.*`
- `permission.grant_*`
- `group.member_*`
- `share_link.*`
- `email_invite.*`
- expiring and expired access events

Ordinary `document.updated`, draft autosave, and typing activity do not enter Updates.

Future document notification types can be added only with explicit backend contract and fanout, for example:

- `mention.created`
- `comment.created`
- `comment.resolved`
- `document.published`
- `document.title_changed`
- `document.version_published`

These future notifications should still pass through notification policy and user preference checks.

## Audit Boundary

Audit records security-sensitive and administration-relevant operations. Audit events may include:

- Permission grants created, updated, revoked, expired, or expiring.
- Share links created or revoked.
- Email invites created, accepted, revoked, or failed.
- Workspace member and group membership changes.
- Role changes and high-risk administrative actions.

Audit is not equal to Notification. A permission grant can be recorded in audit while Notification fanout is decided separately by policy, recipient, resource preference, and mute state.

## Presence Boundary

Presence is realtime state such as who is currently editing or viewing a document. Presence should be transient and normally backed by realtime infrastructure rather than persistent notification tables.

Presence should not:

- Create Updates inbox items.
- Create audit records by default.
- Be stored in document JSON.
- Be persisted as Activity unless a future collaboration-session model defines retention and privacy rules.

## Future Coalescing Architecture

The recommended backend direction is Outbox + Background Job + Coalescing. The system should not create a notification every time a document draft is saved.

Event sources:

- Document Service domain events.
- Comment Service domain events.
- Permission Service domain events.
- Share link and email invite domain events.

Pipeline:

1. Domain event is written to Outbox with the application transaction.
2. Background job or hosted service consumes outbox or activity events.
3. Aggregator classifies the event as activity, notification, audit, presence, or multiple outputs.
4. Low-signal activity is coalesced by deduplication key.
5. High-signal notification events are emitted immediately after policy checks.

Suggested deduplication key:

```text
actorId + resourceType + resourceId + normalizedActionType + timeWindow
```

The time component can be a fixed window or a sliding inactivity window.

Recommended behavior:

- Same actor + same document + `document.updated` within 5-15 minutes -> one activity summary.
- Mention created -> immediate notification.
- Comment created or resolved -> future immediate notification if backend contract exists.
- Permission grant created -> immediate notification and audit.
- Share link created or revoked -> immediate notification and audit.
- Access request created -> immediate notification and audit.

## Optional Future Data Structures

Future backend implementation may introduce `activity_aggregates`, `notification_aggregates`, or equivalent state. This decision does not create them.

Optional fields:

- `aggregate_key`
- `workspace_id`
- `resource_type`
- `resource_id`
- `actor_id`
- `action_type`
- `count`
- `first_occurred_at`
- `last_occurred_at`
- `status` such as `pending`, `sent`, or `archived`

These structures should be introduced only when backend event classification and migration scope are approved.

## Product Rules

- Home Team Activity should be treated as recent document activity or workspace activity preview.
- Editor Activity should show only current-document activity.
- Updates should show access, sharing, permission, group, invite, and expiry notifications backed by current backend contract.
- Updates should not advertise Comments, Mentions, Document changes, Versions, or System alerts as primary capabilities until backend support exists.
- Settings Notifications should describe resource watch/mute preferences and workspace permission/access/share notification preferences only.
- Daily document sharing stays in the Editor Share Drawer.
- Advanced document permissions stay document-scoped.
- Public links continue through the dedicated share-link API.

## Current State

- Frontend activity display performs simple aggregation to reduce repeated ordinary document update noise.
- Backend document activity now exposes classification metadata on activity timeline items:
  - `document.updated` is classified as low-signal, coalescible Activity only.
  - Ordinary document activity is not marked as a Notification candidate.
  - destructive document operations can be marked as Audit candidates without creating audit rows in this phase.
- Updates is narrowed to access and sharing notifications.
- Settings exposes notification preferences only where current backend support exists.
- There is no backend activity aggregator, notification aggregator, digest job, presence system, or document-update notification fanout in this round.

## Migration Path

1. Keep frontend display aggregation as a temporary UX noise reduction.
2. Add backend activity event classification without changing notification fanout.
3. Add coalescing aggregation for low-signal document activity.
4. Add explicit high-signal document notification contracts such as mention, comment, publish, title change, or version publish.
5. Add user preferences such as per-document follow, mute, frequency, and digest only after backend contracts exist.

## Non-Goals

- No backend aggregation implementation.
- No `notification_aggregates` or `activity_aggregates` table.
- No migration.
- No API route change.
- No outbox consumer, hosted service, Hangfire job, WebSocket, or presence implementation.
- No document update notification fanout.
- No comments mention notification implementation.
- No Tiptap JSON storage for comments, mentions, notifications, permissions, audit, presence, or activity.
