/* =====================================================================
   SyncBar — Config de taxa de serviço (10%) por filial (liga/desliga)
   Espelha o padrão de ComandaSetting. Idempotente.
   Enabled = 0 durante eventos em que a taxa não pode ser cobrada.

   COMO RODAR: execute o arquivo INTEIRO (F5). Não insira SELECTs antes
   do CREATE — a verificação já está no final, em lote separado.
   ===================================================================== */

USE [BarRestauranteDb];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ServiceFeeSetting')
BEGIN
    CREATE TABLE dbo.ServiceFeeSetting
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        BranchId  BIGINT    NOT NULL,
        Enabled   BIT       NOT NULL CONSTRAINT DF_ServiceFeeSetting_Enabled DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ServiceFeeSetting_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2 NULL,
        IsActive  BIT       NOT NULL CONSTRAINT DF_ServiceFeeSetting_IsActive DEFAULT (1),
        CONSTRAINT PK_ServiceFeeSetting PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ServiceFeeSetting_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_ServiceFeeSetting_BranchId
        ON dbo.ServiceFeeSetting (BranchId) WHERE IsActive = 1;

    PRINT 'Tabela ServiceFeeSetting criada.';
END
ELSE
    PRINT 'Tabela ServiceFeeSetting ja existia.';
GO

-- Semente: taxa LIGADA por padrão na filial ativa.
DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);
IF @BranchId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.ServiceFeeSetting WHERE BranchId = @BranchId AND IsActive = 1)
    INSERT INTO dbo.ServiceFeeSetting (BranchId, Enabled) VALUES (@BranchId, 1);
GO

-- Verificação (roda em lote separado, depois da criação):
SELECT * FROM dbo.ServiceFeeSetting;
GO
