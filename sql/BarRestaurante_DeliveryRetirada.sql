/* =====================================================================
   Delivery / retirada — CustomerOrder passa a suportar pedidos sem mesa/comanda.
   OrderTypeId inline (TINYINT + CHECK, sem tabela de lookup): 1=Mesa (padrão,
   comportamento atual), 2=Retirada, 3=Delivery (ver OrderTypeIds no código).
   ===================================================================== */

USE BarRestauranteDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CustomerOrder') AND name = 'OrderTypeId')
BEGIN
    ALTER TABLE dbo.CustomerOrder ADD OrderTypeId TINYINT NOT NULL CONSTRAINT DF_CustomerOrder_OrderTypeId DEFAULT (1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CustomerOrder') AND name = 'CustomerName')
BEGIN
    ALTER TABLE dbo.CustomerOrder ADD CustomerName NVARCHAR(150) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CustomerOrder') AND name = 'CustomerPhone')
BEGIN
    ALTER TABLE dbo.CustomerOrder ADD CustomerPhone VARCHAR(20) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CustomerOrder') AND name = 'DeliveryAddress')
BEGIN
    ALTER TABLE dbo.CustomerOrder ADD DeliveryAddress NVARCHAR(300) NULL;
END;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CustomerOrder_Origin')
BEGIN
    ALTER TABLE dbo.CustomerOrder DROP CONSTRAINT CK_CustomerOrder_Origin;
END;
GO

-- Mesa (1) continua exigindo DiningTableId ou ComandaId; Retirada (2)/Delivery (3) não.
ALTER TABLE dbo.CustomerOrder
    ADD CONSTRAINT CK_CustomerOrder_Origin
    CHECK (OrderTypeId <> 1 OR DiningTableId IS NOT NULL OR ComandaId IS NOT NULL);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CustomerOrder_OrderTypeId')
BEGIN
    ALTER TABLE dbo.CustomerOrder ADD CONSTRAINT CK_CustomerOrder_OrderTypeId CHECK (OrderTypeId BETWEEN 1 AND 3);
END;
GO
