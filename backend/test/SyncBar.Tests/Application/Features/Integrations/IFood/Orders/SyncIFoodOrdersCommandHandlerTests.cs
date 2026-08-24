using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Orders;

// Núcleo do fluxo essencial de sincronização de pedidos iFood — o handler mais crítico e mais
// complexo do backend (dinheiro real, SLA de 8 minutos, idempotência de eventos). ProcessEventAsync/
// ProcessNewOrderAsync/ProcessCancelledAsync são privados, então os testes abaixo exercitam tudo
// através de Handle(), montando o cenário completo de dependências (setting/token/mapping/evento)
// para cada branch relevante.
public sealed class SyncIFoodOrdersCommandHandlerTests
{
    private const long CompanyId = 1;
    private const long BranchId = 10;
    private const long EmployeeId = 5;
    private const string ValidToken = "valid-token";
    private const string MerchantId = "merchant-1";
    private const string IFoodOrderExternalId = "ifood-order-1";

    private readonly IIFoodIntegrationSettingRepository _settingRepository = Substitute.For<IIFoodIntegrationSettingRepository>();
    private readonly IIFoodTokenProvider _tokenProvider = Substitute.For<IIFoodTokenProvider>();
    private readonly IIFoodOrderClient _orderClient = Substitute.For<IIFoodOrderClient>();
    private readonly IIFoodMerchantMappingRepository _merchantMappingRepository = Substitute.For<IIFoodMerchantMappingRepository>();
    private readonly IIFoodOrderRepository _ifoodOrderRepository = Substitute.For<IIFoodOrderRepository>();
    private readonly ICustomerOrderRepository _customerOrderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IIFoodComplementMappingRepository _complementMappingRepository = Substitute.For<IIFoodComplementMappingRepository>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SyncIFoodOrdersCommandHandler CreateSut() => new(
        _settingRepository, _tokenProvider, _orderClient, _merchantMappingRepository, _ifoodOrderRepository,
        _customerOrderRepository, _productRepository, _branchRepository, _complementGroupRepository,
        _complementMappingRepository, TimeProvider.System, _cache, _logRepository, _unitOfWork);

    // ---- helpers de cenário ----

    private void GivenIntegrationEnabledWithValidToken()
    {
        var setting = IFoodIntegrationSetting.Create(CompanyId).Value;
        setting.SaveCredentials("client-id", "encrypted-secret", true, null);
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(ValidToken);
    }

