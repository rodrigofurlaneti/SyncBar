using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class AddOrderItemCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ShouldFreezeMenuPriceOnItem()
    {
        _promotionRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>());
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        var product = Product.Create(1, 1, 1, "Cerveja Pilsen 600ml", null, null, 14.90m, 6.50m, true, null).Value;
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new AddOrderItemCommandHandler(_orderRepository, _productRepository, _promotionRepository, _printingService, _unitOfWork);
        var result = await handler.Handle(new AddOrderItemCommand(1, 5, 2, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(1);
        order.Items.First().UnitPrice.Should().Be(14.90m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownProduct_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new AddOrderItemCommandHandler(_orderRepository, _productRepository, _promotionRepository, _printingService, _unitOfWork);
        var result = await handler.Handle(new AddOrderItemCommand(1, 99, 1, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }
}

public sealed class AddOrderItemPromotionTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static T WithId<T>(T entity, long id) where T : SyncBar.Domain.Primitives.Entity
    {
        typeof(SyncBar.Domain.Primitives.Entity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    [Fact]
    public async Task Handle_WithActivePromotion_ShouldAddFreeBonusLine()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        var caipirinha = WithId(Product.Create(1, 2, 6, "Caipirinha", null, null, 22m, 7m, false, 8).Value, 3);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(caipirinha);

        // Promocao valida para HOJE, o dia inteiro — o teste roda em qualquer horario.
        var today = (int)DateTime.Now.DayOfWeek;
        var promo = Promotion.Create(1, 3, "Caipirinha em dobro", today, 0, 1440).Value;
        _promotionRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion> { promo });

        var handler = new AddOrderItemCommandHandler(_orderRepository, _productRepository, _promotionRepository, _printingService, _unitOfWork);
        var result = await handler.Handle(new AddOrderItemCommand(1, 3, 1, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(2);
        order.Items.First().UnitPrice.Should().Be(22m);        // paga 1
        order.Items.Last().UnitPrice.Should().Be(0m);          // leva 2
        order.Items.Last().Notes.Should().Contain("Caipirinha em dobro");
        order.SubtotalAmount.Should().Be(22m);                 // cliente paga so a primeira
    }

    [Fact]
    public async Task Handle_OutsidePromotionWindow_ShouldChargeNormalPrice()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        var caipirinha = WithId(Product.Create(1, 2, 6, "Caipirinha", null, null, 22m, 7m, false, 8).Value, 3);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(caipirinha);

        // Promocao de OUTRO dia da semana — fora da janela agora.
        var otherDay = ((int)DateTime.Now.DayOfWeek + 1) % 7;
        var promo = Promotion.Create(1, 3, "Caipirinha em dobro", otherDay, 960, 1200).Value;
        _promotionRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion> { promo });

        var handler = new AddOrderItemCommandHandler(_orderRepository, _productRepository, _promotionRepository, _printingService, _unitOfWork);
        var result = await handler.Handle(new AddOrderItemCommand(1, 3, 1, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(1);      // sem bonus
        order.SubtotalAmount.Should().Be(22m);  // preco normal do cardapio
    }
}

public sealed class AddOrderItemDiscountPromotionTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static T WithId<T>(T entity, long id) where T : SyncBar.Domain.Primitives.Entity
    {
        typeof(SyncBar.Domain.Primitives.Entity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    [Fact]
    public async Task Handle_WithActiveDiscountPromotion_ShouldChargeDiscountedFrozenPrice()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        var chapa = WithId(Product.Create(1, 4, 7, "Porção Chapa Mista", null, null, 80m, 30m, false, 25).Value, 9);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns(chapa);

        var today = (int)DateTime.Now.DayOfWeek;
        var promo = Promotion.Create(1, 9, "Domingo da chapa -25%", today, 0, 1440,
            SyncBar.Domain.Constants.PromotionTypeIds.Desconto, 0.25m).Value;
        _promotionRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion> { promo });

        var handler = new AddOrderItemCommandHandler(_orderRepository, _productRepository, _promotionRepository, _printingService, _unitOfWork);
        var result = await handler.Handle(new AddOrderItemCommand(1, 9, 1, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(1);                      // desconto nao gera linha bonus
        order.Items.First().UnitPrice.Should().Be(60m);         // 80 − 25%
        order.Items.First().Notes.Should().Contain("Domingo da chapa");
        order.SubtotalAmount.Should().Be(60m);
    }
}
