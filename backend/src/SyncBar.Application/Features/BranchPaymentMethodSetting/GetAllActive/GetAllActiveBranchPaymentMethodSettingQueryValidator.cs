using FluentValidation;

namespace SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive
{
    // Sem parametros para validar (query nao tem argumentos) — validador vazio mantido apenas
    // para seguir o padrao do projeto de todo Command/Query ter um validator correspondente.
    public sealed class GetAllActiveBranchPaymentMethodSettingQueryValidator
        : AbstractValidator<GetAllActiveBranchPaymentMethodSettingQuery>
    {
    }
}
