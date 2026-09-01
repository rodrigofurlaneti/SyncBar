using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Employees.GetJobTitles;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.GetJobTitles;

public sealed class GetJobTitlesQueryHandlerTests
{
    private readonly IJobTitleRepository _jobTitleRepository = Substitute.For<IJobTitleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetJobTitlesQueryHandler _handler;

    public GetJobTitlesQueryHandlerTests()
    {
        _handler = new GetJobTitlesQueryHandler(_jobTitleRepository, _logRepository, _unitOfWork);
    }

    private static JobTitle CreateJobTitle(string name, long companyId = 1)
        => JobTitle.Create(companyId, name).Value;

    [Fact]
    public async Task Handle_NoJobTitlesForCompany_ShouldReturnEmptyCollection()
    {
        var query = new GetJobTitlesQuery(CompanyId: 1);
        _jobTitleRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<JobTitle>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleJobTitles_ShouldOrderByNameAndMapFields()
    {
        var query = new GetJobTitlesQuery(CompanyId: 1);
        var jobTitleGarcom = CreateJobTitle("Garçom");
        var jobTitleCaixa = CreateJobTitle("Caixa");
        _jobTitleRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([jobTitleGarcom, jobTitleCaixa]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.Name).Should().ContainInOrder("Caixa", "Garçom");

        var firstResponse = result.Value.First();
        firstResponse.Id.Should().Be(jobTitleCaixa.Id);
        firstResponse.Name.Should().Be(jobTitleCaixa.Name);
    }
}
