using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Promotions.Create;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Promotions.Create;

public sealed class CreatePromotionCommandHandlerTests
{
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreatePromotionCommandHandler _handler;

    public CreatePromotionCommandHandlerTests()
    {
        _handler = new CreatePromotionCommandHandler(_promotionRepository, _productRepository, _logRepository, _unitOfWork);
    }

    private static Product CreateActiveProduct()
        => Product.Create(
            companyId: 1, categoryId: 1, unitOfMeasureId: 1, name: "Chopp Pilsen",
            description: null, barcode: null, salePrice: 15m, costPrice: 5m,
            isStockControlled: false, preparationTimeMinutes: null).Value;

    private static CreatePromotionCommand CreateValidCommand(string name = "Happy Hour")
        => new(
            BranchId: 1,
            ProductId: 100,
            Name: name,
            DayOfWeek: 5,
            StartMinuteOfDay: 960,
            EndMinuteOfDay: 1200,
            PromotionTypeId: PromotionTypeIds.EmDobro,
            DiscountRate: null);

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand();
        _productRepository.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        await _promotionRepository.DidNotReceive().AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
        // Falha antes de qualquer persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductInactive_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand();
        var product = CreateActiveProduct();
        product.Deactivate();
        _productRepository.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        await _promotionRepository.DidNotReceive().AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidPromotionWindow_ShouldReturnDomainFailureWithoutPersisting()
    {
        // StartMinuteOfDay >= EndMinuteOfDay é rejeitado pela própria entidade Promotion.
        var command = CreateValidCommand() with { StartMinuteOfDay = 1200, EndMinuteOfDay = 960 };
        _productRepository.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns(CreateActiveProduct());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Promotion.InvalidWindow");
        await _promotionRepository.DidNotReceive().AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidDiscountPromotion_ShouldPersistTrimmedNameAndReturnId()
    {
        var command = CreateValidCommand(name: "  Desconto Terça  ") with
        {
            PromotionTypeId = PromotionTypeIds.Desconto,
            DiscountRate = 0.25m
        };
        _productRepository.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns(CreateActiveProduct());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _promotionRepository.Received(1).AddAsync(
            Arg.Is<Promotion>(p =>
                p.BranchId == command.BranchId &&
                p.ProductId == command.ProductId &&
                p.Name == "Desconto Terça" &&
                p.DayOfWeek == command.DayOfWeek &&
                p.StartMinuteOfDay == command.StartMinuteOfDay &&
                p.EndMinuteOfDay == command.EndMinuteOfDay &&
                p.PromotionTypeId == PromotionTypeIds.Desconto &&
                p.DiscountRate == 0.25m &&
                p.IsActive),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
