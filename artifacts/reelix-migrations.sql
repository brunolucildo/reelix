CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026133000_InitialUserAccounts') THEN
    CREATE TABLE user_accounts (
        id uuid NOT NULL,
        supabase_user_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_accounts" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026133000_InitialUserAccounts') THEN
    CREATE UNIQUE INDEX "IX_user_accounts_supabase_user_id" ON user_accounts (supabase_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026133000_InitialUserAccounts') THEN
    ALTER TABLE public.user_accounts ENABLE ROW LEVEL SECURITY;
    REVOKE ALL ON TABLE public.user_accounts FROM PUBLIC;
    DO $$
    DECLARE
        role_name text;
    BEGIN
        FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
            IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                EXECUTE format(
                    'REVOKE ALL ON TABLE public.user_accounts FROM %I',
                    role_name);
            END IF;
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026133000_InitialUserAccounts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261026133000_InitialUserAccounts', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026193000_AddExternalCatalogReferences') THEN
    CREATE TABLE catalog_external_references (
        id uuid NOT NULL,
        provider character varying(20) NOT NULL,
        external_id character varying(64) NOT NULL,
        content_type character varying(20) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_catalog_external_references" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026193000_AddExternalCatalogReferences') THEN
    CREATE UNIQUE INDEX "UX_catalog_external_references_external_key" ON catalog_external_references (provider, external_id, content_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026193000_AddExternalCatalogReferences') THEN
    ALTER TABLE public.catalog_external_references ENABLE ROW LEVEL SECURITY;
    REVOKE ALL ON TABLE public.catalog_external_references FROM PUBLIC;
    DO $$
    DECLARE
        role_name text;
    BEGIN
        FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
            IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                EXECUTE format(
                    'REVOKE ALL ON TABLE public.catalog_external_references FROM %I',
                    role_name);
            END IF;
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026193000_AddExternalCatalogReferences') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261026193000_AddExternalCatalogReferences', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031014300_AddUserOnboardingAndProfiles') THEN
    CREATE TABLE user_preferences (
        account_id uuid NOT NULL,
        current_step character varying(30) NOT NULL,
        completed_at timestamp with time zone,
        CONSTRAINT "PK_user_preferences" PRIMARY KEY (account_id),
        CONSTRAINT "FK_user_preferences_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031014300_AddUserOnboardingAndProfiles') THEN
    CREATE TABLE user_profiles (
        account_id uuid NOT NULL,
        display_name character varying(50),
        biography character varying(280),
        profile_is_public boolean NOT NULL DEFAULT FALSE,
        favorites_are_public boolean NOT NULL DEFAULT FALSE,
        watch_history_is_public boolean NOT NULL DEFAULT FALSE,
        CONSTRAINT "PK_user_profiles" PRIMARY KEY (account_id),
        CONSTRAINT "FK_user_profiles_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031014300_AddUserOnboardingAndProfiles') THEN
    CREATE TABLE user_favorite_titles (
        account_id uuid NOT NULL,
        title_id uuid NOT NULL,
        content_type character varying(20) NOT NULL,
        CONSTRAINT "PK_user_favorite_titles" PRIMARY KEY (account_id, title_id),
        CONSTRAINT "FK_user_favorite_titles_catalog_external_references_title_id" FOREIGN KEY (title_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_favorite_titles_user_preferences_account_id" FOREIGN KEY (account_id) REFERENCES user_preferences (account_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031014300_AddUserOnboardingAndProfiles') THEN
    CREATE TABLE user_genre_preferences (
        account_id uuid NOT NULL,
        content_type character varying(20) NOT NULL,
        genre_id integer NOT NULL,
        CONSTRAINT "PK_user_genre_preferences" PRIMARY KEY (account_id, content_type, genre_id),
        CONSTRAINT "FK_user_genre_preferences_user_preferences_account_id" FOREIGN KEY (account_id) REFERENCES user_preferences (account_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031014300_AddUserOnboardingAndProfiles') THEN
    CREATE INDEX "IX_user_favorite_titles_title_id" ON user_favorite_titles (title_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031014300_AddUserOnboardingAndProfiles') THEN
    DO $$
    DECLARE
        table_name text;
        role_name text;
    BEGIN
        FOREACH table_name IN ARRAY ARRAY[
            'user_profiles',
            'user_preferences',
            'user_genre_preferences',
            'user_favorite_titles'
        ] LOOP
            EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
            EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', table_name);
            FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                    EXECUTE format('REVOKE ALL ON TABLE public.%I FROM %I', table_name, role_name);
                END IF;
            END LOOP;
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031014300_AddUserOnboardingAndProfiles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261031014300_AddUserOnboardingAndProfiles', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE TABLE tracking_records (
        id uuid NOT NULL,
        account_id uuid NOT NULL,
        target_id uuid NOT NULL,
        item_type character varying(20) NOT NULL,
        series_id uuid,
        watched_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_tracking_records" PRIMARY KEY (id),
        CONSTRAINT "CK_tracking_records_target_type" CHECK ((item_type = 'movie' AND series_id IS NULL) OR (item_type = 'episode' AND series_id IS NOT NULL)),
        CONSTRAINT "FK_tracking_records_catalog_external_references_series_id" FOREIGN KEY (series_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_tracking_records_catalog_external_references_target_id" FOREIGN KEY (target_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_tracking_records_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE TABLE user_custom_lists (
        id uuid NOT NULL,
        account_id uuid NOT NULL,
        name character varying(80) NOT NULL,
        description character varying(500),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_custom_lists" PRIMARY KEY (id),
        CONSTRAINT "FK_user_custom_lists_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE TABLE user_watchlist_titles (
        account_id uuid NOT NULL,
        title_id uuid NOT NULL,
        content_type character varying(20) NOT NULL,
        added_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_watchlist_titles" PRIMARY KEY (account_id, title_id),
        CONSTRAINT "FK_user_watchlist_titles_catalog_external_references_title_id" FOREIGN KEY (title_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_watchlist_titles_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE TABLE user_custom_list_titles (
        list_id uuid NOT NULL,
        title_id uuid NOT NULL,
        content_type character varying(20) NOT NULL,
        position integer NOT NULL,
        added_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_custom_list_titles" PRIMARY KEY (list_id, title_id),
        CONSTRAINT "FK_user_custom_list_titles_catalog_external_references_title_id" FOREIGN KEY (title_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_custom_list_titles_user_custom_lists_list_id" FOREIGN KEY (list_id) REFERENCES user_custom_lists (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_tracking_records_account_series_target" ON tracking_records (account_id, series_id, target_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_tracking_records_account_type_watched_at" ON tracking_records (account_id, item_type, watched_at, id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_tracking_records_account_watched_at" ON tracking_records (account_id, watched_at, id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_tracking_records_series_id" ON tracking_records (series_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_tracking_records_target_id" ON tracking_records (target_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE UNIQUE INDEX "UX_tracking_records_account_target" ON tracking_records (account_id, target_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_user_custom_list_titles_list_position" ON user_custom_list_titles (list_id, position);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_user_custom_list_titles_title_id" ON user_custom_list_titles (title_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_user_custom_lists_account_updated_at" ON user_custom_lists (account_id, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_user_watchlist_titles_account_added_at" ON user_watchlist_titles (account_id, added_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    CREATE INDEX "IX_user_watchlist_titles_title_id" ON user_watchlist_titles (title_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    DO $$
    DECLARE
        table_name text;
        role_name text;
    BEGIN
        FOREACH table_name IN ARRAY ARRAY[
            'tracking_records',
            'user_custom_lists',
            'user_custom_list_titles',
            'user_watchlist_titles'
        ] LOOP
            EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
            EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', table_name);
            FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                    EXECUTE format('REVOKE ALL ON TABLE public.%I FROM %I', table_name, role_name);
                END IF;
            END LOOP;
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101141707_AddTrackingAndLists') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101141707_AddTrackingAndLists', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE TABLE catalog_episode_series (
        episode_id uuid NOT NULL,
        series_id uuid NOT NULL,
        CONSTRAINT "PK_catalog_episode_series" PRIMARY KEY (episode_id),
        CONSTRAINT "CK_catalog_episode_series_distinct_ids" CHECK (episode_id <> series_id),
        CONSTRAINT "FK_catalog_episode_series_catalog_external_references_episode_~" FOREIGN KEY (episode_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_catalog_episode_series_catalog_external_references_series_id" FOREIGN KEY (series_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE TABLE user_comments (
        id uuid NOT NULL,
        account_id uuid NOT NULL,
        target_id uuid NOT NULL,
        target_type character varying(20) NOT NULL,
        series_id uuid,
        content character varying(2000) NOT NULL,
        contains_spoiler boolean NOT NULL,
        possible_spoiler boolean NOT NULL,
        audience character varying(20) NOT NULL,
        automated_analysis_requested boolean NOT NULL,
        moderation_status character varying(20) NOT NULL,
        moderation_version integer NOT NULL,
        moderation_attempts integer NOT NULL,
        moderation_lease_id uuid,
        moderation_lease_until timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_comments" PRIMARY KEY (id),
        CONSTRAINT "CK_user_comments_audience" CHECK (audience IN ('private', 'public')),
        CONSTRAINT "CK_user_comments_content_length" CHECK (length(btrim(content)) BETWEEN 1 AND 2000),
        CONSTRAINT "CK_user_comments_moderation_status" CHECK (moderation_status IN ('pending', 'published', 'blocked')),
        CONSTRAINT "CK_user_comments_moderation_version" CHECK (moderation_version > 0 AND moderation_attempts >= 0),
        CONSTRAINT "CK_user_comments_target" CHECK ((target_type = 'episode' AND series_id IS NOT NULL) OR (target_type IN ('movie', 'series') AND series_id IS NULL)),
        CONSTRAINT "FK_user_comments_catalog_external_references_series_id" FOREIGN KEY (series_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_comments_catalog_external_references_target_id" FOREIGN KEY (target_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_comments_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE TABLE user_ratings (
        id uuid NOT NULL,
        account_id uuid NOT NULL,
        target_id uuid NOT NULL,
        target_type character varying(20) NOT NULL,
        series_id uuid,
        score numeric(2,1) NOT NULL,
        audience character varying(20) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_ratings" PRIMARY KEY (id),
        CONSTRAINT "CK_user_ratings_audience" CHECK (audience IN ('private', 'public')),
        CONSTRAINT "CK_user_ratings_score" CHECK (score >= 0 AND score <= 5 AND score * 2 = trunc(score * 2)),
        CONSTRAINT "CK_user_ratings_target" CHECK ((target_type = 'episode' AND series_id IS NOT NULL) OR (target_type IN ('movie', 'series') AND series_id IS NULL)),
        CONSTRAINT "FK_user_ratings_catalog_external_references_series_id" FOREIGN KEY (series_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_ratings_catalog_external_references_target_id" FOREIGN KEY (target_id) REFERENCES catalog_external_references (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_ratings_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE INDEX "IX_catalog_episode_series_series_id" ON catalog_episode_series (series_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE INDEX "IX_user_comments_account_target_created" ON user_comments (account_id, target_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE INDEX "IX_user_comments_moderation_queue" ON user_comments (moderation_status, moderation_lease_until);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE INDEX "IX_user_comments_series_id" ON user_comments (series_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE INDEX "IX_user_comments_target_visibility_created" ON user_comments (target_id, target_type, audience, moderation_status, created_at, id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE INDEX "IX_user_ratings_series_id" ON user_ratings (series_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE INDEX "IX_user_ratings_target_audience" ON user_ratings (target_id, target_type, audience);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    CREATE UNIQUE INDEX "UX_user_ratings_account_target" ON user_ratings (account_id, target_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    DO $$
    DECLARE
        table_name text;
        role_name text;
    BEGIN
        FOREACH table_name IN ARRAY ARRAY[
            'catalog_episode_series',
            'user_ratings',
            'user_comments'
        ] LOOP
            EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
            EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', table_name);
            FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                    EXECUTE format('REVOKE ALL ON TABLE public.%I FROM %I', table_name, role_name);
                END IF;
            END LOOP;
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101183022_AddRatingsComments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101183022_AddRatingsComments', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    ALTER TABLE user_profiles ADD favorites_shared_with_friends boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    ALTER TABLE user_profiles ADD is_discoverable boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    ALTER TABLE user_profiles ADD ratings_shared_with_friends boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    ALTER TABLE user_profiles ADD ratings_shared_with_friends_since timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    ALTER TABLE user_profiles ADD watch_history_shared_with_friends boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    ALTER TABLE user_profiles ADD watch_history_shared_with_friends_since timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE TABLE friend_relationships (
        id uuid NOT NULL,
        first_account_id uuid NOT NULL,
        second_account_id uuid NOT NULL,
        requester_account_id uuid NOT NULL,
        addressee_account_id uuid NOT NULL,
        status character varying(20) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_friend_relationships" PRIMARY KEY (id),
        CONSTRAINT "CK_friend_relationships_canonical_order" CHECK (first_account_id < second_account_id),
        CONSTRAINT "CK_friend_relationships_distinct_accounts" CHECK (first_account_id <> second_account_id),
        CONSTRAINT "CK_friend_relationships_participants" CHECK ((requester_account_id = first_account_id AND addressee_account_id = second_account_id) OR (requester_account_id = second_account_id AND addressee_account_id = first_account_id)),
        CONSTRAINT "CK_friend_relationships_status" CHECK (status IN ('Pending', 'Accepted', 'Declined', 'Cancelled', 'Removed')),
        CONSTRAINT "FK_friend_relationships_user_accounts_first_account_id" FOREIGN KEY (first_account_id) REFERENCES user_accounts (id) ON DELETE CASCADE,
        CONSTRAINT "FK_friend_relationships_user_accounts_second_account_id" FOREIGN KEY (second_account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE TABLE social_events (
        id uuid NOT NULL,
        event_type character varying(40) NOT NULL,
        event_version integer NOT NULL,
        actor_account_id uuid NOT NULL,
        recipient_account_id uuid NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_social_events" PRIMARY KEY (id),
        CONSTRAINT "CK_social_events_distinct_accounts" CHECK (actor_account_id <> recipient_account_id),
        CONSTRAINT "CK_social_events_version_and_type" CHECK (event_version > 0 AND event_type IN ('friend.requested', 'friend.accepted')),
        CONSTRAINT "FK_social_events_user_accounts_actor_account_id" FOREIGN KEY (actor_account_id) REFERENCES user_accounts (id) ON DELETE CASCADE,
        CONSTRAINT "FK_social_events_user_accounts_recipient_account_id" FOREIGN KEY (recipient_account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE INDEX "IX_user_profiles_display_name" ON user_profiles (display_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    ALTER TABLE user_profiles ADD CONSTRAINT "CK_user_profiles_friend_history_consent" CHECK (watch_history_shared_with_friends = (watch_history_shared_with_friends_since IS NOT NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    ALTER TABLE user_profiles ADD CONSTRAINT "CK_user_profiles_friend_ratings_consent" CHECK (ratings_shared_with_friends = (ratings_shared_with_friends_since IS NOT NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE INDEX "IX_friend_relationships_incoming" ON friend_relationships (addressee_account_id, status, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE INDEX "IX_friend_relationships_outgoing" ON friend_relationships (requester_account_id, status, updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE INDEX "IX_friend_relationships_second_account_id" ON friend_relationships (second_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE UNIQUE INDEX "UX_friend_relationships_pair" ON friend_relationships (first_account_id, second_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE INDEX "IX_social_events_actor_account_id" ON social_events (actor_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    CREATE INDEX "IX_social_events_recipient_occurred_at" ON social_events (recipient_account_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    DO $$
    DECLARE
        table_name text;
        role_name text;
    BEGIN
        FOREACH table_name IN ARRAY ARRAY[
            'friend_relationships',
            'social_events'
        ] LOOP
            EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
            EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', table_name);
            FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                    EXECUTE format('REVOKE ALL ON TABLE public.%I FROM %I', table_name, role_name);
                END IF;
            END LOOP;
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101190016_AddSocial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101190016_AddSocial', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE TABLE notification_devices (
        id uuid NOT NULL,
        account_id uuid NOT NULL,
        installation_id uuid NOT NULL,
        expo_push_token character varying(255) NOT NULL,
        platform character varying(10) NOT NULL,
        project_id uuid NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        last_registered_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_notification_devices" PRIMARY KEY (id),
        CONSTRAINT "CK_notification_devices_platform" CHECK (platform IN ('Ios', 'Android')),
        CONSTRAINT "FK_notification_devices_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE TABLE notification_preferences (
        account_id uuid NOT NULL,
        social_enabled boolean NOT NULL,
        new_episode_enabled boolean NOT NULL,
        premiere_enabled boolean NOT NULL,
        CONSTRAINT "PK_notification_preferences" PRIMARY KEY (account_id),
        CONSTRAINT "FK_notification_preferences_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE TABLE notification_processed_social_events (
        event_id uuid NOT NULL,
        outcome character varying(12) NOT NULL,
        processed_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_notification_processed_social_events" PRIMARY KEY (event_id),
        CONSTRAINT "CK_notification_processed_social_events_outcome" CHECK (outcome IN ('created', 'disabled', 'obsolete')),
        CONSTRAINT "FK_notification_processed_social_events_social_events_event_id" FOREIGN KEY (event_id) REFERENCES social_events (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE TABLE user_notifications (
        id uuid NOT NULL,
        account_id uuid NOT NULL,
        category character varying(20) NOT NULL,
        source_key character varying(160) NOT NULL,
        title character varying(100) NOT NULL,
        body character varying(240) NOT NULL,
        deep_link character varying(80) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        read_at timestamp with time zone,
        CONSTRAINT "PK_user_notifications" PRIMARY KEY (id),
        CONSTRAINT "CK_user_notifications_category" CHECK (category IN ('Social', 'NewEpisode', 'Premiere')),
        CONSTRAINT "FK_user_notifications_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE TABLE notification_deliveries (
        id uuid NOT NULL,
        notification_id uuid NOT NULL,
        device_id uuid NOT NULL,
        status character varying(12) NOT NULL,
        attempt_count integer NOT NULL,
        next_attempt_at timestamp with time zone NOT NULL,
        lease_id uuid,
        lease_until timestamp with time zone,
        expo_ticket_id character varying(100),
        expo_ticket_created_at timestamp with time zone,
        receipt_check_count integer NOT NULL,
        receipt_check_at timestamp with time zone,
        last_error_code character varying(80),
        completed_at timestamp with time zone,
        CONSTRAINT "PK_notification_deliveries" PRIMARY KEY (id),
        CONSTRAINT "CK_notification_deliveries_status" CHECK (status IN ('Pending', 'Ticketed', 'Completed', 'Failed', 'Cancelled')),
        CONSTRAINT "FK_notification_deliveries_notification_devices_device_id" FOREIGN KEY (device_id) REFERENCES notification_devices (id) ON DELETE CASCADE,
        CONSTRAINT "FK_notification_deliveries_user_notifications_notification_id" FOREIGN KEY (notification_id) REFERENCES user_notifications (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE INDEX "IX_notification_deliveries_device_id" ON notification_deliveries (device_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE INDEX "IX_notification_deliveries_pending" ON notification_deliveries (status, next_attempt_at, lease_until);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE INDEX "IX_notification_deliveries_receipts" ON notification_deliveries (status, receipt_check_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE UNIQUE INDEX "UX_notification_deliveries_notification_device" ON notification_deliveries (notification_id, device_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE INDEX "IX_notification_devices_account_active" ON notification_devices (account_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE UNIQUE INDEX "UX_notification_devices_active_token" ON notification_devices (expo_push_token) WHERE is_active;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE UNIQUE INDEX "UX_notification_devices_installation" ON notification_devices (installation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE INDEX "IX_user_notifications_account_created" ON user_notifications (account_id, created_at, id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    CREATE UNIQUE INDEX "UX_user_notifications_account_source" ON user_notifications (account_id, source_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    DO $$
    DECLARE
        table_name text;
        role_name text;
    BEGIN
        FOREACH table_name IN ARRAY ARRAY[
            'notification_preferences',
            'notification_devices',
            'user_notifications',
            'notification_deliveries',
            'notification_processed_social_events'
        ] LOOP
            EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
            EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', table_name);
            FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                    EXECUTE format('REVOKE ALL ON TABLE public.%I FROM %I', table_name, role_name);
                END IF;
            END LOOP;
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101191005_AddNotifications') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101191005_AddNotifications', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    ALTER TABLE catalog_external_references ADD metadata_updated_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    ALTER TABLE catalog_external_references ADD metadata_version integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    ALTER TABLE catalog_external_references ADD runtime_minutes integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    CREATE TABLE achievement_definitions (
        id uuid NOT NULL,
        code character varying(60) NOT NULL,
        title character varying(100) NOT NULL,
        description character varying(240) NOT NULL,
        rule_type character varying(30) NOT NULL,
        threshold integer NOT NULL,
        version integer NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT "PK_achievement_definitions" PRIMARY KEY (id),
        CONSTRAINT "CK_achievement_definitions_rule_type" CHECK (rule_type IN ('MovieCount', 'EpisodeCount', 'SeriesCount', 'MovieGenreCount')),
        CONSTRAINT "CK_achievement_definitions_threshold_version" CHECK (threshold > 0 AND version > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    CREATE TABLE catalog_reference_genres (
        catalog_reference_id uuid NOT NULL,
        genre_id integer NOT NULL,
        CONSTRAINT "PK_catalog_reference_genres" PRIMARY KEY (catalog_reference_id, genre_id),
        CONSTRAINT "CK_catalog_reference_genres_positive_id" CHECK (genre_id > 0),
        CONSTRAINT "FK_catalog_reference_genres_catalog_external_references_catalo~" FOREIGN KEY (catalog_reference_id) REFERENCES catalog_external_references (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    CREATE TABLE gamification_preferences (
        account_id uuid NOT NULL,
        badges_public boolean NOT NULL DEFAULT TRUE,
        ranking_enabled boolean NOT NULL DEFAULT FALSE,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_gamification_preferences" PRIMARY KEY (account_id),
        CONSTRAINT "FK_gamification_preferences_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    CREATE TABLE user_achievements (
        account_id uuid NOT NULL,
        definition_id uuid NOT NULL,
        status character varying(20) NOT NULL,
        first_earned_at timestamp with time zone NOT NULL,
        last_earned_at timestamp with time zone NOT NULL,
        last_revoked_at timestamp with time zone,
        reactivated_at timestamp with time zone,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_achievements" PRIMARY KEY (account_id, definition_id),
        CONSTRAINT "CK_user_achievements_status" CHECK (status IN ('Earned', 'Revoked')),
        CONSTRAINT "FK_user_achievements_achievement_definitions_definition_id" FOREIGN KEY (definition_id) REFERENCES achievement_definitions (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_achievements_user_accounts_account_id" FOREIGN KEY (account_id) REFERENCES user_accounts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    INSERT INTO achievement_definitions (id, code, description, is_active, rule_type, threshold, title, version)
    VALUES ('09000000-0000-0000-0000-000000000001', 'first-movie', 'Registre seu primeiro filme assistido.', TRUE, 'MovieCount', 1, 'Primeiro filme', 1);
    INSERT INTO achievement_definitions (id, code, description, is_active, rule_type, threshold, title, version)
    VALUES ('09000000-0000-0000-0000-000000000002', 'cinephile', 'Registre 10 filmes distintos como assistidos.', TRUE, 'MovieCount', 10, 'Cinéfilo', 1);
    INSERT INTO achievement_definitions (id, code, description, is_active, rule_type, threshold, title, version)
    VALUES ('09000000-0000-0000-0000-000000000003', 'first-marathon', 'Registre 10 episódios distintos como assistidos.', TRUE, 'EpisodeCount', 10, 'Maratona inicial', 1);
    INSERT INTO achievement_definitions (id, code, description, is_active, rule_type, threshold, title, version)
    VALUES ('09000000-0000-0000-0000-000000000004', 'series-in-focus', 'Assista a pelo menos um episódio em três séries distintas.', TRUE, 'SeriesCount', 3, 'Séries em foco', 1);
    INSERT INTO achievement_definitions (id, code, description, is_active, rule_type, threshold, title, version)
    VALUES ('09000000-0000-0000-0000-000000000005', 'genre-explorer', 'Assista a filmes de cinco gêneros canônicos distintos.', TRUE, 'MovieGenreCount', 5, 'Explorador de gêneros', 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    CREATE UNIQUE INDEX "UX_achievement_definitions_code" ON achievement_definitions (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    CREATE INDEX "IX_catalog_reference_genres_genre_id" ON catalog_reference_genres (genre_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    CREATE INDEX "IX_user_achievements_definition_id" ON user_achievements (definition_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    DO $$
    DECLARE
        table_name text;
        role_name text;
    BEGIN
        FOREACH table_name IN ARRAY ARRAY[
            'user_achievements',
            'gamification_preferences'
        ] LOOP
            EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
            EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', table_name);
            FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated', 'service_role'] LOOP
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                    EXECUTE format('REVOKE ALL ON TABLE public.%I FROM %I', table_name, role_name);
                END IF;
            END LOOP;
        END LOOP;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101193618_AddGamification') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101193618_AddGamification', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103133240_AddFixedAvatars') THEN
    ALTER TABLE user_profiles ADD avatar_key character varying(24);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103133240_AddFixedAvatars') THEN
    ALTER TABLE user_profiles ADD CONSTRAINT "CK_user_profiles_avatar_key" CHECK (avatar_key IS NULL OR avatar_key IN ('noir', 'cosmic', 'phantom', 'comedy', 'animation', 'adventure'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103133240_AddFixedAvatars') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261103133240_AddFixedAvatars', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103134321_ExpandFixedAvatars') THEN
    ALTER TABLE user_profiles DROP CONSTRAINT "CK_user_profiles_avatar_key";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103134321_ExpandFixedAvatars') THEN
    ALTER TABLE user_profiles ADD CONSTRAINT "CK_user_profiles_avatar_key" CHECK (avatar_key IS NULL OR avatar_key IN ('noir', 'cosmic', 'phantom', 'comedy', 'animation', 'adventure', 'robot', 'vampire', 'maestro'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261103134321_ExpandFixedAvatars') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261103134321_ExpandFixedAvatars', '10.0.13');
    END IF;
END $EF$;
COMMIT;

