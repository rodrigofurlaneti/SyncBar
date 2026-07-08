/* =====================================================================
   BarRestauranteDb — Script de Seed (dados iniciais)
   Executar APÓS BarRestaurante_DDL.sql
   Convenções:
     - DBCC CHECKIDENT (RESEED, 0) antes de cada bloco
     - SET IDENTITY_INSERT ON + TRY/CATCH para OFF
     - CONVERT(DATETIME2, 'yyyy-mm-ddThh:mm:ss', 126) para datas
   ===================================================================== */

USE BarRestauranteDb;
GO

/* ============================ UnitOfMeasure ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.UnitOfMeasure)
BEGIN
    DBCC CHECKIDENT ('dbo.UnitOfMeasure', RESEED, 0);
    SET IDENTITY_INSERT dbo.UnitOfMeasure ON;

    INSERT INTO dbo.UnitOfMeasure (Id, Name, Abbreviation) VALUES
        (1, N'Unidade',    'UN'),
        (2, N'Quilograma', 'KG'),
        (3, N'Grama',      'G'),
        (4, N'Litro',      'L'),
        (5, N'Mililitro',  'ML'),
        (6, N'Dose',       'DS'),
        (7, N'Porção',     'PC'),
        (8, N'Garrafa',    'GF'),
        (9, N'Lata',       'LT'),
        (10, N'Caixa',     'CX');

    BEGIN TRY SET IDENTITY_INSERT dbo.UnitOfMeasure OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ TableStatus ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.TableStatus)
BEGIN
    DBCC CHECKIDENT ('dbo.TableStatus', RESEED, 0);
    SET IDENTITY_INSERT dbo.TableStatus ON;

    INSERT INTO dbo.TableStatus (Id, Name) VALUES
        (1, N'Livre'),
        (2, N'Ocupada'),
        (3, N'Reservada'),
        (4, N'EmFechamento'),
        (5, N'Interditada');

    BEGIN TRY SET IDENTITY_INSERT dbo.TableStatus OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ ComandaStatus ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.ComandaStatus)
BEGIN
    DBCC CHECKIDENT ('dbo.ComandaStatus', RESEED, 0);
    SET IDENTITY_INSERT dbo.ComandaStatus ON;

    INSERT INTO dbo.ComandaStatus (Id, Name) VALUES
        (1, N'Disponível'),
        (2, N'EmUso'),
        (3, N'Extraviada'),
        (4, N'Bloqueada');

    BEGIN TRY SET IDENTITY_INSERT dbo.ComandaStatus OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ OrderStatus ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.OrderStatus)
BEGIN
    DBCC CHECKIDENT ('dbo.OrderStatus', RESEED, 0);
    SET IDENTITY_INSERT dbo.OrderStatus ON;

    INSERT INTO dbo.OrderStatus (Id, Name) VALUES
        (1, N'Aberto'),
        (2, N'EmAndamento'),
        (3, N'AguardandoPagamento'),
        (4, N'Pago'),
        (5, N'Cancelado');

    BEGIN TRY SET IDENTITY_INSERT dbo.OrderStatus OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ OrderItemStatus ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.OrderItemStatus)
BEGIN
    DBCC CHECKIDENT ('dbo.OrderItemStatus', RESEED, 0);
    SET IDENTITY_INSERT dbo.OrderItemStatus ON;

    INSERT INTO dbo.OrderItemStatus (Id, Name) VALUES
        (1, N'Lançado'),
        (2, N'EnviadoCozinha'),
        (3, N'EmPreparo'),
        (4, N'Pronto'),
        (5, N'Entregue'),
        (6, N'Cancelado');

    BEGIN TRY SET IDENTITY_INSERT dbo.OrderItemStatus OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ CashSessionStatus ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.CashSessionStatus)
BEGIN
    DBCC CHECKIDENT ('dbo.CashSessionStatus', RESEED, 0);
    SET IDENTITY_INSERT dbo.CashSessionStatus ON;

    INSERT INTO dbo.CashSessionStatus (Id, Name) VALUES
        (1, N'Aberto'),
        (2, N'Fechado'),
        (3, N'Conferido');

    BEGIN TRY SET IDENTITY_INSERT dbo.CashSessionStatus OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ CashMovementType ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.CashMovementType)
BEGIN
    DBCC CHECKIDENT ('dbo.CashMovementType', RESEED, 0);
    SET IDENTITY_INSERT dbo.CashMovementType ON;

    INSERT INTO dbo.CashMovementType (Id, Name, IsInflow) VALUES
        (1, N'Suprimento',       1),
        (2, N'Sangria',          0),
        (3, N'RecebimentoVenda', 1),
        (4, N'EstornoVenda',     0),
        (5, N'Despesa',          0);

    BEGIN TRY SET IDENTITY_INSERT dbo.CashMovementType OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ StockMovementType ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.StockMovementType)
BEGIN
    DBCC CHECKIDENT ('dbo.StockMovementType', RESEED, 0);
    SET IDENTITY_INSERT dbo.StockMovementType ON;

    INSERT INTO dbo.StockMovementType (Id, Name, IsInflow) VALUES
        (1, N'EntradaCompra',        1),
        (2, N'SaidaVenda',           0),
        (3, N'AjusteEntrada',        1),
        (4, N'AjusteSaida',          0),
        (5, N'Perda',                0),
        (6, N'Quebra',               0),
        (7, N'TransferenciaEntrada', 1),
        (8, N'TransferenciaSaida',   0),
        (9, N'DevolucaoFornecedor',  0),
        (10, N'ConsumoInterno',      0);

    BEGIN TRY SET IDENTITY_INSERT dbo.StockMovementType OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ PaymentMethod ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.PaymentMethod)
BEGIN
    DBCC CHECKIDENT ('dbo.PaymentMethod', RESEED, 0);
    SET IDENTITY_INSERT dbo.PaymentMethod ON;

    INSERT INTO dbo.PaymentMethod (Id, Name, AllowsChange) VALUES
        (1, N'Dinheiro',        1),
        (2, N'CartaoCredito',   0),
        (3, N'CartaoDebito',    0),
        (4, N'Pix',             0),
        (5, N'ValeRefeicao',    0),
        (6, N'ValeAlimentacao', 0),
        (7, N'Cortesia',        0);

    BEGIN TRY SET IDENTITY_INSERT dbo.PaymentMethod OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ Permission ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.Permission)
BEGIN
    DBCC CHECKIDENT ('dbo.Permission', RESEED, 0);
    SET IDENTITY_INSERT dbo.Permission ON;

    INSERT INTO dbo.Permission (Id, Code, Name, ModuleName) VALUES
        (1,  'Auth.ManageUsers',       N'Gerenciar usuários',            N'Autenticação'),
        (2,  'Auth.ManageRoles',       N'Gerenciar perfis e permissões', N'Autenticação'),
        (3,  'Employee.Read',          N'Consultar funcionários',        N'Funcionários'),
        (4,  'Employee.Manage',        N'Gerenciar funcionários',        N'Funcionários'),
        (5,  'Order.Create',           N'Abrir pedido (mesa/comanda)',   N'Pedidos'),
        (6,  'Order.AddItem',          N'Lançar itens no pedido',        N'Pedidos'),
        (7,  'Order.Cancel',           N'Cancelar pedido/item',          N'Pedidos'),
        (8,  'Order.ApplyDiscount',    N'Aplicar desconto',              N'Pedidos'),
        (9,  'Cash.OpenSession',       N'Abrir caixa',                   N'Caixa'),
        (10, 'Cash.CloseSession',      N'Fechar caixa',                  N'Caixa'),
        (11, 'Cash.Movement',          N'Sangria e suprimento',          N'Caixa'),
        (12, 'Sale.Register',          N'Registrar venda/pagamento',     N'Faturamento'),
        (13, 'Sale.Refund',            N'Estornar venda',                N'Faturamento'),
        (14, 'Billing.Reports',        N'Relatórios de faturamento',     N'Faturamento'),
        (15, 'Stock.Read',             N'Consultar estoque',             N'Estoque'),
        (16, 'Stock.Movement',         N'Lançar entrada/saída',          N'Estoque'),
        (17, 'Stock.Purchase',         N'Registrar compras',             N'Estoque'),
        (18, 'Stock.Adjust',           N'Ajustar/inventariar estoque',   N'Estoque');

    BEGIN TRY SET IDENTITY_INSERT dbo.Permission OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ Company / Branch ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.Company)
BEGIN
    DBCC CHECKIDENT ('dbo.Company', RESEED, 0);
    SET IDENTITY_INSERT dbo.Company ON;

    INSERT INTO dbo.Company (Id, LegalName, TradeName, Cnpj, Email, Phone) VALUES
        (1, N'Bar e Restaurante Exemplo LTDA', N'Restaurante Exemplo', '12345678000199', 'contato@exemplo.com.br', '11999990000');

    BEGIN TRY SET IDENTITY_INSERT dbo.Company OFF END TRY BEGIN CATCH END CATCH;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Branch)
BEGIN
    DBCC CHECKIDENT ('dbo.Branch', RESEED, 0);
    SET IDENTITY_INSERT dbo.Branch ON;

    INSERT INTO dbo.Branch (Id, CompanyId, Name, Cnpj, Phone, AddressStreet, AddressNumber, AddressDistrict, AddressCity, AddressState, AddressZipCode) VALUES
        (1, 1, N'Matriz — Centro', '12345678000199', '11999990000', N'Rua Exemplo', '100', N'Centro', N'São Paulo', 'SP', '01001000');

    BEGIN TRY SET IDENTITY_INSERT dbo.Branch OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ JobTitle / Employee ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.JobTitle)
BEGIN
    DBCC CHECKIDENT ('dbo.JobTitle', RESEED, 0);
    SET IDENTITY_INSERT dbo.JobTitle ON;

    INSERT INTO dbo.JobTitle (Id, CompanyId, Name) VALUES
        (1, 1, N'Gerente'),
        (2, 1, N'Garçom'),
        (3, 1, N'Operador de Caixa'),
        (4, 1, N'Cozinheiro'),
        (5, 1, N'Barman'),
        (6, 1, N'Estoquista');

    BEGIN TRY SET IDENTITY_INSERT dbo.JobTitle OFF END TRY BEGIN CATCH END CATCH;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Employee)
BEGIN
    DBCC CHECKIDENT ('dbo.Employee', RESEED, 0);
    SET IDENTITY_INSERT dbo.Employee ON;

    INSERT INTO dbo.Employee (Id, BranchId, JobTitleId, Name, Cpf, Email, Phone, HiredAt, Salary) VALUES
        (1, 1, 1, N'Administrador do Sistema', '00000000000', 'admin@exemplo.com.br', '11999990001', CONVERT(DATETIME2, '2026-01-05T08:00:00', 126), 8000.00);

    BEGIN TRY SET IDENTITY_INSERT dbo.Employee OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ Role / AppUser ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.Role)
BEGIN
    DBCC CHECKIDENT ('dbo.Role', RESEED, 0);
    SET IDENTITY_INSERT dbo.Role ON;

    INSERT INTO dbo.Role (Id, CompanyId, Name, Description) VALUES
        (1, 1, N'Administrador', N'Acesso total ao sistema'),
        (2, 1, N'Gerente',       N'Gestão operacional da filial'),
        (3, 1, N'Garçom',        N'Lançamento de pedidos em mesa e comanda'),
        (4, 1, N'Caixa',         N'Operação de caixa e recebimentos'),
        (5, 1, N'Estoquista',    N'Controle de estoque e compras');

    BEGIN TRY SET IDENTITY_INSERT dbo.Role OFF END TRY BEGIN CATCH END CATCH;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppUser)
BEGIN
    DBCC CHECKIDENT ('dbo.AppUser', RESEED, 0);
    SET IDENTITY_INSERT dbo.AppUser ON;

    /* Trocar o hash abaixo pelo hash real gerado pela aplicação (ex.: BCrypt) */
    INSERT INTO dbo.AppUser (Id, CompanyId, EmployeeId, UserName, Email, PasswordHash) VALUES
        (1, 1, 1, 'admin', 'admin@exemplo.com.br', '$2a$11$TROCAR.ESTE.HASH.NA.PRIMEIRA.UTILIZACAO');

    BEGIN TRY SET IDENTITY_INSERT dbo.AppUser OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* Perfil Administrador recebe todas as permissões (sem IDENTITY_INSERT — Ids livres) */
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermission)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT 1, P.Id FROM dbo.Permission AS P WHERE P.IsActive = 1;

    /* Garçom */
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT 3, P.Id FROM dbo.Permission AS P WHERE P.Code IN ('Order.Create', 'Order.AddItem');

    /* Caixa */
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT 4, P.Id FROM dbo.Permission AS P WHERE P.Code IN ('Cash.OpenSession', 'Cash.CloseSession', 'Cash.Movement', 'Sale.Register');

    /* Estoquista */
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT 5, P.Id FROM dbo.Permission AS P WHERE P.ModuleName = N'Estoque';
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.UserRole)
BEGIN
    INSERT INTO dbo.UserRole (AppUserId, RoleId) VALUES (1, 1);
