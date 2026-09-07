using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAppUser.Remove;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainCustomerAppUser = SyncBar.Domain.Entities.CustomerAppUser;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.Remove;

public sealed class RemoveCustomerAppUserCommandHandlerTests
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository = Substitute.For<ICustomerAppUserRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RemoveCustomerAppUserCommandHandler _handler;

    public RemoveCustomerAppUserCommandHandlerTests()
    {
        _handler = new RemoveCustomerAppUserCommandHandler(_customerAppUserRepository, _logRepository, _unitOfWork);
    }

    private static DomainCustomerAppUser CreateActiveUser() =>
        DomainCustomerAppUser.Create(1, 2, 3, "joao123", "joao@bar.com", "hashed-password").Value;

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        var command = new RemoveCustomerAppUserCommand(99);
        _customerAppUserRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((DomainCustomerAppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.NotFound");
        await _customerAppUserRepository.DidNotReceive().RemoveAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserInactive_ShouldReturnFailure()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var command = new RemoveCustomerAppUserCommand(user.Id);
        _customerAppUserRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldRemoveAndCommitTwice()
    {
        var user = CreateActiveUser();
        var command = new RemoveCustomerAppUserCommand(user.Id);
        _customerAppUserRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerAppUserRepository.Received(1).RemoveAsync(command.Id, Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
