using System.Reflection;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Features.Checkout.PayOrderWithCreditCard;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Checkout.PayOrderWithCreditCard;

public sealed class PayOrderWithCreditCardCommandHandlerTests
{
    private readonly ICheckoutOrderPreparer _checkoutPreparer = Substitute.For<ICheckoutOrderPreparer>();
    private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository = Substitute.For<IAsaasIntegrationSavedCardRepository>();
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly PayOrderWithCreditCardCommandHandler _handler;

    public PayOrderWithCreditCardCommandHandlerTests()
    {
        _handler = new PayOrderWithCreditCardCommandHandler(
            _checkoutPreparer, _savedCardRepository, _paymentRepository, _asaasService, _mediator, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static CustomerOrder CreateAwaitingPaymentOrder(long id = 1, long customerId = 1)
    {
        var order = CustomerOrder.Create(
            branchId: 1, diningTableId: null, comandaId: null, employeeId: 1, guestCount: null, notes: null,
            Now: DateTime.UtcNow, orderTypeId: OrderTypeIds.WebSite, customerName: "Cliente Teste", customerId: customerId).Value;
        order.AddItem(1, 50m, 1, null, null, DateTime.UtcNow);
        order.Close(0m, DateTime.UtcNow);
        SetId(order, id);
        return order;
    }

    private static Customer CreateCustomer(long id = 1, long companyId = 1)
    {
        var customer = Customer.Create(companyId, "Fabio Cardoso", "11999999999", "12345678900", "fabio@teste.com").Value;
        SetId(customer, id);
        return customer;
    }

    private static Branch CreateBranch(long companyId = 1) =>
        Branch.Create(companyId, "Matriz", null, null, null, null, null, null, null, null).Value;

    private static CreditCardDataRequest CreateCardData() =>
        new("Fabio Cardoso", "4111111111111111", "12", "2030", "123");

    private void GivenPreparationSucceeds(CustomerOrder order, Customer customer, Branch branch, string asaasCustomerId = "cus_asaas_1")
        => _checkoutPreparer.PrepareAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CheckoutOrderPreparation(order, customer, branch, asaasCustomerId)));

