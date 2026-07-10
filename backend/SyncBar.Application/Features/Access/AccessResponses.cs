namespace SyncBar.Application.Features.Access;

public sealed record FeatureResponse(long Id, string Code, string Name);

public sealed record MyFeaturesResponse(
    bool CanManageAccess,
    IReadOnlyCollection<string> Features);
