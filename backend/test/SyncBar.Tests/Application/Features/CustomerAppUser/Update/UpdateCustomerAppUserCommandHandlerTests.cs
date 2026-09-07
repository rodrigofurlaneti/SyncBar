using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAppUser.Update;
using SyncBar.Domain.Repositories;
using Xunit;
using DomainCustomerAppUser = SyncBar.Domain.Entities.CustomerAppUser;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.Update;

public sealed class UpdateCustomerAppUserCommandHandlerTests
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository = Substitute.For<ICustomerAppUserRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateCustomerAppUserCommandHandler _handler;

    public UpdateCustomerAppUserCommandHandlerTests()
    {
        _handler = new UpdateCustomerAppUserCommandHandler(_customerAppUserRepository, _logRepository, _unitOfWork);
    }

    private static DomainCustomerAppUser CreateActiveUser() =>
        DomainCustomerAppUser.Create(1, 2, 3, "joao123", "joao@bar.com", "hashed-password").Value;

    private static UpdateCustomerAppUserCommand ValidCommand(long id, string? password = null) => new(
        id, 1, 2, 3, "newname", "new@bar.com", password);

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        var command = ValidCommand(99);
        _customerAppUserRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((DomainCustomerAppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.NotFound");
        await _customerAppUserRepository.DidNotReceive().UpdateAsync(Arg.Any<DomainCustomerAppUser>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserInactive_ShouldReturnFailure()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var command = ValidCommand(user.Id);
        _customerAppUserRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAppUser.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommandWithoutPassword_ShouldUpdateDetailsOnlyAndCommitTwice()
    {
        var user = CreateActiveUser();
        var originalPasswordHash = user.PasswordHash;
        var command = ValidCommand(user.Id, password: null);
        _customerAppUserRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.UserName.Should().Be("newname");
        user.Email.Should().Be("new@bar.com");
        // Password nulo/branco -> a senha não deve ser alterada.
        user.PasswordHash.Should().Be(originalPasswordHash);
        await _customerAppUserRepository.Received(1).UpdateAsync(
            Arg.Is<DomainCustomerAppUser>(u => u.UserName == "newname"), Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommandWithPassword_ShouldChangePasswordHashAndCommitTwice()
    {
        var user = CreateActiveUser();
        var originalPasswordHash = user.PasswordHash;
        var command = ValidCommand(user.Id, password: "newpassword123");
        _customerAppUserRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // O handler usa BCrypt.Net.BCrypt.HashPassword diretamente (não o IPasswordHasher injetado
        // em CreateCustomerAppUserCommandHandler), então validamos o hash resultante via
        // BCrypt.Verify em vez de mockar um hasher.
        user.PasswordHash.Should().NotBe(originalPasswordHash);
        BCrypt.Net.BCrypt.Verify("newpassword123", user.PasswordHash).Should().BeTrue();
        await _customerAppUserRepository.Received(1).UpdateAsync(Arg.Any<DomainCustomerAppUser>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankPassword_ShouldNotChangePasswordHash()
    {
        var user = CreateActiveUser();
        var originalPasswordHash = user.PasswordHash;
        var command = ValidCommand(user.Id, password: "   ");
        _customerAppUserRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be(originalPasswordHash);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
