/* =====================================================================
   Reservas de mesa — TableReservation
   Status inline (TINYINT + CHECK) em vez de tabela de lookup: 1=Pending,
   2=Confirmed, 3=Cancelled, 4=Seated, 5=NoShow (ver ReservationStatusIds no código).
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.TableReservation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TableReservation
    (
        Id                   BIGINT IDENTITY(1,1) NOT NULL,
        BranchId             BIGINT        NOT NULL,
        DiningTableId        BIGINT        NULL,
        CustomerName         NVARCHAR(150) NOT NULL,
        CustomerPhone        VARCHAR(20)   NULL,
        PartySize            INT           NOT NULL,
        ReservedFor          DATETIME2     NOT NULL,
        ReservationStatusId  TINYINT       NOT NULL CONSTRAINT DF_TableReservation_Status DEFAULT (1),
        Notes                NVARCHAR(500) NULL,
        CreatedAt            DATETIME2     NOT NULL CONSTRAINT DF_TableReservation_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt            DATETIME2     NULL,
        IsActive             BIT           NOT NULL CONSTRAINT DF_TableReservation_IsActive DEFAULT (1),

        CONSTRAINT PK_TableReservation PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_TableReservation_PartySize CHECK (PartySize > 0),
        CONSTRAINT CK_TableReservation_Status CHECK (ReservationStatusId BETWEEN 1 AND 5),
        CONSTRAINT FK_TableReservation_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),
        CONSTRAINT FK_TableReservation_DiningTable FOREIGN KEY (DiningTableId) REFERENCES dbo.DiningTable(Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TableReservation_BranchId_ReservedFor')
BEGIN
    CREATE INDEX IX_TableReservation_BranchId_ReservedFor ON dbo.TableReservation(BranchId, ReservedFor);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TableReservation_DiningTableId')
BEGIN
    CREATE INDEX IX_TableReservation_DiningTableId ON dbo.TableReservation(DiningTableId);
END;
GO
