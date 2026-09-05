using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Setting.Create;

public sealed class CreateAsaasIntegrationSettingCommandHandlerTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateAsaasIntegrationSettingCommandHandler _handler;

    public CreateAsaasIntegrationSettingCommandHandlerTests()
    {
        _handler = new CreateAsaasIntegrationSettingCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ScopeAlreadyExistsForBranch_ShouldReturnConflict()
    {
        var command = new CreateAsaasIntegrationSettingCommand(1, 2, "api-key", null, "Sandbox", true);
        _settingRepository.GetByScopeAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationSetting.Create(1, 2, "existing-key").Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.AlreadyExists");
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<AsaasIntegrationSetting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ScopeAlreadyExistsForCompany_ShouldReturnConflict()
    {
        var command = new CreateAsaasIntegrationSettingCommand(1, null, "api-key", null, "Sandbox", true);
        _settingRepository.GetByScopeAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(AsaasIntegrationSetting.Create(1, null, "existing-key").Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasSetting.AlreadyExists");
    }

    [Fact]
    public async Task Handle_EmptyApiKey_ShouldReturnDomainValidationFailure()
    {
        var command = new CreateAsaasIntegrationSettingCommand(1, null, "  ", null, "Sandbox", true);
        _settingRepository.GetByScopeAsync(1, null, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ApiKey.Empty");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnMappedResponse()
    {
        var command = new CreateAsaasIntegrationSettingCommand(1, 2, "api-key", "webhook-token", "Production", true);
        _settingRepository.GetByScopeAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().Be(2);
        result.Value.Environment.Should().Be("Production");
        result.Value.IsActive.Should().BeTrue();
        await _settingRepository.Received(1).AddAsync(
            Arg.Is<AsaasIntegrationSetting>(s => s.CompanyId == 1 && s.BranchId == 2 && s.Environment == "Production"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoEnvironmentProvided_ShouldDefaultToSandbox()
    {
        var command = new CreateAsaasIntegrationSettingCommand(1, null, "api-key", null, null, true);
        _settingRepository.GetByScopeAsync(1, null, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Environment.Should().Be("SANDBOX");
    }
}
