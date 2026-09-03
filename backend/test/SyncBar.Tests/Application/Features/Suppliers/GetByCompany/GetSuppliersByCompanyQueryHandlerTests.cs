using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Suppliers.GetByCompany;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Suppliers.GetByCompany;

public sealed class GetSuppliersByCompanyQueryHandlerTests
{
    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetSuppliersByCompanyQueryHandler _handler;

    public GetSuppliersByCompanyQueryHandlerTests()
    {
        _handler = new GetSuppliersByCompanyQueryHandler(_supplierRepository, _logRepository, _unitOfWork);
    }

    private static Supplier CreateSupplier(string legalName, string? tradeName = null, string? cnpj = null, string? email = null, string? phone = null)
        => Supplier.Create(companyId: 1, legalName, tradeName, cnpj, email, phone).Value;

    [Fact]
    public async Task Handle_NoSuppliersForCompany_ShouldReturnEmptyCollection()
    {
        var query = new GetSuppliersByCompanyQuery(CompanyId: 1);
        _supplierRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Supplier>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleSuppliers_ShouldMapAllFieldsForEachSupplier()
    {
        var query = new GetSuppliersByCompanyQuery(CompanyId: 1);
        var supplierCentral = CreateSupplier("Distribuidora Central Ltda", "Central Bebidas", "12345678000199", "contato@central.com.br", "11999998888");
        var supplierSimples = CreateSupplier("Fornecedor Simples ME");
        _supplierRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([supplierCentral, supplierSimples]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var responseCentral = result.Value.First(r => r.LegalName == "Distribuidora Central Ltda");
        responseCentral.Id.Should().Be(supplierCentral.Id);
        responseCentral.TradeName.Should().Be("Central Bebidas");
        responseCentral.Cnpj.Should().Be("12345678000199");
        responseCentral.Email.Should().Be("contato@central.com.br");
        responseCentral.Phone.Should().Be("11999998888");
        responseCentral.IsActive.Should().BeTrue();

        var responseSimples = result.Value.First(r => r.LegalName == "Fornecedor Simples ME");
        responseSimples.TradeName.Should().BeNull();
        responseSimples.Cnpj.Should().BeNull();
        responseSimples.Email.Should().BeNull();
        responseSimples.Phone.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InactiveSupplier_ShouldMapIsActiveFalse()
    {
        var query = new GetSuppliersByCompanyQuery(CompanyId: 1);
        var supplier = CreateSupplier("Fornecedor Desativado Ltda");
        supplier.Deactivate();
        _supplierRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>()).Returns([supplier]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single().IsActive.Should().BeFalse();
    }
}
