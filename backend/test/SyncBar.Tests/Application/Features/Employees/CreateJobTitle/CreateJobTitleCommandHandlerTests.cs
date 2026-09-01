using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Employees.CreateJobTitle;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.CreateJobTitle;

public sealed class CreateJobTitleCommandHandlerTests
{
    private readonly IJobTitleRepository _jobTitleRepository = Substitute.For<IJobTitleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateJobTitleCommandHandler _handler;

    public CreateJobTitleCommandHandlerTests()
    {
        _handler = new CreateJobTitleCommandHandler(_jobTitleRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailureWithoutPersisting()
    {
        var command = new CreateJobTitleCommand(CompanyId: 1, Name: "");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitle.EmptyName");
        await _jobTitleRepository.DidNotReceive().AddAsync(Arg.Any<JobTitle>(), Arg.Any<CancellationToken>());
        // Sem persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistJobTitleAndReturnItsId()
    {
        var command = new CreateJobTitleCommand(CompanyId: 1, Name: "Garçom");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _jobTitleRepository.Received(1).AddAsync(
            Arg.Is<JobTitle>(j =>
                j.CompanyId == command.CompanyId &&
                j.Name == command.Name &&
                j.IsActive),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
