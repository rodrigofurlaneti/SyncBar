using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Checkout.Shared;

public sealed class CheckoutOrderPreparerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IAsaasCustomerProvisioningService _customerProvisioner = Substitute.For<IAsaasCustomerProvisioningService>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CheckoutOrderPreparer _preparer;

    public CheckoutOrderPreparerTests()
    {
        _preparer = new CheckoutOrderPreparer(
            _orderRepository, _customerRepository, _branchRepository, _customerProvisioner, _asaasService, TimeProvider.System, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static CustomerOrder CreateOrder(long id = 1, long? customerId = 1)
    {
        var order = CustomerOrder.Create(
            branchId: 1, diningTableId: null, comandaId: null, employeeId: 1, guestCount: null, notes: null,
            Now: DateTime.UtcNow, orderTypeId: OrderTypeIds.WebSite, customerName: "Cliente Teste", customerId: customerId).Value;
        order.AddItem(1, 50m, 1, null, null, DateTime.UtcNow);
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

    [Fact]
    public async Task PrepareAsync_OrderNotFound_ShouldReturnNotFound()
    {
        _orderRepository.GetByIdForUpdateAsync(99, Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);

        var result = await _preparer.PrepareAsync(99, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
    }

    [Theory]
    [InlineData(OrderStatusIds.Aberto)]
    [InlineData(OrderStatusIds.EmAndamento)]
    public async Task PrepareAsync_OrderStillOpen_ShouldAutoCloseAndSucceed(long initialStatus)
    {
        var order = CreateOrder();
        typeof(CustomerOrder).GetProperty(nameof(CustomerOrder.OrderStatusId))!.SetValue(order, initialStatus);
        _orderRepository.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _customerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateCustomer());
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _customerProvisioner.EnsureLinkedAsync(Arg.Any<Customer>(), Arg.Any<long>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("cus_asaas_1"));

        var result = await _preparer.PrepareAsync(order.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(OrderStatusIds.Pago)]
    [InlineData(OrderStatusIds.Cancelado)]
    public async Task PrepareAsync_OrderInNonPayableState_ShouldReturnValidationFailure(long status)
    {
        var order = CreateOrder();
        typeof(CustomerOrder).GetProperty(nameof(CustomerOrder.OrderStatusId))!.SetValue(order, status);
        _orderRepository.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _preparer.PrepareAsync(order.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotPayable");
    }

    [Fact]
    public async Task PrepareAsync_OrderWithoutCustomer_ShouldReturnValidationFailure()
    {
        var order = CreateOrder(customerId: null);
        typeof(CustomerOrder).GetProperty(nameof(CustomerOrder.OrderStatusId))!.SetValue(order, OrderStatusIds.AguardandoPagamento);
        _orderRepository.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _preparer.PrepareAsync(order.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.CustomerRequired");
    }

    [Fact]
    public async Task PrepareAsync_ProvisioningFails_ShouldPropagateError()
    {
        var order = CreateOrder();
        typeof(CustomerOrder).GetProperty(nameof(CustomerOrder.OrderStatusId))!.SetValue(order, OrderStatusIds.AguardandoPagamento);
        _orderRepository.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _customerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateCustomer());
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _customerProvisioner.EnsureLinkedAsync(Arg.Any<Customer>(), Arg.Any<long>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(new Error("Customer.IncompleteProfile", "CPF e e-mail são obrigatórios.")));

        var result = await _preparer.PrepareAsync(order.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.IncompleteProfile");
    }

    [Fact]
    public async Task PrepareAsync_AlreadyAwaitingPayment_ShouldNotCloseAgain()
    {
        var order = CreateOrder();
        typeof(CustomerOrder).GetProperty(nameof(CustomerOrder.OrderStatusId))!.SetValue(order, OrderStatusIds.AguardandoPagamento);
        _orderRepository.GetByIdForUpdateAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _customerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateCustomer());
        _branchRepository.GetByIdAsync(order.BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _customerProvisioner.EnsureLinkedAsync(Arg.Any<Customer>(), Arg.Any<long>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("cus_asaas_1"));

        var result = await _preparer.PrepareAsync(order.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
