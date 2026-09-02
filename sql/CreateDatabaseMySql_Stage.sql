CREATE DATABASE IF NOT EXISTS `barrestaurantedb_stage` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `barrestaurantedb_stage`;

SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------
-- 1. IDENTIDADE E CONTROLE DE ACESSO
-- ------------------------------------------------------
CREATE TABLE `company` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `LegalName` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `TradeName` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Cnpj` char(14) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `jobtitle` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_JobTitle_Company` (`CompanyId`),
  CONSTRAINT `FK_JobTitle_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `branch` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Cnpj` char(14) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `AddressStreet` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `AddressNumber` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `AddressDistrict` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `AddressCity` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `AddressState` char(2) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `AddressZipCode` char(8) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `SelfServiceEmployeeId` bigint DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_Branch_Company` (`CompanyId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `employee` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `JobTitleId` bigint NOT NULL,
  `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Cpf` char(11) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `HiredAt` datetime(6) NOT NULL,
  `DismissedAt` datetime(6) DEFAULT NULL,
  `Salary` decimal(18,2) DEFAULT NULL,
  `CommissionPercent` decimal(5,2) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_Employee_Branch` (`BranchId`),
  KEY `FK_Employee_JobTitle` (`JobTitleId`),
  CONSTRAINT `FK_Employee_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_Employee_JobTitle` FOREIGN KEY (`JobTitleId`) REFERENCES `jobtitle` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

ALTER TABLE `branch` ADD CONSTRAINT `FK_Branch_SelfServiceEmployee` FOREIGN KEY (`SelfServiceEmployeeId`) REFERENCES `employee` (`Id`);

CREATE TABLE `appuser` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `EmployeeId` bigint DEFAULT NULL,
  `UserName` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PasswordHash` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PasswordSalt` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `FailedAccessCount` int NOT NULL DEFAULT '0',
  `LockoutEndAt` datetime(6) DEFAULT NULL,
  `LastLoginAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_AppUser_Company` (`CompanyId`),
  KEY `FK_AppUser_Employee` (`EmployeeId`),
  CONSTRAINT `FK_AppUser_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),
  CONSTRAINT `FK_AppUser_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `appfeature` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Code` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `appuserfeature` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AppUserId` bigint NOT NULL,
  `AppFeatureId` bigint NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_AppUserFeature_AppUser` (`AppUserId`),
  KEY `FK_AppUserFeature_AppFeature` (`AppFeatureId`),
  CONSTRAINT `FK_AppUserFeature_AppFeature` FOREIGN KEY (`AppFeatureId`) REFERENCES `appfeature` (`Id`),
  CONSTRAINT `FK_AppUserFeature_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `appuser` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `jobtitlefeature` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `JobTitleId` bigint NOT NULL,
  `AppFeatureId` bigint NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_JobTitleFeature_JobTitle` (`JobTitleId`),
  KEY `FK_JobTitleFeature_AppFeature` (`AppFeatureId`),
  CONSTRAINT `FK_JobTitleFeature_AppFeature` FOREIGN KEY (`AppFeatureId`) REFERENCES `appfeature` (`Id`),
  CONSTRAINT `FK_JobTitleFeature_JobTitle` FOREIGN KEY (`JobTitleId`) REFERENCES `jobtitle` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `customer` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Cpf` char(11) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `LoyaltyPoints` int NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_Customer_Company` (`CompanyId`),
  CONSTRAINT `FK_Customer_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------
-- 2. CATÁLOGO DE PRODUTOS (Multi-Loja)
-- ------------------------------------------------------
CREATE TABLE `category` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `BranchId` bigint DEFAULT NULL,
  `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `DisplayOrder` int NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_Category_Company` (`CompanyId`),
  KEY `FK_Category_Branch` (`BranchId`),
  CONSTRAINT `FK_Category_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),
  CONSTRAINT `FK_Category_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `product` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `BranchId` bigint DEFAULT NULL,
  `CategoryId` bigint NOT NULL,
  `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `BasePrice` decimal(18,2) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_Product_Company` (`CompanyId`),
  KEY `FK_Product_Branch` (`BranchId`),
  KEY `FK_Product_Category` (`CategoryId`),
  CONSTRAINT `FK_Product_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),
  CONSTRAINT `FK_Product_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_Product_Category` FOREIGN KEY (`CategoryId`) REFERENCES `category` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `complementgroup` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `BranchId` bigint DEFAULT NULL,
  `Name` varchar(150) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `ComplementGroupTypeId` tinyint NOT NULL,
  `MinSelection` int NOT NULL,
  `MaxSelection` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_ComplementGroup_CompanyId` (`CompanyId`),
  KEY `IX_ComplementGroup_BranchId` (`BranchId`),
  CONSTRAINT `FK_ComplementGroup_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),
  CONSTRAINT `FK_ComplementGroup_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `complementitem` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `BranchId` bigint DEFAULT NULL,
  `Name` varchar(150) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `LinkedProductId` bigint DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_ComplementItem_CompanyId` (`CompanyId`),
  KEY `IX_ComplementItem_BranchId` (`BranchId`),
  KEY `IX_ComplementItem_LinkedProductId` (`LinkedProductId`),
  CONSTRAINT `FK_ComplementItem_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),
  CONSTRAINT `FK_ComplementItem_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_ComplementItem_LinkedProduct` FOREIGN KEY (`LinkedProductId`) REFERENCES `product` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `complement` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ComplementGroupId` bigint NOT NULL,
  `ComplementItemId` bigint NOT NULL,
  `ExtraPrice` decimal(18,2) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_Complement_ComplementGroupId` (`ComplementGroupId`),
  KEY `IX_Complement_ComplementItemId` (`ComplementItemId`),
  CONSTRAINT `FK_Complement_ComplementGroup` FOREIGN KEY (`ComplementGroupId`) REFERENCES `complementgroup` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Complement_ComplementItem` FOREIGN KEY (`ComplementItemId`) REFERENCES `complementitem` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `costtype` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------
