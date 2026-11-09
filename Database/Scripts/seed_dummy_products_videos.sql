-- Reseed caught.products and caught.videos with eight realistic dummy rows each.
--
-- Prerequisites:
--   - Schema and functions are applied (caught schema exists).
--   - At least one row in caught.creators (first by creator_id is used).
--
-- This script deletes sale_alerts, product_click_buckets, videos, and products
-- (clears product backup links first). Creators are not modified.
--
-- Run:
--   psql "<connection_string>" -f Database/Scripts/seed_dummy_products_videos.sql
--
-- Verify:
--   SELECT count(*) FROM caught.products;
--   SELECT count(*) FROM caught.videos;

BEGIN;

DO $seed$
DECLARE
    v_creator_id integer;
BEGIN
    SELECT creators.creator_id
    INTO v_creator_id
    FROM caught.creators
    ORDER BY creators.creator_id
    LIMIT 1;

    IF v_creator_id IS NULL THEN
        RAISE EXCEPTION 'No creator found. Register at least one creator before running this script.';
    END IF;

    DELETE FROM caught.sale_alerts;
    DELETE FROM caught.product_click_buckets;
    DELETE FROM caught.videos;
    UPDATE caught.products SET backup_product_id = NULL;
    DELETE FROM caught.products;

    ALTER TABLE caught.products ALTER COLUMN product_id RESTART WITH 1001;

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
    VALUES
    (
        v_creator_id,
        1,
        'Stanley Quencher H2.0 FlowState Tumbler 40oz',
        'Insulated stainless steel tumbler with handle and straw. Keeps ice water cold for hours—my daily driver for gym and desk.',
        'https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800',
        45.00,
        'USD',
        'https://www.tiktok.com/shop/search?q=stanley%20quencher%2040oz',
        'https://www.amazon.com/dp/B0DFMTPSL2',
        'https://www.stanley1913.com/',
        'available',
        NULL,
        v_creator_id,
        v_creator_id
    ),
    (
        v_creator_id,
        2,
        'Apple AirPods Pro (2nd generation)',
        'Active noise cancellation, Adaptive Audio, and USB-C charging case. Crisp for calls and podcasts on the go.',
        'https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?w=800',
        249.00,
        'USD',
        'https://www.tiktok.com/shop/search?q=airpods%20pro',
        'https://www.amazon.com/dp/B0CHWRXH8B',
        'https://www.apple.com/airpods-pro/',
        'available',
        NULL,
        v_creator_id,
        v_creator_id
    ),
    (
        v_creator_id,
        3,
        'Dyson Airwrap Complete Long',
        'Multi-styler for damp-to-dry styling with barrels, brushes, and a firm smoothing brush—less heat damage than a flat iron.',
        'https://images.unsplash.com/photo-1522338140262-f46f5913618a?w=800',
        599.99,
        'USD',
        'https://www.tiktok.com/shop/search?q=dyson%20airwrap',
        'https://www.amazon.com/dp/B0B7CPSN2P',
        'https://www.dyson.com/hair-care/airwrap',
        'available',
        NULL,
        v_creator_id,
        v_creator_id
    ),
    (
        v_creator_id,
        4,
        'LANEIGE Lip Sleeping Mask',
        'Berry overnight lip mask that softens dry lips by morning. A TikTok classic—one jar lasts months.',
        'https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800',
        24.00,
        'USD',
        'https://www.tiktok.com/shop/search?q=laneige%20lip%20mask',
        'https://www.amazon.com/dp/B07XXPHQZK',
        'https://us.laneige.com/',
        'available',
        NULL,
        v_creator_id,
        v_creator_id
    ),
    (
        v_creator_id,
        5,
        'Ninja CREAMi Ice Cream Maker',
        'Turn frozen bases into ice cream, sorbet, and gelato at home. Great for protein ice cream and fruit-only recipes.',
        'https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=800',
        199.99,
        'USD',
        'https://www.tiktok.com/shop/search?q=ninja%20creami',
        'https://www.amazon.com/dp/B08QXBPJ9K',
        'https://www.ninjakitchen.com/',
        'available',
        NULL,
        v_creator_id,
        v_creator_id
    ),
    (
        v_creator_id,
        6,
        'Levi''s 501 Original Fit Jeans',
        'Straight leg, button fly, timeless denim. Size up one if you want a relaxed vintage fit.',
        'https://images.unsplash.com/photo-1542272604-787c3835535d?w=800',
        69.50,
        'USD',
        'https://www.tiktok.com/shop/search?q=levis%20501',
        'https://www.amazon.com/dp/B0018O7H32',
        'https://www.levi.com/',
        'available',
        NULL,
        v_creator_id,
        v_creator_id
    ),
    (
        v_creator_id,
        7,
        'Amazon Kindle Paperwhite (16 GB)',
        'Glare-free 6.8" display, weeks of battery life, and waterproof for poolside reading.',
        'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800',
        149.99,
        'USD',
        'https://www.tiktok.com/shop/search?q=kindle%20paperwhite',
        'https://www.amazon.com/dp/B0CFPJYX7S',
        'https://www.amazon.com/kindle',
        'available',
        NULL,
        v_creator_id,
        v_creator_id
    ),
    (
        v_creator_id,
        8,
        'Rhode Peptide Lip Treatment',
        'Peptide lip treatment that plumps and smooths without stickiness. The everyday backup when the sleeping mask is gone.',
        'https://images.unsplash.com/photo-1631730359585-38ee855e6349?w=800',
        18.00,
        'USD',
        'https://www.tiktok.com/shop/search?q=rhode%20peptide%20lip',
        'https://www.amazon.com/dp/B0D5L2Y8K9',
        'https://www.rhodeskin.com/',
        'available',
        NULL,
        v_creator_id,
        v_creator_id
    );

    UPDATE caught.products
    SET
        availability = 'out_of_stock',
        backup_product_id = (
            SELECT backup.product_id
            FROM caught.products AS backup
            WHERE backup.creator_id = v_creator_id
              AND backup.public_number = 8
        )
    WHERE creator_id = v_creator_id
      AND public_number = 4;

    UPDATE caught.creators
    SET
        tiktok_url = 'https://www.tiktok.com/@caughtshop',
        instagram_url = 'https://www.instagram.com/caughtshop',
        youtube_url = ''
    WHERE creator_id = v_creator_id;

    INSERT INTO caught.videos (
        creator_id,
        product_id,
        tiktok_url,
        thumbnail_url,
        caption,
        created_by,
        updated_by
    )
    SELECT
        v_creator_id,
        products.product_id,
        seed.tiktok_url,
        seed.thumbnail_url,
        seed.caption,
        v_creator_id,
        v_creator_id
    FROM (
        VALUES
        (
            1,
            'https://www.tiktok.com/@aguyandagolden/video/7453502111095999774',
            'https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=640',
            'POV: the Stanley cup that actually fits in your cup holder. Link in bio for the 40oz Quencher.'
        ),
        (
            2,
            'https://www.tiktok.com/@merissaria/video/7456849267966184734',
            'https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?w=640',
            'Noise canceling on the subway test—AirPods Pro vs the city. These won.'
        ),
        (
            3,
            'https://www.tiktok.com/@avemoves/video/7454158487539551511',
            'https://images.unsplash.com/photo-1522338140262-f46f5913618a?w=640',
            'Beach waves in 12 minutes with the Airwrap. No singed ends, I promise.'
        ),
        (
            4,
            'https://www.tiktok.com/@jacqueszewhipper/video/7452886336777899295',
            'https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=640',
            'Night routine: lip mask on, phone down. Wake up with soft lips every time.'
        ),
        (
            5,
            'https://www.tiktok.com/@tiktok/video/7106594317301234949',
            'https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=640',
            'High-protein vanilla ice cream in the CREAMi—40g protein and it tastes like soft serve.'
        ),
        (
            6,
            'https://www.tiktok.com/@charlidamelio/video/6806898190375220486',
            'https://images.unsplash.com/photo-1542272604-787c3835535d?w=640',
            '501 styling three ways: cuff, stack, or cropped. Vintage denim never misses.'
        ),
        (
            7,
            'https://www.tiktok.com/@merissaria/video/7456849267966184734',
            'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=640',
            'Reading 40 books this year because I stopped doom-scrolling and picked up the Kindle.'
        ),
        (
            8,
            'https://www.tiktok.com/@jacqueszewhipper/video/7452886336777899295',
            'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=640',
            'Lip mask is sold out again. This peptide treatment is the one I keep in my bag until it restocks.'
        )
    ) AS seed (public_number, tiktok_url, thumbnail_url, caption)
    INNER JOIN caught.products
        ON products.creator_id = v_creator_id
       AND products.public_number = seed.public_number;
END;
$seed$;

COMMIT;
