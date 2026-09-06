using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByInternalMerchantId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.GetByInternalMerchantId;

public sealed class GetKeetaMerchantMappingByInternalMerchantIdQueryHandlerTests
{
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaMerchantMappingByInternalMerchantIdQueryHandler _handler;

    public GetKeetaMerchantMappingByInternalMerchantIdQueryHandlerTests()
    {
        _handler = new GetKeetaMerchantMappingByInternalMerchantIdQueryHandler(_mappingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MappingNotFound_ShouldReturnNotFound()
    {
        _mappingRepository.GetByInternalMerchantIdAsync("im-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationMerchantMapping?)null);

        var result = await _handler.Handle(new GetKeetaMerchantMappingByInternalMerchantIdQuery("im-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaMerchantMapping.NotFound");
    }

    [Fact]
    public async Task Handle_MappingFound_ShouldReturnMappedResponse()
    {
        var mapping = KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value;
        _mappingRepository.GetByInternalMerchantIdAsync("im-1", Arg.Any<CancellationToken>()).Returns(mapping);

        var result = await _handler.Handle(new GetKeetaMerchantMappingByInternalMerchantIdQuery("im-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InternalMerchantId.Should().Be("im-1");
    }
}
