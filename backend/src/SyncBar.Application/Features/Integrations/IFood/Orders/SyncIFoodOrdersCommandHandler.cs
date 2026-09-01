using Microsoft.Extensions.Caching.Memory;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

/// <summary>
/// Núcleo do "fluxo essencial" de sincronização de pedidos: busca eventos novos (polling),
/// cria o pedido no SyncBar quando chega um pedido novo, confirma automaticamente dentro do SLA
/// de 8 minutos, e reflete cancelamentos vindos do Ifood. Avançar status manualmente (iniciar
/// preparo/pronto/despachar) e cancelar pelo SyncBar ficam nos commands
/// Start/MarkReady/CancelIfoodOrderCommand, disparados pela tela "Pedidos Ifood".
///
/// Eventos fora desse escopo (ASSIGN_DRIVER, ORDER_PATCHED, HANDSHAKE_*, rastreamento de
/// entrega, etc.) são reconhecidos (acknowledgment) mas não processados — ver
/// Ifood-integration-status no projeto claude.ai para o que ainda falta.
///
/// Fase 2.1 (reforço do polling de eventos): a doc completa do módulo Events avisa que a API
/// pode entregar eventos fora de ordem e reentregues — passa a ordenar por CreatedAt antes de
/// processar, e a deduplicar por Id do evento (IMemoryCache, mesmo padrão do
/// IfoodTokenProvider — dedup não precisa de tabela própria, só de uma janela de tempo maior
/// que a retenção de 8h da API). ACK é sempre enviado pra todo evento recebido, mesmo
/// duplicado ou fora de escopo — evita acumular "strikes" no throttling por falta de ACK.
///
/// Fase 6a (extensão): itens de pedido com options (complementos escolhidos no Ifood) passam a
/// virar OrderItemComplement no SyncBar — casa option.id contra IfoodComplementMapping
/// (IfoodOptionId) por filial, resolve o Complement correspondente (preço/grupo) entre os
/// ComplementGroup ativos da empresa, e aplica via CustomerOrder.AddComplement. Complemento não
/// mapeado (ou não reconhecido) simplesmente não é adicionado — mesmo tratamento tolerante já
/// usado pra item de produto não identificado por EAN (não bloqueia a confirmação do pedido
/// dentro do SLA de 8 minutos).
///
/// Fase 7 (extensão): grava details.DeliveredBy em IfoodOrder.DeliveredBy — usado pela tela
/// "Pedidos Ifood" pra decidir se oferece o botão "Atribuir entregador" (só quando o pedido é
/// DELIVERY e a entrega não é feita pela logística do próprio Ifood, ver comentário em
/// IfoodOrder.DeliveredBy).
/// </summary>
internal sealed class SyncIfoodOrdersCommandHandler : BaseCommandHandler<SyncIfoodOrdersCommand>
{
    // Retenção da API é 8h — usa uma margem generosa pra não reprocessar reentregas tardias.
    private static readonly TimeSpan EventDedupTtl = TimeSpan.FromHours(24);

