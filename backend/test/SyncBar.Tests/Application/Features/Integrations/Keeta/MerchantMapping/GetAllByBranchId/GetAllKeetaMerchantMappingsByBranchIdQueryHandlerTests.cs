using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByBranchId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByBranchId;

public sealed class GetAllKeetaMerchantMappingsByBranchIdQueryHandlerTests
{
    private readonly IKeetaIntegrationMerchantMappingRepository _mappingRepository = Substitute.For<IKeetaIntegrationMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllKeetaMerchantMappingsByBranchIdQueryHandler _handler;

    public GetAllKeetaMerchantMappingsByBranchIdQueryHandlerTests()
    {
        _handler = new GetAllKeetaMerchantMappingsByBranchIdQueryHandler(_mappingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_MappingsExist_ShouldReturnMappedList()
    {
        var mappings = new List<KeetaIntegrationMerchantMapping>
        {
            KeetaIntegrationMerchantMapping.Create(1, 2, "im-1", 100, "Loja 1").Value,
        };
        _mappingRepository.GetAllByBranchIdAsync(2, Arg.Any<CancellationToken>()).Returns(mappings);

        var result = await _handler.Handle(new GetAllKeetaMerchantMappingsByBranchIdQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoMappingsForBranch_ShouldReturnEmptyList()
    {
        _mappingRepository.GetAllByBranchIdAsync(2, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetAllKeetaMerchantMappingsByBranchIdQuery(2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
