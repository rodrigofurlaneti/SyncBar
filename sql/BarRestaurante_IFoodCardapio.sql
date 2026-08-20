-- Fase 3 ("fluxo essencial") — sincronização de cardápio com o iFood (módulo Catalog).
-- Cria as duas tabelas de mapeamento: qual categoria/produto do SyncBar virou qual
-- categoria/item no catálogo de cada loja (merchant) no iFood. Idempotente — pode rodar de novo
-- sem problema.

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS IFoodCategoryMapping (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    CategoryId BIGINT NOT NULL,
    BranchId BIGINT NOT NULL,
    IFoodCategoryId VARCHAR(100) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodCategoryMapping_Category FOREIGN KEY (CategoryId) REFERENCES Category (Id),
    CONSTRAINT FK_IFoodCategoryMapping_Branch FOREIGN KEY (BranchId) REFERENCES Branch (Id)
);

CREATE INDEX IX_IFoodCategoryMapping_CategoryId_BranchId ON IFoodCategoryMapping (CategoryId, BranchId);

CREATE TABLE IF NOT EXISTS IFoodProductMapping (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    ProductId BIGINT NOT NULL,
    BranchId BIGINT NOT NULL,
    IFoodItemId CHAR(36) NOT NULL,
    IFoodProductId CHAR(36) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodProductMapping_Product FOREIGN KEY (ProductId) REFERENCES Product (Id),
    CONSTRAINT FK_IFoodProductMapping_Branch FOREIGN KEY (BranchId) REFERENCES Branch (Id)
);

CREATE INDEX IX_IFoodProductMapping_ProductId_BranchId ON IFoodProductMapping (ProductId, BranchId);
CREATE INDEX IX_IFoodProductMapping_BranchId ON IFoodProductMapping (BranchId);

-- Verificação
SELECT * FROM IFoodCategoryMapping;
SELECT * FROM IFoodProductMapping;

-- Nota operacional: a sincronização (criação de categorias/itens no iFood) só roda pra filiais
-- que já têm MerchantId configurado em IFoodMerchantMapping E integração habilitada em
-- IFoodIntegrationSetting — sem isso o SyncIFoodCatalogCommand retorna "Skipped" sem tentar nada.
