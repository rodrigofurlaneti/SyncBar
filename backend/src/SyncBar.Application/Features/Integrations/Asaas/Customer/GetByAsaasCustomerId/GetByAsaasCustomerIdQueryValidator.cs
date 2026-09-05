using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId
{
    public sealed class GetByAsaasCustomerIdQueryValidator : AbstractValidator<GetByAsaasCustomerIdQuery>
    {
        public GetByAsaasCustomerIdQueryValidator()
        {
            RuleFor(x => x.AsaasCustomerId)
                .NotEmpty()
                .WithMessage("O AsaasCustomerId é obrigatório.")
                .MaximumLength(50)
                .WithMessage("O AsaasCustomerId deve conter no máximo 50 caracteres.");
        }
    }
}
