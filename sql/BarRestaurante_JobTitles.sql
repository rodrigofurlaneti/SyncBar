/* =====================================================================
   BarRestauranteDb - Cargos padrao (idempotente, insere por nome)
   ===================================================================== */

USE BarRestauranteDb;
GO

DECLARE @CompanyId BIGINT = (SELECT TOP 1 Id FROM dbo.Company WHERE IsActive = 1);

INSERT INTO dbo.JobTitle (CompanyId, Name)
SELECT @CompanyId, J.Name
FROM (VALUES
    (N'Gerente'),
    (N'Garçom'),
    (N'Operador de Caixa'),
    (N'Cozinheiro'),
    (N'Barman'),
    (N'Estoquista')) AS J (Name)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.JobTitle X
    WHERE X.CompanyId = @CompanyId AND X.Name = J.Name AND X.IsActive = 1);

SELECT Id, Name FROM dbo.JobTitle WHERE CompanyId = @CompanyId AND IsActive = 1 ORDER BY Name;
GO
