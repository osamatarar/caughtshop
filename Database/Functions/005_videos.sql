-- Video reads and writes. Run after 004_products.sql. Safe to run more than once.

CREATE OR REPLACE FUNCTION caught.video_json(v caught.videos)
RETURNS jsonb
LANGUAGE sql
STABLE
SET search_path = caught
AS $fn$
    SELECT jsonb_build_object(
        'id', v.video_id,
        'tiktokUrl', v.tiktok_url,
        'thumbnailUrl', v.thumbnail_url,
        'caption', v.caption,
        'productId', v.product_id
    );
$fn$;

CREATE OR REPLACE FUNCTION caught.list_videos(p_creator_id integer)
RETURNS TABLE (
    id integer,
    tiktok_url text,
    thumbnail_url text,
    caption text,
    product_id integer
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
BEGIN
    IF NOT EXISTS (SELECT 1 FROM caught.creators WHERE creators.creator_id = p_creator_id) THEN
        RAISE EXCEPTION 'Creator was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    RETURN QUERY
    SELECT
        videos.video_id,
        videos.tiktok_url::text,
        videos.thumbnail_url::text,
        videos.caption::text,
        videos.product_id
    FROM caught.videos
    WHERE videos.creator_id = p_creator_id
    ORDER BY videos.video_id;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.get_video(p_video_id integer)
RETURNS TABLE (
    id integer,
    tiktok_url text,
    thumbnail_url text,
    caption text,
    product_id integer
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
BEGIN
    RETURN QUERY
    SELECT
        videos.video_id,
        videos.tiktok_url::text,
        videos.thumbnail_url::text,
        videos.caption::text,
        videos.product_id
    FROM caught.videos
    WHERE videos.video_id = p_video_id;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.insert_video(
    p_creator_id integer,
    p_tiktok_url text,
    p_thumbnail_url text,
    p_caption text,
    p_product_id integer
)
RETURNS TABLE (
    id integer,
    tiktok_url text,
    thumbnail_url text,
    caption text,
    product_id integer
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
BEGIN
    IF NOT EXISTS (SELECT 1 FROM caught.creators WHERE creators.creator_id = p_creator_id) THEN
        RAISE EXCEPTION 'Creator was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM caught.products
        WHERE products.product_id = p_product_id
          AND products.creator_id = p_creator_id
    ) THEN
        RAISE EXCEPTION 'Choose a product.'
            USING ERRCODE = '22023';
    END IF;

    RETURN QUERY
    INSERT INTO caught.videos (
        creator_id,
        product_id,
        tiktok_url,
        thumbnail_url,
        caption,
        created_by,
        updated_by
    )
    VALUES (
        p_creator_id,
        p_product_id,
        btrim(p_tiktok_url),
        btrim(p_thumbnail_url),
        btrim(p_caption),
        p_creator_id,
        p_creator_id
    )
    RETURNING video_id, tiktok_url::text, thumbnail_url::text, caption::text, product_id;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.update_video(
    p_video_id integer,
    p_creator_id integer,
    p_tiktok_url text,
    p_thumbnail_url text,
    p_caption text,
    p_product_id integer
)
RETURNS TABLE (
    id integer,
    tiktok_url text,
    thumbnail_url text,
    caption text,
    product_id integer
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
BEGIN
    PERFORM videos.video_id
    FROM caught.videos
    WHERE videos.video_id = p_video_id
      AND videos.creator_id = p_creator_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Video was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM caught.products
        WHERE products.product_id = p_product_id
          AND products.creator_id = p_creator_id
    ) THEN
        RAISE EXCEPTION 'Choose a product.'
            USING ERRCODE = '22023';
    END IF;

    RETURN QUERY
    UPDATE caught.videos
    SET
        tiktok_url = btrim(p_tiktok_url),
        thumbnail_url = btrim(p_thumbnail_url),
        caption = btrim(p_caption),
        product_id = p_product_id,
        updated_by = p_creator_id
    WHERE videos.video_id = p_video_id
      AND videos.creator_id = p_creator_id
    RETURNING video_id, tiktok_url::text, thumbnail_url::text, caption::text, product_id;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.delete_video(p_video_id integer, p_creator_id integer)
RETURNS void
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
BEGIN
    DELETE FROM caught.videos
    WHERE videos.video_id = p_video_id
      AND videos.creator_id = p_creator_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Video was not found.'
            USING ERRCODE = 'P0002';
    END IF;
END;
$fn$;
