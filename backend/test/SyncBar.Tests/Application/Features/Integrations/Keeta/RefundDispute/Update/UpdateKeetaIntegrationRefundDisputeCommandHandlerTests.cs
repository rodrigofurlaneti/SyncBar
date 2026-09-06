using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.Update;

public sealed class UpdateKeetaIntegrationRefundDisputeCommandHandlerTests
{
    private readonly IKeetaIntegrationRefundDisputeRepository _disputeRepository = Substitute.For<IKeetaIntegrationRefundDisputeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateKeetaIntegrationRefundDisputeCommandHandler _handler;

    public UpdateKeetaIntegrationRefundDisputeCommandHandlerTests()
    {
        _handler = new UpdateKeetaIntegrationRefundDisputeCommandHandler(_disputeRepository, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationRefundDispute MakeDispute(long companyId = 1) =>
        KeetaIntegrationRefundDispute.Create(companyId, 2, "order-1", 500, 20m, "motivo").Value;

    [Fact]
    public async Task Handle_DisputeNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateKeetaIntegrationRefundDisputeCommand(1, 1, true);
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationRefundDispute?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaRefundDispute.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var dispute = MakeDispute(1);
        var command = new UpdateKeetaIntegrationRefundDisputeCommand(1, 2, true);
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(dispute);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaRefundDispute.NotFound");
    }

    [Fact]
    public async Task Handle_Accepted_ShouldResolveAsAccepted()
    {
        var dispute = MakeDispute(1);
        var command = new UpdateKeetaIntegrationRefundDisputeCommand(1, 1, true);
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(dispute);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dispute.ResolutionStatus.Should().Be("ACCEPTED");
        dispute.ResolvedAtUtc.Should().NotBeNull();
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Rejected_ShouldResolveAsRejectedWithDenialReason()
    {
        var dispute = MakeDispute(1);
        var command = new UpdateKeetaIntegrationRefundDisputeCommand(1, 1, false, "OTHER", "prato já entregue");
        _disputeRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(dispute);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dispute.ResolutionStatus.Should().Be("REJECTED");
        dispute.DenialReasonCode.Should().Be("OTHER");
        dispute.DenialReasonText.Should().Be("prato já entregue");
    }
}
