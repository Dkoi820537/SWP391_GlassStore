-- ============================================================
-- COD Deposit columns for the orders table
-- Adds deposit_amount, pending_balance, and payment_status
-- to support 50% deposit on Cash-on-Delivery orders.
-- ============================================================

-- 1. Deposit amount paid online (50% of TotalAmount for COD)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'orders' AND COLUMN_NAME = 'deposit_amount'
)
BEGIN
    ALTER TABLE dbo.orders
    ADD deposit_amount DECIMAL(18, 2) NOT NULL DEFAULT 0;
END
GO

-- 2. Remaining balance to be collected on delivery
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'orders' AND COLUMN_NAME = 'pending_balance'
)
BEGIN
    ALTER TABLE dbo.orders
    ADD pending_balance DECIMAL(18, 2) NOT NULL DEFAULT 0;
END
GO

-- 3. Payment status: Pending | DepositPaid_AwaitingCOD | FullyPaid
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'orders' AND COLUMN_NAME = 'payment_status'
)
BEGIN
    ALTER TABLE dbo.orders
    ADD payment_status NVARCHAR(50) NOT NULL DEFAULT 'Pending';
END
GO

-- Back-fill existing completed orders
UPDATE dbo.orders
SET payment_status = 'FullyPaid',
    deposit_amount = total_amount,
    pending_balance = 0
WHERE status IN ('Pending Confirmation', 'Confirmed', 'Processing',
                 'Shipped', 'Delivered', 'Completed')
  AND payment_status = 'Pending';
GO
