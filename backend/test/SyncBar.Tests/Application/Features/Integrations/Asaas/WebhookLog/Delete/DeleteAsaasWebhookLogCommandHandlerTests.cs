using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.Delete;

public sealed class DeleteAsaasWebhookLogCommandHandlerTests
{
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteAsaasWebhookLogCommandHandler _handler;

    public DeleteAsaasWebhookLogCommandHandlerTests()
    {
        _handler = new DeleteAsaasWebhookLogCommandHandler(_webhookLogRepository, _logRepository, _unitOfWork);
    }

    private static AsaasIntegrationWebhookLog CreateLog() =>
        AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;

    [Fact]
    public async Task Handle_LogNotFound_ShouldReturnNotFound()
    {
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationWebhookLog?)null);

        var result = await _handler.Handle(new DeleteAsaasWebhookLogCommand(1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var log = CreateLog();
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new DeleteAsaasWebhookLogCommand(1, 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasWebhookLog.NotFound");
        _webhookLogRepository.DidNotReceive().Delete(Arg.Any<AsaasIntegrationWebhookLog>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteAndCommit()
    {
        var log = CreateLog();
        _webhookLogRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new DeleteAsaasWebhookLogCommand(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _webhookLogRepository.Received(1).Delete(log);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
