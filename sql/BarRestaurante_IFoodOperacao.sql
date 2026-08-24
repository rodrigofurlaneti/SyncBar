/* =====================================================================
   SyncBar — Integração iFood: operação da loja / Merchant (fase 5)

   Cria a tabela IFoodOpeningHours (cópia local editável dos turnos de funcionamento, sincronizada
   com o iFood via PUT /opening-hours) e adiciona duas colunas: PreparationTimeMinutes em
   IFoodMerchantMapping (tempo de preparo customizado por filial) e IFoodCustomerId em
   IFoodIntegrationSetting (header X-iFood-Customer-ID exigido só pelos endpoints de tempo de
   preparo — texto puro, não é segredo). Não há tabela nova para status/interrupções — ambos são
   consultados/criados ao vivo direto na API do iFood (ver ifood-integration-status no projeto
   claude.ai).

   Idempotente — pode rodar de novo sem problema (checa coluna existente antes de adicionar).

   COMO RODAR: execute o arquivo inteiro contra o banco BarRestauranteDb (MySQL), depois das
   fases 1-4 (IFoodIntegrationSetting/IFoodMerchantMapping/IFoodOrder/IFoodFinancialEvent/
   IFoodSettlement) já terem rodado.
   ===================================================================== */

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS IFoodOpeningHours (
    Id              BIGINT NOT NULL AUTO_INCREMENT,
    BranchId        BIGINT NOT NULL,
    DayOfWeek       INT NOT NULL,          -- 0 = domingo .. 6 = sábado (convenção .NET DayOfWeek)
    Start           TIME NOT NULL,
    DurationMinutes INT NOT NULL,
    CreatedAt       DATETIME(6) NOT NULL,
    UpdatedAt       DATETIME(6) NULL,
    IsActive        TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodOpeningHours_Branch
        FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodOpeningHours_BranchId_DayOfWeek ON IFoodOpeningHours (BranchId, DayOfWeek);

-- Adiciona as colunas novas só se ainda não existirem (idempotente sem depender de sintaxe
-- "ADD COLUMN IF NOT EXISTS", que só o MySQL 8.0.29+ suporta de forma confiável).
SET @col_exists = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = 'BarRestauranteDb' AND TABLE_NAME = 'IFoodMerchantMapping' AND COLUMN_NAME = 'PreparationTimeMinutes'
);
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE IFoodMerchantMapping ADD COLUMN PreparationTimeMinutes INT NULL',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists = (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = 'BarRestauranteDb' AND TABLE_NAME = 'IFoodIntegrationSetting' AND COLUMN_NAME = 'IFoodCustomerId'
);
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE IFoodIntegrationSetting ADD COLUMN IFoodCustomerId VARCHAR(100) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verificação:
SELECT Id, BranchId, DayOfWeek, Start, DurationMinutes, CreatedAt, UpdatedAt, IsActive FROM IFoodOpeningHours;
SELECT Id, BranchId, MerchantId, PreparationTimeMinutes FROM IFoodMerchantMapping;
SELECT Id, CompanyId, ClientId, IFoodCustomerId FROM IFoodIntegrationSetting;

/* Nota operacional: status, interrupções e horários só funcionam pra filiais com MerchantId
   configurado em IFoodMerchantMapping E integração habilitada em IFoodIntegrationSetting — mesma
   regra das fases anteriores. Tempo de preparo tem um pré-requisito a mais: IFoodCustomerId
   precisa estar preenchido em IFoodIntegrationSetting (configurável na tela de credenciais);
   sem isso, os botões de tempo de preparo ficam desabilitados na tela, mas o resto do módulo
   Merchant (status/interrupções/horários) funciona normalmente.

   Nota de fonte do IFoodCustomerId: a doc do iFood não deixa claro onde exatamente esse UUID
   aparece no portal do desenvolvedor — vale confirmar isso na prática ao testar (ver Pendente
   no doc de status do projeto). */
