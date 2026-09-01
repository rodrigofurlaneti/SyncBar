using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Application.Features.PublicOrdering.ValidateTableReading;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.PublicOrdering.ValidateTableReading;

public sealed class ValidateTableReadingCommandHandlerTests
{
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IImageStorage _imageStorage = Substitute.For<IImageStorage>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ValidateTableReadingCommandHandler _handler;

    public ValidateTableReadingCommandHandlerTests()
    {
        _handler = new ValidateTableReadingCommandHandler(
            _diningTableRepository, _imageStorage, _logRepository, _unitOfWork);
    }

    private static DiningTable MakeTable() => DiningTable.Create(1, 1, 5, 4).Value;

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnFailure()
    {
        var command = new ValidateTableReadingCommand(Guid.NewGuid(), "qrcode", "abc123", null);
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.InvalidToken");
    }

    [Fact]
    public async Task Handle_QrCodeMethodWithScannedValue_ShouldSucceedAndSkipImageStorage()
    {
        var command = new ValidateTableReadingCommand(Guid.NewGuid(), "qrcode", "SCANNED-CODE", null);
        var table = MakeTable();
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _imageStorage.DidNotReceiveWithAnyArgs().SaveTableValidationPhotoAsync(default, default!, default!);
        await _logRepository.Received(1).AddAsync(
            Arg.Is<LogTracker>(l => l.Message != null && l.Message.Contains("qrcode") && l.Message.Contains($"Mesa {table.Number}") && l.IsSuccess),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CameraMethodWithValidPhoto_ShouldSaveImageAndSucceed()
    {
        var photoBytes = new byte[] { 1, 2, 3 };
        var photoBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(photoBytes);
        var command = new ValidateTableReadingCommand(Guid.NewGuid(), "camera", null, photoBase64);
        var table = MakeTable();
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _imageStorage.SaveTableValidationPhotoAsync(table.Id, ".jpg", Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns("/uploads/table-validations/1_20260901.jpg");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _imageStorage.Received(1).SaveTableValidationPhotoAsync(
            table.Id, ".jpg", Arg.Is<byte[]>(b => b.SequenceEqual(photoBytes)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CameraMethodWithInvalidBase64_ShouldReturnFailure()
    {
        var command = new ValidateTableReadingCommand(Guid.NewGuid(), "camera", null, "not-a-valid-base64!!");
        var table = MakeTable();
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReadingValidation.InvalidPhoto");
    }
}
