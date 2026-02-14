-- =============================================
-- Migration: Add prescription_fee to lenses, cart_items, and order_items
-- Date: 2026-02-14
-- =============================================

-- 1. Add prescription_fee to lenses table (nullable, for per-product configuration)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('lenses') AND name = 'prescription_fee')
    ALTER TABLE [lenses] ADD [prescription_fee] decimal(18,2) NULL;

-- 2. Add prescription_id to cart_items table (nullable FK to prescription_profiles)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('cart_items') AND name = 'prescription_id')
BEGIN
    ALTER TABLE [cart_items] ADD [prescription_id] int NULL;
    ALTER TABLE [cart_items] ADD CONSTRAINT [FK_cart_items_prescription]
        FOREIGN KEY ([prescription_id]) REFERENCES [prescription_profiles]([prescription_id]) ON DELETE SET NULL;
END

-- 2b. Add prescription_fee to cart_items table (non-null, default 0)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('cart_items') AND name = 'prescription_fee')
BEGIN
    ALTER TABLE [cart_items] ADD [prescription_fee] decimal(18,2) NOT NULL DEFAULT 0.0;
END

-- 3. Add prescription_fee to order_items table (non-null, default 0)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('order_items') AND name = 'prescription_fee')
    ALTER TABLE [order_items] ADD [prescription_fee] decimal(18,2) NOT NULL DEFAULT 0.0;

-- 4. Backfill existing prescription lenses with the default 500,000 VND fee
UPDATE lenses SET prescription_fee = 500000 WHERE is_prescription = 1;
