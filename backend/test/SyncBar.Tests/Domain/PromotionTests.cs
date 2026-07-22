using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain;

public sealed class PromotionTests
{
    // Quarta da caipirinha: 16:00-20:00 (960-1200)
    private static Promotion QuartaDaCaipirinha()
        => Promotion.Create(1, 3, "Quarta da caipirinha", 3, 960, 1200).Value;

    [Theory]
    [InlineData("2026-07-15T16:00:00", true)]   // quarta 16:00 — comeca
    [InlineData("2026-07-15T19:59:00", true)]   // quarta 19:59 — dentro
    [InlineData("2026-07-15T20:00:00", false)]  // quarta 20:00 — janela fechou
    [InlineData("2026-07-15T15:59:00", false)]  // quarta antes das 16
    [InlineData("2026-07-16T17:00:00", false)]  // quinta no horario — dia errado
    public void IsActiveAt_ShouldRespectDayAndWindow(string localNow, bool expected)
        => QuartaDaCaipirinha().IsActiveAt(DateTime.Parse(localNow)).Should().Be(expected);

    [Fact]
    public void Create_WithStartAfterEnd_ShouldFail()
    {
        var result = Promotion.Create(1, 3, "Invalida", 3, 1200, 960);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Promotion.InvalidWindow");
    }
}

public sealed class PromotionDiscountTests
{
    [Fact]
    public void Create_DiscountWithoutRate_ShouldFail()
    {
        var result = Promotion.Create(1, 9, "Domingo -25%", 0, 0, 1440, PromotionTypeIds.Desconto, null);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Promotion.InvalidDiscount");
    }

    [Fact]
    public void Create_DiscountWithValidRate_ShouldSucceed()
    {
        var result = Promotion.Create(1, 9, "Domingo -25%", 0, 0, 1440, PromotionTypeIds.Desconto, 0.25m);
        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountRate.Should().Be(0.25m);
        result.Value.PromotionTypeId.Should().Be(PromotionTypeIds.Desconto);
    }
}
