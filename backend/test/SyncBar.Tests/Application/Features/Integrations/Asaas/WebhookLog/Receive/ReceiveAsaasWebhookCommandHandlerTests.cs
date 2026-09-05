using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Receive;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.Receive;

public sealed class ReceiveAsaasWebhookCommandHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly IAsaasIntegrationWebhookLogRepository _webhookLogRepository = Substitute.For<IAsaasIntegrationWebhookLogRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ReceiveAsaasWebhookCommandHandler _handler;

    public ReceiveAsaasWebhookCommandHandlerTests()
    {
        _handler = new ReceiveAsaasWebhookCommandHandler(
            _paymentRepository, _settingRepository, _webhookLogRepository, _branchRepository, _logRepository, _unitOfWork);
    }

    private static AsaasIntegrationPayment CreatePayment(long branchId = 1) =>
        AsaasIntegrationPayment.Create(branchId, 10, 1, "pay_1", "PIX", 100m, new DateTime(2026, 9, 20)).Value;

    private static Branch CreateBranch(long companyId = 1) =>
        Branch.Create(companyId, "Matriz", null, null, null, null, null, null, null, null).Value;

    private const string ValidPayload =
        """{"id":"evt_1","event":"PAYMENT_RECEIVED","payment":{"id":"pay_1","value":100,"netValue":98,"status":"RECEIVED","paymentDate":"2026-09-18"}}""";

    [Fact]
    public async Task Handle_MalformedJson_ShouldReturnInvalidPayloadFailure()
    {
        var command = new ReceiveAsaasWebhookCommand("not-json", null, "127.0.0.1");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Asaas.InvalidPayload");
    }

    [Fact]
    public async Task Handle_PayloadWithoutPaymentObject_ShouldReturnInvalidPayloadFailure()
    {
        var command = new ReceiveAsaasWebhookCommand("""{"id":"evt_1","event":"PAYMENT_RECEIVED"}""", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Asaas.InvalidPayload");
    }

    [Fact]
    public async Task Handle_EventOutsidePaymentCategory_ShouldSucceedWithoutTouchingRepositories()
    {
        var command = new ReceiveAsaasWebhookCommand(
            """{"id":"evt_1","event":"INVOICE_CREATED","payment":{"id":"pay_1"}}""", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _paymentRepository.DidNotReceive().GetByAsaasPaymentIdForUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownPayment_ShouldSucceedWithoutUpdatingAnything()
    {
        _paymentRepository.GetByAsaasPaymentIdForUpdateAsync("pay_1", Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationPayment?)null);
        var command = new ReceiveAsaasWebhookCommand(ValidPayload, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _paymentRepository.DidNotReceive().Update(Arg.Any<AsaasIntegrationPayment>());
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ShouldReturnInvalidWebhookTokenFailure()
    {
        var payment = CreatePayment();
        _paymentRepository.GetByAsaasPaymentIdForUpdateAsync("pay_1", Arg.Any<CancellationToken>()).Returns(payment);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        var setting = AsaasIntegrationSetting.Create(1, null, "api-key", "expected-token").Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(setting);

        var command = new ReceiveAsaasWebhookCommand(ValidPayload, "wrong-token", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Asaas.InvalidWebhookToken");
        _paymentRepository.DidNotReceive().Update(Arg.Any<AsaasIntegrationPayment>());
    }

    [Fact]
    public async Task Handle_NoWebhookSecretConfigured_ShouldSkipTokenValidation()
    {
        var payment = CreatePayment();
        _paymentRepository.GetByAsaasPaymentIdForUpdateAsync("pay_1", Arg.Any<CancellationToken>()).Returns(payment);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        var setting = AsaasIntegrationSetting.Create(1, null, "api-key").Value; // sem WebhookSecretEncrypted
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(setting);

        var command = new ReceiveAsaasWebhookCommand(ValidPayload, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be("RECEIVED");
    }

    [Fact]
    public async Task Handle_ValidTokenAndPayment_ShouldSyncStatusNetValueAndPaymentDate()
    {
        var payment = CreatePayment();
        _paymentRepository.GetByAsaasPaymentIdForUpdateAsync("pay_1", Arg.Any<CancellationToken>()).Returns(payment);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        var setting = AsaasIntegrationSetting.Create(1, null, "api-key", "the-secret").Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(setting);

        var command = new ReceiveAsaasWebhookCommand(ValidPayload, "the-secret", "127.0.0.1");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be("RECEIVED");
        payment.NetValue.Should().Be(98m);
        payment.PaymentDate.Should().Be(new DateTime(2026, 9, 18));
        _paymentRepository.Received(1).Update(payment);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewEvent_ShouldPersistAuditLogAlreadyProcessed()
    {
        var payment = CreatePayment();
        _paymentRepository.GetByAsaasPaymentIdForUpdateAsync("pay_1", Arg.Any<CancellationToken>()).Returns(payment);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);
        _webhookLogRepository.ExistsByEventIdAsync("evt_1", Arg.Any<CancellationToken>()).Returns(false);

        var command = new ReceiveAsaasWebhookCommand(ValidPayload, null, "127.0.0.1");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _webhookLogRepository.Received(1).AddAsync(
            Arg.Is<AsaasIntegrationWebhookLog>(l => l.AsaasEventId == "evt_1" && l.CompanyId == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEvent_ShouldNotPersistAnotherAuditLog()
    {
        var payment = CreatePayment();
        _paymentRepository.GetByAsaasPaymentIdForUpdateAsync("pay_1", Arg.Any<CancellationToken>()).Returns(payment);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);
        _webhookLogRepository.ExistsByEventIdAsync("evt_1", Arg.Any<CancellationToken>()).Returns(true);

        var command = new ReceiveAsaasWebhookCommand(ValidPayload, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _webhookLogRepository.DidNotReceive().AddAsync(Arg.Any<AsaasIntegrationWebhookLog>(), Arg.Any<CancellationToken>());
        // A sincronização do pagamento roda de novo mesmo assim — reaplicar o mesmo status é inofensivo.
        payment.Status.Should().Be("RECEIVED");
    }
}
