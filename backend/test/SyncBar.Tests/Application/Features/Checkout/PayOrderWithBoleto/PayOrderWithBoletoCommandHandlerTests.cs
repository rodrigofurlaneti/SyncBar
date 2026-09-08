using System.Reflection;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Features.Checkout.PayOrderWithBoleto;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Checkout.PayOrderWithBoleto;

public sealed class PayOrderWithBoletoCommandHandlerTests
{
    private readonly ICheckoutOrderPreparer _checkoutPreparer = Substitute.For<ICheckoutOrderPreparer>();
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly PayOrderWithBoletoCommandHandler _handler;

    public PayOrderWithBoletoCommandHandlerTests()
    {
        _handler = new PayOrderWithBoletoCommandHandler(
            _checkoutPreparer, _paymentRepository, _asaasService, _mediator, _logRepository, _unitOfWork, SyncBar.Tests.PaymentAvailabilityFixture.Allowed());
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

    private void GivenPreparationSucceeds(CustomerOrder order)
        => _checkoutPreparer.PrepareAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CheckoutOrderPreparation(order, CreateCustomer(), CreateBranch(), "cus_asaas_1")));

    [Fact]
    public async Task Handle_WhenPreparationFails_ShouldPropagateFailure()
    {
        _checkoutPreparer.PrepareAsync(99, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CheckoutOrderPreparation>(new Error("CustomerOrder.NotFound", "Pedido não encontrado.")));

        var result = await _handler.Handle(new PayOrderWithBoletoCommand(99), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
    }

    [Fact]
    public async Task Handle_NoExistingPayment_ShouldCreateBoletoAndFetchIdentificationField()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        _mediator.Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CreateAsaasIntegrationPaymentResponse(
                20, "pay_boleto_1", "PENDING", null, null, null, "https://asaas.test/boleto.pdf")));

        _asaasService.GetBoletoIdentificationFieldAsync("pay_boleto_1", Arg.Any<CancellationToken>())
            .Returns(new AsaasBoletoIdentificationFieldResponse("34191.79001 01043.510047 91020.150008 1 90000010000", "34191900000...", "091020150008"));

        var result = await _handler.Handle(new PayOrderWithBoletoCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BankSlipUrl.Should().Be("https://asaas.test/boleto.pdf");
        result.Value.IdentificationField.Should().StartWith("34191");
        await _mediator.Received(1).Send(
            Arg.Is<CreateAsaasIntegrationPaymentCommand>(c => c.BillingType == "BOLETO"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingPendingPayment_ShouldRefetchIdentificationFieldWithoutChargingAgain()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);

        var existing = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_boleto_2", "BOLETO", 50m, DateTime.UtcNow.AddDays(3)).Value;
        existing.SetUrls(null, "https://asaas.test/boleto2.pdf");
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existing);

        _asaasService.GetBoletoIdentificationFieldAsync("pay_boleto_2", Arg.Any<CancellationToken>())
            .Returns(new AsaasBoletoIdentificationFieldResponse("linha-digitavel-existente", null, null));

        var result = await _handler.Handle(new PayOrderWithBoletoCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IdentificationField.Should().Be("linha-digitavel-existente");
        await _mediator.DidNotReceive().Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdentificationFieldFetchFails_ShouldStillSucceedWithNullField()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        _mediator.Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CreateAsaasIntegrationPaymentResponse(
                21, "pay_boleto_3", "PENDING", null, null, null, "https://asaas.test/boleto3.pdf")));

        _asaasService.GetBoletoIdentificationFieldAsync("pay_boleto_3", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        var result = await _handler.Handle(new PayOrderWithBoletoCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IdentificationField.Should().BeNull();
        result.Value.BankSlipUrl.Should().Be("https://asaas.test/boleto3.pdf");
    }

    [Fact]
    public async Task Handle_ExistingSettledPayment_ShouldReturnConflict()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);

        var existing = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_boleto_4", "BOLETO", 50m, DateTime.UtcNow.AddDays(3)).Value;
        existing.MarkAsPaid(50m);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(new PayOrderWithBoletoCommand(order.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Asaas.PaymentAlreadyExists");
    }
}

