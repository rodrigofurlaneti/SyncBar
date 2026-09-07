using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.CustomerAppUser.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainCustomerAppUser = SyncBar.Domain.Entities.CustomerAppUser;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.Create;

public sealed class CreateCustomerAppUserCommandHandlerTests
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository = Substitute.For<ICustomerAppUserRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateCustomerAppUserCommandHandler _handler;

    public CreateCustomerAppUserCommandHandlerTests()
    {
        _handler = new CreateCustomerAppUserCommandHandler(
            _customerAppUserRepository, _customerRepository, _logRepository, _passwordHasher, _unitOfWork);
        _passwordHasher.Hash(Arg.Any<string>()).Returns(callInfo => $"hashed-{callInfo.Arg<string>()}");
    }

    private static CreateCustomerAppUserCommand ValidCommand(long? customerId, string userName = "johndoe") => new(
        CompanyId: 1,
        BranchId: 2,
        CustomerId: customerId,
        Cpf: "12345678900",
        UserName: userName,
        Email: "john@example.com",
        Password: "password123");

    [Fact]
    public async Task Handle_CustomerIdAlreadyLinked_ShouldSkipCustomerCreationAndCreateAppUser()
    {
        var command = ValidCommand(customerId: 5);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerRepository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _customerAppUserRepository.Received(1).AddAsync(
            Arg.Is<DomainCustomerAppUser>(u =>
                u.CustomerId == 5 &&
                u.UserName == "johndoe" &&
                u.Email == "john@example.com" &&
                u.PasswordHash == "hashed-password123"),
            Arg.Any<CancellationToken>());
        // O handler retorna Result.Success(customerId ?? 0) — ou seja, o Id devolvido é o
        // CustomerId vinculado, não o Id do CustomerAppUser recém-criado.
        result.Value.Should().Be(5);
        // Commit explícito do AddAsync do CustomerAppUser + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerIdNullWithUserNameProvided_ShouldCreateNewCustomerFirst()
    {
        var command = ValidCommand(customerId: null, userName: "newcustomer");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerRepository.Received(1).AddAsync(
            Arg.Is<Customer>(c => c.Name == "newcustomer" && c.CompanyId == command.CompanyId),
            Arg.Any<CancellationToken>());
        await _customerAppUserRepository.Received(1).AddAsync(
            Arg.Any<DomainCustomerAppUser>(), Arg.Any<CancellationToken>());
        // Commit da criação do Customer + commit do AddAsync do CustomerAppUser + commit do
        // finally da base.
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerIdNullAndUserNameBlank_ShouldReturnCustomerAppUserValidationFailureWithoutCreatingCustomer()
    {
        var command = ValidCommand(customerId: null, userName: string.Empty);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.EmptyUserName");
        // UserName em branco não satisfaz a condição de criação de Customer.
        await _customerRepository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _customerAppUserRepository.DidNotReceive().AddAsync(Arg.Any<DomainCustomerAppUser>(), Arg.Any<CancellationToken>());
        // Nenhum commit explícito é alcançado; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerAppUserValidationFailsAfterCustomerCreated_ShouldReturnFailure()
    {
        var command = ValidCommand(customerId: null, userName: "newcustomer") with { Email = string.Empty };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.EmptyEmail");
        await _customerRepository.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _customerAppUserRepository.DidNotReceive().AddAsync(Arg.Any<DomainCustomerAppUser>(), Arg.Any<CancellationToken>());
        // Commit da criação do Customer + commit do finally da base (a falha de validação do
        // CustomerAppUser ocorre antes de qualquer commit explícito adicional).
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
