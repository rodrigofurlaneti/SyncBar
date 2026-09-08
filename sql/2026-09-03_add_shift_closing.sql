-- =====================================================================================
-- Fechamento Diario / Turno Comercial — cria as 3 tabelas novas da feature:
--   shiftclosingstatus   (lookup: Aberto/Fechado)
--   shiftclosing         (o fechamento de turno em si, consolidado por filial e periodo)
--   shiftclosingsession  (vinculo de auditoria: quais cashsession entraram em cada turno)
--
-- Contexto: este projeto nao usa EF Core Migrations em producao (a tabela
-- `__efmigrationshistory` existe mas esta vazia) e o dump `CreateDatabaseMySql.sql`
-- e um snapshot (mysqldump), nao uma fonte viva de schema — por isso este script e
-- entregue separadamente, para ser executado diretamente contra o banco `barrestaurantedb`,
-- no mesmo padrao do script 2026-09-01_add_diningtable_reading_validation_flags.sql.
--
-- NAO idempotente: rode uma unica vez. Rodar de novo falha com "Table already exists"
-- (ou "Duplicate entry" no INSERT de seed), o que e inofensivo e so confirma que o
-- script ja foi aplicado.
-- =====================================================================================

USE `barrestaurantedb`;

-- ---------------------------------------------------------------------------
-- shiftclosingstatus (lookup)
-- ---------------------------------------------------------------------------
CREATE TABLE `shiftclosingstatus` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `shiftclosingstatus` (`Id`, `Name`, `CreatedAt`, `IsActive`) VALUES
  (1, 'Aberto', NOW(), 1),
  (2, 'Fechado', NOW(), 1);

-- ---------------------------------------------------------------------------
-- shiftclosing (fechamento de turno)
-- ---------------------------------------------------------------------------
CREATE TABLE `shiftclosing` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `ShiftClosingStatusId` bigint NOT NULL,
  `OpenedByEmployeeId` bigint NOT NULL,
  `ClosedByEmployeeId` bigint NULL,
  `PeriodStart` datetime(6) NOT NULL,
  `PeriodEnd` datetime(6) NULL,
  `CashSessionsCount` int NOT NULL DEFAULT 0,
  `TotalOpeningAmount` decimal(18,2) NOT NULL DEFAULT 0,
  `TotalExpectedAmount` decimal(18,2) NOT NULL DEFAULT 0,
  `TotalRealizedAmount` decimal(18,2) NOT NULL DEFAULT 0,
  `TotalDifferenceAmount` decimal(18,2) NOT NULL DEFAULT 0,
  `Notes` varchar(500) NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ShiftClosing_BranchId` (`BranchId`),
  KEY `IX_ShiftClosing_ShiftClosingStatusId` (`ShiftClosingStatusId`),
  KEY `IX_ShiftClosing_OpenedByEmployeeId` (`OpenedByEmployeeId`),
  KEY `IX_ShiftClosing_ClosedByEmployeeId` (`ClosedByEmployeeId`),
  KEY `IX_ShiftClosing_PeriodStart` (`PeriodStart`),
  CONSTRAINT `FK_ShiftClosing_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_ShiftClosing_ShiftClosingStatus` FOREIGN KEY (`ShiftClosingStatusId`) REFERENCES `shiftclosingstatus` (`Id`),
  CONSTRAINT `FK_ShiftClosing_OpenedByEmployee` FOREIGN KEY (`OpenedByEmployeeId`) REFERENCES `employee` (`Id`),
  CONSTRAINT `FK_ShiftClosing_ClosedByEmployee` FOREIGN KEY (`ClosedByEmployeeId`) REFERENCES `employee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---------------------------------------------------------------------------
-- shiftclosingsession (vinculo de agregacao/auditoria com cashsession)
-- ---------------------------------------------------------------------------
CREATE TABLE `shiftclosingsession` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ShiftClosingId` bigint NOT NULL,
  `CashSessionId` bigint NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_ShiftClosingSession_ShiftClosingId_CashSessionId` (`ShiftClosingId`, `CashSessionId`),
  KEY `IX_ShiftClosingSession_CashSessionId` (`CashSessionId`),
  CONSTRAINT `FK_ShiftClosingSession_ShiftClosing` FOREIGN KEY (`ShiftClosingId`) REFERENCES `shiftclosing` (`Id`),
  CONSTRAINT `FK_ShiftClosingSession_CashSession` FOREIGN KEY (`CashSessionId`) REFERENCES `cashsession` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Nota: os nomes de tabela referenciados nas FKs (`branch`, `employee`, `cashsession`)
-- seguem o padrao ja confirmado em CashSessionConfiguration/CashMovementConfiguration
-- (nomes de tabela em minusculo no MySQL). Se o nome real de alguma tabela referenciada
-- divergir no seu dump, ajuste a FK correspondente antes de rodar.
