using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Users.GetByCompany;

public sealed record GetUsersByCompanyQuery(long CompanyId) : IQuery<IReadOnlyCollection<UserResponse>>;
