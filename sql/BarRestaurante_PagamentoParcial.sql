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
