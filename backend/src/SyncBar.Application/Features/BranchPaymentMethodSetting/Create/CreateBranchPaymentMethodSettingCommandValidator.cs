using FluentValidation;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.Create
{
    public sealed class CreateBranchPaymentMethodSettingCommandValidator : AbstractValidator<CreateBranchPaymentMethodSettingCommand>
    {
        public CreateBranchPaymentMethodSettingCommandValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .When(x => x.BranchId.HasValue)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero quando informado.");
        }
    }
}