    private void GivenAnActiveMerchantMapping()
    {
        var mapping = IFoodMerchantMapping.Create(BranchId).Value;
        mapping.SetMerchant(MerchantId, "merchant-uuid-1");
        _merchantMappingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IFoodMerchantMapping> { [BranchId] = mapping });
    }

    private void GivenBranchWithSelfServiceEmployee()
    {
        var branch = Branch.Create(CompanyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;
        branch.SetSelfServiceEmployee(EmployeeId);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);
    }

    private void GivenBranchWithoutSelfServiceEmployee()
    {
        var branch = Branch.Create(CompanyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);
    }

    private void GivenIFoodConfirmsTheOrder()
        => _orderClient.ConfirmOrderAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>())
            .Returns(new IFoodOrderActionResult(true, null));

    private static IFoodPollingEvent ConfirmedEvent(string eventId = "evt-1") =>
        new(eventId, "CONFIRMED", null, IFoodOrderExternalId, DateTime.Now);

    private static IFoodOrderDetailsDto OrderDetailsWithItems(params IFoodOrderItemDto[] items) => new(
        Id: IFoodOrderExternalId, DisplayId: "001", OrderType: "DELIVERY", OrderTiming: "IMMEDIATE",
        Category: "FOOD", CreatedAt: DateTime.Now, PreparationStartDateTime: null, MerchantId: MerchantId,
        CustomerName: "Maria Silva", CustomerPhone: "11999999999", DeliveryAddressFormatted: "Rua das Flores, 100",
        DeliveredBy: "IFOOD", TakeoutMode: null, OrderAmount: 29.80m, Items: items);

    private (CustomerOrder? Captured, Func<CustomerOrder?> Get) CaptureCustomerOrderAdded()
    {
        CustomerOrder? captured = null;
        _customerOrderRepository.AddAsync(Arg.Do<CustomerOrder>(co => captured = co), Arg.Any<CancellationToken>());
        return (null, () => captured);
    }

    private Func<IFoodOrder?> CaptureIFoodOrderAdded()
    {
        IFoodOrder? captured = null;
        _ifoodOrderRepository.AddAsync(Arg.Do<IFoodOrder>(io => captured = io), Arg.Any<CancellationToken>());
        // GetByIdForUpdateAsync(0, ...) é chamado logo depois do AddAsync (Id nunca é persistido de
        // verdade aqui, já que IUnitOfWork.CommitAsync é um mock) — retorna a MESMA instância
        // capturada, avaliada lazy (Returns(x => ...)) porque a captura só acontece durante o Handle().
        _ifoodOrderRepository.GetByIdForUpdateAsync(0, Arg.Any<CancellationToken>()).Returns(_ => captured);
        return () => captured;
    }

    // ---- guardas de configuração/rede (nenhuma delas deve chegar a fazer polling) ----

    [Fact]
    public async Task Handle_WhenIntegrationSettingMissing_ShouldSucceedWithoutPolling()
    {
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((IFoodIntegrationSetting?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.DidNotReceive().PollEventsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIntegrationDisabled_ShouldSucceedWithoutPolling()
    {
        var setting = IFoodIntegrationSetting.Create(CompanyId).Value;
        setting.SaveCredentials("client-id", "encrypted-secret", enabled: false, null);
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.DidNotReceive().PollEventsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenUnavailable_ShouldSucceedWithoutPolling()
    {
        var setting = IFoodIntegrationSetting.Create(CompanyId).Value;
        setting.SaveCredentials("client-id", "encrypted-secret", true, null);
        _settingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.DidNotReceive().PollEventsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoActiveMerchantMappings_ShouldSucceedWithoutPolling()
    {
        GivenIntegrationEnabledWithValidToken();
        _merchantMappingRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IFoodMerchantMapping>());
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.DidNotReceive().PollEventsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoEvents_ShouldSucceedWithoutAcknowledging()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IFoodPollingEvent>)Array.Empty<IFoodPollingEvent>());
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.DidNotReceive().AcknowledgeEventsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    // ---- evento CONFIRMED: pedido novo ----

    [Fact]
    public async Task Handle_ConfirmedEvent_WhenOrderAlreadyExists_ShouldAcknowledgeWithoutCreatingDuplicate()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { ConfirmedEvent() });
        _ifoodOrderRepository.GetByIFoodOrderIdAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>())
            .Returns(IFoodOrder.Create(1, BranchId, IFoodOrderExternalId, "001", MerchantId, "DELIVERY", "IFOOD", "IMMEDIATE", null, DateTime.Now, false).Value);
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerOrderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
        await _orderClient.Received(1).AcknowledgeEventsAsync(ValidToken, Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("evt-1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmedEvent_WhenOrderDetailsNotYetAvailable_ShouldNotAcknowledge()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { ConfirmedEvent() });
        _ifoodOrderRepository.GetByIFoodOrderIdAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        _orderClient.GetOrderDetailsAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrderDetailsDto?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.DidNotReceive().AcknowledgeEventsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmedEvent_WhenMerchantNotMapped_ShouldNotAcknowledge()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { ConfirmedEvent() });
        _ifoodOrderRepository.GetByIFoodOrderIdAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        _orderClient.GetOrderDetailsAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>())
            .Returns(OrderDetailsWithItems() with { MerchantId = "outro-merchant-nao-mapeado" });
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.DidNotReceive().AcknowledgeEventsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmedEvent_WhenBranchHasNoSelfServiceEmployee_ShouldNotAcknowledge()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        GivenBranchWithoutSelfServiceEmployee();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { ConfirmedEvent() });
        _ifoodOrderRepository.GetByIFoodOrderIdAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        _orderClient.GetOrderDetailsAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns(OrderDetailsWithItems());
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.DidNotReceive().AcknowledgeEventsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmedEvent_WithMatchedProduct_ShouldCreateOrderAndAcknowledge()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        GivenBranchWithSelfServiceEmployee();
        GivenIFoodConfirmsTheOrder();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { ConfirmedEvent() });
        _ifoodOrderRepository.GetByIFoodOrderIdAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        _orderClient.GetOrderDetailsAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns(OrderDetailsWithItems(
            new IFoodOrderItemDto(null, "7890000000001", "Cerveja Long Neck", 2, 14.90m, [])));
        var product = Product.Create(CompanyId, 1, 1, "Cerveja Long Neck", null, "7890000000001", 14.90m, null, false, null).Value;
        _productRepository.GetByBarcodeAsync(CompanyId, "7890000000001", Arg.Any<CancellationToken>()).Returns(product);
        var getCustomerOrder = CaptureCustomerOrderAdded().Get;
        var getIFoodOrder = CaptureIFoodOrderAdded();
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var customerOrder = getCustomerOrder();
        customerOrder.Should().NotBeNull();
        customerOrder!.Items.Should().ContainSingle();
        customerOrder.TotalAmount.Should().Be(29.80m);
        customerOrder.CustomerName.Should().Be("Maria Silva");
        var ifoodOrder = getIFoodOrder();
        ifoodOrder.Should().NotBeNull();
        ifoodOrder!.HasUnmappedItems.Should().BeFalse();
        ifoodOrder.IFoodOrderId.Should().Be(IFoodOrderExternalId);
        await _orderClient.Received(1).AcknowledgeEventsAsync(ValidToken, Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("evt-1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmedEvent_WithUnmappedItem_ShouldFlagHasUnmappedItemsButStillCreateOrder()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        GivenBranchWithSelfServiceEmployee();
        GivenIFoodConfirmsTheOrder();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { ConfirmedEvent() });
        _ifoodOrderRepository.GetByIFoodOrderIdAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        // Ean sem correspondência no catálogo (GetByBarcodeAsync não configurado devolve null).
        _orderClient.GetOrderDetailsAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns(OrderDetailsWithItems(
            new IFoodOrderItemDto(null, "codigo-desconhecido", "Item Misterioso", 1, 10m, [])));
        var getCustomerOrder = CaptureCustomerOrderAdded().Get;
        var getIFoodOrder = CaptureIFoodOrderAdded();
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        getCustomerOrder()!.Items.Should().BeEmpty();
        getIFoodOrder()!.HasUnmappedItems.Should().BeTrue();
        await _orderClient.Received(1).AcknowledgeEventsAsync(ValidToken, Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("evt-1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfirmedEvent_WithMappedComplementOption_ShouldAddComplementToOrderItem()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        GivenBranchWithSelfServiceEmployee();
        GivenIFoodConfirmsTheOrder();

        var group = ComplementGroup.Create(CompanyId, "Adicionais", ComplementGroupTypeIds.SelecaoAdicional, 0, 3).Value;
        var complement = group.AddComplement(complementItemId: 1, extraPrice: 3.50m).Value;
        _complementGroupRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(new List<ComplementGroup> { group });
        var complementMapping = IFoodComplementMapping.Create(complement.Id, BranchId).Value;
        _complementMappingRepository.GetByIFoodOptionIdAndBranchAsync(complementMapping.IFoodOptionId, BranchId, Arg.Any<CancellationToken>())
            .Returns(complementMapping);

        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { ConfirmedEvent() });
        _ifoodOrderRepository.GetByIFoodOrderIdAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        _orderClient.GetOrderDetailsAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns(OrderDetailsWithItems(
            new IFoodOrderItemDto(null, "7890000000001", "Hambúrguer", 1, 25m,
                [new IFoodOrderItemOptionDto(complementMapping.IFoodOptionId.ToString(), "Bacon extra", 1, 3.50m)])));
        var product = Product.Create(CompanyId, 1, 1, "Hambúrguer", null, "7890000000001", 25m, null, false, null).Value;
        _productRepository.GetByBarcodeAsync(CompanyId, "7890000000001", Arg.Any<CancellationToken>()).Returns(product);
        var getCustomerOrder = CaptureCustomerOrderAdded().Get;
        CaptureIFoodOrderAdded();
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var orderItem = getCustomerOrder()!.Items.Single();
        orderItem.Complements.Should().ContainSingle(c => c.ComplementId == complement.Id && c.UnitPriceCharged == 3.50m);
    }

    [Fact]
    public async Task Handle_ConfirmedEvent_WhenIFoodConfirmsSuccessfully_ShouldMarkIFoodOrderConfirmed()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        GivenBranchWithSelfServiceEmployee();
        GivenIFoodConfirmsTheOrder();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { ConfirmedEvent() });
        _ifoodOrderRepository.GetByIFoodOrderIdAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        _orderClient.GetOrderDetailsAsync(ValidToken, IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns(OrderDetailsWithItems());
        CaptureCustomerOrderAdded();
        var getIFoodOrder = CaptureIFoodOrderAdded();
        var sut = CreateSut();

        await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        getIFoodOrder()!.Status.Should().Be(IFoodOrderStatuses.Confirmed);
        getIFoodOrder()!.ConfirmedAt.Should().NotBeNull();
    }

    // ---- evento CANCELLED ----

    [Fact]
    public async Task Handle_CancelledEvent_WhenIFoodOrderNotFoundLocally_ShouldAcknowledgeWithoutFurtherAction()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { new("evt-cancel", "CANCELLED", null, IFoodOrderExternalId, DateTime.Now) });
        _ifoodOrderRepository.GetByIFoodOrderIdForUpdateAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns((IFoodOrder?)null);
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.Received(1).AcknowledgeEventsAsync(ValidToken, Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("evt-cancel")), Arg.Any<CancellationToken>());
        await _customerOrderRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancelledEvent_WhenCustomerOrderNotYetPaid_ShouldCancelBothOrders()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        var ifoodOrder = IFoodOrder.Create(1, BranchId, IFoodOrderExternalId, "001", MerchantId, "DELIVERY", "IFOOD", "IMMEDIATE", null, DateTime.Now, false).Value;
        var customerOrder = CustomerOrder.Create(
            BranchId, null, null, EmployeeId, null, null, DateTime.Now, orderTypeId: OrderTypeIds.Delivery,
            customerName: "Maria Silva", deliveryAddress: "Rua das Flores, 100").Value;
        customerOrder.AddItem(1, 20m, 1, null, null, DateTime.Now);
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { new("evt-cancel", "CANCELLED", null, IFoodOrderExternalId, DateTime.Now) });
        _ifoodOrderRepository.GetByIFoodOrderIdForUpdateAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns(ifoodOrder);
        _customerOrderRepository.GetByIdForUpdateAsync(ifoodOrder.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(customerOrder);
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ifoodOrder.Status.Should().Be(IFoodOrderStatuses.Cancelled);
        customerOrder.OrderStatusId.Should().Be(OrderStatusIds.Cancelado);
    }

    [Fact]
    public async Task Handle_CancelledEvent_WhenCustomerOrderAlreadyPaid_ShouldNotCancelCustomerOrder()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        var ifoodOrder = IFoodOrder.Create(1, BranchId, IFoodOrderExternalId, "001", MerchantId, "DELIVERY", "IFOOD", "IMMEDIATE", null, DateTime.Now, false).Value;
        var customerOrder = CustomerOrder.Create(
            BranchId, null, null, EmployeeId, null, null, DateTime.Now, orderTypeId: OrderTypeIds.Delivery,
            customerName: "Maria Silva", deliveryAddress: "Rua das Flores, 100").Value;
        customerOrder.AddItem(1, 20m, 1, null, null, DateTime.Now);
        customerOrder.Close(0m, DateTime.Now);
        customerOrder.MarkAsPaid(DateTime.Now);
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { new("evt-cancel", "CANCELLED", null, IFoodOrderExternalId, DateTime.Now) });
        _ifoodOrderRepository.GetByIFoodOrderIdForUpdateAsync(IFoodOrderExternalId, Arg.Any<CancellationToken>()).Returns(ifoodOrder);
        _customerOrderRepository.GetByIdForUpdateAsync(ifoodOrder.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(customerOrder);
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Pedido iFood ainda reflete o cancelamento (lado do iFood é independente), mas o pedido
        // já pago no SyncBar não é mexido — dinheiro já recebido não pode "sumir" de um cancelamento tardio.
        ifoodOrder.Status.Should().Be(IFoodOrderStatuses.Cancelled);
        customerOrder.OrderStatusId.Should().Be(OrderStatusIds.Pago);
    }

    // ---- evento fora de escopo ----

    [Fact]
    public async Task Handle_UnknownEventCode_ShouldAcknowledgeWithoutProcessing()
    {
        GivenIntegrationEnabledWithValidToken();
        GivenAnActiveMerchantMapping();
        _orderClient.PollEventsAsync(ValidToken, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<IFoodPollingEvent> { new("evt-other", "ASSIGN_DRIVER", null, IFoodOrderExternalId, DateTime.Now) });
        var sut = CreateSut();

        var result = await sut.Handle(new SyncIFoodOrdersCommand(CompanyId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _orderClient.Received(1).AcknowledgeEventsAsync(ValidToken, Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("evt-other")), Arg.Any<CancellationToken>());
        await _ifoodOrderRepository.DidNotReceive().GetByIFoodOrderIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
