using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAppUser.GetById;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainCustomerAppUser = SyncBar.Domain.Entities.CustomerAppUser;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.GetById;

public sealed class GetCustomerAppUserByIdQueryHandlerTests
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository = Substitute.For<ICustomerAppUserRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomerAppUserByIdQueryHandler _handler;

    public GetCustomerAppUserByIdQueryHandlerTests()
    {
        _handler = new GetCustomerAppUserByIdQueryHandler(_customerAppUserRepository, _logRepository, _unitOfWork);
    }

    private static DomainCustomerAppUser CreateUser(long companyId = 1, long? branchId = 2, long? customerId = 3)
        => DomainCustomerAppUser.Create(companyId, branchId, customerId, "joao123", "joao@bar.com", "hashed-password").Value;

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        var query = new GetCustomerAppUserByIdQuery(1);
        _customerAppUserRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((DomainCustomerAppUser?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.NotFound");
    }

    [Fact]
    public async Task Handle_UserInactive_ShouldReturnFailure()
    {
        var user = CreateUser();
        user.Deactivate();
        var query = new GetCustomerAppUserByIdQuery(1);
        _customerAppUserRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.NotFound");
    }

    [Fact]
    public async Task Handle_UserFoundAndActive_ShouldReturnMappedResponse()
    {
        var user = CreateUser();
        var query = new GetCustomerAppUserByIdQuery(1);
        _customerAppUserRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.CompanyId.Should().Be(user.CompanyId);
        result.Value.BranchId.Should().Be(user.BranchId);
        result.Value.CustomerId.Should().Be(user.CustomerId);
        result.Value.UserName.Should().Be(user.UserName);
        result.Value.Email.Should().Be(user.Email);
        result.Value.IsActive.Should().BeTrue();
    }
}
