using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Application.Abstractions.Integrations.IFood;

public sealed class IIFoodOperationalAlertStoreDtoTests
{
    [Fact]
    public void IfoodOperationalAlert_ShouldExposeConstructorValues()
    {
        var id = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);

        var alert = new IfoodOperationalAlert(
            Id: id, CompanyId: 1, BranchId: 2, BranchName: "Filial 1", Title: "Loja indisponível",
            Message: "A loja ficou indisponível no iFood.", Severity: IfoodOperationalAlertSeverity.Warning,
            CreatedAtUtc: createdAtUtc);

        alert.Should().BeEquivalentTo(new
        {
            Id = id,
            CompanyId = 1L,
            BranchId = 2L,
            BranchName = "Filial 1",
            Title = "Loja indisponível",
            Message = "A loja ficou indisponível no iFood.",
            Severity = IfoodOperationalAlertSeverity.Warning,
            CreatedAtUtc = createdAtUtc,
        });
    }
}
