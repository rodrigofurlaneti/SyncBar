/* =====================================================================
   Cadastro de clientes / CRM e fidelidade — tabela Customer + vínculo opcional
   com CustomerOrder (histórico de pedidos por cliente).
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.Customer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customer
    (
        Id             BIGINT IDENTITY(1,1) NOT NULL,
        CompanyId      BIGINT        NOT NULL,
        Name           NVARCHAR(150) NOT NULL,
        Phone          VARCHAR(20)   NULL,
        Cpf            CHAR(11)      NULL,
        Email          VARCHAR(150)  NULL,
        LoyaltyPoints  INT           NOT NULL CONSTRAINT DF_Customer_LoyaltyPoints DEFAULT (0),
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_Customer_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt      DATETIME2     NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_Customer_IsActive DEFAULT (1),

        CONSTRAINT PK_Customer PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Customer_LoyaltyPoints CHECK (LoyaltyPoints >= 0),
        CONSTRAINT FK_Customer_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company(Id)
    );

    CREATE INDEX IX_Customer_CompanyId ON dbo.Customer(CompanyId);
    CREATE UNIQUE INDEX UQ_Customer_Cpf ON dbo.Customer(CompanyId, Cpf) WHERE Cpf IS NOT NULL AND IsActive = 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CustomerOrder') AND name = 'CustomerId')
BEGIN
    ALTER TABLE dbo.CustomerOrder ADD CustomerId BIGINT NULL;
    ALTER TABLE dbo.CustomerOrder ADD CONSTRAINT FK_CustomerOrder_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id);
    CREATE INDEX IX_CustomerOrder_CustomerId ON dbo.CustomerOrder(CustomerId);
END;
GO
