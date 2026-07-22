namespace SyncBar.Application.Abstractions.Storage;

public interface IImageStorage
{
    // Salva/substitui a imagem do produto e retorna a URL relativa servida pela API.
    Task<string> SaveProductImageAsync(long productId, string extension, byte[] content, CancellationToken cancellationToken = default);
}
