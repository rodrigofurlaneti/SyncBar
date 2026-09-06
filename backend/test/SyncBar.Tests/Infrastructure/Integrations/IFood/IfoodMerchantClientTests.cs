using System.Net;
using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodMerchantClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IfoodMerchantClient _client;

    public IfoodMerchantClientTests()
    {
        _client = new IfoodMerchantClient(new HttpClient(_handler));
    }

    [Fact]
    public async Task GetStatusAsync_ArrayResponse_ShouldUseFirstItemAndMapValidations()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            [{"state":"OK","available":true,"validations":[{"id":"v-1","state":"OK","message":"tudo certo"}]}]
            """);

        var result = await _client.GetStatusAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OperationState.Should().Be("OK");
        result.Available.Should().BeTrue();
        result.Validations.Should().ContainSingle(v => v.Id == "v-1" && v.Message == "tudo certo");
    }

    [Fact]
    public async Task GetStatusAsync_ObjectResponse_ShouldTreatRootAsStatus()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"status":"OK","available":false}""");

        var result = await _client.GetStatusAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OperationState.Should().Be("OK");
        result.Available.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatusAsync_Failure_ShouldReturnFailureResult()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetStatusAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Validations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatusAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetStatusAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task GetInterruptionsAsync_Success_ShouldMapEntriesAndSkipMissingId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"interruptions":[
                {"interruptionId":"int-1","description":"Manutencao","start":"2026-01-01T10:00:00Z","end":"2026-01-01T12:00:00Z"},
                {"description":"sem id"}
            ]}
            """);

        var result = await _client.GetInterruptionsAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Interruptions.Should().ContainSingle(i => i.Id == "int-1" && i.Description == "Manutencao");
    }

    [Fact]
    public async Task GetInterruptionsAsync_Failure_ShouldReturnEmptyFailureResult()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetInterruptionsAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Interruptions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateInterruptionAsync_Success_ShouldPostPayloadAndReturnId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"int-new"}""");

        var result = await _client.CreateInterruptionAsync("tok", "MERCH-1", "Manutencao", new DateTime(2026, 1, 1, 10, 0, 0), new DateTime(2026, 1, 1, 12, 0, 0), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.InterruptionId.Should().Be("int-new");
        _handler.RequestBodies[^1].Should().Contain("Manutencao");
    }

    [Fact]
    public async Task CreateInterruptionAsync_UnparsableSuccessBody_ShouldStillSucceedWithoutId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.CreateInterruptionAsync("tok", "MERCH-1", "x", DateTime.Today, DateTime.Today, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.InterruptionId.Should().BeNull();
    }

    [Fact]
    public async Task CreateInterruptionAsync_Failure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "periodo invalido");

        var result = await _client.CreateInterruptionAsync("tok", "MERCH-1", "x", DateTime.Today, DateTime.Today, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("periodo invalido");
    }

    [Fact]
    public async Task DeleteInterruptionAsync_Success_ShouldSendDeleteToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.DeleteInterruptionAsync("tok", "MERCH-1", "int-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].Method.Should().Be(HttpMethod.Delete);
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("interruptions/int-1");
    }

    [Fact]
    public async Task GetOpeningHoursAsync_Success_ShouldMapValidShiftsAndSkipInvalidOnes()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"shifts":[
                {"dayOfWeek":"MONDAY","start":"08:00","duration":600},
                {"dayOfWeek":"INVALID_DAY","start":"08:00","duration":600},
                {"dayOfWeek":"TUESDAY","start":"08:00","duration":0}
            ]}
            """);

        var result = await _client.GetOpeningHoursAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Shifts.Should().ContainSingle(s => s.DayOfWeek == 1 && s.DurationMinutes == 600);
    }

    [Fact]
    public async Task GetOpeningHoursAsync_Failure_ShouldReturnEmptyFailureResult()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetOpeningHoursAsync("tok", "MERCH-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Shifts.Should().BeEmpty();
    }

    [Fact]
    public async Task SetOpeningHoursAsync_ShouldPutShiftsInIfoodDayNameFormat()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");
        var shifts = new[] { new IfoodOpeningHourShift(3, new TimeSpan(9, 30, 0), 480) };

        var result = await _client.SetOpeningHoursAsync("tok", "MERCH-1", shifts, CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].Method.Should().Be(HttpMethod.Put);
        var body = _handler.RequestBodies[^1]!;
        body.Should().Contain("WEDNESDAY").And.Contain("09:30").And.Contain("480");
    }

    [Fact]
    public async Task UpsertPreparationTimeAsync_PutSucceeds_ShouldNotFallBackToPost()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.UpsertPreparationTimeAsync("tok", "MERCH-1", "cust-1", 15, CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests.Should().ContainSingle();
        _handler.Requests[0].Method.Should().Be(HttpMethod.Put);
        _handler.Requests[0].Headers.GetValues("X-Ifood-Customer-ID").Should().ContainSingle().Which.Should().Be("cust-1");
    }

    [Fact]
    public async Task UpsertPreparationTimeAsync_PutReturns404_ShouldRetryWithPost()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao configurado");
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.UpsertPreparationTimeAsync("tok", "MERCH-1", "cust-1", 15, CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests.Should().HaveCount(2);
        _handler.Requests[0].Method.Should().Be(HttpMethod.Put);
        _handler.Requests[1].Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task UpsertPreparationTimeAsync_PutFailsWithNon404_ShouldNotRetryAndReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "tempo invalido");

        var result = await _client.UpsertPreparationTimeAsync("tok", "MERCH-1", "cust-1", -5, CancellationToken.None);

        result.Success.Should().BeFalse();
        _handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task DeletePreparationTimeAsync_Success_ShouldSendDeleteWithCustomerHeader()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.DeletePreparationTimeAsync("tok", "MERCH-1", "cust-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].Method.Should().Be(HttpMethod.Delete);
        _handler.Requests[^1].Headers.GetValues("X-Ifood-Customer-ID").Should().ContainSingle().Which.Should().Be("cust-1");
    }

    [Fact]
    public async Task ListMerchantsAsync_Success_ShouldFilterOutEntriesWithoutId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"id":"m-1","name":"Loja 1"},{"id":"","name":"Sem id"}]""");

        var result = await _client.ListMerchantsAsync("tok", 1, 100, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Merchants.Should().ContainSingle(m => m.Id == "m-1");
        _handler.Requests[^1].RequestUri!.ToString().Should().Contain("page=1&size=100");
    }

    [Fact]
    public async Task ListMerchantsAsync_Failure_ShouldReturnFailureResult()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.ListMerchantsAsync("tok", cancellationToken: CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Merchants.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMerchantDetailsAsync_SuccessWithAddress_ShouldMapAllFields()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"id":"m-1","name":"Loja Centro","corporateName":"Loja Centro LTDA","description":"desc","type":"RESTAURANT",
             "status":"AVAILABLE","createdAt":"2020-01-01T00:00:00Z",
             "address":{"country":"BR","state":"SP","city":"Sao Paulo","postalCode":"01310000","district":"Bela Vista",
                        "street":"Av Paulista","number":"100","latitude":-23.5,"longitude":-46.6}}
            """);

        var result = await _client.GetMerchantDetailsAsync("tok", "m-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Name.Should().Be("Loja Centro");
        result.Address.Should().NotBeNull();
        result.Address!.City.Should().Be("Sao Paulo");
    }

    [Fact]
    public async Task GetMerchantDetailsAsync_SuccessWithoutAddress_ShouldReturnNullAddress()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"m-1","name":"Loja"}""");

        var result = await _client.GetMerchantDetailsAsync("tok", "m-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Address.Should().BeNull();
    }

    [Fact]
    public async Task GetMerchantDetailsAsync_Failure_ShouldReturnFailureResult()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao encontrado");

        var result = await _client.GetMerchantDetailsAsync("tok", "m-missing", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetMerchantDetailsAsync_NullBody_ShouldReturnEmptyResponseFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "null");

        var result = await _client.GetMerchantDetailsAsync("tok", "m-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("vazia");
    }

    [Fact]
    public async Task GetStatusByOperationAsync_Success_ShouldMapAllFieldsAndObjectMessage()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"operation":"DELIVERY","salesChannel":"IFOOD","available":true,"state":"OK",
             "validations":[{"id":"v-1","state":"WARN","message":{"description":"aviso importante"}}]}
            """);

        var result = await _client.GetStatusByOperationAsync("tok", "MERCH-1", "DELIVERY", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Operation.Should().Be("DELIVERY");
        result.Available.Should().BeTrue();
        result.Validations.Should().ContainSingle(v => v.Message == "aviso importante");
    }

    [Fact]
    public async Task GetStatusByOperationAsync_ValidationMessageAsPlainString_ShouldUseItDirectly()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"operation":"TAKEOUT","validations":[{"id":"v-1","state":"OK","message":"tudo certo"}]}
            """);

        var result = await _client.GetStatusByOperationAsync("tok", "MERCH-1", "TAKEOUT", CancellationToken.None);

        result.Validations.Should().ContainSingle(v => v.Message == "tudo certo");
    }

    [Fact]
    public async Task GetStatusByOperationAsync_Failure_ShouldReturnFailureResult()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetStatusByOperationAsync("tok", "MERCH-1", "DELIVERY", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Validations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatusByOperationAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetStatusByOperationAsync("tok", "MERCH-1", "DELIVERY", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }
}
