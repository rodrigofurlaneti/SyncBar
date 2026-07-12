/* =====================================================================
   ============  SYNCBAR - BarRestauranteDb - SCRIPT COMPLETO  =========
   =====================================================================
   Criacao do zero com TODOS os modulos, na ordem de dependencias:
     1. Estrutura (banco + tabelas + indices + constraints)
     2. Seed operacional (lookups, permissoes, empresa/filial exemplo,
        cargos, 10 mesas, produtos exemplo, caixa)
     3. Usuario admin (login: admin | senha: SyncBar@2026 - TROQUE!)
     4. Controle de acesso por tela (cargo + pessoa)
     5. Faturamento (custos fixos/variaveis e meta mensal)
     6. Painel de preparo (KDS)
     7. Comandas individuais 00 a 99
     8/9. Promocoes (em dobro e desconto %)
     10. Impressao termica (ELGIN i9 USB / rede TCP 9100)
     11. Pagamento parcial em mesas

   IDEMPOTENTE: pode ser executado mais de uma vez sem duplicar nada.
   Requisito: SQL Server 2016+ (DATETIME2, indices filtrados).
   ===================================================================== */



/* =====================================================================
   >>> 1. ESTRUTURA — banco + 39 tabelas base
   >>> (origem: BarRestaurante_DDL.sql)
   ===================================================================== */

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
GO


/* =====================================================================
   >>> 2. SEED — lookups, permissoes, empresa, mesas, produtos
   >>> (origem: BarRestaurante_Seed.sql)
   ===================================================================== */

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
GO


/* =====================================================================
   >>> 3. ADMIN — senha do usuario admin (SyncBar@2026)
   >>> (origem: BarRestaurante_UpdateAdminPassword.sql)
   ===================================================================== */

/* =====================================================================
   BarRestauranteDb — Define a senha do usuário admin
   Usuário : admin
   Senha   : SyncBar@2026
   Hash    : BCrypt (cost 12) — verificado via BCrypt.Net.BCrypt.Verify
   Executar após BarRestaurante_Seed.sql. Troque a senha em produção.
   ===================================================================== */

USE BarRestauranteDb;
GO

UPDATE dbo.AppUser
SET PasswordHash = '$2b$12$U.CicaJlVoALtY76qurt5.Wkpva9W7uKnIxBgHkrxVnjaFoYLRtSu',
    UpdatedAt = SYSDATETIME()
WHERE UserName = 'admin' AND IsActive = 1;
GO
GO


/* =====================================================================
   >>> 4. ACESSOS — telas por cargo/pessoa
   >>> (origem: BarRestaurante_FeatureAccess.sql)
   ===================================================================== */

/* =====================================================================
   BarRestauranteDb - Acesso por funcionalidade (telas)
   AppFeature: catalogo de telas do sistema
   JobTitleFeature: telas liberadas por CARGO
   AppUserFeature: telas extras liberadas por PESSOA (uniao com o cargo)
   Gerente e Administrador enxergam tudo (bypass na aplicacao).
   Idempotente - pode executar mais de uma vez.
   ===================================================================== */

USE BarRestauranteDb;
GO

/* ============================ Tabelas ============================ */

