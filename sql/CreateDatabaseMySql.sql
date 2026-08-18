CREATE DATABASE IF NOT EXISTS `BarRestauranteDb`;
USE `BarRestauranteDb`;

-- -----------------------------------------------------
-- Table `__EFMigrationsHistory`
-- -----------------------------------------------------
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` VARCHAR(150) NOT NULL,
  `ProductVersion` VARCHAR(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `AppFeature`
-- -----------------------------------------------------
CREATE TABLE `AppFeature` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Code` VARCHAR(50) NOT NULL,
  `Name` VARCHAR(100) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Company`
-- -----------------------------------------------------
CREATE TABLE `Company` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `LegalName` VARCHAR(200) NOT NULL,
  `TradeName` VARCHAR(150) NOT NULL,
  `Cnpj` CHAR(14) NOT NULL,
  `Email` VARCHAR(150) NULL,
  `Phone` VARCHAR(20) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `JobTitle`
-- -----------------------------------------------------
CREATE TABLE `JobTitle` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `Name` VARCHAR(100) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_JobTitle_Company` (`CompanyId`),
  CONSTRAINT `FK_JobTitle_Company` FOREIGN KEY (`CompanyId`) REFERENCES `Company` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `JobTitleFeature`
-- -----------------------------------------------------
CREATE TABLE `JobTitleFeature` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `JobTitleId` BIGINT NOT NULL,
  `AppFeatureId` BIGINT NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_JobTitleFeature_JobTitle` (`JobTitleId`),
  INDEX `FK_JobTitleFeature_AppFeature` (`AppFeatureId`),
  CONSTRAINT `FK_JobTitleFeature_JobTitle` FOREIGN KEY (`JobTitleId`) REFERENCES `JobTitle` (`Id`),
  CONSTRAINT `FK_JobTitleFeature_AppFeature` FOREIGN KEY (`AppFeatureId`) REFERENCES `AppFeature` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Branch`
-- -----------------------------------------------------
CREATE TABLE `Branch` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `Name` VARCHAR(150) NOT NULL,
  `Cnpj` CHAR(14) NULL,
  `Phone` VARCHAR(20) NULL,
  `AddressStreet` VARCHAR(200) NULL,
  `AddressNumber` VARCHAR(20) NULL,
  `AddressDistrict` VARCHAR(100) NULL,
  `AddressCity` VARCHAR(100) NULL,
  `AddressState` CHAR(2) NULL,
  `AddressZipCode` CHAR(8) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `SelfServiceEmployeeId` BIGINT NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_Branch_Company` (`CompanyId`),
  INDEX `FK_Branch_SelfServiceEmployee` (`SelfServiceEmployeeId`),
  CONSTRAINT `FK_Branch_Company` FOREIGN KEY (`CompanyId`) REFERENCES `Company` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Employee`
-- -----------------------------------------------------
CREATE TABLE `Employee` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `JobTitleId` BIGINT NOT NULL,
  `Name` VARCHAR(150) NOT NULL,
  `Cpf` CHAR(11) NOT NULL,
  `Email` VARCHAR(150) NULL,
  `Phone` VARCHAR(20) NULL,
  `HiredAt` DATETIME(6) NOT NULL,
  `DismissedAt` DATETIME(6) NULL,
  `Salary` DECIMAL(18, 2) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CommissionPercent` DECIMAL(5, 2) NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_Employee_Branch` (`BranchId`),
  INDEX `FK_Employee_JobTitle` (`JobTitleId`),
  CONSTRAINT `FK_Employee_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_Employee_JobTitle` FOREIGN KEY (`JobTitleId`) REFERENCES `JobTitle` (`Id`),
  CONSTRAINT `CK_Employee_CommissionPercent` CHECK (`CommissionPercent` IS NULL OR (`CommissionPercent` >= 0 AND `CommissionPercent` <= 100))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Adicionar FK tardia para SelfServiceEmployee em Branch agora que Employee existe
ALTER TABLE `Branch`
  ADD CONSTRAINT `FK_Branch_SelfServiceEmployee` FOREIGN KEY (`SelfServiceEmployeeId`) REFERENCES `Employee` (`Id`);