    [Fact]
    public async Task Handle_WhenPreparationFails_ShouldPropagateFailure()
    {
        _checkoutPreparer.PrepareAsync(99, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CheckoutOrderPreparation>(new Error("CustomerOrder.NotFound", "Pedido não encontrado.")));

        var result = await _handler.Handle(new PayOrderWithCreditCardCommand(99, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
    }

    [Fact]
    public async Task Handle_ExistingPendingPayment_ShouldReturnItWithoutChargingAgain()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order, CreateCustomer(), CreateBranch());

        var existing = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_1", "CREDIT_CARD", 50m, DateTime.UtcNow).Value;
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(
            new PayOrderWithCreditCardCommand(order.Id, 1, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AsaasPaymentId.Should().Be("pay_1");
        await _mediator.DidNotReceive().Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSettledPayment_ShouldReturnConflict()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order, CreateCustomer(), CreateBranch());

        var existing = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_1", "CREDIT_CARD", 50m, DateTime.UtcNow).Value;
        existing.MarkAsPaid(50m);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(
            new PayOrderWithCreditCardCommand(order.Id, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Asaas.PaymentAlreadyExists");
    }

    [Fact]
    public async Task Handle_SavedCardNotOwnedByCustomer_ShouldReturnValidationFailure()
    {
        var order = CreateAwaitingPaymentOrder(customerId: 1);
        GivenPreparationSucceeds(order, CreateCustomer(), CreateBranch());
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        var otherCustomersCard = AsaasIntegrationSavedCard.Create(999, 1, "tok_1", "VISA", "1111").Value;
        _savedCardRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(otherCustomersCard);

        var result = await _handler.Handle(
            new PayOrderWithCreditCardCommand(order.Id, 7, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SavedCard.NotOwnedByCustomer");
    }

    [Fact]
    public async Task Handle_SavedCard_ShouldChargeWithItsToken()
    {
        var order = CreateAwaitingPaymentOrder(customerId: 1);
        GivenPreparationSucceeds(order, CreateCustomer(), CreateBranch());
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        var savedCard = AsaasIntegrationSavedCard.Create(1, 1, "tok_saved", "MASTERCARD", "4242").Value;
        _savedCardRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(savedCard);

        _mediator.Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CreateAsaasIntegrationPaymentResponse(10, "pay_10", "PENDING", null, null, null, null)));

        var result = await _handler.Handle(
            new PayOrderWithCreditCardCommand(order.Id, 7, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CardBrand.Should().Be("MASTERCARD");
        result.Value.Last4Digits.Should().Be("4242");
        await _mediator.Received(1).Send(
            Arg.Is<CreateAsaasIntegrationPaymentCommand>(c => c.CreditCardToken == "tok_saved" && c.BillingType == "CREDIT_CARD"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewCardWithSaveCard_ShouldTokenizePersistAndCharge()
    {
        var order = CreateAwaitingPaymentOrder(customerId: 1);
        GivenPreparationSucceeds(order, CreateCustomer(), CreateBranch());
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        _mediator.Send(Arg.Any<CreateAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CreateAsaasIntegrationSavedCardResponse(55, 1, 1, "VISA", "1234", false)));

        var persistedCard = AsaasIntegrationSavedCard.Create(1, 1, "tok_new", "VISA", "1234").Value;
        _savedCardRepository.GetByIdAsync(55, Arg.Any<CancellationToken>()).Returns(persistedCard);

        _mediator.Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CreateAsaasIntegrationPaymentResponse(11, "pay_11", "PENDING", null, null, null, null)));

        var result = await _handler.Handle(
            new PayOrderWithCreditCardCommand(order.Id, null, CreateCardData(), SaveCard: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _asaasService.DidNotReceive().TokenizeCreditCardAsync(
            Arg.Any<string>(), Arg.Any<CreditCardRequest>(), Arg.Any<CreditCardHolderInfoRequest?>(), Arg.Any<CancellationToken>());
        await _mediator.Received(1).Send(
            Arg.Is<CreateAsaasIntegrationPaymentCommand>(c => c.CreditCardToken == "tok_new"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewCardWithoutSaving_ShouldTokenizeOnTheFlyWithoutPersistingSavedCard()
    {
        var order = CreateAwaitingPaymentOrder(customerId: 1);
        GivenPreparationSucceeds(order, CreateCustomer(), CreateBranch());
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        _asaasService.TokenizeCreditCardAsync(
                "cus_asaas_1", Arg.Any<CreditCardRequest>(), Arg.Any<CreditCardHolderInfoRequest?>(), Arg.Any<CancellationToken>())
            .Returns(new AsaasTokenizeCreditCardResponse("tok_onetime", "ELO", "5555444433332222"));

        _mediator.Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CreateAsaasIntegrationPaymentResponse(12, "pay_12", "PENDING", null, null, null, null)));

        var result = await _handler.Handle(
            new PayOrderWithCreditCardCommand(order.Id, null, CreateCardData(), SaveCard: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CardBrand.Should().Be("ELO");
        result.Value.Last4Digits.Should().Be("2222");
        await _mediator.DidNotReceive().Send(Arg.Any<CreateAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>());
        await _mediator.Received(1).Send(
            Arg.Is<CreateAsaasIntegrationPaymentCommand>(c => c.CreditCardToken == "tok_onetime"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenizeThrowsHttpRequestException_ShouldReturnFailure()
    {
        var order = CreateAwaitingPaymentOrder(customerId: 1);
        GivenPreparationSucceeds(order, CreateCustomer(), CreateBranch());
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        _asaasService.TokenizeCreditCardAsync(
                "cus_asaas_1", Arg.Any<CreditCardRequest>(), Arg.Any<CreditCardHolderInfoRequest?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("gateway timeout"));

        var result = await _handler.Handle(
            new PayOrderWithCreditCardCommand(order.Id, null, CreateCardData(), SaveCard: false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasApi.TokenizeCardFailed");
        await _mediator.DidNotReceive().Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>());
    }
}
