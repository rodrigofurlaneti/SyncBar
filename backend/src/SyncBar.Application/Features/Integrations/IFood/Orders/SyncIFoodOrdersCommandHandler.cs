using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

/// <summary>
/// Núcleo do "fluxo essencial" de sincronização de pedidos: busca eventos novos (polling),
/// cria o pedido no SyncBar quando chega um pedido novo, confirma automaticamente dentro do SLA
/// de 8 minutos, e reflete cancelamentos vindos do iFood. Avançar status manualmente (iniciar
/// preparo/pronto/despachar) e cancelar pelo SyncBar ficam nos commands
/// Start/MarkReady/CancelIFoodOrderCommand, disparados pela tela "Pedidos iFood".
///
/// Eventos fora desse escopo (ASSIGN_DRIVER, ORDER_PATCHED, HANDSHAKE_*, rastreamento de
/// entrega, etc.) são reconhecidos (acknowledgment) mas não processados — ver
/// ifood-integration-status no projeto claude.ai para o que ainda falta.
/// </summary>
internal sealed class SyncIFoodOrdersCommandHandler : BaseCommandHandler<SyncIFoodOrdersCommand>
{
    private readonly IIFoodIntegrationSettingRepository _settingRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodOrderClient _orderClient;
    private readonly IIFoodMerchantMappingRepository _merchantMappingRepository;
    private readonly IIFoodOrderRepository _ifoodOrderRepository;
    private readonly ICustomerOrderRepository _customerOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public SyncIFoodOrdersCommandHandler(
        IIFoodIntegrationSettingRepository settingRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodOrderClient orderClient,
        IIFoodMerchantMappingRepository merchantMappingRepository,
        IIFoodOrderRepository ifoodOrderRepository,
        ICustomerOrderRepository customerOrderRepository,
        IProductRepository productRepository,
        IBranchRepository branchRepository,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _settingRepository = settingRepository;
        _tokenProvider = tokenProvider;
        _orderClient = orderClient;
        _merchantMappingRepository = merchantMappingRepository;
        _ifoodOrderRepository = ifoodOrderRepository;
        _customerOrderRepository = customerOrderRepository;
        _productRepository = productRepository;
        _branchRepository = branchRepository;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SyncIFoodOrdersCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SyncIFoodOrdersCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var setting = await _settingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                if (setting is null || !setting.Enabled || setting.ClientId is null)
                    return Result.Success();

                var token = await _tokenProvider.GetAccessTokenAsync(request.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Success(); // sem token válido — tenta de novo no próximo ciclo

                var events = await _orderClient.PollEventsAsync(token, cancellationToken);
                if (events.Count == 0)
                    return Result.Success();

                var mappings = await _merchantMappingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var acknowledgeIds = new List<string>();

                foreach (var evt in events)
                {
                    bool shouldAcknowledge;
                    try
                    {
                        shouldAcknowledge = await ProcessEventAsync(evt, request.CompanyId, token, mappings, now, cancellationToken);
                    }
                    catch
                    {
                        // Qualquer falha inesperada num evento não derruba o ciclo inteiro — os
                        // outros eventos deste lote continuam sendo processados normalmente.
                        shouldAcknowledge = false;
                    }

                    if (shouldAcknowledge)
                        acknowledgeIds.Add(evt.Id);
                }

                if (acknowledgeIds.Count > 0)
                    await _orderClient.AcknowledgeEventsAsync(token, acknowledgeIds, cancellationToken);

                return Result.Success();
            });
    }

    // Retorna true se o evento deve ser confirmado (acknowledgment) — false faz ele voltar no
    // próximo ciclo de polling (usado quando os detalhes ainda não estão disponíveis, ou quando
    // a loja/merchant ainda não foi mapeada).
    private async Task<bool> ProcessEventAsync(
        IFoodPollingEvent evt, long companyId, string token, IReadOnlyDictionary<long, IFoodMerchantMapping> mappingsByBranch,
        DateTime now, CancellationToken cancellationToken)
    {
        switch (evt.Code)
        {
            case "CONFIRMED":
                return await ProcessNewOrderAsync(evt, companyId, token, mappingsByBranch, now, cancellationToken);

            case "CANCELLED":
                return await ProcessCancelledAsync(evt, now, cancellationToken);

            default:
                // Fora do escopo desta fase — reconhece a leitura pra não acumular no polling.
                return true;
        }
    }

    private async Task<bool> ProcessNewOrderAsync(
        IFoodPollingEvent evt, long companyId, string token, IReadOnlyDictionary<long, IFoodMerchantMapping> mappingsByBranch,
        DateTime now, CancellationToken cancellationToken)
    {
        var existing = await _ifoodOrderRepository.GetByIFoodOrderIdAsync(evt.OrderId, cancellationToken);
        if (existing is not null)
            return true; // já processado antes (idempotência) — só confirma a leitura do evento

        var details = await _orderClient.GetOrderDetailsAsync(token, evt.OrderId, cancellationToken);
        if (details is null)
            return false; // 404 — detalhes ainda não disponíveis, tenta de novo no próximo ciclo (30s)

        var branchEntry = mappingsByBranch.FirstOrDefault(m => m.Value.MerchantId == details.MerchantId);
        if (branchEntry.Value is null)
            return false; // loja não mapeada ainda em "Lojas (merchants)" — espera o usuário mapear

        var branchId = branchEntry.Key;

        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch?.SelfServiceEmployeeId is null)
            return false; // filial precisa de um "funcionário de autoatendimento" configurado (Config > Filiais) antes de aceitar pedidos iFood

        var employeeId = branch.SelfServiceEmployeeId.Value;

        var orderTypeId = details.OrderType switch
        {
            "DELIVERY" => OrderTypeIds.Delivery,
            _ => OrderTypeIds.Retirada, // TAKEOUT e DINE_IN (autoatendimento iFood) tratados como retirada nesta fase
        };

        var customerName = string.IsNullOrWhiteSpace(details.CustomerName) ? "Cliente iFood" : details.CustomerName;

        var orderResult = CustomerOrder.Create(
            branchId, null, null, employeeId, null,
            $"Pedido iFood #{details.DisplayId ?? evt.OrderId}", now, null, orderTypeId,
            customerName, details.CustomerPhone,
            orderTypeId == OrderTypeIds.Delivery ? (details.DeliveryAddressFormatted ?? "Endereço não informado") : null);

        if (orderResult.IsFailure)
            return false;

        var customerOrder = orderResult.Value;
        var hasUnmappedItems = false;

        foreach (var item in details.Items)
        {
            Domain.Entities.Product? product = null;
            if (!string.IsNullOrWhiteSpace(item.Ean))
                product = await _productRepository.GetByBarcodeAsync(companyId, item.Ean, cancellationToken);

            if (product is null)
            {
                hasUnmappedItems = true;
                continue; // item não identificado no catálogo — sinalizado no pedido, não bloqueia a confirmação
            }

            customerOrder.AddItem(product.Id, item.UnitPrice, item.Quantity <= 0 ? 1 : item.Quantity, null, employeeId, now);
        }

        await _customerOrderRepository.AddAsync(customerOrder, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken); // precisa do Id gerado do CustomerOrder antes de criar o IFoodOrder

        var ifoodOrderResult = IFoodOrder.Create(
            customerOrder.Id, branchId, evt.OrderId, details.DisplayId, details.MerchantId,
            details.OrderType, now, hasUnmappedItems);

        if (ifoodOrderResult.IsFailure)
            return true; // pedido já foi salvo no SyncBar — melhor ter o pedido sem o link do que perdê-lo

        await _ifoodOrderRepository.AddAsync(ifoodOrderResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Confirma dentro do SLA de 8 minutos — fluxo essencial confirma automaticamente assim
        // que o pedido é criado no SyncBar (a equipe já vê ele na tela normal de pedidos).
        var confirmResult = await _orderClient.ConfirmOrderAsync(token, evt.OrderId, cancellationToken);
        if (confirmResult.Success)
        {
            var tracked = await _ifoodOrderRepository.GetByIdForUpdateAsync(ifoodOrderResult.Value.Id, cancellationToken);
            tracked?.MarkConfirmed(now);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        return true;
    }

    private async Task<bool> ProcessCancelledAsync(IFoodPollingEvent evt, DateTime now, CancellationToken cancellationToken)
    {
        var ifoodOrder = await _ifoodOrderRepository.GetByIFoodOrderIdForUpdateAsync(evt.OrderId, cancellationToken);
        if (ifoodOrder is null)
            return true; // nunca chegamos a criar esse pedido — nada a fazer

        ifoodOrder.SetStatus(IFoodOrderStatuses.Cancelled, now);

        var customerOrder = await _customerOrderRepository.GetByIdForUpdateAsync(ifoodOrder.CustomerOrderId, cancellationToken);
        if (customerOrder is not null && customerOrder.OrderStatusId != OrderStatusIds.Pago)
            customerOrder.Cancel(now);

        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }
}
