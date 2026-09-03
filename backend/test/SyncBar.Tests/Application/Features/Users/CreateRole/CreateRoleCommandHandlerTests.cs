using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Users.CreateRole;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Users.CreateRole;

public sealed class CreateRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _handler = new CreateRoleCommandHandler(_roleRepository, _logRepository, _unitOfWork);
    }

    private static CreateRoleCommand CreateValidCommand(string name = "Gerente")
        => new(CompanyId: 1, Name: name, Description: "Acesso administrativo");

    [Fact]
    public async Task Handle_NameAlreadyExistsForCompany_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand();
        _roleRepository.ExistsByNameAsync(command.CompanyId, command.Name, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Role.AlreadyExists");
        await _roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        // Sem persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(name: "");
        _roleRepository.ExistsByNameAsync(command.CompanyId, command.Name, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Role.EmptyName");
        await _roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistRoleAndReturnItsId()
    {
        var command = CreateValidCommand();
        _roleRepository.ExistsByNameAsync(command.CompanyId, command.Name, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _roleRepository.Received(1).AddAsync(
            Arg.Is<Role>(r =>
                r.CompanyId == command.CompanyId &&
                r.Name == command.Name &&
                r.Description == command.Description &&
                r.IsActive),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
