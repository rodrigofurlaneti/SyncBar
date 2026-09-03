using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Promotions.GetByBranch;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Promotions.GetByBranch;

public sealed class GetPromotionsByBranchQueryHandlerTests
{
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPromotionsByBranchQueryHandler _handler;

    public GetPromotionsByBranchQueryHandlerTests()
    {
        _handler = new GetPromotionsByBranchQueryHandler(_promotionRepository, _logRepository, _unitOfWork);
    }

    private static Promotion CreatePromotion(long productId, string name, int dayOfWeek, int startMinuteOfDay,
        long promotionTypeId = PromotionTypeIds.EmDobro, decimal? discountRate = null)
        => Promotion.Create(
            branchId: 1, productId, name, dayOfWeek, startMinuteOfDay,
            endMinuteOfDay: startMinuteOfDay + 60, promotionTypeId, discountRate).Value;

    [Fact]
    public async Task Handle_NoPromotionsForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetPromotionsByBranchQuery(BranchId: 1);
        _promotionRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Promotion>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultiplePromotions_ShouldOrderByDayOfWeekThenStartMinuteOfDay()
    {
        var query = new GetPromotionsByBranchQuery(BranchId: 1);

        var sextaTarde = CreatePromotion(productId: 100, name: "Sexta Tarde", dayOfWeek: 5, startMinuteOfDay: 960);
        var sextaManha = CreatePromotion(productId: 200, name: "Sexta Manhã", dayOfWeek: 5, startMinuteOfDay: 480);
        var domingo = CreatePromotion(productId: 300, name: "Domingo", dayOfWeek: 0, startMinuteOfDay: 600);

        // Retorno do repositório fora de ordem de propósito, para provar que o handler reordena.
        _promotionRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([sextaTarde, sextaManha, domingo]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(r => r.Name).Should().ContainInOrder("Domingo", "Sexta Manhã", "Sexta Tarde");
    }

    [Fact]
    public async Task Handle_SinglePromotion_ShouldMapAllFields()
    {
        var query = new GetPromotionsByBranchQuery(BranchId: 1);
        var promotion = CreatePromotion(
            productId: 400, name: "Desconto Petiscos", dayOfWeek: 3, startMinuteOfDay: 720,
            promotionTypeId: PromotionTypeIds.Desconto, discountRate: 0.15m);
        _promotionRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([promotion]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.Id.Should().Be(promotion.Id);
        response.ProductId.Should().Be(400);
        response.Name.Should().Be("Desconto Petiscos");
        response.DayOfWeek.Should().Be(3);
        response.StartMinuteOfDay.Should().Be(720);
        response.EndMinuteOfDay.Should().Be(780);
        response.PromotionTypeId.Should().Be(PromotionTypeIds.Desconto);
        response.DiscountRate.Should().Be(0.15m);
    }
}
