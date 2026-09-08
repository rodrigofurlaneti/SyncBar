-- Selecione o schema correto no cliente MySQL antes de executar.
-- Códigos definidos em SyncBar.Domain/Constants/FeatureCodes.cs.
-- Insere apenas códigos ausentes; preserva IDs, nomes e status existentes.
-- Não concede permissões a cargos ou usuários.
SET NAMES utf8mb4;

INSERT INTO `appfeature` (`Code`, `Name`, `CreatedAt`, `IsActive`)
SELECT seed.Code, seed.Name, CURRENT_TIMESTAMP(6), 1
FROM (
    SELECT 'Salao' AS Code, 'Salão' AS Name
    UNION ALL SELECT 'Cardapio', 'Cardápio'
    UNION ALL SELECT 'Estoque', 'Estoque'
    UNION ALL SELECT 'Equipe', 'Equipe'
    UNION ALL SELECT 'Usuarios', 'Usuários'
    UNION ALL SELECT 'Caixa', 'Caixa'
    UNION ALL SELECT 'Faturamento', 'Faturamento'
    UNION ALL SELECT 'Preparo', 'Preparo'
    UNION ALL SELECT 'Promocoes', 'Promoções'
    UNION ALL SELECT 'Impressao', 'Impressão'
) AS seed
WHERE NOT EXISTS (
    SELECT 1 FROM `appfeature` AS existing WHERE existing.Code = seed.Code
);

SELECT `Id`, `Code`, `Name`, `IsActive`
FROM `appfeature`
ORDER BY `Id`;
