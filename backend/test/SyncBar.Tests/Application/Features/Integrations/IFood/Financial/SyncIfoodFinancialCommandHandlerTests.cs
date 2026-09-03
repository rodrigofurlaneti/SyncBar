using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Financial;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Financial;

public sealed class SyncIfoodFinancialCommandHandlerTests
{
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodFinancialClient _financialClient = Substitute.For<IIfoodFinancialClient>();
    private readonly IIfoodMerchantMappingRepository _merchantMappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodFinancialEventRepository _financialEventRepository = Substitute.For<IIfoodFinancialEventRepository>();
    private readonly IIfoodSettlementRepository _settlementRepository = Substitute.For<IIfoodSettlementRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SyncIfoodFinancialCommandHandler _handler;

    public SyncIfoodFinancialCommandHandlerTests()
    {
        _handler = new SyncIfoodFinancialCommandHandler(
            _settingRepository, _tokenProvider, _financialClient, _merchantMappingRepository,
            _financialEventRepository, _settlementRepository, _timeProvider, _logRepository, _unitOfWork);

        var now = new DateTime(2026, 9, 3, 10, 0, 0);
        // GetLocalNow() em si não é interceptável de forma confiável pelo NSubstitute — stubamos
        // os primitivos que a implementação real consome (GetUtcNow + LocalTimeZone fixo em UTC).
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(now, TimeSpan.Zero));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
    }

    private static IfoodIntegrationSetting CreateEnabledSetting()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        return setting;
    }

    private static IfoodMerchantMapping CreateActiveMapping(string merchantId)
    {
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant(merchantId, "uuid-1");
        return mapping;
    }

    [Fact]
    public async Task Handle_SettingNotFound_ShouldReturnSuccessWithoutCallingTokenProvider()
    {
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns((IfoodIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _tokenProvider.DidNotReceive().GetAccessTokenAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        // Sem sincronização nenhuma: só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IntegrationDisabled_ShouldReturnSuccessWithoutSyncing()
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: false, ifoodCustomerId: null);
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _tokenProvider.DidNotReceive().GetAccessTokenAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenUnavailable_ShouldReturnSuccessWithoutFetchingMappings()
    {
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _tokenProvider.GetAccessTokenAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _merchantMappingRepository.DidNotReceive().GetByCompanyAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoActiveBranchMappings_ShouldReturnSuccessWithoutCallingFinancialClient()
    {
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _tokenProvider.GetAccessTokenAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _merchantMappingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _financialClient.DidNotReceive().GetFinancialEventsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewEventAndSettlement_ShouldPersistBothAndCommitOncePerBranch()
    {
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _tokenProvider.GetAccessTokenAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _merchantMappingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping> { [1] = CreateActiveMapping("MERCH-1") });

        var eventDto = new IfoodFinancialEventDto(
            "evt-1", "Repasse", null, null, 100m, true, new DateTime(2026, 9, 1), new DateTime(2026, 8, 24),
            new DateTime(2026, 9, 3), null, null, null, "{}");
        _financialClient.GetFinancialEventsAsync("token-1", "MERCH-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([eventDto]);
        _financialEventRepository.ExistsByIfoodEventIdAsync(1, "evt-1", Arg.Any<CancellationToken>()).Returns(false);

        var settlementDto = new IfoodSettlementDto(
            "settle-1", "REPASSE", null, 500m, "SUCCEED", new DateTime(2026, 9, 2), "001", "1234", "56789-0", "{}");
        _financialClient.GetSettlementsAsync("token-1", "MERCH-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([settlementDto]);
        _settlementRepository.GetByIfoodSettlementIdForUpdateAsync(1, "settle-1", Arg.Any<CancellationToken>())
            .Returns((IfoodSettlement?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _financialEventRepository.Received(1).AddAsync(
            Arg.Is<IfoodFinancialEvent>(e => e.IfoodEventId == "evt-1" && e.BranchId == 1), Arg.Any<CancellationToken>());
        await _settlementRepository.Received(1).AddAsync(
            Arg.Is<IfoodSettlement>(s => s.IfoodSettlementId == "settle-1" && s.BranchId == 1), Arg.Any<CancellationToken>());
        // 1 commit explícito por filial sincronizada + 1 do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventAlreadySynced_ShouldSkipDuplicateInsert()
    {
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _tokenProvider.GetAccessTokenAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _merchantMappingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping> { [1] = CreateActiveMapping("MERCH-1") });

        var eventDto = new IfoodFinancialEventDto(
            "evt-1", "Repasse", null, null, 100m, true, new DateTime(2026, 9, 1), new DateTime(2026, 8, 24),
            new DateTime(2026, 9, 3), null, null, null, "{}");
        _financialClient.GetFinancialEventsAsync("token-1", "MERCH-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([eventDto]);
        _financialEventRepository.ExistsByIfoodEventIdAsync(1, "evt-1", Arg.Any<CancellationToken>()).Returns(true);
        _financialClient.GetSettlementsAsync("token-1", "MERCH-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _financialEventRepository.DidNotReceive().AddAsync(Arg.Any<IfoodFinancialEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SettlementAlreadyExists_ShouldUpdateInPlaceInsteadOfInserting()
    {
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _tokenProvider.GetAccessTokenAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _merchantMappingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping> { [1] = CreateActiveMapping("MERCH-1") });
        _financialClient.GetFinancialEventsAsync("token-1", "MERCH-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var existingSettlement = IfoodSettlement.Create(
            branchId: 1, IfoodSettlementId: "settle-1", type: "REPASSE", product: null, amount: 500m,
            status: "PENDING", paymentDate: null, bankCode: null, bankAgency: null, bankAccount: null, rawPayload: "{}").Value;
        var settlementDto = new IfoodSettlementDto(
            "settle-1", "REPASSE", null, 500m, "SUCCEED", new DateTime(2026, 9, 2), "001", "1234", "56789-0", "{\"status\":\"SUCCEED\"}");
        _financialClient.GetSettlementsAsync("token-1", "MERCH-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([settlementDto]);
        _settlementRepository.GetByIfoodSettlementIdForUpdateAsync(1, "settle-1", Arg.Any<CancellationToken>())
            .Returns(existingSettlement);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingSettlement.Status.Should().Be("SUCCEED");
        existingSettlement.PaymentDate.Should().Be(new DateTime(2026, 9, 2));
        await _settlementRepository.DidNotReceive().AddAsync(Arg.Any<IfoodSettlement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleActiveBranches_ShouldSyncEachAndCommitOncePerBranch()
    {
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _tokenProvider.GetAccessTokenAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _merchantMappingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping>
            {
                [1] = CreateActiveMapping("MERCH-1"),
                [2] = CreateActiveMapping("MERCH-2"),
            });
        _financialClient.GetFinancialEventsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _financialClient.GetSettlementsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _financialClient.Received(1).GetFinancialEventsAsync("token-1", "MERCH-1", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _financialClient.Received(1).GetFinancialEventsAsync("token-1", "MERCH-2", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        // 1 commit explícito por filial (2) + 1 do finally da base.
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SyncFailsForOneBranch_ShouldContinueWithOthersWithoutPropagatingFailure()
    {
        var command = new SyncIfoodFinancialCommand(CompanyId: 1);
        _settingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns(CreateEnabledSetting());
        _tokenProvider.GetAccessTokenAsync(command.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _merchantMappingRepository.GetByCompanyAsync(command.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, IfoodMerchantMapping>
            {
                [1] = CreateActiveMapping("MERCH-FAILS"),
                [2] = CreateActiveMapping("MERCH-OK"),
            });
        _financialClient.GetFinancialEventsAsync("token-1", "MERCH-FAILS", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyCollection<IfoodFinancialEventDto>>(_ => throw new InvalidOperationException("Ifood API unavailable"));
        _financialClient.GetFinancialEventsAsync("token-1", "MERCH-OK", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _financialClient.GetSettlementsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _financialClient.Received(1).GetFinancialEventsAsync("token-1", "MERCH-OK", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        // A filial que falhou não chega a commitar; a que funcionou commita 1x + o finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
