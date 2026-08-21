namespace SyncBar.Domain.Constants;

// Fase 8 (Shipping — entrega via malha do iFood pra pedidos de OUTROS canais: telefone, WhatsApp,
// site próprio). Ao contrário do módulo Logistics (fase 7, frota própria — tem uma sequência
// clara de 6 passos), o Shipping não devolve um "status" explícito em nenhuma resposta: só
// confirma a criação da entrega (id + trackingUrl) e depois expõe /tracking (lat/long) e
// /cancel. Por isso o status local aqui é só um reflexo das AÇÕES QUE O SYNCBAR TOMOU
// (pediu motorista / cancelou), não um espelho de um enum do iFood.
public static class IFoodShippingStatuses
{
    public const string DriverRequested = "DRIVER_REQUESTED";
    public const string Cancelled = "CANCELLED";
}
