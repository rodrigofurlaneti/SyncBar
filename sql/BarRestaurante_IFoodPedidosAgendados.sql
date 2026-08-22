/* =====================================================================
   SyncBar — Integração iFood: pedidos agendados (Fase 14)

   Até esta fase, o campo orderTiming/preparationStartDateTime já vinha na resposta de
   GET orders/{id} (IFoodOrderDetailsDto), mas era descartado sem nunca ser persistido em
   IFoodOrder — a tela de Pedidos não tinha como saber que um pedido era agendado. Esta migração
   adiciona as duas colunas em IFoodOrder pra guardar esse dado (ver comentário em IFoodOrder.cs).

   Pedidos sincronizados ANTES desta migração ficam com OrderTiming = 'IMMEDIATE' (default) e
   PreparationStartDateTime = NULL — mesma limitação já aceita pra DeliveredBy na Fase 7 (não há
   como retroagir sem reconsultar o pedido no iFood, e pedidos antigos já foram concluídos).

   COMO RODAR: idempotente (checa a coluna antes de alterar, mesmo padrão do script da Fase 7),
   pode ser executado quantas vezes for preciso contra o banco BarRestauranteDb (MySQL).
   ===================================================================== */

USE BarRestauranteDb;

SET @col_exists = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = 'BarRestauranteDb' AND TABLE_NAME = 'IFoodOrder' AND COLUMN_NAME = 'OrderTiming'
);
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE IFoodOrder ADD COLUMN OrderTiming VARCHAR(20) NOT NULL DEFAULT ''IMMEDIATE'' AFTER DeliveredBy',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = 'BarRestauranteDb' AND TABLE_NAME = 'IFoodOrder' AND COLUMN_NAME = 'PreparationStartDateTime'
);
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE IFoodOrder ADD COLUMN PreparationStartDateTime DATETIME(6) NULL AFTER OrderTiming',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificação:
SELECT Id, IFoodOrderId, OrderTiming, PreparationStartDateTime, CreatedAt
FROM IFoodOrder ORDER BY Id DESC LIMIT 20;