IF OBJECT_ID('dbo.AppFeature', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppFeature
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Code      VARCHAR(50)   NOT NULL,
        Name      NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_AppFeature_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2     NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_AppFeature_IsActive DEFAULT (1),
        CONSTRAINT PK_AppFeature PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_AppFeature_Code ON dbo.AppFeature (Code) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.JobTitleFeature', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobTitleFeature
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        JobTitleId   BIGINT    NOT NULL,
        AppFeatureId BIGINT    NOT NULL,
        CreatedAt    DATETIME2 NOT NULL CONSTRAINT DF_JobTitleFeature_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt    DATETIME2 NULL,
        IsActive     BIT       NOT NULL CONSTRAINT DF_JobTitleFeature_IsActive DEFAULT (1),
        CONSTRAINT PK_JobTitleFeature PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_JobTitleFeature_JobTitle FOREIGN KEY (JobTitleId) REFERENCES dbo.JobTitle (Id),
        CONSTRAINT FK_JobTitleFeature_AppFeature FOREIGN KEY (AppFeatureId) REFERENCES dbo.AppFeature (Id)
    );

    CREATE NONCLUSTERED INDEX IX_JobTitleFeature_JobTitleId ON dbo.JobTitleFeature (JobTitleId);
    CREATE NONCLUSTERED INDEX IX_JobTitleFeature_AppFeatureId ON dbo.JobTitleFeature (AppFeatureId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_JobTitleFeature_JobTitleId_AppFeatureId
        ON dbo.JobTitleFeature (JobTitleId, AppFeatureId) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID('dbo.AppUserFeature', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUserFeature
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        AppUserId    BIGINT    NOT NULL,
        AppFeatureId BIGINT    NOT NULL,
        CreatedAt    DATETIME2 NOT NULL CONSTRAINT DF_AppUserFeature_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt    DATETIME2 NULL,
        IsActive     BIT       NOT NULL CONSTRAINT DF_AppUserFeature_IsActive DEFAULT (1),
        CONSTRAINT PK_AppUserFeature PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AppUserFeature_AppUser FOREIGN KEY (AppUserId) REFERENCES dbo.AppUser (Id),
        CONSTRAINT FK_AppUserFeature_AppFeature FOREIGN KEY (AppFeatureId) REFERENCES dbo.AppFeature (Id)
    );

    CREATE NONCLUSTERED INDEX IX_AppUserFeature_AppUserId ON dbo.AppUserFeature (AppUserId);
    CREATE NONCLUSTERED INDEX IX_AppUserFeature_AppFeatureId ON dbo.AppUserFeature (AppFeatureId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AppUserFeature_AppUserId_AppFeatureId
        ON dbo.AppUserFeature (AppUserId, AppFeatureId) WHERE IsActive = 1;
END;
GO

/* ============================ Seed: telas ============================ */

IF NOT EXISTS (SELECT 1 FROM dbo.AppFeature)
BEGIN
    DBCC CHECKIDENT ('dbo.AppFeature', RESEED, 0);
    SET IDENTITY_INSERT dbo.AppFeature ON;

    INSERT INTO dbo.AppFeature (Id, Code, Name) VALUES
        (1, 'Salao',    N'Salao (mesas e pedidos)'),
        (2, 'Cardapio', N'Cardapio (produtos)'),
        (3, 'Estoque',  N'Estoque'),
        (4, 'Equipe',   N'Equipe (funcionarios)'),
        (5, 'Usuarios', N'Usuarios e perfis'),
        (6, 'Caixa',    N'Caixa (sessao e pagamentos)');

    BEGIN TRY SET IDENTITY_INSERT dbo.AppFeature OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ Seed: grants padrao por cargo ============================ */

INSERT INTO dbo.JobTitleFeature (JobTitleId, AppFeatureId)
SELECT J.Id, F.Id
FROM (VALUES
    (N'Garçom',            'Salao'),
    (N'Garçom',            'Cardapio'),
    (N'Operador de Caixa',  'Salao'),
    (N'Operador de Caixa',  'Caixa'),
    (N'Cozinheiro',         'Salao'),
    (N'Barman',             'Salao'),
    (N'Barman',             'Cardapio'),
    (N'Estoquista',         'Estoque'),
    (N'Estoquista',         'Cardapio')) AS G (JobTitleName, FeatureCode)
JOIN dbo.JobTitle   AS J ON J.Name = G.JobTitleName AND J.IsActive = 1
JOIN dbo.AppFeature AS F ON F.Code = G.FeatureCode  AND F.IsActive = 1
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.JobTitleFeature X
    WHERE X.JobTitleId = J.Id AND X.AppFeatureId = F.Id AND X.IsActive = 1);
GO

/* ============================ Conferencia ============================ */

SELECT J.Name AS Cargo, F.Name AS Tela
FROM dbo.JobTitleFeature JF
JOIN dbo.JobTitle J ON J.Id = JF.JobTitleId
JOIN dbo.AppFeature F ON F.Id = JF.AppFeatureId
WHERE JF.IsActive = 1
ORDER BY J.Name, F.Id;
GO
GO


/* =====================================================================
   >>> 5. FATURAMENTO — custos e metas
   >>> (origem: BarRestaurante_Faturamento.sql)
   ===================================================================== */

/* =====================================================================
   BarRestauranteDb - Modulo Faturamento
   CostType: tipo de custo (Fixo / Variavel)
   OperatingCost: custos lancados por filial e mes de referencia
   RevenueTarget: meta de faturamento por filial e mes
   + nova tela 'Faturamento' no controle de acessos
   Idempotente - pode executar mais de uma vez.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.CostType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CostType
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_CostType_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_CostType_IsActive DEFAULT (1),
        CONSTRAINT PK_CostType PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID('dbo.OperatingCost', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperatingCost
    (
        Id             BIGINT IDENTITY(1,1) NOT NULL,
        BranchId       BIGINT        NOT NULL,
        CostTypeId     BIGINT        NOT NULL,
        Description    NVARCHAR(200) NOT NULL,
        Amount         DECIMAL(18,2) NOT NULL,
        ReferenceYear  INT           NOT NULL,
        ReferenceMonth INT           NOT NULL,
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_OperatingCost_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt      DATETIME2     NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_OperatingCost_IsActive DEFAULT (1),
        CONSTRAINT PK_OperatingCost PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OperatingCost_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_OperatingCost_CostType FOREIGN KEY (CostTypeId) REFERENCES dbo.CostType (Id),
        CONSTRAINT CK_OperatingCost_Amount CHECK (Amount > 0),
        CONSTRAINT CK_OperatingCost_ReferenceMonth CHECK (ReferenceMonth BETWEEN 1 AND 12),
        CONSTRAINT CK_OperatingCost_ReferenceYear CHECK (ReferenceYear BETWEEN 2000 AND 2100)
    );

    CREATE NONCLUSTERED INDEX IX_OperatingCost_BranchId ON dbo.OperatingCost (BranchId);
    CREATE NONCLUSTERED INDEX IX_OperatingCost_CostTypeId ON dbo.OperatingCost (CostTypeId);
    CREATE NONCLUSTERED INDEX IX_OperatingCost_BranchId_ReferenceYear_ReferenceMonth
        ON dbo.OperatingCost (BranchId, ReferenceYear, ReferenceMonth);
END;
GO

IF OBJECT_ID('dbo.RevenueTarget', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RevenueTarget
    (
        Id             BIGINT IDENTITY(1,1) NOT NULL,
        BranchId       BIGINT        NOT NULL,
        ReferenceYear  INT           NOT NULL,
        ReferenceMonth INT           NOT NULL,
        TargetAmount   DECIMAL(18,2) NOT NULL,
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_RevenueTarget_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt      DATETIME2     NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_RevenueTarget_IsActive DEFAULT (1),
        CONSTRAINT PK_RevenueTarget PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RevenueTarget_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT CK_RevenueTarget_TargetAmount CHECK (TargetAmount > 0),
        CONSTRAINT CK_RevenueTarget_ReferenceMonth CHECK (ReferenceMonth BETWEEN 1 AND 12),
        CONSTRAINT CK_RevenueTarget_ReferenceYear CHECK (ReferenceYear BETWEEN 2000 AND 2100)
    );

    CREATE NONCLUSTERED INDEX IX_RevenueTarget_BranchId ON dbo.RevenueTarget (BranchId);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_RevenueTarget_BranchId_ReferenceYear_ReferenceMonth
        ON dbo.RevenueTarget (BranchId, ReferenceYear, ReferenceMonth) WHERE IsActive = 1;
END;
GO

/* ============================ Seed: tipos de custo ============================ */

IF NOT EXISTS (SELECT 1 FROM dbo.CostType)
BEGIN
    DBCC CHECKIDENT ('dbo.CostType', RESEED, 0);
    SET IDENTITY_INSERT dbo.CostType ON;

    INSERT INTO dbo.CostType (Id, Name) VALUES
        (1, N'Fixo'),
        (2, N'Variavel');

    BEGIN TRY SET IDENTITY_INSERT dbo.CostType OFF END TRY BEGIN CATCH END CATCH;
END;
GO

/* ============================ Nova tela: Faturamento ============================ */

INSERT INTO dbo.AppFeature (Code, Name)
SELECT 'Faturamento', N'Faturamento (custos e metas)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppFeature WHERE Code = 'Faturamento' AND IsActive = 1);
GO

SELECT Id, Code, Name FROM dbo.AppFeature WHERE IsActive = 1 ORDER BY Id;
GO
GO


/* =====================================================================
   >>> 6. PREPARO — painel cozinha/bar
   >>> (origem: BarRestaurante_Preparo.sql)
   ===================================================================== */

/* =====================================================================
   BarRestauranteDb - Tela 'Preparo' (painel de cozinha/bar - KDS)
   Cria a feature e concede por padrao a Cozinheiro e Barman.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

INSERT INTO dbo.AppFeature (Code, Name)
SELECT 'Preparo', N'Preparo (painel cozinha/bar)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppFeature WHERE Code = 'Preparo' AND IsActive = 1);
GO

INSERT INTO dbo.JobTitleFeature (JobTitleId, AppFeatureId)
SELECT J.Id, F.Id
FROM (VALUES (N'Cozinheiro'), (N'Barman')) AS G (JobTitleName)
JOIN dbo.JobTitle   AS J ON J.Name = G.JobTitleName AND J.IsActive = 1
JOIN dbo.AppFeature AS F ON F.Code = 'Preparo' AND F.IsActive = 1
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.JobTitleFeature X
    WHERE X.JobTitleId = J.Id AND X.AppFeatureId = F.Id AND X.IsActive = 1);
GO

SELECT J.Name AS Cargo, F.Name AS Tela
FROM dbo.JobTitleFeature JF
JOIN dbo.JobTitle J ON J.Id = JF.JobTitleId
JOIN dbo.AppFeature F ON F.Id = JF.AppFeatureId
WHERE JF.IsActive = 1
ORDER BY J.Name, F.Id;
GO
GO


/* =====================================================================
   >>> 7. COMANDAS — cartoes individuais 00 a 99
   >>> (origem: BarRestaurante_Comandas00a99.sql)
   ===================================================================== */

/* =====================================================================
   BarRestauranteDb - Comandas individuais 00 a 99
   Desativa cartoes antigos que estejam DISPONIVEIS (comandas em uso ou
   com historico permanecem) e cria os codigos '00' a '99'.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);

/* Desativa cartoes fora do padrao 00-99 que nao estejam em uso */
UPDATE dbo.Comanda
SET IsActive = 0, UpdatedAt = SYSDATETIME()
WHERE BranchId = @BranchId
  AND IsActive = 1
  AND ComandaStatusId = 1 -- Disponivel
  AND (LEN(Code) <> 2 OR Code NOT LIKE '[0-9][0-9]');

/* Cria 00..99 onde nao existir */
;WITH Numbers AS (
    SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS N
    FROM sys.objects a CROSS JOIN sys.objects b
)
INSERT INTO dbo.Comanda (BranchId, ComandaStatusId, Code)
SELECT @BranchId, 1, RIGHT('0' + CONVERT(VARCHAR(2), N), 2)
FROM Numbers
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Comanda X
    WHERE X.BranchId = @BranchId
      AND X.Code = RIGHT('0' + CONVERT(VARCHAR(2), Numbers.N), 2)
      AND X.IsActive = 1);

