using SyncBar.Application.Abstractions.Storage;

namespace SyncBar.Infrastructure.Storage;

// Armazena em wwwroot/uploads/products — servido por UseStaticFiles e
// incluido no backup da pasta da aplicacao.
internal sealed class LocalImageStorage : IImageStorage
{
    private static string Root => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");

    private static string ComandaValidationsRoot =>
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "comanda-validations");

    private static string TableValidationsRoot =>
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "table-validations");

    public async Task<string> SaveProductImageAsync(long productId, string extension, byte[] content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Root);

        // Remove versoes antigas com outra extensao.
        foreach (var old in Directory.GetFiles(Root, $"{productId}.*"))
            File.Delete(old);

        var fileName = $"{productId}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(Root, fileName), content, cancellationToken);

        // Cache-busting: o navegador recarrega quando a imagem muda.
        return $"/uploads/products/{fileName}?v={DateTime.Now.Ticks}";
    }

    // Cada comprovação fica com seu próprio nome (mesa + comanda + timestamp) — diferente da foto
    // de produto, aqui não há "a foto atual de X" para substituir; é um registro histórico.
    public async Task<string> SaveComandaValidationPhotoAsync(long tableId, string comandaCode, string extension, byte[] content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ComandaValidationsRoot);

        var safeComandaCode = string.Concat(comandaCode.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        var fileName = $"{tableId}_{safeComandaCode}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(ComandaValidationsRoot, fileName), content, cancellationToken);

        return $"/uploads/comanda-validations/{fileName}";
    }

    // Validação da MESA (fluxo sem comanda) — mesmo raciocínio de histórico da comanda:
    // cada validação fica com seu próprio arquivo, nada é sobrescrito.
    public async Task<string> SaveTableValidationPhotoAsync(long tableId, string extension, byte[] content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(TableValidationsRoot);

        var fileName = $"{tableId}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(TableValidationsRoot, fileName), content, cancellationToken);

        return $"/uploads/table-validations/{fileName}";
    }
}
