namespace SyncBar.Infrastructure.Integrations.Asaas
{
    public class AsaasPaymentService
    {
        private readonly AsaasAuthClient _authClient;

        public AsaasPaymentService(AsaasAuthClient authClient)
        {
            _authClient = authClient;
        }

        public async Task<string> ConsultarSaldoAsync()
        {
            var response = await _authClient.Client.GetAsync("finance/balance");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}