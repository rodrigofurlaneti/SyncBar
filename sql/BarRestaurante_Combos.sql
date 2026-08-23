-- Fase 18 ("combos") — extensão mínima do modelo de complementos (Fase 6a) para suportar "combo":
-- um LinkedProductId opcional em ComplementItem, que aponta pra um Product real do cardápio (com
-- sua própria imagem/estoque) em vez do item ser só um texto solto. Nenhuma tabela nova — combo
-- continua sendo ComplementGroup/Complement/ComplementItem/ProductComplementGroup, exatamente
-- como qualquer outro grupo de opções (ex.: o grupo "Escolha o sanduíche" dentro de um combo é um
-- ComplementGroup normal, cujas opções são ComplementItem com LinkedProductId apontando pros
-- sanduíches vendáveis). A API do iFood não tem NENHUM endpoint de "combo" (Catalog v1 nem v2) —
-- confirmado por inspeção das collections oficiais — então não há sincronização adicional: o
-- optionGroup/option já sincroniza do mesmo jeito da Fase 6a (ver IFoodComplementGroupMapping/
-- IFoodComplementMapping), só passando a usar a imagem/descrição do produto vinculado quando
-- presente (ver MenuComplementsBuilder).
--
-- Idempotente — pode rodar de novo sem problema. Rodar DEPOIS de sql/BarRestaurante_Complementos.sql
-- (que cria ComplementItem) e de sql/BarRestaurante_DDL.sql (que cria Product).

USE BarRestauranteDb;

ALTER TABLE ComplementItem
    ADD COLUMN LinkedProductId BIGINT NULL AFTER Name;

ALTER TABLE ComplementItem
    ADD CONSTRAINT FK_ComplementItem_LinkedProduct FOREIGN KEY (LinkedProductId) REFERENCES Product (Id);

CREATE INDEX IX_ComplementItem_LinkedProductId ON ComplementItem (LinkedProductId);

-- Verificação
SELECT Id, CompanyId, Name, LinkedProductId, IsActive FROM ComplementItem;
