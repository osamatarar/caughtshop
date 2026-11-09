-- Product reads and writes. Run after 003_creators.sql. Safe to run more than once.

CREATE OR REPLACE FUNCTION caught.resolve_backup(
    p_creator_id integer,
    p_product_id integer,
    p_availability text,
    p_backup_product_id integer
)
RETURNS integer
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
DECLARE
    backup_creator integer;
    backup_availability varchar(20);
BEGIN
    IF p_availability <> 'out_of_stock' THEN
        RETURN NULL;
    END IF;

    IF p_backup_product_id IS NULL THEN
        RAISE EXCEPTION 'Choose a backup product.'
            USING ERRCODE = '22023';
    END IF;

    IF p_product_id IS NOT NULL AND p_backup_product_id = p_product_id THEN
        RAISE EXCEPTION 'Choose a different backup product.'
            USING ERRCODE = '22023';
    END IF;

    SELECT products.creator_id, products.availability
    INTO backup_creator, backup_availability
    FROM caught.products
    WHERE products.product_id = p_backup_product_id;

    IF backup_creator IS NULL OR backup_creator <> p_creator_id THEN
        RAISE EXCEPTION 'Backup product must belong to the same creator.'
            USING ERRCODE = '22023';
    END IF;

    IF backup_availability <> 'available' THEN
        RAISE EXCEPTION 'Backup product must be in stock.'
            USING ERRCODE = '22023';
    END IF;

    RETURN p_backup_product_id;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.product_json(p caught.products)
RETURNS jsonb
LANGUAGE sql
STABLE
SET search_path = caught
AS $fn$
    SELECT jsonb_build_object(
        'id', p.product_id,
        'publicNumber', p.public_number,
        'name', p.name,
        'description', p.description,
        'imageUrl', p.image_url,
        'price', p.price,
        'currency', p.currency,
        'tiktokUrl', p.tiktok_url,
        'amazonUrl', p.amazon_url,
        'brandUrl', p.brand_url,
        'availability', p.availability,
        'backupProductId', p.backup_product_id
    );
$fn$;

CREATE OR REPLACE FUNCTION caught.list_products(p_creator_id integer)
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
    WHERE products.creator_id = p_creator_id
    ORDER BY products.public_number;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.get_product(p_product_id integer)
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
    WHERE products.product_id = p_product_id;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.insert_product(
    p_creator_id integer,
    p_name text,
    p_description text,
    p_image_url text,
    p_price numeric,
    p_currency text,
    p_tiktok_url text,
    p_amazon_url text,
    p_brand_url text,
    p_availability text,
    p_backup_product_id integer
)
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
SET search_path = caught
AS $fn$
#variable_conflict use_column
DECLARE
    new_number integer;
    resolved_backup integer;
BEGIN
    PERFORM creators.creator_id
    FROM caught.creators
    WHERE creators.creator_id = p_creator_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Creator was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    resolved_backup := caught.resolve_backup(p_creator_id, NULL, p_availability, p_backup_product_id);

    SELECT COALESCE(MAX(products.public_number), 0) + 1
    INTO new_number
    FROM caught.products
    WHERE products.creator_id = p_creator_id;

    RETURN QUERY
    INSERT INTO caught.products (
        creator_id,
        public_number,
        name,
        description,
        image_url,
        price,
        currency,
        tiktok_url,
        amazon_url,
        brand_url,
        availability,
        backup_product_id,
        created_by,
        updated_by
    )
    VALUES (
        p_creator_id,
        new_number,
        btrim(p_name),
        btrim(p_description),
        btrim(p_image_url),
        p_price,
        p_currency,
        btrim(p_tiktok_url),
        COALESCE(btrim(p_amazon_url), ''),
        COALESCE(btrim(p_brand_url), ''),
        p_availability,
        resolved_backup,
        p_creator_id,
        p_creator_id
    )
    RETURNING
        product_id,
        public_number,
        name::text,
        description::text,
        image_url::text,
        price::numeric,
        currency::text,
        tiktok_url::text,
        amazon_url::text,
        brand_url::text,
        availability::text,
        backup_product_id;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.update_product(
    p_product_id integer,
    p_creator_id integer,
    p_name text,
    p_description text,
    p_image_url text,
    p_price numeric,
    p_currency text,
    p_tiktok_url text,
    p_amazon_url text,
    p_brand_url text,
    p_availability text,
    p_backup_product_id integer
)
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
SET search_path = caught
AS $fn$
#variable_conflict use_column
DECLARE
    resolved_backup integer;
BEGIN
    PERFORM products.product_id
    FROM caught.products
    WHERE products.product_id = p_product_id
      AND products.creator_id = p_creator_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Product was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    IF p_availability = 'out_of_stock' AND EXISTS (
        SELECT 1
        FROM caught.products
        WHERE products.backup_product_id = p_product_id
          AND products.product_id <> p_product_id
    ) THEN
        RAISE EXCEPTION 'This product is a backup for another product.'
            USING ERRCODE = '22023';
    END IF;

    resolved_backup := caught.resolve_backup(p_creator_id, p_product_id, p_availability, p_backup_product_id);

    RETURN QUERY
    UPDATE caught.products
    SET
        name = btrim(p_name),
        description = btrim(p_description),
        image_url = btrim(p_image_url),
        price = p_price,
        currency = p_currency,
        tiktok_url = btrim(p_tiktok_url),
        amazon_url = COALESCE(btrim(p_amazon_url), ''),
        brand_url = COALESCE(btrim(p_brand_url), ''),
        availability = p_availability,
        backup_product_id = resolved_backup,
        updated_by = p_creator_id
    WHERE products.product_id = p_product_id
      AND products.creator_id = p_creator_id
    RETURNING
        product_id,
        public_number,
        name::text,
        description::text,
        image_url::text,
        price::numeric,
        currency::text,
        tiktok_url::text,
        amazon_url::text,
        brand_url::text,
        availability::text,
        backup_product_id;
END;
$fn$;

CREATE OR REPLACE FUNCTION caught.delete_product(p_product_id integer, p_creator_id integer)
RETURNS void
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
BEGIN
    PERFORM products.product_id
    FROM caught.products
    WHERE products.product_id = p_product_id
      AND products.creator_id = p_creator_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Product was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    IF EXISTS (SELECT 1 FROM caught.videos WHERE videos.product_id = p_product_id) THEN
        RAISE EXCEPTION 'Delete the videos for this product first.'
            USING ERRCODE = '22023';
    END IF;

    DELETE FROM caught.product_click_buckets
    WHERE product_click_buckets.product_id = p_product_id;

    DELETE FROM caught.sale_alerts
    WHERE sale_alerts.product_id = p_product_id;

    UPDATE caught.products
    SET
        backup_product_id = NULL,
        updated_by = p_creator_id
    WHERE products.backup_product_id = p_product_id;

    DELETE FROM caught.products
    WHERE products.product_id = p_product_id
      AND products.creator_id = p_creator_id;
END;
$fn$;
