namespace SyncBar.Application.Abstractions.Integrations.Keeta
{
    public sealed record KeetaAuthorizedShop(long Id, string Name, string Address, double Longitude, double Latitude, string TimeZone);
}
