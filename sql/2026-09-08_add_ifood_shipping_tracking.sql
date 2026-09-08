-- Aplicar antes de publicar o worker de rastreamento Shipping.
CREATE TABLE IF NOT EXISTS `ifoodshippingtracking` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `BranchId` bigint NOT NULL,
  `OrderId` varchar(100) NOT NULL,
  `AssignedAtUtc` datetime(6) NOT NULL,
  `NextPollAtUtc` datetime(6) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `Latitude` double NULL,
  `Longitude` double NULL,
  `ExpectedDelivery` datetime(6) NULL,
  `DeliveryEtaEndMinutes` double NULL,
  `PickupEtaStartMinutes` double NULL,
  `UpdatedAtUtc` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_IFoodShippingTracking_CompanyOrder` (`CompanyId`, `OrderId`),
  KEY `IX_IFoodShippingTracking_Poll` (`IsActive`, `NextPollAtUtc`),
  CONSTRAINT `FK_IFoodShippingTracking_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
);
