using FluentValidation;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.Update
{
    public sealed class UpdateBranchPaymentMethodSettingCommandValidator
        : AbstractValidator<UpdateBranchPaymentMethodSettingCommand>
    {
        public UpdateBranchPaymentMethodSettingCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador da configuração (Id) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}