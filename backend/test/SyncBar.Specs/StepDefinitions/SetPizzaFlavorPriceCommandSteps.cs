using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Pizza.SetPizzaFlavorPrice;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

// Nivel de handler (Application) — ver MarkIfoodOrderReadySteps.cs para o padrao completo.
// Maior handler do lote Catalog/Pizza: upsert de preco (ver PizzaConfiguration.SetFlavorPrice) —
// a existencia da linha de preco e o que torna o sabor vendavel naquele tamanho, e e a UNICA
// operacao entre AddSize/AddCrust/AddEdge/SetFlavorPrice que dispara TriggerCompanySync.
// Observacao (nao testada aqui por estar fora do handler): o preco final de uma pizza fracionada
// (varios sabores) usa o MAIOR preco entre os sabores escolhidos — ver
// PizzaConfiguration.CalculateUnitPrice/FindMaxFlavorPrice, usado por AddPizzaOrderItem, fora do
// escopo deste comando.
[Binding]
[Scope(Feature = "Definir preco de um sabor de pizza num tamanho")]
public sealed class SetPizzaFlavorPriceCommandSteps
{
    private const long CompanyId = 1;

    private readonly Mock<IPizzaConfigurationRepository> _pizzaConfigurationRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IPizzaFlavorRepository> _pizzaFlavorRepository = new();
    private readonly Mock<IIfoodCatalogSyncTrigger> _catalogSyncTrigger = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private PizzaConfiguration? _configuration;
    private long _pizzaSizeId;
    private Result<long>? _result;

    [Given(@"nao existe nenhuma configuracao de pizza com o id (.*)")]
    public void GivenNaoExisteNenhumaConfiguracaoDePizzaComOId(long id)
        => _pizzaConfigurationRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PizzaConfiguration?)null);

    [Given(@"uma configuracao de pizza ativa com id (.*) para o produto (.*), com um tamanho ""(.*)""")]
    public void GivenUmaConfiguracaoDePizzaAtivaComIdParaOProdutoComUmTamanho(long id, long productId, string sizeName)
    {
        _configuration = PizzaConfiguration.Create(productId).Value;
        var size = _configuration.AddSize(sizeName, null, 4, 1).Value;
        _pizzaSizeId = size.Id;

        _pizzaConfigurationRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configuration);

        var product = Product.Create(CompanyId, 1, 1, "Pizza Grande", null, null, 0m, null, false, null).Value;
        _productRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
    }

    [Given(@"a configuracao de pizza (.*) esta inativa")]
    public void GivenAConfiguracaoDePizzaEstaInativa(long id)
        => _configuration!.Deactivate();

    [Given(@"o produto (.*) da configuracao de pizza nao existe mais")]
    public void GivenOProdutoDaConfiguracaoDePizzaNaoExisteMais(long productId)
        => _productRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [Given(@"nao existe nenhum sabor de pizza com o id (.*)")]
    public void GivenNaoExisteNenhumSaborDePizzaComOId(long flavorId)
        => _pizzaFlavorRepository
            .Setup(r => r.GetByIdAsync(flavorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PizzaFlavor?)null);

    [Given(@"um sabor de pizza (.*) da empresa (.*)")]
    public void GivenUmSaborDePizzaDaEmpresa(long flavorId, long companyId)
        => _pizzaFlavorRepository
            .Setup(r => r.GetByIdAsync(flavorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PizzaFlavor.Create(companyId, "Calabresa", null).Value);

    [Given(@"a configuracao de pizza (.*) ja tem um preco de (.*) para o sabor (.*) no tamanho cadastrado")]
    public void GivenAConfiguracaoDePizzaJaTemUmPrecoParaOSaborNoTamanhoCadastrado(long id, decimal price, long flavorId)
        => _configuration!.SetFlavorPrice(flavorId, _pizzaSizeId, price);

    [When(@"eu tento definir o preco (.*) do sabor (.*) para o tamanho cadastrado na configuracao de pizza (.*)")]
    public Task WhenEuTentoDefinirOPrecoDoSaborParaOTamanhoCadastradoNaConfiguracaoDePizza(decimal price, long flavorId, long configurationId)
        => ExecuteAsync(configurationId, flavorId, _pizzaSizeId, price);

    [When(@"eu tento definir o preco (.*) do sabor (.*) para o tamanho (.*) na configuracao de pizza (.*)")]
    public Task WhenEuTentoDefinirOPrecoDoSaborParaOTamanhoNaConfiguracaoDePizza(decimal price, long flavorId, long sizeId, long configurationId)
        => ExecuteAsync(configurationId, flavorId, sizeId, price);

    private async Task ExecuteAsync(long configurationId, long flavorId, long sizeId, decimal price)
    {
        var handler = new SetPizzaFlavorPriceCommandHandler(
            _pizzaConfigurationRepository.Object, _productRepository.Object, _pizzaFlavorRepository.Object,
            _catalogSyncTrigger.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new SetPizzaFlavorPriceCommand(configurationId, flavorId, sizeId, price), CancellationToken.None);
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

    [Then(@"o preco do sabor no tamanho cadastrado deve ser (.*)")]
    public void ThenOPrecoDoSaborNoTamanhoCadastradoDeveSer(decimal expectedPrice)
        => _configuration!.FlavorPrices.Single(p => p.PizzaSizeId == _pizzaSizeId).Price.Should().Be(expectedPrice);

    [Then(@"a sincronizacao do catalogo da empresa deve ser disparada")]
    public void ThenASincronizacaoDoCatalogoDaEmpresaDeveSerDisparada()
        => _catalogSyncTrigger.Verify(t => t.TriggerCompanySync(CompanyId), Times.Once);
}
