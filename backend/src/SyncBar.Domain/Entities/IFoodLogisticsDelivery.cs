using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

/// <summary>
/// Rastreia a entrega feita pela FROTA PRÓPRIA de um pedido Ifood (módulo Logistics, fase 7) —
/// só existe para pedidos com deliveredBy diferente de "Ifood" (self-delivery/frota própria);
/// pedidos entregues pela logística do próprio Ifood não usam esta entidade (não há o que o
/// SyncBar comandar nesse caso). 1:1 com <see cref="IfoodOrder"/>, referenciado pelo Id LOCAL
/// (long) do SyncBar — não pelo IfoodOrderId string do Ifood, mesma convenção de FK usada em
/// outras entidades ligadas a IfoodOrder.
///
/// Ressalva de confiança: nomes de ação/status batem com a doc oficial (POST assignDriver/
/// goingToOrigin/arrivedAtOrigin/dispatch/arrivedAtDestination/verifyDeliveryCode, todas 202
/// Accepted sem corpo, exceto verifyDeliveryCode que retorna {success: boolean} e pode devolver
/// 412 se o pedido ainda não foi recebido ou não é self-delivery) — confirmados contra a doc
/// oficial (Postman collection "Logistics") colada pelo usuário em 2026-08-20. A ORDEM exata das
/// transições (DRIVER_ASSIGNED → GOING_TO_ORIGIN → ARRIVED_AT_ORIGIN → DISPATCHED →
/// ARRIVED_AT_DESTINATION → DELIVERY_CODE_VERIFIED) é inferida do nome/sequência dos endpoints
/// na doc, não de um diagrama de estados explícito — validada aqui no Domain para não deixar a
/// tela pular passos, mas vale reconferir se o Ifood devolver erro de sequência inesperado.
/// </summary>
public sealed class IfoodLogisticsDelivery : AggregateRoot
{
    private const string InvalidTransitionErrorCode = "IfoodLogisticsDelivery.InvalidTransition";

    public long IfoodOrderId { get; private set; }
    public long BranchId { get; private set; }
    public string DriverName { get; private set; } = null!;
    public string DriverPhone { get; private set; } = null!;
    public string DriverVehicleType { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public DateTime AssignedAt { get; private set; }
    public DateTime? GoingToOriginAt { get; private set; }
    public DateTime? ArrivedAtOriginAt { get; private set; }
    public DateTime? DispatchedAt { get; private set; }
    public DateTime? ArrivedAtDestinationAt { get; private set; }
    public DateTime? DeliveryCodeVerifiedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodLogisticsDelivery() : base(0) { }

    private IfoodLogisticsDelivery(
        long IfoodOrderId, long branchId, string driverName, string driverPhone, string driverVehicleType, DateTime now) : base(0)
    {
        IfoodOrderId = IfoodOrderId;
        BranchId = branchId;
        DriverName = driverName;
        DriverPhone = driverPhone;
        DriverVehicleType = driverVehicleType;
        Status = IfoodLogisticsStatuses.DriverAssigned;
        AssignedAt = now;
        IsActive = true;
        CreatedAt = now;
    }

    public static Result<IfoodLogisticsDelivery> Create(
        long IfoodOrderId, long branchId, string driverName, string driverPhone, string driverVehicleType, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(driverName))
            return Result.Failure<IfoodLogisticsDelivery>(new Error("IfoodLogisticsDelivery.MissingDriverName", "Driver name is required."));
        if (string.IsNullOrWhiteSpace(driverPhone))
            return Result.Failure<IfoodLogisticsDelivery>(new Error("IfoodLogisticsDelivery.MissingDriverPhone", "Driver phone is required."));
        if (string.IsNullOrWhiteSpace(driverVehicleType))
            return Result.Failure<IfoodLogisticsDelivery>(new Error("IfoodLogisticsDelivery.MissingVehicleType", "Driver vehicle type is required."));

        return Result.Success(new IfoodLogisticsDelivery(
            IfoodOrderId, branchId, driverName.Trim(), driverPhone.Trim(), driverVehicleType.Trim(), now));
    }

    public Result MarkGoingToOrigin(DateTime now)
    {
        if (Status != IfoodLogisticsStatuses.DriverAssigned)
            return Result.Failure(new Error(InvalidTransitionErrorCode,
                "O entregador precisa estar atribuído antes de sair para a origem."));

        Status = IfoodLogisticsStatuses.GoingToOrigin;
        GoingToOriginAt = now;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result MarkArrivedAtOrigin(DateTime now)
    {
        if (Status != IfoodLogisticsStatuses.GoingToOrigin)
            return Result.Failure(new Error(InvalidTransitionErrorCode,
                "O entregador precisa estar a caminho da origem antes de chegar nela."));

        Status = IfoodLogisticsStatuses.ArrivedAtOrigin;
        ArrivedAtOriginAt = now;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result MarkDispatched(DateTime now)
    {
        if (Status != IfoodLogisticsStatuses.ArrivedAtOrigin)
            return Result.Failure(new Error(InvalidTransitionErrorCode,
                "O entregador precisa ter chegado na origem antes de despachar."));

        Status = IfoodLogisticsStatuses.Dispatched;
        DispatchedAt = now;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result MarkArrivedAtDestination(DateTime now)
    {
        if (Status != IfoodLogisticsStatuses.Dispatched)
            return Result.Failure(new Error(InvalidTransitionErrorCode,
                "O entregador precisa estar despachado antes de chegar no destino."));

        Status = IfoodLogisticsStatuses.ArrivedAtDestination;
        ArrivedAtDestinationAt = now;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result MarkDeliveryCodeVerified(DateTime now)
    {
        if (Status != IfoodLogisticsStatuses.ArrivedAtDestination)
            return Result.Failure(new Error(InvalidTransitionErrorCode,
                "O entregador precisa ter chegado no destino antes de verificar o código de entrega."));

        Status = IfoodLogisticsStatuses.DeliveryCodeVerified;
        DeliveryCodeVerifiedAt = now;
        UpdatedAt = now;
        return Result.Success();
    }

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}
