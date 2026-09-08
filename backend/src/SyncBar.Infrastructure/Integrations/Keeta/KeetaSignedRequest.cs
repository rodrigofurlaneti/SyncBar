using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.Keeta;

namespace SyncBar.Infrastructure.Integrations.Keeta;

internal static class KeetaSignedRequest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static HttpRequestMessage Create(KeetaCredentials credentials, HttpMethod method, string path, object? payload = null)
    {
        var uri = new Uri(new Uri(credentials.BaseUrl.TrimEnd('/') + "/"), path);
        var request = new HttpRequestMessage(method, uri);
        string body = payload is null ? string.Empty : Canonicalize(JsonSerializer.SerializeToElement(payload, JsonOptions));
        if (payload is not null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Add("X-App-Id", credentials.AppId);
        request.Headers.Add("X-App-Signature", Sign(uri, body, credentials.ClientSecret));
        return request;
    }

    internal static string Sign(Uri uri, string body, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Select(pair => new KeyValuePair<string, string>(Decode(pair[0]), pair.Length > 1 ? Decode(pair[1]) : ""))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}");
        using var document = string.IsNullOrWhiteSpace(body) ? null : JsonDocument.Parse(body);
        var canonicalBody = document is null ? "" : Canonicalize(document.RootElement);
        if (canonicalBody == "{}") canonicalBody = "";
        var message = string.Join('&', new[] { uri.GetLeftPart(UriPartial.Path), string.Join('&', query), canonicalBody }
            .Where(part => part.Length > 0));
        return Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(message)));
    }

    private static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));

    internal static string Canonicalize(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(',', element.EnumerateObject()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => JsonSerializer.Serialize(property.Name, JsonOptions) + ":" + Canonicalize(property.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(',', element.EnumerateArray().Select(Canonicalize)) + "]",
        JsonValueKind.String => JsonSerializer.Serialize(element.GetString(), JsonOptions),
        JsonValueKind.Number => FormatNumber(element.GetDouble()),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => throw new JsonException("Unsupported JSON value.")
    };

    private static string FormatNumber(double value)
    {
        if (!double.IsFinite(value)) throw new JsonException("Non-finite JSON number.");
        if (value == 0) return "0";
        var text = value.ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant();
        var parts = text.Split('e');
        if (parts.Length == 1) return text;
        var exponent = int.Parse(parts[1], CultureInfo.InvariantCulture);
        if (Math.Abs(value) >= 1e-6 && Math.Abs(value) < 1e21)
        {
            var negative = parts[0].StartsWith('-');
            var mantissa = parts[0].TrimStart('-');
            var dot = mantissa.IndexOf('.');
            var decimalPosition = (dot < 0 ? mantissa.Length : dot) + exponent;
            var digits = mantissa.Replace(".", "");
            var expanded = decimalPosition <= 0 ? "0." + new string('0', -decimalPosition) + digits
                : decimalPosition >= digits.Length ? digits + new string('0', decimalPosition - digits.Length)
                : digits.Insert(decimalPosition, ".");
            return negative ? "-" + expanded : expanded;
        }
        return parts[0] + "e" + (exponent >= 0 ? "+" : "") + exponent.ToString(CultureInfo.InvariantCulture);
    }
}
