using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Ifood.Catalog;
using SyncBar.Domain.Primitives;
using SyncBar.Infrastructure.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodCatalogSyncTriggerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    private IfoodCatalogSyncTrigger CreateTrigger()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IMediator)).Returns(_mediator);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateScope().Returns(scope);

        return new IfoodCatalogSyncTrigger(_scopeFactory);
    }

    [Fact]
    public async Task TriggerCompanySync_ShouldDispatchSyncCommandOnOwnScope()
    {
        var tcs = new TaskCompletionSource<SyncIfoodCatalogCommand>();
        _mediator.Send(Arg.Any<SyncIfoodCatalogCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                tcs.SetResult(callInfo.Arg<SyncIfoodCatalogCommand>());
                return Task.FromResult(Result.Success(new IfoodCatalogSyncSummary(false, 1, 0, 0, 0, 0)));
            });

        CreateTrigger().TriggerCompanySync(42);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        completed.Should().Be(tcs.Task, "the mediator should be invoked from the trigger's own background scope");
        (await tcs.Task).CompanyId.Should().Be(42);
        _scopeFactory.Received(1).CreateScope();
    }

    [Fact]
    public async Task TriggerCompanySync_WhenMediatorThrows_ShouldSwallowExceptionAndNotPropagate()
    {
        var tcs = new TaskCompletionSource<bool>();
        _mediator.Send(Arg.Any<SyncIfoodCatalogCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                tcs.SetResult(true);
                return Task.FromException<Result<IfoodCatalogSyncSummary>>(new InvalidOperationException("falha simulada"));
            });

        var act = () => CreateTrigger().TriggerCompanySync(1);

        act.Should().NotThrow();
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        completed.Should().Be(tcs.Task);
    }
}
