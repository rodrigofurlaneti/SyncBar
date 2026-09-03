using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood;

public sealed class SetIfoodMerchantMappingCommandHandlerTests
{
    private readonly IIfoodMerchantMappingRepository _mappingRepository = Substitute.For<IIfoodMerchantMappingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetIfoodMerchantMappingCommandHandler _handler;

    public SetIfoodMerchantMappingCommandHandlerTests()
    {
        _handler = new SetIfoodMerchantMappingCommandHandler(_mappingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoExistingMapping_ShouldCreateNewMappingSetMerchantAndPersist()
    {
        var command = new SetIfoodMerchantMappingCommand(BranchId: 1, MerchantId: "MERCH-1", MerchantUuid: "uuid-1");
        _mappingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((IfoodMerchantMapping?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mappingRepository.Received(1).AddAsync(
            Arg.Is<IfoodMerchantMapping>(m =>
                m.BranchId == command.BranchId &&
                m.MerchantId == "MERCH-1" &&
                m.MerchantUuid == "uuid-1"),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingMapping_ShouldUpdateInPlaceWithoutCreatingNew()
    {
        var existing = IfoodMerchantMapping.Create(branchId: 1).Value;
        existing.SetMerchant("OLD-MERCH", "old-uuid");

        var command = new SetIfoodMerchantMappingCommand(BranchId: 1, MerchantId: "NEW-MERCH", MerchantUuid: "new-uuid");
        _mappingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.MerchantId.Should().Be("NEW-MERCH");
        existing.MerchantUuid.Should().Be("new-uuid");
        await _mappingRepository.DidNotReceive().AddAsync(Arg.Any<IfoodMerchantMapping>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullMerchantFields_ShouldClearExistingMapping()
    {
        var existing = IfoodMerchantMapping.Create(branchId: 1).Value;
        existing.SetMerchant("OLD-MERCH", "old-uuid");

        var command = new SetIfoodMerchantMappingCommand(BranchId: 1, MerchantId: null, MerchantUuid: null);
        _mappingRepository.GetByBranchForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.MerchantId.Should().BeNull();
        existing.MerchantUuid.Should().BeNull();
    }
}
