using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.ActivateProduct;
using SyncBar.Application.Features.Catalog.CreateProduct;
using SyncBar.Application.Features.Catalog.DeactivateProduct;
using SyncBar.Application.Features.Catalog.GetMenuForManagement;
using SyncBar.Application.Features.Catalog.GetProductById;
using SyncBar.Application.Features.Catalog.SetProductImage;
using SyncBar.Application.Features.Catalog.UpdateProduct;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class ProductsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    private static CreateProductCommand ValidCreateCommand() => new(1, 1, 1, "Refrigerante", null, null, 8m, 4m, false, null);

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithValue()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(5L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Category.NotFound", "categoria nao encontrada")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateProductRequest(1, 1, "Refrigerante", null, null, 8m, 4m, false, null);
        _mediator.Send(Arg.Is<UpdateProductCommand>(c => c.ProductId == 1 && c.CategoryId == 1 && c.Name == "Refrigerante"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateProductRequest(1, 1, "Refrigerante", null, null, 8m, 4m, false, null);
        _mediator.Send(Arg.Any<UpdateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UploadImage_NoFile_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var result = await _controller.UploadImage(1, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<SetProductImageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadImage_EmptyFile_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var emptyFile = new FormFile(new MemoryStream([]), 0, 0, "file", "image.png");

        var result = await _controller.UploadImage(1, emptyFile, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<SetProductImageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadImage_Success_ShouldForwardExtensionAndContentAndReturnOkWithWrappedUrl()
    {
        var content = new byte[] { 1, 2, 3 };
        var file = new FormFile(new MemoryStream(content), 0, content.Length, "file", "image.png");
        _mediator.Send(Arg.Is<SetProductImageCommand>(c => c.ProductId == 1 && c.Extension == ".png" && c.Content.Length == 3),
            Arg.Any<CancellationToken>()).Returns(Result.Success("https://cdn.example/image.png"));

        var result = await _controller.UploadImage(1, file, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { imageUrl = "https://cdn.example/image.png" });
    }

    [Fact]
    public async Task UploadImage_Failure_ShouldReturnMappedErrorResult()
    {
        var content = new byte[] { 1 };
        var file = new FormFile(new MemoryStream(content), 0, content.Length, "file", "image.png");
        _mediator.Send(Arg.Any<SetProductImageCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.UploadImage(999, file, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetProductByIdQuery>(q => q.ProductId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((ProductResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetProductByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ProductResponse>(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetForManagement_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetMenuForManagementQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<ProductManagementResponse>>([]));

        var result = await _controller.GetForManagement(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetForManagement_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetMenuForManagementQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<ProductManagementResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetForManagement(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Deactivate_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivateProductCommand>(c => c.ProductId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Deactivate(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.Deactivate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Activate_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<ActivateProductCommand>(c => c.ProductId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Activate(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Activate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ActivateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.Activate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
