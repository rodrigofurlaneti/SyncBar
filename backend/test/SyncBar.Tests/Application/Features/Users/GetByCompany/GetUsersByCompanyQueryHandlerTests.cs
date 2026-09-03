using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Users.GetByCompany;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Users.GetByCompany;

public sealed class GetUsersByCompanyQueryHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IUserRoleRepository _userRoleRepository = Substitute.For<IUserRoleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetUsersByCompanyQueryHandler _handler;

    public GetUsersByCompanyQueryHandlerTests()
    {
        _handler = new GetUsersByCompanyQueryHandler(_userRepository, _userRoleRepository, _logRepository, _unitOfWork);
    }

    private static AppUser CreateUser(string userName, long id)
    {
        var user = AppUser.Create(companyId: 1, employeeId: null, userName: userName, email: $"{userName}@teste.com", passwordHash: "hash-fake").Value;
        // AppUser.Id só existe após persistência real (fábrica usa base(0)) — para simular 2+
        // usuários coexistindo com Ids distintos (necessário aqui, pois o handler casa vínculos de
        // role por AppUserId == u.Id), atribuímos via reflection, conforme convenção documentada
        // no projeto ("Entity.Id tem getter público").
        typeof(AppUser).GetProperty(nameof(AppUser.Id))!.SetValue(user, id);
        return user;
    }

    [Fact]
    public async Task Handle_NoUsersForCompany_ShouldReturnEmptyCollection()
    {
        var query = new GetUsersByCompanyQuery(CompanyId: 1);
        _userRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>()).Returns(Array.Empty<AppUser>());
        _userRoleRepository.GetByUsersAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserRole>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleUsers_ShouldOrderByUserNameAndMapRoleIdsPerUser()
    {
        var query = new GetUsersByCompanyQuery(CompanyId: 1);
        var userBeatriz = CreateUser("beatriz", id: 2);
        var userAna = CreateUser("ana", id: 1);
        _userRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>())
            .Returns([userBeatriz, userAna]);

        var linkAna = UserRole.Create(1, userAna.Id, roleId: 10).Value;
        var linkBeatriz = UserRole.Create(1, userBeatriz.Id, roleId: 20).Value;
        _userRoleRepository.GetByUsersAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([linkAna, linkBeatriz]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.UserName).Should().ContainInOrder("ana", "beatriz");

        var firstResponse = result.Value.First();
        firstResponse.Id.Should().Be(userAna.Id);
        firstResponse.UserName.Should().Be("ana");
        firstResponse.Email.Should().Be(userAna.Email);
        firstResponse.EmployeeId.Should().Be(userAna.EmployeeId);
        firstResponse.IsActive.Should().Be(userAna.IsActive);
        firstResponse.RoleIds.Should().ContainSingle().Which.Should().Be(10);

        var secondResponse = result.Value.Last();
        secondResponse.Id.Should().Be(userBeatriz.Id);
        secondResponse.RoleIds.Should().ContainSingle().Which.Should().Be(20);
    }

    [Fact]
    public async Task Handle_UserWithoutRoleLinks_ShouldReturnEmptyRoleIdsForThatUser()
    {
        var query = new GetUsersByCompanyQuery(CompanyId: 1);
        var user = CreateUser("carla", id: 3);
        _userRepository.GetByCompanyAsync(query.CompanyId, Arg.Any<CancellationToken>()).Returns([user]);
        _userRoleRepository.GetByUsersAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserRole>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single().RoleIds.Should().BeEmpty();
    }
}