-- 3. OPERACIONAL DE SALÃO
-- ------------------------------------------------------
CREATE TABLE `diningarea` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_DiningArea_BranchId` (`BranchId`),
  CONSTRAINT `FK_DiningArea_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `tablestatus` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `diningtable` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `TableStatusId` bigint NOT NULL,
  `Number` int NOT NULL,
  `Capacity` int DEFAULT NULL,
  `QrToken` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `IsQrViewEnabled` tinyint(1) NOT NULL DEFAULT '1',
  `IsCameraInputEnabled` tinyint(1) NOT NULL DEFAULT '0',
  `IsBarcodeEnabled` tinyint(1) NOT NULL DEFAULT '0',
  `IsQrCodeEnabled` tinyint(1) NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_DiningTable_Branch` (`BranchId`),
  KEY `FK_DiningTable_TableStatus` (`TableStatusId`),
  CONSTRAINT `FK_DiningTable_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_DiningTable_TableStatus` FOREIGN KEY (`TableStatusId`) REFERENCES `tablestatus` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `diningareatable` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `DiningAreaId` bigint NOT NULL,
  `DiningTableId` bigint NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_DiningAreaTable_Table` (`DiningTableId`),
  KEY `IX_DiningAreaTable_DiningAreaId` (`DiningAreaId`),
  CONSTRAINT `FK_DiningAreaTable_DiningArea` FOREIGN KEY (`DiningAreaId`) REFERENCES `diningarea` (`Id`),
  CONSTRAINT `FK_DiningAreaTable_DiningTable` FOREIGN KEY (`DiningTableId`) REFERENCES `diningtable` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `diningareaassignment` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `DiningAreaId` bigint NOT NULL,
  `EmployeeId` bigint NOT NULL,
  `StartAt` datetime(6) NOT NULL,
  `EndAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_DiningAreaAssignment_DiningAreaId` (`DiningAreaId`),
  KEY `IX_DiningAreaAssignment_EmployeeId` (`EmployeeId`),
  CONSTRAINT `FK_DiningAreaAssignment_DiningArea` FOREIGN KEY (`DiningAreaId`) REFERENCES `diningarea` (`Id`),
  CONSTRAINT `FK_DiningAreaAssignment_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `comandastatus` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `comandasetting` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `DefaultLimitAmount` decimal(18,2) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_ComandaSetting_Branch` (`BranchId`),
  CONSTRAINT `FK_ComandaSetting_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `comanda` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `ComandaStatusId` bigint NOT NULL,
  `Code` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_Comanda_Branch` (`BranchId`),
  KEY `FK_Comanda_ComandaStatus` (`ComandaStatusId`),
  CONSTRAINT `FK_Comanda_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_Comanda_ComandaStatus` FOREIGN KEY (`ComandaStatusId`) REFERENCES `comandastatus` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------
