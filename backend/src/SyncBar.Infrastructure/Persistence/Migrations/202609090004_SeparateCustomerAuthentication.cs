using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SyncBar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609090004_SeparateCustomerAuthentication")]
public sealed class SeparateCustomerAuthentication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE `customerrefreshtoken` (
 `Id` bigint NOT NULL AUTO_INCREMENT PRIMARY KEY,
 `CustomerAppUserId` bigint NOT NULL,
 `Token` varchar(500) NOT NULL,
 `ExpiresAt` datetime(6) NOT NULL,
 `RevokedAt` datetime(6) NULL,
 `CreatedAt` datetime(6) NOT NULL,
 `UpdatedAt` datetime(6) NULL,
 `IsActive` tinyint(1) NOT NULL DEFAULT 1,
 KEY `IX_CustomerRefreshToken_CustomerAppUserId` (`CustomerAppUserId`),
 CONSTRAINT `FK_CustomerRefreshToken_CustomerAppUser` FOREIGN KEY (`CustomerAppUserId`) REFERENCES `customerappuser` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
ALTER TABLE `accesslog`
 MODIFY COLUMN `UserName` varchar(150) NOT NULL,
 ADD COLUMN `CustomerAppUserId` bigint NULL,
 ADD CONSTRAINT `FK_AccessLog_CustomerAppUser` FOREIGN KEY (`CustomerAppUserId`) REFERENCES `customerappuser` (`Id`) ON DELETE RESTRICT;
-- Old tokens do not identify whether their owner was an employee or a customer.
-- Revoke them instead of guessing ownership from overlapping numeric IDs.
UPDATE `refreshtoken` SET `RevokedAt` = CURRENT_TIMESTAMP(6), `UpdatedAt` = CURRENT_TIMESTAMP(6)
 WHERE `RevokedAt` IS NULL;
""");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
ALTER TABLE `accesslog` DROP FOREIGN KEY `FK_AccessLog_CustomerAppUser`, DROP COLUMN `CustomerAppUserId`;
DROP TABLE `customerrefreshtoken`;
""");
}
