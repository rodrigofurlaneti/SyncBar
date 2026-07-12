/* =====================================================================
   BarRestauranteDb - Imagem do produto no cardapio
   Product.ImageUrl: caminho relativo servido pela API
   (wwwroot/uploads/products). Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF COL_LENGTH('dbo.Product', 'ImageUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Product ADD ImageUrl NVARCHAR(300) NULL;
END;
GO
