using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Customers;
using SyncBar.Application.Features.Customers.GetByCompany;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Customers.GetByCompany;

public sealed class GetCustomersByCompanyQueryHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomersByCompanyQueryHandler _handler;

    public GetCustomersByCompanyQueryHandlerTests()
    {
        _handler = new GetCustomersByCompanyQueryHandler(_customerRepository, _logRepository, _unitOfWork);
    }

    private static Customer CreateCustomer(string name = "Cliente Teste")
        => Customer.Create(companyId: 1, name: name, phone: "11999990000", cpf: "12345678900", email: "cliente@teste.com").Value;

    [Fact]
    public async Task Handle_NoSearchTerm_ShouldListAllCustomersOfTheCompany()
    {
        var query = new GetCustomersByCompanyQuery(CompanyId: 1, Search: null);
        var customer = CreateCustomer();
        _customerRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>()).Returns([customer]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var response = result.Value.Single();
        response.Id.Should().Be(customer.Id);
        response.Name.Should().Be(customer.Name);
        response.Phone.Should().Be(customer.Phone);
        response.Cpf.Should().Be(customer.Cpf);
        response.Email.Should().Be(customer.Email);
        response.LoyaltyPoints.Should().Be(customer.LoyaltyPoints);
        response.IsActive.Should().Be(customer.IsActive);

        await _customerRepository.DidNotReceive().SearchAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankSearchTerm_ShouldListAllCustomersInsteadOfSearching()
    {
        var query = new GetCustomersByCompanyQuery(CompanyId: 1, Search: "   ");
        _customerRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerRepository.Received(1).GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>());
        await _customerRepository.DidNotReceive().SearchAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldSearchUsingTrimmedTerm()
    {
        var query = new GetCustomersByCompanyQuery(CompanyId: 1, Search: "  Rodrigo  ");
        _customerRepository.SearchAsync(query.CompanyId, "Rodrigo", Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerRepository.Received(1).SearchAsync(query.CompanyId, "Rodrigo", Arg.Any<CancellationToken>());
        await _customerRepository.DidNotReceive().GetByCompanyAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }
}
