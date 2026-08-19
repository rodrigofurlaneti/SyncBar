using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.IFood;
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
}

public sealed record CancelIFoodOrderRequest(string ReasonCode);
