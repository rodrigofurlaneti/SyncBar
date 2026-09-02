using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Employees.RegisterTeamMember;

// Orquestra em um único passo o que antes exigia duas telas separadas (Equipe + Usuários) com
// campos duplicados (Nome/E-mail) e uma terceira tela de "Perfil" desconectada do Cargo: cadastra
// sempre o Employee (todo colaborador entra na Equipe, mesmo quem não usa o sistema — auxiliar de
// limpeza, vigilante) e, só quando HasSystemAccess=true, também o AppUser (com o Perfil de acesso
// auto-provisionado a partir do Cargo — ver CreateUserCommandHandler) e, opcionalmente, exceções
// de acesso por pessoa além do que o Cargo já concede por padrão (JobTitleFeature).
public sealed record RegisterTeamMemberCommand(
    long BranchId,
    long CompanyId,
    long JobTitleId,
    string Name,
    string Cpf,
    string? Email,
    string? Phone,
    DateTime HiredAt,
    decimal? Salary,
    bool HasSystemAccess,
    string? UserName,
    string? UserEmail,
    string? Password,
    IReadOnlyCollection<long>? ExtraFeatureIds) : ICommand<RegisterTeamMemberResult>;

// AppUserId nulo com AccessWarning preenchido = "degradação graciosa": o Employee foi criado com
// sucesso, mas o AppUser não pôde ser criado (ex.: username/e-mail já em uso). Isso é reportado
// como sucesso parcial, não como falha — o funcionário cadastrado é um resultado válido por si só
// (nem todo colaborador precisa de usuário), e a pessoa pode tentar criar o acesso de novo na tela
// Usuários sem perder o cadastro já feito.
public sealed record RegisterTeamMemberResult(long EmployeeId, long? AppUserId, string? AccessWarning);
