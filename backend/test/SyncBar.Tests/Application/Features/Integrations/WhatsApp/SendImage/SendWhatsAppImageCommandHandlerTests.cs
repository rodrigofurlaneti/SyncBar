using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Notifications;
using SyncBar.Application.Features.Integrations.WhatsApp.SendImage;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.WhatsApp.SendImage;

public sealed class SendWhatsAppImageCommandHandlerTests
{
    private readonly IWhatsAppQueue _whatsAppService = Substitute.For<IWhatsAppQueue>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SendWhatsAppImageCommandHandler _handler;

    public SendWhatsAppImageCommandHandlerTests()
    {
        _handler = new SendWhatsAppImageCommandHandler(_whatsAppService, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ServiceSucceeds_ShouldReturnSuccess()
    {
        var command = new SendWhatsAppImageCommand("5511999999999", "Ola", "https://cdn.example.com/img.jpg");
        _whatsAppService
            .EnqueueAsync(command.PhoneNumber, command.Message, command.FileUrl, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _whatsAppService.Received(1)
            .EnqueueAsync(command.PhoneNumber, command.Message, command.FileUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceFails_ShouldReturnFailureFromService()
    {
        var command = new SendWhatsAppImageCommand("5511999999999", "Ola", "https://cdn.example.com/img.jpg");
        var error = Error.Failure("WhatsApp.SendFailed", "A API do WhatsApp retornou status 500.");
        _whatsAppService
            .EnqueueAsync(command.PhoneNumber, command.Message, command.FileUrl, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.SendFailed");
    }
}

