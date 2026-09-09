-- Reference SQL for migration 202609090002_AddProductExtrasAndBoosts.
-- The API applies this migration automatically through EF on startup.
-- Do not apply this file manually before starting the API: EF owns its migration history.
ALTER TABLE `product` ADD COLUMN `HasOptionalExtras` tinyint(1) NOT NULL DEFAULT 0, ADD COLUMN `HasBoosts` tinyint(1) NOT NULL DEFAULT 0;
CREATE TABLE `productoptionalextra` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ProductId` bigint NOT NULL,
  `OptionalExtraName` varchar(150) NOT NULL,
  `DisplayOrder` int NOT NULL DEFAULT 0,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_productoptionalextra_ProductId_DisplayOrder` (`ProductId`, `DisplayOrder`),
  CONSTRAINT `FK_productoptionalextra_product` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `CK_productoptionalextra_DisplayOrder` CHECK (`DisplayOrder` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE `productboost` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ProductId` bigint NOT NULL,
  `BoostName` varchar(150) NOT NULL,
  `IncrementalValue` decimal(18,2) NOT NULL,
  `DisplayOrder` int NOT NULL DEFAULT 0,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_productboost_ProductId_DisplayOrder` (`ProductId`, `DisplayOrder`),
  CONSTRAINT `FK_productboost_product` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `CK_productboost_DisplayOrder` CHECK (`DisplayOrder` >= 0),
  CONSTRAINT `CK_productboost_IncrementalValue` CHECK (`IncrementalValue` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
