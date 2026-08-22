using Microsoft.Extensions.Caching.Memory;
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
///
/// Fase 2.1 (reforço do polling de eventos): a doc completa do módulo Events avisa que a API
/// pode entregar eventos fora de ordem e reentregues — passa a ordenar por CreatedAt antes de
/// processar, e a deduplicar por Id do evento (IMemoryCache, mesmo padrão do
/// IFoodTokenProvider — dedup não precisa de tabela própria, só de uma janela de tempo maior
/// que a retenção de 8h da API). ACK é sempre enviado pra todo evento recebido, mesmo
/// duplicado ou fora de escopo — evita acumular "strikes" no throttling por falta de ACK.
///
/// Fase 6a (extensão): itens de pedido com options (complementos escolhidos no iFood) passam a
/// virar OrderItemComplement no SyncBar — casa option.id contra IFoodComplementMapping
/// (IFoodOptionId) por filial, resolve o Complement correspondente (preço/grupo) entre os
/// ComplementGroup ativos da empresa, e aplica via CustomerOrder.AddComplement. Complemento não
/// mapeado (ou não reconhecido) simplesmente não é adicionado — mesmo tratamento tolerante já
/// usado pra item de produto não identificado por EAN (não bloqueia a confirmação do pedido
/// dentro do SLA de 8 minutos).
///
/// Fase 7 (extensão): grava details.DeliveredBy em IFoodOrder.DeliveredBy — usado pela tela
/// "Pedidos iFood" pra decidir se oferece o botão "Atribuir entregador" (só quando o pedido é
/// DELIVERY e a entrega não é feita pela logística do próprio iFood, ver comentário em
/// IFoodOrder.DeliveredBy).
/// </summary>
internal sealed class SyncIFoodOrdersCommandHandler : BaseCommandHandler<SyncIFoodOrdersCommand>
{
    // Retenção da API é 8h — usa uma margem generosa pra não reprocessar reentregas tardias.
    private static readonly TimeSpan EventDedupTtl = TimeSpan.FromHours(24);

    private readonly IIFoodIntegrationSettingRepository _settingRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodOrderClient _orderClient;
    private readonly IIFoodMerchantMappingRepository _merchantMappingRepository;
    private readonly IIFoodOrderRepository _ifoodOrderRepository;
    private readonly ICustomerOrderRepository _customerOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IIFoodComplementMappingRepository _complementMappingRepository;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IMemoryCache _cache;
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
        IComplementGroupRepository complementGroupRepository,
        IIFoodComplementMappingRepository complementMappingRepository,
        TimeProvider timeProviderCustom,
        IMemoryCache cache,
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
        _complementGroupRepository = complementGroupRepository;
        _complementMappingRepository = complementMappingRepository;
        _timeProviderCustom = timeProviderCustom;
        _cache = cache;
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

                // Mappings primeiro: precisa dos MerchantIds ativos da empresa pra montar o
                // header x-polling-merchants exigido pelo módulo Events (fase 2.1).
                var mappings = await _merchantMappingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var merchantIds = mappings.Values
                    .Where(m => m.IsActive && !string.IsNullOrWhiteSpace(m.MerchantId))
                    .Select(m => m.MerchantId!)
                    .Distinct()
                    .ToList();

                if (merchantIds.Count == 0)
                    return Result.Success(); // nenhuma loja mapeada ainda — nada pra fazer polling

                var events = await _orderClient.PollEventsAsync(token, merchantIds, cancellationToken);
                if (events.Count == 0)
                    return Result.Success();

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var acknowledgeIds = new List<string>();

                // Ordena por CreatedAt (a API pode entregar fora de ordem) antes de processar.
                foreach (var evt in events.OrderBy(e => e.CreatedAt))
                {
                    bool shouldAcknowledge;

                    if (IsDuplicateEvent(evt.Id))
                    {
                        // Reentrega de um evento já processado num ciclo anterior — confirma de
                        // novo (ACK sempre enviado) sem repetir a ação.
                        shouldAcknowledge = true;
                    }
                    else
                    {
                        try
                        {
                            shouldAcknowledge = await ProcessEventAsync(evt, request.CompanyId, token, mappings, now, cancellationToken);
                            if (shouldAcknowledge)
                                MarkEventProcessed(evt.Id);
                        }
                        catch
                        {
                            // Qualquer falha inesperada num evento não derruba o ciclo inteiro — os
                            // outros eventos deste lote continuam sendo processados normalmente.
                            shouldAcknowledge = false;
                        }
                    }

                    if (shouldAcknowledge)
                        acknowledgeIds.Add(evt.Id);
                }

