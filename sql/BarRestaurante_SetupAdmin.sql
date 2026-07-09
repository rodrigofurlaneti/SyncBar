/* =====================================================================
   BarRestauranteDb — Setup do usuário admin (idempotente)
   Cria a cadeia mínima se não existir (Company → Branch → JobTitle →
   Employee → Role → AppUser → UserRole) e define a senha do admin.
   Usuário: admin | Senha: SyncBar@2026
   Pode ser executado quantas vezes quiser.
   ===================================================================== */

USE BarRestauranteDb;
GO

DECLARE @CompanyId  BIGINT,
        @BranchId   BIGINT,
        @JobTitleId BIGINT,
        @EmployeeId BIGINT,
        @RoleId     BIGINT,
        @AppUserId  BIGINT;

/* ---------- Company ---------- */
SELECT @CompanyId = Id FROM dbo.Company WHERE Cnpj = '12345678000199' AND IsActive = 1;
IF @CompanyId IS NULL
BEGIN
    INSERT INTO dbo.Company (LegalName, TradeName, Cnpj, Email, Phone)
    VALUES (N'Bar e Restaurante Exemplo LTDA', N'Restaurante Exemplo', '12345678000199', 'contato@exemplo.com.br', '11999990000');
    SET @CompanyId = SCOPE_IDENTITY();
    PRINT 'Company criada: Id = ' + CONVERT(VARCHAR(20), @CompanyId);
END
ELSE PRINT 'Company OK: Id = ' + CONVERT(VARCHAR(20), @CompanyId);

/* ---------- Branch ---------- */
SELECT @BranchId = Id FROM dbo.Branch WHERE CompanyId = @CompanyId AND IsActive = 1;
IF @BranchId IS NULL
BEGIN
    INSERT INTO dbo.Branch (CompanyId, Name, Cnpj, Phone, AddressStreet, AddressNumber, AddressDistrict, AddressCity, AddressState, AddressZipCode)
    VALUES (@CompanyId, N'Matriz — Centro', '12345678000199', '11999990000', N'Rua Exemplo', '100', N'Centro', N'São Paulo', 'SP', '01001000');
    SET @BranchId = SCOPE_IDENTITY();
    PRINT 'Branch criada: Id = ' + CONVERT(VARCHAR(20), @BranchId);
END
ELSE PRINT 'Branch OK: Id = ' + CONVERT(VARCHAR(20), @BranchId);

/* ---------- JobTitle ---------- */
SELECT @JobTitleId = Id FROM dbo.JobTitle WHERE CompanyId = @CompanyId AND Name = N'Gerente' AND IsActive = 1;
IF @JobTitleId IS NULL
BEGIN
    INSERT INTO dbo.JobTitle (CompanyId, Name) VALUES (@CompanyId, N'Gerente');
    SET @JobTitleId = SCOPE_IDENTITY();
    PRINT 'JobTitle criado: Id = ' + CONVERT(VARCHAR(20), @JobTitleId);
END
ELSE PRINT 'JobTitle OK: Id = ' + CONVERT(VARCHAR(20), @JobTitleId);

/* ---------- Employee ---------- */
SELECT @EmployeeId = Id FROM dbo.Employee WHERE Cpf = '00000000000' AND IsActive = 1;
IF @EmployeeId IS NULL
BEGIN
    INSERT INTO dbo.Employee (BranchId, JobTitleId, Name, Cpf, Email, Phone, HiredAt, Salary)
    VALUES (@BranchId, @JobTitleId, N'Administrador do Sistema', '00000000000', 'admin@exemplo.com.br', '11999990001',
            CONVERT(DATETIME2, '2026-01-05T08:00:00', 126), 8000.00);
    SET @EmployeeId = SCOPE_IDENTITY();
    PRINT 'Employee criado: Id = ' + CONVERT(VARCHAR(20), @EmployeeId);
END
ELSE PRINT 'Employee OK: Id = ' + CONVERT(VARCHAR(20), @EmployeeId);

/* ---------- Role ---------- */
SELECT @RoleId = Id FROM dbo.Role WHERE CompanyId = @CompanyId AND Name = N'Administrador' AND IsActive = 1;
IF @RoleId IS NULL
BEGIN
    INSERT INTO dbo.Role (CompanyId, Name, Description) VALUES (@CompanyId, N'Administrador', N'Acesso total ao sistema');
    SET @RoleId = SCOPE_IDENTITY();
    PRINT 'Role criada: Id = ' + CONVERT(VARCHAR(20), @RoleId);
END
ELSE PRINT 'Role OK: Id = ' + CONVERT(VARCHAR(20), @RoleId);

/* ---------- AppUser (senha: SyncBar@2026, BCrypt cost 12) ---------- */
SELECT @AppUserId = Id FROM dbo.AppUser WHERE UserName = 'admin' AND IsActive = 1;
IF @AppUserId IS NULL
BEGIN
    INSERT INTO dbo.AppUser (CompanyId, EmployeeId, UserName, Email, PasswordHash)
    VALUES (@CompanyId, @EmployeeId, 'admin', 'admin@exemplo.com.br',
            '$2b$12$U.CicaJlVoALtY76qurt5.Wkpva9W7uKnIxBgHkrxVnjaFoYLRtSu');
    SET @AppUserId = SCOPE_IDENTITY();
    PRINT 'AppUser criado: Id = ' + CONVERT(VARCHAR(20), @AppUserId);
END
ELSE
BEGIN
    UPDATE dbo.AppUser
    SET PasswordHash = '$2b$12$U.CicaJlVoALtY76qurt5.Wkpva9W7uKnIxBgHkrxVnjaFoYLRtSu',
        UpdatedAt = SYSDATETIME()
    WHERE Id = @AppUserId;
    PRINT 'AppUser OK (senha atualizada): Id = ' + CONVERT(VARCHAR(20), @AppUserId);
END

/* ---------- UserRole ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.UserRole WHERE AppUserId = @AppUserId AND RoleId = @RoleId AND IsActive = 1)
BEGIN
    INSERT INTO dbo.UserRole (AppUserId, RoleId) VALUES (@AppUserId, @RoleId);
    PRINT 'UserRole criado.';
END
ELSE PRINT 'UserRole OK.';

/* ---------- Diagnóstico geral ---------- */
PRINT '--- Diagnóstico ---';
IF NOT EXISTS (SELECT 1 FROM dbo.TableStatus)
    PRINT 'ATENÇÃO: lookups vazios — execute BarRestaurante_Seed.sql (mesas, cardápio e status dependem dele).';
ELSE
    PRINT 'Lookups OK.';
SELECT 'DiningTable' AS Tabela, COUNT(*) AS Registros FROM dbo.DiningTable
UNION ALL SELECT 'Product', COUNT(*) FROM dbo.Product
UNION ALL SELECT 'AppUser', COUNT(*) FROM dbo.AppUser;
GO
