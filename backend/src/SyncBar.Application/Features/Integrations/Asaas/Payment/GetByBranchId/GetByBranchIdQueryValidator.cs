using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByBranchId
{
    public sealed class GetByBranchIdQueryValidator : AbstractValidator<GetByBranchIdQuery>
    {
        public GetByBranchIdQueryValidator()
        {
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
