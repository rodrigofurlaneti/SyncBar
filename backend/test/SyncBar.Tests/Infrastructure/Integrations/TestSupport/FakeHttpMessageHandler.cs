using System.Net;

namespace SyncBar.Tests.Infrastructure.Integrations.TestSupport;

// Fake de HttpMessageHandler pra testar unitariamente os clientes HTTP de integração (Asaas/
// Ifood/Keeta) sem bater na API real. Cada handler guarda as requisições recebidas (pra
// assertions de método/URL/corpo) e devolve as respostas enfileiradas via Enqueue, na ordem em
// que chegam — se a fila esvaziar, devolve DefaultResponseFactory (200 vazio por padrão).
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string?> RequestBodies { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage> DefaultResponseFactory { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };

    public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(_ => response);

    public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) => _responses.Enqueue(responseFactory);

    public void EnqueueJson(HttpStatusCode statusCode, string json) =>
        Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));

        var factory = _responses.Count > 0 ? _responses.Dequeue() : DefaultResponseFactory;
        return factory(request);
    }
}
