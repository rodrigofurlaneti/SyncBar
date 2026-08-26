using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Catalog.GetProductById
{
    public sealed record GetProductByIdQuery(long ProductId) : IQuery<ProductResponse>;
}
