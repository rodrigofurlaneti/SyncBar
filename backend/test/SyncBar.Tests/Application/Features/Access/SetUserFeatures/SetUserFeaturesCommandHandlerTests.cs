using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Access.SetUserFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Access.SetUserFeatures;

public sealed class SetUserFeaturesCommandHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IAppUserFeatureRepository _userFeatureRepository = Substitute.For<IAppUserFeatureRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetUserFeaturesCommandHandler _handler;

    public SetUserFeaturesCommandHandlerTests()
    {
        _handler = new SetUserFeaturesCommandHandler(_userRepository, _userFeatureRepository, _unitOfWork);
    }

    private static AppUser CreateActiveUser(long companyId = 1, string userName = "waiter")
        => AppUser.Create(companyId, null, userName, $"{userName}@bar.com", "hashed-password").Value;

    // Este handler implementa ICommandHandler diretamente (não usa BaseCommandHandler/ExecuteWithLogAsync),
    // então, ao contrário dos outros handlers testados, NÃO há um commit implícito de log no finally —
    // CommitAsync só é chamado explicitamente, uma única vez, no caminho de sucesso.

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailureWithoutTouchingLinksOrCommitting()
    {
        var command = new SetUserFeaturesCommand(AppUserId: 1, FeatureIds: [10, 20]);
        _userRepository.GetByIdAsync(command.AppUserId, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUser.NotFound");

        await _userFeatureRepository.DidNotReceive().GetByUserForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserInactive_ShouldReturnFailure()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var command = new SetUserFeaturesCommand(AppUserId: 1, FeatureIds: [10]);
        _userRepository.GetByIdAsync(command.AppUserId, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUser.NotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewLinkCreationFails_ShouldReturnFailureWithoutPersistingOrCommitting()
    {
        var user = CreateActiveUser();
        // FeatureId 0 é inválido para AppUserFeature.Create (Ids devem ser > 0).
        var command = new SetUserFeaturesCommand(AppUserId: user.Id, FeatureIds: [0]);
        _userRepository.GetByIdAsync(command.AppUserId, Arg.Any<CancellationToken>()).Returns(user);
        _userFeatureRepository.GetByUserForUpdateAsync(command.AppUserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AppUserFeature>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUserFeature.InvalidIds");

        await _userFeatureRepository.DidNotReceive().AddAsync(Arg.Any<AppUserFeature>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
