-- Sale alert create. A repeat contact for the same product returns the existing row.
-- inserted is true only when this call stored a new contact.
-- Run after 004_products.sql. Safe to run more than once.

DROP FUNCTION IF EXISTS caught.insert_sale_alert(integer, text, text);

CREATE OR REPLACE FUNCTION caught.insert_sale_alert(
    p_product_id integer,
    p_channel text,
    p_contact text
)
RETURNS TABLE (
    id integer,
    product_id integer,
    channel text,
    contact text,
    inserted boolean
)
LANGUAGE plpgsql
SET search_path = caught
AS $fn$
#variable_conflict use_column
DECLARE
    owner_id integer;
    was_inserted boolean := false;
BEGIN
    IF p_channel NOT IN ('email', 'phone') OR p_contact IS NULL OR length(btrim(p_contact)) = 0 THEN
        RAISE EXCEPTION 'Enter an email or a phone number.'
            USING ERRCODE = '22023';
    END IF;

    SELECT products.creator_id
    INTO owner_id
    FROM caught.products
    WHERE products.product_id = p_product_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Product was not found.'
            USING ERRCODE = 'P0002';
    END IF;

    RETURN QUERY
    SELECT
        sale_alerts.sale_alert_id,
        sale_alerts.product_id,
        sale_alerts.channel::text,
        sale_alerts.contact::text,
        false
    FROM caught.sale_alerts
    WHERE sale_alerts.product_id = p_product_id
      AND sale_alerts.contact = p_contact;

    IF FOUND THEN
        RETURN;
    END IF;

    BEGIN
        INSERT INTO caught.sale_alerts (
            product_id,
            channel,
            contact,
            created_by,
            updated_by
        )
        VALUES (
            p_product_id,
            p_channel,
            p_contact,
            owner_id,
            owner_id
        );
        was_inserted := true;
    EXCEPTION
        WHEN unique_violation THEN
            was_inserted := false;
    END;

    RETURN QUERY
    SELECT
        sale_alerts.sale_alert_id,
        sale_alerts.product_id,
        sale_alerts.channel::text,
        sale_alerts.contact::text,
        was_inserted
    FROM caught.sale_alerts
    WHERE sale_alerts.product_id = p_product_id
      AND sale_alerts.contact = p_contact;
END;
$fn$;
