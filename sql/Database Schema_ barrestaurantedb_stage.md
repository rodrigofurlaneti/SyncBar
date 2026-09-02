# **Database Schema: barrestaurantedb\_stage**

This document details the complete database schema for barrestaurantedb\_stage, incorporating a multi-tenant (Company) and multi-store (Branch) architecture. It uses the "Global \+ Exclusive" hybrid model for the product catalog, where setting BranchId to NULL means the item is available network-wide, while specifying a BranchId restricts it to that particular store.

## **Core Architecture & Identity**

### **company**

The root tenant. All catalog items, customers, and global settings belong to a company.  
``CREATE TABLE `company` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `LegalName` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `TradeName` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Cnpj` char(14) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Email` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `Phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **jobtitle**

Roles within a company, used for permission scoping.  
``CREATE TABLE `jobtitle` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CompanyId` bigint NOT NULL, ``  
  `` `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_JobTitle_Company` (`CompanyId`),``  
  ``CONSTRAINT `FK_JobTitle_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **employee**

Staff members assigned to a specific branch.  
``CREATE TABLE `employee` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `BranchId` bigint NOT NULL, ``  
  `` `JobTitleId` bigint NOT NULL, ``  
  `` `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Cpf` char(11) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Email` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `Phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `HiredAt` datetime(6) NOT NULL, ``  
  `` `DismissedAt` datetime(6) DEFAULT NULL, ``  
  `` `Salary` decimal(18,2) DEFAULT NULL, ``  
  `` `CommissionPercent` decimal(5,2) DEFAULT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_Employee_Branch` (`BranchId`),``  
  ``KEY `FK_Employee_JobTitle` (`JobTitleId`),``  
  ``CONSTRAINT `FK_Employee_JobTitle` FOREIGN KEY (`JobTitleId`) REFERENCES `jobtitle` (`Id`),``  
  ``CONSTRAINT `CK_Employee_CommissionPercent` CHECK (((`CommissionPercent` is null) or ((`CommissionPercent` >= 0) and (`CommissionPercent` <= 100))))``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **branch**

Physical store locations belonging to a company.  
``CREATE TABLE `branch` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CompanyId` bigint NOT NULL, ``  
  `` `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Cnpj` char(14) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `Phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `AddressStreet` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `AddressNumber` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `AddressDistrict` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `AddressCity` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `AddressState` char(2) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `AddressZipCode` char(8) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `SelfServiceEmployeeId` bigint DEFAULT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_Branch_Company` (`CompanyId`),``  
  ``KEY `FK_Branch_SelfServiceEmployee` (`SelfServiceEmployeeId`),``  
  ``CONSTRAINT `FK_Branch_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),``  
  ``CONSTRAINT `FK_Branch_SelfServiceEmployee` FOREIGN KEY (`SelfServiceEmployeeId`) REFERENCES `employee` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

*Note: Add the foreign key for employee.BranchId after creating the branch table:*  
`` ALTER TABLE `employee` ``  
``ADD CONSTRAINT `FK_Employee_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`);``

### **appuser**

System access credentials.  
``CREATE TABLE `appuser` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CompanyId` bigint NOT NULL, ``  
  `` `EmployeeId` bigint DEFAULT NULL, ``  
  `` `UserName` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Email` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `PasswordHash` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `PasswordSalt` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `FailedAccessCount` int NOT NULL DEFAULT '0', ``  
  `` `LockoutEndAt` datetime(6) DEFAULT NULL, ``  
  `` `LastLoginAt` datetime(6) DEFAULT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_AppUser_Company` (`CompanyId`),``  
  ``KEY `FK_AppUser_Employee` (`EmployeeId`),``  
  ``CONSTRAINT `FK_AppUser_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),``  
  ``CONSTRAINT `FK_AppUser_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **customer**

Client database, shared across the entire company network.  
``CREATE TABLE `customer` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CompanyId` bigint NOT NULL, ``  
  `` `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Phone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `Cpf` char(11) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `Email` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `LoyaltyPoints` int NOT NULL DEFAULT '0', ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_Customer_Company` (`CompanyId`),``  
  ``CONSTRAINT `FK_Customer_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),``  
  ``CONSTRAINT `CK_Customer_LoyaltyPoints` CHECK ((`LoyaltyPoints` >= 0))``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

## **Product Catalog (Multi-Store Support)**

These tables use the BranchId nullable approach. If BranchId is null, the item is global. If populated, it is exclusive to that branch.

### **category**

``CREATE TABLE `category` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CompanyId` bigint NOT NULL, ``  
  `` `BranchId` bigint DEFAULT NULL, ``  
  `` `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `DisplayOrder` int NOT NULL DEFAULT '0', ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_Category_Company` (`CompanyId`),``  
  ``KEY `FK_Category_Branch` (`BranchId`),``  
  ``CONSTRAINT `FK_Category_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),``  
  ``CONSTRAINT `FK_Category_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **product**

