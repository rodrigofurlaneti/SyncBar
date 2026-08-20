/* =====================================================================
   SyncBar — Integração iFood: logística por frota própria (fase 7)

   Cria a tabela IFoodLogisticsDelivery, que rastreia a entrega feita pela FROTA PRÓPRIA de um
   pedido iFood (módulo Logistics: assignDriver, goingToOrigin, arrivedAtOrigin, dispatch,
   arrivedAtDestination, verifyDeliveryCode). 1:1 com IFoodOrder, referenciado pelo Id LOCAL
   (BIGINT) do SyncBar — não pela string IFoodOrderId do iFood.

   Também adiciona a coluna DeliveredBy em IFoodOrder — bruto do iFood (delivery.deliveredBy),
   usado pra decidir se um pedido é elegível pro fluxo de frota própria ("IFOOD" = logística do
   próprio iFood; qualquer outro valor = self-delivery). Pedidos sincronizados ANTES desta
   migração ficam com DeliveredBy NULL (a tela simplesmente não oferece "Atribuir entregador"
   pra eles — não há como retroagir esse dado sem reconsultar o pedido no iFood).

   Tabela nova, sem dados prévios — CREATE TABLE IF NOT EXISTS é suficiente. Coluna nova em
   IFoodOrder é adicionada de forma idempotente (checa antes, mesmo padrão do script da fase 5).

   COMO RODAR: execute o arquivo inteiro contra o banco BarRestauranteDb (MySQL), depois das
   fases 1-6 (IFoodIntegrationSetting/IFoodMerchantMapping/IFoodOrder/IFoodFinancialEvent/
   IFoodSettlement/IFoodOpeningHours/Complementos) já terem rodado.
   ===================================================================== */

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS IFoodLogisticsDelivery (
    Id                     BIGINT NOT NULL AUTO_INCREMENT,
    IFoodOrderId           BIGINT NOT NULL,
    BranchId               BIGINT NOT NULL,
    DriverName             VARCHAR(150) NOT NULL,
    DriverPhone            VARCHAR(30) NOT NULL,
    DriverVehicleType      VARCHAR(30) NOT NULL,
    Status                 VARCHAR(30) NOT NULL,
    AssignedAt             DATETIME(6) NOT NULL,
    GoingToOriginAt        DATETIME(6) NULL,
    ArrivedAtOriginAt      DATETIME(6) NULL,
    DispatchedAt           DATETIME(6) NULL,
    ArrivedAtDestinationAt DATETIME(6) NULL,
    DeliveryCodeVerifiedAt DATETIME(6) NULL,
    CreatedAt              DATETIME(6) NOT NULL,
    UpdatedAt              DATETIME(6) NULL,
    IsActive               TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodLogisticsDelivery_IFoodOrder
        FOREIGN KEY (IFoodOrderId) REFERENCES IFoodOrder (Id),
    CONSTRAINT FK_IFoodLogisticsDelivery_Branch
        FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE UNIQUE INDEX UQ_IFoodLogisticsDelivery_IFoodOrderId ON IFoodLogisticsDelivery (IFoodOrderId);
CREATE INDEX IX_IFoodLogisticsDelivery_BranchId ON IFoodLogisticsDelivery (BranchId);

-- Coluna nova em IFoodOrder, idempotente (sem depender de "ADD COLUMN IF NOT EXISTS", que só o
-- MySQL 8.0.29+ suporta de forma confiável — mesmo padrão do script da fase 5).
SET @col_exists = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = 'BarRestauranteDb' AND TABLE_NAME = 'IFoodOrder' AND COLUMN_NAME = 'DeliveredBy'
);
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE IFoodOrder ADD COLUMN DeliveredBy VARCHAR(30) NULL AFTER IFoodOrderType',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificação:
SELECT * FROM IFoodLogisticsDelivery;
SELECT Id, IFoodOrderId, IFoodOrderType, DeliveredBy, Status FROM IFoodOrder ORDER BY Id DESC LIMIT 20;

/* Nota operacional: assim como as demais ações do módulo Order/Logistics, cada passo desta tela
   é acionado manualmente pela equipe (sem sincronização automática de fundo) — o iFood não
   empurra eventos de progresso da entrega própria de volta pro polling (ASSIGN_DRIVER e afins
   são reconhecidos mas não processados, ver comentário em SyncIFoodOrdersCommandHandler).

   Nota de fonte do valor "IFOOD" em deliveredBy: a doc não documenta uma lista fechada de
   valores possíveis para esse campo — "IFOOD" foi inferido do contexto (logística do próprio
   iFood) cruzando as docs de Order e Logistics; vale reconfirmar na prática assim que houver um
   pedido real de self-delivery pra observar o valor exato que chega. */
