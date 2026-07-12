/* =====================================================================
   BarRestauranteDb - Limite de consumo por COMANDA (antifraude)
   ComandaSetting: limite padrao por filial (gerente altera).
   CustomerOrder.CreditLimitAmount: limite da comanda no pedido —
   lancamento que ultrapassar e bloqueado; so o gerente libera mais.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF COL_LENGTH('dbo.CustomerOrder', 'CreditLimitAmount') IS NULL
BEGIN
    ALTER TABLE dbo.CustomerOrder ADD CreditLimitAmount DECIMAL(18,2) NULL;
END;
GO

IF OBJECT_ID('dbo.ComandaSetting', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComandaSetting
    (
        Id                 BIGINT IDENTITY(1,1) NOT NULL,
        BranchId           BIGINT        NOT NULL,
        DefaultLimitAmount DECIMAL(18,2) NOT NULL,
        CreatedAt          DATETIME2     NOT NULL CONSTRAINT DF_ComandaSetting_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt          DATETIME2     NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_ComandaSetting_IsActive DEFAULT (1),
        CONSTRAINT PK_ComandaSetting PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ComandaSetting_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT CK_ComandaSetting_DefaultLimitAmount CHECK (DefaultLimitAmount > 0)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_ComandaSetting_BranchId
        ON dbo.ComandaSetting (BranchId) WHERE IsActive = 1;
END;
GO

DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);
IF NOT EXISTS (SELECT 1 FROM dbo.ComandaSetting WHERE BranchId = @BranchId AND IsActive = 1)
    INSERT INTO dbo.ComandaSetting (BranchId, DefaultLimitAmount) VALUES (@BranchId, 150.00);
GO

SELECT BranchId, DefaultLimitAmount FROM dbo.ComandaSetting WHERE IsActive = 1;
GO
