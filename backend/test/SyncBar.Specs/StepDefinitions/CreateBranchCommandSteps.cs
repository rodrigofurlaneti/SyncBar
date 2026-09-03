using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.Branches.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Criar filial")]
public sealed class CreateBranchCommandSteps
{
    private readonly Mock<IBranchRepository> _branchRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateBranchCommand? _command;
    private Result<long>? _result;

    [When(@"eu crio uma filial para a empresa (.*) com o nome ""(.*)""")]
    public async Task WhenEuCrioUmaFilialParaAEmpresaComONome(long companyId, string name)
    {
        _command = new CreateBranchCommand(
            companyId, name, "12345678000199", "1122223333",
            "Rua das Flores", "100", "Centro", "Sao Paulo", "SP", "01000000");

        var handler = new CreateBranchCommandHandler(_branchRepository.Object, _logRepository.Object, _unitOfWork.Object);
        _result = await handler.Handle(_command, CancellationToken.None);
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

    [Then(@"a filial deve ser persistida com os dados informados")]
    public void ThenAFilialDeveSerPersistidaComOsDadosInformados()
        => _branchRepository.Verify(r => r.AddAsync(
            It.Is<Branch>(b =>
                b.CompanyId == _command!.CompanyId &&
                b.Name == _command.Name &&
                b.Cnpj == _command.Cnpj &&
                b.Phone == _command.Phone &&
                b.AddressStreet == _command.AddressStreet &&
                b.AddressNumber == _command.AddressNumber &&
                b.AddressDistrict == _command.AddressDistrict &&
                b.AddressCity == _command.AddressCity &&
                b.AddressState == _command.AddressState &&
                b.AddressZipCode == _command.AddressZipCode),
            It.IsAny<CancellationToken>()), Times.Once);

    [Then(@"nenhuma filial deve ser persistida")]
    public void ThenNenhumaFilialDeveSerPersistida()
        => _branchRepository.Verify(r => r.AddAsync(It.IsAny<Branch>(), It.IsAny<CancellationToken>()), Times.Never);
}
