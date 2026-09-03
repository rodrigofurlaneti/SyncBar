using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Users.GetRoles;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Users.GetRoles;

public sealed class GetRolesQueryHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetRolesQueryHandler _handler;

    public GetRolesQueryHandlerTests()
    {
        _handler = new GetRolesQueryHandler(_roleRepository, _logRepository, _unitOfWork);
    }

    private static Role CreateRole(string name, string? description = null, long companyId = 1)
        => Role.Create(companyId, name, description).Value;

    [Fact]
    public async Task Handle_NoRolesForCompany_ShouldReturnEmptyCollection()
    {
        var query = new GetRolesQuery(CompanyId: 1);
        _roleRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Role>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleRoles_ShouldOrderByNameAndMapFields()
    {
        var query = new GetRolesQuery(CompanyId: 1);
        var roleGerente = CreateRole("Gerente", "Acesso administrativo");
        var roleAtendente = CreateRole("Atendente", null);
        _roleRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([roleGerente, roleAtendente]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.Name).Should().ContainInOrder("Atendente", "Gerente");

        var firstResponse = result.Value.First();
        firstResponse.Name.Should().Be(roleAtendente.Name);
        firstResponse.Description.Should().Be(roleAtendente.Description);

        var secondResponse = result.Value.Last();
        secondResponse.Name.Should().Be(roleGerente.Name);
        secondResponse.Description.Should().Be(roleGerente.Description);
    }
}
