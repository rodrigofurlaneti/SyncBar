using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SyncBar.Tests.API;

// Program.cs usa top-level statements — os "métodos locais" (ConfigureLogging, ValidateJwtSecret
// etc.) são compilados pelo C# como métodos privados estáticos do tipo gerado `Program` (nomes
// mangled, ex.: "<<Main>$>g__ValidateJwtSecret|0_6"), então dá pra invocá-los via reflection.
// Só ValidateJwtSecret/ValidateDatabaseConnectionString são exercitados aqui: são funções puras
// (recebem IConfiguration, não têm efeito colateral além de lançar) e representam o único
// comportamento de bootstrap que vale a pena isolar em teste unitário. As demais (ConfigureLogging,
// ConfigureAuthentication, ConfigureCors, ConfigureRateLimiting, ConfigureSwagger,
// SeedDefaultCashRegisterAsync, UseDocsOrTransportSecurity) são wiring de DI/middleware/host que só
// fazem sentido validados via WebApplicationFactory (uma camada de teste de integração que este
// projeto não usa hoje) — forçá-las em teste unitário via reflection produziria exatamente o teste
// frágil e acoplado à infraestrutura de hospedagem que o cartão pede pra evitar.
public sealed class ProgramTests
{
    private static MethodInfo GetLocalFunction(string name)
        => typeof(Program)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name.Contains($"g__{name}|"));

    private static void InvokeValidateJwtSecret(IConfiguration configuration)
    {
        try
        {
            GetLocalFunction("ValidateJwtSecret").Invoke(null, [configuration]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static void InvokeValidateDatabaseConnectionString(IConfiguration configuration)
    {
        try
        {
            GetLocalFunction("ValidateDatabaseConnectionString").Invoke(null, [configuration]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateJwtSecret_MissingOrBlank_ShouldThrow(string? secret)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["Jwt:Secret"] = secret });

        var act = () => InvokeValidateJwtSecret(configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void ValidateJwtSecret_StillContainingPlaceholder_ShouldThrow()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "TROCAR-ESTE-SEGREDO-POR-VARIAVEL-DE-AMBIENTE-COM-64-CHARS!!"
        });

        var act = () => InvokeValidateJwtSecret(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateJwtSecret_ShorterThan32Chars_ShouldThrow()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('a', 31) });

        var act = () => InvokeValidateJwtSecret(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateJwtSecret_ValidSecret_ShouldNotThrow()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('a', 64) });

        var act = () => InvokeValidateJwtSecret(configuration);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateDatabaseConnectionString_MissingOrBlank_ShouldThrow(string? connectionString)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString
        });

        var act = () => InvokeValidateDatabaseConnectionString(configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings*");
    }

    [Fact]
    public void ValidateDatabaseConnectionString_StillContainingPlaceholder_ShouldThrow()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Server=x;Uid=x;Pwd=TROCAR-VIA-VARIAVEL-DE-AMBIENTE;"
        });

        var act = () => InvokeValidateDatabaseConnectionString(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateDatabaseConnectionString_RealConnectionString_ShouldNotThrow()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=x;Uid=x;Pwd=RealPassword123;"
        });

        var act = () => InvokeValidateDatabaseConnectionString(configuration);

        act.Should().NotThrow();
    }
}
