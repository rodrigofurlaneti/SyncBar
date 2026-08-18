using System.Security.Claims;
using SyncBar.Application.Abstractions.Security;

namespace SyncBar.API.Services;

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public long UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User
                ?? throw new InvalidOperationException("Nenhum HttpContext disponível para resolver o usuário atual.");

            // Ajuste o tipo de claim para o mesmo usado no AuthController ao gerar o token
            // (ex.: ClaimTypes.NameIdentifier, "sub", ou um claim customizado como "appUserId").
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Claim de identificação do usuário não encontrada no token.");

            if (!long.TryParse(claim.Value, out var userId))
                throw new InvalidOperationException("Claim de identificação do usuário não é um Id válido.");

            return userId;
        }
    }

    public long? EmployeeId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst("employeeId");

            if (claim is null || !long.TryParse(claim.Value, out var employeeId))
                return null;

            return employeeId;
        }
    }
}