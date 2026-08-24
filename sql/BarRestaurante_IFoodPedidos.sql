/* =====================================================================
   SyncBar — Integração iFood: sincronização de pedidos (fase 2, "fluxo essencial")

   Cria a tabela IFoodOrder, que liga um CustomerOrder (o pedido "de verdade" no SyncBar —
   cozinha, faturamento) ao pedido correspondente no iFood. O CustomerOrder em si não precisa de
   nenhuma coluna nova: já suporta pedido sem mesa/comanda via OrderTypeId (Retirada/Delivery,
   ver BarRestaurante_DeliveryRetirada.sql) — reaproveitado tal como está.

   Tabela nova, sem dados prévios — CREATE TABLE IF NOT EXISTS é suficiente (idempotente, não
   precisa de DROP como o script anterior do iFood).

   COMO RODAR: execute o arquivo inteiro contra o banco BarRestauranteDb (MySQL), depois da API
   já ter rodado ao menos uma vez com o schema anterior (IFoodIntegrationSetting/
   IFoodMerchantMapping) presente.
   ===================================================================== */

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS IFoodOrder (
    Id                BIGINT NOT NULL AUTO_INCREMENT,
    CustomerOrderId   BIGINT NOT NULL,
    BranchId          BIGINT NOT NULL,
    IFoodOrderId      VARCHAR(100) NOT NULL,
    DisplayId         VARCHAR(50) NULL,
    MerchantId        VARCHAR(100) NOT NULL,
    IFoodOrderType    VARCHAR(30) NOT NULL,
    Status            VARCHAR(30) NOT NULL,
    ConfirmDeadlineAt DATETIME(6) NOT NULL,
    ConfirmedAt       DATETIME(6) NULL,
    HasUnmappedItems  TINYINT(1) NOT NULL DEFAULT 0,
    CreatedAt         DATETIME(6) NOT NULL,
    UpdatedAt         DATETIME(6) NULL,
    IsActive          TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodOrder_Branch
        FOREIGN KEY (BranchId) REFERENCES Branch (Id),
    CONSTRAINT FK_IFoodOrder_CustomerOrder
        FOREIGN KEY (CustomerOrderId) REFERENCES CustomerOrder (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE UNIQUE INDEX UQ_IFoodOrder_IFoodOrderId ON IFoodOrder (IFoodOrderId);
CREATE INDEX IX_IFoodOrder_BranchId ON IFoodOrder (BranchId);
CREATE INDEX IX_IFoodOrder_CustomerOrderId ON IFoodOrder (CustomerOrderId);

-- Verificação:
SELECT Id, CustomerOrderId, BranchId, IFoodOrderId, DisplayId, MerchantId, IFoodOrderType, Status, ConfirmDeadlineAt, ConfirmedAt, HasUnmappedItems, CreatedAt, UpdatedAt, IsActive FROM IFoodOrder;

/* IMPORTANTE — pré-requisito operacional (não é SQL, é configuração):
   pedidos do iFood são criados no SyncBar em nome do "funcionário de autoatendimento" da
   filial (o mesmo campo Branch.SelfServiceEmployeeId usado no autoatendimento via QR Code —
   ver BarRestaurante_DDL.sql). Se uma filial não tiver esse campo configurado, os pedidos iFood
   dela ficam parados no polling (não são criados) até alguém configurar em Config > Filiais. */
