using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAppUser.GetByCompanyId;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainCustomerAppUser = SyncBar.Domain.Entities.CustomerAppUser;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.GetByCompanyId;

public sealed class GetCustomerAppUsersByCompanyIdQueryHandlerTests
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository = Substitute.For<ICustomerAppUserRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomerAppUsersByCompanyIdQueryHandler _handler;

    public GetCustomerAppUsersByCompanyIdQueryHandlerTests()
    {
        _handler = new GetCustomerAppUsersByCompanyIdQueryHandler(_customerAppUserRepository, _logRepository, _unitOfWork);
    }

    private static DomainCustomerAppUser CreateUser(long companyId = 1, long? branchId = 2, long? customerId = 3)
        => DomainCustomerAppUser.Create(companyId, branchId, customerId, "joao123", "joao@bar.com", "hashed-password").Value;

    [Fact]
    public async Task Handle_InvalidCompanyId_ShouldReturnFailure()
    {
        var query = new GetCustomerAppUsersByCompanyIdQuery(0);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.InvalidCompanyId");
        await _customerAppUserRepository.DidNotReceive().GetByCompanyId(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoUsersForCompany_ShouldReturnEmptyCollection()
    {
        var query = new GetCustomerAppUsersByCompanyIdQuery(1);
        _customerAppUserRepository.GetByCompanyId(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<DomainCustomerAppUser>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UsersFound_ShouldReturnOnlyActiveOnesMapped()
    {
        var activeUser = CreateUser(companyId: 1);
        var inactiveUser = CreateUser(companyId: 1);
        inactiveUser.Deactivate();
        var query = new GetCustomerAppUsersByCompanyIdQuery(1);
        _customerAppUserRepository.GetByCompanyId(1, Arg.Any<CancellationToken>())
            .Returns(new List<DomainCustomerAppUser> { activeUser, inactiveUser });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Should().ContainSingle().Subject;
        response.Id.Should().Be(activeUser.Id);
        response.CompanyId.Should().Be(activeUser.CompanyId);
        response.UserName.Should().Be(activeUser.UserName);
    }
}
