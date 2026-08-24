-- Fase 17 ("pizza") — cadastro de sabores, e a configuração de pizza (tamanhos, bordas, recheios
-- de borda e o preço de cada sabor por tamanho) de um Product. Espelha a hierarquia
-- sizes/crusts/edges/toppings da API de pizza (Catalog v1, legado) do iFood — ver comentário em
-- IFoodCatalogClient.IFoodCatalogV1Operation. Funciona 100% independente de integração com iFood —
-- é usado no balcão/mesa/QR Code também (mesmo espírito de BarRestaurante_Complementos.sql).
--
-- PizzaFlavor        : cadastro leve de sabor, reaproveitável entre várias pizzas da mesma empresa
--                      (ex.: "Calabresa"), com Description/ImageUrl porque o iFood exige esses
--                      campos no objeto "topping" da API v1.
-- PizzaConfiguration : 1:1 com Product — o Product vira "vendável como pizza" quando tem uma
--                      configuração ativa com pelo menos 1 tamanho e 1 preço de sabor.
-- PizzaSize/PizzaCrust/PizzaEdge : filhas de PizzaConfiguration — tamanhos, bordas e recheios
--                      de borda disponíveis para aquela pizza.
-- PizzaFlavorPrice   : o preço de um sabor num tamanho específico — a existência da linha é o que
--                      torna o sabor vendável naquele tamanho (sem tabela de vínculo separada).
-- OrderItemPizzaFlavor : sabor(es) efetivamente escolhidos numa linha (OrderItem) do pedido —
--                      fração de cada sabor guardada em FractionShare (ex.: 0.5 = meio a meio).
--                      Preço já congelado em OrderItem.UnitPrice (regra: sabor mais caro entre os
--                      escolhidos + borda + recheio de borda — decisão do SyncBar, não do iFood).
--
-- Idempotente — pode rodar de novo sem problema (CREATE TABLE IF NOT EXISTS).

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS PizzaFlavor (
    Id          BIGINT NOT NULL AUTO_INCREMENT,
    CompanyId   BIGINT NOT NULL,
    Name        NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    ImageUrl    NVARCHAR(300) NULL,
    CreatedAt   DATETIME(6) NOT NULL,
    UpdatedAt   DATETIME(6) NULL,
    IsActive    TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_PizzaFlavor_Company FOREIGN KEY (CompanyId) REFERENCES Company (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_PizzaFlavor_CompanyId ON PizzaFlavor (CompanyId);

CREATE TABLE IF NOT EXISTS PizzaConfiguration (
    Id        BIGINT NOT NULL AUTO_INCREMENT,
    ProductId BIGINT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NULL,
    IsActive  TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_PizzaConfiguration_Product FOREIGN KEY (ProductId) REFERENCES Product (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sem índice único filtrado (MySQL sem índice parcial confiável em CREATE INDEX) — "1
-- configuração ativa por produto" é garantido pelo handler (get-or-create), mesmo padrão de
-- IFoodProductMapping/ProductComplementGroup.
CREATE INDEX IX_PizzaConfiguration_ProductId ON PizzaConfiguration (ProductId);

CREATE TABLE IF NOT EXISTS PizzaSize (
    Id                   BIGINT NOT NULL AUTO_INCREMENT,
    PizzaConfigurationId BIGINT NOT NULL,
    Name                 NVARCHAR(150) NOT NULL,
    Slices               INT NULL,
    AcceptedFractions    INT NOT NULL,
    DisplayOrder         INT NOT NULL,
    CreatedAt            DATETIME(6) NOT NULL,
    UpdatedAt            DATETIME(6) NULL,
    IsActive             TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_PizzaSize_PizzaConfiguration FOREIGN KEY (PizzaConfigurationId) REFERENCES PizzaConfiguration (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_PizzaSize_PizzaConfigurationId ON PizzaSize (PizzaConfigurationId);

CREATE TABLE IF NOT EXISTS PizzaCrust (
    Id                   BIGINT NOT NULL AUTO_INCREMENT,
    PizzaConfigurationId BIGINT NOT NULL,
    Name                 NVARCHAR(150) NOT NULL,
    ExtraPrice           DECIMAL(18,2) NOT NULL,
    DisplayOrder         INT NOT NULL,
    CreatedAt            DATETIME(6) NOT NULL,
    UpdatedAt            DATETIME(6) NULL,
    IsActive             TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_PizzaCrust_PizzaConfiguration FOREIGN KEY (PizzaConfigurationId) REFERENCES PizzaConfiguration (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_PizzaCrust_PizzaConfigurationId ON PizzaCrust (PizzaConfigurationId);

CREATE TABLE IF NOT EXISTS PizzaEdge (
    Id                   BIGINT NOT NULL AUTO_INCREMENT,
    PizzaConfigurationId BIGINT NOT NULL,
    Name                 NVARCHAR(150) NOT NULL,
    ExtraPrice           DECIMAL(18,2) NOT NULL,
    DisplayOrder         INT NOT NULL,
    CreatedAt            DATETIME(6) NOT NULL,
    UpdatedAt            DATETIME(6) NULL,
    IsActive             TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_PizzaEdge_PizzaConfiguration FOREIGN KEY (PizzaConfigurationId) REFERENCES PizzaConfiguration (Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_PizzaEdge_PizzaConfigurationId ON PizzaEdge (PizzaConfigurationId);

CREATE TABLE IF NOT EXISTS PizzaFlavorPrice (
    Id                   BIGINT NOT NULL AUTO_INCREMENT,
    PizzaConfigurationId BIGINT NOT NULL,
    PizzaFlavorId        BIGINT NOT NULL,
    PizzaSizeId          BIGINT NOT NULL,
    Price                DECIMAL(18,2) NOT NULL,
    CreatedAt            DATETIME(6) NOT NULL,
    UpdatedAt            DATETIME(6) NULL,
    IsActive             TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_PizzaFlavorPrice_PizzaConfiguration FOREIGN KEY (PizzaConfigurationId) REFERENCES PizzaConfiguration (Id) ON DELETE CASCADE,
    CONSTRAINT FK_PizzaFlavorPrice_PizzaFlavor FOREIGN KEY (PizzaFlavorId) REFERENCES PizzaFlavor (Id),
    CONSTRAINT FK_PizzaFlavorPrice_PizzaSize FOREIGN KEY (PizzaSizeId) REFERENCES PizzaSize (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sem índice único filtrado (MySQL sem índice parcial) — "1 preço ativo por sabor x tamanho" é
-- garantido no domínio (PizzaConfiguration.SetFlavorPrice faz upsert in-memory antes de persistir).
CREATE INDEX IX_PizzaFlavorPrice_PizzaConfigurationId ON PizzaFlavorPrice (PizzaConfigurationId);
CREATE INDEX IX_PizzaFlavorPrice_PizzaFlavorId ON PizzaFlavorPrice (PizzaFlavorId);
CREATE INDEX IX_PizzaFlavorPrice_PizzaFlavorId_PizzaSizeId ON PizzaFlavorPrice (PizzaFlavorId, PizzaSizeId);

-- Fase 17 — colunas novas em OrderItem: preenchidas só quando o item lançado é uma pizza (ver
-- OrderItem.CreatePizza / CustomerOrder.AddPizzaItem). Rodar DEPOIS de sql/BarRestaurante_DDL.sql
-- (que cria OrderItem).
ALTER TABLE OrderItem
    ADD COLUMN PizzaSizeId  BIGINT NULL AFTER Notes,
    ADD COLUMN PizzaCrustId BIGINT NULL AFTER PizzaSizeId,
    ADD COLUMN PizzaEdgeId  BIGINT NULL AFTER PizzaCrustId;

ALTER TABLE OrderItem
    ADD CONSTRAINT FK_OrderItem_PizzaSize  FOREIGN KEY (PizzaSizeId)  REFERENCES PizzaSize (Id),
    ADD CONSTRAINT FK_OrderItem_PizzaCrust FOREIGN KEY (PizzaCrustId) REFERENCES PizzaCrust (Id),
    ADD CONSTRAINT FK_OrderItem_PizzaEdge  FOREIGN KEY (PizzaEdgeId)  REFERENCES PizzaEdge (Id);

CREATE INDEX IX_OrderItem_PizzaSizeId ON OrderItem (PizzaSizeId);

CREATE TABLE IF NOT EXISTS OrderItemPizzaFlavor (
    Id            BIGINT NOT NULL AUTO_INCREMENT,
    OrderItemId   BIGINT NOT NULL,
    PizzaFlavorId BIGINT NOT NULL,
    FractionShare DECIMAL(9,4) NOT NULL,
    CreatedAt     DATETIME(6) NOT NULL,
    IsActive      TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_OrderItemPizzaFlavor_OrderItem FOREIGN KEY (OrderItemId) REFERENCES OrderItem (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItemPizzaFlavor_PizzaFlavor FOREIGN KEY (PizzaFlavorId) REFERENCES PizzaFlavor (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_OrderItemPizzaFlavor_PizzaFlavorId ON OrderItemPizzaFlavor (PizzaFlavorId);

-- Verificação
SELECT Id, CompanyId, Name, Description, ImageUrl, CreatedAt, UpdatedAt, IsActive FROM PizzaFlavor;
SELECT Id, ProductId, CreatedAt, UpdatedAt, IsActive FROM PizzaConfiguration;
SELECT Id, PizzaConfigurationId, Name, Slices, AcceptedFractions, DisplayOrder, CreatedAt, UpdatedAt, IsActive FROM PizzaSize;
SELECT Id, PizzaConfigurationId, Name, ExtraPrice, DisplayOrder, CreatedAt, UpdatedAt, IsActive FROM PizzaCrust;
SELECT Id, PizzaConfigurationId, Name, ExtraPrice, DisplayOrder, CreatedAt, UpdatedAt, IsActive FROM PizzaEdge;
SELECT Id, PizzaConfigurationId, PizzaFlavorId, PizzaSizeId, Price, CreatedAt, UpdatedAt, IsActive FROM PizzaFlavorPrice;
SELECT Id, OrderItemId, PizzaFlavorId, FractionShare, CreatedAt, IsActive FROM OrderItemPizzaFlavor;

-- Nota: rodar ANTES de sql/BarRestaurante_IFoodPizza.sql (que referencia PizzaConfiguration por FK).
