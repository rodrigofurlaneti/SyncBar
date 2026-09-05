using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Update
{
    public sealed class UpdateAsaasIntegrationPaymentCommandValidator
        : AbstractValidator<UpdateAsaasIntegrationPaymentCommand>
    {
        private static readonly string[] ValidStatuses =
        [
            "PENDING",                      // Aguardando pagamento
            "RECEIVED",                     // Cobrança recebida (saldo já disponível)
            "CONFIRMED",                    // Pagamento confirmado (saldo a ser creditado)
            "OVERDUE",                      // Cobrança vencida
            "REFUNDED",                     // Cobrança estornada
            "RECEIVED_IN_CASH",             // Recebida em dinheiro (não compensada no gateway)
            "REFUND_REQUESTED",             // Estorno solicitado
            "CHARGEBACK_REQUESTED",         // Chargeback solicitado pelo titular do cartão
            "CHARGEBACK_DISPUTE",           // Em disputa de chargeback (apresentação de defesa)
            "AWAITING_CHARGEBACK_REVERSAL", // Aguardando reversão do estorno/chargeback
            "DUNNING_REQUESTED",            // Processo de negativação/recuperação solicitado
            "DUNNING_RECEIVED",             // Valor recuperado via processo de negativação
            "AWAITING_RISK_ANALYSIS"        // Em análise manual de risco antifraude
        ];

        public UpdateAsaasIntegrationPaymentCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do pagamento (Id) deve ser maior que zero.");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("O status do pagamento é obrigatório.")
                .Must(status => ValidStatuses.Contains(status.ToUpperInvariant()))
                .WithMessage("Status do Asaas inválido.");

            When(x => x.NetValue.HasValue, () =>
            {
                RuleFor(x => x.NetValue!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("O valor líquido (NetValue) não pode ser negativo.");
            });
        }
    }
}