-- -----------------------------------------------------
-- Table `AppUser`
-- -----------------------------------------------------
CREATE TABLE `AppUser` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `EmployeeId` BIGINT NULL,
  `UserName` VARCHAR(100) NOT NULL,
  `Email` VARCHAR(150) NOT NULL,
  `PasswordHash` VARCHAR(500) NOT NULL,
  `PasswordSalt` VARCHAR(200) NULL,
  `FailedAccessCount` INT NOT NULL DEFAULT 0,
  `LockoutEndAt` DATETIME(6) NULL,
  `LastLoginAt` DATETIME(6) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_AppUser_Company` (`CompanyId`),
  INDEX `FK_AppUser_Employee` (`EmployeeId`),
  CONSTRAINT `FK_AppUser_Company` FOREIGN KEY (`CompanyId`) REFERENCES `Company` (`Id`),
  CONSTRAINT `FK_AppUser_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `Employee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `AccessLog`
-- -----------------------------------------------------
CREATE TABLE `AccessLog` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `AppUserId` BIGINT NULL,
  `UserName` VARCHAR(100) NOT NULL,
  `EventType` VARCHAR(30) NOT NULL,
  `IpAddress` VARCHAR(45) NULL,
  `UserAgent` VARCHAR(300) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_AccessLog_AppUser` (`AppUserId`),
  CONSTRAINT `FK_AccessLog_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `AppUser` (`Id`),
  CONSTRAINT `CK_AccessLog_EventType` CHECK (`EventType` IN ('Lockout', 'LoginFailed', 'Logout', 'Login'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `AppUserFeature`
-- -----------------------------------------------------
CREATE TABLE `AppUserFeature` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `AppUserId` BIGINT NOT NULL,
  `AppFeatureId` BIGINT NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_AppUserFeature_AppUser` (`AppUserId`),
  INDEX `FK_AppUserFeature_AppFeature` (`AppFeatureId`),
  CONSTRAINT `FK_AppUserFeature_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `AppUser` (`Id`),
  CONSTRAINT `FK_AppUserFeature_AppFeature` FOREIGN KEY (`AppFeatureId`) REFERENCES `AppFeature` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `CashMovementType`
-- -----------------------------------------------------
CREATE TABLE `CashMovementType` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `IsInflow` TINYINT(1) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `CashRegister`
-- -----------------------------------------------------
CREATE TABLE `CashRegister` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `Name` VARCHAR(100) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_CashRegister_Branch` (`BranchId`),
  CONSTRAINT `FK_CashRegister_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `CashSessionStatus`
-- -----------------------------------------------------
CREATE TABLE `CashSessionStatus` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `CashSession`
-- -----------------------------------------------------
CREATE TABLE `CashSession` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CashRegisterId` BIGINT NOT NULL,
  `CashSessionStatusId` BIGINT NOT NULL,
  `OpenedByEmployeeId` BIGINT NOT NULL,
  `ClosedByEmployeeId` BIGINT NULL,
  `OpeningAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `ClosingAmount` DECIMAL(18, 2) NULL,
  `ExpectedAmount` DECIMAL(18, 2) NULL,
  `DifferenceAmount` DECIMAL(18, 2) NULL,
  `OpenedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ClosedAt` DATETIME(6) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_CashSession_CashRegister` (`CashRegisterId`),
  INDEX `FK_CashSession_CashSessionStatus` (`CashSessionStatusId`),
  INDEX `FK_CashSession_OpenedByEmployee` (`OpenedByEmployeeId`),
  INDEX `FK_CashSession_ClosedByEmployee` (`ClosedByEmployeeId`),
  CONSTRAINT `FK_CashSession_CashRegister` FOREIGN KEY (`CashRegisterId`) REFERENCES `CashRegister` (`Id`),
  CONSTRAINT `FK_CashSession_CashSessionStatus` FOREIGN KEY (`CashSessionStatusId`) REFERENCES `CashSessionStatus` (`Id`),
  CONSTRAINT `FK_CashSession_OpenedByEmployee` FOREIGN KEY (`OpenedByEmployeeId`) REFERENCES `Employee` (`Id`),
  CONSTRAINT `FK_CashSession_ClosedByEmployee` FOREIGN KEY (`ClosedByEmployeeId`) REFERENCES `Employee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Category`
