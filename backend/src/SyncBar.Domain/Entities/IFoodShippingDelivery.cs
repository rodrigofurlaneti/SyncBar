using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

/// <summary>
/// Fase 8 — Shipping: pedido feito por OUTRO canal (telefone, WhatsApp, site próprio — não um
/// IFoodOrder) que a equipe decide entregar usando a malha de entregadores do iFood. Diferente de
/// <see cref="IFoodLogisticsDelivery"/> (fase 7, frota PRÓPRIA do restaurante entregando pedido
/// QUE VEIO do iFood), aqui é o INVERSO: pedido que NÃO veio do iFood, entregue POR entregadores
/// do iFood. Por isso não há FK pra IFoodOrder nem pra CustomerOrder — <see cref="OrderReference"/>
/// é só um texto livre digitado pela equipe pra identificar o pedido (ex.: "Balcão #45",
/// "Telefone (11) 98765-4321"), sem vínculo forçado com nenhuma tabela do POS.
///
/// Criado somente DEPOIS que "Request a driver for an external order" responde 202 com um
/// {id, trackingUrl} — a cotação (GET deliveryAvailabilities) anterior é efêmera (não persistida
/// aqui), só usada pela tela pra mostrar preço/prazo antes de confirmar (o quoteId dela expira,
/// ver campo ExpirationAt na resposta do iFood, e é obrigatório no corpo do request-driver).
///
/// Ressalva de confiança: campos e formatos confirmados contra a doc oficial (Postman collection
/// "Shipping") colada pelo usuário em 2026-08-20. O endereço/telefone estruturado (postalCode,
/// streetNumber, streetName, areaCode, number, etc.) é exigido pelo corpo do request-driver — não
/// existe um endpoint de geocodificação nesta doc, então coordinates aqui são OPCIONAIS (nullable)
/// e, se omitidas, o SyncBar simplesmente não as envia (o iFood aceita o endereço textual sozinho,
/// pela descrição da doc, mas isso não foi testado contra o sandbox).
/// </summary>
public sealed class IFoodShippingDelivery : AggregateRoot
{
    public long BranchId { get; private set; }
    public string? OrderReference { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string CustomerPhoneAreaCode { get; private set; } = null!;
    public string CustomerPhoneNumber { get; private set; } = null!;
    public string PostalCode { get; private set; } = null!;
    public string StreetName { get; private set; } = null!;
    public string StreetNumber { get; private set; } = null!;
    public string? Complement { get; private set; }
    public string Neighborhood { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string? Reference { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public decimal MerchantFee { get; private set; }
    public string QuoteId { get; private set; } = null!;
    public string IFoodDeliveryId { get; private set; } = null!;
    public string? TrackingUrl { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTime RequestedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodShippingDelivery() : base(0) { }

    private IFoodShippingDelivery(
        long branchId, string? orderReference, string customerName, string customerPhoneAreaCode, string customerPhoneNumber,
        string postalCode, string streetName, string streetNumber, string? complement, string neighborhood,
        string city, string state, string country, string? reference, double? latitude, double? longitude,
        decimal merchantFee, string quoteId, string ifoodDeliveryId, string? trackingUrl, DateTime now) : base(0)
    {
        BranchId = branchId;
        OrderReference = orderReference;
        CustomerName = customerName;
        CustomerPhoneAreaCode = customerPhoneAreaCode;
        CustomerPhoneNumber = customerPhoneNumber;
        PostalCode = postalCode;
        StreetName = streetName;
        StreetNumber = streetNumber;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        State = state;
        Country = country;
        Reference = reference;
        Latitude = latitude;
        Longitude = longitude;
        MerchantFee = merchantFee;
        QuoteId = quoteId;
        IFoodDeliveryId = ifoodDeliveryId;
        TrackingUrl = trackingUrl;
        Status = IFoodShippingStatuses.DriverRequested;
        RequestedAt = now;
        IsActive = true;
        CreatedAt = now;
    }

    public static Result<IFoodShippingDelivery> Create(
        long branchId, string? orderReference, string customerName, string customerPhoneAreaCode, string customerPhoneNumber,
        string postalCode, string streetName, string streetNumber, string? complement, string neighborhood,
        string city, string state, string country, string? reference, double? latitude, double? longitude,
        decimal merchantFee, string quoteId, string ifoodDeliveryId, string? trackingUrl, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return Result.Failure<IFoodShippingDelivery>(new Error("IFoodShippingDelivery.MissingCustomerName", "Customer name is required."));
        if (string.IsNullOrWhiteSpace(customerPhoneAreaCode) || string.IsNullOrWhiteSpace(customerPhoneNumber))
            return Result.Failure<IFoodShippingDelivery>(new Error("IFoodShippingDelivery.MissingCustomerPhone", "Customer phone is required."));
        if (string.IsNullOrWhiteSpace(postalCode) || string.IsNullOrWhiteSpace(streetName) || string.IsNullOrWhiteSpace(streetNumber)
            || string.IsNullOrWhiteSpace(neighborhood) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
            return Result.Failure<IFoodShippingDelivery>(new Error("IFoodShippingDelivery.IncompleteAddress", "Delivery address is incomplete."));
        if (merchantFee < 0)
            return Result.Failure<IFoodShippingDelivery>(new Error("IFoodShippingDelivery.InvalidMerchantFee", "Merchant fee cannot be negative."));
        if (string.IsNullOrWhiteSpace(quoteId))
            return Result.Failure<IFoodShippingDelivery>(new Error("IFoodShippingDelivery.MissingQuoteId", "A valid quote is required before requesting a driver."));
        if (string.IsNullOrWhiteSpace(ifoodDeliveryId))
            return Result.Failure<IFoodShippingDelivery>(new Error("IFoodShippingDelivery.MissingDeliveryId", "iFood did not return a delivery id."));

        return Result.Success(new IFoodShippingDelivery(
            branchId, orderReference?.Trim(), customerName.Trim(), customerPhoneAreaCode.Trim(), customerPhoneNumber.Trim(),
            postalCode.Trim(), streetName.Trim(), streetNumber.Trim(), complement?.Trim(), neighborhood.Trim(),
            city.Trim(), state.Trim(), string.IsNullOrWhiteSpace(country) ? "Brasil" : country.Trim(), reference?.Trim(),
            latitude, longitude, merchantFee, quoteId, ifoodDeliveryId, trackingUrl, now));
    }

    public Result MarkCancelled(string? reason, DateTime now)
    {
        if (Status == IFoodShippingStatuses.Cancelled)
            return Result.Failure(new Error("IFoodShippingDelivery.AlreadyCancelled", "Esta entrega já foi cancelada."));

        Status = IFoodShippingStatuses.Cancelled;
        CancellationReason = reason;
        CancelledAt = now;
        UpdatedAt = now;
        return Result.Success();
    }
}
