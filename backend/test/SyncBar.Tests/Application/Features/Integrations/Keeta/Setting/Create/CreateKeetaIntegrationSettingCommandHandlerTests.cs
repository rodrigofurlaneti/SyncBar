using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.Create;

public sealed class CreateKeetaIntegrationSettingCommandHandlerTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateKeetaIntegrationSettingCommandHandler _handler;

    public CreateKeetaIntegrationSettingCommandHandlerTests()
    {
        _handler = new CreateKeetaIntegrationSettingCommandHandler(_settingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ScopeAlreadyExistsForBranch_ShouldReturnConflict()
    {
        var command = new CreateKeetaIntegrationSettingCommand(1, 2, "client-id", "client-secret", "app-id");
        _settingRepository.GetByScopeAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(KeetaIntegrationSetting.Create(1, 2).Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.AlreadyExists");
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<KeetaIntegrationSetting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ScopeAlreadyExistsForCompany_ShouldReturnConflict()
    {
        var command = new CreateKeetaIntegrationSettingCommand(1, 0, "client-id", "client-secret", "app-id");
        _settingRepository.GetByScopeAsync(1, 0, Arg.Any<CancellationToken>())
            .Returns(KeetaIntegrationSetting.Create(1, 0).Value);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaSetting.AlreadyExists");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnMappedResponse()
    {
        var command = new CreateKeetaIntegrationSettingCommand(1, 2, "client-id", "client-secret", "app-id", "https://custom.keeta/api");
        _settingRepository.GetByScopeAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
        result.Value.BranchId.Should().Be(2);
        result.Value.BaseUrl.Should().Be("https://custom.keeta/api");
        await _settingRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationSetting>(s => s.CompanyId == 1 && s.BranchId == 2 && s.ClientId == "client-id"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoBaseUrlProvided_ShouldUseDomainDefault()
    {
        var command = new CreateKeetaIntegrationSettingCommand(1, 0, "client-id", "client-secret", "app-id");
        _settingRepository.GetByScopeAsync(1, 0, Arg.Any<CancellationToken>())
            .Returns((KeetaIntegrationSetting?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BaseUrl.Should().Be("https://open.mykeeta.com/api/open/opendelivery");
    }
}
