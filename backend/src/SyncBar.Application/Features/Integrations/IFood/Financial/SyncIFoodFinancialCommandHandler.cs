using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

/// <summary>
/// Núcleo da Fase 4 (financeiro): busca Financial Events + Settlement dos últimos dias, por
/// filial com MerchantId configurado, e grava/atualiza os registros locais de forma idempotente
/// (dedup por IfoodEventId pra eventos; get-or-update por IfoodSettlementId pra títulos, já que
/// o mesmo título pode ser reentregue com status diferente conforme o Ifood processa o repasse).
///
/// Este módulo é só de auditoria/reconciliação — não mexe no fluxo operacional existente
/// (Sale/CashSession/CashMovement continuam sendo a fonte de verdade do caixa físico da loja).
/// </summary>
internal sealed class SyncIfoodFinancialCommandHandler : BaseCommandHandler<SyncIfoodFinancialCommand>
{
    // Janela de sincronização: 10 dias — bem menor que o limite de 33 dias por chamada da API, e
    // com sobra suficiente pra cobrir eventos que a apuração semanal ainda não tinha consolidado
    // no ciclo anterior (dedup garante que reprocessar dias já sincronizados não duplica nada).
    private static readonly TimeSpan SyncWindow = TimeSpan.FromDays(10);

    private readonly IIfoodIntegrationSettingRepository _settingRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodFinancialClient _financialClient;
    private readonly IIfoodMerchantMappingRepository _merchantMappingRepository;
    private readonly IIfoodFinancialEventRepository _financialEventRepository;
    private readonly IIfoodSettlementRepository _settlementRepository;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public SyncIfoodFinancialCommandHandler(
        IIfoodIntegrationSettingRepository settingRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodFinancialClient financialClient,
        IIfoodMerchantMappingRepository merchantMappingRepository,
        IIfoodFinancialEventRepository financialEventRepository,
        IIfoodSettlementRepository settlementRepository,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _settingRepository = settingRepository;
        _tokenProvider = tokenProvider;
        _financialClient = financialClient;
        _merchantMappingRepository = merchantMappingRepository;
        _financialEventRepository = financialEventRepository;
        _settlementRepository = settlementRepository;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SyncIfoodFinancialCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SyncIfoodFinancialCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var setting = await _settingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                if (setting is null || !setting.Enabled || setting.ClientId is null)
                    return Result.Success();

                var token = await _tokenProvider.GetAccessTokenAsync(request.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Success(); // sem token válido — tenta de novo no próximo ciclo (1x/dia)

                var mappings = await _merchantMappingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var activeBranches = mappings
                    .Where(kv => kv.Value.IsActive && !string.IsNullOrWhiteSpace(kv.Value.MerchantId))
                    .ToList();

                if (activeBranches.Count == 0)
                    return Result.Success();

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var periodStart = now - SyncWindow;

                foreach (var (branchId, mapping) in activeBranches)
                {
                    try
                    {
                        await SyncBranchAsync(branchId, mapping.MerchantId!, token, periodStart, now, cancellationToken);
                    }
                    catch
                    {
                        // Falha numa filial não derruba a sincronização das demais — próximo
                        // ciclo (1x/dia) tenta de novo.
                    }
                }

                return Result.Success();
            });
    }

    private async Task SyncBranchAsync(
        long branchId, string merchantId, string token, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken)
    {
        var events = await _financialClient.GetFinancialEventsAsync(token, merchantId, periodStart, periodEnd, cancellationToken);
        foreach (var evt in events)
        {
            var exists = await _financialEventRepository.ExistsByIfoodEventIdAsync(branchId, evt.Id, cancellationToken);
            if (exists)
                continue;

            var result = IfoodFinancialEvent.Create(
                branchId, evt.Id, evt.Name, evt.Description, evt.Trigger, evt.Amount, evt.HasTransferImpact,
                evt.CompetenceDate, evt.PeriodStart, evt.PeriodEnd, evt.SettlementExpectedDate,
                evt.ReferenceType, evt.ReferenceId, evt.RawPayload);

            if (result.IsSuccess)
                await _financialEventRepository.AddAsync(result.Value, cancellationToken);
        }

        var settlements = await _financialClient.GetSettlementsAsync(token, merchantId, periodStart, periodEnd, cancellationToken);
        foreach (var settlement in settlements)
        {
            var existing = await _settlementRepository.GetByIfoodSettlementIdForUpdateAsync(branchId, settlement.Id, cancellationToken);
            if (existing is not null)
            {
                existing.UpdateFromSync(
                    settlement.Status, settlement.PaymentDate, settlement.BankCode, settlement.BankAgency,
                    settlement.BankAccount, settlement.RawPayload);
                continue;
            }

            var result = IfoodSettlement.Create(
                branchId, settlement.Id, settlement.Type, settlement.Product, settlement.Amount, settlement.Status,
                settlement.PaymentDate, settlement.BankCode, settlement.BankAgency, settlement.BankAccount, settlement.RawPayload);

            if (result.IsSuccess)
                await _settlementRepository.AddAsync(result.Value, cancellationToken);
        }

        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
