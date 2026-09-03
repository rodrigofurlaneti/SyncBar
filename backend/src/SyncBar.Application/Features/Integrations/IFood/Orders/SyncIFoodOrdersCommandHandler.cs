using Microsoft.Extensions.Caching.Memory;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Access;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

internal sealed class SyncIfoodOrdersCommandHandler : BaseCommandHandler<SyncIfoodOrdersCommand>
{
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
    private readonly ILogTrackerRepository _logRepository;
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
        IUnitOfWork unitOfWork
        )
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
        _logRepository = logRepository;
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
               var stopwatch = Stopwatch.StartNew();

               var setting = await _settingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

                if (setting is not null && !setting.Enabled)
               {
                   return Result.Failure(new Error(
                       "SyncIfoodOrders.MissingSettingsEnabled",
                       $"Polling ignorado: Integração do iFood está explicitamente desabilitada para a empresa {request.CompanyId}."
                   ));
               }

               if (setting is null || setting.ClientId is null)
               {
                   return Result.Failure(new Error(
                       "SyncIfoodOrders.MissingSettings",
                       $"A integração com o iFood foi acionada para a empresa {request.CompanyId}, mas as configurações (Settings/ClientId) não foram encontradas no banco de dados."
                   ));
               }

               var token = await _tokenProvider.GetAccessTokenAsync(request.CompanyId, cancellationToken);
               if (string.IsNullOrEmpty(token))
               {
                   return Result.Failure(new Error(
                       "SyncIfoodOrders.TokenFailed",
                       "Falha ao obter o Access Token do iFood. Verifique o LogTracker para detalhes sobre erro de criptografia (Data Protection) ou credenciais inválidas."
                   ));
               }

               var mappings = await _merchantMappingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
               var merchantIds = mappings.Values
                   .Where(m => m.IsActive && !string.IsNullOrWhiteSpace(m.MerchantUuid))
                   .Select(m => m.MerchantUuid!)
                   .Distinct()
                   .ToList();

               if (merchantIds.Count == 0)
               {
                   return Result.Failure(new Error(
                       "SyncIfoodOrders.NoMerchantsMapped",
                       $"A integração com o iFood está habilitada para a empresa {request.CompanyId}, mas não há nenhuma loja (Merchant UUID) ativa e mapeada."
                   ));
               }

               var events = await _orderClient.PollEventsAsync(token, merchantIds, cancellationToken);
               if (events.Count == 0)
                   return Result.Success();

               var now = _timeProviderCustom.GetLocalNow().DateTime;
               var acknowledgeIds = await ProcessEventsAsync(events, request.CompanyId, token, stopwatch,
                   mappings, now, cancellationToken);

               if (acknowledgeIds.Count > 0)
                   await _orderClient.AcknowledgeEventsAsync(token, acknowledgeIds, cancellationToken);

               return Result.Success();
           });
    }

    private sealed record PollingContext(string Token, IReadOnlyDictionary<long, IfoodMerchantMapping> Mappings, List<string> MerchantIds);
    private async Task<PollingContext?> TryBuildPollingContextAsync(long companyId, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetByCompanyAsync(companyId, cancellationToken);
        if (setting is null || !setting.Enabled || setting.ClientId is null)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Application/Features/Integrations/Ifood/Orders",
                ClassName = "SyncIfoodOrdersCommandHandler",
                MethodName = nameof(TryBuildPollingContextAsync),
                IsSuccess = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = "Setting is null or not enabled or ClientId is null.",
                StackTrace = string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _logRepository.AddAsync(log);
            await _unitOfWork.CommitAsync(cancellationToken);
            return null;
        }

        var token = await _tokenProvider.GetAccessTokenAsync(companyId, stopwatch, cancellationToken);
        if (token is null)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Application/Features/Integrations/Ifood/Orders",
                ClassName = "SyncIfoodOrdersCommandHandler",
                MethodName = nameof(TryBuildPollingContextAsync),
                IsSuccess = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = "Token is null.",
                StackTrace = string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await _logRepository.AddAsync(log);
            await _unitOfWork.CommitAsync(cancellationToken);
            return null;
        }

        var mappings = await _merchantMappingRepository.GetByCompanyAsync(companyId, cancellationToken);
        var merchantIds = mappings.Values
            .Where(m => m.IsActive && !string.IsNullOrWhiteSpace(m.MerchantUuid))
            .Select(m => m.MerchantUuid!)
            .Distinct()
            .ToList();

        if (merchantIds.Count == 0)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Application/Features/Integrations/Ifood/Orders",
                ClassName = "SyncIfoodOrdersCommandHandler",
                MethodName = nameof(TryBuildPollingContextAsync),
                IsSuccess = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = "Nenhuma loja com Merchant UUID mapeado ainda — nada pra fazer polling.",
                StackTrace = string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await _logRepository.AddAsync(log);
            await _unitOfWork.CommitAsync(cancellationToken);
            return null;
        }

        return new PollingContext(token, mappings, merchantIds);
    }

    private async Task<List<string>> ProcessEventsAsync(
        IReadOnlyCollection<IfoodPollingEvent> events, 
        long companyId, 
        string token,
        Stopwatch stopwatch,
        IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch, 
        DateTime now, 
        CancellationToken cancellationToken)
    {
        var acknowledgeIds = new List<string>();

        foreach (var evt in events.OrderBy(e => e.CreatedAt))
        {
            var shouldAcknowledge = await ProcessSingleEventAsync(evt, companyId, token, stopwatch, mappingsByBranch, now, cancellationToken);
            if (shouldAcknowledge)
                acknowledgeIds.Add(evt.Id);
        }

        return acknowledgeIds;
    }

    private async Task<bool> ProcessSingleEventAsync(
        IfoodPollingEvent evt, 
        long companyId, 
        string token,
        Stopwatch stopwatch,
        IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch,
        DateTime now, 
        CancellationToken cancellationToken)
    {
        if (IsDuplicateEvent(evt.Id))
            return true;

        try
        {
            var shouldAcknowledge = await ProcessEventAsync(evt, companyId, token, stopwatch, mappingsByBranch, now, cancellationToken);
            if (shouldAcknowledge)
                MarkEventProcessed(evt.Id);

            return shouldAcknowledge;
        }
        catch
        {
            return false;
        }
    }

    private static string EventCacheKey(string eventId) => $"Ifood:event:{eventId}";

    private bool IsDuplicateEvent(string eventId) => _cache.TryGetValue(EventCacheKey(eventId), out _);

    private void MarkEventProcessed(string eventId) => _cache.Set(EventCacheKey(eventId), true, EventDedupTtl);

    private async Task<bool> ProcessEventAsync(
        IfoodPollingEvent evt, 
        long companyId, 
        string token,
        Stopwatch stopwatch,
        IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch,
        DateTime now, 
        CancellationToken cancellationToken)
    {
        switch (evt.FullCode)
        {
            case "PLACED":
                return await ProcessNewOrderAsync(evt, companyId, token, mappingsByBranch, now, cancellationToken);

            case "CONFIRMED":
                return await ProcessStatusSyncAsync(evt.OrderId, ifoodOrder => ifoodOrder.MarkConfirmed(now), cancellationToken);

            case "PREPARATION_STARTED":
                return await ProcessStatusSyncAsync(evt.OrderId, ifoodOrder => ifoodOrder.SetStatus(IfoodOrderStatuses.PreparationStarted, now), cancellationToken);

            case "READY_TO_PICKUP":
                return await ProcessStatusSyncAsync(evt.OrderId, ifoodOrder => ifoodOrder.SetStatus(IfoodOrderStatuses.ReadyToPickup, now), cancellationToken);

            case "DISPATCHED":
                return await ProcessStatusSyncAsync(evt.OrderId, ifoodOrder => ifoodOrder.SetStatus(IfoodOrderStatuses.Dispatched, now), cancellationToken);

            case "DELIVERED": // categoria FOOD_SELF_SERVICE
                return await ProcessStatusSyncAsync(evt.OrderId, ifoodOrder => ifoodOrder.SetStatus(IfoodOrderStatuses.Delivered, now), cancellationToken);

            case "CONCLUDED":
                return await ProcessStatusSyncAsync(evt.OrderId, ifoodOrder => ifoodOrder.SetStatus(IfoodOrderStatuses.Concluded, now), cancellationToken);

            case "CANCELLED":
                return await ProcessCancelledAsync(evt, companyId, token, stopwatch, now, cancellationToken);

            case "ORDER_PATCHED":
            case "ASSIGN_DRIVER":
            case "CANCELLATION_REQUEST_FAILED":
            case "HANDSHAKE_DISPUTE":
            case "HANDSHAKE_SETTLEMENT":
            case "DELIVERY_ADDRESS_CHANGE":
            case "DELIVERY_PHONE_CHANGE":
            default:
                return true;
        }
    }

    private async Task<bool> ProcessStatusSyncAsync(string ifoodOrderId, Action<IfoodOrder> apply, CancellationToken cancellationToken)
    {
        var tracked = await _IfoodOrderRepository.GetByIfoodOrderIdForUpdateAsync(ifoodOrderId, cancellationToken);
        if (tracked is null)
            return true;

        apply(tracked);
        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> ProcessNewOrderAsync(
    IfoodPollingEvent evt, long companyId, string token, IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch,
    DateTime now, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var isSuccess = true;
        string? errorMessage = null;
        string? stackTrace = null;

        try
        {
            var existing = await _IfoodOrderRepository.GetByIfoodOrderIdAsync(evt.OrderId, cancellationToken);
            if (existing is not null)
                return true; // já processado antes (idempotência)

            var details = await _orderClient.GetOrderDetailsAsync(token, evt.OrderId, cancellationToken);
            if (details is null)
            {
                // 404 — detalhes ainda não disponíveis (comportamento normal de polling, mas podemos registrar ou apenas retornar false)
                return false;
            }

            var assignment = await ResolveBranchAssignmentAsync(details, mappingsByBranch, stopwatch, cancellationToken);
            if (assignment.IsFailure)
            {
                isSuccess = false;
                errorMessage = $"Loja do iFood (MerchantId: {details.MerchantId}) não mapeada ou filial sem funcionário de autoatendimento configurado.";
                return false;
            }

            var orderResult = CreateCustomerOrderFromIfoodDetails(details, evt.OrderId, assignment.Value, now);
            if (orderResult.IsFailure)
            {
                isSuccess = false;
                errorMessage = orderResult.Error.Message;
                return false;
            }

            var customerOrder = orderResult.Value;
            var hasUnmappedItems = await AddItemsToOrderAsync(
                customerOrder, details.Items, companyId, assignment.Value.BranchId, assignment.Value.EmployeeId, now, cancellationToken);

            await _customerOrderRepository.AddAsync(customerOrder, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken); // precisa do Id gerado

            var IfoodOrderResult = IfoodOrder.Create(
                customerOrder.Id, assignment.Value.BranchId, evt.OrderId, details.DisplayId, details.MerchantId,
                details.OrderType, details.DeliveredBy, details.OrderTiming, details.PreparationStartDateTime,
                now, hasUnmappedItems);

            if (IfoodOrderResult.IsFailure)
            {
                errorMessage = IfoodOrderResult.Error.Message;
                return true;
            }

            await _IfoodOrderRepository.AddAsync(IfoodOrderResult.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            await ConfirmIfoodOrderWithinSlaAsync(token, evt.OrderId, IfoodOrderResult.Value.Id, now, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMessage = ex.Message;
            stackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            try
            {
                var log = new LogTracker(0)
                {
                    AppUserId = null,
                    DirectoryName = "Application/Services/Features/Integrations/Ifood/Orders",
                    ClassName = "IfoodPollingService",
                    MethodName = nameof(ProcessNewOrderAsync),
                    IsSuccess = isSuccess,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = errorMessage,
                    StackTrace = stackTrace,
                    IpAddress = null,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                await _logRepository.AddAsync(log);
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                // Evita que falhas ao salvar o log quebrem a execução do worker
            }
        }
    }

    private readonly record struct BranchAssignment(long BranchId, long EmployeeId);

    private async Task<Result<BranchAssignment>> ResolveBranchAssignmentAsync(
        IfoodOrderDetailsDto details,
        IReadOnlyDictionary<long, IfoodMerchantMapping> mappingsByBranch,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var branchEntry = mappingsByBranch.FirstOrDefault(m => m.Value.MerchantUuid == details.MerchantId);
        if (branchEntry.Value is null)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Application/Features/Integrations/Ifood/Orders",
                ClassName = "IfoodPollingService",
                MethodName = nameof(ResolveBranchAssignmentAsync),
                IsSuccess = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = "Ifood.BranchUnmapped - A loja do iFood não está mapeada no sistema.",
                StackTrace = string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await _logRepository.AddAsync(log);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Failure<BranchAssignment>(new Error(
                "Ifood.BranchUnmapped",
                $"A loja do iFood (MerchantId: {details.MerchantId}) não está mapeada no sistema. Configure o mapeamento em Config > Integrações > iFood."
            ));
        }

        var branch = await _branchRepository.GetByIdAsync(branchEntry.Key, cancellationToken);
        if (branch is null)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Application/Features/Integrations/Ifood/Orders",
                ClassName = "IfoodPollingService",
                MethodName = nameof(ResolveBranchAssignmentAsync),
                IsSuccess = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = $"A filial com ID '{branchEntry.Key}' não foi encontrada no banco de dados.",
                StackTrace = string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await _logRepository.AddAsync(log);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Failure<BranchAssignment>(new Error(
                "Ifood.BranchNotFound",
                $"A filial com ID '{branchEntry.Key}' não foi encontrada no banco de dados."
            ));
        }

        if (branch.SelfServiceEmployeeId is null)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Application/Features/Integrations/Ifood/Orders",
                ClassName = "IfoodPollingService",
                MethodName = nameof(ResolveBranchAssignmentAsync),
                IsSuccess = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = $"A filial '{branch.Name ?? branchEntry.Key.ToString()}' precisa ter um 'funcionário de autoatendimento' configurado (Config > Filiais) antes de aceitar pedidos do iFood.",
                StackTrace = string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await _logRepository.AddAsync(log);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Failure<BranchAssignment>(new Error(
                "Ifood.BranchMissingSelfServiceEmployee",
                $"A filial '{branch.Name ?? branchEntry.Key.ToString()}' precisa ter um 'funcionário de autoatendimento' configurado (Config > Filiais) antes de aceitar pedidos do iFood."
            ));
        }
        return Result.Success(new BranchAssignment(branchEntry.Key, branch.SelfServiceEmployeeId.Value));
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
                continue; 

            if (!complementsById.TryGetValue(complementMapping.ComplementId, out var resolved))
                continue; 

            customerOrder.AddComplement(orderItemId, resolved.Complement.Id, resolved.Complement.ExtraPrice, now);
        }
    }

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

    private async Task<bool> ProcessCancelledAsync(IfoodPollingEvent evt, long companyId, string token, Stopwatch stopwatch,
        DateTime now, CancellationToken cancellationToken)
    {
        var ifoodOrder = await _IfoodOrderRepository.GetByIfoodOrderIdForUpdateAsync(evt.OrderId, cancellationToken);
        if (ifoodOrder is null)
        {
            // Pedido cancelado antes de ter sido sincronizado localmente (ex.: cliente cancelou
            // entre o PLACED e a confirmação) — cria já nascendo cancelado, pra não perder o
            // registro em CustomerOrder/IfoodOrder.
            var details = await _orderClient.GetOrderDetailsAsync(token, evt.OrderId, cancellationToken);
            if (details is null)
                return true;
            var mappings = await _merchantMappingRepository.GetByCompanyAsync(companyId, cancellationToken);
            var assignment = await ResolveBranchAssignmentAsync(details, mappings, stopwatch, cancellationToken);
            if (assignment.IsFailure)
                return true;
            var orderResult = CreateCustomerOrderFromIfoodDetails(details, evt.OrderId, assignment.Value, now);
            if (orderResult.IsFailure)
                return true;
            var customerOrder = orderResult.Value;
            customerOrder.Cancel(now);
            await _customerOrderRepository.AddAsync(customerOrder, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken); // precisa do Id gerado

            var ifoodOrderResult = IfoodOrder.Create(
                customerOrder.Id,
                assignment.Value.BranchId,
                evt.OrderId,
                details.DisplayId,
                details.MerchantId,
                details.OrderType,
                details.DeliveredBy,
                details.OrderTiming,
                details.PreparationStartDateTime,
                now, hasUnmappedItems: false);
            if (ifoodOrderResult.IsSuccess)
            {
                ifoodOrderResult.Value.SetStatus(IfoodOrderStatuses.Cancelled, now);
                await _IfoodOrderRepository.AddAsync(ifoodOrderResult.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }
            return true;
        }

        // Caminho mais comum: pedido já rastreado (sincronizado via PLACED) que foi cancelado —
        // marca o IfoodOrder e cancela o CustomerOrder vinculado (se ainda não tiver sido pago).
        ifoodOrder.SetStatus(IfoodOrderStatuses.Cancelled, now);
        var trackedCustomerOrder = await _customerOrderRepository.GetByIdForUpdateAsync(ifoodOrder.CustomerOrderId, cancellationToken);
        if (trackedCustomerOrder is not null && trackedCustomerOrder.OrderStatusId != OrderStatusIds.Pago)
            trackedCustomerOrder.Cancel(now);
        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }
}
