using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.Delete;

public sealed class DeleteKeetaIntegrationMerchantMappingCommandHandlerTests
{
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteKeetaIntegrationMerchantMappingCommandHandler _handler;

    public DeleteKeetaIntegrationMerchantMappingCommandHandlerTests()
    {
        _handler = new DeleteKeetaIntegrationMerchantMappingCommandHandler(_mappingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MappingNotFound_ShouldReturnNotFound()
    {
        var command = new DeleteKeetaIntegrationMerchantMappingCommand(1, 1);
        _mappingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationMerchantMapping?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaMerchantMapping.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value;
        var command = new DeleteKeetaIntegrationMerchantMappingCommand(1, 2);
        _mappingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaMerchantMapping.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteAndCommit()
    {
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value;
        var command = new DeleteKeetaIntegrationMerchantMappingCommand(1, 1);
        _mappingRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mappingRepository.Received(1).Delete(mapping);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
