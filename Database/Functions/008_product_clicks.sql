-- Product click buckets and creator reports. Run after 002_tables.sql.
-- Run before 009 when an older product_clicks table is still present. Safe to run more than once.

DROP FUNCTION IF EXISTS caught.insert_product_click(integer, text);

CREATE OR REPLACE FUNCTION caught.add_product_clicks(
    p_product_ids integer[],
    p_channels text[],
    p_bucket_starts timestamptz[],
    p_counts integer[]
)
RETURNS void
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
BEGIN
    IF p_product_ids IS NULL
        OR p_channels IS NULL
        OR p_bucket_starts IS NULL
        OR p_counts IS NULL
        OR cardinality(p_product_ids) IS DISTINCT FROM cardinality(p_channels)
        OR cardinality(p_product_ids) IS DISTINCT FROM cardinality(p_bucket_starts)
        OR cardinality(p_product_ids) IS DISTINCT FROM cardinality(p_counts) THEN
        RAISE EXCEPTION 'Click batch is not valid.'
            USING ERRCODE = '22023';
    END IF;

    INSERT INTO caught.product_click_buckets (product_id, channel, bucket_start, click_count)
    SELECT
        batch.product_id,
        batch.channel,
        batch.bucket_start,
        SUM(batch.click_count)::integer
    FROM unnest(p_product_ids, p_channels, p_bucket_starts, p_counts)
        AS batch(product_id, channel, bucket_start, click_count)
    INNER JOIN caught.products
        ON products.product_id = batch.product_id
    WHERE batch.click_count > 0
      AND batch.channel IN ('tiktok', 'amazon', 'brand', 'product', 'notification')
      AND batch.bucket_start = date_bin('10 seconds', batch.bucket_start, TIMESTAMPTZ '2000-01-01')
      AND (
            batch.channel IN ('product', 'notification')
            OR (batch.channel = 'tiktok' AND btrim(products.tiktok_url) <> '')
            OR (batch.channel = 'amazon' AND btrim(products.amazon_url) <> '')
            OR (batch.channel = 'brand' AND btrim(products.brand_url) <> '')
          )
    GROUP BY batch.product_id, batch.channel, batch.bucket_start
    ON CONFLICT (product_id, channel, bucket_start)
    DO UPDATE SET click_count = product_click_buckets.click_count + EXCLUDED.click_count;
END;
$fn$;

DROP FUNCTION IF EXISTS caught.product_click_totals(integer, integer);

CREATE OR REPLACE FUNCTION caught.product_click_totals(
    p_creator_id integer,
    p_product_id integer
)
RETURNS TABLE (
    tiktok integer,
    amazon integer,
    brand integer,
    product integer,
    notification integer
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM caught.products
        WHERE products.product_id = p_product_id
          AND products.creator_id = p_creator_id
    ) THEN
        RETURN;
    END IF;

    RETURN QUERY
    SELECT
        COALESCE(SUM(buckets.click_count) FILTER (WHERE buckets.channel = 'tiktok'), 0)::integer,
        COALESCE(SUM(buckets.click_count) FILTER (WHERE buckets.channel = 'amazon'), 0)::integer,
        COALESCE(SUM(buckets.click_count) FILTER (WHERE buckets.channel = 'brand'), 0)::integer,
        COALESCE(SUM(buckets.click_count) FILTER (WHERE buckets.channel = 'product'), 0)::integer,
        COALESCE(SUM(buckets.click_count) FILTER (WHERE buckets.channel = 'notification'), 0)::integer
    FROM caught.product_click_buckets AS buckets
    WHERE buckets.product_id = p_product_id;
END;
$fn$;

DROP FUNCTION IF EXISTS caught.product_click_report(integer, date, date, text);

CREATE OR REPLACE FUNCTION caught.product_click_report(
    p_creator_id integer,
    p_start date,
    p_end date,
    p_time_zone text
)
RETURNS TABLE (
    kind text,
    clicked_on text,
    product_id integer,
    tiktok integer,
    amazon integer,
    brand integer,
    product integer,
    notification integer
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
BEGIN
    IF p_time_zone IS NULL OR length(btrim(p_time_zone)) = 0 THEN
        RAISE EXCEPTION 'Choose a time zone.'
            USING ERRCODE = '22023';
    END IF;

    IF p_start IS NULL OR p_end IS NULL OR p_end < p_start THEN
        RAISE EXCEPTION 'Start date must be on or before the end date.'
            USING ERRCODE = '22023';
    END IF;

    IF (p_end - p_start) > 365 THEN
        RAISE EXCEPTION 'Choose a range of 366 days or fewer.'
            USING ERRCODE = '22023';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM caught.creators WHERE creators.creator_id = p_creator_id) THEN
        RAISE EXCEPTION 'Creator was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    BEGIN
        PERFORM now() AT TIME ZONE p_time_zone;
    EXCEPTION
        WHEN invalid_parameter_value THEN
            RAISE EXCEPTION 'Choose a time zone.'
                USING ERRCODE = '22023';
    END;

    RETURN QUERY
    WITH clicks AS (
        SELECT
            (buckets.bucket_start AT TIME ZONE p_time_zone)::date AS click_date,
            buckets.product_id,
            buckets.channel,
            buckets.click_count
        FROM caught.product_click_buckets AS buckets
        INNER JOIN caught.products
            ON products.product_id = buckets.product_id
        WHERE products.creator_id = p_creator_id
          AND buckets.bucket_start >= (p_start::timestamp AT TIME ZONE p_time_zone)
          AND buckets.bucket_start < ((p_end + 1)::timestamp AT TIME ZONE p_time_zone)
    )
    SELECT
        'day'::text,
        to_char(series.day::date, 'YYYY-MM-DD'),
        NULL::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'tiktok'), 0)::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'amazon'), 0)::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'brand'), 0)::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'product'), 0)::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'notification'), 0)::integer
    FROM generate_series(p_start, p_end, interval '1 day') AS series(day)
    LEFT JOIN clicks ON clicks.click_date = series.day::date
    GROUP BY series.day
    UNION ALL
    SELECT
        'product'::text,
        NULL::text,
        clicks.product_id,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'tiktok'), 0)::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'amazon'), 0)::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'brand'), 0)::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'product'), 0)::integer,
        COALESCE(SUM(clicks.click_count) FILTER (WHERE clicks.channel = 'notification'), 0)::integer
    FROM clicks
    GROUP BY clicks.product_id
    ORDER BY 1, 2, 3;
END;
$fn$;
