using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.ActivateCategory;
using SyncBar.Application.Features.Catalog.CreateCategory;
using SyncBar.Application.Features.Catalog.DeactivateCategory;
using SyncBar.Application.Features.Catalog.GetCategories;
using SyncBar.Application.Features.Catalog.GetCategoriesForManagement;
using SyncBar.Application.Features.Catalog.GetCategoryById;
using SyncBar.Application.Features.Catalog.UpdateCategory;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class CategoriesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _controller = new CategoriesController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByCompany_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCategoriesQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<CategoryResponse>>([]));

        var result = await _controller.GetByCompany(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<CategoryResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetByCompany(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCategoryByIdQuery>(q => q.CategoryId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((CategoryResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCategoryByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CategoryResponse>(new Error("Category.NotFound", "categoria nao encontrada")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetForManagement_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCategoriesForManagementQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<CategoryManagementResponse>>([]));

        var result = await _controller.GetForManagement(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetForManagement_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCategoriesForManagementQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<CategoryManagementResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetForManagement(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithValue()
    {
        var command = new CreateCategoryCommand(1, "Bebidas", 0);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(5L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateCategoryCommand(1, "", 0);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Category.EmptyName", "nome obrigatorio")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateCategoryRequest("Bebidas", 1);
        _mediator.Send(Arg.Is<UpdateCategoryCommand>(c => c.CategoryId == 1 && c.Name == "Bebidas" && c.DisplayOrder == 1),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateCategoryRequest("Bebidas", 1);
        _mediator.Send(Arg.Any<UpdateCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Category.NotFound", "categoria nao encontrada")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Deactivate_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivateCategoryCommand>(c => c.CategoryId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Deactivate(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivateCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Category.NotFound", "categoria nao encontrada")));

        var result = await _controller.Deactivate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Activate_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<ActivateCategoryCommand>(c => c.CategoryId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Activate(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Activate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ActivateCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Category.NotFound", "categoria nao encontrada")));

        var result = await _controller.Activate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
