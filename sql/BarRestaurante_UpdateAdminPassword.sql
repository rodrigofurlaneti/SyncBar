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
