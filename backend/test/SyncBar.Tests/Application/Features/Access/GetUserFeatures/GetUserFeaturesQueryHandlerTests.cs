using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Access.GetUserFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Access.GetUserFeatures;

public sealed class GetUserFeaturesQueryHandlerTests
{
    private readonly IAppUserFeatureRepository _userFeatureRepository = Substitute.For<IAppUserFeatureRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetUserFeaturesQueryHandler _handler;

    public GetUserFeaturesQueryHandlerTests()
    {
        _handler = new GetUserFeaturesQueryHandler(_userFeatureRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoLinksForUser_ShouldReturnEmptyCollection()
    {
        var query = new GetUserFeaturesQuery(AppUserId: 1);
        _userFeatureRepository.GetByUserAsync(query.AppUserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AppUserFeature>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithLinks_ShouldReturnAppFeatureIdsInRepositoryOrder()
    {
        var query = new GetUserFeaturesQuery(AppUserId: 7);
        var link1 = AppUserFeature.Create(query.AppUserId, appFeatureId: 10).Value;
        var link2 = AppUserFeature.Create(query.AppUserId, appFeatureId: 20).Value;
        // O handler não filtra por IsActive — assim como em GetJobTitleFeatures, ele confia
        // que o repositório já devolve o conjunto certo. Um vínculo desativado é incluído aqui
        // de propósito para deixar esse comportamento explícito e coberto.
        var inactiveLink = AppUserFeature.Create(query.AppUserId, appFeatureId: 30).Value;
        inactiveLink.Deactivate();

        _userFeatureRepository.GetByUserAsync(query.AppUserId, Arg.Any<CancellationToken>())
            .Returns([link1, link2, inactiveLink]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(10L, 20L, 30L);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
