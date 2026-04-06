-- ============================================================
-- Cancellation & Refund tracking columns for the orders table
-- Adds idempotency key, refund amount, and cancellation timestamp
-- to support the order cancellation + automated refund feature.
-- ============================================================

-- 1. Idempotency key to prevent duplicate Stripe refund requests
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'orders' AND COLUMN_NAME = 'cancellation_idempotency_key'
)
BEGIN
    ALTER TABLE dbo.orders
    ADD cancellation_idempotency_key NVARCHAR(36) NULL;
END
GO

-- 2. Amount actually refunded via Stripe
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'orders' AND COLUMN_NAME = 'refund_amount'
)
BEGIN
    ALTER TABLE dbo.orders
    ADD refund_amount DECIMAL(18, 2) NULL;
END
GO

-- 3. Timestamp recording when the cancellation was finalised
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'orders' AND COLUMN_NAME = 'cancelled_at'
)
BEGIN
    ALTER TABLE dbo.orders
    ADD cancelled_at DATETIME2 NULL;
END
GO
