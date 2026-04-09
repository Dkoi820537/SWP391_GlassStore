-- =======================================================================
-- MIGRATION SCRIPT: Transition to Soft Allocation
-- TARGET TABLE: [dbo].[product]
-- =======================================================================

-- -----------------------------------------------------------------------
-- STEP 1: Rename column — must run in its own isolated batch (before any
--         transaction) because sp_rename cannot be reliably used inside an
--         explicit transaction that also contains DDL in the same batch.
-- -----------------------------------------------------------------------
EXEC sp_rename 'dbo.product.inventory_qty', 'quantity_on_hand', 'COLUMN';
GO

-- -----------------------------------------------------------------------
-- STEP 2 & 3: Data fix + DDL additions — safe to wrap in a transaction.
-- The GO above ensures the rename is fully committed before SQL Server
-- parses the UPDATE below (avoiding "invalid column name" parse-time errors).
-- -----------------------------------------------------------------------
BEGIN TRANSACTION;

-- 2. Null-coerce the renamed column to 0 where applicable
UPDATE [dbo].[product]
SET [quantity_on_hand] = 0
WHERE [quantity_on_hand] IS NULL;

-- 3. Add soft-allocation reserved-stock column
ALTER TABLE [dbo].[product]
ADD [allocated_quantity] INT NOT NULL DEFAULT 0;

-- 4. Add RowVersion column for EF Core Optimistic Concurrency.
--    NOTE: ROWVERSION/TIMESTAMP is implicitly NOT NULL — do NOT specify NOT NULL.
ALTER TABLE [dbo].[product]
ADD [row_version] ROWVERSION;

COMMIT TRANSACTION;
GO
