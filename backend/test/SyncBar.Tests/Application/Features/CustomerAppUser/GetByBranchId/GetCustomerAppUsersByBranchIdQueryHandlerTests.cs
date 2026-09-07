using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAppUser.GetByBranchId;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainCustomerAppUser = SyncBar.Domain.Entities.CustomerAppUser;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.GetByBranchId;

public sealed class GetCustomerAppUsersByBranchIdQueryHandlerTests
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository = Substitute.For<ICustomerAppUserRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomerAppUsersByBranchIdQueryHandler _handler;

    public GetCustomerAppUsersByBranchIdQueryHandlerTests()
    {
        _handler = new GetCustomerAppUsersByBranchIdQueryHandler(_customerAppUserRepository, _logRepository, _unitOfWork);
    }

    private static DomainCustomerAppUser CreateUser(long companyId = 1, long? branchId = 2, long? customerId = 3)
        => DomainCustomerAppUser.Create(companyId, branchId, customerId, "joao123", "joao@bar.com", "hashed-password").Value;

    [Fact]
    public async Task Handle_InvalidBranchId_ShouldReturnFailure()
    {
        var query = new GetCustomerAppUsersByBranchIdQuery(0);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.InvalidBranchId");
        await _customerAppUserRepository.DidNotReceive().GetByBranchId(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoUsersForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetCustomerAppUsersByBranchIdQuery(2);
        _customerAppUserRepository.GetByBranchId(2, Arg.Any<CancellationToken>()).Returns(Array.Empty<DomainCustomerAppUser>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UsersFound_ShouldReturnOnlyActiveOnesMapped()
    {
        var activeUser = CreateUser(branchId: 2);
        var inactiveUser = CreateUser(branchId: 2);
        inactiveUser.Deactivate();
        var query = new GetCustomerAppUsersByBranchIdQuery(2);
        _customerAppUserRepository.GetByBranchId(2, Arg.Any<CancellationToken>())
            .Returns(new List<DomainCustomerAppUser> { activeUser, inactiveUser });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Should().ContainSingle().Subject;
        response.Id.Should().Be(activeUser.Id);
        response.BranchId.Should().Be(activeUser.BranchId);
        response.UserName.Should().Be(activeUser.UserName);
    }
}
