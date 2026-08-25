using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Table.GetById
{
    public sealed record GetDiningAreaTableByIdQuery(long Id) : IQuery<DiningAreaTableResponse>;
}
