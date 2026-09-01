using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Customers.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Customers.Create;

public sealed class CreateCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _handler = new CreateCustomerCommandHandler(_customerRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailureWithoutPersisting()
    {
        var command = new CreateCustomerCommand(CompanyId: 1, Name: "", Phone: "11999990000", Cpf: "12345678900", Email: "cliente@teste.com");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.EmptyName");
        await _customerRepository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistCustomerAndReturnItsId()
    {
        var command = new CreateCustomerCommand(CompanyId: 1, Name: "Cliente Teste", Phone: "11999990000", Cpf: "12345678900", Email: "cliente@teste.com");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _customerRepository.Received(1).AddAsync(
            Arg.Is<Customer>(c =>
                c.CompanyId == command.CompanyId &&
                c.Name == command.Name &&
                c.Phone == command.Phone &&
                c.Cpf == command.Cpf &&
                c.Email == command.Email),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
