using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Access.SetJobTitleFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class SetJobTitleFeaturesCommandHandlerTests
{
    private readonly IJobTitleRepository _jobTitleRepository = Substitute.For<IJobTitleRepository>();
    private readonly IJobTitleFeatureRepository _jobTitleFeatureRepository = Substitute.For<IJobTitleFeatureRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SetJobTitleFeaturesCommandHandler CreateHandler()
        => new(_jobTitleRepository, _jobTitleFeatureRepository, _unitOfWork);

    [Fact]
    public async Task Handle_ShouldDeactivateRemovedAndAddNewGrants()
    {
        _jobTitleRepository.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(JobTitle.Create(1, "Garçom").Value);

        var keep = JobTitleFeature.Create(2, 1).Value;    // Salao — mantem
        var remove = JobTitleFeature.Create(2, 2).Value;  // Cardapio — remove
        _jobTitleFeatureRepository.GetByJobTitleForUpdateAsync(2, Arg.Any<CancellationToken>())
            .Returns(new List<JobTitleFeature> { keep, remove });

        // Desejado: Salao (1) + Estoque (3)
        var result = await CreateHandler().Handle(
            new SetJobTitleFeaturesCommand(2, [1, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        keep.IsActive.Should().BeTrue();
        remove.IsActive.Should().BeFalse();
        await _jobTitleFeatureRepository.Received(1).AddAsync(
            Arg.Is<JobTitleFeature>(l => l.AppFeatureId == 3), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegrantingInactiveLink_ShouldReactivateInsteadOfDuplicating()
    {
        _jobTitleRepository.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(JobTitle.Create(1, "Garçom").Value);

        var inactive = JobTitleFeature.Create(2, 3).Value;
        inactive.Deactivate();
        _jobTitleFeatureRepository.GetByJobTitleForUpdateAsync(2, Arg.Any<CancellationToken>())
            .Returns(new List<JobTitleFeature> { inactive });

        var result = await CreateHandler().Handle(
            new SetJobTitleFeaturesCommand(2, [3]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        inactive.IsActive.Should().BeTrue();
        await _jobTitleFeatureRepository.DidNotReceive().AddAsync(Arg.Any<JobTitleFeature>(), Arg.Any<CancellationToken>());
    }
}
