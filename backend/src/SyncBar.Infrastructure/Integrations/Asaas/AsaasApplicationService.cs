using AppAsaas = SyncBar.Application.Abstractions.Integrations.Asaas;

namespace SyncBar.Infrastructure.Integrations.Asaas;

internal sealed class AsaasApplicationService(AsaasService asaasService) : AppAsaas.IAsaasService
{
    public Task<string> CreateCustomerAsync(string name, string cpfCnpj, string email, string? mobilePhone = null, CancellationToken cancellationToken = default)
        => asaasService.CreateCustomerAsync(name, cpfCnpj, email, mobilePhone, cancellationToken);

    public Task DeleteCustomerAsync(string asaasCustomerId, CancellationToken cancellationToken = default)
        => asaasService.DeleteCustomerAsync(asaasCustomerId, cancellationToken);

    public Task DeletePaymentAsync(string asaasPaymentId, CancellationToken cancellationToken = default)
        => asaasService.DeletePaymentAsync(asaasPaymentId, cancellationToken);

    public async Task<AppAsaas.AsaasPaymentResponse> CreatePixPaymentAsync(string customerId, decimal value, DateTime dueDate, string description, CancellationToken cancellationToken = default)
    {
        var r = await asaasService.CreatePixPaymentAsync(customerId, value, dueDate, description, cancellationToken);
        return new AppAsaas.AsaasPaymentResponse(r.Id, r.Status, r.Value, r.NetValue, null, r.InvoiceUrl, r.BankSlipUrl);
    }

    public async Task<AppAsaas.AsaasPixQrCodeResponse> GetPixQrCodeAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        var r = await asaasService.GetPixQrCodeAsync(paymentId, cancellationToken);
        return new AppAsaas.AsaasPixQrCodeResponse(r.EncodedImage, r.Payload, r.ExpirationDate);
    }

    public async Task<AppAsaas.AsaasCreditCardPaymentResponse> CreateCreditCardPaymentAsync(
        string customerId,
        decimal value,
        DateTime dueDate,
        string description,
        AppAsaas.CreditCardRequest card,
        AppAsaas.CreditCardHolderInfoRequest holderInfo,
        string? remoteIp = null,
        int installmentCount = 1,
        CancellationToken cancellationToken = default)
    {
        var infraCard = new CreditCardRequest(card.HolderName, card.Number, card.ExpiryMonth, card.ExpiryYear, card.Ccv);
        var infraHolder = new CreditCardHolderInfoRequest(
            holderInfo.Name, holderInfo.Email, holderInfo.CpfCnpj, holderInfo.PostalCode, holderInfo.AddressNumber, holderInfo.Phone ?? string.Empty);

        var r = await asaasService.CreateCreditCardPaymentAsync(
            customerId, value, dueDate, description, infraCard, infraHolder, remoteIp, installmentCount, cancellationToken);

        return new AppAsaas.AsaasCreditCardPaymentResponse(r.Id, r.Status, r.Value, r.NetValue, r.CreditCard?.CreditCardToken);
    }

    public async Task<AppAsaas.AsaasPaymentResponse> CreatePaymentWithCardTokenAsync(
        string customerId,
        decimal value,
        DateTime dueDate,
        string description,
        string creditCardToken,
        int installmentCount = 1,
        string? remoteIp = null,
        CancellationToken cancellationToken = default)
    {
        var r = await asaasService.CreatePaymentWithCardTokenAsync(customerId, value, dueDate, description, creditCardToken, installmentCount, remoteIp, cancellationToken);
        return new AppAsaas.AsaasPaymentResponse(r.Id, r.Status, r.Value, r.NetValue, null, r.InvoiceUrl, r.BankSlipUrl);
    }

    public async Task<AppAsaas.AsaasTokenizeCreditCardResponse> TokenizeCreditCardAsync(
        string customerId,
        AppAsaas.CreditCardRequest card,
        AppAsaas.CreditCardHolderInfoRequest? holderInfo = null,
        CancellationToken cancellationToken = default)
    {
        var infraCard = new CreditCardRequest(card.HolderName, card.Number, card.ExpiryMonth, card.ExpiryYear, card.Ccv);
        CreditCardHolderInfoRequest? infraHolder = holderInfo is null
            ? null
            : new CreditCardHolderInfoRequest(
                holderInfo.Name, holderInfo.Email, holderInfo.CpfCnpj, holderInfo.PostalCode, holderInfo.AddressNumber, holderInfo.Phone ?? string.Empty);

        var r = await asaasService.TokenizeCreditCardAsync(customerId, infraCard, infraHolder, cancellationToken);
        return new AppAsaas.AsaasTokenizeCreditCardResponse(r.CreditCardToken, r.CreditCardBrand, r.CreditCardNumber);
    }

    public async Task<AppAsaas.AsaasPaymentResponse> CreatePaymentAsync(
        string customerId,
        string billingType,
        decimal value,
        DateTime dueDate,
        string description,
        string? creditCardToken = null,
        int installmentCount = 1,
        CancellationToken cancellationToken = default)
    {
        var r = await asaasService.CreatePaymentAsync(customerId, billingType, value, dueDate, description, creditCardToken, installmentCount, cancellationToken);
        return new AppAsaas.AsaasPaymentResponse(r.Id, r.Status, r.Value, r.NetValue, null, r.InvoiceUrl, r.BankSlipUrl);
    }
}
