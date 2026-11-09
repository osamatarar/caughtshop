-- Public shelf for one handle. Run after 005_videos.sql. Safe to run more than once.

CREATE OR REPLACE FUNCTION caught.shelf_by_handle(p_handle text)
RETURNS jsonb
LANGUAGE plpgsql
STABLE
SET search_path = caught
AS $fn$
DECLARE
    creator_row caught.creators%ROWTYPE;
    normalized text;
BEGIN
    normalized := lower(btrim(p_handle));
    SELECT *
    INTO creator_row
    FROM caught.creators
    WHERE creators.handle = normalized;

    IF NOT FOUND THEN
        RETURN NULL;
    END IF;

    RETURN jsonb_build_object(
        'creatorId', creator_row.creator_id,
        'fullName', creator_row.full_name,
        'handle', creator_row.handle,
        'tiktokUrl', creator_row.tiktok_url,
        'instagramUrl', creator_row.instagram_url,
        'youtubeUrl', creator_row.youtube_url,
        'products', COALESCE((
            SELECT jsonb_agg(caught.product_json(products) ORDER BY products.public_number)
            FROM caught.products
            WHERE products.creator_id = creator_row.creator_id
        ), '[]'::jsonb),
        'videos', COALESCE((
            SELECT jsonb_agg(caught.video_json(videos) ORDER BY videos.video_id)
            FROM caught.videos
            WHERE videos.creator_id = creator_row.creator_id
        ), '[]'::jsonb)
    );
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.shelf_product(p_handle text, p_public_number integer)
RETURNS TABLE (
    id integer,
    public_number integer,
    name text,
    description text,
    image_url text,
    price numeric,
    currency text,
    tiktok_url text,
    amazon_url text,
    brand_url text,
    availability text,
    backup_product_id integer
)
LANGUAGE plpgsql
STABLE
SET search_path = caught
AS $fn$
#variable_conflict use_column
BEGIN
    RETURN QUERY
    SELECT
        products.product_id,
        products.public_number,
        products.name::text,
        products.description::text,
        products.image_url::text,
        products.price::numeric,
        products.currency::text,
        products.tiktok_url::text,
        products.amazon_url::text,
        products.brand_url::text,
        products.availability::text,
        products.backup_product_id
    FROM caught.products
    INNER JOIN caught.creators ON creators.creator_id = products.creator_id
    WHERE creators.handle = lower(btrim(p_handle))
      AND products.public_number = p_public_number;
END;
$fn$;
