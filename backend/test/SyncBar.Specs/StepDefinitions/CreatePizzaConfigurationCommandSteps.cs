using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Catalog.Pizza.CreatePizzaConfiguration;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

// Nivel de handler (Application) — ver MarkIfoodOrderReadySteps.cs para o padrao completo.
[Binding]
public sealed class CreatePizzaConfigurationCommandSteps
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IPizzaConfigurationRepository> _pizzaConfigurationRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<long>? _result;

    [Given(@"nao existe nenhum produto com o id (.*)")]
    public void GivenNaoExisteNenhumProdutoComOId(long id)
        => _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

    [Given(@"um produto ativo com id (.*)")]
    public void GivenUmProdutoAtivoComId(long id)
    {
        var product = Product.Create(1, 1, 1, "Pizza Grande", null, null, 0m, null, false, null).Value;
        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _pizzaConfigurationRepository
            .Setup(r => r.GetByProductIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PizzaConfiguration?)null);
    }

    [Given(@"o produto (.*) esta inativo")]
    public void GivenOProdutoEstaInativo(long id)
    {
        var product = Product.Create(1, 1, 1, "Pizza Grande", null, null, 0m, null, false, null).Value;
        product.Deactivate();

        _productRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
    }

    [Given(@"o produto (.*) ja tem uma configuracao de pizza com id (.*)")]
    public void GivenOProdutoJaTemUmaConfiguracaoDePizzaComId(long productId, long existingConfigurationId)
    {
        var existing = PizzaConfiguration.Create(productId).Value;
        _pizzaConfigurationRepository
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
    }

    [When(@"eu tento criar uma configuracao de pizza para o produto (.*)")]
    public async Task WhenEuTentoCriarUmaConfiguracaoDePizzaParaOProduto(long productId)
    {
        var handler = new CreatePizzaConfigurationCommandHandler(
            _productRepository.Object, _pizzaConfigurationRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new CreatePizzaConfigurationCommand(productId), CancellationToken.None);
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

    [Then(@"nenhuma nova configuracao de pizza deve ser criada")]
    public void ThenNenhumaNovaConfiguracaoDePizzaDeveSerCriada()
        => _pizzaConfigurationRepository.Verify(
            r => r.AddAsync(It.IsAny<PizzaConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);

    [Then(@"uma nova configuracao de pizza deve ser criada")]
    public void ThenUmaNovaConfiguracaoDePizzaDeveSerCriada()
        => _pizzaConfigurationRepository.Verify(
            r => r.AddAsync(It.IsAny<PizzaConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
}
