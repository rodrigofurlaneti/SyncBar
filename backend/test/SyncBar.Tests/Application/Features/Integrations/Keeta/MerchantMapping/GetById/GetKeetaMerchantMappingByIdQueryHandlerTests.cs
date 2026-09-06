using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.GetById;

public sealed class GetKeetaMerchantMappingByIdQueryHandlerTests
{
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaMerchantMappingByIdQueryHandler _handler;

    public GetKeetaMerchantMappingByIdQueryHandlerTests()
    {
        _handler = new GetKeetaMerchantMappingByIdQueryHandler(_mappingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MappingNotFound_ShouldReturnNotFound()
    {
        _mappingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationMerchantMapping?)null);

        var result = await _handler.Handle(new GetKeetaMerchantMappingByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaMerchantMapping.NotFound");
    }

    [Fact]
    public async Task Handle_MappingFound_ShouldReturnMappedResponse()
    {
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value;
        _mappingRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(new GetKeetaMerchantMappingByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.KeetaMerchantId.Should().Be(100);
        result.Value.StoreName.Should().Be("Loja 1");
    }
}