END;
GO

/* ============================ Mesas, Comandas e Caixa ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.DiningTable)
BEGIN
    DBCC CHECKIDENT ('dbo.DiningTable', RESEED, 0);

    INSERT INTO dbo.DiningTable (BranchId, TableStatusId, Number, Capacity)
    SELECT 1, 1, N.Number, 4
    FROM (VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10)) AS N (Number);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Comanda)
BEGIN
    DBCC CHECKIDENT ('dbo.Comanda', RESEED, 0);

    INSERT INTO dbo.Comanda (BranchId, ComandaStatusId, Code)
    SELECT 1, 1, RIGHT('000' + CONVERT(VARCHAR(10), N.Number), 4)
    FROM (VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10),
                 (11), (12), (13), (14), (15), (16), (17), (18), (19), (20)) AS N (Number);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CashRegister)
BEGIN
    DBCC CHECKIDENT ('dbo.CashRegister', RESEED, 0);
    SET IDENTITY_INSERT dbo.CashRegister ON;

    INSERT INTO dbo.CashRegister (Id, BranchId, Name) VALUES
        (1, 1, N'Caixa 01');

    BEGIN TRY SET IDENTITY_INSERT dbo.CashRegister OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ Categorias e Produtos ============================ */
IF NOT EXISTS (SELECT 1 FROM dbo.Category)
BEGIN
    DBCC CHECKIDENT ('dbo.Category', RESEED, 0);
    SET IDENTITY_INSERT dbo.Category ON;

    INSERT INTO dbo.Category (Id, CompanyId, Name, DisplayOrder) VALUES
        (1, 1, N'Cervejas',            1),
        (2, 1, N'Drinks e Destilados', 2),
        (3, 1, N'Bebidas sem Álcool',  3),
        (4, 1, N'Porções',             4),
        (5, 1, N'Pratos Principais',   5),
        (6, 1, N'Sobremesas',          6);

    BEGIN TRY SET IDENTITY_INSERT dbo.Category OFF END TRY BEGIN CATCH END CATCH;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Product)
