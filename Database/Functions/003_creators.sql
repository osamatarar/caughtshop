-- Creator register and login lookup. Run after 002_tables.sql. Safe to run more than once.

CREATE OR REPLACE FUNCTION caught.make_handle(p_full_name text)
RETURNS text
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
DECLARE
    base text;
    candidate text;
    suffix text;
    stem text;
    n integer := 2;
    reserved text[] := ARRAY['login', 'signup', 'studio', 'dashboard', 'api', 'assets', 'health', 'admin'];
BEGIN
    base := lower(btrim(p_full_name));
    base := regexp_replace(base, '[^a-z0-9]+', '_', 'g');
    base := regexp_replace(base, '^_+|_+$', '', 'g');

    IF base IS NULL OR base = '' THEN
        base := 'creator';
    ELSIF length(base) < 3 THEN
        base := rpad(base, 3, 'x');
    END IF;

    IF length(base) > 30 THEN
        base := rtrim(left(base, 30), '_');
        IF length(base) < 3 THEN
            base := 'creator';
        END IF;
    END IF;

    candidate := base;
    WHILE candidate = ANY (reserved)
        OR EXISTS (SELECT 1 FROM caught.creators WHERE creators.handle = candidate)
    LOOP
        suffix := '_' || n::text;
        stem := rtrim(left(base, 30 - length(suffix)), '_');
        IF stem IS NULL OR stem = '' THEN
            stem := 'creator';
            stem := rtrim(left(stem, 30 - length(suffix)), '_');
        END IF;
        candidate := stem || suffix;
        n := n + 1;
        IF n > 10000 THEN
            RAISE EXCEPTION 'Handle could not be assigned.'
                USING ERRCODE = '22023';
        END IF;
    END LOOP;

    IF candidate !~ '^[a-z0-9_]{3,30}$' THEN
        RAISE EXCEPTION 'Handle could not be assigned.'
            USING ERRCODE = '22023';
    END IF;

    RETURN candidate;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.handle_available(p_handle text)
RETURNS boolean
LANGUAGE sql
STABLE
SET search_path = caught
AS $fn$
    SELECT NOT EXISTS (
        SELECT 1
        FROM caught.creators
        WHERE creators.handle = lower(btrim(p_handle))
    );
$fn$;

DROP FUNCTION IF EXISTS caught.register_creator(text, text, text);

CREATE OR REPLACE FUNCTION caught.register_creator(
    p_full_name text,
    p_email text,
    p_password_hash text,
    p_handle text
)
RETURNS TABLE (
    creator_id integer,
    full_name text,
    email text,
    handle text
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
DECLARE
    clean_name text;
    clean_email text;
    clean_handle text;
    new_id integer;
    reserved text[] := ARRAY['login', 'signup', 'studio', 'dashboard', 'api', 'assets', 'health', 'admin'];
BEGIN
    clean_name := btrim(p_full_name);
    clean_email := lower(btrim(p_email));
    clean_handle := lower(btrim(p_handle));

    IF clean_name IS NULL OR length(clean_name) = 0 OR length(clean_name) > 120 THEN
        RAISE EXCEPTION 'Enter your name.'
            USING ERRCODE = '22023';
    END IF;

    IF clean_email IS NULL OR length(clean_email) > 254 OR position('@' IN clean_email) < 2 THEN
        RAISE EXCEPTION 'Enter a valid email.'
            USING ERRCODE = '22023';
    END IF;

    IF p_password_hash IS NULL OR length(btrim(p_password_hash)) = 0 THEN
        RAISE EXCEPTION 'Enter your password.'
            USING ERRCODE = '22023';
    END IF;

    IF clean_handle IS NOT NULL AND left(clean_handle, 1) = '@' THEN
        clean_handle := substr(clean_handle, 2);
    END IF;

    IF clean_handle IS NULL OR clean_handle !~ '^[a-z0-9_]{3,30}$' THEN
        RAISE EXCEPTION 'Use 3-30 letters, numbers, or underscores.'
            USING ERRCODE = '22023';
    END IF;

    IF clean_handle = ANY (reserved) THEN
        RAISE EXCEPTION 'That link can''t be used.'
            USING ERRCODE = '22023';
    END IF;

    IF EXISTS (SELECT 1 FROM caught.creators WHERE creators.email = clean_email) THEN
        RAISE EXCEPTION 'Email is already registered.'
            USING ERRCODE = '23505';
    END IF;

    IF EXISTS (SELECT 1 FROM caught.creators WHERE creators.handle = clean_handle) THEN
        RAISE EXCEPTION 'That link is already taken.'
            USING ERRCODE = '23505';
    END IF;

    BEGIN
        new_id := nextval(pg_get_serial_sequence('caught.creators', 'creator_id'));

        INSERT INTO caught.creators (
            creator_id,
            full_name,
            email,
            password_hash,
            handle,
            created_by,
            updated_by
        )
        VALUES (
            new_id,
            clean_name,
            clean_email,
            p_password_hash,
            clean_handle,
            new_id,
            new_id
        );

        RETURN QUERY
        SELECT creators.creator_id, creators.full_name::text, creators.email::text, creators.handle::text
        FROM caught.creators
        WHERE creators.creator_id = new_id;
        RETURN;
    EXCEPTION
        WHEN unique_violation THEN
            IF SQLERRM LIKE '%uq_creators_email%' THEN
                RAISE EXCEPTION 'Email is already registered.'
                    USING ERRCODE = '23505';
            END IF;
            RAISE EXCEPTION 'That link is already taken.'
                USING ERRCODE = '23505';
    END;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.creator_login_lookup(p_email text)
RETURNS TABLE (
    creator_id integer,
    full_name text,
    email text,
    handle text,
    password_hash text
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
DECLARE
    clean_email text;
BEGIN
    clean_email := lower(btrim(p_email));
    RETURN QUERY
    SELECT
        creators.creator_id,
        creators.full_name::text,
        creators.email::text,
        creators.handle::text,
        creators.password_hash::text
    FROM caught.creators
    WHERE creators.email = clean_email;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.update_creator_profile(
    p_creator_id integer,
    p_tiktok_url text,
    p_instagram_url text,
    p_youtube_url text
)
RETURNS TABLE (
    creator_id integer,
    tiktok_url text,
    instagram_url text,
    youtube_url text
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
DECLARE
    clean_tiktok text;
    clean_instagram text;
    clean_youtube text;
BEGIN
    PERFORM creators.creator_id
    FROM caught.creators
    WHERE creators.creator_id = p_creator_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Creator was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    clean_tiktok := COALESCE(btrim(p_tiktok_url), '');
    clean_instagram := COALESCE(btrim(p_instagram_url), '');
    clean_youtube := COALESCE(btrim(p_youtube_url), '');

    RETURN QUERY
    UPDATE caught.creators
    SET
        tiktok_url = clean_tiktok,
        instagram_url = clean_instagram,
        youtube_url = clean_youtube,
        updated_by = p_creator_id
    WHERE creators.creator_id = p_creator_id
    RETURNING
        creators.creator_id,
        creators.tiktok_url::text,
        creators.instagram_url::text,
        creators.youtube_url::text;
END;
$fn$;
