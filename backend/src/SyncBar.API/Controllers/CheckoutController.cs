using Microsoft.AspNetCore.Mvc;
using SyncBar.Infrastructure.Integrations.Asaas;

namespace SyncBar.API.Controllers
{
    [ApiController]
    [Route("api/checkout")]
    public class CheckoutController : ControllerBase
    {
        private readonly IAsaasService _asaas;

        public CheckoutController(IAsaasService asaas)
        {
            _asaas = asaas;
        }

        [HttpPost("pix")]
        public async Task<IActionResult> PayWithPix([FromBody] CheckoutPixDto dto)
        {
            var customerId = await _asaas.CreateCustomerAsync(dto.Nome, dto.CpfCnpj, dto.Email, dto.Telefone);
            var cobranca = await _asaas.CreatePixPaymentAsync(
                customerId,
                dto.Valor,
                DateTime.UtcNow.AddDays(1),
                $"Pedido #{dto.PedidoId}"
            );
            var qrCode = await _asaas.GetPixQrCodeAsync(cobranca.Id);
            return Ok(new
            {
                cobrancaId = cobranca.Id,
                status = cobranca.Status,
                copiaECola = qrCode.Payload,
                qrCodeBase64 = qrCode.EncodedImage,
                expiraEm = qrCode.ExpirationDate
            });
        }

        [HttpPost("cartao")]
        public async Task<IActionResult> PayWithCreditCard([FromBody] CheckoutCartaoDto dto)
        {
            var customerId = await _asaas.CreateCustomerAsync(dto.Nome, dto.CpfCnpj, dto.Email, dto.Telefone);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var resultado = await _asaas.CreateCreditCardPaymentAsync(
                customerId,
                dto.Valor,
                DateTime.UtcNow.AddDays(1),
                $"Pedido #{dto.PedidoId}",
                dto.Cartao,
                dto.DadosPortador,
                ipAddress,
                dto.Parcelas > 0 ? dto.Parcelas : 1
            );

            return Ok(new
            {
                cobrancaId = resultado.Id,
                status = resultado.Status,
                bandeira = resultado.CreditCard?.CreditCardBrand,
                ultimosDigitos = resultado.CreditCard?.CreditCardNumber,
                tokenCartao = resultado.CreditCard?.CreditCardToken 
            });
        }
    }

    public record CheckoutPixDto(string Nome, string CpfCnpj, string Email, string Telefone, decimal Valor, int PedidoId);
    public record CheckoutCartaoDto(
        string Nome,
        string CpfCnpj,
        string Email,
        string Telefone,
        decimal Valor,
        int PedidoId,
        int Parcelas,
        CreditCardRequest Cartao,
        CreditCardHolderInfoRequest DadosPortador
    );
}
