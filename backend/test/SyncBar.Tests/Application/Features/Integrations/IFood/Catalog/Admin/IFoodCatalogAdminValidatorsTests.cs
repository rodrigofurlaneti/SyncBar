using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Catalog.Admin;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Catalog/Admin — sem FluentValidation.TestHelper (não referenciado neste
// projeto), então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodCatalogAdminValidatorsTests
{
    [Fact]
    public void DeleteIfoodInventoryBatchCommandValidator_WithValidCommand_ShouldBeValid()
        => new DeleteIfoodInventoryBatchCommandValidator()
            .Validate(new DeleteIfoodInventoryBatchCommand(1, [Guid.NewGuid()]))
            .IsValid.Should().BeTrue();

    [Fact]
    public void DeleteIfoodInventoryBatchCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new DeleteIfoodInventoryBatchCommandValidator()
            .Validate(new DeleteIfoodInventoryBatchCommand(0, [Guid.NewGuid()]))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DeleteIfoodInventoryBatchCommandValidator_WithEmptyProductIds_ShouldBeInvalid()
        => new DeleteIfoodInventoryBatchCommandValidator()
            .Validate(new DeleteIfoodInventoryBatchCommand(1, []))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DowngradeIfoodCatalogVersionCommandValidator_WithValidCommand_ShouldBeValid()
        => new DowngradeIfoodCatalogVersionCommandValidator()
            .Validate(new DowngradeIfoodCatalogVersionCommand(1))
            .IsValid.Should().BeTrue();

    [Fact]
    public void DowngradeIfoodCatalogVersionCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new DowngradeIfoodCatalogVersionCommandValidator()
            .Validate(new DowngradeIfoodCatalogVersionCommand(0))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpgradeIfoodCatalogVersionCommandValidator_WithValidCommand_ShouldBeValid()
        => new UpgradeIfoodCatalogVersionCommandValidator()
            .Validate(new UpgradeIfoodCatalogVersionCommand(1, true))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UpgradeIfoodCatalogVersionCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new UpgradeIfoodCatalogVersionCommandValidator()
            .Validate(new UpgradeIfoodCatalogVersionCommand(0, true))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UploadIfoodImageCommandValidator_WithValidCommand_ShouldBeValid()
        => new UploadIfoodImageCommandValidator()
            .Validate(new UploadIfoodImageCommand(1, "{\"image\":\"base64\"}"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UploadIfoodImageCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new UploadIfoodImageCommandValidator()
            .Validate(new UploadIfoodImageCommand(0, "{\"image\":\"base64\"}"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UploadIfoodImageCommandValidator_WithEmptyJsonBody_ShouldBeInvalid()
        => new UploadIfoodImageCommandValidator()
            .Validate(new UploadIfoodImageCommand(1, ""))
            .IsValid.Should().BeFalse();
}
