using SyncBar.Application.Abstractions.Storage;

namespace SyncBar.Infrastructure.Storage;

// Armazena em wwwroot/uploads/products — servido por UseStaticFiles e
// incluido no backup da pasta da aplicacao.
internal sealed class LocalImageStorage : IImageStorage
{
    private static string Root => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");

    public async Task<string> SaveProductImageAsync(long productId, string extension, byte[] content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Root);

        // Remove versoes antigas com outra extensao.
        foreach (var old in Directory.GetFiles(Root, $"{productId}.*"))
            File.Delete(old);

        var fileName = $"{productId}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(Root, fileName), content, cancellationToken);

        // Cache-busting: o navegador recarrega quando a imagem muda.
        return $"/uploads/products/{fileName}?v={DateTime.UtcNow.Ticks}";
    }
}
