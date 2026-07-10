/* =====================================================================
   BarRestauranteDb - Tela 'Preparo' (painel de cozinha/bar - KDS)
   Cria a feature e concede por padrao a Cozinheiro e Barman.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

INSERT INTO dbo.AppFeature (Code, Name)
SELECT 'Preparo', N'Preparo (painel cozinha/bar)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppFeature WHERE Code = 'Preparo' AND IsActive = 1);
GO

INSERT INTO dbo.JobTitleFeature (JobTitleId, AppFeatureId)
SELECT J.Id, F.Id
FROM (VALUES (N'Cozinheiro'), (N'Barman')) AS G (JobTitleName)
JOIN dbo.JobTitle   AS J ON J.Name = G.JobTitleName AND J.IsActive = 1
JOIN dbo.AppFeature AS F ON F.Code = 'Preparo' AND F.IsActive = 1
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.JobTitleFeature X
    WHERE X.JobTitleId = J.Id AND X.AppFeatureId = F.Id AND X.IsActive = 1);
GO

SELECT J.Name AS Cargo, F.Name AS Tela
FROM dbo.JobTitleFeature JF
JOIN dbo.JobTitle J ON J.Id = JF.JobTitleId
JOIN dbo.AppFeature F ON F.Id = JF.AppFeatureId
WHERE JF.IsActive = 1
ORDER BY J.Name, F.Id;
GO
