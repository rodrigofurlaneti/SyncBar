# Integrações — validação e correções em andamento

Data: 08/09/2026. Revisão do código e contratos oficiais. Não equivale a homologação com lojas reais. Nenhum pedido, entregador ou resposta a cliente foi enviado aos provedores durante esta revisão.

## Resultado da inspeção inicial

As integrações não estão completas. A existência de controllers e testes unitários não comprova o funcionamento ponta a ponta. Os itens abaixo distinguem lacunas encontradas de correções verificadas posteriormente.

| Módulo | Evidência encontrada | Pendências de aceite |
|---|---|---|
| iFood Order/Events | Worker, polling Events v1, acknowledgment, detalhes e ações de status existem | `ORDER_PATCHED`, `ASSIGN_DRIVER` e handshake caem em retorno de sucesso sem processamento; 404 não tem backoff limitado; falha de confirmação não é reexecutada quando o pedido já existe; prazo usa importação local; notas/documento não são integralmente importados; status do pedido interno não acompanha todas as transições; prescrições ausentes |
| iFood Merchant | Listagem/detalhes/status, interrupções, PUT de horários e alteração de preparo existem | Worker era de 5 minutos; MyPreparationTime enviava objeto em vez de inteiro; GET de preparo ausente; leitura de horários é apenas local; não valida sobreposição localmente; `reopenable` descartado; check-in PDF ausente |
| iFood Shipping | Cotação, solicitação nos dois modos e consulta manual de tracking existem | Backup não valida DELIVERY/fullservice nem persiste vínculo logístico; envio externo recebe dados manuais em vez de montar a partir de CustomerOrder/CustomerAddress; tracking consultava a cada 15 segundos; não há worker condicionado a atribuição nem persistência completa de posição/ETA; 404 era exibido como falha |
| iFood Catalog | Cliente v2, descoberta de catálogo, mapeamentos persistidos, PUT de itens com arrays e preços existem | Confirmar ligação product/optionGroups e optionGroupType; IDs v4 dos mapeamentos e gatilhos POS devem ser testados; criação usa rota com catalogId (contrato deve ser conferido, não substituir pelo cartão sem verificar); tratamento 404/409 é genérico |
| iFood Review | Cliente de quatro endpoints e tela existem | Usa v1.0; backend aceita até 2.000 caracteres e tela até 500; não trata replies/status/visibility da v2; não bloqueia prazos de 5 dias/10 minutos; falhas de consulta podem parecer lista vazia |
| iFood Analytics | Consulta manual de agregados existe | Não há extração diária, armazenamento de snapshots ou recálculo retroativo; período padrão inclui hoje; erros HTTP viram agregados vazios; escopos e categoria do app precisam ser confirmados |
| Keeta | OAuth app-level, ações de pedido, polling e webhooks existem | Sem assinatura de saída; webhook valida somente corpo, divergindo do contrato URL/query/corpo; polling confirmava falhas antes do commit; evento existente com falha era tratado como sucesso; pipeline de importação de pedido ausente; onboarding/menu/status/upload/batchDecrypt ausentes; shop-level ausente |

## Correções iniciadas nesta revisão

- Merchant: intervalo de status alterado para 30 segundos, corpo MyPreparationTime como inteiro e leitura de mensagens de validação em objeto.
- Shipping: telas alteradas para 30 segundos, sem retry imediato/refetch por foco; 404 de tracking passa a representar posição ainda indisponível. Isso não substitui o controle por pedido no backend nem o worker pendente.
- Keeta: assinatura de saída para clientes existentes, URLs absolutas para evitar alterar BaseAddress após primeiro envio, acknowledgment somente de sucessos após commit, reprocessamento de logs falhos e resposta 503 em falha do webhook.
- Testes específicos em andamento. Registrar resultados finais antes de considerar qualquer módulo concluído.

## Arquitetura de Events

### Atualização verificada — Shipping

