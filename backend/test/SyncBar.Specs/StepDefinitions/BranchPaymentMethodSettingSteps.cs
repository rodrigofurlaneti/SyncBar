using FluentAssertions;
using Moq;
using Reqnroll;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Create;
using SyncBar.Application.Features.BranchPaymentMethodSetting.Delete;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetAllActive;
using SyncBar.Application.Features.BranchPaymentMethodSetting.GetByBranchOrCompanyFallback;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SettingEntity = SyncBar.Domain.Entities.BranchPaymentMethodSetting;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
[Scope(Feature = "Configuracao de metodos de pagamento por filial/empresa")]
public sealed class BranchPaymentMethodSettingSteps
{
    private readonly Mock<IBranchPaymentMethodSettingRepository> _settingRepository = new();
    private readonly Mock<ILogTrackerRepository> _logRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Dictionary<string, long> _companyIds = [];
    private readonly Dictionary<string, long> _branchIds = [];
    private long _nextId = 1;

    private long _currentCompanyId;
    private SettingEntity? _currentSetting;

    private Result<CreateBranchPaymentMethodSettingResponse>? _createResult;
    private Result? _deleteResult;
    private Result<BranchPaymentMethodSettingResponse?>? _fallbackResult;

    private long IdFor(Dictionary<string, long> map, string name)
    {
        if (!map.TryGetValue(name, out var id))
        {
            id = _nextId++;
            map[name] = id;
        }
        return id;
    }

    private static bool GetFlag(CreateBranchPaymentMethodSettingResponse response, string method) => method switch
    {
        "Pix" => response.EnablePix,
        "Boleto" => response.EnableBoleto,
        "Cartao de Credito" => response.EnableCreditCard,
        "Cartao de Debito" => response.EnableDebitCard,
        "Maquininha" => response.EnableCashMachine,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Metodo de pagamento desconhecido no step."),
    };

    // --- Cenario 1: criacao de configuracao para uma filial -----------------------------------

    [Given(@"que existe uma empresa ""(.*)"" com a filial ""(.*)""")]
    public void GivenQueExisteUmaEmpresaComAFilial(string companyName, string branchName)
    {
        _currentCompanyId = IdFor(_companyIds, companyName);
        IdFor(_branchIds, branchName);
    }

    [Given(@"nao existe configuracao de pagamento para a filial ""(.*)""")]
    public void GivenNaoExisteConfiguracaoDePagamentoParaAFilial(string branchName)
    {
        var branchId = IdFor(_branchIds, branchName);
        _settingRepository
            .Setup(r => r.GetByScopeAsync(_currentCompanyId, branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettingEntity?)null);
    }

