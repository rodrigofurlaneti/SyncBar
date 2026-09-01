using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Application.Features.PublicOrdering.ValidateComandaReading;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.PublicOrdering.ValidateComandaReading;

public sealed class ValidateComandaReadingCommandHandlerTests
{
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IImageStorage _imageStorage = Substitute.For<IImageStorage>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ValidateComandaReadingCommandHandler _handler;

    public ValidateComandaReadingCommandHandlerTests()
    {
        _handler = new ValidateComandaReadingCommandHandler(
            _diningTableRepository, _comandaRepository, _imageStorage, _logRepository, _unitOfWork);
    }

    private static DiningTable MakeTable() => DiningTable.Create(1, 1, 5, 4).Value;
    private static Comanda MakeComanda() => Comanda.Create(1, 1, "001").Value;

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnFailure()
    {
        var command = new ValidateComandaReadingCommand(Guid.NewGuid(), "001", "qrcode", "abc123", null);
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.InvalidToken");
    }

    [Fact]
    public async Task Handle_ComandaNotFound_ShouldReturnFailure()
    {
        var command = new ValidateComandaReadingCommand(Guid.NewGuid(), "999", "qrcode", "abc123", null);
        var table = MakeTable();
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, command.ComandaCode, Arg.Any<CancellationToken>())
            .Returns((Comanda?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.NotFound");
    }

    [Fact]
    public async Task Handle_QrCodeMethodWithScannedValue_ShouldSucceedAndSkipImageStorage()
    {
        var command = new ValidateComandaReadingCommand(Guid.NewGuid(), "001", "qrcode", "SCANNED-CODE", null);
        var table = MakeTable();
        var comanda = MakeComanda();
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, command.ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _imageStorage.DidNotReceiveWithAnyArgs().SaveComandaValidationPhotoAsync(default, default!, default!, default!);
        await _logRepository.Received(1).AddAsync(
            Arg.Is<LogTracker>(l => l.Message != null && l.Message.Contains("qrcode") && l.IsSuccess),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CameraMethodWithValidPhoto_ShouldSaveImageAndSucceed()
    {
        var photoBytes = new byte[] { 1, 2, 3 };
        var photoBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(photoBytes);
        var command = new ValidateComandaReadingCommand(Guid.NewGuid(), "001", "camera", null, photoBase64);
        var table = MakeTable();
        var comanda = MakeComanda();
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, command.ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _imageStorage.SaveComandaValidationPhotoAsync(table.Id, comanda.Code, ".jpg", Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns("/uploads/comanda-validations/1_001_20260901.jpg");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _imageStorage.Received(1).SaveComandaValidationPhotoAsync(
            table.Id, comanda.Code, ".jpg", Arg.Is<byte[]>(b => b.SequenceEqual(photoBytes)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CameraMethodWithInvalidBase64_ShouldReturnFailure()
    {
        var command = new ValidateComandaReadingCommand(Guid.NewGuid(), "001", "camera", null, "not-a-valid-base64!!");
        var table = MakeTable();
        var comanda = MakeComanda();
        _diningTableRepository.GetByQrTokenAsync(command.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, command.ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComandaReadingValidation.InvalidPhoto");
    }
}
