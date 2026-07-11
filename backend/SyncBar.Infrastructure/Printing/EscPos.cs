using System.Globalization;
using System.Text;

namespace SyncBar.Infrastructure.Printing;

// Gera o payload ESC/POS: init + texto ASCII normalizado + alimentacao + corte.
// Acentos sao removidos (codepages variam entre impressoras termicas).
internal static class EscPos
{
    public static byte[] Build(string text)
    {
        var payload = new List<byte> { 0x1B, 0x40 }; // ESC @ — inicializa
        payload.AddRange(Encoding.ASCII.GetBytes(Normalize(text)));
        payload.AddRange("\n\n\n\n"u8.ToArray());   // alimenta antes do corte
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
