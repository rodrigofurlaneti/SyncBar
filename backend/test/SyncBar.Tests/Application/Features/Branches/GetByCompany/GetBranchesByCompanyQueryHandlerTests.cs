using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Branches;
using SyncBar.Application.Features.Branches.GetByCompany;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Branches.GetByCompany;

public sealed class GetBranchesByCompanyQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetBranchesByCompanyQueryHandler _handler;

    public GetBranchesByCompanyQueryHandlerTests()
    {
        _handler = new GetBranchesByCompanyQueryHandler(_branchRepository, _logRepository, _unitOfWork);
    }

    private static Branch CreateBranch(
        long companyId = 1,
        string name = "Filial Centro",
        string? cnpj = "12345678000199",
        string? phone = "1122223333",
        string? addressCity = "São Paulo",
        string? addressState = "SP")
        => Branch.Create(
            companyId, name, cnpj, phone,
            addressStreet: "Rua das Flores", addressNumber: "100", addressDistrict: "Centro",
            addressCity: addressCity, addressState: addressState, addressZipCode: "01000000").Value;

    [Fact]
    public async Task Handle_NoBranchesForCompany_ShouldReturnEmptyCollection()
    {
        var query = new GetBranchesByCompanyQuery(CompanyId: 1);
        _branchRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Branch>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveAndInactiveBranches_ShouldMapAllFieldsAndNotFilterByIsActive()
    {
        var query = new GetBranchesByCompanyQuery(CompanyId: 1);
        var activeBranch = CreateBranch(name: "Filial Centro", addressCity: "São Paulo", addressState: "SP");
        var inactiveBranch = CreateBranch(name: "Filial Zona Sul", addressCity: "Santo André", addressState: "SP");
        inactiveBranch.Deactivate();

        _branchRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([activeBranch, inactiveBranch]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // O handler apenas mapeia o que o repositório retorna, sem filtrar por IsActive:
        // a filial inativa deve aparecer normalmente na resposta.
        result.Value.Should().HaveCount(2);

        var activeResponse = result.Value.Single(r => r.Name == "Filial Centro");
        activeResponse.Id.Should().Be(activeBranch.Id);
        activeResponse.Name.Should().Be(activeBranch.Name);
        activeResponse.Cnpj.Should().Be(activeBranch.Cnpj);
        activeResponse.Phone.Should().Be(activeBranch.Phone);
        activeResponse.AddressCity.Should().Be(activeBranch.AddressCity);
        activeResponse.AddressState.Should().Be(activeBranch.AddressState);
        activeResponse.IsActive.Should().BeTrue();

        var inactiveResponse = result.Value.Single(r => r.Name == "Filial Zona Sul");
        inactiveResponse.Id.Should().Be(inactiveBranch.Id);
        inactiveResponse.Cnpj.Should().Be(inactiveBranch.Cnpj);
        inactiveResponse.Phone.Should().Be(inactiveBranch.Phone);
        inactiveResponse.AddressCity.Should().Be(inactiveBranch.AddressCity);
        inactiveResponse.AddressState.Should().Be(inactiveBranch.AddressState);
        inactiveResponse.IsActive.Should().BeFalse();
    }
}
