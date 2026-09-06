using FluentAssertions;
using SyncBar.Application.Features.Catalog.Complements.AddComplement;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementItem;
using SyncBar.Application.Features.Catalog.Complements.LinkProductComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementItem;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementPrice;
using SyncBar.Domain.Constants;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements;

public sealed class ComplementsValidatorsTests
{
    [Fact]
    public void AddComplementCommandValidator_ValidCommand_ShouldBeValid()
        => new AddComplementCommandValidator().Validate(new AddComplementCommand(1, 1, 0m)).IsValid.Should().BeTrue();

    [Fact]
    public void AddComplementCommandValidator_NegativeExtraPrice_ShouldBeInvalid()
        => new AddComplementCommandValidator().Validate(new AddComplementCommand(1, 1, -1m)).IsValid.Should().BeFalse();

    [Fact]
    public void CreateComplementGroupCommandValidator_ValidCommand_ShouldBeValid()
        => new CreateComplementGroupCommandValidator()
            .Validate(new CreateComplementGroupCommand(1, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 0, 1))
            .IsValid.Should().BeTrue();

    [Fact]
    public void CreateComplementGroupCommandValidator_InvalidType_ShouldBeInvalid()
        => new CreateComplementGroupCommandValidator()
            .Validate(new CreateComplementGroupCommand(1, "Bebidas", 999, 0, 1))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateComplementGroupCommandValidator_MinGreaterThanMax_ShouldBeInvalid()
        => new CreateComplementGroupCommandValidator()
            .Validate(new CreateComplementGroupCommand(1, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 2, 1))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateComplementGroupCommandValidator_MaxSelectionZero_ShouldBeInvalid()
        => new CreateComplementGroupCommandValidator()
            .Validate(new CreateComplementGroupCommand(1, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 0, 0))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateComplementItemCommandValidator_ValidCommand_ShouldBeValid()
        => new CreateComplementItemCommandValidator().Validate(new CreateComplementItemCommand(1, "Bacon")).IsValid.Should().BeTrue();

    [Fact]
    public void CreateComplementItemCommandValidator_EmptyName_ShouldBeInvalid()
        => new CreateComplementItemCommandValidator().Validate(new CreateComplementItemCommand(1, "")).IsValid.Should().BeFalse();

    [Fact]
    public void LinkProductComplementGroupCommandValidator_ValidCommand_ShouldBeValid()
        => new LinkProductComplementGroupCommandValidator().Validate(new LinkProductComplementGroupCommand(1, 1, 0)).IsValid.Should().BeTrue();

    [Fact]
    public void LinkProductComplementGroupCommandValidator_NegativeDisplayOrder_ShouldBeInvalid()
        => new LinkProductComplementGroupCommandValidator().Validate(new LinkProductComplementGroupCommand(1, 1, -1)).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateComplementGroupCommandValidator_ValidCommand_ShouldBeValid()
        => new UpdateComplementGroupCommandValidator()
            .Validate(new UpdateComplementGroupCommand(1, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 0, 1))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UpdateComplementGroupCommandValidator_InvalidType_ShouldBeInvalid()
        => new UpdateComplementGroupCommandValidator()
            .Validate(new UpdateComplementGroupCommand(1, "Bebidas", 999, 0, 1))
            .IsValid.Should().BeFalse();

    [Fact]
    public void UpdateComplementItemCommandValidator_ValidCommand_ShouldBeValid()
        => new UpdateComplementItemCommandValidator().Validate(new UpdateComplementItemCommand(1, "Bacon")).IsValid.Should().BeTrue();

    [Fact]
    public void UpdateComplementItemCommandValidator_EmptyName_ShouldBeInvalid()
        => new UpdateComplementItemCommandValidator().Validate(new UpdateComplementItemCommand(1, "")).IsValid.Should().BeFalse();

    [Fact]
    public void UpdateComplementPriceCommandValidator_ValidCommand_ShouldBeValid()
        => new UpdateComplementPriceCommandValidator().Validate(new UpdateComplementPriceCommand(1, 1, 0m)).IsValid.Should().BeTrue();

    [Fact]
    public void UpdateComplementPriceCommandValidator_NegativeExtraPrice_ShouldBeInvalid()
        => new UpdateComplementPriceCommandValidator().Validate(new UpdateComplementPriceCommand(1, 1, -1m)).IsValid.Should().BeFalse();
}
