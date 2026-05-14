# Northstar Search / Knowledge Graph Backend Strategy v1

Date: 2026-05-11

## Decision

Northstar backend search will use the existing `document_search_index` projection as the search source of truth and run in PostgreSQL with:

- weighted `tsvector` over document title and plain text content;
- GIN index over the generated search vector;
- `pg_trgm` similarity and trigram indexes for typo-tolerant title/content fallback;
- existing Application-layer effective permission filtering before results leave `/api/v1/search`.

Northstar Knowledge Graph v1 will remain a document-context graph, not a separate graph database. It is derived from existing persisted resources:

- `document_links` for related documents and backlinks;
- `document_versions` for version trail;
- `documents`, `collections`, and `spaces` for location context;
- central effective permission filtering for any linked document titles returned by context APIs.

No external search engine, vector database, graph database, or new public API surface is introduced in this version.

## Boundaries

- Backend model remains `Organization -> Workspace -> Space -> Collection -> Document`.
- User-facing product language remains `Workspace -> Library -> Folder/Document`.
- `Space` is still the backend entity behind user-facing Library.
- `Collection` is still the backend entity behind user-facing Folder.
- Search, context, backlinks, related documents, activity, comments, files, and permissions must not be stored inside Tiptap document JSON.
- Public share links must not broaden authenticated search/list/map/bootstrap/export APIs.
- Public links continue to use dedicated share-link APIs only.

## Search Contract

`GET /api/v1/search?q=&spaceId=` remains the public contract. The response DTO is unchanged.

Search behavior:

1. Validate the requested Space exists and resolve its Workspace.
2. Enforce Workspace view access.
3. Query `document_search_index` for non-deleted, non-archived documents in that Space.
4. Rank PostgreSQL results with full-text rank first, title similarity second, then recency.
5. Filter candidate results through effective document access before returning titles or excerpts.
6. Return empty results for blank queries.

PostgreSQL is the production path. Non-PostgreSQL providers, including InMemory tests, keep a small contains-search fallback so test infrastructure does not force production code into provider-specific compromises.

Operational repair:

- `ISearchIndexMaintenanceService` is the internal rebuild/repair boundary.
- Rebuild reads active `documents + document_drafts` and repairs `document_search_index`.
- Archived, deleted, or otherwise inactive documents are removed from the search projection.
- This is not exposed as a public API in v1.

## Knowledge Graph Contract

`GET /api/v1/documents/{documentId}/context` remains the public contract. It returns:

- related documents from outbound `document_links`;
- backlinks from inbound `document_links`;
- version trail from `document_versions` plus the current draft marker.

Graph behavior:

1. Enforce access to the requested document first.
2. Build context from persisted links and versions.
3. Filter related documents and backlinks through effective document access.
4. Do not expose inaccessible document titles, excerpts, or codes through context.
5. Do not use Organization, Workspace members, or global permission administration as document-context graph nodes.

Operational validation:

- PostgreSQL smoke should assert `pg_trgm` is installed, `document_search_index.search_vector` exists, and full-text query matches seeded data.
- PostgreSQL smoke is passed only when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set and the smoke command actually runs.

## Non-Goals

- No Elasticsearch, OpenSearch, Meilisearch, vector search, or graph database.
- No semantic search or embeddings.
- No new `/api/v1/libraries` route.
- No broad public/share-token access to search or context.
- No full activity inbox.
- No comment mention notification implementation.
- No Tiptap JSON persistence for links/comments/files/permissions/activity.

## Future Path

If search scale or language requirements exceed PostgreSQL:

1. Keep `document_search_index` as the canonical internal projection.
2. Add an outbox-driven external indexer behind an Application interface.
3. Preserve the same `/api/v1/search` contract.
4. Keep effective permission filtering server-side even if the external index prefilters candidates.

If Knowledge Graph expands:

1. Keep `document_links` as the first graph edge source.
2. Add explicit typed relationship projections only when product behavior requires them.
3. Keep graph APIs document-scoped first.
4. Avoid global graph traversal until permission-safe expansion rules are specified.
