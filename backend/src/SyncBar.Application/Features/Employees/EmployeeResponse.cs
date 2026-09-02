namespace SyncBar.Application.Features.Employees;

public sealed record EmployeeResponse(
    long Id,
    long BranchId,
    long JobTitleId,
    string Name,
    string Cpf,
    string? Email,
    string? Phone,
    DateTime HiredAt,
    DateTime? DismissedAt,
    decimal? Salary,
    decimal? CommissionPercent,
    bool IsActive,
    // Resumo do acesso ao sistema desta pessoa — evita a tela Equipe ter que ir buscar em
    // Usuários/Acessos separadamente para saber quem tem login. RoleName é o Perfil
    // auto-provisionado a partir do Cargo (ver CreateUserCommandHandler/RegisterTeamMemberCommandHandler);
    // ExtraFeatureCount conta só os acessos ALÉM do que o Cargo já libera por padrão.
    bool HasSystemAccess,
    long? AppUserId,
    string? RoleName,
    int ExtraFeatureCount);

public sealed record JobTitleResponse(long Id, string Name);
