-- Fase 17 ("pizza") — sincronização de pizza com o iFood (Catalog v1, legado — a API v2 atual não
-- tem endpoint de criação/atualização de pizza, só leitura embutida em GET .../categories; ver
-- comentário em IFoodCatalogClient). Cria as duas tabelas de mapeamento: qual PizzaConfiguration
-- do SyncBar virou qual pizza no catálogo de cada loja (merchant) no iFood, e o id de cada
-- elemento (size/crust/edge/topping) devolvido pelo iFood na criação — necessário porque, ao
-- contrário dos outros mapeamentos, a API de pizza do v1 NÃO aceita um id proposto no create, só
-- devolve um (ver comentário em IFoodPizzaMapping). Idempotente — pode rodar de novo sem problema.
--
-- Rodar DEPOIS de sql/BarRestaurante_Pizza.sql (referencia PizzaConfiguration).

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS IFoodPizzaMapping (
    Id                   BIGINT NOT NULL AUTO_INCREMENT,
    PizzaConfigurationId BIGINT NOT NULL,
    BranchId             BIGINT NOT NULL,
    IFoodPizzaId         VARCHAR(100) NOT NULL,
    CreatedAt            DATETIME(6) NOT NULL,
    UpdatedAt            DATETIME(6) NULL,
    IsActive             TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodPizzaMapping_PizzaConfiguration FOREIGN KEY (PizzaConfigurationId) REFERENCES PizzaConfiguration (Id),
    CONSTRAINT FK_IFoodPizzaMapping_Branch FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodPizzaMapping_PizzaConfigurationId_BranchId ON IFoodPizzaMapping (PizzaConfigurationId, BranchId);
CREATE INDEX IX_IFoodPizzaMapping_BranchId ON IFoodPizzaMapping (BranchId);

CREATE TABLE IF NOT EXISTS IFoodPizzaElementMapping (
    Id                  BIGINT NOT NULL AUTO_INCREMENT,
    IFoodPizzaMappingId BIGINT NOT NULL,
    -- Kind: 1=Size, 2=Crust, 3=Edge, 4=Topping — constante de código, ver IFoodPizzaElementKind.
    Kind                TINYINT NOT NULL,
    LocalId             BIGINT NOT NULL,
    IFoodElementId      VARCHAR(100) NOT NULL,
    CreatedAt           DATETIME(6) NOT NULL,
    UpdatedAt           DATETIME(6) NULL,
    IsActive            TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodPizzaElementMapping_IFoodPizzaMapping FOREIGN KEY (IFoodPizzaMappingId) REFERENCES IFoodPizzaMapping (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodPizzaElementMapping_MappingId_Kind_LocalId ON IFoodPizzaElementMapping (IFoodPizzaMappingId, Kind, LocalId);

-- Verificação
SELECT * FROM IFoodPizzaMapping;
SELECT * FROM IFoodPizzaElementMapping;

-- Nota operacional: mesma regra das fases anteriores — a sincronização de pizza só roda pra
-- filiais que já têm MerchantId configurado em IFoodMerchantMapping E integração habilitada em
-- IFoodIntegrationSetting. Diferente de produto/complemento (idempotentes por id local gerado
-- pelo SyncBar), o fluxo de pizza é: 1) POST cria no iFood, 2) grava IFoodPizzaId/elementos
-- retornados aqui, 3) daí em diante todo PUT/PATCH usa esses ids capturados.
