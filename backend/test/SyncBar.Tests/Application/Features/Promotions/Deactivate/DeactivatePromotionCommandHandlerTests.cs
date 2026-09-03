using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Promotions.Deactivate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Promotions.Deactivate;

public sealed class DeactivatePromotionCommandHandlerTests
{
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivatePromotionCommandHandler _handler;

    public DeactivatePromotionCommandHandlerTests()
    {
        _handler = new DeactivatePromotionCommandHandler(_promotionRepository, _logRepository, _unitOfWork);
    }

    private static Promotion CreatePromotion()
        => Promotion.Create(
            branchId: 1, productId: 100, name: "Happy Hour",
            dayOfWeek: 5, startMinuteOfDay: 960, endMinuteOfDay: 1200).Value;

    [Fact]
    public async Task Handle_PromotionNotFound_ShouldReturnFailure()
    {
        var command = new DeactivatePromotionCommand(PromotionId: 1);
        _promotionRepository.GetByIdForUpdateAsync(command.PromotionId, Arg.Any<CancellationToken>())
            .Returns((Promotion?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Promotion.NotFound");
        // Falha antes de qualquer persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PromotionAlreadyInactive_ShouldReturnFailure()
    {
        var command = new DeactivatePromotionCommand(PromotionId: 1);
        var promotion = CreatePromotion();
        promotion.Deactivate();
        _promotionRepository.GetByIdForUpdateAsync(command.PromotionId, Arg.Any<CancellationToken>())
            .Returns(promotion);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Promotion.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActivePromotion_ShouldDeactivateAndCommit()
    {
        var command = new DeactivatePromotionCommand(PromotionId: 1);
        var promotion = CreatePromotion();
        _promotionRepository.GetByIdForUpdateAsync(command.PromotionId, Arg.Any<CancellationToken>())
            .Returns(promotion);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        promotion.IsActive.Should().BeFalse();
        promotion.UpdatedAt.Should().NotBeNull();
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
