using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class RequestIfoodShippingDriverCommandHandler : BaseCommandHandler<RequestIfoodShippingDriverCommand, long>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodIntegrationSettingRepository _settingRepository;
    private readonly IIfoodMerchantMappingRepository _mappingRepository;
    private readonly IIfoodShippingClient _shippingClient;
    private readonly IIfoodShippingDeliveryRepository _deliveryRepository;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public RequestIfoodShippingDriverCommandHandler(
        IBranchRepository branchRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodIntegrationSettingRepository settingRepository,
        IIfoodMerchantMappingRepository mappingRepository,
        IIfoodShippingClient shippingClient,
        IIfoodShippingDeliveryRepository deliveryRepository,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _settingRepository = settingRepository;
        _mappingRepository = mappingRepository;
        _shippingClient = shippingClient;
        _deliveryRepository = deliveryRepository;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(RequestIfoodShippingDriverCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RequestIfoodShippingDriverCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<long>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;

                var items = request.Items
                    .Select(i => new IfoodShippingItemPayload(
                        i.Name, i.ExternalCode, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity, i.UnitPrice * i.Quantity))
                    .ToList();

                var payload = new IfoodShippingRequestDriverPayload(
                    request.CustomerName, request.CustomerPhoneAreaCode, request.CustomerPhoneNumber,
                    request.MerchantFee, request.QuoteId,
                    request.PostalCode, request.StreetNumber, request.StreetName, request.Complement, request.Neighborhood,
                    request.City, request.State, string.IsNullOrWhiteSpace(request.Country) ? "Brasil" : request.Country,
                    request.Reference, request.Latitude, request.Longitude, items);

                var driverResult = await _shippingClient.RequestDriverAsync(token, merchantId, payload, cancellationToken);
                if (!driverResult.Success || string.IsNullOrWhiteSpace(driverResult.DeliveryId))
                    return Result.Failure<long>(new Error("IfoodShipping.RequestDriverFailed",
                        driverResult.ErrorMessage ?? "Falha ao solicitar entregador no Ifood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var deliveryResult = IfoodShippingDelivery.Create(
                    request.BranchId, request.OrderReference, request.CustomerName, request.CustomerPhoneAreaCode, request.CustomerPhoneNumber,
                    request.PostalCode, request.StreetName, request.StreetNumber, request.Complement, request.Neighborhood,
                    request.City, request.State, request.Country ?? "Brasil", request.Reference, request.Latitude, request.Longitude,
                    request.MerchantFee, request.QuoteId, driverResult.DeliveryId, driverResult.TrackingUrl, now);
                if (deliveryResult.IsFailure)
                    return Result.Failure<long>(deliveryResult.Error);

                await _deliveryRepository.AddAsync(deliveryResult.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(deliveryResult.Value.Id);
            });
    }
}
