using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Catalog.Complements;
using SyncBar.Application.Features.Catalog.Complements.AddComplement;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementItem;
using SyncBar.Application.Features.Catalog.Complements.DeactivateComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.DeactivateComplementItem;
using SyncBar.Application.Features.Catalog.Complements.GetComplementGroups;
using SyncBar.Application.Features.Catalog.Complements.GetComplementItems;
using SyncBar.Application.Features.Catalog.Complements.GetProductComplementGroups;
using SyncBar.Application.Features.Catalog.Complements.LinkProductComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.RemoveComplement;
using SyncBar.Application.Features.Catalog.Complements.UnlinkProductComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementItem;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementPrice;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class ComplementsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ComplementsController _controller;

    public ComplementsControllerTests()
    {
        _controller = new ComplementsController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetItems_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetComplementItemsQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<ComplementItemResponse>>([]));

        var result = await _controller.GetItems(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetItems_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetComplementItemsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<ComplementItemResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetItems(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateItem_Success_ShouldReturnOkWithValue()
    {
        var command = new CreateComplementItemCommand(1, "Gelo");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.CreateItem(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(5L);
    }

    [Fact]
    public async Task CreateItem_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateComplementItemCommand(1, "");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("ComplementItem.EmptyName", "nome obrigatorio")));

        var result = await _controller.CreateItem(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateItem_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateComplementItemRequest("Gelo extra");
        _mediator.Send(Arg.Is<UpdateComplementItemCommand>(c => c.ComplementItemId == 1 && c.Name == "Gelo extra"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UpdateItem(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateItem_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateComplementItemRequest("Gelo extra");
        _mediator.Send(Arg.Any<UpdateComplementItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("ComplementItem.NotFound", "item nao encontrado")));

        var result = await _controller.UpdateItem(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeactivateItem_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivateComplementItemCommand>(c => c.ComplementItemId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.DeactivateItem(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeactivateItem_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivateComplementItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("ComplementItem.NotFound", "item nao encontrado")));

        var result = await _controller.DeactivateItem(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetGroups_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetComplementGroupsQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<ComplementGroupResponse>>([]));

        var result = await _controller.GetGroups(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetGroups_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetComplementGroupsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<ComplementGroupResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetGroups(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateGroup_Success_ShouldReturnOkWithValue()
    {
        var command = new CreateComplementGroupCommand(1, "Adicionais", 1, 0, 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(6L));

        var result = await _controller.CreateGroup(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(6L);
    }

    [Fact]
    public async Task CreateGroup_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateComplementGroupCommand(1, "", 1, 0, 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("ComplementGroup.EmptyName", "nome obrigatorio")));

        var result = await _controller.CreateGroup(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateGroup_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateComplementGroupRequest("Adicionais", 1, 0, 1);
        _mediator.Send(Arg.Is<UpdateComplementGroupCommand>(c => c.ComplementGroupId == 1 && c.Name == "Adicionais"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UpdateGroup(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateGroup_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateComplementGroupRequest("Adicionais", 1, 0, 1);
        _mediator.Send(Arg.Any<UpdateComplementGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("ComplementGroup.NotFound", "grupo nao encontrado")));

        var result = await _controller.UpdateGroup(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeactivateGroup_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivateComplementGroupCommand>(c => c.ComplementGroupId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.DeactivateGroup(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeactivateGroup_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivateComplementGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("ComplementGroup.NotFound", "grupo nao encontrado")));

        var result = await _controller.DeactivateGroup(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddComplement_Success_ShouldSendCommandWithGroupIdAndReturnOkWithValue()
    {
        var request = new AddComplementRequest(1, 2m);
        _mediator.Send(Arg.Is<AddComplementCommand>(c => c.ComplementGroupId == 1 && c.ComplementItemId == 1 && c.ExtraPrice == 2m),
            Arg.Any<CancellationToken>()).Returns(Result.Success(7L));

        var result = await _controller.AddComplement(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(7L);
    }

    [Fact]
    public async Task AddComplement_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddComplementRequest(1, 2m);
        _mediator.Send(Arg.Any<AddComplementCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("ComplementGroup.NotFound", "grupo nao encontrado")));

        var result = await _controller.AddComplement(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateComplementPrice_Success_ShouldSendCommandWithBothIdsAndReturnNoContent()
    {
        var request = new UpdateComplementPriceRequest(3m);
        _mediator.Send(Arg.Is<UpdateComplementPriceCommand>(c => c.ComplementGroupId == 1 && c.ComplementId == 2 && c.ExtraPrice == 3m),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.UpdateComplementPrice(1, 2, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateComplementPrice_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateComplementPriceRequest(3m);
        _mediator.Send(Arg.Any<UpdateComplementPriceCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Complement.NotFound", "complemento nao encontrado")));

        var result = await _controller.UpdateComplementPrice(999, 2, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveComplement_Success_ShouldSendCommandWithBothIdsAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<RemoveComplementCommand>(c => c.ComplementGroupId == 1 && c.ComplementId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.RemoveComplement(1, 2, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveComplement_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RemoveComplementCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Complement.NotFound", "complemento nao encontrado")));

        var result = await _controller.RemoveComplement(999, 2, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetProductGroups_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetProductComplementGroupsQuery>(q => q.ProductId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<ProductComplementGroupResponse>>([]));

        var result = await _controller.GetProductGroups(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProductGroups_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetProductComplementGroupsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<ProductComplementGroupResponse>>(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.GetProductGroups(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task LinkProductGroup_Success_ShouldSendCommandWithProductIdAndReturnOkWithValue()
    {
        var request = new LinkProductComplementGroupRequest(1, 0);
        _mediator.Send(Arg.Is<LinkProductComplementGroupCommand>(c => c.ProductId == 1 && c.ComplementGroupId == 1 && c.DisplayOrder == 0),
            Arg.Any<CancellationToken>()).Returns(Result.Success(8L));

        var result = await _controller.LinkProductGroup(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(8L);
    }

    [Fact]
    public async Task LinkProductGroup_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new LinkProductComplementGroupRequest(1, 0);
        _mediator.Send(Arg.Any<LinkProductComplementGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.LinkProductGroup(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UnlinkProductGroup_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<UnlinkProductComplementGroupCommand>(c => c.ProductComplementGroupId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UnlinkProductGroup(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UnlinkProductGroup_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<UnlinkProductComplementGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("ProductComplementGroup.NotFound", "vinculo nao encontrado")));

        var result = await _controller.UnlinkProductGroup(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
