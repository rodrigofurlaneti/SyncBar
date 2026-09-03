using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Financial;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Financial;

public sealed class GetIfoodReconciliationOnDemandStatusQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodFinancialClient _financialClient = Substitute.For<IIfoodFinancialClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodReconciliationOnDemandStatusQueryHandler _handler;

    public GetIfoodReconciliationOnDemandStatusQueryHandlerTests()
    {
        _handler = new GetIfoodReconciliationOnDemandStatusQueryHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _financialClient,
            _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    private void SetupResolvedMerchant(Branch branch, string merchantId = "MERCH-1", string token = "token-1")
    {
        var setting = IfoodIntegrationSetting.Create(companyId: 1).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        var mapping = IfoodMerchantMapping.Create(branchId: 1).Value;
        mapping.SetMerchant(merchantId, "uuid-1");

        _branchRepository.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(setting);
        _mappingRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(mapping);
        _tokenProvider.GetAccessTokenAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns(token);
    }

    [Fact]
    public async Task Handle_IntegrationNotConfigured_ShouldPropagateResolutionFailureWithoutCallingFinancialClient()
    {
        var branch = CreateBranch();
        var query = new GetIfoodReconciliationOnDemandStatusQuery(BranchId: 1, RequestId: "req-1");
        _branchRepository.GetByIdAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _settingRepository.GetByCompanyAsync(branch.CompanyId, Arg.Any<CancellationToken>()).Returns((IfoodIntegrationSetting?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.NotConfigured");
        await _financialClient.DidNotReceive().GetReconciliationOnDemandStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StatusFound_ShouldReturnFoundTrueWithRawPayload()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodReconciliationOnDemandStatusQuery(BranchId: 1, RequestId: "req-1");
        _financialClient.GetReconciliationOnDemandStatusAsync("token-1", "MERCH-1", "req-1", Arg.Any<CancellationToken>())
            .Returns("{\"status\":\"CONCLUDED\"}");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Found.Should().BeTrue();
        result.Value.RawPayload.Should().Be("{\"status\":\"CONCLUDED\"}");
    }

    [Fact]
    public async Task Handle_StatusNotFound_ShouldReturnFoundFalseWithNullPayload()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var query = new GetIfoodReconciliationOnDemandStatusQuery(BranchId: 1, RequestId: "req-missing");
        _financialClient.GetReconciliationOnDemandStatusAsync("token-1", "MERCH-1", "req-missing", Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Found.Should().BeFalse();
        result.Value.RawPayload.Should().BeNull();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