    [When(@"eu envio uma solicitacao para habilitar apenas ""(.*)"" e ""(.*)"" para a filial ""(.*)""")]
    public async Task WhenEuEnvioUmaSolicitacaoParaHabilitarApenasEParaAFilial(string method1, string method2, string branchName)
    {
        var branchId = IdFor(_branchIds, branchName);
        var enabled = new HashSet<string> { method1, method2 };

        var command = new CreateBranchPaymentMethodSettingCommand(
            _currentCompanyId,
            branchId,
            EnablePix: enabled.Contains("Pix"),
            EnableBoleto: enabled.Contains("Boleto"),
            EnableCreditCard: enabled.Contains("Cartao de Credito"),
            EnableDebitCard: enabled.Contains("Cartao de Debito"),
            EnableCashMachine: enabled.Contains("Maquininha"));

        var handler = new CreateBranchPaymentMethodSettingCommandHandler(
            _settingRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _createResult = await handler.Handle(command, CancellationToken.None);
    }

    [Then(@"a configuracao deve ser salva com sucesso")]
    public void ThenAConfiguracaoDeveSerSalvaComSucesso()
        => _createResult!.IsSuccess.Should().BeTrue();

    [Then(@"os metodos ""(.*)"", ""(.*)"" e ""(.*)"" devem estar desabilitados")]
    public void ThenOsMetodosDevemEstarDesabilitados(string method1, string method2, string method3)
    {
        var response = _createResult!.Value;
        foreach (var method in new[] { method1, method2, method3 })
            GetFlag(response, method).Should().BeFalse($"o metodo {method} deveria estar desabilitado");
    }

    // --- Cenario 2: fallback da filial para a configuracao geral da empresa -------------------

    [Given(@"que existe uma configuracao geral para a empresa ""(.*)"" com ""(.*)"" habilitado")]
    public void GivenQueExisteUmaConfiguracaoGeralParaAEmpresaComHabilitado(string companyName, string method)
    {
        _currentCompanyId = IdFor(_companyIds, companyName);
        _currentSetting = SettingEntity.Create(
            _currentCompanyId,
            branchId: null,
            enablePix: method == "Pix").Value;
    }

    [Given(@"a filial ""(.*)"" nao possui configuracao propria")]
    public void GivenAFilialNaoPossuiConfiguracaoPropria(string branchName)
        => IdFor(_branchIds, branchName);

    [When(@"o sistema consulta os metodos de pagamento usando a regra de fallback para a filial ""(.*)""")]
    public async Task WhenOSistemaConsultaOsMetodosDePagamentoUsandoARegraDeFallbackParaAFilial(string branchName)
    {
        var branchId = IdFor(_branchIds, branchName);
        _settingRepository
            .Setup(r => r.GetByBranchOrCompanyFallbackAsync(_currentCompanyId, branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_currentSetting);

        var handler = new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandler(
            _settingRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _fallbackResult = await handler.Handle(
            new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery(_currentCompanyId, branchId), CancellationToken.None);
    }

    [Then(@"o sistema deve retornar a configuracao geral da empresa")]
    public void ThenOSistemaDeveRetornarAConfiguracaoGeralDaEmpresa()
    {
        _fallbackResult!.IsSuccess.Should().BeTrue();
        _fallbackResult.Value.Should().NotBeNull();
        _fallbackResult.Value!.CompanyId.Should().Be(_currentCompanyId);
        _fallbackResult.Value.BranchId.Should().BeNull();
        _fallbackResult.Value.EnablePix.Should().BeTrue();
    }

    // --- Cenario 3: exclusao de configuracao ---------------------------------------------------

    [Given(@"que existe uma configuracao de pagamento para a filial ""(.*)""")]
    public void GivenQueExisteUmaConfiguracaoDePagamentoParaAFilial(string branchName)
    {
        _currentCompanyId = IdFor(_companyIds, "empresa-dona-da-filial");
        var branchId = IdFor(_branchIds, branchName);
        _currentSetting = SettingEntity.Create(_currentCompanyId, branchId).Value;

        _settingRepository
            .Setup(r => r.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_currentSetting);
    }

    [When(@"o administrador solicita a exclusao desta configuracao")]
    public async Task WhenOAdministradorSolicitaAExclusaoDestaConfiguracao()
    {
        var handler = new DeleteBranchPaymentMethodSettingCommandHandler(
            _settingRepository.Object, _logRepository.Object, _unitOfWork.Object);

        _deleteResult = await handler.Handle(new DeleteBranchPaymentMethodSettingCommand(1, _currentCompanyId), CancellationToken.None);
    }

    [Then(@"a configuracao deve ser removida do banco de dados")]
    public void ThenAConfiguracaoDeveSerRemovidaDoBancoDeDados()
    {
        _deleteResult!.IsSuccess.Should().BeTrue();
        _settingRepository.Verify(r => r.Delete(_currentSetting!), Times.Once);
    }

    [Then(@"a filial passara a depender da configuracao geral da empresa, se houver")]
    public async Task ThenAFilialPassaraADependerDaConfiguracaoGeralDaEmpresaSeHouver()
    {
        // Apos a exclusao, uma nova consulta de fallback para a mesma filial nao encontra mais
        // configuracao propria — como nenhuma configuracao geral da empresa foi cadastrada neste
        // cenario, o fallback resolve para null (comportamento explicitamente coberto pelo
        // Cenario 2 quando uma configuracao geral realmente existe).
        var branchId = _branchIds.Values.Single();
        _settingRepository
            .Setup(r => r.GetByBranchOrCompanyFallbackAsync(_currentCompanyId, branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettingEntity?)null);

        var handler = new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQueryHandler(
            _settingRepository.Object, _logRepository.Object, _unitOfWork.Object);

        var result = await handler.Handle(
            new GetByBranchOrCompanyFallbackBranchPaymentMethodSettingQuery(_currentCompanyId, branchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
