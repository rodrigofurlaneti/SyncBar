namespace SyncBar.Application.Abstractions.Storage;

public interface IImageStorage
{
    // Salva/substitui a imagem do produto e retorna a URL relativa servida pela API.
    Task<string> SaveProductImageAsync(long productId, string extension, byte[] content, CancellationToken cancellationToken = default);

    // Salva a foto de comprovação da validação de leitura de comanda (cenário "câmera")
    // e retorna a URL relativa servida pela API.
    Task<string> SaveComandaValidationPhotoAsync(long tableId, string comandaCode, string extension, byte[] content, CancellationToken cancellationToken = default);

    // Salva a foto de comprovação da validação de leitura da MESA (fluxo sem comanda,
    // "Visualização do Cliente (QR Code)" desligada — cenário "câmera") e retorna a URL
    // relativa servida pela API.
    Task<string> SaveTableValidationPhotoAsync(long tableId, string extension, byte[] content, CancellationToken cancellationToken = default);
}
