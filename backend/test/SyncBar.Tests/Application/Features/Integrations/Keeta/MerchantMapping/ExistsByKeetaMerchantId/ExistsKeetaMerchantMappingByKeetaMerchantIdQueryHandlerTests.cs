using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.ExistsByKeetaMerchantId;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.ExistsByKeetaMerchantId;

public sealed class ExistsKeetaMerchantMappingByKeetaMerchantIdQueryHandlerTests
{
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsKeetaMerchantMappingByKeetaMerchantIdQueryHandler _handler;

    public ExistsKeetaMerchantMappingByKeetaMerchantIdQueryHandlerTests()
    {
        _handler = new ExistsKeetaMerchantMappingByKeetaMerchantIdQueryHandler(_mappingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MappingExists_ShouldReturnTrue()
    {
        _mappingRepository.ExistsByKeetaMerchantIdAsync(100, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsKeetaMerchantMappingByKeetaMerchantIdQuery(100), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MappingDoesNotExist_ShouldReturnFalse()
    {
        _mappingRepository.ExistsByKeetaMerchantIdAsync(100, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsKeetaMerchantMappingByKeetaMerchantIdQuery(100), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
