using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Analytics;
using SyncBar.Application.Features.Integrations.IFood.Catalog;
using SyncBar.Application.Features.Integrations.IFood.Financial;
using SyncBar.Application.Features.Integrations.IFood.Logistics;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Application.Features.Integrations.IFood.Review;
using SyncBar.Application.Features.Integrations.IFood.Shipping;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

// Credenciais de integrações externas (iFood e, futuramente, outras) — só quem administra a
// empresa mexe aqui, mesmo padrão de acesso do resto de Configurações (ManagerGate no frontend).
[Authorize(Roles = "Administrador,Gerente")]
public sealed class IntegrationsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    // Credenciais do app iFood (client_id/client_secret) — por EMPRESA, não por filial: o app
    // criado no portal do iFood é "centralizado" e um único client_id dá acesso a vários
    // merchants (ver comentário em IFoodIntegrationSetting.cs).
    [HttpGet("ifood/company/{companyId:long}")]
    public Task<IActionResult> GetIFoodSettings(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodSettings), async () =>
        {
            var result = await Mediator.Send(new GetIFoodSettingsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood")]
    public Task<IActionResult> SaveIFoodSettings([FromBody] SaveIFoodSettingsCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SaveIFoodSettings), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Testa as credenciais salvas contra o endpoint OAuth do iFood — endpoint/payload
    // confirmados contra a doc oficial (ver IFoodAuthClient).
    [HttpPost("ifood/test-connection")]
    public Task<IActionResult> TestIFoodConnection([FromBody] TestIFoodConnectionCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(TestIFoodConnection), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Mapeamento loja (filial) → MerchantId do iFood — por filial, diferente das credenciais.
    [HttpGet("ifood/merchants/company/{companyId:long}")]
    public Task<IActionResult> GetIFoodMerchantMappings(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodMerchantMappings), async () =>
        {
            var result = await Mediator.Send(new GetIFoodMerchantMappingsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood/merchants")]
    public Task<IActionResult> SetIFoodMerchantMapping([FromBody] SetIFoodMerchantMappingCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodMerchantMapping), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Pedidos iFood ("fluxo essencial", fase 2) — a sincronização em si roda sozinha em segundo
    // plano (IFoodOrderPollingBackgroundService); estes endpoints são só pra tela acompanhar e
    // avançar o status manualmente (confirmar já é automático).
    [HttpGet("ifood/orders/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodOrders(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOrders), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOrdersQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/start-preparation")]
    public Task<IActionResult> StartIFoodOrderPreparation(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(StartIFoodOrderPreparation), async () =>
        {
            var result = await Mediator.Send(new StartIFoodOrderPreparationCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/ready")]
    public Task<IActionResult> MarkIFoodOrderReady(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIFoodOrderReady), async () =>
        {
            var result = await Mediator.Send(new MarkIFoodOrderReadyCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/orders/{ifoodOrderId:long}/cancellation-reasons")]
    public Task<IActionResult> GetIFoodCancellationReasons(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodCancellationReasons), async () =>
        {
            var result = await Mediator.Send(new GetIFoodCancellationReasonsQuery(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/orders/{ifoodOrderId:long}/cancel")]
    public Task<IActionResult> CancelIFoodOrder(long ifoodOrderId, [FromBody] CancelIFoodOrderRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIFoodOrder), async () =>
        {
            var result = await Mediator.Send(new CancelIFoodOrderCommand(ifoodOrderId, request.ReasonCode), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Cardápio iFood ("fluxo essencial", fase 3) — assim como os pedidos, a sincronização roda
    // sozinha (disparada automaticamente a cada produto/categoria criado/editado/desativado, ver
    // IIFoodCatalogSyncTrigger); este endpoint é só o botão "Sincronizar agora" da tela, pra
    // reenviar tudo de uma vez (carga inicial ou recuperação de falha).
    [HttpPost("ifood/catalog/sync")]
    public Task<IActionResult> SyncIFoodCatalog([FromBody] SyncIFoodCatalogRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SyncIFoodCatalog), async () =>
        {
            var result = await Mediator.Send(new SyncIFoodCatalogCommand(request.CompanyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Financeiro iFood (fase 4) — a sincronização em si roda sozinha em segundo plano 1x/dia
    // (IFoodFinancialSyncBackgroundService); estes endpoints são pra tela "Financeiro" ler o
    // resumo do período e pro botão "Sincronizar agora" reenviar sob demanda.
    [HttpGet("ifood/financial/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodFinancialSummary(long branchId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodFinancialSummary), async () =>
        {
            var result = await Mediator.Send(new GetIFoodFinancialSummaryQuery(branchId, from, to), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/financial/sync")]
    public Task<IActionResult> SyncIFoodFinancial([FromBody] SyncIFoodFinancialRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SyncIFoodFinancial), async () =>
        {
            var result = await Mediator.Send(new SyncIFoodFinancialCommand(request.CompanyId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Fase 9 — cobertura dos 13 relatórios financeiros restantes (financial/v2.0 ×12 +
    // financial/v2.1 ×1), mais anticipations/sales (financial/v3.0) via o mesmo catálogo
    // genérico. reportType é o nome do enum IFoodFinancialReportType (ex.: "SalesAdjustments").
    [HttpGet("ifood/financial/branch/{branchId:long}/reports/{reportType}")]
    public Task<IActionResult> GetIFoodFinancialReport(
        long branchId, IFoodFinancialReportType reportType, [FromQuery] string? periodId,
        [FromQuery] DateTime? rangeStart, [FromQuery] DateTime? rangeEnd, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodFinancialReport), async () =>
        {
            var result = await Mediator.Send(new GetIFoodFinancialReportQuery(branchId, reportType, periodId, rangeStart, rangeEnd), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Fase 9 — apuração sob demanda (financial/v3.0/.../reconciliation/on-demand), pra quando a
    // apuração automática do período ainda não foi gerada. Competence no formato "yyyy-MM".
    [HttpPost("ifood/financial/branch/{branchId:long}/reconciliation-on-demand")]
    public Task<IActionResult> RequestIFoodReconciliationOnDemand(
        long branchId, [FromBody] RequestIFoodReconciliationOnDemandRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodReconciliationOnDemand), async () =>
        {
            var result = await Mediator.Send(new RequestIFoodReconciliationOnDemandCommand(branchId, request.Competence), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/financial/branch/{branchId:long}/reconciliation-on-demand/{requestId}")]
    public Task<IActionResult> GetIFoodReconciliationOnDemandStatus(long branchId, string requestId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodReconciliationOnDemandStatus), async () =>
        {
            var result = await Mediator.Send(new GetIFoodReconciliationOnDemandStatusQuery(branchId, requestId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Operação da loja iFood (fase 5, módulo Merchant) — tudo sob demanda (sem sincronização
    // automática em segundo plano): status é lido ao vivo, interrupções são criadas/removidas
    // direto na API do iFood, horários e tempo de preparo são editados numa cópia local e
    // reenviados ao salvar (ver comentário nos handlers em Features/Integrations/IFood/Merchant).
    [HttpGet("ifood/merchant/status/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodMerchantStatus(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodMerchantStatus), async () =>
        {
            var result = await Mediator.Send(new GetIFoodMerchantStatusQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/merchant/interruptions/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodInterruptions(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodInterruptions), async () =>
        {
            var result = await Mediator.Send(new GetIFoodInterruptionsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/merchant/interruptions")]
    public Task<IActionResult> CreateIFoodInterruption([FromBody] CreateIFoodInterruptionCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CreateIFoodInterruption), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("ifood/merchant/interruptions/{interruptionId}")]
    public Task<IActionResult> DeleteIFoodInterruption(string interruptionId, [FromQuery] long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DeleteIFoodInterruption), async () =>
        {
            var result = await Mediator.Send(new DeleteIFoodInterruptionCommand(branchId, interruptionId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/merchant/opening-hours/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodOpeningHours(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOpeningHours), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOpeningHoursQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("ifood/merchant/opening-hours")]
    public Task<IActionResult> SaveIFoodOpeningHours([FromBody] SaveIFoodOpeningHoursCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SaveIFoodOpeningHours), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("ifood/merchant/preparation-time")]
    public Task<IActionResult> SetIFoodPreparationTime([FromBody] SetIFoodPreparationTimeCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(SetIFoodPreparationTime), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Logística por frota própria (fase 7, módulo Logistics) — só se aplica a pedidos DELIVERY
    // com deliveredBy diferente de "IFOOD" (ver IFoodOrder.DeliveredBy); tudo sob demanda, cada
    // passo é acionado manualmente pela equipe conforme o entregador avança (atribuir → saiu pra
    // origem → chegou na origem → despachou → chegou no destino → verificar código de entrega).
    [HttpGet("ifood/logistics/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodLogisticsDeliveries(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodLogisticsDeliveries), async () =>
        {
            var result = await Mediator.Send(new GetIFoodLogisticsDeliveriesQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/assign-driver")]
    public Task<IActionResult> AssignIFoodDriver(long ifoodOrderId, [FromBody] AssignIFoodDriverRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(AssignIFoodDriver), async () =>
        {
            var result = await Mediator.Send(new AssignIFoodDriverCommand(ifoodOrderId, request.DriverName, request.DriverPhone, request.DriverVehicleType), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/going-to-origin")]
    public Task<IActionResult> MarkIFoodGoingToOrigin(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIFoodGoingToOrigin), async () =>
        {
            var result = await Mediator.Send(new MarkIFoodGoingToOriginCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/arrived-at-origin")]
    public Task<IActionResult> MarkIFoodArrivedAtOrigin(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIFoodArrivedAtOrigin), async () =>
        {
            var result = await Mediator.Send(new MarkIFoodArrivedAtOriginCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/dispatch")]
    public Task<IActionResult> DispatchIFoodLogistics(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(DispatchIFoodLogistics), async () =>
        {
            var result = await Mediator.Send(new DispatchIFoodLogisticsCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/arrived-at-destination")]
    public Task<IActionResult> MarkIFoodArrivedAtDestination(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(MarkIFoodArrivedAtDestination), async () =>
        {
            var result = await Mediator.Send(new MarkIFoodArrivedAtDestinationCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/logistics/order/{ifoodOrderId:long}/verify-delivery-code")]
    public Task<IActionResult> VerifyIFoodDeliveryCode(long ifoodOrderId, [FromBody] VerifyIFoodDeliveryCodeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(VerifyIFoodDeliveryCode), async () =>
        {
            var result = await Mediator.Send(new VerifyIFoodDeliveryCodeCommand(ifoodOrderId, request.Code), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { codeMatched = result.Value });
        });

    // Shipping (fase 8, módulo Shipping) — entrega, via malha de entregadores do iFood, de
    // pedidos que NÃO vieram do iFood (telefone, WhatsApp, site próprio). Tudo sob demanda: a
    // equipe cota o endereço, confirma o pedido de motorista, acompanha o rastreamento e cancela
    // se preciso. Também cobre a variante "pedido já existente no iFood" (quote/requestDriver/
    // cancelRequestDriver sobre um ifoodOrderId), que fecha uma lacuna do módulo Order.
    [HttpGet("ifood/shipping/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodShippingDeliveries(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodShippingDeliveries), async () =>
        {
            var result = await Mediator.Send(new GetIFoodShippingDeliveriesQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/shipping/branch/{branchId:long}/quote")]
    public Task<IActionResult> GetIFoodShippingQuote(long branchId, [FromQuery] double latitude, [FromQuery] double longitude, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodShippingQuote), async () =>
        {
            var result = await Mediator.Send(new GetIFoodShippingQuoteQuery(branchId, latitude, longitude), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/shipping")]
    public Task<IActionResult> RequestIFoodShippingDriver([FromBody] RequestIFoodShippingDriverCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodShippingDriver), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { id = result.Value });
        });

    [HttpGet("ifood/shipping/{id:long}/tracking")]
    public Task<IActionResult> GetIFoodShippingTracking(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodShippingTracking), async () =>
        {
            var result = await Mediator.Send(new GetIFoodShippingTrackingQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/shipping/{id:long}/cancellation-reasons")]
    public Task<IActionResult> GetIFoodShippingCancellationReasons(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodShippingCancellationReasons), async () =>
        {
            var result = await Mediator.Send(new GetIFoodShippingCancellationReasonsQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/shipping/{id:long}/cancel")]
    public Task<IActionResult> CancelIFoodShippingDelivery(long id, [FromBody] CancelIFoodShippingDeliveryRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIFoodShippingDelivery), async () =>
        {
            var result = await Mediator.Send(new CancelIFoodShippingDeliveryCommand(id, request.Reason, request.CancellationCode), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("ifood/shipping/{id:long}/safe-delivery-score")]
    public Task<IActionResult> GetIFoodSafeDeliveryScore(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodSafeDeliveryScore), async () =>
        {
            var result = await Mediator.Send(new GetIFoodSafeDeliveryScoreQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Variante "pedido já existente no iFood" (mesmo módulo Shipping, atua sobre um ifoodOrderId).
    [HttpGet("ifood/shipping/order/{ifoodOrderId:long}/quote")]
    public Task<IActionResult> GetIFoodOrderShippingQuote(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOrderShippingQuote), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOrderShippingQuoteQuery(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/shipping/order/{ifoodOrderId:long}/request-driver")]
    public Task<IActionResult> RequestIFoodOrderShippingDriver(long ifoodOrderId, [FromBody] RequestIFoodOrderShippingDriverRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(RequestIFoodOrderShippingDriver), async () =>
        {
            var result = await Mediator.Send(new RequestIFoodOrderShippingDriverCommand(ifoodOrderId, request.QuoteId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("ifood/shipping/order/{ifoodOrderId:long}/cancel-request-driver")]
    public Task<IActionResult> CancelIFoodOrderShippingDriver(long ifoodOrderId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(CancelIFoodOrderShippingDriver), async () =>
        {
            var result = await Mediator.Send(new CancelIFoodOrderShippingDriverCommand(ifoodOrderId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Avaliações (fase 9, módulo Review v1.0) — sem persistência local, sempre lido/escrito
    // direto no iFood (ver comentário em IIFoodReviewClient).
    [HttpGet("ifood/reviews/branch/{branchId:long}")]
    public Task<IActionResult> GetIFoodReviews(
        long branchId, [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, [FromQuery] string? sort, [FromQuery] string? sortBy, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodReviews), async () =>
        {
            var result = await Mediator.Send(new GetIFoodReviewsQuery(
                branchId, page <= 0 ? 1 : page, pageSize <= 0 ? 10 : pageSize, dateFrom, dateTo,
                string.IsNullOrWhiteSpace(sort) ? "DESC" : sort, string.IsNullOrWhiteSpace(sortBy) ? "CREATED_AT" : sortBy), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/reviews/branch/{branchId:long}/{reviewId}")]
    public Task<IActionResult> GetIFoodReviewById(long branchId, string reviewId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodReviewById), async () =>
        {
            var result = await Mediator.Send(new GetIFoodReviewByIdQuery(branchId, reviewId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("ifood/reviews/branch/{branchId:long}/{reviewId}/reply")]
    public Task<IActionResult> ReplyIFoodReview(long branchId, string reviewId, [FromBody] ReplyIFoodReviewRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(ReplyIFoodReview), async () =>
        {
            var result = await Mediator.Send(new ReplyIFoodReviewCommand(branchId, reviewId, request.Text), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("ifood/reviews/branch/{branchId:long}/summary")]
    public Task<IActionResult> GetIFoodReviewsSummary(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodReviewsSummary), async () =>
        {
            var result = await Mediator.Send(new GetIFoodReviewsSummaryQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Indicadores (fase 9, módulo Analytics v1.0) — 1 endpoint (KPIs de pedidos); ver ressalva
    // sobre o payload padrão usado em IIFoodAnalyticsClient.
    [HttpGet("ifood/analytics/branch/{branchId:long}/order-kpis")]
    public Task<IActionResult> GetIFoodOrderKpis(
        long branchId, [FromQuery] DateTime? periodStart, [FromQuery] DateTime? periodEnd, [FromQuery] int page, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(IntegrationsController), nameof(GetIFoodOrderKpis), async () =>
        {
            var result = await Mediator.Send(new GetIFoodOrderKpisQuery(branchId, periodStart, periodEnd, page), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}

public sealed record SyncIFoodCatalogRequest(long CompanyId);

public sealed record CancelIFoodOrderRequest(string ReasonCode);

public sealed record SyncIFoodFinancialRequest(long CompanyId);

public sealed record AssignIFoodDriverRequest(string DriverName, string DriverPhone, string DriverVehicleType);

public sealed record VerifyIFoodDeliveryCodeRequest(string Code);

public sealed record CancelIFoodShippingDeliveryRequest(string Reason, int CancellationCode);

public sealed record RequestIFoodOrderShippingDriverRequest(string QuoteId);

public sealed record RequestIFoodReconciliationOnDemandRequest(string Competence);

public sealed record ReplyIFoodReviewRequest(string Text);
