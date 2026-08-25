using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Dining.Table.Create
{
    public sealed class CreateDiningAreaTableCommandValidator : AbstractValidator<CreateDiningAreaTableCommand>
    {
        public CreateDiningAreaTableCommandValidator()
        {
            RuleFor(x => x.DiningAreaId).GreaterThan(0);
            RuleFor(x => x.DiningTableId).GreaterThan(0);
        }
    }
}