SELECT COUNT(*) AS ComandasAtivas FROM dbo.Comanda WHERE BranchId = @BranchId AND IsActive = 1;
GO
GO


/* =====================================================================
   >>> 8. PROMOCOES — em dobro por dia/horario
   >>> (origem: BarRestaurante_Promocoes.sql)
   ===================================================================== */

/* =====================================================================
   BarRestauranteDb - Modulo Promocoes (em dobro por dia/horario)
   Promotion: produto em promocao num dia da semana e janela de horario.
   DayOfWeek: 0=Domingo ... 6=Sabado (padrao .NET).
   Minutos do dia: 960 = 16:00 | 1080 = 18:00 | 1200 = 20:00.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.Promotion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Promotion
    (
        Id               BIGINT IDENTITY(1,1) NOT NULL,
        BranchId         BIGINT        NOT NULL,
        ProductId        BIGINT        NOT NULL,
        Name             NVARCHAR(150) NOT NULL,
        DayOfWeek        INT           NOT NULL,
        StartMinuteOfDay INT           NOT NULL,
        EndMinuteOfDay   INT           NOT NULL,
        CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_Promotion_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt        DATETIME2     NULL,
        IsActive         BIT           NOT NULL CONSTRAINT DF_Promotion_IsActive DEFAULT (1),
        CONSTRAINT PK_Promotion PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Promotion_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branch (Id),
        CONSTRAINT FK_Promotion_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product (Id),
        CONSTRAINT CK_Promotion_DayOfWeek CHECK (DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT CK_Promotion_StartMinute CHECK (StartMinuteOfDay BETWEEN 0 AND 1439),
        CONSTRAINT CK_Promotion_EndMinute CHECK (EndMinuteOfDay BETWEEN 1 AND 1440),
        CONSTRAINT CK_Promotion_Window CHECK (StartMinuteOfDay < EndMinuteOfDay)
    );

    CREATE NONCLUSTERED INDEX IX_Promotion_BranchId ON dbo.Promotion (BranchId);
    CREATE NONCLUSTERED INDEX IX_Promotion_ProductId ON dbo.Promotion (ProductId);
END;
GO

/* ============================ Tela Promocoes ============================ */

