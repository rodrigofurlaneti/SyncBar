/* =====================================================================
   BarRestauranteDb - Acesso por funcionalidade (telas)
   AppFeature: catalogo de telas do sistema
   JobTitleFeature: telas liberadas por CARGO
   AppUserFeature: telas extras liberadas por PESSOA (uniao com o cargo)
   Gerente e Administrador enxergam tudo (bypass na aplicacao).
   Idempotente - pode executar mais de uma vez.
   ===================================================================== */

USE BarRestauranteDb;
GO

/* ============================ Tabelas ============================ */

IF OBJECT_ID('dbo.AppFeature', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppFeature
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Code      VARCHAR(50)   NOT NULL,
        Name      NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_AppFeature_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2     NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_AppFeature_IsActive DEFAULT (1),
        CONSTRAINT PK_AppFeature PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_AppFeature_Code ON dbo.AppFeature (Code) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.JobTitleFeature', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobTitleFeature
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        JobTitleId   BIGINT    NOT NULL,
        AppFeatureId BIGINT    NOT NULL,
        CreatedAt    DATETIME2 NOT NULL CONSTRAINT DF_JobTitleFeature_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt    DATETIME2 NULL,
        IsActive     BIT       NOT NULL CONSTRAINT DF_JobTitleFeature_IsActive DEFAULT (1),
        CONSTRAINT PK_JobTitleFeature PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_JobTitleFeature_JobTitle FOREIGN KEY (JobTitleId) REFERENCES dbo.JobTitle (Id),
        CONSTRAINT FK_JobTitleFeature_AppFeature FOREIGN KEY (AppFeatureId) REFERENCES dbo.AppFeature (Id)
    );

    CREATE NONCLUSTERED INDEX IX_JobTitleFeature_JobTitleId ON dbo.JobTitleFeature (JobTitleId);
    CREATE NONCLUSTERED INDEX IX_JobTitleFeature_AppFeatureId ON dbo.JobTitleFeature (AppFeatureId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_JobTitleFeature_JobTitleId_AppFeatureId
        ON dbo.JobTitleFeature (JobTitleId, AppFeatureId) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.AppUserFeature', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUserFeature
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        AppUserId    BIGINT    NOT NULL,
        AppFeatureId BIGINT    NOT NULL,
        CreatedAt    DATETIME2 NOT NULL CONSTRAINT DF_AppUserFeature_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt    DATETIME2 NULL,
        IsActive     BIT       NOT NULL CONSTRAINT DF_AppUserFeature_IsActive DEFAULT (1),
        CONSTRAINT PK_AppUserFeature PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AppUserFeature_AppUser FOREIGN KEY (AppUserId) REFERENCES dbo.AppUser (Id),
        CONSTRAINT FK_AppUserFeature_AppFeature FOREIGN KEY (AppFeatureId) REFERENCES dbo.AppFeature (Id)
    );

    CREATE NONCLUSTERED INDEX IX_AppUserFeature_AppUserId ON dbo.AppUserFeature (AppUserId);
    CREATE NONCLUSTERED INDEX IX_AppUserFeature_AppFeatureId ON dbo.AppUserFeature (AppFeatureId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AppUserFeature_AppUserId_AppFeatureId
        ON dbo.AppUserFeature (AppUserId, AppFeatureId) WHERE IsActive = 1;
END;
GO

/* ============================ Seed: telas ============================ */

IF NOT EXISTS (SELECT 1 FROM dbo.AppFeature)
BEGIN
    DBCC CHECKIDENT ('dbo.AppFeature', RESEED, 0);
    SET IDENTITY_INSERT dbo.AppFeature ON;

    INSERT INTO dbo.AppFeature (Id, Code, Name) VALUES
        (1, 'Salao',    N'Salao (mesas e pedidos)'),
        (2, 'Cardapio', N'Cardapio (produtos)'),
        (3, 'Estoque',  N'Estoque'),
        (4, 'Equipe',   N'Equipe (funcionarios)'),
        (5, 'Usuarios', N'Usuarios e perfis'),
        (6, 'Caixa',    N'Caixa (sessao e pagamentos)');

    BEGIN TRY SET IDENTITY_INSERT dbo.AppFeature OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ Seed: grants padrao por cargo ============================ */

INSERT INTO dbo.JobTitleFeature (JobTitleId, AppFeatureId)
SELECT J.Id, F.Id
FROM (VALUES
    (N'Garçom',            'Salao'),
    (N'Garçom',            'Cardapio'),
    (N'Operador de Caixa',  'Salao'),
    (N'Operador de Caixa',  'Caixa'),
    (N'Cozinheiro',         'Salao'),
    (N'Barman',             'Salao'),
    (N'Barman',             'Cardapio'),
    (N'Estoquista',         'Estoque'),
    (N'Estoquista',         'Cardapio')) AS G (JobTitleName, FeatureCode)
JOIN dbo.JobTitle   AS J ON J.Name = G.JobTitleName AND J.IsActive = 1
JOIN dbo.AppFeature AS F ON F.Code = G.FeatureCode  AND F.IsActive = 1
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.JobTitleFeature X
    WHERE X.JobTitleId = J.Id AND X.AppFeatureId = F.Id AND X.IsActive = 1);
GO

/* ============================ Conferencia ============================ */

SELECT J.Name AS Cargo, F.Name AS Tela
FROM dbo.JobTitleFeature JF
JOIN dbo.JobTitle J ON J.Id = JF.JobTitleId
JOIN dbo.AppFeature F ON F.Id = JF.AppFeatureId
WHERE JF.IsActive = 1
ORDER BY J.Name, F.Id;
GO
