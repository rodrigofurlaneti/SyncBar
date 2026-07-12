using System.Globalization;
using System.Text;

namespace SyncBar.Infrastructure.Printing;

// Gera o payload ESC/POS: init + texto ASCII normalizado + alimentacao + corte.
// Acentos sao removidos (codepages variam entre impressoras termicas).
// Tamanho por linha via marcadores do TicketFormatter:
//   \u0001 (Tall) = altura dupla | \u0002 (Big) = altura+largura duplas + negrito
internal static class EscPos
{
    public const char TallMarker = '\u0001';
    public const char BigMarker = '\u0002';

    public static byte[] Build(string text)
    {
        var payload = new List<byte> { 0x1B, 0x40 }; // ESC @ — inicializa

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine;
            byte size = 0x00;
            var bold = false;

            if (line.StartsWith(BigMarker))
            {
                line = line[1..];
                size = 0x11; // 2x altura + 2x largura (21 colunas)
                bold = true;
            }
            else if (line.StartsWith(TallMarker))
            {
                line = line[1..];
                size = 0x01; // 2x altura (mantem 42 colunas)
            }

            payload.AddRange([0x1D, 0x21, size]);                    // GS ! n — tamanho
            payload.AddRange([0x1B, 0x45, (byte)(bold ? 1 : 0)]);    // ESC E — negrito
            payload.AddRange(Encoding.ASCII.GetBytes(Normalize(line)));
            payload.Add(0x0A);
        }

        payload.AddRange([0x1D, 0x21, 0x00, 0x1B, 0x45, 0x00]);      // reset
        payload.AddRange("\n\n\n"u8.ToArray());   // alimenta antes do corte
        payload.AddRange([0x1D, 0x56, 0x42, 0x00]);  // GS V B 0 — corte parcial
        return [.. payload];
    }

    internal static string Normalize(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(ch <= 127 ? ch : '?');
        }
        return builder.ToString();
    }
}
