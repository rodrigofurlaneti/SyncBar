/* =====================================================================
   BarRestauranteDb - Modulo Impressao
   Printer: impressoras da filial (USB via driver Windows ou rede TCP:9100)
     - PrintsOrders: imprime PEDIDOS (cozinha/bar)
     - PrintsBills : imprime CONTAS e fechamentos (caixa)
   PrinterSetting: interruptores gerais por filial (pedido / conta).
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.Printer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Printer
    (
        Id             BIGINT IDENTITY(1,1) NOT NULL,
        BranchId       BIGINT        NOT NULL,
        Name           NVARCHAR(100) NOT NULL,
        ConnectionType INT           NOT NULL, -- 1 = Windows/USB (driver) | 2 = Rede (TCP raw)
        PrinterName    NVARCHAR(200) NULL,     -- nome exato do driver no Windows
        IpAddress      VARCHAR(45)   NULL,
        Port           INT           NULL,
        PrintsOrders   BIT           NOT NULL CONSTRAINT DF_Printer_PrintsOrders DEFAULT (0),
        PrintsBills    BIT           NOT NULL CONSTRAINT DF_Printer_PrintsBills DEFAULT (0),
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_Printer_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt      DATETIME2     NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_Printer_IsActive DEFAULT (1),
        CONSTRAINT PK_Printer PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Printer_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT CK_Printer_ConnectionType CHECK (ConnectionType IN (1, 2)),
        CONSTRAINT CK_Printer_Target CHECK (
            (ConnectionType = 1 AND PrinterName IS NOT NULL) OR
            (ConnectionType = 2 AND IpAddress IS NOT NULL AND Port IS NOT NULL))
    );

    CREATE NONCLUSTERED INDEX IX_Printer_BranchId ON dbo.Printer (BranchId);
END;
GO

IF OBJECT_ID('dbo.PrinterSetting', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrinterSetting
    (
        Id                  BIGINT IDENTITY(1,1) NOT NULL,
        BranchId            BIGINT    NOT NULL,
        PrintOrdersEnabled  BIT       NOT NULL CONSTRAINT DF_PrinterSetting_PrintOrdersEnabled DEFAULT (1),
        PrintBillsEnabled   BIT       NOT NULL CONSTRAINT DF_PrinterSetting_PrintBillsEnabled DEFAULT (1),
        CreatedAt           DATETIME2 NOT NULL CONSTRAINT DF_PrinterSetting_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt           DATETIME2 NULL,
        IsActive            BIT       NOT NULL CONSTRAINT DF_PrinterSetting_IsActive DEFAULT (1),
        CONSTRAINT PK_PrinterSetting PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_PrinterSetting_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_PrinterSetting_BranchId
        ON dbo.PrinterSetting (BranchId) WHERE IsActive = 1;
END;
GO

INSERT INTO dbo.AppFeature (Code, Name)
SELECT 'Impressao', N'Impressao (impressoras e recibos)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppFeature WHERE Code = 'Impressao' AND IsActive = 1);
GO

/* ============================ Seed ============================ */

DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);

IF NOT EXISTS (SELECT 1 FROM dbo.PrinterSetting WHERE BranchId = @BranchId AND IsActive = 1)
    INSERT INTO dbo.PrinterSetting (BranchId, PrintOrdersEnabled, PrintBillsEnabled)
    VALUES (@BranchId, 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Printer WHERE BranchId = @BranchId AND IsActive = 1)
    INSERT INTO dbo.Printer (BranchId, Name, ConnectionType, PrinterName, PrintsOrders, PrintsBills)
    VALUES (@BranchId, N'Caixa - ELGIN i9', 1, N'ELGIN i9(USB)', 1, 1);

SELECT Name, ConnectionType, PrinterName, IpAddress, Port, PrintsOrders, PrintsBills
FROM dbo.Printer WHERE IsActive = 1;
GO