    private readonly IIfoodIntegrationSettingRepository _settingRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodOrderClient _orderClient;
    private readonly IIfoodMerchantMappingRepository _merchantMappingRepository;
    private readonly IIfoodOrderRepository _IfoodOrderRepository;
    private readonly ICustomerOrderRepository _customerOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IIfoodComplementMappingRepository _complementMappingRepository;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public SyncIfoodOrdersCommandHandler(
        IIfoodIntegrationSettingRepository settingRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodOrderClient orderClient,
        IIfoodMerchantMappingRepository merchantMappingRepository,
        IIfoodOrderRepository IfoodOrderRepository,
        ICustomerOrderRepository customerOrderRepository,
        IProductRepository productRepository,
        IBranchRepository branchRepository,
        IComplementGroupRepository complementGroupRepository,
        IIfoodComplementMappingRepository complementMappingRepository,
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
        _IfoodOrderRepository = IfoodOrderRepository;
        _customerOrderRepository = customerOrderRepository;
        _productRepository = productRepository;
        _branchRepository = branchRepository;
        _complementGroupRepository = complementGroupRepository;
        _complementMappingRepository = complementMappingRepository;
        _timeProviderCustom = timeProviderCustom;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SyncIfoodOrdersCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SyncIfoodOrdersCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var context = await TryBuildPollingContextAsync(request.CompanyId, cancellationToken);
                if (context is null)
                    return Result.Success();

                var events = await _orderClient.PollEventsAsync(context.Token, context.MerchantIds, cancellationToken);
                if (events.Count == 0)
                    return Result.Success();

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var acknowledgeIds = await ProcessEventsAsync(events, request.CompanyId, context.Token, context.Mappings, now, cancellationToken);

                if (acknowledgeIds.Count > 0)
                    await _orderClient.AcknowledgeEventsAsync(context.Token, acknowledgeIds, cancellationToken);

                return Result.Success();
            });
    }

    // Contexto necessário pro polling: token válido + merchants ativos mapeados. null em
    // qualquer um dos pré-requisitos ausentes (integração desabilitada, sem token, sem loja
    // mapeada) — Handle trata isso como "nada a fazer neste ciclo".
    private sealed record PollingContext(string Token, IReadOnlyDictionary<long, IfoodMerchantMapping> Mappings, List<string> MerchantIds);

    private async Task<PollingContext?> TryBuildPollingContextAsync(long companyId, CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetByCompanyAsync(companyId, cancellationToken);
        if (setting is null || !setting.Enabled || setting.ClientId is null)
            return null;

        var token = await _tokenProvider.GetAccessTokenAsync(companyId, cancellationToken);
        if (token is null)
            return null; // sem token válido — tenta de novo no próximo ciclo

        // Mappings primeiro: precisa dos Merchant UUIDs ativos da empresa pra montar o
        // header x-polling-merchants exigido pelo módulo Events (fase 2.1).
        //
        // Fase 20 (2026-08-24): corrigido de MerchantId (numérico, ex. "4049623" — o "ID da
        // Loja" usado no módulo Merchant/status) para MerchantUuid. O módulo Events identifica
        // merchants por UUID — confirmado pelo schema de erro 403 da própria doc oficial
        // (unauthorizedMerchants retorna uma lista de UUIDs) e pelo mesmo requisito já
        // documentado no módulo Catalog v2 ("Merchant UUID", obrigatório). Enviar o MerchantId
        // numérico nesse header (bug anterior) explica o 400 "Bad Request. One or more request
        // parameters were not valid." visto em produção — não é um problema de permissão/
        // credencial como o 403 do módulo Merchant (Fase 19), é a própria loja não sendo
        // reconhecida no formato esperado. Ver claude/Ifood-integration-status.md, Fase 20.
        var mappings = await _merchantMappingRepository.GetByCompanyAsync(companyId, cancellationToken);
        var merchantIds = mappings.Values
            .Where(m => m.IsActive && !string.IsNullOrWhiteSpace(m.MerchantUuid))
            .Select(m => m.MerchantUuid!)
            .Distinct()
            .ToList();

        if (merchantIds.Count == 0)
            return null; // nenhuma loja com Merchant UUID mapeado ainda — nada pra fazer polling

        return new PollingContext(token, mappings, merchantIds);
    }

    // Ordena por CreatedAt (a API pode entregar fora de ordem) antes de processar, e devolve os
    // Ids que devem ser confirmados (acknowledgment) ao final do ciclo.
    private async Task<List<string>> ProcessEventsAsync(
        IReadOnlyCollection<IfoodPollingEvent> events, long companyId, string token,
        IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch, DateTime now, CancellationToken cancellationToken)
    {
        var acknowledgeIds = new List<string>();

        foreach (var evt in events.OrderBy(e => e.CreatedAt))
        {
            var shouldAcknowledge = await ProcessSingleEventAsync(evt, companyId, token, mappingsByBranch, now, cancellationToken);
            if (shouldAcknowledge)
                acknowledgeIds.Add(evt.Id);
        }

        return acknowledgeIds;
    }

    // Retorna true se o evento deve ser confirmado (acknowledgment). Reentregas de eventos já
    // processados num ciclo anterior confirmam de novo sem repetir a ação; falhas inesperadas
    // num evento não derrubam o ciclo inteiro — os outros eventos do lote continuam sendo
    // processados normalmente.
    private async Task<bool> ProcessSingleEventAsync(
        IfoodPollingEvent evt, long companyId, string token, IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch,
        DateTime now, CancellationToken cancellationToken)
    {
        if (IsDuplicateEvent(evt.Id))
            return true;

        try
        {
            var shouldAcknowledge = await ProcessEventAsync(evt, companyId, token, mappingsByBranch, now, cancellationToken);
            if (shouldAcknowledge)
                MarkEventProcessed(evt.Id);

            return shouldAcknowledge;
        }
        catch
        {
            return false;
        }
    }

    // Dedup por Id do evento — mesmo padrão de cache do IfoodTokenProvider (chave
    // "Ifood:{purpose}:{id}"), sem precisar de tabela/coluna nova só pra isso.
    private static string EventCacheKey(string eventId) => $"Ifood:event:{eventId}";

    private bool IsDuplicateEvent(string eventId) => _cache.TryGetValue(EventCacheKey(eventId), out _);

    private void MarkEventProcessed(string eventId) => _cache.Set(EventCacheKey(eventId), true, EventDedupTtl);

    // Retorna true se o evento deve ser confirmado (acknowledgment) — false faz ele voltar no
    // próximo ciclo de polling (usado quando os detalhes ainda não estão disponíveis, ou quando
    // a loja/merchant ainda não foi mapeada).
    private async Task<bool> ProcessEventAsync(
        IfoodPollingEvent evt, long companyId, string token, IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch,
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
        IfoodPollingEvent evt, long companyId, string token, IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch,
        DateTime now, CancellationToken cancellationToken)
    {
        var existing = await _IfoodOrderRepository.GetByIfoodOrderIdAsync(evt.OrderId, cancellationToken);
        if (existing is not null)
            return true; // já processado antes (idempotência) — só confirma a leitura do evento

        var details = await _orderClient.GetOrderDetailsAsync(token, evt.OrderId, cancellationToken);
        if (details is null)
            return false; // 404 — detalhes ainda não disponíveis, tenta de novo no próximo ciclo (30s)

        var assignment = await ResolveBranchAssignmentAsync(details, mappingsByBranch, cancellationToken);
        if (assignment is null)
            return false; // loja não mapeada, ou filial sem "funcionário de autoatendimento" configurado

        var orderResult = CreateCustomerOrderFromIfoodDetails(details, evt.OrderId, assignment.Value, now);
        if (orderResult.IsFailure)
            return false;

        var customerOrder = orderResult.Value;
        var hasUnmappedItems = await AddItemsToOrderAsync(
            customerOrder, details.Items, companyId, assignment.Value.BranchId, assignment.Value.EmployeeId, now, cancellationToken);

        await _customerOrderRepository.AddAsync(customerOrder, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken); // precisa do Id gerado do CustomerOrder antes de criar o IfoodOrder

        var IfoodOrderResult = IfoodOrder.Create(
            customerOrder.Id, assignment.Value.BranchId, evt.OrderId, details.DisplayId, details.MerchantId,
            details.OrderType, details.DeliveredBy, details.OrderTiming, details.PreparationStartDateTime,
            now, hasUnmappedItems);

        if (IfoodOrderResult.IsFailure)
            return true; // pedido já foi salvo no SyncBar — melhor ter o pedido sem o link do que perdê-lo

        await _IfoodOrderRepository.AddAsync(IfoodOrderResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        await ConfirmIfoodOrderWithinSlaAsync(token, evt.OrderId, IfoodOrderResult.Value.Id, now, cancellationToken);

        return true;
    }

    // BranchId da filial mapeada pro merchant do pedido + EmployeeId do "funcionário de
    // autoatendimento" configurado nela (Config > Filiais) — ambos exigidos antes de aceitar um
    // pedido Ifood.
    private readonly record struct BranchAssignment(long BranchId, long EmployeeId);

    private async Task<BranchAssignment?> ResolveBranchAssignmentAsync(
        IfoodOrderDetailsDto details, IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch, CancellationToken cancellationToken)
    {
        var branchEntry = mappingsByBranch.FirstOrDefault(m => m.Value.MerchantId == details.MerchantId);
        if (branchEntry.Value is null)
            return null; // loja não mapeada ainda em "Lojas (merchants)" — espera o usuário mapear

        var branch = await _branchRepository.GetByIdAsync(branchEntry.Key, cancellationToken);
        if (branch?.SelfServiceEmployeeId is null)
            return null; // filial precisa de um "funcionário de autoatendimento" configurado (Config > Filiais) antes de aceitar pedidos Ifood

        return new BranchAssignment(branchEntry.Key, branch.SelfServiceEmployeeId.Value);
    }

    private static Result<CustomerOrder> CreateCustomerOrderFromIfoodDetails(
        IfoodOrderDetailsDto details, string IfoodOrderId, BranchAssignment assignment, DateTime now)
    {
        var orderTypeId = details.OrderType switch
        {
            "DELIVERY" => OrderTypeIds.Delivery,
            _ => OrderTypeIds.Retirada, // TAKEOUT e DINE_IN (autoatendimento Ifood) tratados como retirada nesta fase
        };

        var customerName = string.IsNullOrWhiteSpace(details.CustomerName) ? "Cliente Ifood" : details.CustomerName;

        return CustomerOrder.Create(
            assignment.BranchId, null, null, assignment.EmployeeId, null,
            $"Pedido Ifood #{details.DisplayId ?? IfoodOrderId}", now, null, orderTypeId,
            customerName, details.CustomerPhone,
            orderTypeId == OrderTypeIds.Delivery ? (details.DeliveryAddressFormatted ?? "Endereço não informado") : null);
    }

    // Fase 6a (extensão): só busca os complementos ativos da empresa se algum item do pedido
    // realmente trouxer options — evita a query extra no caminho comum (pedido sem
    // complementos). Complement não é consultável isoladamente por Id (é filho de
    // ComplementGroup no domínio), então resolve por empresa inteira e indexa por Id uma vez.
    private async Task<bool> AddItemsToOrderAsync(
        CustomerOrder customerOrder, IReadOnlyCollection<IfoodOrderItemDto> items, long companyId, long branchId, long employeeId,
        DateTime now, CancellationToken cancellationToken)
    {
        var hasUnmappedItems = false;
        Dictionary<long, (Complement Complement, long ComplementGroupId)>? complementsById = null;

        foreach (var item in items)
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

            if (item.Options.Count == 0)
                continue;

            var orderItemId = customerOrder.Items.ElementAt(itemCountBefore).Id;
            complementsById ??= await BuildComplementsByIdAsync(companyId, cancellationToken);

            await AddComplementsToOrderItemAsync(customerOrder, orderItemId, item.Options, branchId, complementsById, now, cancellationToken);
        }

        return hasUnmappedItems;
    }

    private async Task AddComplementsToOrderItemAsync(
        CustomerOrder customerOrder, long orderItemId, IReadOnlyCollection<IfoodOrderItemOptionDto> options, long branchId,
        Dictionary<long, (Complement Complement, long ComplementGroupId)> complementsById, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var option in options)
        {
            if (!Guid.TryParse(option.Id, out var IfoodOptionId))
                continue;

            var complementMapping = await _complementMappingRepository.GetByIfoodOptionIdAndBranchAsync(IfoodOptionId, branchId, cancellationToken);
            if (complementMapping is null)
                continue; // opção não mapeada (ou mapeada em outra filial) — não bloqueia o pedido

            if (!complementsById.TryGetValue(complementMapping.ComplementId, out var resolved))
                continue; // Complement foi removido/desativado depois do mapeamento ser criado

            customerOrder.AddComplement(orderItemId, resolved.Complement.Id, resolved.Complement.ExtraPrice, now);
        }
    }

    // Confirma dentro do SLA de 8 minutos — fluxo essencial confirma automaticamente assim que o
    // pedido é criado no SyncBar (a equipe já vê ele na tela normal de pedidos).
    private async Task ConfirmIfoodOrderWithinSlaAsync(
        string token, string IfoodOrderId, long trackedIfoodOrderId, DateTime now, CancellationToken cancellationToken)
    {
        var confirmResult = await _orderClient.ConfirmOrderAsync(token, IfoodOrderId, cancellationToken);
        if (!confirmResult.Success)
            return;

        var tracked = await _IfoodOrderRepository.GetByIdForUpdateAsync(trackedIfoodOrderId, cancellationToken);
        tracked?.MarkConfirmed(now);
        await _unitOfWork.CommitAsync(cancellationToken);
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

    private async Task<bool> ProcessCancelledAsync(IfoodPollingEvent evt, DateTime now, CancellationToken cancellationToken)
    {
        var IfoodOrder = await _IfoodOrderRepository.GetByIfoodOrderIdForUpdateAsync(evt.OrderId, cancellationToken);
        if (IfoodOrder is null)
            return true; // nunca chegamos a criar esse pedido — nada a fazer

        IfoodOrder.SetStatus(IfoodOrderStatuses.Cancelled, now);

        var customerOrder = await _customerOrderRepository.GetByIdForUpdateAsync(IfoodOrder.CustomerOrderId, cancellationToken);
        if (customerOrder is not null && customerOrder.OrderStatusId != OrderStatusIds.Pago)
            customerOrder.Cancel(now);

        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }
}
