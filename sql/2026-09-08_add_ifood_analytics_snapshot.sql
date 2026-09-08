-- Aplicar antes de habilitar IfoodAnalytics:Enabled. Preserva os dados existentes.
CREATE TABLE IF NOT EXISTS `ifoodanalyticssnapshot` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `BranchId` bigint NOT NULL,
  `MerchantId` varchar(100) NOT NULL,
  `ReferenceDate` date NOT NULL,
  `AggregatesJson` json NOT NULL,
  `RefreshedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_IFoodAnalytics_ScopeDate` (`BranchId`, `MerchantId`, `ReferenceDate`),
  CONSTRAINT `FK_IFoodAnalytics_Branch` FOREIGN KEY (`BranchId`) REFERENCES `branch` (`Id`)
);