- Solicitação de apoio à frota pela tela de pedidos usa Shipping, com consulta de cotação, exibição de custo e confirmação. Backend bloqueia pedidos não DELIVERY, encerrados ou sem entrega MERCHANT e consulta disponibilidade antes do POST. A cotação escolhida pelo operador continua sendo enviada ao provedor.
- Solicitações externas validam o formulário e as coordenadas e verificam cobertura antes de criar a entrega. Ainda recebem os dados manualmente; a montagem automática a partir de CustomerOrder/CustomerAddress permanece pendente.
- ASSIGN_DRIVER e REQUEST_DRIVER_SUCCESS passam a registrar rastreamento persistente antes do ACK. Eventos terminais encerram esse rastreamento; repetição da atribuição não reativa uma entrega encerrada.
- Worker consulta posições a cada 30 segundos com reserva atômica no banco por pedido. As telas leem snapshots, sem provocar chamadas adicionais ao tracking remoto. 404 significa posição pendente; estimativas recebidas em segundos são convertidas para minutos.
- Testes com SQLite verificam persistência, isolamento por empresa, repetição de eventos, espera pela atribuição, intervalo entre consultas por duas instâncias e parada no encerramento. Isso não substitui validação de concorrência e migração em MySQL nem homologação no iFood.
- Validação atual: 6.471 testes do backend aprovados; TypeScript compilado. SonarQube, execução visual do novo fluxo e integração real não executados nesta etapa.
- **Antes de publicar:** aplicar `sql/2026-09-08_add_ifood_shipping_tracking.sql` no banco correto. Não foi aplicado nesta revisão. A ausência da tabela impede o worker e as consultas de snapshots.
- Pendências: vínculo automático dos pedidos externos com o POS, reconciliação de todos os eventos logísticos com o status exibido da entrega, validação dos filtros/categorias com pedidos sob demanda reais e homologação da contratação/cobertura. Não marcar o cartão inteiro como 100% concluído.

Manter polling como método operacional atual. Ele já está registrado como BackgroundService e não depende de configurar entrada pública. Endpoints usados: `/events/v1.0/events:polling` e `/events/v1.0/events/acknowledgment`. Webhook iFood não foi habilitado no Portal. A decisão não deve gerar ingestão duplicada sem armazenamento idempotente persistente.

## Analytics

Modelo proposto: por merchant, compatível com o mapeamento local existente. A configuração efetiva no iFood não foi inspecionada. Exigir `analytics` e `merchant_scope`, sem misturar `chain_scope`. Planejar execução depois de 09h em America/Sao_Paulo, D-1 e atualização de 15 dias anteriores. Cancelamentos tardios devem substituir snapshots, não somar duplicadamente.

## Fontes e limites

- [iFood Order](https://developer.ifood.com.br/pt-BR/docs/guides/modules/order/workflow/)
- [iFood Merchant](https://developer.ifood.com.br/pt-BR/docs/guides/modules/merchant/workflow/)
- [MyPreparationTime — contrato](https://developer.ifood.com.br/en-US/docs/guides/modules/logistics/endpoints)
- [Shipping](https://developer.ifood.com.br/pt-BR/docs/guides/modules/shipping/intro/)
- [Catalog v2](https://developer.ifood.com.br/pt-BR/docs/guides/modules/catalog/introduction)
- [Review v2](https://developer.ifood.com.br/pt-BR/docs/guides/modules/review/endpoints)
- [Analytics](https://developer.ifood.com.br/en-US/docs/food/guides/modules/analytics/intro)
- [Keeta Open Delivery](https://api-docs.mykeeta.com/apis/opendelivery)
- [Assinatura Keeta](https://api-docs.mykeeta.com/apis/opendelivery/signature-calculation)
- OpenAPI fornecida: `C:\Users\AMD\Downloads\opendelivery (2).json` e `.yaml` (22 paths).

Ainda não executados: homologação iFood, Test Store Tool/Keeta Test App, escopos reais, deploy, alterações no portal, migrações em banco real e SonarQube. Não declarar operação 100% com base somente nos mocks.
