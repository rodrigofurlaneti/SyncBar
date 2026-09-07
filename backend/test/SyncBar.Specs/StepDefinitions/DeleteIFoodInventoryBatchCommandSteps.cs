using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Excluir estoque em lote no Ifood")]
public sealed class DeleteIFoodInventoryBatchCommandSteps
{
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<IIfoodTokenProvider> _tokenProvider = new();
    private readonly Mock<IIfoodIntegrationSettingRepository> _settingRepository = new();
    private readonly Mock<IIfoodMerchantMappingRepository> _mappingRepository = new();
    private readonly Mock<IIfoodCatalogClient> _catalogClient = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result? _result;

    private static Branch CreateBranch()
        => Branch.Create(
            companyId: 1, "Loja Centro", cnpj: null, phone: null, addressStreet: null, addressNumber: null,
            addressDistrict: null, addressCity: null, addressState: null, addressZipCode: null).Value;

    [Given(@"nao existe nenhuma filial cadastrada com o id (.*)")]
    public void GivenNaoExisteNenhumaFilialCadastradaComOId(long branchId)
        => _branchRepository
            .Setup(r => r.GetByIdAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

    [Given(@"a filial (.*) esta com o merchant Ifood resolvido com sucesso")]
    public void GivenAFilialEstaComOMerchantIfoodResolvidoComSucesso(long branchId)
    {
        var branch = CreateBranch();
        var setting = IfoodIntegrationSetting.Create(companyId: branch.CompanyId).Value;
        setting.SaveCredentials("client-1", "encrypted", enabled: true, ifoodCustomerId: null);
        var mapping = IfoodMerchantMapping.Create(branchId: branchId).Value;
        mapping.SetMerchant("MERCH-1", "uuid-1");

        _branchRepository.Setup(r => r.GetByIdAsync(branchId, It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _settingRepository.Setup(r => r.GetByCompanyAsync(branch.CompanyId, It.IsAny<CancellationToken>())).ReturnsAsync(setting);
        _mappingRepository.Setup(r => r.GetByBranchAsync(branchId, It.IsAny<CancellationToken>())).ReturnsAsync(mapping);
        _tokenProvider.Setup(t => t.GetAccessTokenAsync(branch.CompanyId, It.IsAny<CancellationToken>())).ReturnsAsync("token-1");
    }

    [Given(@"a exclusao de estoque em lote no Ifood falha com a mensagem ""(.*)""")]
    public void GivenAExclusaoDeEstoqueEmLoteNoIfoodFalhaComAMensagem(string errorMessage)
        => _catalogClient
            .Setup(c => c.DeleteInventoryBatchAsync("token-1", "MERCH-1", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodCatalogActionResult(false, errorMessage));

    [Given(@"a exclusao de estoque em lote no Ifood tem sucesso")]
    public void GivenAExclusaoDeEstoqueEmLoteNoIfoodTemSucesso()
        => _catalogClient
            .Setup(c => c.DeleteInventoryBatchAsync("token-1", "MERCH-1", It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IfoodCatalogActionResult(true, null));

    [When(@"eu tento excluir o estoque em lote dos produtos da filial (.*)")]
    public async Task WhenEuTentoExcluirOEstoqueEmLoteDosProdutosDaFilial(long branchId)
    {
        var handler = new DeleteIfoodInventoryBatchCommandHandler(
            _branchRepository.Object, _tokenProvider.Object, _settingRepository.Object,
            _mappingRepository.Object, _catalogClient.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new DeleteIfoodInventoryBatchCommand(branchId, [Guid.NewGuid()]), CancellationToken.None);
    }

    [Then(@"a operacao deve falhar com o erro ""(.*)""")]
    public void ThenAOperacaoDeveFalharComOErro(string errorCode)
    {
        _result!.IsFailure.Should().BeTrue();
        _result.Error.Code.Should().Be(errorCode);
    }

    [Then(@"a operacao deve ter sucesso")]
    public void ThenAOperacaoDeveTerSucesso()
        => _result!.IsSuccess.Should().BeTrue();
}
