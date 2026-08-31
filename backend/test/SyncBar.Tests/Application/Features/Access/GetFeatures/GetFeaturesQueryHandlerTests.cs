using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Access;
using SyncBar.Application.Features.Access.GetFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Access.GetFeatures;

public sealed class GetFeaturesQueryHandlerTests
{
    private readonly IAppFeatureRepository _featureRepository = Substitute.For<IAppFeatureRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetFeaturesQueryHandler _handler;

    public GetFeaturesQueryHandlerTests()
    {
        _handler = new GetFeaturesQueryHandler(_featureRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoFeatures_ShouldReturnEmptyCollection()
    {
        _featureRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<AppFeature>());

        var result = await _handler.Handle(new GetFeaturesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFeatures_ShouldMapIdCodeAndName()
    {
        var feature1 = AppFeature.Create("orders.read", "Ver pedidos").Value;
        var feature2 = AppFeature.Create("orders.write", "Editar pedidos").Value;
        _featureRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([feature1, feature2]);

        var result = await _handler.Handle(new GetFeaturesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // AppFeature.Id não é controlável em teste (sem setter público), então comparo por
        // conteúdo sem exigir uma ordem específica em vez de depender do OrderBy(f => f.Id) do handler.
        result.Value.Should().BeEquivalentTo(
        [
            new FeatureResponse(feature1.Id, feature1.Code, feature1.Name),
            new FeatureResponse(feature2.Id, feature2.Code, feature2.Name)
        ]);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
