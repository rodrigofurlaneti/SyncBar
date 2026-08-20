using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Catalog;
using SyncBar.Application.Features.Integrations.IFood.Financial;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Application.Features.Integrations.IFood.Orders;
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
}

public sealed record SyncIFoodCatalogRequest(long CompanyId);

public sealed record CancelIFoodOrderRequest(string ReasonCode);

public sealed record SyncIFoodFinancialRequest(long CompanyId);