INSERT INTO dbo.AppFeature (Code, Name)
SELECT 'Promocoes', N'Promocoes (em dobro)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppFeature WHERE Code = 'Promocoes' AND IsActive = 1);
GO

/* ============================ Seed: semana exemplo ============================ */

DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);

INSERT INTO dbo.Promotion (BranchId, ProductId, Name, DayOfWeek, StartMinuteOfDay, EndMinuteOfDay)
SELECT @BranchId, P.Id, S.PromoName, S.Day, S.StartMin, S.EndMin
FROM (VALUES
    (N'Caipirinha de Limão',       N'Quarta da caipirinha em dobro', 3,  960, 1200),
    (N'Cerveja Pilsen 600ml',      N'Quinta da cerveja em dobro',    4,  960, 1200),
    (N'Cerveja Lata 350ml',        N'Quinta da cerveja em dobro',    4,  960, 1200),
    (N'Porção de Batata Frita',    N'Sexta da batata em dobro',      5, 1080, 1200),
    (N'Caipirinha de Limão',       N'Sábado da caipirinha em dobro', 6,  960, 1200)
) AS S (ProductName, PromoName, Day, StartMin, EndMin)
JOIN dbo.Product AS P ON P.Name = S.ProductName AND P.IsActive = 1
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Promotion X
    WHERE X.BranchId = @BranchId AND X.ProductId = P.Id
      AND X.DayOfWeek = S.Day AND X.IsActive = 1);