``CREATE TABLE `product` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CompanyId` bigint NOT NULL, ``  
  `` `BranchId` bigint DEFAULT NULL, ``  
  `` `CategoryId` bigint NOT NULL, ``  
  `` `Name` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Description` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `BasePrice` decimal(18,2) NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_Product_Company` (`CompanyId`),``  
  ``KEY `FK_Product_Branch` (`BranchId`),``  
  ``KEY `FK_Product_Category` (`CategoryId`),``  
  ``CONSTRAINT `FK_Product_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),``  
  ``CONSTRAINT `FK_Product_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),``  
  ``CONSTRAINT `FK_Product_Category` FOREIGN KEY (`CategoryId`) REFERENCES `category` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **complementgroup**

``CREATE TABLE `complementgroup` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CompanyId` bigint NOT NULL, ``  
  `` `BranchId` bigint DEFAULT NULL, ``  
  `` `Name` varchar(150) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL, ``  
  `` `ComplementGroupTypeId` tinyint NOT NULL, ``  
  `` `MinSelection` int NOT NULL, ``  
  `` `MaxSelection` int NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL, ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL, ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `IX_ComplementGroup_CompanyId` (`CompanyId`),``  
  ``KEY `IX_ComplementGroup_BranchId` (`BranchId`),``  
  ``CONSTRAINT `FK_ComplementGroup_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),``  
  ``CONSTRAINT `FK_ComplementGroup_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),``  
  ``CONSTRAINT `CK_ComplementGroup_TypeId` CHECK ((`ComplementGroupTypeId` between 1 and 4))``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;`

### **complementitem**

``CREATE TABLE `complementitem` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CompanyId` bigint NOT NULL, ``  
  `` `BranchId` bigint DEFAULT NULL, ``  
  `` `Name` varchar(150) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL, ``  
  `` `LinkedProductId` bigint DEFAULT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL, ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL, ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `IX_ComplementItem_CompanyId` (`CompanyId`),``  
  ``KEY `IX_ComplementItem_BranchId` (`BranchId`),``  
  ``KEY `IX_ComplementItem_LinkedProductId` (`LinkedProductId`),``  
  ``CONSTRAINT `FK_ComplementItem_Company` FOREIGN KEY (`CompanyId`) REFERENCES `company` (`Id`),``  
  ``CONSTRAINT `FK_ComplementItem_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),``  
  ``CONSTRAINT `FK_ComplementItem_LinkedProduct` FOREIGN KEY (`LinkedProductId`) REFERENCES `product` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;`

### **complement**

