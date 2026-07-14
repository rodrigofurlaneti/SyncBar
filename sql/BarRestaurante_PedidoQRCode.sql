/* =====================================================================
   Pedido via QR Code (autoatendimento) — token público por mesa (DiningTable.QrToken)
   e funcionário "dono" dos pedidos de autoatendimento (Branch.SelfServiceEmployeeId).
   ===================================================================== */

USE BarRestauranteDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DiningTable') AND name = 'QrToken')
BEGIN
    ALTER TABLE dbo.DiningTable ADD QrToken UNIQUEIDENTIFIER NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_DiningTable_QrToken')
BEGIN
    CREATE UNIQUE INDEX UQ_DiningTable_QrToken ON dbo.DiningTable(QrToken) WHERE QrToken IS NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Branch') AND name = 'SelfServiceEmployeeId')
BEGIN
    ALTER TABLE dbo.Branch ADD SelfServiceEmployeeId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Branch_SelfServiceEmployee')
BEGIN
    ALTER TABLE dbo.Branch ADD CONSTRAINT FK_Branch_SelfServiceEmployee FOREIGN KEY (SelfServiceEmployeeId) REFERENCES dbo.Employee(Id);
END;
GO
