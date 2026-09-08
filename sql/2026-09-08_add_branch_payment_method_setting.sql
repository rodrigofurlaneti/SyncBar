-- =====================================================================================
-- Integracao Asaas — Metodos de Pagamento por Empresa/Filial
--
-- Cada filial pode habilitar/desabilitar suas proprias formas de recebimento (Pix,
-- Boleto, Cartao de Credito, Cartao de Debito, Maquininha Fisica). Uma linha com
-- BranchId NULL representa a configuracao padrao da empresa (matriz); uma filial sem
-- configuracao propria herda essa configuracao padrao (fallback resolvido pela API em
-- GET /api/branch-payment-method-settings/resolve).
--
-- Contexto: este projeto nao usa EF Core Migrations em producao (mesmo padrao dos
-- scripts anteriores em sql/) — execute diretamente contra o banco `barrestaurantedb`.
--
-- NAO idempotente: rode uma unica vez.
-- =====================================================================================

USE `barrestaurantedb`;

CREATE TABLE `branchpaymentmethodsetting` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `BranchId` bigint NULL,
  `EnablePix` tinyint(1) NOT NULL DEFAULT 1,
  `EnableBoleto` tinyint(1) NOT NULL DEFAULT 1,
  `EnableCreditCard` tinyint(1) NOT NULL DEFAULT 1,
  `EnableDebitCard` tinyint(1) NOT NULL DEFAULT 1,
  `EnableCashMachine` tinyint(1) NOT NULL DEFAULT 1,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_BranchPaymentMethodSetting_Company` (`CompanyId`),
  KEY `IX_BranchPaymentMethodSetting_Branch` (`BranchId`),
  CONSTRAINT `FK_BranchPaymentMethodSetting_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),
  CONSTRAINT `FK_BranchPaymentMethodSetting_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Nota: nomes de tabela referenciados nas FKs (`company`, `branch`) seguem o padrao ja
-- confirmado em outras configurations (nomes de tabela em minusculo no MySQL). Se algum
-- nome divergir no seu dump, ajuste a FK correspondente antes de rodar.
