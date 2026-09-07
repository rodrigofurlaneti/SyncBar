using FluentAssertions;
using SyncBar.Infrastructure.Storage;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Storage;

// LocalImageStorage writes real files under Directory.GetCurrentDirectory()/wwwroot/uploads/...
// (the test project's own bin output directory when running under xUnit). We cannot avoid the
// real filesystem here without mutating Environment.CurrentDirectory (process-global, unsafe with
// parallel test runs), so instead we use unique negative ids per test and delete every file we
// create in Dispose, keeping runs deterministic and leaving no garbage behind.
public sealed class LocalImageStorageTests : IDisposable
{
    private readonly LocalImageStorage _storage = new();
    private readonly List<string> _createdFiles = [];

    private static string ProductsRoot => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
    private static string ComandaValidationsRoot => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "comanda-validations");
    private static string TableValidationsRoot => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "table-validations");

    private static long UniqueId() => -Math.Abs(DateTime.UtcNow.Ticks % 1_000_000_000) - Random.Shared.Next(1, 100_000);

    public void Dispose()
    {
        foreach (var file in _createdFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public async Task SaveProductImageAsync_ShouldCreateFileWithCorrectNameAndReturnUrlWithCacheBusting()
    {
        var productId = UniqueId();
        var content = new byte[] { 1, 2, 3 };

        var url = await _storage.SaveProductImageAsync(productId, ".jpg", content, CancellationToken.None);

        var expectedPath = Path.Combine(ProductsRoot, $"{productId}.jpg");
        _createdFiles.Add(expectedPath);

        File.Exists(expectedPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(expectedPath)).Should().BeEquivalentTo(content);

        url.Should().StartWith($"/uploads/products/{productId}.jpg?v=");
        url.Should().MatchRegex(@"^/uploads/products/-?\d+\.jpg\?v=\d+$");
    }

    [Fact]
    public async Task SaveProductImageAsync_CalledTwiceWithDifferentExtension_ShouldDeleteOldFile()
    {
        var productId = UniqueId();
        var firstPath = Path.Combine(ProductsRoot, $"{productId}.jpg");
        var secondPath = Path.Combine(ProductsRoot, $"{productId}.png");
        _createdFiles.Add(firstPath);
        _createdFiles.Add(secondPath);

        await _storage.SaveProductImageAsync(productId, ".jpg", [1, 2, 3], CancellationToken.None);
        File.Exists(firstPath).Should().BeTrue();

        await _storage.SaveProductImageAsync(productId, ".png", [4, 5, 6], CancellationToken.None);

        File.Exists(firstPath).Should().BeFalse("the old file with a different extension should have been removed");
        File.Exists(secondPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveComandaValidationPhotoAsync_ShouldStripUnsafeCharactersFromComandaCode()
    {
        var tableId = UniqueId();
        const string comandaCode = "AB 12/34-CD_#!";
        var content = new byte[] { 9, 9 };

        var url = await _storage.SaveComandaValidationPhotoAsync(tableId, comandaCode, ".png", content, CancellationToken.None);

        url.Should().StartWith("/uploads/comanda-validations/");
        url.Should().Contain($"{tableId}_AB1234-CD_");
        url.Should().NotContain("/34");
        url.Should().NotContain(" ");
        url.Should().NotContain("!");
        url.Should().NotContain("#");

        var fileName = url["/uploads/comanda-validations/".Length..];
        var fullPath = Path.Combine(ComandaValidationsRoot, fileName);
        _createdFiles.Add(fullPath);

        File.Exists(fullPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(fullPath)).Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task SaveTableValidationPhotoAsync_ShouldCreateFileAndReturnExpectedUrlPrefix()
    {
        var tableId = UniqueId();
        var content = new byte[] { 7, 7, 7 };

        var url = await _storage.SaveTableValidationPhotoAsync(tableId, ".jpg", content, CancellationToken.None);

        url.Should().StartWith($"/uploads/table-validations/{tableId}_");
        url.Should().EndWith(".jpg");

        var fileName = url["/uploads/table-validations/".Length..];
        var fullPath = Path.Combine(TableValidationsRoot, fileName);
        _createdFiles.Add(fullPath);

        File.Exists(fullPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(fullPath)).Should().BeEquivalentTo(content);
    }
}
