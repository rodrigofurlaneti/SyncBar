using System.Net;
using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodFinancialClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IfoodFinancialClient _client;

    public IfoodFinancialClientTests()
    {
        _client = new IfoodFinancialClient(new HttpClient(_handler));
    }

    [Fact]
    public async Task GetFinancialEventsAsync_SingleMonth_ShouldQueryOneCompetenceAndMapEvents()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            [{"id":"evt-1","name":"DELIVERY_FEE","description":"Taxa de entrega","trigger":"ORDER","amount":10.5,
              "hasTransferImpact":true,"competenceDate":"2026-01-15","periodStartDate":"2026-01-01","periodEndDate":"2026-01-31",
              "settlementExpectedDate":"2026-02-05","reference":{"type":"ORDER","id":"order-1"}}]
            """);

        var events = await _client.GetFinancialEventsAsync("tok", "MERCH-1", new DateTime(2026, 1, 10), new DateTime(2026, 1, 20), CancellationToken.None);

        events.Should().ContainSingle();
        var evt = events.Single();
        evt.Id.Should().Be("evt-1");
        evt.Amount.Should().Be(10.5m);
        evt.HasTransferImpact.Should().BeTrue();
        evt.ReferenceType.Should().Be("ORDER");
        _handler.Requests.Should().ContainSingle();
        _handler.Requests[0].RequestUri!.ToString().Should().Contain("competence=2026-01");
    }

    [Fact]
    public async Task GetFinancialEventsAsync_MultiMonthRange_ShouldQueryOnePerDistinctMonth()
    {
        _handler.DefaultResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };

        await _client.GetFinancialEventsAsync("tok", "MERCH-1", new DateTime(2026, 1, 20), new DateTime(2026, 3, 5), CancellationToken.None);

        _handler.Requests.Should().HaveCount(3);
        _handler.Requests[0].RequestUri!.ToString().Should().Contain("competence=2026-01");
        _handler.Requests[1].RequestUri!.ToString().Should().Contain("competence=2026-02");
        _handler.Requests[2].RequestUri!.ToString().Should().Contain("competence=2026-03");
    }

    [Fact]
    public async Task GetFinancialEventsAsync_HttpFailure_ShouldReturnEmpty()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var events = await _client.GetFinancialEventsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFinancialEventsAsync_WrappedInDataKey_ShouldStillResolveArray()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"data":[{"id":"evt-1","name":"X","amount":1}]}""");

        var events = await _client.GetFinancialEventsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        events.Should().ContainSingle(e => e.Id == "evt-1");
    }

    [Fact]
    public async Task GetFinancialEventsAsync_MinimalFields_ShouldFallBackToDefaults()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{}]""");

        var events = await _client.GetFinancialEventsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        events.Should().ContainSingle();
        events.Single().Name.Should().Be("UNKNOWN");
        events.Single().Amount.Should().Be(0m);
    }

    [Fact]
    public async Task GetSettlementsAsync_NestedClosingItems_ShouldFlattenIntoSettlementList()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"beginDate":"2026-01-01","endDate":"2026-01-31","balance":100,"merchantId":"MERCH-1",
             "settlements":[{"startDateCalculation":"2026-01-01","endDateCalculation":"2026-01-15","closingItems":[
                {"id":"s-1","type":"REPASSE","product":"FOOD","amount":50,"status":"PAID","paymentDate":"2026-01-16",
                 "accountDetails":{"bankCode":"001","agency":"1234","account":"56789"}}
             ]}]}
            """);

        var settlements = await _client.GetSettlementsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        settlements.Should().ContainSingle();
        var s = settlements.Single();
        s.Id.Should().Be("s-1");
        s.Amount.Should().Be(50);
        s.BankCode.Should().Be("001");
        var url = _handler.Requests[^1].RequestUri!.ToString();
        url.Should().Contain("/settlements").And.Contain("beginCalculationDate=").And.Contain("endCalculationDate=");
    }

    // O teste acima só manda "amount" como número JSON — GetDecimal também aceita o valor vindo
    // como string (fallback decimal.TryParse), ramo nunca exercitado até agora. Valor inteiro (sem
    // separador decimal) de propósito: decimal.TryParse aqui usa a cultura corrente do processo,
    // então um separador fracionário fixo ("." ou ",") tornaria o teste dependente da cultura do
    // ambiente onde a suíte roda.
    [Fact]
    public async Task GetSettlementsAsync_AmountAsString_ShouldParseSuccessfully()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"settlements":[{"startDateCalculation":"2026-01-01","closingItems":[
                {"id":"s-2","type":"REPASSE","amount":"75","status":"PAID"}
             ]}]}
            """);

        var settlements = await _client.GetSettlementsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        settlements.Should().ContainSingle(s => s.Id == "s-2" && s.Amount == 75m);
    }

    // GetDecimal(item, "amount", "value") só segue pro segundo nome candidato se o primeiro
    // existir mas não bater nem como número nem como string parseável — nenhum teste até agora
    // tinha um item com "amount" de tipo inesperado E um "value" numérico como alternativa válida.
    [Fact]
    public async Task GetSettlementsAsync_AmountWrongTypeWithValueFallback_ShouldUseValue()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"settlements":[{"startDateCalculation":"2026-01-01","closingItems":[
                {"id":"s-3","type":"REPASSE","amount":true,"value":30,"status":"PAID"}
             ]}]}
            """);

        var settlements = await _client.GetSettlementsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        settlements.Should().ContainSingle(s => s.Id == "s-3" && s.Amount == 30m);
    }

    [Fact]
    public async Task GetSettlementsAsync_PeriodWithoutClosingItems_ShouldBeSkipped()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"settlements":[{"startDateCalculation":"2026-01-01"}]}""");

        var settlements = await _client.GetSettlementsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        settlements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSettlementsAsync_LegacyFlatArrayFormat_ShouldFallBackToFlatParsing()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"s-1","type":"REPASSE","amount":10,"status":"PAID"}]""");

        var settlements = await _client.GetSettlementsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        settlements.Should().ContainSingle(s => s.Id == "s-1");
    }

    [Fact]
    public async Task GetSettlementsAsync_UnexpectedFormat_ShouldReturnEmpty()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"unexpected":"shape"}""");

        var settlements = await _client.GetSettlementsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        settlements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSettlementsAsync_HttpFailure_ShouldReturnEmpty()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var settlements = await _client.GetSettlementsAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, CancellationToken.None);

        settlements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAnticipationsAsync_ArrayResponse_ShouldReturnRawItemsPerElement()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"a-1"},{"id":"a-2"}]""");

        var result = await _client.GetAnticipationsAsync("tok", "MERCH-1", CancellationToken.None);

        result.RawItems.Should().HaveCount(2);
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("anticipations");
    }

    [Fact]
    public async Task GetAnticipationsAsync_SingleObjectResponse_ShouldReturnOneRawItem()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"totalAnticipated":100}""");

        var result = await _client.GetAnticipationsAsync("tok", "MERCH-1", CancellationToken.None);

        result.RawItems.Should().ContainSingle();
    }

    [Fact]
    public async Task GetAnticipationsAsync_HttpFailure_ShouldReturnEmptyResult()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetAnticipationsAsync("tok", "MERCH-1", CancellationToken.None);

        result.RawItems.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSalesV3Async_ShouldIncludeDateRangeAndPageInQuery()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[]""");

        await _client.GetSalesV3Async("tok", "MERCH-1", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), 2, CancellationToken.None);

        var url = _handler.Requests[^1].RequestUri!.ToString();
        url.Should().Contain("/sales").And.Contain("beginSalesDate=2026-01-01").And.Contain("endSalesDate=2026-01-31").And.Contain("page=2");
    }

    [Fact]
    public async Task RequestReconciliationOnDemandAsync_Success_ShouldPostCompetenceAndReturnRequestId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"requestId":"req-1"}""");

        var result = await _client.RequestReconciliationOnDemandAsync("tok", "MERCH-1", "2026-01", CancellationToken.None);

        result.RequestId.Should().Be("req-1");
        _handler.RequestBodies[^1].Should().Contain("2026-01");
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("reconciliation/on-demand");
    }

    [Fact]
    public async Task RequestReconciliationOnDemandAsync_ResponseUsesIdInsteadOfRequestId_ShouldFallBackToId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"req-2"}""");

        var result = await _client.RequestReconciliationOnDemandAsync("tok", "MERCH-1", "2026-01", CancellationToken.None);

        result.RequestId.Should().Be("req-2");
    }

    [Fact]
    public async Task RequestReconciliationOnDemandAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "competencia invalida");

        var act = () => _client.RequestReconciliationOnDemandAsync("tok", "MERCH-1", "invalid", CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*400*competencia invalida*");
    }

    [Fact]
    public async Task RequestReconciliationOnDemandAsync_UnparsableBody_ShouldReturnEmptyRequestId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "not-json");

        var result = await _client.RequestReconciliationOnDemandAsync("tok", "MERCH-1", "2026-01", CancellationToken.None);

        result.RequestId.Should().BeEmpty();
        result.RawPayload.Should().Be("not-json");
    }

    [Fact]
    public async Task GetReconciliationOnDemandStatusAsync_Success_ShouldReturnRawBody()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"status":"PROCESSING"}""");

        var result = await _client.GetReconciliationOnDemandStatusAsync("tok", "MERCH-1", "req-1", CancellationToken.None);

        result.Should().Be("""{"status":"PROCESSING"}""");
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("reconciliation/on-demand/req-1");
    }

    [Fact]
    public async Task GetReconciliationOnDemandStatusAsync_Failure_ShouldReturnNull()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "");

        var result = await _client.GetReconciliationOnDemandStatusAsync("tok", "MERCH-1", "req-missing", CancellationToken.None);

        result.Should().BeNull();
    }

    public static TheoryData<IfoodFinancialReportType, string> ReportTypeUrlCases()
    {
        var data = new TheoryData<IfoodFinancialReportType, string>
        {
            { IfoodFinancialReportType.SalesAdjustments, "salesAdjustments" },
            { IfoodFinancialReportType.Payments, "payments" },
            { IfoodFinancialReportType.PaymentDetails, "paymentDetails" },
            { IfoodFinancialReportType.Occurrences, "occurrences" },
            { IfoodFinancialReportType.MaintenanceFees, "maintenanceFees" },
            { IfoodFinancialReportType.IncomeTaxes, "incomeTaxes" },
            { IfoodFinancialReportType.Periods, "periods" },
            { IfoodFinancialReportType.ChargeCancellations, "chargeCancellations" },
            { IfoodFinancialReportType.Cancellations, "cancellations" },
            { IfoodFinancialReportType.ReceivableRecords, "receivableRecords" },
            { IfoodFinancialReportType.SalesBenefits, "salesBenefits" },
            { IfoodFinancialReportType.AdjustmentsBenefits, "adjustmentsBenefits" },
            { IfoodFinancialReportType.SalesV21, "sales" },
            { IfoodFinancialReportType.AnticipationsV3, "anticipations" },
            { IfoodFinancialReportType.SalesV3, "sales" }
        };
        return data;
    }

    [Theory]
    [MemberData(nameof(ReportTypeUrlCases))]
    public async Task GetReportAsync_EachReportType_ShouldHitExpectedPath(IfoodFinancialReportType reportType, string expectedPathSegment)
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[]""");

        await _client.GetReportAsync("tok", "MERCH-1", reportType, "period-1", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().Contain($"/{expectedPathSegment}");
    }

    [Fact]
    public async Task GetReportAsync_SalesAdjustmentsWithPeriodAndDates_ShouldIncludeAllQueryParams()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[]""");

        await _client.GetReportAsync("tok", "MERCH-1", IfoodFinancialReportType.SalesAdjustments, "period-1", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), CancellationToken.None);

        var url = _handler.Requests[^1].RequestUri!.ToString();
        url.Should().Contain("periodId=period-1").And.Contain("beginUpdateDate=2026-01-01").And.Contain("endUpdateDate=2026-01-31");
    }

    [Fact]
    public async Task GetReportAsync_WithoutOptionalFilters_ShouldOmitQueryString()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[]""");

        await _client.GetReportAsync("tok", "MERCH-1", IfoodFinancialReportType.AnticipationsV3, null, null, null, CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().NotContain("?");
    }

    [Fact]
    public async Task GetReportAsync_ArrayResponse_ShouldReturnOneRawItemPerElement()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"1"},{"id":"2"},{"id":"3"}]""");

        var result = await _client.GetReportAsync("tok", "MERCH-1", IfoodFinancialReportType.Payments, null, null, null, CancellationToken.None);

        result.RawItems.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetReportAsync_HttpFailure_ShouldReturnEmptyResult()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetReportAsync("tok", "MERCH-1", IfoodFinancialReportType.Periods, null, null, null, CancellationToken.None);

        result.RawItems.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReportAsync_InvalidReportType_ShouldThrowArgumentOutOfRange()
    {
        var invalidType = (IfoodFinancialReportType)999;

        var act = () => _client.GetReportAsync("tok", "MERCH-1", invalidType, null, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
