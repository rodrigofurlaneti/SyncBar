using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.Update;

public sealed class UpdateKeetaIntegrationMerchantMappingCommandHandlerTests
{
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateKeetaIntegrationMerchantMappingCommandHandler _handler;

    public UpdateKeetaIntegrationMerchantMappingCommandHandlerTests()
    {
        _handler = new UpdateKeetaIntegrationMerchantMappingCommandHandler(_mappingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MappingNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateKeetaIntegrationMerchantMappingCommand(1, 1, IsAuthorized: false);
        _mappingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationMerchantMapping?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaMerchantMapping.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value;
        var command = new UpdateKeetaIntegrationMerchantMappingCommand(1, 2, IsAuthorized: false);
        _mappingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaMerchantMapping.NotFound");
    }

    [Fact]
    public async Task Handle_UpdateStatus_ShouldApplyAndCommit()
    {
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value;
        var command = new UpdateKeetaIntegrationMerchantMappingCommand(1, 1, IsAuthorized: false, IsOnboarded: true);
        _mappingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mapping.IsAuthorized.Should().BeFalse();
        mapping.IsOnboarded.Should().BeTrue();
        _mappingRepository.Received(1).Update(mapping);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateUrls_ShouldApplyUrls()
    {
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value;
        var command = new UpdateKeetaIntegrationMerchantMappingCommand(1, 1, MenuBaseUrl: "https://menu", WebhookUrl: "https://webhook");
        _mappingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mapping.MenuBaseUrl.Should().Be("https://menu");
        mapping.WebhookUrl.Should().Be("https://webhook");
    }

    [Fact]
    public async Task Handle_RegisterMenuSync_ShouldSetLastMenuSyncAtUtc()
    {
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value;
        var command = new UpdateKeetaIntegrationMerchantMappingCommand(1, 1, RegisterMenuSync: true);
        _mappingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mapping.LastMenuSyncAtUtc.Should().NotBeNull();
    }
}
