using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.Create;

public sealed class CreateAsaasIntegrationPaymentCommandHandlerTests
{
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateAsaasIntegrationPaymentCommandHandler _handler;

    public CreateAsaasIntegrationPaymentCommandHandlerTests()
    {
        _handler = new CreateAsaasIntegrationPaymentCommandHandler(
            _paymentRepository, _asaasCustomerRepository, _settingRepository, _branchRepository, _asaasService,
            _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch(long companyId = 1) =>
        Branch.Create(companyId, "Matriz", null, null, null, null, null, null, null, null).Value;

    private static CreateAsaasIntegrationPaymentCommand ValidCommand(
        long? customerId = 1,
        string billingType = "PIX",
        string? creditCardToken = null,
        int installmentCount = 1) =>
        new(BranchId: 1, CustomerOrderId: 10, CustomerId: customerId, BillingType: billingType,
            Value: 100m, DueDate: new DateTime(2026, 9, 20), InstallmentCount: installmentCount,
            CreditCardToken: creditCardToken);

    private void SetupActiveSettingAndBoundCustomer(long companyId = 1)
    {
        var setting = AsaasIntegrationSetting.Create(companyId, null, "api-key").Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(companyId, 1, Arg.Any<CancellationToken>()).Returns(setting);

        var binding = AsaasIntegrationCustomer.Create(1, companyId, "cus_123").Value;
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(1, companyId, Arg.Any<CancellationToken>()).Returns(binding);
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnNotFound()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_NoActiveSettingForUnit_ShouldReturnValidationFailure()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_SettingInactive_ShouldReturnValidationFailure()
    {
        var inactiveSetting = AsaasIntegrationSetting.Create(1, null, "api-key", isActive: false).Value;
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(inactiveSetting);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_NoCustomerIdProvided_ShouldReturnAsaasCustomerNotFound()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationSetting.Create(1, null, "api-key").Value);

        var result = await _handler.Handle(ValidCommand(customerId: null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomer.NotFound");
        await _asaasService.DidNotReceive().CreatePaymentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<DateTime>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerHasNoAsaasBinding_ShouldReturnAsaasCustomerNotFound()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationSetting.Create(1, null, "api-key").Value);
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationCustomer?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomer.NotFound");
    }

    [Fact]
    public async Task Handle_AsaasApiThrowsOnCreatePayment_ShouldReturnFailureWithoutPersisting()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        SetupActiveSettingAndBoundCustomer();
        _asaasService.CreatePaymentAsync(
                "cus_123", "PIX", 100m, Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<AsaasPaymentResponse>>(_ => throw new HttpRequestException("Asaas indisponível"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasApi.CreatePaymentFailed");
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<AsaasIntegrationPayment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PixPayment_ShouldFetchQrCodeAndPersistPaymentWithPixDetails()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        SetupActiveSettingAndBoundCustomer();
        _asaasService.CreatePaymentAsync(
                "cus_123", "PIX", 100m, Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AsaasPaymentResponse("pay_1", "PENDING", 100m, null, null, "https://invoice", null));
        _asaasService.GetPixQrCodeAsync("pay_1", Arg.Any<CancellationToken>())
            .Returns(new AsaasPixQrCodeResponse("base64image", "copia-e-cola", DateTime.UtcNow.AddMinutes(30)));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PixQrCodeBase64.Should().Be("base64image");
        result.Value.PixPayload.Should().Be("copia-e-cola");
        result.Value.InvoiceUrl.Should().Be("https://invoice");
        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<AsaasIntegrationPayment>(p => p.AsaasPaymentId == "pay_1" && p.PixQrCodeBase64 == "base64image"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PixQrCodeFetchFails_ShouldStillPersistPaymentWithoutQrCode()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        SetupActiveSettingAndBoundCustomer();
        _asaasService.CreatePaymentAsync(
                "cus_123", "PIX", 100m, Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AsaasPaymentResponse("pay_1", "PENDING", 100m, null, null));
        _asaasService.GetPixQrCodeAsync("pay_1", Arg.Any<CancellationToken>())
            .Returns<Task<AsaasPixQrCodeResponse>>(_ => throw new HttpRequestException("timeout"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PixQrCodeBase64.Should().BeNull();
        await _paymentRepository.Received(1).AddAsync(Arg.Any<AsaasIntegrationPayment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonPixBillingType_ShouldNotCallGetPixQrCode()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        SetupActiveSettingAndBoundCustomer();
        _asaasService.CreatePaymentAsync(
                "cus_123", "BOLETO", 100m, Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AsaasPaymentResponse("pay_2", "PENDING", 100m, null, null, null, "https://bankslip"));

        var result = await _handler.Handle(ValidCommand(billingType: "BOLETO"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BankSlipUrl.Should().Be("https://bankslip");
        await _asaasService.DidNotReceive().GetPixQrCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCreditCardToken_ShouldSetTokenOnPersistedEntity()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        SetupActiveSettingAndBoundCustomer();
        _asaasService.CreatePaymentAsync(
                "cus_123", "CREDIT_CARD", 100m, Arg.Any<DateTime>(), Arg.Any<string>(), "card-token-1", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AsaasPaymentResponse("pay_3", "CONFIRMED", 100m, 98m, DateTime.UtcNow));

        var result = await _handler.Handle(
            ValidCommand(billingType: "CREDIT_CARD", creditCardToken: "card-token-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _paymentRepository.Received(1).AddAsync(
            Arg.Is<AsaasIntegrationPayment>(p => p.CreditCardToken == "card-token-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValueIsZero_ShouldReturnDomainValidationFailure()
    {
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        SetupActiveSettingAndBoundCustomer();
        _asaasService.CreatePaymentAsync(
                "cus_123", "PIX", 0m, Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new AsaasPaymentResponse("pay_4", "PENDING", 0m, null, null));

        var command = ValidCommand() with { Value = 0m };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Value.Invalid");
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<AsaasIntegrationPayment>(), Arg.Any<CancellationToken>());
    }
}
