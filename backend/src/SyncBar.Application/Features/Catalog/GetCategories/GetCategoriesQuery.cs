using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.GetCategories;

public sealed record GetCategoriesQuery(long CompanyId) : IQuery<IReadOnlyCollection<CategoryResponse>>;
