-- Fold per-click rows into 10-second buckets.
-- Run after 008_product_clicks.sql so the click functions no longer read product_clicks.
-- Safe to run more than once.

CREATE TABLE IF NOT EXISTS caught.product_click_buckets (
    product_id integer NOT NULL,
    channel varchar(16) NOT NULL,
    bucket_start timestamptz NOT NULL,
    click_count integer NOT NULL,
    CONSTRAINT pk_product_click_buckets PRIMARY KEY (product_id, channel, bucket_start),
    CONSTRAINT ck_product_click_buckets_count CHECK (click_count > 0),
    CONSTRAINT ck_product_click_buckets_channel CHECK (
        channel IN ('tiktok', 'amazon', 'brand', 'product', 'notification')
    ),
    CONSTRAINT ck_product_click_buckets_aligned CHECK (
        bucket_start = date_bin('10 seconds', bucket_start, TIMESTAMPTZ '2000-01-01')
    ),
    CONSTRAINT fk_product_click_buckets_product FOREIGN KEY (product_id) REFERENCES caught.products (product_id)
);

CREATE INDEX IF NOT EXISTS ix_product_click_buckets_product_bucket
    ON caught.product_click_buckets (product_id, bucket_start);

ALTER TABLE caught.product_click_buckets DROP CONSTRAINT IF EXISTS ck_product_click_buckets_aligned;
ALTER TABLE caught.product_click_buckets
    ADD CONSTRAINT ck_product_click_buckets_aligned
    CHECK (bucket_start = date_bin('10 seconds', bucket_start, TIMESTAMPTZ '2000-01-01'));

DO $fn$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'caught'
          AND table_name = 'product_clicks'
          AND column_name = 'clicked_at'
    ) THEN
        INSERT INTO caught.product_click_buckets (product_id, channel, bucket_start, click_count)
        SELECT
            product_clicks.product_id,
            product_clicks.channel,
            date_bin('10 seconds', product_clicks.clicked_at, TIMESTAMPTZ '2000-01-01'),
            COUNT(*)::integer
        FROM caught.product_clicks
        GROUP BY 1, 2, 3
        ON CONFLICT (product_id, channel, bucket_start)
        DO UPDATE SET click_count = product_click_buckets.click_count + EXCLUDED.click_count;

        DROP FUNCTION IF EXISTS caught.insert_product_click(integer, text);
        DROP TABLE caught.product_clicks;
    END IF;
END;
$fn$;