-- 4. VENDAS E CAIXA
-- ------------------------------------------------------
CREATE TABLE `orderstatus` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `customerorder` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `DiningTableId` bigint DEFAULT NULL,
  `ComandaId` bigint DEFAULT NULL,
  `EmployeeId` bigint NOT NULL,
  `OrderStatusId` bigint NOT NULL,
  `CustomerId` bigint DEFAULT NULL,
  `OrderTypeId` tinyint NOT NULL DEFAULT '1',
  `GuestCount` int DEFAULT NULL,
  `SubtotalAmount` decimal(18,2) NOT NULL DEFAULT '0.00',
  `DiscountAmount` decimal(18,2) NOT NULL DEFAULT '0.00',
  `ServiceFeeAmount` decimal(18,2) NOT NULL DEFAULT '0.00',
  `TotalAmount` decimal(18,2) NOT NULL DEFAULT '0.00',
  `CreditLimitAmount` decimal(18,2) DEFAULT NULL,
  `CustomerName` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CustomerPhone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `DeliveryAddress` varchar(300) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Notes` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `OpenedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ClosedAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_CustomerOrder_Branch` (`BranchId`),
  KEY `FK_CustomerOrder_DiningTable` (`DiningTableId`),
  KEY `FK_CustomerOrder_Comanda` (`ComandaId`),
  KEY `FK_CustomerOrder_Employee` (`EmployeeId`),
  KEY `FK_CustomerOrder_OrderStatus` (`OrderStatusId`),
  KEY `FK_CustomerOrder_Customer` (`CustomerId`),
  KEY `IX_CustomerOrder_CreatedAt_Status` (`CreatedAt`,`OrderStatusId`),
  CONSTRAINT `FK_CustomerOrder_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_CustomerOrder_Comanda` FOREIGN KEY (`ComandaId`) REFERENCES `comanda` (`Id`),
  CONSTRAINT `FK_CustomerOrder_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`),
  CONSTRAINT `FK_CustomerOrder_DiningTable` FOREIGN KEY (`DiningTableId`) REFERENCES `diningtable` (`Id`),
  CONSTRAINT `FK_CustomerOrder_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`),
  CONSTRAINT `FK_CustomerOrder_OrderStatus` FOREIGN KEY (`OrderStatusId`) REFERENCES `orderstatus` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `comandaitemtransfer` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CustomerOrderId` bigint NOT NULL,
  `CustomerOrderItemId` bigint NOT NULL,
  `SourceComandaId` bigint NOT NULL,
  `TargetComandaId` bigint NOT NULL,
  `EmployeeId` bigint NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_ComandaItemTransfer_CustomerOrder` (`CustomerOrderId`),
  KEY `FK_ComandaItemTransfer_Employee` (`EmployeeId`),
  CONSTRAINT `FK_ComandaItemTransfer_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `customerorder` (`Id`),
  CONSTRAINT `FK_ComandaItemTransfer_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `cashregister` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_CashRegister_Branch` (`BranchId`),
  CONSTRAINT `FK_CashRegister_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `cashsessionstatus` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `cashsession` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CashRegisterId` bigint NOT NULL,
  `CashSessionStatusId` bigint NOT NULL,
  `OpenedByEmployeeId` bigint NOT NULL,
  `ClosedByEmployeeId` bigint DEFAULT NULL,
  `OpeningAmount` decimal(18,2) NOT NULL DEFAULT '0.00',
  `ClosingAmount` decimal(18,2) DEFAULT NULL,
  `ExpectedAmount` decimal(18,2) DEFAULT NULL,
  `DifferenceAmount` decimal(18,2) DEFAULT NULL,
  `OpenedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ClosedAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_CashSession_CashRegister` (`CashRegisterId`),
  KEY `FK_CashSession_CashSessionStatus` (`CashSessionStatusId`),
  KEY `FK_CashSession_OpenedByEmployee` (`OpenedByEmployeeId`),
  KEY `FK_CashSession_ClosedByEmployee` (`ClosedByEmployeeId`),
  KEY `IX_CashSession_OpenedAt` (`OpenedAt`),
  CONSTRAINT `FK_CashSession_CashRegister` FOREIGN KEY (`CashRegisterId`) REFERENCES `cashregister` (`Id`),
  CONSTRAINT `FK_CashSession_CashSessionStatus` FOREIGN KEY (`CashSessionStatusId`) REFERENCES `cashsessionstatus` (`Id`),
  CONSTRAINT `FK_CashSession_ClosedByEmployee` FOREIGN KEY (`ClosedByEmployeeId`) REFERENCES `employee` (`Id`),
  CONSTRAINT `FK_CashSession_OpenedByEmployee` FOREIGN KEY (`OpenedByEmployeeId`) REFERENCES `employee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `cashmovementtype` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `IsInflow` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `sale` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CustomerOrderId` bigint NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_Sale_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `customerorder` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `cashmovement` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CashSessionId` bigint NOT NULL,
  `CashMovementTypeId` bigint NOT NULL,
  `SaleId` bigint DEFAULT NULL,
  `EmployeeId` bigint NOT NULL,
  `Amount` decimal(18,2) NOT NULL,
  `Description` varchar(300) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_CashMovement_CashSession` (`CashSessionId`),
  KEY `FK_CashMovement_CashMovementType` (`CashMovementTypeId`),
  KEY `FK_CashMovement_Sale` (`SaleId`),
  KEY `FK_CashMovement_Employee` (`EmployeeId`),
  KEY `IX_CashMovement_CreatedAt` (`CreatedAt`),
  CONSTRAINT `FK_CashMovement_CashMovementType` FOREIGN KEY (`CashMovementTypeId`) REFERENCES `cashmovementtype` (`Id`),
  CONSTRAINT `FK_CashMovement_CashSession` FOREIGN KEY (`CashSessionId`) REFERENCES `cashsession` (`Id`),
  CONSTRAINT `FK_CashMovement_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`),
  CONSTRAINT `FK_CashMovement_Sale` FOREIGN KEY (`SaleId`) REFERENCES `sale` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------
