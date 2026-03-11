-- =========================
-- FRAME–LENS-TYPE COMPATIBILITY
-- Maps each frame to the lens types it physically supports.
-- =========================
CREATE TABLE dbo.frame_compatible_lens_types (
    frame_product_id  INT           NOT NULL,
    lens_type         NVARCHAR(50)  NOT NULL,
    PRIMARY KEY (frame_product_id, lens_type),
    FOREIGN KEY (frame_product_id) REFERENCES dbo.product(product_id)
);
GO
