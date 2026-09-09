# Recebimento de eventos iFood

O SyncBar permite selecionar **Polling** ou **Webhook** em Integração iFood → Credenciais → Recebimento de eventos. O padrão é Polling. A escolha pertence à empresa e não altera configurações do Asaas.

## Rota exclusiva

`POST /api/webhook/ifood`

Cadastre no aplicativo centralizado do iFood uma URL pública HTTPS, por exemplo `https://seu-dominio/api/webhook/ifood`. A interface apresenta a URL considerando o domínio pelo qual o frontend foi acessado, preservando a porta; confirme que o proxy encaminha `/api/` à API. No ambiente HTTP atual, o endereço é `http://9.205.156.87:84/api/webhook/ifood`, mas é necessário disponibilizar HTTPS para cadastrar no iFood.

A rota identifica a empresa pela assinatura válida do aplicativo e pelo merchant vinculado. Um merchant associado a mais de uma empresa autenticada é rejeitado com 503 para evitar encaminhamento ambíguo. KEEPALIVE por merchant agrega as lojas das empresas autenticadas. A rota anterior com `/{companyId}` continua disponível para compatibilidade.

Não usar a URL do Swagger ou uma rota do Asaas. A URL HTTP atual do ambiente não atende ao requisito HTTPS do iFood.

## Publicação e ativação

1. A migration `202609090001_AddIfoodWebhook` aplica na inicialização da API a coluna de modo e a tabela da fila. Ela também aceita bancos onde o SQL avulso já foi executado. O banco existente precisa conter a tabela `IfoodIntegrationSetting`; esta é uma atualização incremental, não a criação de todo o sistema.
2. Publicar API/frontend e verificar `/health`. Garantir persistência das chaves Data Protection usadas para descriptografar o client secret já cadastrado.
3. Conferir credenciais, integração ativa e merchants vinculados. O segredo da assinatura é o client secret do aplicativo; não vai na URL nem é devolvido pelo frontend.
4. Salvar Webhook no SyncBar e cadastrar/ativar a URL no Portal do Desenvolvedor iFood. Coordenar a troca: o polling de eventos cessa ao salvar o modo Webhook.
5. Preferir presença **por merchant**. A resposta inclui somente merchants vinculados a filiais ativas. No modo por aplicativo, um `202` anuncia todos os merchants do aplicativo no iFood; usar apenas quando esse aplicativo é integralmente atendido por esta empresa.
6. Testar conexão, pedido, repetição do evento e atualização de status em loja de teste. Nenhuma ativação, publicação ou homologação real foi feita nesta implementação.

Para retornar: salvar Polling no SyncBar e desativar o webhook no portal. Eventos já armazenados continuam na fila; não são descartados pela troca de modo.

## Contrato e processamento

- Header obrigatório `X-IFood-Signature`: HMAC-SHA256 hexadecimal calculado sobre os bytes exatos do corpo. Comparação em tempo constante antes de interpretar JSON.
- Um evento JSON por requisição, limite de 1 MiB. Eventos de pedido devem conter `id`, `orderId`, `merchantId`, `createdAt` e código. Merchant precisa estar vinculado à empresa da rota.
- `202`: evento salvo ou duplicado já existente; presença respondida conforme modo.
- `400`: estrutura inválida; `401`: assinatura inválida; `403`: merchant não vinculado; `413`: corpo excedeu limite; `503`: modo desativado, configuração indisponível ou falha ao persistir.
- `KEEPALIVE` é autenticado e respondido diretamente; não entra na fila de pedidos.
- Polling também persiste na mesma fila antes do ACK remoto. Chave única `(CompanyId, EventId)` evita duplicação entre os transportes.
- Worker consulta pendências a cada 2 segundos, reserva cada evento no banco, usa o processador de pedidos existente e registra conclusão ou nova tentativa com espera crescente até 5 minutos. Após 20 tentativas, preserva payload/erro para revisão; não marca sucesso silenciosamente.
- Eventos recebidos por webhook não provocam polling nem ACK remoto durante o processamento assíncrono. O status financeiro e a importação seguem os handlers existentes; funcionalidades anteriormente pendentes, como ORDER_PATCHED e partes de disputas, continuam pendentes e aparecem como eventos não processados.

## Diagnóstico

Consultar, com acesso administrativo ao banco, sem expor o payload:

```sql
SELECT Id, CompanyId, EventId, ReceivedAtUtc, Attempts, LastError,
       NextAttemptAtUtc, ProcessedAtUtc
FROM ifoodeventinbox
WHERE ProcessedAtUtc IS NULL
ORDER BY ReceivedAtUtc;
```

`Attempts >= 20` exige revisão da causa antes de reprocessamento. Payloads podem conter dados pessoais; não publicar dumps ou logs completos. A reserva de processamento expira após 5 minutos para permitir recuperação de um worker interrompido. Validar concorrência e comportamento de reinício também em MySQL na homologação.

Fontes: [assinatura](https://developer.ifood.com.br/pt-BR/docs/guides/modules/events/webhook-signature), [presença](https://developer.ifood.com.br/pt-BR/docs/guides/modules/events/webhook-presence), [webhook](https://developer.ifood.com.br/pt-BR/docs/guides/modules/events/webhook-overview).
