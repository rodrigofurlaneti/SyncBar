using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.Delete;

public sealed class DeleteKeetaIntegrationRefundDisputeCommandHandlerTests
{
    private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository = Substitute.For<IKeetaIntegrationRefundDisputeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteKeetaIntegrationRefundDisputeCommandHandler _handler;

    public DeleteKeetaIntegrationRefundDisputeCommandHandlerTests()
    {
        _handler = new DeleteKeetaIntegrationRefundDisputeCommandHandler(_disputeRepository, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationRefundDispute MakeDispute(long companyId = 1) =>
        KeetaIntegrationRefundDispute.Create(companyId, 2, "order-1", 500, 20m, "motivo").Value;

    [Fact]
    public async Task Handle_DisputeNotFound_ShouldReturnNotFound()
    {
        var command = new DeleteKeetaIntegrationRefundDisputeCommand(1, 1);
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationRefundDispute?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaRefundDispute.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var dispute = MakeDispute(1);
        var command = new DeleteKeetaIntegrationRefundDisputeCommand(1, 2);
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(dispute);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaRefundDispute.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteAndCommit()
    {
        var dispute = MakeDispute(1);
        var command = new DeleteKeetaIntegrationRefundDisputeCommand(1, 1);
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(dispute);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _disputeRepository.Received(1).Delete(dispute);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
