/* =====================================================================
   SyncBar — Integração iFood: credenciais (por empresa) + mapeamento de lojas (por filial)

   CORREÇÃO 2026-08-19: a primeira versão deste script guardava client_id/client_secret por
   FILIAL. Conferindo o portal do iFood, o app do usuário é do tipo "aplicativo centralizado"
   — um único client_id/client_secret autoriza acesso a VÁRIOS merchants (a própria tela de
   Permissões do app no portal lista os merchants autorizados). Ou seja: as credenciais são por
   EMPRESA, e só o MerchantId (a loja em si) é por FILIAL. Por isso agora são duas tabelas.

   Se você já rodou a versão anterior deste script: a tabela antiga tinha um esquema diferente
   (BranchId + ClientId + ClientSecret + MerchantId juntos) e, como a feature acabou de ser
   construída, não deveria haver nenhuma credencial real salva ainda — o DROP abaixo é seguro.
   Se por algum motivo você já salvou credenciais reais na tela antes de rodar isto, cadastre-as
   de novo depois (a tela pede o Client ID/Secret sempre que estão vazios).

   ATENÇÃO DE SINTAXE: o motor real deste banco é MySQL (Pomelo.EntityFrameworkCore.MySql —
   confira SyncBar.Infrastructure/DependencyInjection.cs). Vários scripts mais antigos desta
   pasta (ex.: BarRestaurante_TaxaServico.sql) estão escritos em T-SQL e NÃO refletem a sintaxe
   do banco real — use este script como referência de sintaxe correta daqui pra frente.

   ClientSecret NUNCA é gravado em texto puro — a API criptografa antes de salvar
   (ASP.NET Data Protection) e só grava aqui o valor cifrado.

   COMO RODAR: execute o arquivo inteiro contra o banco BarRestauranteDb (MySQL). Idempotente.
   ===================================================================== */

USE BarRestauranteDb;

-- Superseded pela versão nova (esquema por CompanyId) — seguro dropar, ver nota acima.
DROP TABLE IF EXISTS IFoodIntegrationSetting;

CREATE TABLE IFoodIntegrationSetting (
    Id                          BIGINT NOT NULL AUTO_INCREMENT,
    CompanyId                   BIGINT NOT NULL,
    ClientId                    VARCHAR(200) NULL,
    ClientSecretEncrypted       VARCHAR(1000) NULL,
    Enabled                     TINYINT(1) NOT NULL DEFAULT 0,
    LastConnectionTestAt        DATETIME(6) NULL,
    LastConnectionTestSucceeded TINYINT(1) NULL,
    CreatedAt                   DATETIME(6) NOT NULL,
    UpdatedAt                   DATETIME(6) NULL,
    IsActive                    TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodIntegrationSetting_Company
        FOREIGN KEY (CompanyId) REFERENCES Company (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodIntegrationSetting_CompanyId ON IFoodIntegrationSetting (CompanyId);

CREATE TABLE IF NOT EXISTS IFoodMerchantMapping (
    Id           BIGINT NOT NULL AUTO_INCREMENT,
    BranchId     BIGINT NOT NULL,
    MerchantId   VARCHAR(100) NULL,
    MerchantUuid VARCHAR(100) NULL,
    CreatedAt    DATETIME(6) NOT NULL,
    UpdatedAt    DATETIME(6) NULL,
    IsActive     TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_IFoodMerchantMapping_Branch
        FOREIGN KEY (BranchId) REFERENCES Branch (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX IX_IFoodMerchantMapping_BranchId ON IFoodMerchantMapping (BranchId);

-- Verificação:
SELECT * FROM IFoodIntegrationSetting;
SELECT * FROM IFoodMerchantMapping;
