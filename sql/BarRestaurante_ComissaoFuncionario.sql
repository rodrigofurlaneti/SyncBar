/* =====================================================================
   Comissão de garçom/vendedor — adiciona percentual de comissão ao Employee.
   O relatório de comissão é calculado em runtime a partir de Sale.EmployeeId
   (não precisa de tabela nova: comissão = soma das vendas do período * %).
   ===================================================================== */

USE BarRestauranteDb;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Employee') AND name = 'CommissionPercent'
)
BEGIN
    ALTER TABLE dbo.Employee ADD CommissionPercent DECIMAL(5,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Employee_CommissionPercent'
)
BEGIN
    ALTER TABLE dbo.Employee
        ADD CONSTRAINT CK_Employee_CommissionPercent
        CHECK (CommissionPercent IS NULL OR CommissionPercent BETWEEN 0 AND 100);
END;
GO
