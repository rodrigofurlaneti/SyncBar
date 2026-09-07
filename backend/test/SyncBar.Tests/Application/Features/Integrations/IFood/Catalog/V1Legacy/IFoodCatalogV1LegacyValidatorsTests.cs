using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.V1Legacy;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Catalog.V1Legacy;

// Cobertura das regras de validação (FluentValidation) do comando de
// Integrations/Ifood/Catalog/V1Legacy — sem FluentValidation.TestHelper (não referenciado neste
// projeto), então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodCatalogV1LegacyValidatorsTests
{
    [Fact]
    public void InvokeIfoodCatalogV1OperationCommandValidator_WithValidCommand_ShouldBeValid()
        => new InvokeIfoodCatalogV1OperationCommandValidator()
            .Validate(new InvokeIfoodCatalogV1OperationCommand(1, IfoodCatalogV1Operation.ListCatalogs, null, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void InvokeIfoodCatalogV1OperationCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new InvokeIfoodCatalogV1OperationCommandValidator()
            .Validate(new InvokeIfoodCatalogV1OperationCommand(0, IfoodCatalogV1Operation.ListCatalogs, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void InvokeIfoodCatalogV1OperationCommandValidator_WithInvalidOperation_ShouldBeInvalid()
        => new InvokeIfoodCatalogV1OperationCommandValidator()
            .Validate(new InvokeIfoodCatalogV1OperationCommand(1, (IfoodCatalogV1Operation)9999, null, null, null))
            .IsValid.Should().BeFalse();
}
