using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.ExistsByToken
{
    public sealed record ExistsByTokenQuery(
        string CreditCardToken) : IQuery<bool>;
}