BEGIN
    DBCC CHECKIDENT ('dbo.Product', RESEED, 0);
    SET IDENTITY_INSERT dbo.Product ON;

    INSERT INTO dbo.Product (Id, CompanyId, CategoryId, UnitOfMeasureId, Name, Description, SalePrice, CostPrice, IsStockControlled, PreparationTimeMinutes) VALUES
        (1, 1, 1, 8, N'Cerveja Pilsen 600ml',      N'Garrafa 600ml',                14.90,  6.50, 1, NULL),
        (2, 1, 1, 9, N'Cerveja Lata 350ml',        N'Lata 350ml',                    8.90,  3.80, 1, NULL),
        (3, 1, 2, 6, N'Caipirinha de Limão',       N'Cachaça, limão e açúcar',      22.00,  7.00, 0, 8),
        (4, 1, 3, 9, N'Refrigerante Lata',         N'Lata 350ml',                    7.50,  3.00, 1, NULL),
        (5, 1, 3, 8, N'Água Mineral 500ml',        N'Com ou sem gás',                5.00,  1.50, 1, NULL),
        (6, 1, 4, 7, N'Porção de Batata Frita',    N'400g, serve 2 pessoas',        32.00, 10.00, 0, 20),
        (7, 1, 4, 7, N'Porção de Frango a Passarinho', N'500g, serve 2 pessoas',    39.00, 14.00, 0, 25),
        (8, 1, 5, 1, N'Parmegiana de Filé',        N'Acompanha arroz e fritas',     58.00, 22.00, 0, 35),
        (9, 1, 5, 1, N'Picanha na Chapa',          N'300g, acompanha vinagrete',    79.00, 35.00, 0, 30),
        (10, 1, 6, 1, N'Pudim de Leite',           N'Fatia',                        14.00,  4.00, 0, 5);

    BEGIN TRY SET IDENTITY_INSERT dbo.Product OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* Itens de estoque da filial 1 para produtos controlados */
IF NOT EXISTS (SELECT 1 FROM dbo.StockItem)
BEGIN
    DBCC CHECKIDENT ('dbo.StockItem', RESEED, 0);

    INSERT INTO dbo.StockItem (BranchId, ProductId, CurrentQuantity, MinimumQuantity)
    SELECT 1, P.Id, 0, 12
    FROM dbo.Product AS P
    WHERE P.IsStockControlled = 1 AND P.IsActive = 1;
END;
GO

/* ===================== FIM DO SCRIPT DE SEED ===================== */
