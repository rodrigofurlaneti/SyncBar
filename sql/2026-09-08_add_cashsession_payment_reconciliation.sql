-- =====================================================================================
-- Revisao do Fechamento de Caixa (Detalhamento por Forma de Pagamento)
--
-- Adiciona:
--   1) coluna `TotalDifferenceAmount` em `cashsession` — soma da diferenca de dinheiro
--      com a diferenca de todas as formas de pagamento conferidas no fechamento
--      (antes so existia `DifferenceAmount`, que e so a quebra do dinheiro em especie).
--   2) tabela `cashsessionpaymentreconciliation` — conferencia por forma de pagamento
--      (Cartao de Credito/Debito/Pix): esperado (agregado das vendas liquidadas da
--      sessao), conferido (digitado pelo operador) e a diferenca isolada por modalidade.
--
-- Contexto: este projeto nao usa EF Core Migrations em producao (mesmo padrao dos
-- scripts 2026-09-01_add_diningtable_reading_validation_flags.sql e
-- 2026-09-03_add_shift_closing.sql) — execute diretamente contra o banco `barrestaurantedb`.
--
-- Reexecutável: adiciona somente estruturas ausentes. Selecione o banco correto na conexão.
-- =====================================================================================

SET @cash_reconciliation_ddl = IF(
  EXISTS(SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE() AND table_name = 'cashsession' AND column_name = 'TotalDifferenceAmount'),
  'SELECT 1',
  'ALTER TABLE `cashsession` ADD COLUMN `TotalDifferenceAmount` decimal(18,2) NULL AFTER `DifferenceAmount`'
);
PREPARE cash_reconciliation_stmt FROM @cash_reconciliation_ddl;
EXECUTE cash_reconciliation_stmt;
DEALLOCATE PREPARE cash_reconciliation_stmt;

CREATE TABLE IF NOT EXISTS `cashsessionpaymentreconciliation` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CashSessionId` bigint NOT NULL,
  `PaymentMethodId` bigint NOT NULL,
  `ExpectedAmount` decimal(18,2) NOT NULL DEFAULT 0,
  `CountedAmount` decimal(18,2) NOT NULL DEFAULT 0,
  `DifferenceAmount` decimal(18,2) NOT NULL DEFAULT 0,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_CashSessionPaymentReconciliation_CashSessionId_PaymentMethodId` (`CashSessionId`, `PaymentMethodId`),
  KEY `IX_CashSessionPaymentReconciliation_PaymentMethodId` (`PaymentMethodId`),
  CONSTRAINT `FK_CashSessionPaymentReconciliation_CashSession` FOREIGN KEY (`CashSessionId`) REFERENCES `cashsession` (`Id`),
  CONSTRAINT `FK_CashSessionPaymentReconciliation_PaymentMethod` FOREIGN KEY (`PaymentMethodId`) REFERENCES `paymentmethod` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Nota: nome da tabela de referencia na FK (`paymentmethod`) segue o padrao ja
-- confirmado em SalePaymentConfiguration (nomes de tabela em minusculo no MySQL). Se o
-- nome real divergir no seu dump, ajuste a FK correspondente antes de rodar.
