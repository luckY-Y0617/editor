CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE EXTENSION IF NOT EXISTS pgcrypto;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE TABLE collections (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        space_id uuid NOT NULL,
        parent_collection_id uuid,
        title text NOT NULL,
        slug text,
        sort_order numeric(20,10) NOT NULL DEFAULT 0.0,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        archived_at timestamp with time zone,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_collections" PRIMARY KEY (id),
        CONSTRAINT "FK_collections_collections_parent_collection_id" FOREIGN KEY (parent_collection_id) REFERENCES collections (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE TABLE document_drafts (
        document_id uuid NOT NULL,
        workspace_id uuid NOT NULL,
        content jsonb NOT NULL,
        text_content text NOT NULL DEFAULT '',
        outline jsonb NOT NULL DEFAULT ('[]'::jsonb),
        word_count integer NOT NULL DEFAULT 0,
        content_hash text,
        updated_by uuid,
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_document_drafts" PRIMARY KEY (document_id),
        CONSTRAINT document_drafts_word_count_check CHECK (word_count >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE TABLE documents (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        space_id uuid NOT NULL,
        collection_id uuid,
        owner_id uuid,
        title text NOT NULL,
        slug text,
        status text NOT NULL DEFAULT 'draft',
        sort_order numeric(20,10) NOT NULL DEFAULT 0.0,
        revision bigint NOT NULL DEFAULT 0,
        current_published_version_id uuid,
        last_edited_by uuid,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        published_at timestamp with time zone,
        archived_at timestamp with time zone,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_documents" PRIMARY KEY (id),
        CONSTRAINT documents_revision_check CHECK (revision >= 0),
        CONSTRAINT documents_status_check CHECK (status IN ('draft', 'published', 'archived')),
        CONSTRAINT "FK_documents_collections_collection_id" FOREIGN KEY (collection_id) REFERENCES collections (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE TABLE spaces (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        name text NOT NULL,
        slug text NOT NULL,
        description text,
        visibility text NOT NULL DEFAULT 'workspace',
        sort_order numeric(20,10) NOT NULL DEFAULT 0.0,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        archived_at timestamp with time zone,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_spaces" PRIMARY KEY (id),
        CONSTRAINT spaces_visibility_check CHECK (visibility IN ('private', 'workspace', 'public'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE TABLE workspaces (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        name text NOT NULL,
        slug text NOT NULL,
        created_by uuid,
        default_space_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_workspaces" PRIMARY KEY (id),
        CONSTRAINT "FK_workspaces_spaces_default_space_id" FOREIGN KEY (default_space_id) REFERENCES spaces (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX collections_space_order_idx ON collections (workspace_id, space_id, parent_collection_id, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX "IX_collections_parent_collection_id" ON collections (parent_collection_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX "IX_collections_space_id" ON collections (space_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX document_drafts_workspace_idx ON document_drafts (workspace_id, updated_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX documents_collection_order_idx ON documents (workspace_id, space_id, collection_id, sort_order) WHERE deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX documents_updated_idx ON documents (workspace_id, updated_at DESC) WHERE deleted_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX "IX_documents_collection_id" ON documents (collection_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX "IX_documents_space_id" ON documents (space_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE UNIQUE INDEX spaces_workspace_slug_key ON spaces (workspace_id, slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE INDEX "IX_workspaces_default_space_id" ON workspaces (default_space_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    CREATE UNIQUE INDEX workspaces_slug_key ON workspaces (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    ALTER TABLE collections ADD CONSTRAINT "FK_collections_spaces_space_id" FOREIGN KEY (space_id) REFERENCES spaces (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    ALTER TABLE collections ADD CONSTRAINT "FK_collections_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    ALTER TABLE document_drafts ADD CONSTRAINT "FK_document_drafts_documents_document_id" FOREIGN KEY (document_id) REFERENCES documents (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    ALTER TABLE document_drafts ADD CONSTRAINT "FK_document_drafts_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    ALTER TABLE documents ADD CONSTRAINT "FK_documents_spaces_space_id" FOREIGN KEY (space_id) REFERENCES spaces (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    ALTER TABLE documents ADD CONSTRAINT "FK_documents_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    ALTER TABLE spaces ADD CONSTRAINT "FK_spaces_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428012644_InitialKnowledgeCore') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260428012644_InitialKnowledgeCore', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE EXTENSION IF NOT EXISTS citext;
    CREATE EXTENSION IF NOT EXISTS pgcrypto;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE TABLE users (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        email citext,
        display_name text NOT NULL,
        avatar_url text,
        external_provider text,
        external_subject text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_users" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE TABLE tags (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        name text NOT NULL,
        slug text NOT NULL,
        color text,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_tags" PRIMARY KEY (id),
        CONSTRAINT "FK_tags_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_tags_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE TABLE workspace_members (
        workspace_id uuid NOT NULL,
        user_id uuid NOT NULL,
        role text NOT NULL,
        status text NOT NULL DEFAULT 'active',
        joined_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_workspace_members" PRIMARY KEY (workspace_id, user_id),
        CONSTRAINT workspace_members_role_check CHECK (role IN ('owner', 'admin', 'editor', 'viewer')),
        CONSTRAINT workspace_members_status_check CHECK (status IN ('invited', 'active', 'suspended')),
        CONSTRAINT "FK_workspace_members_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT "FK_workspace_members_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE TABLE document_tags (
        document_id uuid NOT NULL,
        tag_id uuid NOT NULL,
        workspace_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_document_tags" PRIMARY KEY (document_id, tag_id),
        CONSTRAINT "FK_document_tags_documents_document_id" FOREIGN KEY (document_id) REFERENCES documents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_document_tags_tags_tag_id" FOREIGN KEY (tag_id) REFERENCES tags (id) ON DELETE CASCADE,
        CONSTRAINT "FK_document_tags_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_workspaces_created_by" ON workspaces (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_spaces_created_by" ON spaces (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_documents_created_by" ON documents (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_documents_last_edited_by" ON documents (last_edited_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_documents_owner_id" ON documents (owner_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_document_drafts_updated_by" ON document_drafts (updated_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_collections_created_by" ON collections (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX document_tags_tag_idx ON document_tags (workspace_id, tag_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_document_tags_tag_id" ON document_tags (tag_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_tags_created_by" ON tags (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE UNIQUE INDEX tags_workspace_slug_key ON tags (workspace_id, slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE UNIQUE INDEX users_email_key ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE UNIQUE INDEX users_external_provider_subject_key ON users (external_provider, external_subject);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    CREATE INDEX "IX_workspace_members_user_id" ON workspace_members (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    ALTER TABLE collections ADD CONSTRAINT "FK_collections_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    ALTER TABLE document_drafts ADD CONSTRAINT "FK_document_drafts_users_updated_by" FOREIGN KEY (updated_by) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    ALTER TABLE documents ADD CONSTRAINT "FK_documents_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    ALTER TABLE documents ADD CONSTRAINT "FK_documents_users_last_edited_by" FOREIGN KEY (last_edited_by) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    ALTER TABLE documents ADD CONSTRAINT "FK_documents_users_owner_id" FOREIGN KEY (owner_id) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    ALTER TABLE spaces ADD CONSTRAINT "FK_spaces_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    ALTER TABLE workspaces ADD CONSTRAINT "FK_workspaces_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428015030_AddKnowledgeApiPhase2') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260428015030_AddKnowledgeApiPhase2', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE TABLE activity_events (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        actor_id uuid,
        entity_type text NOT NULL,
        entity_id uuid NOT NULL,
        action text NOT NULL,
        summary text NOT NULL,
        metadata jsonb NOT NULL DEFAULT ('{}'::jsonb),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_activity_events" PRIMARY KEY (id),
        CONSTRAINT "FK_activity_events_users_actor_id" FOREIGN KEY (actor_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_activity_events_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE TABLE document_links (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        source_document_id uuid NOT NULL,
        target_document_id uuid,
        target_url text,
        link_type text NOT NULL DEFAULT 'reference',
        anchor_text text,
        source_anchor jsonb,
        target_anchor jsonb,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_document_links" PRIMARY KEY (id),
        CONSTRAINT document_links_link_type_check CHECK (link_type IN ('reference', 'related', 'embed', 'external')),
        CONSTRAINT document_links_target_check CHECK (target_document_id IS NOT NULL OR target_url IS NOT NULL),
        CONSTRAINT "FK_document_links_documents_source_document_id" FOREIGN KEY (source_document_id) REFERENCES documents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_document_links_documents_target_document_id" FOREIGN KEY (target_document_id) REFERENCES documents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_document_links_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_document_links_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE TABLE document_search_index (
        document_id uuid NOT NULL,
        workspace_id uuid NOT NULL,
        space_id uuid NOT NULL,
        title text NOT NULL,
        text_content text NOT NULL DEFAULT '',
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_document_search_index" PRIMARY KEY (document_id),
        CONSTRAINT "FK_document_search_index_documents_document_id" FOREIGN KEY (document_id) REFERENCES documents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_document_search_index_spaces_space_id" FOREIGN KEY (space_id) REFERENCES spaces (id) ON DELETE CASCADE,
        CONSTRAINT "FK_document_search_index_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE TABLE document_versions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        document_id uuid NOT NULL,
        version_no integer NOT NULL,
        label text NOT NULL,
        version_type text NOT NULL DEFAULT 'system',
        content jsonb NOT NULL,
        text_content text NOT NULL DEFAULT '',
        outline jsonb NOT NULL DEFAULT ('[]'::jsonb),
        word_count integer NOT NULL DEFAULT 0,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        published_at timestamp with time zone,
        CONSTRAINT "PK_document_versions" PRIMARY KEY (id),
        CONSTRAINT document_versions_version_no_check CHECK (version_no > 0),
        CONSTRAINT document_versions_version_type_check CHECK (version_type IN ('manual', 'published', 'imported', 'system')),
        CONSTRAINT document_versions_word_count_check CHECK (word_count >= 0),
        CONSTRAINT "FK_document_versions_documents_document_id" FOREIGN KEY (document_id) REFERENCES documents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_document_versions_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_document_versions_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX activity_events_actor_idx ON activity_events (workspace_id, actor_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX activity_events_entity_idx ON activity_events (workspace_id, entity_type, entity_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX "IX_activity_events_actor_id" ON activity_events (actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX document_links_source_idx ON document_links (workspace_id, source_document_id, link_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX document_links_target_idx ON document_links (workspace_id, target_document_id) WHERE target_document_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX "IX_document_links_created_by" ON document_links (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX "IX_document_links_source_document_id" ON document_links (source_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX "IX_document_links_target_document_id" ON document_links (target_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX document_search_workspace_idx ON document_search_index (workspace_id, space_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX "IX_document_search_index_space_id" ON document_search_index (space_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX document_versions_doc_idx ON document_versions (workspace_id, document_id, version_no DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE UNIQUE INDEX document_versions_document_label_key ON document_versions (document_id, label);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE UNIQUE INDEX document_versions_document_version_no_key ON document_versions (document_id, version_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    CREATE INDEX "IX_document_versions_created_by" ON document_versions (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428053422_AddKnowledgeContextActivitySearchPhase3') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260428053422_AddKnowledgeContextActivitySearchPhase3', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    CREATE TABLE auth_events (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid,
        action text NOT NULL,
        succeeded boolean NOT NULL,
        ip_address text,
        user_agent text,
        metadata jsonb NOT NULL DEFAULT ('{}'::jsonb),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_auth_events" PRIMARY KEY (id),
        CONSTRAINT "FK_auth_events_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_id uuid NOT NULL,
        token_hash text NOT NULL,
        family_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        expires_at timestamp with time zone NOT NULL,
        rotated_at timestamp with time zone,
        revoked_at timestamp with time zone,
        replaced_by_token_id uuid,
        created_by_ip text,
        user_agent text,
        CONSTRAINT "PK_refresh_tokens" PRIMARY KEY (id),
        CONSTRAINT "FK_refresh_tokens_refresh_tokens_replaced_by_token_id" FOREIGN KEY (replaced_by_token_id) REFERENCES refresh_tokens (id) ON DELETE SET NULL,
        CONSTRAINT "FK_refresh_tokens_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    CREATE TABLE user_credentials (
        user_id uuid NOT NULL,
        password_hash text NOT NULL,
        password_hash_algorithm text NOT NULL,
        password_updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_user_credentials" PRIMARY KEY (user_id),
        CONSTRAINT "FK_user_credentials_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    CREATE INDEX auth_events_user_idx ON auth_events (user_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    CREATE INDEX "IX_refresh_tokens_replaced_by_token_id" ON refresh_tokens (replaced_by_token_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    CREATE INDEX refresh_tokens_expires_idx ON refresh_tokens (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    CREATE UNIQUE INDEX refresh_tokens_token_hash_key ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    CREATE INDEX refresh_tokens_user_family_idx ON refresh_tokens (user_id, family_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428061730_AddAuthWorkspacePermissionsPhase4') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260428061730_AddAuthWorkspacePermissionsPhase4', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE TABLE file_outbox_events (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        aggregate_type text NOT NULL,
        aggregate_id uuid NOT NULL,
        event_type text NOT NULL,
        payload jsonb NOT NULL,
        headers jsonb NOT NULL DEFAULT ('{}'::jsonb),
        status text NOT NULL DEFAULT 'pending',
        retry_count integer NOT NULL DEFAULT 0,
        next_retry_at timestamp with time zone NOT NULL DEFAULT (now()),
        last_error text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_file_outbox_events" PRIMARY KEY (id),
        CONSTRAINT file_outbox_events_retry_count_check CHECK (retry_count >= 0),
        CONSTRAINT file_outbox_events_status_check CHECK (status IN ('pending', 'published', 'failed')),
        CONSTRAINT "FK_file_outbox_events_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE TABLE files (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        uploaded_by uuid,
        storage_provider text NOT NULL,
        bucket text NOT NULL,
        object_key text NOT NULL,
        original_filename text NOT NULL,
        mime_type text NOT NULL,
        byte_size bigint NOT NULL,
        checksum_sha256 text,
        width integer,
        height integer,
        metadata jsonb NOT NULL DEFAULT ('{}'::jsonb),
        scan_status text NOT NULL DEFAULT 'clean',
        processing_status text NOT NULL DEFAULT 'ready',
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_files" PRIMARY KEY (id),
        CONSTRAINT files_byte_size_check CHECK (byte_size >= 0),
        CONSTRAINT files_processing_status_check CHECK (processing_status IN ('pending', 'ready', 'failed')),
        CONSTRAINT files_scan_status_check CHECK (scan_status IN ('pending', 'clean', 'blocked', 'failed')),
        CONSTRAINT "FK_files_users_uploaded_by" FOREIGN KEY (uploaded_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_files_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE TABLE document_attachments (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        document_id uuid NOT NULL,
        file_id uuid NOT NULL,
        relation_type text NOT NULL DEFAULT 'attachment',
        metadata jsonb NOT NULL DEFAULT ('{}'::jsonb),
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_document_attachments" PRIMARY KEY (id),
        CONSTRAINT document_attachments_relation_type_check CHECK (relation_type IN ('attachment', 'inline_image', 'cover')),
        CONSTRAINT "FK_document_attachments_documents_document_id" FOREIGN KEY (document_id) REFERENCES documents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_document_attachments_files_file_id" FOREIGN KEY (file_id) REFERENCES files (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_document_attachments_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_document_attachments_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE TABLE upload_sessions (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        owner_id uuid NOT NULL,
        idempotency_key text NOT NULL,
        original_filename text NOT NULL,
        mime_type text NOT NULL,
        byte_size bigint NOT NULL,
        checksum_sha256 text,
        biz_type text,
        storage_provider text NOT NULL,
        bucket text NOT NULL,
        object_key text NOT NULL,
        upload_mode text NOT NULL DEFAULT 'single',
        multipart_upload_id text,
        chunk_size bigint,
        total_parts integer,
        status text NOT NULL DEFAULT 'initiated',
        finalized_file_id uuid,
        expires_at timestamp with time zone NOT NULL,
        finalized_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_upload_sessions" PRIMARY KEY (id),
        CONSTRAINT upload_sessions_byte_size_check CHECK (byte_size > 0),
        CONSTRAINT upload_sessions_status_check CHECK (status IN ('initiated', 'uploading', 'completed', 'aborted', 'expired', 'failed', 'finalized')),
        CONSTRAINT upload_sessions_upload_mode_check CHECK (upload_mode IN ('single', 'multipart')),
        CONSTRAINT "FK_upload_sessions_files_finalized_file_id" FOREIGN KEY (finalized_file_id) REFERENCES files (id) ON DELETE SET NULL,
        CONSTRAINT "FK_upload_sessions_users_owner_id" FOREIGN KEY (owner_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_upload_sessions_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE UNIQUE INDEX document_attachments_document_file_relation_key ON document_attachments (document_id, file_id, relation_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX document_attachments_document_idx ON document_attachments (workspace_id, document_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX document_attachments_file_idx ON document_attachments (workspace_id, file_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX "IX_document_attachments_created_by" ON document_attachments (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX "IX_document_attachments_file_id" ON document_attachments (file_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX file_outbox_events_aggregate_idx ON file_outbox_events (workspace_id, aggregate_type, aggregate_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX file_outbox_events_dispatch_idx ON file_outbox_events (status, next_retry_at, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE UNIQUE INDEX files_storage_object_key ON files (storage_provider, bucket, object_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX files_workspace_created_idx ON files (workspace_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX "IX_files_uploaded_by" ON files (uploaded_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX "IX_upload_sessions_finalized_file_id" ON upload_sessions (finalized_file_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX "IX_upload_sessions_owner_id" ON upload_sessions (owner_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX upload_sessions_owner_idx ON upload_sessions (workspace_id, owner_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE INDEX upload_sessions_status_expires_idx ON upload_sessions (status, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE UNIQUE INDEX upload_sessions_storage_object_key ON upload_sessions (storage_provider, bucket, object_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    CREATE UNIQUE INDEX upload_sessions_workspace_idempotency_key ON upload_sessions (workspace_id, idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260428075433_AddFilesUploadSessionsPhase6') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260428075433_AddFilesUploadSessionsPhase6', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429052913_AddCommentPersistence') THEN
    CREATE TABLE comment_threads (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        document_id uuid NOT NULL,
        status text NOT NULL DEFAULT 'open',
        anchor jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        resolved_at timestamp with time zone,
        CONSTRAINT "PK_comment_threads" PRIMARY KEY (id),
        CONSTRAINT comment_threads_status_check CHECK (status IN ('open', 'resolved')),
        CONSTRAINT "FK_comment_threads_documents_document_id" FOREIGN KEY (document_id) REFERENCES documents (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429052913_AddCommentPersistence') THEN
    CREATE TABLE comment_messages (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        thread_id uuid NOT NULL,
        body text NOT NULL,
        author_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone,
        deleted_at timestamp with time zone,
        CONSTRAINT "PK_comment_messages" PRIMARY KEY (id),
        CONSTRAINT "FK_comment_messages_comment_threads_thread_id" FOREIGN KEY (thread_id) REFERENCES comment_threads (id) ON DELETE CASCADE,
        CONSTRAINT "FK_comment_messages_users_author_user_id" FOREIGN KEY (author_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429052913_AddCommentPersistence') THEN
    CREATE INDEX comment_messages_thread_idx ON comment_messages (thread_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429052913_AddCommentPersistence') THEN
    CREATE INDEX "IX_comment_messages_author_user_id" ON comment_messages (author_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429052913_AddCommentPersistence') THEN
    CREATE INDEX comment_threads_document_idx ON comment_threads (document_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429052913_AddCommentPersistence') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260429052913_AddCommentPersistence', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE TABLE resource_access_grants (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        resource_type text NOT NULL,
        resource_id uuid NOT NULL,
        subject_type text NOT NULL,
        subject_id uuid NOT NULL,
        role_key text NOT NULL,
        granted_by uuid,
        granted_at timestamp with time zone NOT NULL DEFAULT (now()),
        expires_at timestamp with time zone,
        revoked_at timestamp with time zone,
        revoked_by uuid,
        reason text,
        CONSTRAINT "PK_resource_access_grants" PRIMARY KEY (id),
        CONSTRAINT resource_access_grants_resource_type_check CHECK (resource_type IN ('collection', 'document')),
        CONSTRAINT resource_access_grants_role_key_check CHECK (role_key IN ('owner', 'admin', 'editor', 'commenter', 'viewer')),
        CONSTRAINT resource_access_grants_subject_type_check CHECK (subject_type IN ('user')),
        CONSTRAINT "FK_resource_access_grants_users_granted_by" FOREIGN KEY (granted_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_resource_access_grants_users_revoked_by" FOREIGN KEY (revoked_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_resource_access_grants_users_subject_id" FOREIGN KEY (subject_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT "FK_resource_access_grants_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE TABLE resource_access_policies (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        resource_type text NOT NULL,
        resource_id uuid NOT NULL,
        inheritance_mode text NOT NULL DEFAULT 'inherit',
        link_mode text NOT NULL DEFAULT 'disabled',
        default_link_role text,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_resource_access_policies" PRIMARY KEY (id),
        CONSTRAINT resource_access_policies_default_link_role_check CHECK (default_link_role IS NULL OR default_link_role IN ('viewer', 'commenter')),
        CONSTRAINT resource_access_policies_inheritance_mode_check CHECK (inheritance_mode IN ('inherit', 'restricted')),
        CONSTRAINT resource_access_policies_link_mode_check CHECK (link_mode IN ('disabled', 'internal', 'public')),
        CONSTRAINT resource_access_policies_resource_type_check CHECK (resource_type IN ('collection', 'document')),
        CONSTRAINT "FK_resource_access_policies_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_resource_access_policies_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE INDEX idx_grants_expiry ON resource_access_grants (expires_at) WHERE revoked_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE INDEX idx_grants_subject ON resource_access_grants (workspace_id, subject_type, subject_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE INDEX idx_grants_workspace_resource ON resource_access_grants (workspace_id, resource_type, resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE INDEX "IX_resource_access_grants_granted_by" ON resource_access_grants (granted_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE INDEX "IX_resource_access_grants_revoked_by" ON resource_access_grants (revoked_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE INDEX "IX_resource_access_grants_subject_id" ON resource_access_grants (subject_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE UNIQUE INDEX resource_access_grants_resource_subject_key ON resource_access_grants (resource_type, resource_id, subject_type, subject_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE UNIQUE INDEX idx_policies_resource ON resource_access_policies (resource_type, resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE INDEX "IX_resource_access_policies_created_by" ON resource_access_policies (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    CREATE INDEX "IX_resource_access_policies_workspace_id" ON resource_access_policies (workspace_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429154618_AddResourceAccessPoliciesPhase2') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260429154618_AddResourceAccessPoliciesPhase2', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429163812_AddPermissionAuditEventsPhase3') THEN
    CREATE TABLE permission_audit_events (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        actor_id uuid,
        action text NOT NULL,
        resource_type text NOT NULL,
        resource_id uuid NOT NULL,
        subject_type text,
        subject_id uuid,
        before_json jsonb,
        after_json jsonb,
        metadata jsonb NOT NULL DEFAULT ('{}'::jsonb),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_permission_audit_events" PRIMARY KEY (id),
        CONSTRAINT "FK_permission_audit_events_users_actor_id" FOREIGN KEY (actor_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_permission_audit_events_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429163812_AddPermissionAuditEventsPhase3') THEN
    CREATE INDEX idx_permission_audit_resource_created ON permission_audit_events (resource_type, resource_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429163812_AddPermissionAuditEventsPhase3') THEN
    CREATE INDEX idx_permission_audit_workspace_created ON permission_audit_events (workspace_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429163812_AddPermissionAuditEventsPhase3') THEN
    CREATE INDEX "IX_permission_audit_events_actor_id" ON permission_audit_events (actor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260429163812_AddPermissionAuditEventsPhase3') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260429163812_AddPermissionAuditEventsPhase3', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    ALTER TABLE resource_access_grants DROP CONSTRAINT "FK_resource_access_grants_users_subject_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    DROP INDEX "IX_resource_access_grants_subject_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    DROP INDEX resource_access_grants_resource_subject_key;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    ALTER TABLE resource_access_grants DROP CONSTRAINT resource_access_grants_subject_type_check;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE TABLE workspace_groups (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        name text NOT NULL,
        description text,
        type text NOT NULL,
        archived_at timestamp with time zone,
        external_provider text,
        external_group_id text,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_workspace_groups" PRIMARY KEY (id),
        CONSTRAINT workspace_groups_type_check CHECK (type IN ('static', 'dynamic')),
        CONSTRAINT "FK_workspace_groups_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_workspace_groups_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE TABLE workspace_group_members (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        group_id uuid NOT NULL,
        user_id uuid NOT NULL,
        added_by uuid,
        added_at timestamp with time zone NOT NULL DEFAULT (now()),
        expires_at timestamp with time zone,
        removed_at timestamp with time zone,
        CONSTRAINT "PK_workspace_group_members" PRIMARY KEY (id),
        CONSTRAINT "FK_workspace_group_members_users_added_by" FOREIGN KEY (added_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_workspace_group_members_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT "FK_workspace_group_members_workspace_groups_group_id" FOREIGN KEY (group_id) REFERENCES workspace_groups (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE INDEX idx_grants_workspace_resource_subject_type ON resource_access_grants (workspace_id, resource_type, resource_id, subject_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE UNIQUE INDEX resource_access_grants_resource_subject_key ON resource_access_grants (resource_type, resource_id, subject_type, subject_id) WHERE revoked_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    ALTER TABLE resource_access_grants ADD CONSTRAINT resource_access_grants_subject_type_check CHECK (subject_type IN ('user', 'group'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE INDEX idx_workspace_group_members_user_active ON workspace_group_members (user_id, removed_at, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE INDEX "IX_workspace_group_members_added_by" ON workspace_group_members (added_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE UNIQUE INDEX workspace_group_members_group_user_active_key ON workspace_group_members (group_id, user_id) WHERE removed_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE INDEX idx_workspace_groups_workspace_archived ON workspace_groups (workspace_id, archived_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE INDEX "IX_workspace_groups_created_by" ON workspace_groups (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    CREATE UNIQUE INDEX workspace_groups_workspace_name_active_key ON workspace_groups (workspace_id, name) WHERE archived_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430002845_AddWorkspaceGroupsPhase4') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260430002845_AddWorkspaceGroupsPhase4', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE TABLE access_requests (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        resource_type text NOT NULL,
        resource_id uuid NOT NULL,
        requester_id uuid NOT NULL,
        subject_type text NOT NULL,
        subject_id uuid NOT NULL,
        requested_role text NOT NULL,
        reason text,
        status text NOT NULL,
        decided_by uuid,
        decided_at timestamp with time zone,
        decision_reason text,
        resulting_grant_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        updated_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_access_requests" PRIMARY KEY (id),
        CONSTRAINT access_requests_requested_role_check CHECK (requested_role IN ('owner', 'admin', 'editor', 'commenter', 'viewer')),
        CONSTRAINT access_requests_resource_type_check CHECK (resource_type IN ('collection', 'document')),
        CONSTRAINT access_requests_status_check CHECK (status IN ('pending', 'approved', 'denied', 'cancelled')),
        CONSTRAINT access_requests_subject_type_check CHECK (subject_type IN ('user', 'group')),
        CONSTRAINT "FK_access_requests_resource_access_grants_resulting_grant_id" FOREIGN KEY (resulting_grant_id) REFERENCES resource_access_grants (id) ON DELETE SET NULL,
        CONSTRAINT "FK_access_requests_users_decided_by" FOREIGN KEY (decided_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_access_requests_users_requester_id" FOREIGN KEY (requester_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT "FK_access_requests_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE TABLE permission_notifications (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        recipient_user_id uuid NOT NULL,
        actor_user_id uuid,
        type text NOT NULL,
        resource_type text,
        resource_id uuid,
        access_request_id uuid,
        permission_grant_id uuid,
        title text NOT NULL,
        body text,
        action_url text,
        read_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_permission_notifications" PRIMARY KEY (id),
        CONSTRAINT permission_notifications_resource_type_check CHECK (resource_type IS NULL OR resource_type IN ('workspace', 'collection', 'document')),
        CONSTRAINT permission_notifications_type_check CHECK (type IN ('access_request.created', 'access_request.approved', 'access_request.denied', 'permission.grant_created', 'permission.grant_updated', 'permission.grant_revoked', 'group.member_added', 'group.member_removed')),
        CONSTRAINT "FK_permission_notifications_access_requests_access_request_id" FOREIGN KEY (access_request_id) REFERENCES access_requests (id) ON DELETE SET NULL,
        CONSTRAINT "FK_permission_notifications_resource_access_grants_permission_~" FOREIGN KEY (permission_grant_id) REFERENCES resource_access_grants (id) ON DELETE SET NULL,
        CONSTRAINT "FK_permission_notifications_users_actor_user_id" FOREIGN KEY (actor_user_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_permission_notifications_users_recipient_user_id" FOREIGN KEY (recipient_user_id) REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT "FK_permission_notifications_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE UNIQUE INDEX access_requests_pending_subject_key ON access_requests (workspace_id, resource_type, resource_id, subject_type, subject_id) WHERE status = 'pending';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX idx_access_requests_requester_status ON access_requests (requester_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX idx_access_requests_resource_status ON access_requests (resource_type, resource_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX idx_access_requests_workspace_status_created ON access_requests (workspace_id, status, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX "IX_access_requests_decided_by" ON access_requests (decided_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX "IX_access_requests_resulting_grant_id" ON access_requests (resulting_grant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX idx_permission_notifications_access_request ON permission_notifications (access_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX idx_permission_notifications_recipient_read_created ON permission_notifications (recipient_user_id, read_at, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX idx_permission_notifications_workspace_created ON permission_notifications (workspace_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX "IX_permission_notifications_actor_user_id" ON permission_notifications (actor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    CREATE INDEX "IX_permission_notifications_permission_grant_id" ON permission_notifications (permission_grant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260430005640_AddAccessRequestsAndPermissionNotificationsPhase5', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430014036_AddPermissionExpiryNotificationsPhase6') THEN
    ALTER TABLE permission_notifications DROP CONSTRAINT permission_notifications_type_check;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430014036_AddPermissionExpiryNotificationsPhase6') THEN
    ALTER TABLE permission_notifications ADD dedupe_key text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430014036_AddPermissionExpiryNotificationsPhase6') THEN
    CREATE UNIQUE INDEX permission_notifications_dedupe_key ON permission_notifications (dedupe_key) WHERE dedupe_key IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430014036_AddPermissionExpiryNotificationsPhase6') THEN
    ALTER TABLE permission_notifications ADD CONSTRAINT permission_notifications_type_check CHECK (type IN ('access_request.created', 'access_request.approved', 'access_request.denied', 'permission.grant_created', 'permission.grant_updated', 'permission.grant_revoked', 'permission.grant_expiring', 'permission.grant_expired', 'group.member_added', 'group.member_removed', 'group.member_expiring', 'group.member_expired'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430014036_AddPermissionExpiryNotificationsPhase6') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260430014036_AddPermissionExpiryNotificationsPhase6', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430052705_AddInternalShareLinksPhase7') THEN
    CREATE TABLE share_links (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        resource_type text NOT NULL,
        resource_id uuid NOT NULL,
        token_hash text NOT NULL,
        role_key text NOT NULL,
        audience text NOT NULL,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        expires_at timestamp with time zone,
        revoked_at timestamp with time zone,
        CONSTRAINT "PK_share_links" PRIMARY KEY (id),
        CONSTRAINT share_links_audience_check CHECK (audience IN ('workspace')),
        CONSTRAINT share_links_resource_type_check CHECK (resource_type IN ('collection', 'document')),
        CONSTRAINT share_links_role_key_check CHECK (role_key IN ('viewer', 'commenter')),
        CONSTRAINT "FK_share_links_users_created_by" FOREIGN KEY (created_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_share_links_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430052705_AddInternalShareLinksPhase7') THEN
    CREATE INDEX idx_share_links_expiry ON share_links (expires_at) WHERE revoked_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430052705_AddInternalShareLinksPhase7') THEN
    CREATE INDEX idx_share_links_resource ON share_links (workspace_id, resource_type, resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430052705_AddInternalShareLinksPhase7') THEN
    CREATE UNIQUE INDEX idx_share_links_token_hash ON share_links (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430052705_AddInternalShareLinksPhase7') THEN
    CREATE INDEX "IX_share_links_created_by" ON share_links (created_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430052705_AddInternalShareLinksPhase7') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260430052705_AddInternalShareLinksPhase7', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430063226_AddIamSyncPhase8') THEN
    DROP INDEX users_external_provider_subject_key;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430063226_AddIamSyncPhase8') THEN
    ALTER TABLE users RENAME COLUMN external_subject TO external_subject_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430063226_AddIamSyncPhase8') THEN
    ALTER TABLE workspace_groups ADD external_synced_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430063226_AddIamSyncPhase8') THEN
    CREATE UNIQUE INDEX workspace_groups_workspace_external_key ON workspace_groups (workspace_id, external_provider, external_group_id) WHERE external_provider IS NOT NULL AND external_group_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430063226_AddIamSyncPhase8') THEN
    CREATE UNIQUE INDEX users_external_provider_subject_key ON users (external_provider, external_subject_id) WHERE external_provider IS NOT NULL AND external_subject_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430063226_AddIamSyncPhase8') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260430063226_AddIamSyncPhase8', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    ALTER TABLE share_links DROP CONSTRAINT share_links_audience_check;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    ALTER TABLE resource_access_policies DROP CONSTRAINT resource_access_policies_link_mode_check;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    ALTER TABLE share_links ADD subject_email citext;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    CREATE TABLE resource_email_invites (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        workspace_id uuid NOT NULL,
        resource_type text NOT NULL,
        resource_id uuid NOT NULL,
        email citext NOT NULL,
        token_hash text NOT NULL,
        role_key text NOT NULL,
        status text NOT NULL,
        invited_by uuid,
        accepted_by uuid,
        revoked_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        expires_at timestamp with time zone NOT NULL,
        accepted_at timestamp with time zone,
        revoked_at timestamp with time zone,
        expired_at timestamp with time zone,
        CONSTRAINT "PK_resource_email_invites" PRIMARY KEY (id),
        CONSTRAINT resource_email_invites_resource_type_check CHECK (resource_type IN ('collection', 'document')),
        CONSTRAINT resource_email_invites_role_key_check CHECK (role_key IN ('viewer', 'commenter')),
        CONSTRAINT resource_email_invites_status_check CHECK (status IN ('pending', 'accepted', 'revoked', 'expired')),
        CONSTRAINT "FK_resource_email_invites_users_accepted_by" FOREIGN KEY (accepted_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_resource_email_invites_users_invited_by" FOREIGN KEY (invited_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_resource_email_invites_users_revoked_by" FOREIGN KEY (revoked_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT "FK_resource_email_invites_workspaces_workspace_id" FOREIGN KEY (workspace_id) REFERENCES workspaces (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    ALTER TABLE share_links ADD CONSTRAINT share_links_audience_check CHECK (audience IN ('workspace', 'external', 'public'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    ALTER TABLE resource_access_policies ADD CONSTRAINT resource_access_policies_link_mode_check CHECK (link_mode IN ('disabled', 'internal', 'external', 'public'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    CREATE INDEX idx_resource_email_invites_pending_expiry ON resource_email_invites (expires_at) WHERE status = 'pending';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    CREATE INDEX idx_resource_email_invites_resource ON resource_email_invites (workspace_id, resource_type, resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    CREATE UNIQUE INDEX idx_resource_email_invites_token_hash ON resource_email_invites (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    CREATE INDEX "IX_resource_email_invites_accepted_by" ON resource_email_invites (accepted_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    CREATE INDEX "IX_resource_email_invites_invited_by" ON resource_email_invites (invited_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    CREATE INDEX "IX_resource_email_invites_revoked_by" ON resource_email_invites (revoked_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    CREATE UNIQUE INDEX resource_email_invites_pending_resource_email_key ON resource_email_invites (workspace_id, resource_type, resource_id, email) WHERE status = 'pending';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260430070347_AddExternalShareLinksAndEmailInvitesPhase9') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260430070347_AddExternalShareLinksAndEmailInvitesPhase9', '8.0.11');
    END IF;
END $EF$;
COMMIT;

