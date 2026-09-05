using SyncBar.Application.Abstractions.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create
{
    public sealed record CreateAsaasIntegrationSavedCardCommand(
        long CustomerId,
        long CompanyId,
        string HolderName,
        string CardNumber,
        string ExpiryMonth,
        string ExpiryYear,
        string Ccv,
        bool SetAsDefault = false) : ICommand<CreateAsaasIntegrationSavedCardResponse>;
}
