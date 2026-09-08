using FluentValidation;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetById
{
    public sealed class GetByIdBranchPaymentMethodSettingQueryValidator
        : AbstractValidator<GetByIdBranchPaymentMethodSettingQuery>
    {
        public GetByIdBranchPaymentMethodSettingQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador da configuração (Id) deve ser maior que zero.");
        }
    }
}