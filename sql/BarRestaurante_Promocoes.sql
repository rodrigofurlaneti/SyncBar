/* =====================================================================
   BarRestauranteDb - Modulo Promocoes (em dobro por dia/horario)
   Promotion: produto em promocao num dia da semana e janela de horario.
   DayOfWeek: 0=Domingo ... 6=Sabado (padrao .NET).
   Minutos do dia: 960 = 16:00 | 1080 = 18:00 | 1200 = 20:00.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.Promotion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Promotion
    (
        Id               BIGINT IDENTITY(1,1) NOT NULL,
        BranchId         BIGINT        NOT NULL,
        ProductId        BIGINT        NOT NULL,
        Name             NVARCHAR(150) NOT NULL,
        DayOfWeek        INT           NOT NULL,
        StartMinuteOfDay INT           NOT NULL,
        EndMinuteOfDay   INT           NOT NULL,
        CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_Promotion_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt        DATETIME2     NULL,
        IsActive         BIT           NOT NULL CONSTRAINT DF_Promotion_IsActive DEFAULT (1),
        CONSTRAINT PK_Promotion PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Promotion_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_Promotion_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product (Id),
        CONSTRAINT CK_Promotion_DayOfWeek CHECK (DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT CK_Promotion_StartMinute CHECK (StartMinuteOfDay BETWEEN 0 AND 1439),
        CONSTRAINT CK_Promotion_EndMinute CHECK (EndMinuteOfDay BETWEEN 1 AND 1440),
        CONSTRAINT CK_Promotion_Window CHECK (StartMinuteOfDay < EndMinuteOfDay)
    );

    CREATE NONCLUSTERED INDEX IX_Promotion_BranchId ON dbo.Promotion (BranchId);
    CREATE NONCLUSTERED INDEX IX_Promotion_ProductId ON dbo.Promotion (ProductId);
END;
GO

/* ============================ Tela Promocoes ============================ */

INSERT INTO dbo.AppFeature (Code, Name)
SELECT 'Promocoes', N'Promocoes (em dobro)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppFeature WHERE Code = 'Promocoes' AND IsActive = 1);
GO

/* ============================ Seed: semana exemplo ============================ */

DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);

INSERT INTO dbo.Promotion (BranchId, ProductId, Name, DayOfWeek, StartMinuteOfDay, EndMinuteOfDay)
SELECT @BranchId, P.Id, S.PromoName, S.Day, S.StartMin, S.EndMin
FROM (VALUES
    (N'Caipirinha de Limão',       N'Quarta da caipirinha em dobro', 3,  960, 1200),
    (N'Cerveja Pilsen 600ml',      N'Quinta da cerveja em dobro',    4,  960, 1200),
    (N'Cerveja Lata 350ml',        N'Quinta da cerveja em dobro',    4,  960, 1200),
    (N'Porção de Batata Frita',    N'Sexta da batata em dobro',      5, 1080, 1200),
    (N'Caipirinha de Limão',       N'Sábado da caipirinha em dobro', 6,  960, 1200)
) AS S (ProductName, PromoName, Day, StartMin, EndMin)
JOIN dbo.Product AS P ON P.Name = S.ProductName AND P.IsActive = 1
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Promotion X
    WHERE X.BranchId = @BranchId AND X.ProductId = P.Id
      AND X.DayOfWeek = S.Day AND X.IsActive = 1);

SELECT P.Name AS Produto, PR.Name AS Promocao, PR.DayOfWeek,
       PR.StartMinuteOfDay / 60 AS HoraInicio, PR.EndMinuteOfDay / 60 AS HoraFim
FROM dbo.Promotion PR JOIN dbo.Product P ON P.Id = PR.ProductId
WHERE PR.IsActive = 1 ORDER BY PR.DayOfWeek, P.Name;
GO
