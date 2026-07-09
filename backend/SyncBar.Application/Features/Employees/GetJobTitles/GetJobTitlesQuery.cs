using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Employees.GetJobTitles;

public sealed record GetJobTitlesQuery(long CompanyId) : IQuery<IReadOnlyCollection<JobTitleResponse>>;
