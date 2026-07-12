/* =====================================================================
   BarRestauranteDb - Onda de melhorias 1
   - OrderItem.CancelledByEmployeeId: quem cancelou o item (auditoria)
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF COL_LENGTH('dbo.OrderItem', 'CancelledByEmployeeId') IS NULL
BEGIN
    ALTER TABLE dbo.OrderItem ADD CancelledByEmployeeId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItem_CancelledByEmployee')
BEGIN
    ALTER TABLE dbo.OrderItem ADD
        CONSTRAINT FK_OrderItem_CancelledByEmployee FOREIGN KEY (CancelledByEmployeeId) REFERENCES dbo.Employee (Id);
END;
GO
