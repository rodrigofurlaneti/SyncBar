using SyncBar.Application.Abstractions.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Create
{
    public sealed record CreateAsaasIntegrationPaymentCommand(
        long BranchId,
        long CustomerOrderId,
        long? CustomerId,
        string BillingType,
        decimal Value,
        DateTime DueDate,
        int InstallmentCount = 1,
        string? CreditCardToken = null,
        CreditCardDataRequest? CreditCard = null) : ICommand<CreateAsaasIntegrationPaymentResponse>;
}
