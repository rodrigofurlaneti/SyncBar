using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Customers.AddLoyaltyPoints;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Customers.AddLoyaltyPoints;

public sealed class AddLoyaltyPointsCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddLoyaltyPointsCommandHandler _handler;

    public AddLoyaltyPointsCommandHandlerTests()
    {
        _handler = new AddLoyaltyPointsCommandHandler(_customerRepository, _logRepository, _unitOfWork);
    }

    private static Customer CreateActiveCustomer()
        => Customer.Create(companyId: 1, name: "Cliente Teste", phone: "11999990000", cpf: "12345678900", email: "cliente@teste.com").Value;

    [Fact]
    public async Task Handle_CustomerNotFound_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var command = new AddLoyaltyPointsCommand(CustomerId: 1, Points: 10);
        _customerRepository.GetByIdForUpdateAsync(command.CustomerId, Arg.Any<CancellationToken>())
            .Returns((Customer?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerInactive_ShouldReturnFailure()
    {
        var customer = CreateActiveCustomer();
        customer.Deactivate();
        var command = new AddLoyaltyPointsCommand(CustomerId: 1, Points: 10);
        _customerRepository.GetByIdForUpdateAsync(command.CustomerId, Arg.Any<CancellationToken>())
            .Returns(customer);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidPoints_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var customer = CreateActiveCustomer();
        var command = new AddLoyaltyPointsCommand(CustomerId: 1, Points: 0);
        _customerRepository.GetByIdForUpdateAsync(command.CustomerId, Arg.Any<CancellationToken>())
            .Returns(customer);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.InvalidPoints");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldIncreaseLoyaltyPointsAndCommit()
    {
        var customer = CreateActiveCustomer();
        var initialPoints = customer.LoyaltyPoints;
        var command = new AddLoyaltyPointsCommand(CustomerId: 1, Points: 25);
        _customerRepository.GetByIdForUpdateAsync(command.CustomerId, Arg.Any<CancellationToken>())
            .Returns(customer);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        customer.LoyaltyPoints.Should().Be(initialPoints + command.Points);
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
