using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaCrust;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

// Nivel de handler (Application) — ver MarkIfoodOrderReadySteps.cs para o padrao completo.
// PizzaCrust, apesar do nome em ingles, representa a "borda" da pizza (ex.: Borda Fina, Borda
// Grossa) — ver comentario da entidade PizzaCrust; nao confundir com PizzaEdge (o recheio da
// borda, ex.: Catupiry), coberto em AddPizzaEdgeCommandSteps.cs.
[Binding]
[Scope(Feature = "Adicionar borda a uma configuracao de pizza")]
public sealed class AddPizzaCrustCommandSteps
{
    private readonly Mock<IPizzaConfigurationRepository> _pizzaConfigurationRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private PizzaConfiguration? _configuration;
    private Result<long>? _result;

    [Given(@"nao existe nenhuma configuracao de pizza com o id (.*)")]
    public void GivenNaoExisteNenhumaConfiguracaoDePizzaComOId(long id)
        => _pizzaConfigurationRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PizzaConfiguration?)null);

    [Given(@"uma configuracao de pizza ativa com id (.*) para o produto (.*)")]
    public void GivenUmaConfiguracaoDePizzaAtivaComIdParaOProduto(long id, long productId)
    {
        _configuration = PizzaConfiguration.Create(productId).Value;

        _pizzaConfigurationRepository
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configuration);

        _productRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Product.Create(1, 1, 1, "Pizza Grande", null, null, 0m, null, false, null).Value);
    }

    [Given(@"a configuracao de pizza (.*) esta inativa")]
    public void GivenAConfiguracaoDePizzaEstaInativa(long id)
        => _configuration!.Deactivate();

    [Given(@"o produto (.*) da configuracao de pizza nao existe mais")]
    public void GivenOProdutoDaConfiguracaoDePizzaNaoExisteMais(long productId)
        => _productRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [When(@"eu tento adicionar a borda ""(.*)"" com preco extra (.*) na configuracao de pizza (.*)")]
    public async Task WhenEuTentoAdicionarABordaComPrecoExtraNaConfiguracaoDePizza(string name, decimal extraPrice, long configurationId)
    {
        var handler = new AddPizzaCrustCommandHandler(
            _pizzaConfigurationRepository.Object, _productRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new AddPizzaCrustCommand(configurationId, name, extraPrice, 1), CancellationToken.None);
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
