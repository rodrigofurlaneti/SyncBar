using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.V1Legacy;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Catalog.V1Legacy;

public sealed class InvokeIfoodCatalogV1OperationCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();
    private readonly IIfoodIntegrationSettingRepository _settingRepository = Substitute.For<IIfoodIntegrationSettingRepository>();
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly IIfoodCatalogClient _catalogClient = Substitute.For<IIfoodCatalogClient>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly InvokeIfoodCatalogV1OperationCommandHandler _handler;

    public InvokeIfoodCatalogV1OperationCommandHandlerTests()
    {
        _handler = new InvokeIfoodCatalogV1OperationCommandHandler(
            _branchRepository, _tokenProvider, _settingRepository, _mappingRepository, _catalogClient, _logRepository, _unitOfWork);
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
    public async Task Handle_BranchNotFound_ShouldPropagateResolutionFailure()
    {
        var command = new InvokeIfoodCatalogV1OperationCommand(1, IfoodCatalogV1Operation.ListCatalogs, null, null, null);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodMerchant.BranchNotFound");
    }

    [Fact]
    public async Task Handle_LocalCallFailure_StatusCodeZero_ShouldReturnV1InvokeFailed()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new InvokeIfoodCatalogV1OperationCommand(1, IfoodCatalogV1Operation.ListCatalogs, null, null, null);
        _catalogClient.InvokeCatalogV1Async(
            "token-1", "MERCH-1", IfoodCatalogV1Operation.ListCatalogs,
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(false, 0, null, "conexão recusada"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.V1InvokeFailed");
    }

    [Fact]
    public async Task Handle_RemoteHttpError_ShouldStillReturnSuccessWithRawResponse()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var command = new InvokeIfoodCatalogV1OperationCommand(1, IfoodCatalogV1Operation.ListCatalogs, null, null, null);
        _catalogClient.InvokeCatalogV1Async(
            "token-1", "MERCH-1", IfoodCatalogV1Operation.ListCatalogs,
            Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(false, 404, "{\"error\":\"not found\"}", "Not Found"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.StatusCode.Should().Be(404);
        result.Value.ResponseBody.Should().Be("{\"error\":\"not found\"}");
        result.Value.ErrorMessage.Should().Be("Not Found");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldForwardRouteAndQueryParamsAndReturnMappedResponse()
    {
        var branch = CreateBranch();
        SetupResolvedMerchant(branch);
        var routeParams = new Dictionary<string, string> { ["itemId"] = "item-1" };
        var queryParams = new Dictionary<string, string> { ["page"] = "1" };
        var command = new InvokeIfoodCatalogV1OperationCommand(1, IfoodCatalogV1Operation.GetItem, routeParams, queryParams, "{\"name\":\"x\"}");
        _catalogClient.InvokeCatalogV1Async(
            "token-1", "MERCH-1", IfoodCatalogV1Operation.GetItem, routeParams, queryParams, "{\"name\":\"x\"}", Arg.Any<CancellationToken>())
            .Returns(new IfoodRawApiResult(true, 200, "{\"id\":\"item-1\"}", null));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.StatusCode.Should().Be(200);
        result.Value.ResponseBody.Should().Be("{\"id\":\"item-1\"}");
    }
}
