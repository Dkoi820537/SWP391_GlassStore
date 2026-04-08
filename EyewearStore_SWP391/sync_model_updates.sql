BEGIN TRANSACTION;
GO

ALTER TABLE [orders] ADD [cancellation_idempotency_key] nvarchar(36) NULL;
GO

ALTER TABLE [orders] ADD [cancelled_at] datetime2 NULL;
GO

ALTER TABLE [orders] ADD [deposit_amount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [orders] ADD [order_group_id] nvarchar(36) NULL;
GO

ALTER TABLE [orders] ADD [order_type] nvarchar(20) NOT NULL DEFAULT N'Standard';
GO

ALTER TABLE [orders] ADD [payment_status] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [orders] ADD [pending_balance] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [orders] ADD [refund_amount] decimal(18,2) NULL;
GO

CREATE TABLE [order_status_history] (
    [history_id] int NOT NULL IDENTITY,
    [order_id] int NOT NULL,
    [status] nvarchar(100) NOT NULL,
    [actor] nvarchar(100) NOT NULL DEFAULT N'System',
    [note] nvarchar(500) NULL,
    [created_at] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_order_status_history] PRIMARY KEY ([history_id]),
    CONSTRAINT [FK_order_status_history_orders] FOREIGN KEY ([order_id]) REFERENCES [orders] ([order_id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_orders_order_group] ON [orders] ([order_group_id]);
GO

CREATE INDEX [IX_order_status_history_created] ON [order_status_history] ([created_at]);
GO

CREATE INDEX [IX_order_status_history_order] ON [order_status_history] ([order_id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260406155919_SyncModelUpdate', N'8.0.0');
GO

COMMIT;
GO

