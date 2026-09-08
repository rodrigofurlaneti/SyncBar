using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SyncBar.Application.Features.Access.GetMyFeatures;
using SyncBar.Domain.Constants;

namespace SyncBar.API.Authorization;

// Bloqueio no SERVIDOR: esconder o menu no frontend nao basta —
// toda chamada a um controller com policy Feature:X passa por aqui.
internal sealed class FeatureAuthorizationHandler(IMediator mediator)
    : AuthorizationHandler<FeatureRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, FeatureRequirement requirement)
    {
        // Gerente/Administrador tem acesso total.
        if (FeatureCodes.ManagerRoles.Any(context.User.IsInRole))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? context.User.FindFirstValue("sub");
        if (!long.TryParse(userIdClaim, out var userId))
            return;

        var result = await mediator.Send(new GetMyFeaturesQuery(userId, false));
        if (result.IsSuccess && requirement.FeatureCode.Split(',').Any(result.Value.Features.Contains))
            context.Succeed(requirement);
    }
}
