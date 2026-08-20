/* =====================================================================
   SyncBar — Integração iFood: financeiro (fase 4)

   Cria as duas tabelas de auditoria/reconciliação financeira: IFoodFinancialEvent (um registro
   por lançamento retornado pela API Financial Events — o que rendeu, taxas, comissão, subsídio,
   HasTransferImpact etc.) e IFoodSettlement (um registro por título retornado pela API
   Settlement — repasse consolidado semanal, boleto, registro de recebíveis).

   Este módulo é só de auditoria — não mexe no fluxo operacional existente (Sale, CashSession,
   CashMovement continuam sendo a fonte de verdade do caixa físico da loja). Idempotente —
   CREATE TABLE IF NOT EXISTS, pode rodar de novo sem problema.

   COMO RODAR: execute o arquivo inteiro contra o banco BarRestauranteDb (MySQL), depois das
   fases 1-3 (IFoodIntegrationSetting/IFoodMerchantMapping/IFoodOrder) já terem rodado.
   ===================================================================== */

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS IFoodFinancialEvent (
    Id                     BIGINT NOT NULL AUTO_INCREMENT,
    BranchId               BIGINT NOT NULL,
    IFoodEventId           VARCHAR(100) NOT NULL,
    Name                   VARCHAR(150) NOT NULL,
    Description            VARCHAR(500) NULL,
    Trigger                VARCHAR(100) NULL,
    Amount                 DECIMAL(18,2) NOT NULL,
    HasTransferImpact      TINYINT(1) NOT NULL DEFAULT 0,
    CompetenceDate         DATETIME(6) NOT NULL,
    PeriodStart            DATETIME(6) NOT NULL,
    PeriodEnd              DATETIME(6) NOT NULL,
    SettlementExpectedDate DATETIME(6) NULL,
    ReferenceType          VARCHAR(30) NULL,
    ReferenceId            VARCHAR(100) NULL,
    RawPayload             TEXT NOT NULL,
    CreatedAt              DATETIME(6) NOT NULL,
    UpdatedAt              DATETIME(6) NULL,
    IsActive               TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodFinancialEvent_Branch
        FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodFinancialEvent_BranchId_IFoodEventId ON IFoodFinancialEvent (BranchId, IFoodEventId);
CREATE INDEX IX_IFoodFinancialEvent_BranchId_CompetenceDate ON IFoodFinancialEvent (BranchId, CompetenceDate);
CREATE INDEX IX_IFoodFinancialEvent_ReferenceType_ReferenceId ON IFoodFinancialEvent (ReferenceType, ReferenceId);

CREATE TABLE IF NOT EXISTS IFoodSettlement (
    Id                BIGINT NOT NULL AUTO_INCREMENT,
    BranchId          BIGINT NOT NULL,
    IFoodSettlementId VARCHAR(100) NOT NULL,
    Type              VARCHAR(30) NOT NULL,
    Product           VARCHAR(50) NULL,
    Amount            DECIMAL(18,2) NOT NULL,
    Status            VARCHAR(30) NOT NULL,
    PaymentDate       DATETIME(6) NULL,
    BankCode          VARCHAR(20) NULL,
    BankAgency        VARCHAR(20) NULL,
    BankAccount       VARCHAR(30) NULL,
    RawPayload        TEXT NOT NULL,
    CreatedAt         DATETIME(6) NOT NULL,
    UpdatedAt         DATETIME(6) NULL,
    IsActive          TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodSettlement_Branch
        FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodSettlement_BranchId_IFoodSettlementId ON IFoodSettlement (BranchId, IFoodSettlementId);
CREATE INDEX IX_IFoodSettlement_BranchId_PaymentDate ON IFoodSettlement (BranchId, PaymentDate);

-- Verificação:
SELECT * FROM IFoodFinancialEvent;
SELECT * FROM IFoodSettlement;

/* Nota operacional: assim como cardápio e pedidos, a sincronização financeira só roda pra
   filiais que já têm MerchantId configurado em IFoodMerchantMapping E integração habilitada em
   IFoodIntegrationSetting — o ciclo (1x/dia, IFoodFinancialSyncBackgroundService) simplesmente
   não encontra filiais elegíveis e não faz nada até isso ser configurado.

   Nota de homologação: acesso de PRODUÇÃO ao módulo Financial exige conta profissional (CNPJ) —
   contas Pessoal/Estudante (como a usada nas fases 1-3) não são aceitas. Isso não bloqueia
   implementação nem testes em sandbox, mas a sincronização real só vai trazer dados depois que
   a loja de teste tiver pedidos com repasse processado. */
