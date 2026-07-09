using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.GetJobTitles;

internal sealed class GetJobTitlesQueryHandler(IJobTitleRepository jobTitleRepository)
    : IQueryHandler<GetJobTitlesQuery, IReadOnlyCollection<JobTitleResponse>>
{
    public async Task<Result<IReadOnlyCollection<JobTitleResponse>>> Handle(
        GetJobTitlesQuery request, CancellationToken cancellationToken)
    {
        var jobTitles = await jobTitleRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

        IReadOnlyCollection<JobTitleResponse> response = jobTitles
            .OrderBy(j => j.Name)
            .Select(j => new JobTitleResponse(j.Id, j.Name))
            .ToList();

        return Result.Success(response);
    }
}
