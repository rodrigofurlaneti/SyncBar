using System.Reflection;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Features.Checkout.PayOrderWithPix;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Checkout.PayOrderWithPix;

public sealed class PayOrderWithPixCommandHandlerTests
{
    private readonly ICheckoutOrderPreparer _checkoutPreparer = Substitute.For<ICheckoutOrderPreparer>();
    private readonly IAsaasIntegrationPaymentRepository _paymentRepository = Substitute.For<IAsaasIntegrationPaymentRepository>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly PayOrderWithPixCommandHandler _handler;

    public PayOrderWithPixCommandHandlerTests()
    {
        _handler = new PayOrderWithPixCommandHandler(
            _checkoutPreparer, _paymentRepository, _asaasService, _mediator, _logRepository, _unitOfWork);
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

        var result = await _handler.Handle(new PayOrderWithPixCommand(99), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
    }

    [Fact]
    public async Task Handle_NoExistingPayment_ShouldChargeViaMediatorAndReturnResponse()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        _mediator.Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new CreateAsaasIntegrationPaymentResponse(
                10, "pay_pix_1", "PENDING", "base64qrcode", "00020126...", "https://asaas.test/invoice", null)));

        var result = await _handler.Handle(new PayOrderWithPixCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentId.Should().Be(10);
        result.Value.AsaasPaymentId.Should().Be("pay_pix_1");
        result.Value.PixQrCodeBase64.Should().Be("base64qrcode");
        result.Value.Value.Should().Be(order.TotalAmount);
        await _mediator.Received(1).Send(
            Arg.Is<CreateAsaasIntegrationPaymentCommand>(c => c.BillingType == "PIX"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingPendingPayment_ShouldReturnItWithoutChargingAgain()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);

        var existing = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_pix_2", "PIX", 50m, DateTime.UtcNow.AddHours(1)).Value;
        existing.SetPixDetails("existing-qrcode", "existing-payload");
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(new PayOrderWithPixCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PixQrCodeBase64.Should().Be("existing-qrcode");
        result.Value.PixPayload.Should().Be("existing-payload");
        await _mediator.DidNotReceive().Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>());
    }

    // Cobre o bug real: uma tentativa anterior criou a cobrança Pix no Asaas mas falhou ao buscar
    // o QR Code (hiccup pontual do gateway) — o registro fica salvo com PixQrCodeBase64 nulo. Sem
    // este re-fetch, a idempotência acima devolveria pra sempre esse mesmo registro quebrado, e o
    // cliente nunca mais veria o QR Code, mesmo reabrindo a tela de pagamento.
    [Fact]
    public async Task Handle_ExistingPendingPaymentWithoutQrCode_ShouldRefetchAndPersistQrCode()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);

        var existing = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_pix_4", "PIX", 50m, DateTime.UtcNow.AddHours(1)).Value;
        SetId(existing, 77);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var trackedPayment = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_pix_4", "PIX", 50m, DateTime.UtcNow.AddHours(1)).Value;
        SetId(trackedPayment, 77);
        _paymentRepository.GetByIdForUpdateAsync(77, Arg.Any<CancellationToken>()).Returns(trackedPayment);

        _asaasService.GetPixQrCodeAsync("pay_pix_4", Arg.Any<CancellationToken>())
            .Returns(new AsaasPixQrCodeResponse("refetched-qrcode", "refetched-payload", null));

        var result = await _handler.Handle(new PayOrderWithPixCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PixQrCodeBase64.Should().Be("refetched-qrcode");
        result.Value.PixPayload.Should().Be("refetched-payload");
        trackedPayment.PixQrCodeBase64.Should().Be("refetched-qrcode");
        // 2 commits: o do re-fetch de QR Code (dentro do handler) + o do log de auditoria da
        // BaseCommandHandler (sempre roda no finally, independente do resultado de negócio).
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
        await _mediator.DidNotReceive().Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>());
    }

    // Se o re-fetch também falhar (gateway ainda indisponível), a operação continua bem-sucedida
    // (idempotente) devolvendo sem QR Code — o cliente pode tentar de novo depois.
    [Fact]
    public async Task Handle_ExistingPendingPaymentWithoutQrCode_WhenRefetchFails_ShouldStillSucceedWithoutQrCode()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);

        var existing = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_pix_5", "PIX", 50m, DateTime.UtcNow.AddHours(1)).Value;
        SetId(existing, 78);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _asaasService.GetPixQrCodeAsync("pay_pix_5", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Asaas indisponível"));

        var result = await _handler.Handle(new PayOrderWithPixCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PixQrCodeBase64.Should().BeNull();
        await _paymentRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingSettledPayment_ShouldReturnConflict()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);

        var existing = AsaasIntegrationPayment.Create(
            order.BranchId, order.Id, order.CustomerId, "pay_pix_3", "PIX", 50m, DateTime.UtcNow.AddHours(1)).Value;
        existing.MarkAsPaid(50m);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(new PayOrderWithPixCommand(order.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Asaas.PaymentAlreadyExists");
    }

    [Fact]
    public async Task Handle_ChargeFails_ShouldPropagateFailure()
    {
        var order = CreateAwaitingPaymentOrder();
        GivenPreparationSucceeds(order);
        _paymentRepository.GetByCustomerOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationPayment?)null);

        _mediator.Send(Arg.Any<CreateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CreateAsaasIntegrationPaymentResponse>(new Error("AsaasApi.Failure", "Falha ao criar cobrança.")));

        var result = await _handler.Handle(new PayOrderWithPixCommand(order.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasApi.Failure");
    }
}
