using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Branches.SetSelfServiceEmployee;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class SetSelfServiceEmployeeCommandSteps
{
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Branch? _branch;
    private Result? _result;

    [Given(@"nao existe a filial (.*)")]
    public void GivenNaoExisteAFilial(long branchId)
        => _branchRepository
            .Setup(r => r.GetByIdForUpdateAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

    [Given(@"existe a filial (.*) ativa")]
    public void GivenExisteAFilialAtiva(long branchId)
    {
        _branch = Branch.Create(1, "Filial Centro", null, null, null, null, null, null, null, null).Value;
        _branchRepository
            .Setup(r => r.GetByIdForUpdateAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_branch);
    }

    [Given(@"existe a filial (.*) inativa")]
    public void GivenExisteAFilialInativa(long branchId)
    {
        _branch = Branch.Create(1, "Filial Centro", null, null, null, null, null, null, null, null).Value;
        _branch.Deactivate();
        _branchRepository
            .Setup(r => r.GetByIdForUpdateAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_branch);
    }

    [Given(@"nao existe o funcionario (.*)")]
    public void GivenNaoExisteOFuncionario(long employeeId)
        => _employeeRepository
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

    [Given(@"existe o funcionario (.*) ativo na filial (.*)")]
    public void GivenExisteOFuncionarioAtivoNaFilial(long employeeId, long branchId)
    {
        var employee = Employee.Create(branchId, 1, "Ana", "11122233344", null, null, DateTime.Now, null, null).Value;
        _employeeRepository
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
    }

    [Given(@"existe o funcionario (.*) inativo na filial (.*)")]
    public void GivenExisteOFuncionarioInativoNaFilial(long employeeId, long branchId)
    {
        var employee = Employee.Create(branchId, 1, "Ana", "11122233344", null, null, DateTime.Now, null, null).Value;
        employee.Deactivate();
        _employeeRepository
            .Setup(r => r.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
    }

    [When(@"eu defino o funcionario (.*) como atendente self-service da filial (.*)")]
    public async Task WhenEuDefinoOFuncionarioComoAtendenteSelfServiceDaFilial(long employeeId, long branchId)
    {
        var handler = new SetSelfServiceEmployeeCommandHandler(
            _branchRepository.Object, _employeeRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new SetSelfServiceEmployeeCommand(branchId, employeeId), CancellationToken.None);
    }

    [When(@"eu removo o atendente self-service da filial (.*)")]
    public async Task WhenEuRemovoOAtendenteSelfServiceDaFilial(long branchId)
    {
        var handler = new SetSelfServiceEmployeeCommandHandler(
            _branchRepository.Object, _employeeRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _result = await handler.Handle(new SetSelfServiceEmployeeCommand(branchId, null), CancellationToken.None);
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

    [Then(@"a filial deve ter o funcionario (.*) como atendente self-service")]
    public void ThenAFilialDeveTerOFuncionarioComoAtendenteSelfService(long employeeId)
        => _branch!.SelfServiceEmployeeId.Should().Be(employeeId);

    [Then(@"a filial nao deve ter atendente self-service")]
    public void ThenAFilialNaoDeveTerAtendenteSelfService()
        => _branch!.SelfServiceEmployeeId.Should().BeNull();

    [Then(@"o repositorio de funcionarios nao deve ser consultado")]
    public void ThenORepositorioDeFuncionariosNaoDeveSerConsultado()
        => _employeeRepository.Verify(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
}
