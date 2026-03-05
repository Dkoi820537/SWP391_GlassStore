-- Add Stripe refund columns to the returns table
-- Run this script against the EyewearStore database before testing the refund feature

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.returns') AND name = 'stripe_payment_intent_id')
BEGIN
    ALTER TABLE [dbo].[returns] ADD [stripe_payment_intent_id] NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.returns') AND name = 'refund_resolved_at')
BEGIN
    ALTER TABLE [dbo].[returns] ADD [refund_resolved_at] DATETIME2 NULL;
END
GO
