using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Financial;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Financial;

// Cobertura das regras de validação (FluentValidation) dos comandos/queries de
// Integrations/Ifood/Financial — sem FluentValidation.TestHelper (não referenciado neste
// projeto), então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodFinancialValidatorsTests
{
    [Fact]
    public void GetIfoodFinancialReportQueryValidator_WithValidQuery_ShouldBeValid()
        => new GetIfoodFinancialReportQueryValidator()
            .Validate(new GetIfoodFinancialReportQuery(1, IfoodFinancialReportType.SalesAdjustments, null, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void GetIfoodFinancialReportQueryValidator_WithZeroBranchId_ShouldBeInvalid()
        => new GetIfoodFinancialReportQueryValidator()
            .Validate(new GetIfoodFinancialReportQuery(0, IfoodFinancialReportType.SalesAdjustments, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void GetIfoodFinancialReportQueryValidator_WithInvalidReportType_ShouldBeInvalid()
        => new GetIfoodFinancialReportQueryValidator()
            .Validate(new GetIfoodFinancialReportQuery(1, (IfoodFinancialReportType)9999, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodReconciliationOnDemandCommandValidator_WithValidCommand_ShouldBeValid()
        => new RequestIfoodReconciliationOnDemandCommandValidator()
            .Validate(new RequestIfoodReconciliationOnDemandCommand(1, "2024-05"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void RequestIfoodReconciliationOnDemandCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new RequestIfoodReconciliationOnDemandCommandValidator()
            .Validate(new RequestIfoodReconciliationOnDemandCommand(0, "2024-05"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodReconciliationOnDemandCommandValidator_WithEmptyCompetence_ShouldBeInvalid()
        => new RequestIfoodReconciliationOnDemandCommandValidator()
            .Validate(new RequestIfoodReconciliationOnDemandCommand(1, ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodReconciliationOnDemandCommandValidator_WithWrongCompetenceFormat_ShouldBeInvalid()
        => new RequestIfoodReconciliationOnDemandCommandValidator()
            .Validate(new RequestIfoodReconciliationOnDemandCommand(1, "05-2024"))
            .IsValid.Should().BeFalse();
}
