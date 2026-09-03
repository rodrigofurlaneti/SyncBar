using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Application.Features.Catalog.Pizza.CreatePizzaFlavor;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

// Nivel de handler (Application) — ver MarkIfoodOrderReadySteps.cs para o padrao completo.
[Binding]
[Scope(Feature = "Criar sabor de pizza")]
public sealed class CreatePizzaFlavorCommandSteps
{
    private readonly Mock<IPizzaFlavorRepository> _pizzaFlavorRepository = new();
    private readonly Mock<ICurrentTenantService> _currentTenant = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Result<long>? _result;

    [Given(@"o usuario autenticado pertence a empresa (.*)")]
    public void GivenOUsuarioAutenticadoPertenceAEmpresa(long companyId)
        => _currentTenant.Setup(t => t.CompanyId).Returns(companyId);

    [Given(@"nao ha usuario autenticado")]
    public void GivenNaoHaUsuarioAutenticado()
        => _currentTenant.Setup(t => t.CompanyId).Returns((long?)null);

    [When(@"eu tento criar o sabor de pizza ""(.*)"" para a empresa (.*)")]
    public async Task WhenEuTentoCriarOSaborDePizzaParaAEmpresa(string name, long companyId)
    {
        var handler = new CreatePizzaFlavorCommandHandler(
            _pizzaFlavorRepository.Object, _currentTenant.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(
            new CreatePizzaFlavorCommand(companyId, name, "Descricao qualquer"), CancellationToken.None);
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
