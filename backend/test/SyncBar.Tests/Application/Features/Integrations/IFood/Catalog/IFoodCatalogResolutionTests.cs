using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Catalog;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Catalog;

public sealed class IFoodCatalogResolutionTests
{
    private const string AccessToken = "valid-token";
    private const string MerchantId = "merchant-1";

    private readonly IIfoodCatalogClient _catalogClient = Substitute.For<IIfoodCatalogClient>();

    [Fact]
    public async Task ResolveDefaultCatalogIdAsync_WhenFetchFails_ShouldReturnFailureWithClientErrorMessage()
    {
        var result1 = new IfoodCatalogsListResult(false, Array.Empty<IfoodCatalogSummaryDto>(), "Ifood indisponível");
        _catalogClient.GetCatalogsAsync(AccessToken, MerchantId, Arg.Any<CancellationToken>()).Returns(result1);

        var result = await IfoodCatalogResolution.ResolveDefaultCatalogIdAsync(AccessToken, MerchantId, _catalogClient, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.CatalogsFetchFailed");
        result.Error.Message.Should().Be("Ifood indisponível");
    }

    [Fact]
    public async Task ResolveDefaultCatalogIdAsync_WhenFetchFailsWithoutErrorMessage_ShouldUseDefaultMessage()
    {
        var result1 = new IfoodCatalogsListResult(false, Array.Empty<IfoodCatalogSummaryDto>(), null);
        _catalogClient.GetCatalogsAsync(AccessToken, MerchantId, Arg.Any<CancellationToken>()).Returns(result1);

        var result = await IfoodCatalogResolution.ResolveDefaultCatalogIdAsync(AccessToken, MerchantId, _catalogClient, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.CatalogsFetchFailed");
        result.Error.Message.Should().Be("Falha ao listar os catálogos da loja no Ifood.");
    }

    [Fact]
    public async Task ResolveDefaultCatalogIdAsync_WhenNoCatalogs_ShouldReturnNoCatalogsFailure()
    {
        var result1 = new IfoodCatalogsListResult(true, Array.Empty<IfoodCatalogSummaryDto>(), null);
        _catalogClient.GetCatalogsAsync(AccessToken, MerchantId, Arg.Any<CancellationToken>()).Returns(result1);

        var result = await IfoodCatalogResolution.ResolveDefaultCatalogIdAsync(AccessToken, MerchantId, _catalogClient, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.NoCatalogs");
    }

    [Fact]
    public async Task ResolveDefaultCatalogIdAsync_WhenAvailableCatalogExists_ShouldPreferItOverOthersCaseInsensitively()
    {
        var catalogs = new List<IfoodCatalogSummaryDto>
        {
            new("catalog-inactive", "PAUSED", null, null, null),
            new("catalog-available", "available", null, null, null),
        };
        var result1 = new IfoodCatalogsListResult(true, catalogs, null);
        _catalogClient.GetCatalogsAsync(AccessToken, MerchantId, Arg.Any<CancellationToken>()).Returns(result1);

        var result = await IfoodCatalogResolution.ResolveDefaultCatalogIdAsync(AccessToken, MerchantId, _catalogClient, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("catalog-available");
    }

    [Fact]
    public async Task ResolveDefaultCatalogIdAsync_WhenNoAvailableCatalog_ShouldFallBackToFirst()
    {
        var catalogs = new List<IfoodCatalogSummaryDto>
        {
            new("catalog-first", "PAUSED", null, null, null),
            new("catalog-second", "DRAFT", null, null, null),
        };
        var result1 = new IfoodCatalogsListResult(true, catalogs, null);
        _catalogClient.GetCatalogsAsync(AccessToken, MerchantId, Arg.Any<CancellationToken>()).Returns(result1);

        var result = await IfoodCatalogResolution.ResolveDefaultCatalogIdAsync(AccessToken, MerchantId, _catalogClient, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("catalog-first");
    }

    [Fact]
    public async Task ResolveDefaultCatalogIdAsync_WhenChosenCatalogHasNoCatalogId_ShouldReturnNoCatalogIdFailure()
    {
        var catalogs = new List<IfoodCatalogSummaryDto> { new(" ", "AVAILABLE", null, null, null) };
        var result1 = new IfoodCatalogsListResult(true, catalogs, null);
        _catalogClient.GetCatalogsAsync(AccessToken, MerchantId, Arg.Any<CancellationToken>()).Returns(result1);

        var result = await IfoodCatalogResolution.ResolveDefaultCatalogIdAsync(AccessToken, MerchantId, _catalogClient, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodCatalog.NoCatalogId");
    }
}
