using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Access.SetJobTitleFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Access.SetJobTitleFeatures;

public sealed class SetJobTitleFeaturesCommandHandlerTests
{
    private readonly IJobTitleRepository _jobTitleRepository = Substitute.For<IJobTitleRepository>();
    private readonly IJobTitleFeatureRepository _jobTitleFeatureRepository = Substitute.For<IJobTitleFeatureRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetJobTitleFeaturesCommandHandler _handler;

    public SetJobTitleFeaturesCommandHandlerTests()
    {
        _handler = new SetJobTitleFeaturesCommandHandler(
            _jobTitleRepository, _jobTitleFeatureRepository, _logRepository, _unitOfWork);
    }

    private static JobTitle CreateActiveJobTitle(long companyId = 1, string name = "Garçom")
        => JobTitle.Create(companyId, name).Value;

    [Fact]
    public async Task Handle_JobTitleNotFound_ShouldReturnFailureWithoutTouchingLinks()
    {
        var command = new SetJobTitleFeaturesCommand(JobTitleId: 1, FeatureIds: [10, 20]);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns((JobTitle?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitle.NotFound");

        await _jobTitleFeatureRepository.DidNotReceive().GetByJobTitleForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        // Sem commit explícito nesse ramo: só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_JobTitleInactive_ShouldReturnFailure()
    {
        var jobTitle = CreateActiveJobTitle();
        jobTitle.Deactivate();
        var command = new SetJobTitleFeaturesCommand(JobTitleId: 1, FeatureIds: [10]);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns(jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitle.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeactivateReactivateAndCreateLinksAsNeeded()
    {
        var jobTitle = CreateActiveJobTitle();
        var command = new SetJobTitleFeaturesCommand(JobTitleId: 1, FeatureIds: [20, 30, 40]);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns(jobTitle);

        // 10: vinculado e ativo, mas não está mais na lista desejada -> deve ser desativado.
        var link10 = JobTitleFeature.Create(command.JobTitleId, 10).Value;
        // 20: vinculado e inativo, mas voltou a ser desejado -> deve ser reativado.
        var link20 = JobTitleFeature.Create(command.JobTitleId, 20).Value;
        link20.Deactivate();
        // 30: vinculado, ativo e continua desejado -> não deve ser alterado.
        var link30 = JobTitleFeature.Create(command.JobTitleId, 30).Value;

        _jobTitleFeatureRepository.GetByJobTitleForUpdateAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns([link10, link20, link30]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsSuccess.Should().BeTrue();

        link10.IsActive.Should().BeFalse();
        link20.IsActive.Should().BeTrue();
        link30.IsActive.Should().BeTrue();

        // 40 não existia -> deve ser criado e persistido.
        await _jobTitleFeatureRepository.Received(1).AddAsync(
            Arg.Is<JobTitleFeature>(l => l.JobTitleId == command.JobTitleId && l.AppFeatureId == 40),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewLinkCreationFails_ShouldReturnFailureWithoutPersisting()
    {
        var jobTitle = CreateActiveJobTitle();
        // FeatureId 0 é inválido para JobTitleFeature.Create (Ids devem ser > 0).
        var command = new SetJobTitleFeaturesCommand(JobTitleId: 1, FeatureIds: [0]);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns(jobTitle);
        _jobTitleFeatureRepository.GetByJobTitleForUpdateAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<JobTitleFeature>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitleFeature.InvalidIds");

        await _jobTitleFeatureRepository.DidNotReceive().AddAsync(Arg.Any<JobTitleFeature>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
