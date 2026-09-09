using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace SyncBar.E2ETests;

public sealed class CustomerSignupTheoryAttribute : TheoryAttribute
{
    public CustomerSignupTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("E2E_RUN") != "1" ||
            Environment.GetEnvironmentVariable("E2E_CUSTOMER_SIGNUP") != "1")
            Skip = "Defina E2E_RUN=1 e E2E_CUSTOMER_SIGNUP=1 para cadastrar clientes reais de teste.";
    }
}

internal sealed class CustomerTestSettings
{
    public Uri BaseUrl { get; } = new(Settings.Required("E2E_BASE_URL"));
    public long BranchId { get; } = long.Parse(Settings.Required("E2E_BRANCH_ID"), CultureInfo.InvariantCulture);
    public string SequenceFile { get; }

    public CustomerTestSettings()
    {
        if (BaseUrl.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(BaseUrl.UserInfo) || BranchId <= 0)
            throw new InvalidOperationException("Configure E2E_BASE_URL HTTP(S) sem credenciais e E2E_BRANCH_ID positivo.");
        var target = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{BaseUrl.GetLeftPart(UriPartial.Authority)}/{BranchId}")))[..16];
        SequenceFile = Environment.GetEnvironmentVariable("E2E_CUSTOMER_SEQUENCE_FILE") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SyncBar", "E2E", $"customer-sequence-{target}.txt");
    }
}

// Deliberately not a record: its default ToString must not expose credentials.
internal sealed class CustomerTestData
{
    public const string ZipCode = "07025020";
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Cpf { get; init; }

    public static CustomerTestData Create(string sequenceFile)
    {
        var number = NextNumber(sequenceFile);
        return new CustomerTestData
        {
            Name = $"João Furlaneti Teste {number}",
            Email = $"joao.furlaneti.teste.{number}.{Guid.NewGuid():N}@example.com",
            Password = $"E2e!{Guid.NewGuid():N}",
            Cpf = ValidCpf(),
        };
    }

    internal static long NextNumber(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        // FileShare.None coordinates processes sharing this counter. Reserve before the
        // request: a failed/partial registration must never reuse a previously issued name.
        FileStream? stream = null;
        for (var attempt = 0; stream is null; attempt++)
        {
            try { stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (attempt < 50) { Thread.Sleep(100); }
        }
        using (stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
            var text = reader.ReadToEnd().Trim();
            var previous = text.Length == 0 ? 0 : long.Parse(text, CultureInfo.InvariantCulture);
            if (previous < 0) throw new InvalidOperationException("Contador de clientes inválido.");
            var next = checked(previous + 1);
            var bytes = Encoding.UTF8.GetBytes(next.ToString(CultureInfo.InvariantCulture));
            stream.Position = 0;
            stream.Write(bytes);
            stream.SetLength(bytes.Length);
            stream.Flush(flushToDisk: true);
            return next;
        }
    }

    internal static string ValidCpf()
    {
        var digits = Enumerable.Range(0, 9).Select(_ => RandomNumberGenerator.GetInt32(10)).ToList();
        for (var weight = 10; weight <= 11; weight++)
        {
            var remainder = digits.Select((digit, index) => digit * (weight - index)).Sum() % 11;
            digits.Add(remainder < 2 ? 0 : 11 - remainder);
        }
        return digits.Distinct().Count() == 1 ? ValidCpf() : string.Concat(digits);
    }
}