-- -----------------------------------------------------
CREATE TABLE `Category` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `Name` VARCHAR(100) NOT NULL,
  `DisplayOrder` INT NOT NULL DEFAULT 0,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_Category_Company` (`CompanyId`),
  CONSTRAINT `FK_Category_Company` FOREIGN KEY (`CompanyId`) REFERENCES `Company` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `ComandaStatus`
-- -----------------------------------------------------
CREATE TABLE `ComandaStatus` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Comanda`
-- -----------------------------------------------------
CREATE TABLE `Comanda` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `ComandaStatusId` BIGINT NOT NULL,
  `Code` VARCHAR(30) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_Comanda_Branch` (`BranchId`),
  INDEX `FK_Comanda_ComandaStatus` (`ComandaStatusId`),
  CONSTRAINT `FK_Comanda_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_Comanda_ComandaStatus` FOREIGN KEY (`ComandaStatusId`) REFERENCES `ComandaStatus` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `ComandaSetting`
-- -----------------------------------------------------
CREATE TABLE `ComandaSetting` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `DefaultLimitAmount` DECIMAL(18, 2) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_ComandaSetting_Branch` (`BranchId`),
  CONSTRAINT `FK_ComandaSetting_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `CK_ComandaSetting_DefaultLimitAmount` CHECK (`DefaultLimitAmount` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `CostType`
-- -----------------------------------------------------
CREATE TABLE `CostType` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Customer`
-- -----------------------------------------------------
CREATE TABLE `Customer` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `Name` VARCHAR(150) NOT NULL,
  `Phone` VARCHAR(20) NULL,
  `Cpf` CHAR(11) NULL,
  `Email` VARCHAR(150) NULL,
  `LoyaltyPoints` INT NOT NULL DEFAULT 0,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_Customer_Company` (`CompanyId`),
  CONSTRAINT `FK_Customer_Company` FOREIGN KEY (`CompanyId`) REFERENCES `Company` (`Id`),
  CONSTRAINT `CK_Customer_LoyaltyPoints` CHECK (`LoyaltyPoints` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `TableStatus`
-- -----------------------------------------------------
CREATE TABLE `TableStatus` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `DiningTable`
-- -----------------------------------------------------
CREATE TABLE `DiningTable` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `TableStatusId` BIGINT NOT NULL,
  `Number` INT NOT NULL,
  `Capacity` INT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `QrToken` CHAR(36) NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_DiningTable_Branch` (`BranchId`),
  INDEX `FK_DiningTable_TableStatus` (`TableStatusId`),
  CONSTRAINT `FK_DiningTable_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_DiningTable_TableStatus` FOREIGN KEY (`TableStatusId`) REFERENCES `TableStatus` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `OrderStatus`