SELECT P.Name AS Produto, PR.Name AS Promocao, PR.DayOfWeek,
       PR.StartMinuteOfDay / 60 AS HoraInicio, PR.EndMinuteOfDay / 60 AS HoraFim
FROM dbo.Promotion PR JOIN dbo.Product P ON P.Id = PR.ProductId
WHERE PR.IsActive = 1 ORDER BY PR.DayOfWeek, P.Name;
GO
GO


/* =====================================================================
   >>> 9. PROMOCOES — tipo desconto percentual
   >>> (origem: BarRestaurante_PromocaoDesconto.sql)
   ===================================================================== */

/* =====================================================================
   BarRestauranteDb - Promocoes: tipo Desconto percentual
   PromotionType: 1 = EmDobro | 2 = Desconto
   DiscountRate: fracao (0.25 = 25%) obrigatoria no tipo Desconto.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.PromotionType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PromotionType
    (
        Id        BIGINT IDENTITY(1,1) NOT NULL,
        Name      NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2    NOT NULL CONSTRAINT DF_PromotionType_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2    NULL,
        IsActive  BIT          NOT NULL CONSTRAINT DF_PromotionType_IsActive DEFAULT (1),
        CONSTRAINT PK_PromotionType PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PromotionType)
BEGIN
    DBCC CHECKIDENT ('dbo.PromotionType', RESEED, 0);
    SET IDENTITY_INSERT dbo.PromotionType ON;

    INSERT INTO dbo.PromotionType (Id, Name) VALUES
        (1, N'Em dobro'),
        (2, N'Desconto');

    BEGIN TRY SET IDENTITY_INSERT dbo.PromotionType OFF END TRY BEGIN CATCH END CATCH;
END;
GO

IF COL_LENGTH('dbo.Promotion', 'PromotionTypeId') IS NULL
BEGIN
    ALTER TABLE dbo.Promotion ADD
        PromotionTypeId BIGINT NOT NULL CONSTRAINT DF_Promotion_PromotionTypeId DEFAULT (1),
        DiscountRate DECIMAL(5,4) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Promotion_PromotionType')
BEGIN
    ALTER TABLE dbo.Promotion ADD
        CONSTRAINT FK_Promotion_PromotionType FOREIGN KEY (PromotionTypeId) REFERENCES dbo.PromotionType (Id),
        CONSTRAINT CK_Promotion_DiscountRate CHECK (
            PromotionTypeId <> 2 OR (DiscountRate IS NOT NULL AND DiscountRate > 0 AND DiscountRate < 1));

    CREATE NONCLUSTERED INDEX IX_Promotion_PromotionTypeId ON dbo.Promotion (PromotionTypeId);
END;
GO

/* ============================ Seed: domingo 25% na chapa mista ============================ */

