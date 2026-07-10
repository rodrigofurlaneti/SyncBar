/* =====================================================================
   BarRestauranteDb - Comandas individuais 00 a 99
   Desativa cartoes antigos que estejam DISPONIVEIS (comandas em uso ou
   com historico permanecem) e cria os codigos '00' a '99'.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);

/* Desativa cartoes fora do padrao 00-99 que nao estejam em uso */
UPDATE dbo.Comanda
SET IsActive = 0, UpdatedAt = SYSDATETIME()
WHERE BranchId = @BranchId
  AND IsActive = 1
  AND ComandaStatusId = 1 -- Disponivel
  AND (LEN(Code) <> 2 OR Code NOT LIKE '[0-9][0-9]');

/* Cria 00..99 onde nao existir */
;WITH Numbers AS (
    SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS N
    FROM sys.objects a CROSS JOIN sys.objects b
)
INSERT INTO dbo.Comanda (BranchId, ComandaStatusId, Code)
SELECT @BranchId, 1, RIGHT('0' + CONVERT(VARCHAR(2), N), 2)
FROM Numbers
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Comanda X
    WHERE X.BranchId = @BranchId
      AND X.Code = RIGHT('0' + CONVERT(VARCHAR(2), Numbers.N), 2)
      AND X.IsActive = 1);

SELECT COUNT(*) AS ComandasAtivas FROM dbo.Comanda WHERE BranchId = @BranchId AND IsActive = 1;
GO
