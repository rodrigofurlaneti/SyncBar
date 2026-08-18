namespace SyncBar.Application.Abstractions.Security;

/// <summary>
/// Abstração do usuário autenticado na requisição atual.
/// Implementada na camada API/Infrastructure a partir das claims do JWT,
/// mantendo a Application layer sem dependência de HttpContext.
/// </summary>
public interface ICurrentUserService
{
    long UserId { get; }

    /// <summary>
    /// Employee.Id vinculado ao AppUser logado (espelha AppUser.EmployeeId, que é nullable).
    /// Null quando o AppUser não tem funcionário vinculado.
    /// </summary>
    long? EmployeeId { get; }
}