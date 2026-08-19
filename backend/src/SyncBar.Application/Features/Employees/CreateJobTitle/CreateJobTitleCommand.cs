using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Employees.CreateJobTitle;

public sealed record CreateJobTitleCommand(long CompanyId, string Name) : ICommand<long>;
