namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public interface IAsaasService
    {
        /// <summary>
        /// Aplica a credencial (BaseUrl + access_token) da empresa/filial informada nas próximas
        /// chamadas feitas por esta instância — deve ser chamado antes de qualquer outro método.
        /// </summary>
        Task ConfigureForTenantAsync(long companyId, long? branchId, CancellationToken cancellationToken = default);

        Task<string> CreateCustomerAsync(string name, string cpfCnpj, string email, string? mobilePhone = null, CancellationToken cancellationToken = default);
        Task DeleteCustomerAsync(string asaasCustomerId, CancellationToken cancellationToken = default);

        Task<AsaasPaymentResponse> CreatePixPaymentAsync(string customerId, decimal value, DateTime dueDate, string description, CancellationToken cancellationToken = default);
        Task<AsaasPixQrCodeResponse> GetPixQrCodeAsync(string paymentId, CancellationToken cancellationToken = default);

        Task<AsaasBoletoIdentificationFieldResponse> GetBoletoIdentificationFieldAsync(string paymentId, CancellationToken cancellationToken = default);

        Task<AsaasCreditCardPaymentResponse> CreateCreditCardPaymentAsync(
            string customerId,
            decimal value,
            DateTime dueDate,
            string description,
            CreditCardRequest card,
            CreditCardHolderInfoRequest holderInfo,
            string? remoteIp = null,
            int installmentCount = 1,
            CancellationToken cancellationToken = default);

        Task<AsaasPaymentResponse> CreatePaymentWithCardTokenAsync(
            string customerId,
            decimal value,
            DateTime dueDate,
            string description,
            string creditCardToken,
            int installmentCount = 1,
            string? remoteIp = null,
            CancellationToken cancellationToken = default);

        Task<AsaasTokenizeCreditCardResponse> TokenizeCreditCardAsync(
            string customerId,
            CreditCardRequest card,
            CreditCardHolderInfoRequest? holderInfo = null,
            CancellationToken cancellationToken = default);

        Task DeletePaymentAsync(string asaasPaymentId, CancellationToken cancellationToken = default);
    }
}

