using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.GetJobTitles;

internal sealed class GetJobTitlesQueryHandler(
    IJobTitleRepository jobTitleRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetJobTitlesQuery, IReadOnlyCollection<JobTitleResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<JobTitleResponse>>> Handle(
        GetJobTitlesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetJobTitlesQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está executando a ação, preencha:
                // userIdBox.Value = request.UserId;

                var jobTitles = await jobTitleRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

                IReadOnlyCollection<JobTitleResponse> response = jobTitles
                    .OrderBy(j => j.Name)
                    .Select(j => new JobTitleResponse(j.Id, j.Name))
                    .ToList();

                return Result.Success(response);
            });
    }
}