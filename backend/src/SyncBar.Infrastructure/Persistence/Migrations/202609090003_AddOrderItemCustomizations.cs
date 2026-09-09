using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace SyncBar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609090003_AddOrderItemCustomizations")]
public sealed class AddOrderItemCustomizations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE `orderitemoptionalextra` (
 `Id` bigint NOT NULL AUTO_INCREMENT PRIMARY KEY,
 `OrderItemId` bigint NOT NULL,
 `ProductOptionalExtraId` bigint NOT NULL,
 `Name` varchar(150) NOT NULL,
 `CreatedAt` datetime(6) NOT NULL,
 `UpdatedAt` datetime(6) NULL,
 `IsActive` tinyint(1) NOT NULL DEFAULT 1,
 CONSTRAINT `FK_orderitemoptionalextra_orderitem` FOREIGN KEY (`OrderItemId`) REFERENCES `orderitem` (`Id`) ON DELETE CASCADE,
 CONSTRAINT `FK_orderitemoptionalextra_productoptionalextra` FOREIGN KEY (`ProductOptionalExtraId`) REFERENCES `productoptionalextra` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE `orderitemboost` (
 `Id` bigint NOT NULL AUTO_INCREMENT PRIMARY KEY,
 `OrderItemId` bigint NOT NULL,
 `ProductBoostId` bigint NOT NULL,
 `Name` varchar(150) NOT NULL,
 `UnitPriceCharged` decimal(18,2) NOT NULL,
 `CreatedAt` datetime(6) NOT NULL,
 `UpdatedAt` datetime(6) NULL,
 `IsActive` tinyint(1) NOT NULL DEFAULT 1,
 CONSTRAINT `FK_orderitemboost_orderitem` FOREIGN KEY (`OrderItemId`) REFERENCES `orderitem` (`Id`) ON DELETE CASCADE,
 CONSTRAINT `FK_orderitemboost_productboost` FOREIGN KEY (`ProductBoostId`) REFERENCES `productboost` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

""");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
DROP TABLE `orderitemboost`;
DROP TABLE `orderitemoptionalextra`;
""");
}

