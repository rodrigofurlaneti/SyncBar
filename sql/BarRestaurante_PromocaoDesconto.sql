/* =====================================================================
   BarRestauranteDb - Promocoes: tipo Desconto percentual
   PromotionType: 1 = EmDobro | 2 = Desconto
   DiscountRate: fracao (0.25 = 25%) obrigatoria no tipo Desconto.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.PromotionType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PromotionType
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_PromotionType_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_PromotionType_IsActive DEFAULT (1),
        CONSTRAINT PK_PromotionType PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PromotionType)
BEGIN
    DBCC CHECKIDENT ('dbo.PromotionType', RESEED, 0);
    SET IDENTITY_INSERT dbo.PromotionType ON;

    INSERT INTO dbo.PromotionType (Id, Name) VALUES
        (1, N'Em dobro'),
        (2, N'Desconto');

    BEGIN TRY SET IDENTITY_INSERT dbo.PromotionType OFF END TRY BEGIN CATCH END CATCH;
END;
GO

IF COL_LENGTH('dbo.Promotion', 'PromotionTypeId') IS NULL
BEGIN
    ALTER TABLE dbo.Promotion ADD
        PromotionTypeId BIGINT NOT NULL CONSTRAINT DF_Promotion_PromotionTypeId DEFAULT (1),
        DiscountRate DECIMAL(5,4) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Promotion_PromotionType')
BEGIN
    ALTER TABLE dbo.Promotion ADD
        CONSTRAINT FK_Promotion_PromotionType FOREIGN KEY (PromotionTypeId) REFERENCES dbo.PromotionType (Id),
        CONSTRAINT CK_Promotion_DiscountRate CHECK (
            PromotionTypeId <> 2 OR (DiscountRate IS NOT NULL AND DiscountRate > 0 AND DiscountRate < 1));

    CREATE NONCLUSTERED INDEX IX_Promotion_PromotionTypeId ON dbo.Promotion (PromotionTypeId);
END;
GO

/* ============================ Seed: domingo 25% na chapa mista ============================ */

DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);

INSERT INTO dbo.Promotion (BranchId, ProductId, Name, DayOfWeek, StartMinuteOfDay, EndMinuteOfDay, PromotionTypeId, DiscountRate)
SELECT @BranchId, P.Id, N'Domingo da chapa mista -25%', 0, 0, 1440, 2, 0.25
FROM dbo.Product AS P
WHERE P.Name LIKE N'%Chapa Mista%' AND P.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Promotion X
      WHERE X.BranchId = @BranchId AND X.ProductId = P.Id AND X.DayOfWeek = 0 AND X.IsActive = 1);

IF @@ROWCOUNT = 0
    PRINT 'Produto com "Chapa Mista" no nome nao encontrado — cadastre no Cardapio e crie a promocao pela tela Promocoes.';

SELECT PR.Name, PT.Name AS Tipo, PR.DiscountRate, PR.DayOfWeek
FROM dbo.Promotion PR JOIN dbo.PromotionType PT ON PT.Id = PR.PromotionTypeId
WHERE PR.IsActive = 1 ORDER BY PR.DayOfWeek;
GO