Mapping table linking items to groups.  
``CREATE TABLE `complement` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `ComplementGroupId` bigint NOT NULL, ``  
  `` `ComplementItemId` bigint NOT NULL, ``  
  `` `ExtraPrice` decimal(18,2) NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL, ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL, ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `IX_Complement_ComplementGroupId` (`ComplementGroupId`),``  
  ``KEY `IX_Complement_ComplementItemId` (`ComplementItemId`),``  
  ``CONSTRAINT `FK_Complement_ComplementGroup` FOREIGN KEY (`ComplementGroupId`) REFERENCES `complementgroup` (`Id`) ON DELETE CASCADE,``  
  ``CONSTRAINT `FK_Complement_ComplementItem` FOREIGN KEY (`ComplementItemId`) REFERENCES `complementitem` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;`

## **Front-of-House & Operations (Branch Isolated)**

### **diningarea & diningtable**

``CREATE TABLE `diningarea` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `BranchId` bigint NOT NULL, ``  
  `` `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `IX_DiningArea_BranchId` (`BranchId`),``  
  ``CONSTRAINT `FK_DiningArea_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `tablestatus` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `diningtable` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `BranchId` bigint NOT NULL, ``  
  `` `TableStatusId` bigint NOT NULL, ``  
  `` `Number` int NOT NULL, ``  
  `` `Capacity` int DEFAULT NULL, ``  
  `` `QrToken` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `IsQrViewEnabled` tinyint(1) NOT NULL DEFAULT '1', ``  
  `` `IsCameraInputEnabled` tinyint(1) NOT NULL DEFAULT '0', ``  
  `` `IsBarcodeEnabled` tinyint(1) NOT NULL DEFAULT '0', ``  
  `` `IsQrCodeEnabled` tinyint(1) NOT NULL DEFAULT '0', ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_DiningTable_Branch` (`BranchId`),``  
  ``KEY `FK_DiningTable_TableStatus` (`TableStatusId`),``  
  ``CONSTRAINT `FK_DiningTable_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),``  
  ``CONSTRAINT `FK_DiningTable_TableStatus` FOREIGN KEY (`TableStatusId`) REFERENCES `tablestatus` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `diningareatable` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `DiningAreaId` bigint NOT NULL, ``  
  `` `DiningTableId` bigint NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``UNIQUE KEY `UK_DiningAreaTable_Table` (`DiningTableId`),``  
  ``KEY `IX_DiningAreaTable_DiningAreaId` (`DiningAreaId`),``  
  ``CONSTRAINT `FK_DiningAreaTable_DiningArea` FOREIGN KEY (`DiningAreaId`) REFERENCES `diningarea` (`Id`),``  
  ``CONSTRAINT `FK_DiningAreaTable_DiningTable` FOREIGN KEY (`DiningTableId`) REFERENCES `diningtable` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **comanda**

``CREATE TABLE `comandastatus` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `comandasetting` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `BranchId` bigint NOT NULL, ``  
  `` `DefaultLimitAmount` decimal(18,2) NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_ComandaSetting_Branch` (`BranchId`),``  
  ``CONSTRAINT `FK_ComandaSetting_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),``  
  ``CONSTRAINT `CK_ComandaSetting_DefaultLimitAmount` CHECK ((`DefaultLimitAmount` > 0))``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `comanda` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `BranchId` bigint NOT NULL, ``  
  `` `ComandaStatusId` bigint NOT NULL, ``  
  `` `Code` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_Comanda_Branch` (`BranchId`),``  
  ``KEY `FK_Comanda_ComandaStatus` (`ComandaStatusId`),``  
  ``CONSTRAINT `FK_Comanda_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),``  
  ``CONSTRAINT `FK_Comanda_ComandaStatus` FOREIGN KEY (`ComandaStatusId`) REFERENCES `comandastatus` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **customerorder**

``CREATE TABLE `orderstatus` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `customerorder` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `BranchId` bigint NOT NULL, ``  
  `` `DiningTableId` bigint DEFAULT NULL, ``  
  `` `ComandaId` bigint DEFAULT NULL, ``  
  `` `EmployeeId` bigint NOT NULL, ``  
  `` `OrderStatusId` bigint NOT NULL, ``  
  `` `CustomerId` bigint DEFAULT NULL, ``  
  `` `OrderTypeId` tinyint NOT NULL DEFAULT '1', ``  
  `` `GuestCount` int DEFAULT NULL, ``  
  `` `SubtotalAmount` decimal(18,2) NOT NULL DEFAULT '0.00', ``  
  `` `DiscountAmount` decimal(18,2) NOT NULL DEFAULT '0.00', ``  
  `` `ServiceFeeAmount` decimal(18,2) NOT NULL DEFAULT '0.00', ``  
  `` `TotalAmount` decimal(18,2) NOT NULL DEFAULT '0.00', ``  
  `` `CreditLimitAmount` decimal(18,2) DEFAULT NULL, ``  
  `` `CustomerName` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `CustomerPhone` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `DeliveryAddress` varchar(300) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `Notes` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `OpenedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `ClosedAt` datetime(6) DEFAULT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_CustomerOrder_Branch` (`BranchId`),``  
  ``KEY `FK_CustomerOrder_DiningTable` (`DiningTableId`),``  
  ``KEY `FK_CustomerOrder_Comanda` (`ComandaId`),``  
  ``KEY `FK_CustomerOrder_Employee` (`EmployeeId`),``  
  ``KEY `FK_CustomerOrder_OrderStatus` (`OrderStatusId`),``  
  ``KEY `FK_CustomerOrder_Customer` (`CustomerId`),``  
  ``KEY `IX_CustomerOrder_CreatedAt_Status` (`CreatedAt`,`OrderStatusId`),``  
  ``CONSTRAINT `FK_CustomerOrder_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`),``  
  ``CONSTRAINT `FK_CustomerOrder_Comanda` FOREIGN KEY (`ComandaId`) REFERENCES `comanda` (`Id`),``  
  ``CONSTRAINT `FK_CustomerOrder_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`),``  
  ``CONSTRAINT `FK_CustomerOrder_DiningTable` FOREIGN KEY (`DiningTableId`) REFERENCES `diningtable` (`Id`),``  
  ``CONSTRAINT `FK_CustomerOrder_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`),``  
  ``CONSTRAINT `FK_CustomerOrder_OrderStatus` FOREIGN KEY (`OrderStatusId`) REFERENCES `orderstatus` (`Id`),``  
  ``CONSTRAINT `CK_CustomerOrder_OrderTypeId` CHECK (((`OrderTypeId` >= 1) and (`OrderTypeId` <= 3))),``  
  ``CONSTRAINT `CK_CustomerOrder_Origin` CHECK (((`OrderTypeId` <> 1) or (`DiningTableId` is not null) or (`ComandaId` is not null)))``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

## **Cash & Financial Management**

### **cashregister & cashsession**

``CREATE TABLE `cashregister` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `BranchId` bigint NOT NULL, ``  
  `` `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_CashRegister_Branch` (`BranchId`),``  
  ``CONSTRAINT `FK_CashRegister_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `cashsessionstatus` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `cashsession` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CashRegisterId` bigint NOT NULL, ``  
  `` `CashSessionStatusId` bigint NOT NULL, ``  
  `` `OpenedByEmployeeId` bigint NOT NULL, ``  
  `` `ClosedByEmployeeId` bigint DEFAULT NULL, ``  
  `` `OpeningAmount` decimal(18,2) NOT NULL DEFAULT '0.00', ``  
  `` `ClosingAmount` decimal(18,2) DEFAULT NULL, ``  
  `` `ExpectedAmount` decimal(18,2) DEFAULT NULL, ``  
  `` `DifferenceAmount` decimal(18,2) DEFAULT NULL, ``  
  `` `OpenedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `ClosedAt` datetime(6) DEFAULT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_CashSession_CashRegister` (`CashRegisterId`),``  
  ``KEY `FK_CashSession_CashSessionStatus` (`CashSessionStatusId`),``  
  ``KEY `FK_CashSession_OpenedByEmployee` (`OpenedByEmployeeId`),``  
  ``KEY `FK_CashSession_ClosedByEmployee` (`ClosedByEmployeeId`),``  
  ``KEY `IX_CashSession_OpenedAt` (`OpenedAt`),``  
  ``CONSTRAINT `FK_CashSession_CashRegister` FOREIGN KEY (`CashRegisterId`) REFERENCES `cashregister` (`Id`),``  
  ``CONSTRAINT `FK_CashSession_CashSessionStatus` FOREIGN KEY (`CashSessionStatusId`) REFERENCES `cashsessionstatus` (`Id`),``  
  ``CONSTRAINT `FK_CashSession_ClosedByEmployee` FOREIGN KEY (`ClosedByEmployeeId`) REFERENCES `employee` (`Id`),``  
  ``CONSTRAINT `FK_CashSession_OpenedByEmployee` FOREIGN KEY (`OpenedByEmployeeId`) REFERENCES `employee` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

### **cashmovement**

``CREATE TABLE `cashmovementtype` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `Name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `IsInflow` tinyint(1) NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `sale` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CustomerOrderId` bigint NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``CONSTRAINT `FK_Sale_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `customerorder` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `cashmovement` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `CashSessionId` bigint NOT NULL, ``  
  `` `CashMovementTypeId` bigint NOT NULL, ``  
  `` `SaleId` bigint DEFAULT NULL, ``  
  `` `EmployeeId` bigint NOT NULL, ``  
  `` `Amount` decimal(18,2) NOT NULL, ``  
  `` `Description` varchar(300) COLLATE utf8mb4_unicode_ci DEFAULT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_CashMovement_CashSession` (`CashSessionId`),``  
  ``KEY `FK_CashMovement_CashMovementType` (`CashMovementTypeId`),``  
  ``KEY `FK_CashMovement_Sale` (`SaleId`),``  
  ``KEY `FK_CashMovement_Employee` (`EmployeeId`),``  
  ``KEY `IX_CashMovement_CreatedAt` (`CreatedAt`),``  
  ``CONSTRAINT `FK_CashMovement_CashMovementType` FOREIGN KEY (`CashMovementTypeId`) REFERENCES `cashmovementtype` (`Id`),``  
  ``CONSTRAINT `FK_CashMovement_CashSession` FOREIGN KEY (`CashSessionId`) REFERENCES `cashsession` (`Id`),``  
  ``CONSTRAINT `FK_CashMovement_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`),``  
  ``CONSTRAINT `FK_CashMovement_Sale` FOREIGN KEY (`SaleId`) REFERENCES `sale` (`Id`),``  
  ``CONSTRAINT `CK_CashMovement_Amount` CHECK ((`Amount` > 0))``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

## **Features & Security**

### **appfeature**

``CREATE TABLE `appfeature` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `Code` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `Name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `appuserfeature` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `AppUserId` bigint NOT NULL, ``  
  `` `AppFeatureId` bigint NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_AppUserFeature_AppUser` (`AppUserId`),``  
  ``KEY `FK_AppUserFeature_AppFeature` (`AppFeatureId`),``  
  ``CONSTRAINT `FK_AppUserFeature_AppFeature` FOREIGN KEY (`AppFeatureId`) REFERENCES `appfeature` (`Id`),``  
  ``CONSTRAINT `FK_AppUserFeature_AppUser` FOREIGN KEY (`AppUserId`) REFERENCES `appuser` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`

``CREATE TABLE `jobtitlefeature` (``  
  `` `Id` bigint NOT NULL AUTO_INCREMENT, ``  
  `` `JobTitleId` bigint NOT NULL, ``  
  `` `AppFeatureId` bigint NOT NULL, ``  
  `` `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6), ``  
  `` `UpdatedAt` datetime(6) DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6), ``  
  `` `IsActive` tinyint(1) NOT NULL DEFAULT '1', ``  
  ``PRIMARY KEY (`Id`),``  
  ``KEY `FK_JobTitleFeature_JobTitle` (`JobTitleId`),``  
  ``KEY `FK_JobTitleFeature_AppFeature` (`AppFeatureId`),``  
  ``CONSTRAINT `FK_JobTitleFeature_AppFeature` FOREIGN KEY (`AppFeatureId`) REFERENCES `appfeature` (`Id`),``  
  ``CONSTRAINT `FK_JobTitleFeature_JobTitle` FOREIGN KEY (`JobTitleId`) REFERENCES `jobtitle` (`Id`)``  
`) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`  
