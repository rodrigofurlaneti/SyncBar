using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetById
{
    public sealed class GetAsaasCustomerByIdQueryValidator : AbstractValidator<GetAsaasCustomerByIdQuery>
    {
        public GetAsaasCustomerByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador (Id) deve ser maior que zero.");
        }
    }
}
