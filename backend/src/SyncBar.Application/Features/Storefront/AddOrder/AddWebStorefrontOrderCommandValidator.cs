using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Storefront.AddOrder
{
    public sealed class AddWebStorefrontOrderCommandValidator : AbstractValidator<AddWebStorefrontOrderCommand>
    {
        public AddWebStorefrontOrderCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial é obrigatório.");

            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .When(x => x.CustomerId.HasValue)
                .WithMessage("O identificador do cliente é inválido.");

            RuleFor(x => x.CustomerName)
                .NotEmpty()
                .MaximumLength(150)
                .WithMessage("O nome do cliente é obrigatório e deve ter no máximo 150 caracteres.");

            RuleFor(x => x.CustomerPhone)
                .MaximumLength(20)
                .When(x => !string.IsNullOrEmpty(x.CustomerPhone))
                .WithMessage("O telefone deve ter no máximo 20 caracteres.");

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("O carrinho deve conter pelo menos um item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .GreaterThan(0)
                    .WithMessage("O ID do produto é obrigatório.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("A quantidade do item deve ser maior que zero.");
            });
        }
    }
}