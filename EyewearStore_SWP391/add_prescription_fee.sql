-- =============================================
-- Migration: Add prescription_fee to lenses, cart_items, and order_items
-- Date: 2026-02-14
-- =============================================

-- 1. Add prescription_fee to lenses table (nullable, for per-product configuration)
ALTER TABLE [lenses] ADD [prescription_fee] decimal(18,2) NULL;

-- 2. Add prescription_fee to cart_items table (non-null, default 0)
ALTER TABLE [cart_items] ADD [prescription_fee] decimal(18,2) NOT NULL DEFAULT 0.0;

-- 3. Add prescription_fee to order_items table (non-null, default 0)
ALTER TABLE [order_items] ADD [prescription_fee] decimal(18,2) NOT NULL DEFAULT 0.0;

-- 4. Backfill existing prescription lenses with the default 500,000 VND fee
UPDATE lenses SET prescription_fee = 500000 WHERE is_prescription = 1;
