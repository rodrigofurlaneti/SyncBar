using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.Create;

public sealed class CreateKeetaIntegrationMerchantMappingCommandHandlerTests
{
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateKeetaIntegrationMerchantMappingCommandHandler _handler;

    public CreateKeetaIntegrationMerchantMappingCommandHandlerTests()
    {
        _handler = new CreateKeetaIntegrationMerchantMappingCommandHandler(_mappingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_KeetaMerchantIdAlreadyMapped_ShouldReturnConflict()
    {
        var command = new CreateKeetaIntegrationMerchantMappingCommand(1, 2, "im-1", 100, "Loja 1");
        _mappingRepository.ExistsByKeetaMerchantIdAsync(100, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaMerchantMapping.AlreadyExists");
        await _mappingRepository.DidNotReceive().AddAsync(Arg.Any<KeetaIntegrationMerchantMapping>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnMappedResponse()
    {
        var command = new CreateKeetaIntegrationMerchantMappingCommand(1, 2, "im-1", 100, "Loja 1");
        _mappingRepository.ExistsByKeetaMerchantIdAsync(100, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(1);
        result.Value.KeetaMerchantId.Should().Be(100);
        result.Value.StoreName.Should().Be("Loja 1");
        await _mappingRepository.Received(1).AddAsync(
            Arg.Is<KeetaIntegrationMerchantMapping>(m => m.KeetaMerchantId == 100 && m.InternalMerchantId == "im-1"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
