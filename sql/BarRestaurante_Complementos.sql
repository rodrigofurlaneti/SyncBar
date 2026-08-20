-- Fase 6a ("complementos") — cadastro de complementos e grupos de complementos, e o vínculo
-- deles com o pedido (item lançado) e com o produto. Espelha 1:1 a hierarquia optionGroup/option
-- do módulo Catalog do iFood (ver ComplementGroupTypeIds em LookupIds.cs), mas funciona 100%
-- independente de integração com iFood — é usado no balcão/mesa/QR Code também.
--
-- ComplementItem   : cadastro leve pro nome do complemento (ex.: "bacon extra", "sem cebola") —
--                    não é um Product completo (sem categoria/estoque/preço de balcão).
-- ComplementGroup   : agrupador (ex.: "Escolha uma bebida"), com MinSelection/MaxSelection.
-- Complement        : uma opção dentro de um ComplementGroup, referenciando um ComplementItem
--                    com o preço adicional (ExtraPrice) específico daquele grupo.
-- ProductComplementGroup : liga um Product a um ComplementGroup (produto pode ter vários grupos).
-- OrderItemComplement    : complemento efetivamente escolhido numa linha (OrderItem) do pedido —
--                    preço congelado no lançamento (UnitPriceCharged), mesma regra de
--                    OrderItem.UnitPrice.
--
-- Idempotente — pode rodar de novo sem problema (CREATE TABLE IF NOT EXISTS).

USE BarRestauranteDb;

CREATE TABLE IF NOT EXISTS ComplementItem (
    Id        BIGINT NOT NULL AUTO_INCREMENT,
    CompanyId BIGINT NOT NULL,
    Name      NVARCHAR(150) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NULL,
    IsActive  TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_ComplementItem_Company FOREIGN KEY (CompanyId) REFERENCES Company (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_ComplementItem_CompanyId ON ComplementItem (CompanyId);

CREATE TABLE IF NOT EXISTS ComplementGroup (
    Id                    BIGINT NOT NULL AUTO_INCREMENT,
    CompanyId             BIGINT NOT NULL,
    Name                  NVARCHAR(150) NOT NULL,
    -- Constante de código (sem tabela própria) — ver ComplementGroupTypeIds em LookupIds.cs.
    -- 1 = SelecaoAdicional (OFFER_UNIT), 2 = Especificacao (SPECIFICATION),
    -- 3 = Ingredientes (INGREDIENTS), 4 = Utensilios (CUTLERY).
    ComplementGroupTypeId TINYINT NOT NULL,
    MinSelection          INT NOT NULL,
    MaxSelection          INT NOT NULL,
    CreatedAt             DATETIME(6) NOT NULL,
    UpdatedAt             DATETIME(6) NULL,
    IsActive              TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_ComplementGroup_Company FOREIGN KEY (CompanyId) REFERENCES Company (Id),
    CONSTRAINT CK_ComplementGroup_TypeId CHECK (ComplementGroupTypeId BETWEEN 1 AND 4)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_ComplementGroup_CompanyId ON ComplementGroup (CompanyId);

CREATE TABLE IF NOT EXISTS Complement (
    Id                  BIGINT NOT NULL AUTO_INCREMENT,
    ComplementGroupId   BIGINT NOT NULL,
    ComplementItemId    BIGINT NOT NULL,
    ExtraPrice          DECIMAL(18,2) NOT NULL,
    CreatedAt           DATETIME(6) NOT NULL,
    UpdatedAt           DATETIME(6) NULL,
    IsActive            TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_Complement_ComplementGroup FOREIGN KEY (ComplementGroupId) REFERENCES ComplementGroup (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Complement_ComplementItem FOREIGN KEY (ComplementItemId) REFERENCES ComplementItem (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_Complement_ComplementGroupId ON Complement (ComplementGroupId);
CREATE INDEX IX_Complement_ComplementItemId ON Complement (ComplementItemId);

CREATE TABLE IF NOT EXISTS ProductComplementGroup (
    Id                  BIGINT NOT NULL AUTO_INCREMENT,
    ProductId           BIGINT NOT NULL,
    ComplementGroupId   BIGINT NOT NULL,
    DisplayOrder        INT NOT NULL,
    CreatedAt           DATETIME(6) NOT NULL,
    UpdatedAt           DATETIME(6) NULL,
    IsActive            TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_ProductComplementGroup_Product FOREIGN KEY (ProductId) REFERENCES Product (Id),
    CONSTRAINT FK_ProductComplementGroup_ComplementGroup FOREIGN KEY (ComplementGroupId) REFERENCES ComplementGroup (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sem índice único filtrado (MySQL sem índice parcial confiável em CREATE INDEX) — "1 vínculo
-- ativo por produto x grupo" é garantido pelo handler (LinkProductComplementGroupCommandHandler
-- checa duplicata antes de inserir), mesmo padrão de IFoodProductMapping.
CREATE INDEX IX_ProductComplementGroup_ProductId ON ProductComplementGroup (ProductId);
CREATE INDEX IX_ProductComplementGroup_ComplementGroupId ON ProductComplementGroup (ComplementGroupId);

CREATE TABLE IF NOT EXISTS OrderItemComplement (
    Id                BIGINT NOT NULL AUTO_INCREMENT,
    OrderItemId       BIGINT NOT NULL,
    ComplementId      BIGINT NOT NULL,
    UnitPriceCharged  DECIMAL(18,2) NOT NULL,
    CreatedAt         DATETIME(6) NOT NULL,
    UpdatedAt         DATETIME(6) NULL,
    IsActive          TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_OrderItemComplement_OrderItem FOREIGN KEY (OrderItemId) REFERENCES OrderItem (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItemComplement_Complement FOREIGN KEY (ComplementId) REFERENCES Complement (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_OrderItemComplement_OrderItemId ON OrderItemComplement (OrderItemId);
CREATE INDEX IX_OrderItemComplement_ComplementId ON OrderItemComplement (ComplementId);

-- Verificação
SELECT * FROM ComplementItem;
SELECT * FROM ComplementGroup;
SELECT * FROM Complement;
SELECT * FROM ProductComplementGroup;
SELECT * FROM OrderItemComplement;

-- Nota: rodar ANTES de sql/BarRestaurante_IFoodComplementos.sql (que referencia ComplementGroup
-- e Complement por FK).
