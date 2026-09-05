using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.Create;

public sealed class CreateAsaasIntegrationSavedCardCommandHandlerTests
{
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository = Substitute.For<IAsaasIntegrationSavedCardRepository>();
    private readonly IAsaasIntegrationCustomerRepository _customerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateAsaasIntegrationSavedCardCommandHandler _handler;

    public CreateAsaasIntegrationSavedCardCommandHandlerTests()
    {
        _handler = new CreateAsaasIntegrationSavedCardCommandHandler(
            _savedCardRepository, _customerRepository, _settingRepository, _asaasService, _logRepository, _unitOfWork);
    }

    private static CreateAsaasIntegrationSavedCardCommand ValidCommand(bool setAsDefault = false) =>
        new(CustomerId: 1, CompanyId: 1, HolderName: "Fulano", CardNumber: "4111111111111111",
            ExpiryMonth: "12", ExpiryYear: "2030", Ccv: "123", SetAsDefault: setAsDefault);

    [Fact]
    public async Task Handle_NoActiveSetting_ShouldReturnValidationFailure()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.NotFound");
    }

    [Fact]
    public async Task Handle_CustomerNotBoundToAsaas_ShouldReturnNotFound()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationSetting.Create(1, null, "api-key").Value);
        _customerRepository.GetByCustomerIdAndCompanyIdAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationCustomer?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomer.NotFound");
    }

    [Fact]
    public async Task Handle_TokenizeApiThrows_ShouldReturnFailure()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationSetting.Create(1, null, "api-key").Value);
        _customerRepository.GetByCustomerIdAndCompanyIdAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationCustomer.Create(1, 1, "cus_123").Value);
        _asaasService.TokenizeCreditCardAsync("cus_123", Arg.Any<CreditCardRequest>(), Arg.Any<CreditCardHolderInfoRequest?>(), Arg.Any<CancellationToken>())
            .Returns<Task<AsaasTokenizeCreditCardResponse>>(_ => throw new HttpRequestException("falhou"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Asaas.TokenizeError");
        await _savedCardRepository.DidNotReceive().AddAsync(Arg.Any<AsaasIntegrationSavedCard>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistCardWithMaskedData()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationSetting.Create(1, null, "api-key").Value);
        _customerRepository.GetByCustomerIdAndCompanyIdAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationCustomer.Create(1, 1, "cus_123").Value);
        _asaasService.TokenizeCreditCardAsync("cus_123", Arg.Any<CreditCardRequest>(), Arg.Any<CreditCardHolderInfoRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new AsaasTokenizeCreditCardResponse("card-token-1", "MASTERCARD", "4111111111111111"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CardBrand.Should().Be("MASTERCARD");
        result.Value.Last4Digits.Should().Be("1111");
        await _savedCardRepository.Received(1).AddAsync(
            Arg.Is<AsaasIntegrationSavedCard>(c => c.CreditCardToken == "card-token-1" && c.Last4Digits == "1111"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetAsDefaultTrue_ShouldUnsetPreviousDefaultCards()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationSetting.Create(1, null, "api-key").Value);
        _customerRepository.GetByCustomerIdAndCompanyIdAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationCustomer.Create(1, 1, "cus_123").Value);
        _asaasService.TokenizeCreditCardAsync("cus_123", Arg.Any<CreditCardRequest>(), Arg.Any<CreditCardHolderInfoRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new AsaasTokenizeCreditCardResponse("card-token-new", "VISA", "4111111111111111"));

        var existingDefault = AsaasIntegrationSavedCard.Create(1, 1, "card-token-old", "VISA", "0000", isDefault: true).Value;
        _savedCardRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns(new List<AsaasIntegrationSavedCard> { existingDefault });

        var result = await _handler.Handle(ValidCommand(setAsDefault: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingDefault.IsDefault.Should().BeFalse();
        _savedCardRepository.Received(1).Update(existingDefault);
        result.Value.IsDefault.Should().BeTrue();
    }
}
