using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.GetByBranch;

internal sealed class GetEmployeesByBranchQueryHandler(
    IEmployeeRepository employeeRepository,
    IAppUserRepository appUserRepository,
    IAppUserFeatureRepository appUserFeatureRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetEmployeesByBranchQuery, IReadOnlyCollection<EmployeeResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<EmployeeResponse>>> Handle(
        GetEmployeesByBranchQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetEmployeesByBranchQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var employees = await employeeRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                // Resumo de acesso por pessoa (tela Equipe unificada — ver EmployeeResponse):
                // busca só os AppUsers ligados a este lote de funcionários e, para cada um, o
                // Perfil (Role) e a contagem de acessos extras além do Cargo.
                var employeeIds = employees.Select(e => e.Id).ToList();
                var appUsers = await appUserRepository.GetByEmployeeIdsAsync(employeeIds, cancellationToken);
                var appUserByEmployeeId = appUsers.ToDictionary(u => u.EmployeeId!.Value, u => u);

                var response = new List<EmployeeResponse>();
                foreach (var e in employees.OrderBy(e => e.Name))
                {
                    appUserByEmployeeId.TryGetValue(e.Id, out var appUser);

                    string? roleName = null;
                    var extraFeatureCount = 0;
                    if (appUser is not null)
                    {
                        var roleNames = await appUserRepository.GetRoleNamesAsync(appUser.Id, cancellationToken);
                        roleName = roleNames.FirstOrDefault();
                        var extras = await appUserFeatureRepository.GetByUserAsync(appUser.Id, cancellationToken);
                        extraFeatureCount = extras.Count;
                    }

                    response.Add(new EmployeeResponse(
                        e.Id, e.BranchId, e.JobTitleId, e.Name, e.Cpf, e.Email, e.Phone,
                        e.HiredAt, e.DismissedAt, e.Salary, e.CommissionPercent, e.IsActive,
                        appUser is not null, appUser?.Id, roleName, extraFeatureCount));
                }

                return Result.Success<IReadOnlyCollection<EmployeeResponse>>(response);
            });
    }
}