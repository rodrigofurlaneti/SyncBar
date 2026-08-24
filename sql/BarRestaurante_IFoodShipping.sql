/* =====================================================================
   SyncBar — Integração iFood: shipping (fase 8)

   Cria a tabela IFoodShippingDelivery, que rastreia um pedido de OUTRO canal (telefone, WhatsApp,
   site próprio — NÃO um IFoodOrder) entregue usando a malha de entregadores do iFood (módulo
   Shipping: deliveryAvailabilities, "Request a driver for an external order", tracking, cancel,
   safeDelivery). Ao contrário de IFoodLogisticsDelivery (fase 7 — frota PRÓPRIA entregando pedido
   QUE VEIO do iFood), aqui é o inverso: pedido que NÃO veio do iFood, entregue POR entregadores
   do iFood. Por isso NÃO há FK pra IFoodOrder nem pra CustomerOrder — OrderReference é texto
   livre digitado pela equipe (ver comentário na entidade).

   Tabela nova, sem dados prévios — CREATE TABLE IF NOT EXISTS é suficiente.

   COMO RODAR: execute o arquivo inteiro contra o banco BarRestauranteDb (MySQL), depois das
   fases 1-7 já terem rodado (não depende de nenhuma tabela nova das fases anteriores, só de
   Branch e Company que já existem desde o início).
   ===================================================================== */

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS IFoodShippingDelivery (
    Id                      BIGINT NOT NULL AUTO_INCREMENT,
    BranchId                BIGINT NOT NULL,
    OrderReference          VARCHAR(150) NULL,
    CustomerName            VARCHAR(150) NOT NULL,
    CustomerPhoneAreaCode   VARCHAR(5) NOT NULL,
    CustomerPhoneNumber     VARCHAR(20) NOT NULL,
    PostalCode              VARCHAR(15) NOT NULL,
    StreetName              VARCHAR(200) NOT NULL,
    StreetNumber            VARCHAR(20) NOT NULL,
    Complement              VARCHAR(100) NULL,
    Neighborhood            VARCHAR(100) NOT NULL,
    City                    VARCHAR(100) NOT NULL,
    State                   VARCHAR(2) NOT NULL,
    Country                 VARCHAR(60) NOT NULL,
    Reference               VARCHAR(200) NULL,
    Latitude                DOUBLE NULL,
    Longitude               DOUBLE NULL,
    MerchantFee             DECIMAL(18,2) NOT NULL,
    QuoteId                 VARCHAR(100) NOT NULL,
    IFoodDeliveryId         VARCHAR(100) NOT NULL,
    TrackingUrl             VARCHAR(500) NULL,
    Status                  VARCHAR(30) NOT NULL,
    RequestedAt             DATETIME(6) NOT NULL,
    CancelledAt             DATETIME(6) NULL,
    CancellationReason      VARCHAR(300) NULL,
    CreatedAt               DATETIME(6) NOT NULL,
    UpdatedAt               DATETIME(6) NULL,
    IsActive                TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodShippingDelivery_Branch
        FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodShippingDelivery_BranchId ON IFoodShippingDelivery (BranchId);
CREATE UNIQUE INDEX UQ_IFoodShippingDelivery_IFoodDeliveryId ON IFoodShippingDelivery (IFoodDeliveryId);

-- Verificação:
SELECT Id, BranchId, OrderReference, CustomerName, CustomerPhoneAreaCode, CustomerPhoneNumber, PostalCode, StreetName, StreetNumber, Complement, Neighborhood, City, State, Country, Reference, Latitude, Longitude, MerchantFee, QuoteId, IFoodDeliveryId, TrackingUrl, Status, RequestedAt, CancelledAt, CancellationReason, CreatedAt, UpdatedAt, IsActive FROM IFoodShippingDelivery;

/* Nota operacional: assim como o módulo Logistics (fase 7), cada passo é acionado manualmente
   pela equipe (cotar → pedir motorista → acompanhar/cancelar) — sem sincronização automática de
   fundo. O iFood não devolve um "status" de entrega neste módulo (só id + trackingUrl na criação
   e lat/long em /tracking); por isso Status aqui só reflete AÇÕES QUE O SYNCBAR TOMOU
   (DRIVER_REQUESTED/CANCELLED), não um enum espelhado do iFood.

   Escopo desta fase: cobre cotação, pedido de motorista, tracking, motivos de cancelamento,
   cancelamento e "safe delivery score" — tanto pra pedido externo (telefone/WhatsApp/site) quanto
   pra um IFoodOrder já existente que o lojista decide entregar via Shipping em vez da logística
   padrão ou da frota própria. NÃO implementado: o fluxo de negociação de troca de endereço em
   andamento (accept/deny/request/userConfirm — 4 endpoints), por não ter gatilho claro do lado do
   lojista na doc consultada (ver ressalva em IIFoodShippingClient). */
