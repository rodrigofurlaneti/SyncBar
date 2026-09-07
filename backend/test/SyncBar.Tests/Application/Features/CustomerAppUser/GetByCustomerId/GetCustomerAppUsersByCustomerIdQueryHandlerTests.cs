using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAppUser.GetByCustomerId;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainCustomerAppUser = SyncBar.Domain.Entities.CustomerAppUser;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.GetByCustomerId;

public sealed class GetCustomerAppUsersByCustomerIdQueryHandlerTests
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository = Substitute.For<ICustomerAppUserRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomerAppUsersByCustomerIdQueryHandler _handler;

    public GetCustomerAppUsersByCustomerIdQueryHandlerTests()
    {
        _handler = new GetCustomerAppUsersByCustomerIdQueryHandler(_customerAppUserRepository, _logRepository, _unitOfWork);
    }

    private static DomainCustomerAppUser CreateUser(long companyId = 1, long? branchId = 2, long? customerId = 3)
        => DomainCustomerAppUser.Create(companyId, branchId, customerId, "joao123", "joao@bar.com", "hashed-password").Value;

    [Fact]
    public async Task Handle_InvalidCustomerId_ShouldReturnFailure()
    {
        var query = new GetCustomerAppUsersByCustomerIdQuery(0);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.InvalidCustomerId");
        await _customerAppUserRepository.DidNotReceive().GetByCustomerId(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoUsersForCustomer_ShouldReturnEmptyCollection()
    {
        var query = new GetCustomerAppUsersByCustomerIdQuery(3);
        _customerAppUserRepository.GetByCustomerId(3, Arg.Any<CancellationToken>()).Returns(Array.Empty<DomainCustomerAppUser>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UsersFound_ShouldReturnOnlyActiveOnesMapped()
    {
        var activeUser = CreateUser(customerId: 3);
        var inactiveUser = CreateUser(customerId: 3);
        inactiveUser.Deactivate();
        var query = new GetCustomerAppUsersByCustomerIdQuery(3);
        _customerAppUserRepository.GetByCustomerId(3, Arg.Any<CancellationToken>())
            .Returns(new List<DomainCustomerAppUser> { activeUser, inactiveUser });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Should().ContainSingle().Subject;
        response.Id.Should().Be(activeUser.Id);
        response.CustomerId.Should().Be(activeUser.CustomerId);
        response.UserName.Should().Be(activeUser.UserName);
    }
}
