using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyncBar.API.Serialization;

// Todas as datas do sistema sao gravadas em UTC, mas o EF Core as devolve com
// Kind = Unspecified — serializadas sem o sufixo 'Z', o navegador as interpreta
// como horario LOCAL (cronometros travados, horarios deslocados em -3h).
// Este converter garante ISO-8601 com 'Z' na saida e UTC na entrada.
internal sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = DateTime.Parse(
            reader.GetString()!,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc), // Unspecified: o banco guarda UTC
        };
        writer.WriteStringValue(utc.ToString("yyyy-MM-dd\'T\'HH:mm:ss.fffffff\'Z\'", CultureInfo.InvariantCulture));
    }
}
