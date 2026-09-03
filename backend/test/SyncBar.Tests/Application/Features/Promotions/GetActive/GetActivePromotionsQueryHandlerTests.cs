using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Promotions.GetActive;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Promotions.GetActive;

public sealed class GetActivePromotionsQueryHandlerTests
{
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetActivePromotionsQueryHandler _handler;

    public GetActivePromotionsQueryHandlerTests()
    {
        _handler = new GetActivePromotionsQueryHandler(_promotionRepository, _logRepository, _unitOfWork);
    }

    // Janela 0..1440 cobre o dia inteiro (Promotion valida EndMinuteOfDay até 1440), então a
    // promoção fica ativa em QUALQUER horário do dia atual — evita depender do minuto exato em
    // que o teste roda. O dia da semana usa DateTime.Now.DayOfWeek real (o handler também usa
    // DateTime.Now internamente, sem clock injetável).
    private static Promotion CreatePromotionActiveToday(long productId = 100, string name = "Happy Hour",
        long promotionTypeId = PromotionTypeIds.EmDobro, decimal? discountRate = null)
        => Promotion.Create(
            branchId: 1, productId, name, dayOfWeek: (int)DateTime.Now.DayOfWeek,
            startMinuteOfDay: 0, endMinuteOfDay: 1440, promotionTypeId, discountRate).Value;

    [Fact]
    public async Task Handle_NoPromotionsForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetActivePromotionsQuery(BranchId: 1);
        _promotionRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Promotion>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeactivatedPromotion_ShouldBeExcludedFromResult()
    {
        var query = new GetActivePromotionsQuery(BranchId: 1);
        var deactivated = CreatePromotionActiveToday();
        deactivated.Deactivate();
        _promotionRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([deactivated]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // IsActiveAt retorna false para promoção desativada, mesmo dentro da janela de horário.
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ActiveAndInactivePromotionsMixed_ShouldMapOnlyTheActiveOne()
    {
        var query = new GetActivePromotionsQuery(BranchId: 1);
        var active = CreatePromotionActiveToday(
            productId: 200, name: "Chopp em Dobro", promotionTypeId: PromotionTypeIds.EmDobro, discountRate: null);
        var deactivated = CreatePromotionActiveToday(productId: 300, name: "Promoção Encerrada");
        deactivated.Deactivate();

        _promotionRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([active, deactivated]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var response = result.Value.Single();
        response.ProductId.Should().Be(200);
        response.Name.Should().Be("Chopp em Dobro");
        response.EndMinuteOfDay.Should().Be(1440);
        response.PromotionTypeId.Should().Be(PromotionTypeIds.EmDobro);
        response.DiscountRate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ActiveDiscountPromotion_ShouldMapDiscountRate()
    {
        var query = new GetActivePromotionsQuery(BranchId: 1);
        var active = CreatePromotionActiveToday(
            productId: 400, name: "Desconto Petiscos", promotionTypeId: PromotionTypeIds.Desconto, discountRate: 0.3m);
        _promotionRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([active]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.PromotionTypeId.Should().Be(PromotionTypeIds.Desconto);
        response.DiscountRate.Should().Be(0.3m);
    }
}
