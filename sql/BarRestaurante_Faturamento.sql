/* =====================================================================
   BarRestauranteDb - Modulo Faturamento
   CostType: tipo de custo (Fixo / Variavel)
   OperatingCost: custos lancados por filial e mes de referencia
   RevenueTarget: meta de faturamento por filial e mes
   + nova tela 'Faturamento' no controle de acessos
   Idempotente - pode executar mais de uma vez.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.CostType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CostType
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_CostType_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_CostType_IsActive DEFAULT (1),
        CONSTRAINT PK_CostType PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.OperatingCost', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperatingCost
    (
        Id             BIGINT IDENTITY(1,1) NOT NULL,
        BranchId       BIGINT        NOT NULL,
        CostTypeId     BIGINT        NOT NULL,
        Description    NVARCHAR(200) NOT NULL,
        Amount         DECIMAL(18,2) NOT NULL,
        ReferenceYear  INT           NOT NULL,
        ReferenceMonth INT           NOT NULL,
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_OperatingCost_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt      DATETIME2     NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_OperatingCost_IsActive DEFAULT (1),
        CONSTRAINT PK_OperatingCost PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OperatingCost_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_OperatingCost_CostType FOREIGN KEY (CostTypeId) REFERENCES dbo.CostType (Id),
        CONSTRAINT CK_OperatingCost_Amount CHECK (Amount > 0),
        CONSTRAINT CK_OperatingCost_ReferenceMonth CHECK (ReferenceMonth BETWEEN 1 AND 12),
        CONSTRAINT CK_OperatingCost_ReferenceYear CHECK (ReferenceYear BETWEEN 2000 AND 2100)
    );

    CREATE NONCLUSTERED INDEX IX_OperatingCost_BranchId ON dbo.OperatingCost (BranchId);
    CREATE NONCLUSTERED INDEX IX_OperatingCost_CostTypeId ON dbo.OperatingCost (CostTypeId);
    CREATE NONCLUSTERED INDEX IX_OperatingCost_BranchId_ReferenceYear_ReferenceMonth
        ON dbo.OperatingCost (BranchId, ReferenceYear, ReferenceMonth);
END;
GO

IF OBJECT_ID('dbo.RevenueTarget', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RevenueTarget
    (
        Id             BIGINT IDENTITY(1,1) NOT NULL,
        BranchId       BIGINT        NOT NULL,
        ReferenceYear  INT           NOT NULL,
        ReferenceMonth INT           NOT NULL,
        TargetAmount   DECIMAL(18,2) NOT NULL,
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_RevenueTarget_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt      DATETIME2     NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_RevenueTarget_IsActive DEFAULT (1),
        CONSTRAINT PK_RevenueTarget PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RevenueTarget_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT CK_RevenueTarget_TargetAmount CHECK (TargetAmount > 0),
        CONSTRAINT CK_RevenueTarget_ReferenceMonth CHECK (ReferenceMonth BETWEEN 1 AND 12),
        CONSTRAINT CK_RevenueTarget_ReferenceYear CHECK (ReferenceYear BETWEEN 2000 AND 2100)
    );

    CREATE NONCLUSTERED INDEX IX_RevenueTarget_BranchId ON dbo.RevenueTarget (BranchId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_RevenueTarget_BranchId_ReferenceYear_ReferenceMonth
        ON dbo.RevenueTarget (BranchId, ReferenceYear, ReferenceMonth) WHERE IsActive = 1;
END;
GO

/* ============================ Seed: tipos de custo ============================ */

IF NOT EXISTS (SELECT 1 FROM dbo.CostType)
BEGIN
    DBCC CHECKIDENT ('dbo.CostType', RESEED, 0);
    SET IDENTITY_INSERT dbo.CostType ON;

    INSERT INTO dbo.CostType (Id, Name) VALUES
        (1, N'Fixo'),
        (2, N'Variavel');

    BEGIN TRY SET IDENTITY_INSERT dbo.CostType OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ Nova tela: Faturamento ============================ */

INSERT INTO dbo.AppFeature (Code, Name)
SELECT 'Faturamento', N'Faturamento (custos e metas)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppFeature WHERE Code = 'Faturamento' AND IsActive = 1);
GO

SELECT Id, Code, Name FROM dbo.AppFeature WHERE IsActive = 1 ORDER BY Id;
GO
