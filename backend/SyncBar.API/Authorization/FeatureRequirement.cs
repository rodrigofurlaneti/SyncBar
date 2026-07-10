using Microsoft.AspNetCore.Authorization;

namespace SyncBar.API.Authorization;

public sealed class FeatureRequirement(string featureCode) : IAuthorizationRequirement
{
    public string FeatureCode { get; } = featureCode;
}