DECLARE @BranchId BIGINT = (SELECT TOP 1 Id FROM dbo.Branch WHERE IsActive = 1);

INSERT INTO dbo.Promotion (BranchId, ProductId, Name, DayOfWeek, StartMinuteOfDay, EndMinuteOfDay, PromotionTypeId, DiscountRate)
SELECT @BranchId, P.Id, N'Domingo da chapa mista -25%', 0, 0, 1440, 2, 0.25
FROM dbo.Product AS P
WHERE P.Name LIKE N'%Chapa Mista%' AND P.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.Promotion X
      WHERE X.BranchId = @BranchId AND X.ProductId = P.Id AND X.DayOfWeek = 0 AND X.IsActive = 1);

IF @@ROWCOUNT = 0
    PRINT 'Produto com "Chapa Mista" no nome nao encontrado — cadastre no Cardapio e crie a promocao pela tela Promocoes.';

SELECT PR.Name, PT.Name AS Tipo, PR.DiscountRate, PR.DayOfWeek
FROM dbo.Promotion PR JOIN dbo.PromotionType PT ON PT.Id = PR.PromotionTypeId
WHERE PR.IsActive = 1 ORDER BY PR.DayOfWeek;
GO
GO


/* =====================================================================
   >>> 10. IMPRESSAO — impressoras e interruptores
   >>> (origem: BarRestaurante_Impressao.sql)
   ===================================================================== */

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
GO


/* =====================================================================
   >>> 11. PAGAMENTO PARCIAL — mesas
   >>> (origem: BarRestaurante_PagamentoParcial.sql)
   ===================================================================== */

/* =====================================================================
   BarRestauranteDb - Pagamento parcial (somente contas de MESA)
   OrderPartialPayment: cliente paga parte da conta e vai embora;
   a mesa continua aberta e o valor entra no caixa da sessao.
   Idempotente.
   ===================================================================== */

USE BarRestauranteDb;
GO

IF OBJECT_ID('dbo.OrderPartialPayment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderPartialPayment
    (
        Id                BIGINT IDENTITY(1,1) NOT NULL,
        CustomerOrderId   BIGINT        NOT NULL,
        CashSessionId     BIGINT        NOT NULL,
        PaymentMethodId   BIGINT        NOT NULL,
        EmployeeId        BIGINT        NOT NULL,
        Amount            DECIMAL(18,2) NOT NULL,
        AuthorizationCode VARCHAR(100)  NULL,
        PayerName         NVARCHAR(100) NULL,
        CreatedAt         DATETIME2     NOT NULL CONSTRAINT DF_OrderPartialPayment_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt         DATETIME2     NULL,
        IsActive          BIT           NOT NULL CONSTRAINT DF_OrderPartialPayment_IsActive DEFAULT (1),
        CONSTRAINT PK_OrderPartialPayment PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OrderPartialPayment_CustomerOrder FOREIGN KEY (CustomerOrderId) REFERENCES dbo.CustomerOrder (Id),
        CONSTRAINT FK_OrderPartialPayment_CashSession FOREIGN KEY (CashSessionId) REFERENCES dbo.CashSession (Id),
        CONSTRAINT FK_OrderPartialPayment_PaymentMethod FOREIGN KEY (PaymentMethodId) REFERENCES dbo.PaymentMethod (Id),
        CONSTRAINT FK_OrderPartialPayment_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (Id),
        CONSTRAINT CK_OrderPartialPayment_Amount CHECK (Amount > 0)
    );

    CREATE NONCLUSTERED INDEX IX_OrderPartialPayment_CustomerOrderId ON dbo.OrderPartialPayment (CustomerOrderId);
    CREATE NONCLUSTERED INDEX IX_OrderPartialPayment_CashSessionId ON dbo.OrderPartialPayment (CashSessionId);
    CREATE NONCLUSTERED INDEX IX_OrderPartialPayment_PaymentMethodId ON dbo.OrderPartialPayment (PaymentMethodId);
    CREATE NONCLUSTERED INDEX IX_OrderPartialPayment_EmployeeId ON dbo.OrderPartialPayment (EmployeeId);
END;
GO
GO