-- 5. INTEGRAÇÕES (IFOOD)
-- ------------------------------------------------------
CREATE TABLE `ifoodintegrationsetting` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `ClientId` varchar(200) DEFAULT NULL,
  `ClientSecretEncrypted` varchar(1000) DEFAULT NULL,
  `Enabled` tinyint(1) NOT NULL DEFAULT '0',
  `LastConnectionTestAt` datetime(6) DEFAULT NULL,
  `LastConnectionTestSucceeded` tinyint(1) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `IFoodCustomerId` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodIntegrationSetting_CompanyId` (`CompanyId`),
  CONSTRAINT `FK_IFoodIntegrationSetting_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodmerchantmapping` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `MerchantId` varchar(100) DEFAULT NULL,
  `MerchantUuid` varchar(100) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `PreparationTimeMinutes` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodMerchantMapping_BranchId` (`BranchId`),
  CONSTRAINT `FK_IFoodMerchantMapping_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodopeninghours` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `DayOfWeek` int NOT NULL,
  `Start` time NOT NULL,
  `DurationMinutes` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodOpeningHours_BranchId_DayOfWeek` (`BranchId`,`DayOfWeek`),
  CONSTRAINT `FK_IFoodOpeningHours_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodcategorymapping` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CategoryId` bigint NOT NULL,
  `BranchId` bigint NOT NULL,
  `IFoodCategoryId` varchar(100) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_IFoodCategoryMapping_Branch` (`BranchId`),
  KEY `IX_IFoodCategoryMapping_CategoryId_BranchId` (`CategoryId`,`BranchId`),
  CONSTRAINT `FK_IFoodCategoryMapping_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_IFoodCategoryMapping_Category` FOREIGN KEY (`CategoryId`) REFERENCES `category` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodproductmapping` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ProductId` bigint NOT NULL,
  `BranchId` bigint NOT NULL,
  `IFoodItemId` char(36) NOT NULL,
  `IFoodProductId` char(36) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodProductMapping_ProductId_BranchId` (`ProductId`,`BranchId`),
  KEY `IX_IFoodProductMapping_BranchId` (`BranchId`),
  CONSTRAINT `FK_IFoodProductMapping_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_IFoodProductMapping_Product` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodcomplementgroupmapping` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ComplementGroupId` bigint NOT NULL,
  `BranchId` bigint NOT NULL,
  `IFoodOptionGroupId` char(36) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodComplementGroupMapping_ComplementGroupId_BranchId` (`ComplementGroupId`,`BranchId`),
  KEY `IX_IFoodComplementGroupMapping_BranchId` (`BranchId`),
  CONSTRAINT `FK_IFoodComplementGroupMapping_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_IFoodComplementGroupMapping_ComplementGroup` FOREIGN KEY (`ComplementGroupId`) REFERENCES `complementgroup` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodcomplementmapping` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ComplementId` bigint NOT NULL,
  `BranchId` bigint NOT NULL,
  `IFoodOptionId` char(36) NOT NULL,
  `IFoodProductId` char(36) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodComplementMapping_ComplementId_BranchId` (`ComplementId`,`BranchId`),
  KEY `IX_IFoodComplementMapping_BranchId` (`BranchId`),
  KEY `IX_IFoodComplementMapping_IFoodOptionId` (`IFoodOptionId`),
  CONSTRAINT `FK_IFoodComplementMapping_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_IFoodComplementMapping_Complement` FOREIGN KEY (`ComplementId`) REFERENCES `complement` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `pizzaconfiguration` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `ifoodpizzamapping` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `PizzaConfigurationId` bigint NOT NULL,
  `BranchId` bigint NOT NULL,
  `IFoodPizzaId` varchar(100) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodPizzaMapping_PizzaConfigurationId_BranchId` (`PizzaConfigurationId`,`BranchId`),
  KEY `IX_IFoodPizzaMapping_BranchId` (`BranchId`),
  CONSTRAINT `FK_IFoodPizzaMapping_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_IFoodPizzaMapping_PizzaConfiguration` FOREIGN KEY (`PizzaConfigurationId`) REFERENCES `pizzaconfiguration` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodpizzaelementmapping` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `IFoodPizzaMappingId` bigint NOT NULL,
  `Kind` tinyint NOT NULL,
  `LocalId` bigint NOT NULL,
  `IFoodElementId` varchar(100) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodPizzaElementMapping_MappingId_Kind_LocalId` (`IFoodPizzaMappingId`,`Kind`,`LocalId`),
  CONSTRAINT `FK_IFoodPizzaElementMapping_IFoodPizzaMapping` FOREIGN KEY (`IFoodPizzaMappingId`) REFERENCES `ifoodpizzamapping` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodorder` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CustomerOrderId` bigint NOT NULL,
  `BranchId` bigint NOT NULL,
  `IFoodOrderId` varchar(100) NOT NULL,
  `DisplayId` varchar(50) DEFAULT NULL,
  `MerchantId` varchar(100) NOT NULL,
  `IFoodOrderType` varchar(30) NOT NULL,
  `DeliveredBy` varchar(30) DEFAULT NULL,
  `OrderTiming` varchar(20) NOT NULL DEFAULT 'IMMEDIATE',
  `PreparationStartDateTime` datetime(6) DEFAULT NULL,
  `Status` varchar(30) NOT NULL,
  `ConfirmDeadlineAt` datetime(6) NOT NULL,
  `ConfirmedAt` datetime(6) DEFAULT NULL,
  `HasUnmappedItems` tinyint(1) NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_IFoodOrder_IFoodOrderId` (`IFoodOrderId`),
  KEY `IX_IFoodOrder_BranchId` (`BranchId`),
  KEY `IX_IFoodOrder_CustomerOrderId` (`CustomerOrderId`),
  CONSTRAINT `FK_IFoodOrder_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_IFoodOrder_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `customerorder` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodshippingdelivery` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `OrderReference` varchar(150) DEFAULT NULL,
  `CustomerName` varchar(150) NOT NULL,
  `CustomerPhoneAreaCode` varchar(5) NOT NULL,
  `CustomerPhoneNumber` varchar(20) NOT NULL,
  `PostalCode` varchar(15) NOT NULL,
  `StreetName` varchar(200) NOT NULL,
  `StreetNumber` varchar(20) NOT NULL,
  `Complement` varchar(100) DEFAULT NULL,
  `Neighborhood` varchar(100) NOT NULL,
  `City` varchar(100) NOT NULL,
  `State` varchar(2) NOT NULL,
  `Country` varchar(60) NOT NULL,
  `Reference` varchar(200) DEFAULT NULL,
  `Latitude` double DEFAULT NULL,
  `Longitude` double DEFAULT NULL,
  `MerchantFee` decimal(18,2) NOT NULL,
  `QuoteId` varchar(100) NOT NULL,
  `IFoodDeliveryId` varchar(100) NOT NULL,
  `TrackingUrl` varchar(500) DEFAULT NULL,
  `Status` varchar(30) NOT NULL,
  `RequestedAt` datetime(6) NOT NULL,
  `CancelledAt` datetime(6) DEFAULT NULL,
  `CancellationReason` varchar(300) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_IFoodShippingDelivery_IFoodDeliveryId` (`IFoodDeliveryId`),
  KEY `IX_IFoodShippingDelivery_BranchId` (`BranchId`),
  CONSTRAINT `FK_IFoodShippingDelivery_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodlogisticsdelivery` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `IFoodOrderId` bigint NOT NULL,
  `BranchId` bigint NOT NULL,
  `DriverName` varchar(150) NOT NULL,
  `DriverPhone` varchar(30) NOT NULL,
  `DriverVehicleType` varchar(30) NOT NULL,
  `Status` varchar(30) NOT NULL,
  `AssignedAt` datetime(6) NOT NULL,
  `GoingToOriginAt` datetime(6) DEFAULT NULL,
  `ArrivedAtOriginAt` datetime(6) DEFAULT NULL,
  `DispatchedAt` datetime(6) DEFAULT NULL,
  `ArrivedAtDestinationAt` datetime(6) DEFAULT NULL,
  `DeliveryCodeVerifiedAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_IFoodLogisticsDelivery_IFoodOrderId` (`IFoodOrderId`),
  KEY `IX_IFoodLogisticsDelivery_BranchId` (`BranchId`),
  CONSTRAINT `FK_IFoodLogisticsDelivery_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),
  CONSTRAINT `FK_IFoodLogisticsDelivery_IFoodOrder` FOREIGN KEY (`IFoodOrderId`) REFERENCES `ifoodorder` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodfinancialevent` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `IFoodEventId` varchar(100) NOT NULL,
  `Name` varchar(150) NOT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `Trigger` varchar(100) DEFAULT NULL,
  `Amount` decimal(18,2) NOT NULL,
  `HasTransferImpact` tinyint(1) NOT NULL DEFAULT '0',
  `CompetenceDate` datetime(6) NOT NULL,
  `PeriodStart` datetime(6) NOT NULL,
  `PeriodEnd` datetime(6) NOT NULL,
  `SettlementExpectedDate` datetime(6) DEFAULT NULL,
  `ReferenceType` varchar(30) DEFAULT NULL,
  `ReferenceId` varchar(100) DEFAULT NULL,
  `RawPayload` text NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodFinancialEvent_BranchId_IFoodEventId` (`BranchId`,`IFoodEventId`),
  KEY `IX_IFoodFinancialEvent_BranchId_CompetenceDate` (`BranchId`,`CompetenceDate`),
  KEY `IX_IFoodFinancialEvent_ReferenceType_ReferenceId` (`ReferenceType`,`ReferenceId`),
  CONSTRAINT `FK_IFoodFinancialEvent_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `ifoodsettlement` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `IFoodSettlementId` varchar(100) NOT NULL,
  `Type` varchar(30) NOT NULL,
  `Product` varchar(50) DEFAULT NULL,
  `Amount` decimal(18,2) NOT NULL,
  `Status` varchar(30) NOT NULL,
  `PaymentDate` datetime(6) DEFAULT NULL,
  `BankCode` varchar(20) DEFAULT NULL,
  `BankAgency` varchar(20) DEFAULT NULL,
  `BankAccount` varchar(30) DEFAULT NULL,
  `RawPayload` text NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_IFoodSettlement_BranchId_IFoodSettlementId` (`BranchId`,`IFoodSettlementId`),
  KEY `IX_IFoodSettlement_BranchId_PaymentDate` (`BranchId`,`PaymentDate`),
  CONSTRAINT `FK_IFoodSettlement_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ------------------------------------------------------
-- 6. LOGS DO SISTEMA
-- ------------------------------------------------------
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ProductVersion` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `accesslog` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AppUserId` bigint DEFAULT NULL,
  `UserName` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `EventType` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `IpAddress` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `UserAgent` varchar(300) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_AccessLog_AppUser` (`AppUserId`),
  CONSTRAINT `FK_AccessLog_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `appuser` (`Id`),
  CONSTRAINT `CK_AccessLog_EventType` CHECK ((`EventType` in (_utf8mb4'Lockout',_utf8mb4'LoginFailed',_utf8mb4'Logout',_utf8mb4'Login')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `logtracker` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `AppUserId` bigint DEFAULT NULL,
  `DirectoryName` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ClassName` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `MethodName` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `IsSuccess` tinyint(1) NOT NULL DEFAULT '1',
  `ExecutionTimeMs` bigint DEFAULT NULL,
  `Message` text COLLATE utf8mb4_unicode_ci,
  `ErrorMessage` text COLLATE utf8mb4_unicode_ci,
  `StackTrace` text COLLATE utf8mb4_unicode_ci,
  `IpAddress` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `FK_LogTracker_AppUser` (`AppUserId`),
  KEY `IX_LogTracker_CreatedAt` (`CreatedAt`),
  CONSTRAINT `FK_LogTracker_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `appuser` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;