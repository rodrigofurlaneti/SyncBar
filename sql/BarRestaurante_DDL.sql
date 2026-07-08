/* =====================================================================
   BarRestauranteDb — Script DDL (SQL Server)
   Sistema de Gestão de Bar e Restaurante — Multi-empresa / Multi-filial
   Convenções:
     - PascalCase em tabelas, colunas, índices e FKs
     - BIGINT IDENTITY(1,1) em todas as PKs
     - DATETIME2 em todas as datas
     - DECIMAL(18,2) para valores monetários / DECIMAL(18,3) para quantidades
     - Soft delete: IsActive BIT (nunca DELETE físico)
   Gerado em: 2026-07-08
   ===================================================================== */

IF DB_ID('BarRestauranteDb') IS NULL
BEGIN
    CREATE DATABASE BarRestauranteDb;
END;
GO

USE BarRestauranteDb;
GO

/* =====================================================================
   1. ESTRUTURA ORGANIZACIONAL
   ===================================================================== */

IF OBJECT_ID('dbo.Company', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Company
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL,
        LegalName   NVARCHAR(200) NOT NULL,
        TradeName   NVARCHAR(150) NOT NULL,
        Cnpj        CHAR(14)      NOT NULL,
        Email       VARCHAR(150)  NULL,
        Phone       VARCHAR(20)   NULL,
        CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Company_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt   DATETIME2     NULL,
        IsActive    BIT           NOT NULL CONSTRAINT DF_Company_IsActive DEFAULT (1),
        CONSTRAINT PK_Company PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_Company_Cnpj ON dbo.Company (Cnpj) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.Branch', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Branch
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        CompanyId       BIGINT        NOT NULL,
        Name            NVARCHAR(150) NOT NULL,
        Cnpj            CHAR(14)      NULL,
        Phone           VARCHAR(20)   NULL,
        AddressStreet   NVARCHAR(200) NULL,
        AddressNumber   VARCHAR(20)   NULL,
        AddressDistrict NVARCHAR(100) NULL,
        AddressCity     NVARCHAR(100) NULL,
        AddressState    CHAR(2)       NULL,
        AddressZipCode  CHAR(8)       NULL,
        CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Branch_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt       DATETIME2     NULL,
        IsActive        BIT           NOT NULL CONSTRAINT DF_Branch_IsActive DEFAULT (1),
        CONSTRAINT PK_Branch PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Branch_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Branch_CompanyId ON dbo.Branch (CompanyId);
END;
GO

/* =====================================================================
   2. TABELAS DE DOMÍNIO (LOOKUPS)
   ===================================================================== */

IF OBJECT_ID('dbo.UnitOfMeasure', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UnitOfMeasure
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        Name         NVARCHAR(50) NOT NULL,
        Abbreviation VARCHAR(10)  NOT NULL,
        CreatedAt    DATETIME2    NOT NULL CONSTRAINT DF_UnitOfMeasure_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt    DATETIME2    NULL,
        IsActive     BIT          NOT NULL CONSTRAINT DF_UnitOfMeasure_IsActive DEFAULT (1),
        CONSTRAINT PK_UnitOfMeasure PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.TableStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TableStatus
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_TableStatus_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_TableStatus_IsActive DEFAULT (1),
        CONSTRAINT PK_TableStatus PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.ComandaStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComandaStatus
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_ComandaStatus_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_ComandaStatus_IsActive DEFAULT (1),
        CONSTRAINT PK_ComandaStatus PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.OrderStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderStatus
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_OrderStatus_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_OrderStatus_IsActive DEFAULT (1),
        CONSTRAINT PK_OrderStatus PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.OrderItemStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItemStatus
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_OrderItemStatus_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_OrderItemStatus_IsActive DEFAULT (1),
        CONSTRAINT PK_OrderItemStatus PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.CashSessionStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashSessionStatus
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_CashSessionStatus_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_CashSessionStatus_IsActive DEFAULT (1),
        CONSTRAINT PK_CashSessionStatus PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.CashMovementType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashMovementType
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        IsInflow  BIT          NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_CashMovementType_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_CashMovementType_IsActive DEFAULT (1),
        CONSTRAINT PK_CashMovementType PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.StockMovementType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovementType
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        IsInflow  BIT          NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_StockMovementType_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_StockMovementType_IsActive DEFAULT (1),
        CONSTRAINT PK_StockMovementType PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.PaymentMethod', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentMethod
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        Name         NVARCHAR(50) NOT NULL,
        AllowsChange BIT          NOT NULL CONSTRAINT DF_PaymentMethod_AllowsChange DEFAULT (0),
        CreatedAt    DATETIME2    NOT NULL CONSTRAINT DF_PaymentMethod_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt    DATETIME2    NULL,
        IsActive     BIT          NOT NULL CONSTRAINT DF_PaymentMethod_IsActive DEFAULT (1),
        CONSTRAINT PK_PaymentMethod PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* =====================================================================
   3. FUNCIONÁRIOS
   ===================================================================== */

IF OBJECT_ID('dbo.JobTitle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobTitle
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        CompanyId BIGINT        NOT NULL,
        Name      NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_JobTitle_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2     NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_JobTitle_IsActive DEFAULT (1),
        CONSTRAINT PK_JobTitle PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_JobTitle_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (Id)
    );

    CREATE NONCLUSTERED INDEX IX_JobTitle_CompanyId ON dbo.JobTitle (CompanyId);
END;
GO

IF OBJECT_ID('dbo.Employee', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employee
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL,
        BranchId    BIGINT        NOT NULL,
        JobTitleId  BIGINT        NOT NULL,
        Name        NVARCHAR(150) NOT NULL,
        Cpf         CHAR(11)      NOT NULL,
        Email       VARCHAR(150)  NULL,
        Phone       VARCHAR(20)   NULL,
        HiredAt     DATETIME2     NOT NULL,
        DismissedAt DATETIME2     NULL,
        Salary      DECIMAL(18,2) NULL,
        CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Employee_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt   DATETIME2     NULL,
        IsActive    BIT           NOT NULL CONSTRAINT DF_Employee_IsActive DEFAULT (1),
        CONSTRAINT PK_Employee PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Employee_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_Employee_JobTitle FOREIGN KEY (JobTitleId) REFERENCES dbo.JobTitle (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Employee_BranchId ON dbo.Employee (BranchId);
    CREATE NONCLUSTERED INDEX IX_Employee_JobTitleId ON dbo.Employee (JobTitleId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Employee_Cpf ON dbo.Employee (Cpf) WHERE IsActive = 1;
END;
GO

/* =====================================================================
   4. AUTENTICAÇÃO E CONTROLE DE ACESSO
   ===================================================================== */

IF OBJECT_ID('dbo.AppUser', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUser
    (
        Id                BIGINT IDENTITY(1,1) NOT NULL,
        CompanyId         BIGINT       NOT NULL,
        EmployeeId        BIGINT       NULL,
        UserName          VARCHAR(100) NOT NULL,
        Email             VARCHAR(150) NOT NULL,
        PasswordHash      VARCHAR(500) NOT NULL,
        PasswordSalt      VARCHAR(200) NULL,
        FailedAccessCount INT          NOT NULL CONSTRAINT DF_AppUser_FailedAccessCount DEFAULT (0),
        LockoutEndAt      DATETIME2    NULL,
        LastLoginAt       DATETIME2    NULL,
        CreatedAt         DATETIME2    NOT NULL CONSTRAINT DF_AppUser_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt         DATETIME2    NULL,
        IsActive          BIT          NOT NULL CONSTRAINT DF_AppUser_IsActive DEFAULT (1),
        CONSTRAINT PK_AppUser PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AppUser_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (Id),
        CONSTRAINT FK_AppUser_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (Id)
    );

    CREATE NONCLUSTERED INDEX IX_AppUser_CompanyId ON dbo.AppUser (CompanyId);
    CREATE NONCLUSTERED INDEX IX_AppUser_EmployeeId ON dbo.AppUser (EmployeeId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AppUser_UserName ON dbo.AppUser (UserName) WHERE IsActive = 1;
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AppUser_Email ON dbo.AppUser (Email) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.Role', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Role
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL,
        CompanyId   BIGINT        NOT NULL,
        Name        NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NULL,
        CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Role_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt   DATETIME2     NULL,
        IsActive    BIT           NOT NULL CONSTRAINT DF_Role_IsActive DEFAULT (1),
        CONSTRAINT PK_Role PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Role_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Role_CompanyId ON dbo.Role (CompanyId);
END;
GO

IF OBJECT_ID('dbo.Permission', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permission
    (
        Id         BIGINT IDENTITY(1,1) NOT NULL,
        Code       VARCHAR(100)  NOT NULL,
        Name       NVARCHAR(150) NOT NULL,
        ModuleName NVARCHAR(100) NOT NULL,
        CreatedAt  DATETIME2     NOT NULL CONSTRAINT DF_Permission_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt  DATETIME2     NULL,
        IsActive   BIT           NOT NULL CONSTRAINT DF_Permission_IsActive DEFAULT (1),
        CONSTRAINT PK_Permission PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_Permission_Code ON dbo.Permission (Code) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.RolePermission', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermission
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        RoleId       BIGINT    NOT NULL,
        PermissionId BIGINT    NOT NULL,
        CreatedAt    DATETIME2 NOT NULL CONSTRAINT DF_RolePermission_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt    DATETIME2 NULL,
        IsActive     BIT       NOT NULL CONSTRAINT DF_RolePermission_IsActive DEFAULT (1),
        CONSTRAINT PK_RolePermission PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RolePermission_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role (Id),
        CONSTRAINT FK_RolePermission_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.Permission (Id)
    );

    CREATE NONCLUSTERED INDEX IX_RolePermission_RoleId ON dbo.RolePermission (RoleId);
    CREATE NONCLUSTERED INDEX IX_RolePermission_PermissionId ON dbo.RolePermission (PermissionId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_RolePermission_RoleId_PermissionId
        ON dbo.RolePermission (RoleId, PermissionId) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.UserRole', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRole
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        AppUserId BIGINT    NOT NULL,
        RoleId    BIGINT    NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserRole_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2 NULL,
        IsActive  BIT       NOT NULL CONSTRAINT DF_UserRole_IsActive DEFAULT (1),
        CONSTRAINT PK_UserRole PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_UserRole_AppUser FOREIGN KEY (AppUserId) REFERENCES dbo.AppUser (Id),
        CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role (Id)
    );

    CREATE NONCLUSTERED INDEX IX_UserRole_AppUserId ON dbo.UserRole (AppUserId);
    CREATE NONCLUSTERED INDEX IX_UserRole_RoleId ON dbo.UserRole (RoleId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_UserRole_AppUserId_RoleId
        ON dbo.UserRole (AppUserId, RoleId) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.RefreshToken', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshToken
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        AppUserId BIGINT       NOT NULL,
        Token     VARCHAR(500) NOT NULL,
        ExpiresAt DATETIME2    NOT NULL,
        RevokedAt DATETIME2    NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_RefreshToken_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_RefreshToken_IsActive DEFAULT (1),
        CONSTRAINT PK_RefreshToken PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RefreshToken_AppUser FOREIGN KEY (AppUserId) REFERENCES dbo.AppUser (Id)
    );

    CREATE NONCLUSTERED INDEX IX_RefreshToken_AppUserId ON dbo.RefreshToken (AppUserId);
END;
GO

IF OBJECT_ID('dbo.AccessLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccessLog
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        AppUserId BIGINT        NULL,
        UserName  VARCHAR(100)  NOT NULL,
        EventType VARCHAR(30)   NOT NULL,
        IpAddress VARCHAR(45)   NULL,
        UserAgent NVARCHAR(300) NULL,
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_AccessLog_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2     NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_AccessLog_IsActive DEFAULT (1),
        CONSTRAINT PK_AccessLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AccessLog_AppUser FOREIGN KEY (AppUserId) REFERENCES dbo.AppUser (Id),
        CONSTRAINT CK_AccessLog_EventType CHECK (EventType IN ('Login', 'Logout', 'LoginFailed', 'Lockout'))
    );

    CREATE NONCLUSTERED INDEX IX_AccessLog_AppUserId ON dbo.AccessLog (AppUserId);
END;
GO

/* =====================================================================
   5. PRODUTOS, FORNECEDORES E ESTOQUE
   ===================================================================== */

IF OBJECT_ID('dbo.Category', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Category
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        CompanyId    BIGINT        NOT NULL,
        Name         NVARCHAR(100) NOT NULL,
        DisplayOrder INT           NOT NULL CONSTRAINT DF_Category_DisplayOrder DEFAULT (0),
        CreatedAt    DATETIME2     NOT NULL CONSTRAINT DF_Category_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt    DATETIME2     NULL,
        IsActive     BIT           NOT NULL CONSTRAINT DF_Category_IsActive DEFAULT (1),
        CONSTRAINT PK_Category PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Category_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Category_CompanyId ON dbo.Category (CompanyId);
END;
GO

IF OBJECT_ID('dbo.Product', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Product
    (
        Id                     BIGINT IDENTITY(1,1) NOT NULL,
        CompanyId              BIGINT        NOT NULL,
        CategoryId             BIGINT        NOT NULL,
        UnitOfMeasureId        BIGINT        NOT NULL,
        Name                   NVARCHAR(150) NOT NULL,
        Description            NVARCHAR(500) NULL,
        Barcode                VARCHAR(50)   NULL,
        SalePrice              DECIMAL(18,2) NOT NULL,
        CostPrice              DECIMAL(18,2) NULL,
        IsStockControlled      BIT           NOT NULL CONSTRAINT DF_Product_IsStockControlled DEFAULT (1),
        PreparationTimeMinutes INT           NULL,
        CreatedAt              DATETIME2     NOT NULL CONSTRAINT DF_Product_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt              DATETIME2     NULL,
        IsActive               BIT           NOT NULL CONSTRAINT DF_Product_IsActive DEFAULT (1),
        CONSTRAINT PK_Product PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Product_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (Id),
        CONSTRAINT FK_Product_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category (Id),
        CONSTRAINT FK_Product_UnitOfMeasure FOREIGN KEY (UnitOfMeasureId) REFERENCES dbo.UnitOfMeasure (Id),
        CONSTRAINT CK_Product_SalePrice CHECK (SalePrice >= 0)
    );

    CREATE NONCLUSTERED INDEX IX_Product_CompanyId ON dbo.Product (CompanyId);
    CREATE NONCLUSTERED INDEX IX_Product_CategoryId ON dbo.Product (CategoryId);
    CREATE NONCLUSTERED INDEX IX_Product_UnitOfMeasureId ON dbo.Product (UnitOfMeasureId);
END;
GO

IF OBJECT_ID('dbo.Supplier', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Supplier
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        CompanyId BIGINT        NOT NULL,
        LegalName NVARCHAR(200) NOT NULL,
        TradeName NVARCHAR(150) NULL,
        Cnpj      CHAR(14)      NULL,
        Email     VARCHAR(150)  NULL,
        Phone     VARCHAR(20)   NULL,
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_Supplier_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2     NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Supplier_IsActive DEFAULT (1),
        CONSTRAINT PK_Supplier PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Supplier_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Company (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Supplier_CompanyId ON dbo.Supplier (CompanyId);
END;
GO

IF OBJECT_ID('dbo.StockItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockItem
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        BranchId        BIGINT        NOT NULL,
        ProductId       BIGINT        NOT NULL,
        CurrentQuantity DECIMAL(18,3) NOT NULL CONSTRAINT DF_StockItem_CurrentQuantity DEFAULT (0),
        MinimumQuantity DECIMAL(18,3) NOT NULL CONSTRAINT DF_StockItem_MinimumQuantity DEFAULT (0),
        MaximumQuantity DECIMAL(18,3) NULL,
        CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_StockItem_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt       DATETIME2     NULL,
        IsActive        BIT           NOT NULL CONSTRAINT DF_StockItem_IsActive DEFAULT (1),
        CONSTRAINT PK_StockItem PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_StockItem_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_StockItem_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product (Id)
    );

    CREATE NONCLUSTERED INDEX IX_StockItem_ProductId ON dbo.StockItem (ProductId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_StockItem_BranchId_ProductId
        ON dbo.StockItem (BranchId, ProductId) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.Purchase', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Purchase
    (
        Id             BIGINT IDENTITY(1,1) NOT NULL,
        BranchId       BIGINT        NOT NULL,
        SupplierId     BIGINT        NOT NULL,
        DocumentNumber VARCHAR(50)   NULL,
        PurchasedAt    DATETIME2     NOT NULL,
        TotalAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchase_TotalAmount DEFAULT (0),
        Notes          NVARCHAR(500) NULL,
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_Purchase_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt      DATETIME2     NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_Purchase_IsActive DEFAULT (1),
        CONSTRAINT PK_Purchase PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Purchase_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_Purchase_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Supplier (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Purchase_BranchId ON dbo.Purchase (BranchId);
    CREATE NONCLUSTERED INDEX IX_Purchase_SupplierId ON dbo.Purchase (SupplierId);
END;
GO

IF OBJECT_ID('dbo.PurchaseItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseItem
    (
        Id         BIGINT IDENTITY(1,1) NOT NULL,
        PurchaseId BIGINT        NOT NULL,
        ProductId  BIGINT        NOT NULL,
        Quantity   DECIMAL(18,3) NOT NULL,
        UnitCost   DECIMAL(18,2) NOT NULL,
        TotalCost  DECIMAL(18,2) NOT NULL,
        CreatedAt  DATETIME2     NOT NULL CONSTRAINT DF_PurchaseItem_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt  DATETIME2     NULL,
        IsActive   BIT           NOT NULL CONSTRAINT DF_PurchaseItem_IsActive DEFAULT (1),
        CONSTRAINT PK_PurchaseItem PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_PurchaseItem_Purchase FOREIGN KEY (PurchaseId) REFERENCES dbo.Purchase (Id),
        CONSTRAINT FK_PurchaseItem_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product (Id),
        CONSTRAINT CK_PurchaseItem_Quantity CHECK (Quantity > 0)
    );

    CREATE NONCLUSTERED INDEX IX_PurchaseItem_PurchaseId ON dbo.PurchaseItem (PurchaseId);
    CREATE NONCLUSTERED INDEX IX_PurchaseItem_ProductId ON dbo.PurchaseItem (ProductId);
END;
GO

/* =====================================================================
   6. MESAS, COMANDAS E PEDIDOS
   ===================================================================== */

IF OBJECT_ID('dbo.DiningTable', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DiningTable
    (
        Id            BIGINT IDENTITY(1,1) NOT NULL,
        BranchId      BIGINT    NOT NULL,
        TableStatusId BIGINT    NOT NULL,
        Number        INT       NOT NULL,
        Capacity      INT       NULL,
        CreatedAt     DATETIME2 NOT NULL CONSTRAINT DF_DiningTable_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt     DATETIME2 NULL,
        IsActive      BIT       NOT NULL CONSTRAINT DF_DiningTable_IsActive DEFAULT (1),
        CONSTRAINT PK_DiningTable PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_DiningTable_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_DiningTable_TableStatus FOREIGN KEY (TableStatusId) REFERENCES dbo.TableStatus (Id)
    );

    CREATE NONCLUSTERED INDEX IX_DiningTable_TableStatusId ON dbo.DiningTable (TableStatusId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_DiningTable_BranchId_Number
        ON dbo.DiningTable (BranchId, Number) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.Comanda', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Comanda
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        BranchId        BIGINT      NOT NULL,
        ComandaStatusId BIGINT      NOT NULL,
        Code            VARCHAR(30) NOT NULL,
        CreatedAt       DATETIME2   NOT NULL CONSTRAINT DF_Comanda_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt       DATETIME2   NULL,
        IsActive        BIT         NOT NULL CONSTRAINT DF_Comanda_IsActive DEFAULT (1),
        CONSTRAINT PK_Comanda PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Comanda_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_Comanda_ComandaStatus FOREIGN KEY (ComandaStatusId) REFERENCES dbo.ComandaStatus (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Comanda_ComandaStatusId ON dbo.Comanda (ComandaStatusId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Comanda_BranchId_Code
        ON dbo.Comanda (BranchId, Code) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.CustomerOrder', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerOrder
    (
        Id               BIGINT IDENTITY(1,1) NOT NULL,
        BranchId         BIGINT        NOT NULL,
        DiningTableId    BIGINT        NULL,
        ComandaId        BIGINT        NULL,
        EmployeeId       BIGINT        NOT NULL,
        OrderStatusId    BIGINT        NOT NULL,
        GuestCount       INT           NULL,
        OpenedAt         DATETIME2     NOT NULL CONSTRAINT DF_CustomerOrder_OpenedAt DEFAULT SYSDATETIME(),
        ClosedAt         DATETIME2     NULL,
        SubtotalAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_CustomerOrder_SubtotalAmount DEFAULT (0),
        DiscountAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_CustomerOrder_DiscountAmount DEFAULT (0),
        ServiceFeeAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_CustomerOrder_ServiceFeeAmount DEFAULT (0),
        TotalAmount      DECIMAL(18,2) NOT NULL CONSTRAINT DF_CustomerOrder_TotalAmount DEFAULT (0),
        Notes            NVARCHAR(500) NULL,
        CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_CustomerOrder_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt        DATETIME2     NULL,
        IsActive         BIT           NOT NULL CONSTRAINT DF_CustomerOrder_IsActive DEFAULT (1),
        CONSTRAINT PK_CustomerOrder PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CustomerOrder_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_CustomerOrder_DiningTable FOREIGN KEY (DiningTableId) REFERENCES dbo.DiningTable (Id),
        CONSTRAINT FK_CustomerOrder_Comanda FOREIGN KEY (ComandaId) REFERENCES dbo.Comanda (Id),
        CONSTRAINT FK_CustomerOrder_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (Id),
        CONSTRAINT FK_CustomerOrder_OrderStatus FOREIGN KEY (OrderStatusId) REFERENCES dbo.OrderStatus (Id),
        CONSTRAINT CK_CustomerOrder_Origin CHECK (DiningTableId IS NOT NULL OR ComandaId IS NOT NULL)
    );

    CREATE NONCLUSTERED INDEX IX_CustomerOrder_BranchId ON dbo.CustomerOrder (BranchId);
    CREATE NONCLUSTERED INDEX IX_CustomerOrder_DiningTableId ON dbo.CustomerOrder (DiningTableId);
    CREATE NONCLUSTERED INDEX IX_CustomerOrder_ComandaId ON dbo.CustomerOrder (ComandaId);
    CREATE NONCLUSTERED INDEX IX_CustomerOrder_EmployeeId ON dbo.CustomerOrder (EmployeeId);
    CREATE NONCLUSTERED INDEX IX_CustomerOrder_OrderStatusId ON dbo.CustomerOrder (OrderStatusId);
    CREATE NONCLUSTERED INDEX IX_CustomerOrder_OpenedAt ON dbo.CustomerOrder (OpenedAt);
END;
GO

IF OBJECT_ID('dbo.OrderItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItem
    (
        Id                BIGINT IDENTITY(1,1) NOT NULL,
        CustomerOrderId   BIGINT        NOT NULL,
        ProductId         BIGINT        NOT NULL,
        OrderItemStatusId BIGINT        NOT NULL,
        EmployeeId        BIGINT        NULL,
        Quantity          DECIMAL(18,3) NOT NULL,
        UnitPrice         DECIMAL(18,2) NOT NULL,
        DiscountAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderItem_DiscountAmount DEFAULT (0),
        TotalAmount       DECIMAL(18,2) NOT NULL,
        Notes             NVARCHAR(300) NULL,
        SentToKitchenAt   DATETIME2     NULL,
        DeliveredAt       DATETIME2     NULL,
        CreatedAt         DATETIME2     NOT NULL CONSTRAINT DF_OrderItem_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt         DATETIME2     NULL,
        IsActive          BIT           NOT NULL CONSTRAINT DF_OrderItem_IsActive DEFAULT (1),
        CONSTRAINT PK_OrderItem PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OrderItem_CustomerOrder FOREIGN KEY (CustomerOrderId) REFERENCES dbo.CustomerOrder (Id),
        CONSTRAINT FK_OrderItem_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product (Id),
        CONSTRAINT FK_OrderItem_OrderItemStatus FOREIGN KEY (OrderItemStatusId) REFERENCES dbo.OrderItemStatus (Id),
        CONSTRAINT FK_OrderItem_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (Id),
        CONSTRAINT CK_OrderItem_Quantity CHECK (Quantity > 0)
    );

    CREATE NONCLUSTERED INDEX IX_OrderItem_CustomerOrderId ON dbo.OrderItem (CustomerOrderId);
    CREATE NONCLUSTERED INDEX IX_OrderItem_ProductId ON dbo.OrderItem (ProductId);
    CREATE NONCLUSTERED INDEX IX_OrderItem_OrderItemStatusId ON dbo.OrderItem (OrderItemStatusId);
    CREATE NONCLUSTERED INDEX IX_OrderItem_EmployeeId ON dbo.OrderItem (EmployeeId);
END;
GO

/* =====================================================================
   7. CAIXA
   ===================================================================== */

IF OBJECT_ID('dbo.CashRegister', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashRegister
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        BranchId  BIGINT        NOT NULL,
        Name      NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_CashRegister_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2     NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_CashRegister_IsActive DEFAULT (1),
        CONSTRAINT PK_CashRegister PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CashRegister_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id)
    );

    CREATE NONCLUSTERED INDEX IX_CashRegister_BranchId ON dbo.CashRegister (BranchId);
END;
GO

IF OBJECT_ID('dbo.CashSession', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashSession
    (
        Id                  BIGINT IDENTITY(1,1) NOT NULL,
        CashRegisterId      BIGINT        NOT NULL,
        CashSessionStatusId BIGINT        NOT NULL,
        OpenedByEmployeeId  BIGINT        NOT NULL,
        ClosedByEmployeeId  BIGINT        NULL,
        OpeningAmount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_CashSession_OpeningAmount DEFAULT (0),
        ClosingAmount       DECIMAL(18,2) NULL,
        ExpectedAmount      DECIMAL(18,2) NULL,
        DifferenceAmount    DECIMAL(18,2) NULL,
        OpenedAt            DATETIME2     NOT NULL CONSTRAINT DF_CashSession_OpenedAt DEFAULT SYSDATETIME(),
        ClosedAt            DATETIME2     NULL,
        CreatedAt           DATETIME2     NOT NULL CONSTRAINT DF_CashSession_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt           DATETIME2     NULL,
        IsActive            BIT           NOT NULL CONSTRAINT DF_CashSession_IsActive DEFAULT (1),
        CONSTRAINT PK_CashSession PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CashSession_CashRegister FOREIGN KEY (CashRegisterId) REFERENCES dbo.CashRegister (Id),
        CONSTRAINT FK_CashSession_CashSessionStatus FOREIGN KEY (CashSessionStatusId) REFERENCES dbo.CashSessionStatus (Id),
        CONSTRAINT FK_CashSession_OpenedByEmployee FOREIGN KEY (OpenedByEmployeeId) REFERENCES dbo.Employee (Id),
        CONSTRAINT FK_CashSession_ClosedByEmployee FOREIGN KEY (ClosedByEmployeeId) REFERENCES dbo.Employee (Id)
    );

    CREATE NONCLUSTERED INDEX IX_CashSession_CashRegisterId ON dbo.CashSession (CashRegisterId);
    CREATE NONCLUSTERED INDEX IX_CashSession_CashSessionStatusId ON dbo.CashSession (CashSessionStatusId);
    CREATE NONCLUSTERED INDEX IX_CashSession_OpenedByEmployeeId ON dbo.CashSession (OpenedByEmployeeId);
    CREATE NONCLUSTERED INDEX IX_CashSession_ClosedByEmployeeId ON dbo.CashSession (ClosedByEmployeeId);
END;
GO

/* =====================================================================
   8. FATURAMENTO (VENDAS E PAGAMENTOS)
   ===================================================================== */

IF OBJECT_ID('dbo.Sale', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sale
    (
        Id               BIGINT IDENTITY(1,1) NOT NULL,
        BranchId         BIGINT        NOT NULL,
        CustomerOrderId  BIGINT        NOT NULL,
        CashSessionId    BIGINT        NOT NULL,
        EmployeeId       BIGINT        NOT NULL,
        SaleNumber       BIGINT        NOT NULL,
        SubtotalAmount   DECIMAL(18,2) NOT NULL,
        DiscountAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sale_DiscountAmount DEFAULT (0),
        ServiceFeeAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sale_ServiceFeeAmount DEFAULT (0),
        TotalAmount      DECIMAL(18,2) NOT NULL,
        SoldAt           DATETIME2     NOT NULL CONSTRAINT DF_Sale_SoldAt DEFAULT SYSDATETIME(),
        CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_Sale_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt        DATETIME2     NULL,
        IsActive         BIT           NOT NULL CONSTRAINT DF_Sale_IsActive DEFAULT (1),
        CONSTRAINT PK_Sale PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Sale_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_Sale_CustomerOrder FOREIGN KEY (CustomerOrderId) REFERENCES dbo.CustomerOrder (Id),
        CONSTRAINT FK_Sale_CashSession FOREIGN KEY (CashSessionId) REFERENCES dbo.CashSession (Id),
        CONSTRAINT FK_Sale_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Sale_BranchId ON dbo.Sale (BranchId);
    CREATE NONCLUSTERED INDEX IX_Sale_CashSessionId ON dbo.Sale (CashSessionId);
    CREATE NONCLUSTERED INDEX IX_Sale_EmployeeId ON dbo.Sale (EmployeeId);
    CREATE NONCLUSTERED INDEX IX_Sale_SoldAt ON dbo.Sale (SoldAt);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Sale_CustomerOrderId ON dbo.Sale (CustomerOrderId) WHERE IsActive = 1;
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Sale_BranchId_SaleNumber ON dbo.Sale (BranchId, SaleNumber) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.SalePayment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalePayment
    (
        Id                BIGINT IDENTITY(1,1) NOT NULL,
        SaleId            BIGINT        NOT NULL,
        PaymentMethodId   BIGINT        NOT NULL,
        Amount            DECIMAL(18,2) NOT NULL,
        ChangeAmount      DECIMAL(18,2) NULL,
        AuthorizationCode VARCHAR(100)  NULL,
        CreatedAt         DATETIME2     NOT NULL CONSTRAINT DF_SalePayment_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt         DATETIME2     NULL,
        IsActive          BIT           NOT NULL CONSTRAINT DF_SalePayment_IsActive DEFAULT (1),
        CONSTRAINT PK_SalePayment PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SalePayment_Sale FOREIGN KEY (SaleId) REFERENCES dbo.Sale (Id),
        CONSTRAINT FK_SalePayment_PaymentMethod FOREIGN KEY (PaymentMethodId) REFERENCES dbo.PaymentMethod (Id),
        CONSTRAINT CK_SalePayment_Amount CHECK (Amount > 0)
    );

    CREATE NONCLUSTERED INDEX IX_SalePayment_SaleId ON dbo.SalePayment (SaleId);
    CREATE NONCLUSTERED INDEX IX_SalePayment_PaymentMethodId ON dbo.SalePayment (PaymentMethodId);
END;
GO

IF OBJECT_ID('dbo.CashMovement', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashMovement
    (
        Id                 BIGINT IDENTITY(1,1) NOT NULL,
        CashSessionId      BIGINT        NOT NULL,
        CashMovementTypeId BIGINT        NOT NULL,
        SaleId             BIGINT        NULL,
        EmployeeId         BIGINT        NOT NULL,
        Amount             DECIMAL(18,2) NOT NULL,
        Description        NVARCHAR(300) NULL,
        CreatedAt          DATETIME2     NOT NULL CONSTRAINT DF_CashMovement_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt          DATETIME2     NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_CashMovement_IsActive DEFAULT (1),
        CONSTRAINT PK_CashMovement PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CashMovement_CashSession FOREIGN KEY (CashSessionId) REFERENCES dbo.CashSession (Id),
        CONSTRAINT FK_CashMovement_CashMovementType FOREIGN KEY (CashMovementTypeId) REFERENCES dbo.CashMovementType (Id),
        CONSTRAINT FK_CashMovement_Sale FOREIGN KEY (SaleId) REFERENCES dbo.Sale (Id),
        CONSTRAINT FK_CashMovement_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (Id),
        CONSTRAINT CK_CashMovement_Amount CHECK (Amount > 0)
    );

    CREATE NONCLUSTERED INDEX IX_CashMovement_CashSessionId ON dbo.CashMovement (CashSessionId);
    CREATE NONCLUSTERED INDEX IX_CashMovement_CashMovementTypeId ON dbo.CashMovement (CashMovementTypeId);
    CREATE NONCLUSTERED INDEX IX_CashMovement_SaleId ON dbo.CashMovement (SaleId);
    CREATE NONCLUSTERED INDEX IX_CashMovement_EmployeeId ON dbo.CashMovement (EmployeeId);
END;
GO

/* =====================================================================
   9. CONTROLE DE ENTRADA E SAÍDA DE ESTOQUE
   ===================================================================== */

IF OBJECT_ID('dbo.StockMovement', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovement
    (
        Id                  BIGINT IDENTITY(1,1) NOT NULL,
        StockItemId         BIGINT        NOT NULL,
        StockMovementTypeId BIGINT        NOT NULL,
        PurchaseItemId      BIGINT        NULL,
        OrderItemId         BIGINT        NULL,
        EmployeeId          BIGINT        NULL,
        Quantity            DECIMAL(18,3) NOT NULL,
        UnitCost            DECIMAL(18,2) NULL,
        TotalCost           DECIMAL(18,2) NULL,
        DocumentNumber      VARCHAR(50)   NULL,
        MovedAt             DATETIME2     NOT NULL CONSTRAINT DF_StockMovement_MovedAt DEFAULT SYSDATETIME(),
        Notes               NVARCHAR(300) NULL,
        CreatedAt           DATETIME2     NOT NULL CONSTRAINT DF_StockMovement_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt           DATETIME2     NULL,
        IsActive            BIT           NOT NULL CONSTRAINT DF_StockMovement_IsActive DEFAULT (1),
        CONSTRAINT PK_StockMovement PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_StockMovement_StockItem FOREIGN KEY (StockItemId) REFERENCES dbo.StockItem (Id),
        CONSTRAINT FK_StockMovement_StockMovementType FOREIGN KEY (StockMovementTypeId) REFERENCES dbo.StockMovementType (Id),
        CONSTRAINT FK_StockMovement_PurchaseItem FOREIGN KEY (PurchaseItemId) REFERENCES dbo.PurchaseItem (Id),
        CONSTRAINT FK_StockMovement_OrderItem FOREIGN KEY (OrderItemId) REFERENCES dbo.OrderItem (Id),
        CONSTRAINT FK_StockMovement_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (Id),
        CONSTRAINT CK_StockMovement_Quantity CHECK (Quantity > 0)
    );

    CREATE NONCLUSTERED INDEX IX_StockMovement_StockItemId ON dbo.StockMovement (StockItemId);
    CREATE NONCLUSTERED INDEX IX_StockMovement_StockMovementTypeId ON dbo.StockMovement (StockMovementTypeId);
    CREATE NONCLUSTERED INDEX IX_StockMovement_PurchaseItemId ON dbo.StockMovement (PurchaseItemId);
    CREATE NONCLUSTERED INDEX IX_StockMovement_OrderItemId ON dbo.StockMovement (OrderItemId);
    CREATE NONCLUSTERED INDEX IX_StockMovement_EmployeeId ON dbo.StockMovement (EmployeeId);
    CREATE NONCLUSTERED INDEX IX_StockMovement_MovedAt ON dbo.StockMovement (MovedAt);
END;
GO

/* ===================== FIM DO SCRIPT DDL ===================== */
