using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Financial;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Financial;

public sealed class RequestIfoodReconciliationOnDemandCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodFinancialClient _financialClient = Substitute.For<IIfoodFinancialClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RequestIfoodReconciliationOnDemandCommandHandler _handler;

    public RequestIfoodReconciliationOnDemandCommandHandlerTests()
    {
        _handler = new RequestIfoodReconciliationOnDemandCommandHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _financialClient,
            _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    [Fact]
    public async Task Handle_BranchNotFound_ShouldPropagateResolutionFailureWithoutCallingFinancialClient()
    {
        var command = new RequestIfoodReconciliationOnDemandCommand(BranchId: 1, Competence: "2026-08");
        _branchRepository.GetByIdAsync(command.BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
        await _financialClient.DidNotReceive().RequestReconciliationOnDemandAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        // Handler não persiste nada localmente — só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldForwardCompetenceAndReturnMappedResponse()
    {
        var branch = CreateBranch();
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant("MERCH-1", "uuid-1");
        var command = new RequestIfoodReconciliationOnDemandCommand(BranchId: 1, Competence: "2026-08");
        _branchRepository.GetByIdAsync(command.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _mappingRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>()).Returns(mapping);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns("token-1");
        _financialClient.RequestReconciliationOnDemandAsync("token-1", "MERCH-1", "2026-08", Arg.Any<CancellationToken>())
            .Returns(new IfoodReconciliationOnDemandRequestDto("req-1", "{\"status\":\"PENDING\"}"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestId.Should().Be("req-1");
        result.Value.RawPayload.Should().Be("{\"status\":\"PENDING\"}");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
