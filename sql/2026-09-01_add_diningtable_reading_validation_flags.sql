-- =====================================================================================
-- Adiciona os três parâmetros de configuração de validação de leitura de comanda/mesa
-- na tabela `diningtable`: câmera, código de barras e QR Code.
--
-- Contexto: este projeto não usa EF Core Migrations em produção (a tabela
-- `__efmigrationshistory` existe mas está vazia) e o dump `CreateDatabaseMySql.sql`
-- é um snapshot (mysqldump de 2026-08-25), não uma fonte viva de schema — por isso
-- este script de ALTER é entregue separadamente, para ser executado diretamente
-- contra o banco `barrestaurantedb`.
--
-- NÃO idempotente: o servidor em uso rejeitou `ADD COLUMN IF NOT EXISTS`
-- (erro 1064 — a sintaxe é suportada só a partir do MySQL 8.0.29, e este servidor
-- não aceitou). Rode uma única vez; rodar de novo falha com "Duplicate column name",
-- o que é inofensivo e só confirma que a coluna já existe.
-- =====================================================================================

USE `barrestaurantedb`;

ALTER TABLE `diningtable`
  ADD COLUMN `IsCameraInputEnabled` tinyint(1) NOT NULL DEFAULT '0',
  ADD COLUMN `IsBarcodeEnabled` tinyint(1) NOT NULL DEFAULT '0',
  ADD COLUMN `IsQrCodeEnabled` tinyint(1) NOT NULL DEFAULT '0';
