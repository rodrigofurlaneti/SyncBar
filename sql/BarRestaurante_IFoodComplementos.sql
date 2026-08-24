-- Fase 6a ("complementos") — sincronização de complementos com o iFood (módulo Catalog:
-- optionGroups/options). Cria as duas tabelas de mapeamento: qual ComplementGroup/Complement do
-- SyncBar virou qual optionGroup/option no catálogo de cada loja (merchant) no iFood — mesmo
-- padrão de IFoodCategoryMapping/IFoodProductMapping (sql/BarRestaurante_IFoodCardapio.sql).
-- Idempotente — pode rodar de novo sem problema.
--
-- Rodar DEPOIS de sql/BarRestaurante_Complementos.sql (referencia ComplementGroup/Complement).

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS IFoodComplementGroupMapping (
    Id                 BIGINT NOT NULL AUTO_INCREMENT,
    ComplementGroupId  BIGINT NOT NULL,
    BranchId           BIGINT NOT NULL,
    IFoodOptionGroupId CHAR(36) NOT NULL,
    CreatedAt          DATETIME(6) NOT NULL,
    UpdatedAt          DATETIME(6) NULL,
    IsActive           TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodComplementGroupMapping_ComplementGroup FOREIGN KEY (ComplementGroupId) REFERENCES ComplementGroup (Id),
    CONSTRAINT FK_IFoodComplementGroupMapping_Branch FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodComplementGroupMapping_ComplementGroupId_BranchId ON IFoodComplementGroupMapping (ComplementGroupId, BranchId);
CREATE INDEX IX_IFoodComplementGroupMapping_BranchId ON IFoodComplementGroupMapping (BranchId);

CREATE TABLE IF NOT EXISTS IFoodComplementMapping (
    Id             BIGINT NOT NULL AUTO_INCREMENT,
    ComplementId   BIGINT NOT NULL,
    BranchId       BIGINT NOT NULL,
    IFoodOptionId  CHAR(36) NOT NULL,
    IFoodProductId CHAR(36) NOT NULL,
    CreatedAt      DATETIME(6) NOT NULL,
    UpdatedAt      DATETIME(6) NULL,
    IsActive       TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodComplementMapping_Complement FOREIGN KEY (ComplementId) REFERENCES Complement (Id),
    CONSTRAINT FK_IFoodComplementMapping_Branch FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodComplementMapping_ComplementId_BranchId ON IFoodComplementMapping (ComplementId, BranchId);
CREATE INDEX IX_IFoodComplementMapping_BranchId ON IFoodComplementMapping (BranchId);
CREATE INDEX IX_IFoodComplementMapping_IFoodOptionId ON IFoodComplementMapping (IFoodOptionId);

-- Verificação
SELECT Id, ComplementGroupId, BranchId, IFoodOptionGroupId, CreatedAt, UpdatedAt, IsActive FROM IFoodComplementGroupMapping;
SELECT Id, ComplementId, BranchId, IFoodOptionId, IFoodProductId, CreatedAt, UpdatedAt, IsActive FROM IFoodComplementMapping;

-- Nota operacional: mesma regra das fases anteriores — a sincronização de optionGroups/options só
-- roda pra filiais que já têm MerchantId configurado em IFoodMerchantMapping E integração
-- habilitada em IFoodIntegrationSetting.