                if (acknowledgeIds.Count > 0)
                    await _orderClient.AcknowledgeEventsAsync(token, acknowledgeIds, cancellationToken);

                return Result.Success();
            });
    }

    // Dedup por Id do evento — mesmo padrão de cache do IFoodTokenProvider (chave
    // "ifood:{purpose}:{id}"), sem precisar de tabela/coluna nova só pra isso.
    private static string EventCacheKey(string eventId) => $"ifood:event:{eventId}";

    private bool IsDuplicateEvent(string eventId) => _cache.TryGetValue(EventCacheKey(eventId), out _);

    private void MarkEventProcessed(string eventId) => _cache.Set(EventCacheKey(eventId), true, EventDedupTtl);

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

        // Fase 6a (extensão): só busca os complementos ativos da empresa se algum item do pedido
        // realmente trouxer options — evita a query extra no caminho comum (pedido sem
        // complementos). Complement não é consultável isoladamente por Id (é filho de
        // ComplementGroup no domínio), então resolve por empresa inteira e indexa por Id uma vez.
        Dictionary<long, (Complement Complement, long ComplementGroupId)>? complementsById = null;

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

            var itemCountBefore = customerOrder.Items.Count;
            customerOrder.AddItem(product.Id, item.UnitPrice, item.Quantity <= 0 ? 1 : item.Quantity, null, employeeId, now);
            if (customerOrder.Items.Count == itemCountBefore)
                continue; // AddItem falhou (quantidade inválida etc.) — item já teria virado hasUnmappedItems se fosse o caso

            var orderItemId = customerOrder.Items.ElementAt(itemCountBefore).Id;

            if (item.Options.Count == 0)
                continue;

            complementsById ??= await BuildComplementsByIdAsync(companyId, cancellationToken);

            foreach (var option in item.Options)
            {
                if (!Guid.TryParse(option.Id, out var ifoodOptionId))
                    continue;

                var complementMapping = await _complementMappingRepository.GetByIFoodOptionIdAndBranchAsync(ifoodOptionId, branchId, cancellationToken);
                if (complementMapping is null)
                    continue; // opção não mapeada (ou mapeada em outra filial) — não bloqueia o pedido

                if (!complementsById.TryGetValue(complementMapping.ComplementId, out var resolved))
                    continue; // Complement foi removido/desativado depois do mapeamento ser criado

                customerOrder.AddComplement(orderItemId, resolved.Complement.Id, resolved.Complement.ExtraPrice, now);
            }
        }

        await _customerOrderRepository.AddAsync(customerOrder, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken); // precisa do Id gerado do CustomerOrder antes de criar o IFoodOrder

        var ifoodOrderResult = IFoodOrder.Create(
            customerOrder.Id, branchId, evt.OrderId, details.DisplayId, details.MerchantId,
            details.OrderType, details.DeliveredBy, details.OrderTiming, details.PreparationStartDateTime,
            now, hasUnmappedItems);

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

    // Fase 6a (extensão): Complement é Entity filha de ComplementGroup (sem repositório próprio
    // por Id) — carrega todos os grupos ativos da empresa uma vez e indexa por ComplementId.
    private async Task<Dictionary<long, (Complement Complement, long ComplementGroupId)>> BuildComplementsByIdAsync(
        long companyId, CancellationToken cancellationToken)
    {
        var groups = await _complementGroupRepository.GetByCompanyAsync(companyId, cancellationToken);
        var result = new Dictionary<long, (Complement, long)>();
        foreach (var group in groups)
        {
            foreach (var complement in group.Complements.Where(c => c.IsActive))
                result[complement.Id] = (complement, group.Id);
        }

        return result;
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
