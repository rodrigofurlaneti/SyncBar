-- Aplicar antes de publicar webhook/fila persistente. Instalações atuais continuam em Polling.
SET @ifood_webhook_ddl = IF(
  EXISTS(SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE() AND LOWER(table_name) = 'ifoodintegrationsetting' AND column_name = 'EventDeliveryMode'),
  'SELECT 1',
  'ALTER TABLE `IfoodIntegrationSetting` ADD COLUMN `EventDeliveryMode` varchar(16) NOT NULL DEFAULT ''Polling'''
);
PREPARE ifood_webhook_stmt FROM @ifood_webhook_ddl;
EXECUTE ifood_webhook_stmt;
DEALLOCATE PREPARE ifood_webhook_stmt;

CREATE TABLE IF NOT EXISTS `ifoodeventinbox` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CompanyId` bigint NOT NULL,
  `EventId` varchar(100) NOT NULL,
  `Payload` longtext NOT NULL,
  `ReceivedAtUtc` datetime(6) NOT NULL,
  `NextAttemptAtUtc` datetime(6) NOT NULL,
  `ProcessedAtUtc` datetime(6) NULL,
  `Attempts` int NOT NULL DEFAULT 0,
  `LastError` varchar(1000) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_IFoodEventInbox_CompanyEvent` (`CompanyId`, `EventId`),
  KEY `IX_IFoodEventInbox_Pending` (`ProcessedAtUtc`, `NextAttemptAtUtc`)
);