-- -----------------------------------------------------
CREATE TABLE `OrderStatus` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `CustomerOrder`
-- -----------------------------------------------------
CREATE TABLE `CustomerOrder` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `DiningTableId` BIGINT NULL,
  `ComandaId` BIGINT NULL,
  `EmployeeId` BIGINT NOT NULL,
  `OrderStatusId` BIGINT NOT NULL,
  `GuestCount` INT NULL,
  `OpenedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ClosedAt` DATETIME(6) NULL,
  `SubtotalAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `DiscountAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `ServiceFeeAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `TotalAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `Notes` VARCHAR(500) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CreditLimitAmount` DECIMAL(18, 2) NULL,
  `OrderTypeId` TINYINT NOT NULL DEFAULT 1,
  `CustomerName` VARCHAR(150) NULL,
  `CustomerPhone` VARCHAR(20) NULL,
  `DeliveryAddress` VARCHAR(300) NULL,
  `CustomerId` BIGINT NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_CustomerOrder_Branch` (`BranchId`),
  INDEX `FK_CustomerOrder_DiningTable` (`DiningTableId`),
  INDEX `FK_CustomerOrder_Comanda` (`ComandaId`),
  INDEX `FK_CustomerOrder_Employee` (`EmployeeId`),
  INDEX `FK_CustomerOrder_OrderStatus` (`OrderStatusId`),
  INDEX `FK_CustomerOrder_Customer` (`CustomerId`),
  CONSTRAINT `FK_CustomerOrder_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_CustomerOrder_DiningTable` FOREIGN KEY (`DiningTableId`) REFERENCES `DiningTable` (`Id`),
  CONSTRAINT `FK_CustomerOrder_Comanda` FOREIGN KEY (`ComandaId`) REFERENCES `Comanda` (`Id`),
  CONSTRAINT `FK_CustomerOrder_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `Employee` (`Id`),
  CONSTRAINT `FK_CustomerOrder_OrderStatus` FOREIGN KEY (`OrderStatusId`) REFERENCES `OrderStatus` (`Id`),
  CONSTRAINT `FK_CustomerOrder_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`),
  CONSTRAINT `CK_CustomerOrder_OrderTypeId` CHECK (`OrderTypeId` >= 1 AND `OrderTypeId` <= 3),
  CONSTRAINT `CK_CustomerOrder_Origin` CHECK (`OrderTypeId` <> 1 OR `DiningTableId` IS NOT NULL OR `ComandaId` IS NOT NULL)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `LogTracker`
-- -----------------------------------------------------
CREATE TABLE `LogTracker` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `AppUserId` BIGINT NULL,
  `DirectoryName` VARCHAR(150) NULL,
  `ClassName` VARCHAR(150) NOT NULL,
  `MethodName` VARCHAR(150) NOT NULL,
  `IsSuccess` TINYINT(1) NOT NULL DEFAULT 1,
  `ExecutionTimeMs` BIGINT NULL,
  `Message` TEXT NULL,
  `ErrorMessage` TEXT NULL,
  `StackTrace` TEXT NULL,
  `IpAddress` VARCHAR(45) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_LogTracker_AppUser` (`AppUserId`),
  CONSTRAINT `FK_LogTracker_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `AppUser` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `OperatingCost`
-- -----------------------------------------------------
CREATE TABLE `OperatingCost` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `CostTypeId` BIGINT NOT NULL,
  `Description` VARCHAR(200) NOT NULL,
  `Amount` DECIMAL(18, 2) NOT NULL,
  `ReferenceYear` INT NOT NULL,
  `ReferenceMonth` INT NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_OperatingCost_Branch` (`BranchId`),
  INDEX `FK_OperatingCost_CostType` (`CostTypeId`),
  CONSTRAINT `FK_OperatingCost_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_OperatingCost_CostType` FOREIGN KEY (`CostTypeId`) REFERENCES `CostType` (`Id`),
  CONSTRAINT `CK_OperatingCost_Amount` CHECK (`Amount` > 0),
  CONSTRAINT `CK_OperatingCost_ReferenceMonth` CHECK (`ReferenceMonth` >= 1 AND `ReferenceMonth` <= 12),
  CONSTRAINT `CK_OperatingCost_ReferenceYear` CHECK (`ReferenceYear` >= 2000 AND `ReferenceYear` <= 2100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `UnitOfMeasure`
-- -----------------------------------------------------
CREATE TABLE `UnitOfMeasure` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `Abbreviation` VARCHAR(10) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Product`
-- -----------------------------------------------------
CREATE TABLE `Product` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `CategoryId` BIGINT NOT NULL,
  `UnitOfMeasureId` BIGINT NOT NULL,
  `Name` VARCHAR(150) NOT NULL,
  `Description` VARCHAR(500) NULL,
  `Barcode` VARCHAR(50) NULL,
  `SalePrice` DECIMAL(18, 2) NOT NULL,
  `CostPrice` DECIMAL(18, 2) NULL,
  `IsStockControlled` TINYINT(1) NOT NULL DEFAULT 1,
  `PreparationTimeMinutes` INT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `ImageUrl` VARCHAR(300) NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_Product_Company` (`CompanyId`),
  INDEX `FK_Product_Category` (`CategoryId`),
  INDEX `FK_Product_UnitOfMeasure` (`UnitOfMeasureId`),
  CONSTRAINT `FK_Product_Company` FOREIGN KEY (`CompanyId`) REFERENCES `Company` (`Id`),
  CONSTRAINT `FK_Product_Category` FOREIGN KEY (`CategoryId`) REFERENCES `Category` (`Id`),
  CONSTRAINT `FK_Product_UnitOfMeasure` FOREIGN KEY (`UnitOfMeasureId`) REFERENCES `UnitOfMeasure` (`Id`),
  CONSTRAINT `CK_Product_SalePrice` CHECK (`SalePrice` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `OrderItemStatus`
-- -----------------------------------------------------
CREATE TABLE `OrderItemStatus` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `OrderItem`
-- -----------------------------------------------------
CREATE TABLE `OrderItem` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CustomerOrderId` BIGINT NOT NULL,
  `ProductId` BIGINT NOT NULL,
  `OrderItemStatusId` BIGINT NOT NULL,
  `EmployeeId` BIGINT NULL,
  `Quantity` DECIMAL(18, 3) NOT NULL,
  `UnitPrice` DECIMAL(18, 2) NOT NULL,
  `DiscountAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `TotalAmount` DECIMAL(18, 2) NOT NULL,
  `Notes` VARCHAR(300) NULL,
  `SentToKitchenAt` DATETIME(6) NULL,
  `DeliveredAt` DATETIME(6) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CancelledByEmployeeId` BIGINT NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_OrderItem_CustomerOrder` (`CustomerOrderId`),
  INDEX `FK_OrderItem_Product` (`ProductId`),
  INDEX `FK_OrderItem_OrderItemStatus` (`OrderItemStatusId`),
  INDEX `FK_OrderItem_Employee` (`EmployeeId`),
  INDEX `FK_OrderItem_CancelledByEmployee` (`CancelledByEmployeeId`),
  CONSTRAINT `FK_OrderItem_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrder` (`Id`),
  CONSTRAINT `FK_OrderItem_Product` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`),
  CONSTRAINT `FK_OrderItem_OrderItemStatus` FOREIGN KEY (`OrderItemStatusId`) REFERENCES `OrderItemStatus` (`Id`),
  CONSTRAINT `FK_OrderItem_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `Employee` (`Id`),
  CONSTRAINT `FK_OrderItem_CancelledByEmployee` FOREIGN KEY (`CancelledByEmployeeId`) REFERENCES `Employee` (`Id`),
  CONSTRAINT `CK_OrderItem_Quantity` CHECK (`Quantity` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `PaymentMethod`
-- -----------------------------------------------------
CREATE TABLE `PaymentMethod` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `AllowsChange` TINYINT(1) NOT NULL DEFAULT 0,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `OrderPartialPayment`
-- -----------------------------------------------------
CREATE TABLE `OrderPartialPayment` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CustomerOrderId` BIGINT NOT NULL,
  `CashSessionId` BIGINT NOT NULL,
  `PaymentMethodId` BIGINT NOT NULL,
  `EmployeeId` BIGINT NOT NULL,
  `Amount` DECIMAL(18, 2) NOT NULL,
  `AuthorizationCode` VARCHAR(100) NULL,
  `PayerName` VARCHAR(100) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_OrderPartialPayment_CustomerOrder` (`CustomerOrderId`),
  INDEX `FK_OrderPartialPayment_CashSession` (`CashSessionId`),
  INDEX `FK_OrderPartialPayment_PaymentMethod` (`PaymentMethodId`),
  INDEX `FK_OrderPartialPayment_Employee` (`EmployeeId`),
  CONSTRAINT `FK_OrderPartialPayment_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrder` (`Id`),
  CONSTRAINT `FK_OrderPartialPayment_CashSession` FOREIGN KEY (`CashSessionId`) REFERENCES `CashSession` (`Id`),
  CONSTRAINT `FK_OrderPartialPayment_PaymentMethod` FOREIGN KEY (`PaymentMethodId`) REFERENCES `PaymentMethod` (`Id`),
  CONSTRAINT `FK_OrderPartialPayment_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `Employee` (`Id`),
  CONSTRAINT `CK_OrderPartialPayment_Amount` CHECK (`Amount` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Permission`
-- -----------------------------------------------------
CREATE TABLE `Permission` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Code` VARCHAR(100) NOT NULL,
  `Name` VARCHAR(150) NOT NULL,
  `ModuleName` VARCHAR(100) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Printer`
-- -----------------------------------------------------
CREATE TABLE `Printer` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `Name` VARCHAR(100) NOT NULL,
  `ConnectionType` INT NOT NULL,
  `PrinterName` VARCHAR(200) NULL,
  `IpAddress` VARCHAR(45) NULL,
  `Port` INT NULL,
  `PrintsOrders` TINYINT(1) NOT NULL DEFAULT 0,
  `PrintsBills` TINYINT(1) NOT NULL DEFAULT 0,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_Printer_Branch` (`BranchId`),
  CONSTRAINT `FK_Printer_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `CK_Printer_ConnectionType` CHECK (`ConnectionType` IN (1, 2)),
  CONSTRAINT `CK_Printer_Target` CHECK ((`ConnectionType` = 1 AND `PrinterName` IS NOT NULL) OR (`ConnectionType` = 2 AND `IpAddress` IS NOT NULL AND `Port` IS NOT NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `PrinterSetting`
-- -----------------------------------------------------
CREATE TABLE `PrinterSetting` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `PrintOrdersEnabled` TINYINT(1) NOT NULL DEFAULT 1,
  `PrintBillsEnabled` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_PrinterSetting_Branch` (`BranchId`),
  CONSTRAINT `FK_PrinterSetting_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `ProductStock`
-- -----------------------------------------------------
CREATE TABLE `ProductStock` (
  `ProductId` BIGINT NOT NULL,
  `CurrentBalance` DECIMAL(18, 3) NOT NULL DEFAULT 0,
  `MinimumQuantity` DECIMAL(18, 3) NOT NULL DEFAULT 0,
  `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`ProductId`),
  CONSTRAINT `FK_ProductStock_Product` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `PromotionType`
-- -----------------------------------------------------
CREATE TABLE `PromotionType` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Promotion`
-- -----------------------------------------------------
CREATE TABLE `Promotion` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `ProductId` BIGINT NOT NULL,
  `Name` VARCHAR(150) NOT NULL,
  `DayOfWeek` INT NOT NULL,
  `StartMinuteOfDay` INT NOT NULL,
  `EndMinuteOfDay` INT NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `PromotionTypeId` BIGINT NOT NULL DEFAULT 1,
  `DiscountRate` DECIMAL(5, 4) NULL,
  PRIMARY KEY (`Id`),
  INDEX `FK_Promotion_Branch` (`BranchId`),
  INDEX `FK_Promotion_Product` (`ProductId`),
  INDEX `FK_Promotion_PromotionType` (`PromotionTypeId`),
  CONSTRAINT `FK_Promotion_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_Promotion_Product` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`),
  CONSTRAINT `FK_Promotion_PromotionType` FOREIGN KEY (`PromotionTypeId`) REFERENCES `PromotionType` (`Id`),
  CONSTRAINT `CK_Promotion_DayOfWeek` CHECK (`DayOfWeek` >= 0 AND `DayOfWeek` <= 6),
  CONSTRAINT `CK_Promotion_DiscountRate` CHECK (`PromotionTypeId` <> 2 OR (`DiscountRate` IS NOT NULL AND `DiscountRate` > 0 AND `DiscountRate` < 1)),
  CONSTRAINT `CK_Promotion_EndMinute` CHECK (`EndMinuteOfDay` >= 1 AND `EndMinuteOfDay` <= 1440),
  CONSTRAINT `CK_Promotion_StartMinute` CHECK (`StartMinuteOfDay` >= 0 AND `StartMinuteOfDay` <= 1439),
  CONSTRAINT `CK_Promotion_Window` CHECK (`StartMinuteOfDay` < `EndMinuteOfDay`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Supplier`
-- -----------------------------------------------------
CREATE TABLE `Supplier` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `LegalName` VARCHAR(200) NOT NULL,
  `TradeName` VARCHAR(150) NULL,
  `Cnpj` CHAR(14) NULL,
  `Email` VARCHAR(150) NULL,
  `Phone` VARCHAR(20) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_Supplier_Company` (`CompanyId`),
  CONSTRAINT `FK_Supplier_Company` FOREIGN KEY (`CompanyId`) REFERENCES `Company` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Purchase`
-- -----------------------------------------------------
CREATE TABLE `Purchase` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `SupplierId` BIGINT NOT NULL,
  `DocumentNumber` VARCHAR(50) NULL,
  `PurchasedAt` DATETIME(6) NOT NULL,
  `TotalAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `Notes` VARCHAR(500) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_Purchase_Branch` (`BranchId`),
  INDEX `FK_Purchase_Supplier` (`SupplierId`),
  CONSTRAINT `FK_Purchase_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_Purchase_Supplier` FOREIGN KEY (`SupplierId`) REFERENCES `Supplier` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `PurchaseItem`
-- -----------------------------------------------------
CREATE TABLE `PurchaseItem` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `PurchaseId` BIGINT NOT NULL,
  `ProductId` BIGINT NOT NULL,
  `Quantity` DECIMAL(18, 3) NOT NULL,
  `UnitCost` DECIMAL(18, 2) NOT NULL,
  `TotalCost` DECIMAL(18, 2) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_PurchaseItem_Purchase` (`PurchaseId`),
  INDEX `FK_PurchaseItem_Product` (`ProductId`),
  CONSTRAINT `FK_PurchaseItem_Purchase` FOREIGN KEY (`PurchaseId`) REFERENCES `Purchase` (`Id`),
  CONSTRAINT `FK_PurchaseItem_Product` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`),
  CONSTRAINT `CK_PurchaseItem_Quantity` CHECK (`Quantity` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `RefreshToken`
-- -----------------------------------------------------
CREATE TABLE `RefreshToken` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `AppUserId` BIGINT NOT NULL,
  `Token` VARCHAR(500) NOT NULL,
  `ExpiresAt` DATETIME(6) NOT NULL,
  `RevokedAt` DATETIME(6) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_RefreshToken_AppUser` (`AppUserId`),
  CONSTRAINT `FK_RefreshToken_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `AppUser` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `RevenueTarget`
-- -----------------------------------------------------
CREATE TABLE `RevenueTarget` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `ReferenceYear` INT NOT NULL,
  `ReferenceMonth` INT NOT NULL,
  `TargetAmount` DECIMAL(18, 2) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_RevenueTarget_Branch` (`BranchId`),
  CONSTRAINT `FK_RevenueTarget_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `CK_RevenueTarget_ReferenceMonth` CHECK (`ReferenceMonth` >= 1 AND `ReferenceMonth` <= 12),
  CONSTRAINT `CK_RevenueTarget_ReferenceYear` CHECK (`ReferenceYear` >= 2000 AND `ReferenceYear` <= 2100),
  CONSTRAINT `CK_RevenueTarget_TargetAmount` CHECK (`TargetAmount` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Role`
-- -----------------------------------------------------
CREATE TABLE `Role` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `Name` VARCHAR(100) NOT NULL,
  `Description` VARCHAR(300) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_Role_Company` (`CompanyId`),
  CONSTRAINT `FK_Role_Company` FOREIGN KEY (`CompanyId`) REFERENCES `Company` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `RolePermission`
-- -----------------------------------------------------
CREATE TABLE `RolePermission` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `RoleId` BIGINT NOT NULL,
  `PermissionId` BIGINT NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_RolePermission_Role` (`RoleId`),
  INDEX `FK_RolePermission_Permission` (`PermissionId`),
  CONSTRAINT `FK_RolePermission_Role` FOREIGN KEY (`RoleId`) REFERENCES `Role` (`Id`),
  CONSTRAINT `FK_RolePermission_Permission` FOREIGN KEY (`PermissionId`) REFERENCES `Permission` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `Sale`
-- -----------------------------------------------------
CREATE TABLE `Sale` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `CustomerOrderId` BIGINT NOT NULL,
  `CashSessionId` BIGINT NOT NULL,
  `EmployeeId` BIGINT NOT NULL,
  `SaleNumber` BIGINT NOT NULL,
  `SubtotalAmount` DECIMAL(18, 2) NOT NULL,
  `DiscountAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `ServiceFeeAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0,
  `TotalAmount` DECIMAL(18, 2) NOT NULL,
  `SoldAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_Sale_Branch` (`BranchId`),
  INDEX `FK_Sale_CustomerOrder` (`CustomerOrderId`),
  INDEX `FK_Sale_CashSession` (`CashSessionId`),
  INDEX `FK_Sale_Employee` (`EmployeeId`),
  CONSTRAINT `FK_Sale_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_Sale_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrder` (`Id`),
  CONSTRAINT `FK_Sale_CashSession` FOREIGN KEY (`CashSessionId`) REFERENCES `CashSession` (`Id`),
  CONSTRAINT `FK_Sale_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `Employee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `SalePayment`
-- -----------------------------------------------------
CREATE TABLE `SalePayment` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `SaleId` BIGINT NOT NULL,
  `PaymentMethodId` BIGINT NOT NULL,
  `Amount` DECIMAL(18, 2) NOT NULL,
  `ChangeAmount` DECIMAL(18, 2) NULL,
  `AuthorizationCode` VARCHAR(100) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_SalePayment_Sale` (`SaleId`),
  INDEX `FK_SalePayment_PaymentMethod` (`PaymentMethodId`),
  CONSTRAINT `FK_SalePayment_Sale` FOREIGN KEY (`SaleId`) REFERENCES `Sale` (`Id`),
  CONSTRAINT `FK_SalePayment_PaymentMethod` FOREIGN KEY (`PaymentMethodId`) REFERENCES `PaymentMethod` (`Id`),
  CONSTRAINT `CK_SalePayment_Amount` CHECK (`Amount` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `ServiceFeeSetting`
-- -----------------------------------------------------
CREATE TABLE `ServiceFeeSetting` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `Enabled` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_ServiceFeeSetting_Branch` (`BranchId`),
  CONSTRAINT `FK_ServiceFeeSetting_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `StockItem`
-- -----------------------------------------------------
CREATE TABLE `StockItem` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `ProductId` BIGINT NOT NULL,
  `CurrentQuantity` DECIMAL(18, 3) NOT NULL DEFAULT 0,
  `MinimumQuantity` DECIMAL(18, 3) NOT NULL DEFAULT 0,
  `MaximumQuantity` DECIMAL(18, 3) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_StockItem_Branch` (`BranchId`),
  INDEX `FK_StockItem_Product` (`ProductId`),
  CONSTRAINT `FK_StockItem_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_StockItem_Product` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `StockMovementType`
-- -----------------------------------------------------
CREATE TABLE `StockMovementType` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `IsInflow` TINYINT(1) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `StockMovement`
-- -----------------------------------------------------
CREATE TABLE `StockMovement` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `StockItemId` BIGINT NOT NULL,
  `StockMovementTypeId` BIGINT NOT NULL,
  `PurchaseItemId` BIGINT NULL,
  `OrderItemId` BIGINT NULL,
  `EmployeeId` BIGINT NULL,
  `Quantity` DECIMAL(18, 3) NOT NULL,
  `UnitCost` DECIMAL(18, 2) NULL,
  `TotalCost` DECIMAL(18, 2) NULL,
  `DocumentNumber` VARCHAR(50) NULL,
  `MovedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `Notes` VARCHAR(300) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_StockMovement_StockItem` (`StockItemId`),
  INDEX `FK_StockMovement_StockMovementType` (`StockMovementTypeId`),
  INDEX `FK_StockMovement_PurchaseItem` (`PurchaseItemId`),
  INDEX `FK_StockMovement_OrderItem` (`OrderItemId`),
  INDEX `FK_StockMovement_Employee` (`EmployeeId`),
  CONSTRAINT `FK_StockMovement_StockItem` FOREIGN KEY (`StockItemId`) REFERENCES `StockItem` (`Id`),
  CONSTRAINT `FK_StockMovement_StockMovementType` FOREIGN KEY (`StockMovementTypeId`) REFERENCES `StockMovementType` (`Id`),
  CONSTRAINT `FK_StockMovement_PurchaseItem` FOREIGN KEY (`PurchaseItemId`) REFERENCES `PurchaseItem` (`Id`),
  CONSTRAINT `FK_StockMovement_OrderItem` FOREIGN KEY (`OrderItemId`) REFERENCES `OrderItem` (`Id`),
  CONSTRAINT `FK_StockMovement_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `Employee` (`Id`),
  CONSTRAINT `CK_StockMovement_Quantity` CHECK (`Quantity` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `TableReservation`
-- -----------------------------------------------------
CREATE TABLE `TableReservation` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `BranchId` BIGINT NOT NULL,
  `DiningTableId` BIGINT NULL,
  `CustomerName` VARCHAR(150) NOT NULL,
  `CustomerPhone` VARCHAR(20) NULL,
  `PartySize` INT NOT NULL,
  `ReservedFor` DATETIME(6) NOT NULL,
  `ReservationStatusId` TINYINT NOT NULL DEFAULT 1,
  `Notes` VARCHAR(500) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_TableReservation_Branch` (`BranchId`),
  INDEX `FK_TableReservation_DiningTable` (`DiningTableId`),
  CONSTRAINT `FK_TableReservation_Branch` FOREIGN KEY (`BranchId`) REFERENCES `Branch` (`Id`),
  CONSTRAINT `FK_TableReservation_DiningTable` FOREIGN KEY (`DiningTableId`) REFERENCES `DiningTable` (`Id`),
  CONSTRAINT `CK_TableReservation_PartySize` CHECK (`PartySize` > 0),
  CONSTRAINT `CK_TableReservation_Status` CHECK (`ReservationStatusId` >= 1 AND `ReservationStatusId` <= 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `UserRole`
-- -----------------------------------------------------
CREATE TABLE `UserRole` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CompanyId` BIGINT NOT NULL,
  `Id_UserRole` INT NULL,
  `AppUserId` BIGINT NOT NULL,
  `RoleId` BIGINT NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_UserRole_AppUser` (`AppUserId`),
  INDEX `FK_UserRole_Role` (`RoleId`),
  CONSTRAINT `FK_UserRole_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `AppUser` (`Id`),
  CONSTRAINT `FK_UserRole_Role` FOREIGN KEY (`RoleId`) REFERENCES `Role` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `CashMovement`
-- -----------------------------------------------------
CREATE TABLE `CashMovement` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CashSessionId` BIGINT NOT NULL,
  `CashMovementTypeId` BIGINT NOT NULL,
  `SaleId` BIGINT NULL,
  `EmployeeId` BIGINT NOT NULL,
  `Amount` DECIMAL(18, 2) NOT NULL,
  `Description` VARCHAR(300) NULL,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  INDEX `FK_CashMovement_CashSession` (`CashSessionId`),
  INDEX `FK_CashMovement_CashMovementType` (`CashMovementTypeId`),
  INDEX `FK_CashMovement_Sale` (`SaleId`),
  INDEX `FK_CashMovement_Employee` (`EmployeeId`),
  CONSTRAINT `FK_CashMovement_CashSession` FOREIGN KEY (`CashSessionId`) REFERENCES `CashSession` (`Id`),
  CONSTRAINT `FK_CashMovement_CashMovementType` FOREIGN KEY (`CashMovementTypeId`) REFERENCES `CashMovementType` (`Id`),
  CONSTRAINT `FK_CashMovement_Sale` FOREIGN KEY (`SaleId`) REFERENCES `Sale` (`Id`),
  CONSTRAINT `FK_CashMovement_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `Employee` (`Id`),
  CONSTRAINT `CK_CashMovement_Amount` CHECK (`Amount` > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
