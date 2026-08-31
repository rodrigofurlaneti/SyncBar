using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Access.GetJobTitleFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Access.GetJobTitleFeatures;

public sealed class GetJobTitleFeaturesQueryHandlerTests
{
    private readonly IJobTitleFeatureRepository _jobTitleFeatureRepository = Substitute.For<IJobTitleFeatureRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetJobTitleFeaturesQueryHandler _handler;

    public GetJobTitleFeaturesQueryHandlerTests()
    {
        _handler = new GetJobTitleFeaturesQueryHandler(_jobTitleFeatureRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoLinksForJobTitle_ShouldReturnEmptyCollection()
    {
        var query = new GetJobTitleFeaturesQuery(JobTitleId: 1);
        _jobTitleFeatureRepository.GetByJobTitleAsync(query.JobTitleId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<JobTitleFeature>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithLinks_ShouldReturnAppFeatureIdsInRepositoryOrder()
    {
        var query = new GetJobTitleFeaturesQuery(JobTitleId: 7);
        var link1 = JobTitleFeature.Create(query.JobTitleId, appFeatureId: 10).Value;
        var link2 = JobTitleFeature.Create(query.JobTitleId, appFeatureId: 20).Value;
        // O handler não filtra por IsActive — ele confia que o repositório já devolve o conjunto certo.
        // Incluo um vínculo desativado aqui só para deixar esse comportamento explícito e coberto.
        var inactiveLink = JobTitleFeature.Create(query.JobTitleId, appFeatureId: 30).Value;
        inactiveLink.Deactivate();

        _jobTitleFeatureRepository.GetByJobTitleAsync(query.JobTitleId, Arg.Any<CancellationToken>())
            .Returns([link1, link2, inactiveLink]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(10L, 20L, 30L);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
