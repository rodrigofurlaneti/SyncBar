// Este worker foi removido (Fase 12 — revisão de duplicidade no polling de eventos).
//
// Ele duplicava — com um intervalo errado (120s, e não os 30s que o iFood exige tanto para o
// SLA de 8 minutos de confirmação quanto para o heartbeat de presença da loja) e sem processar
// nenhum evento de verdade — o polling que já roda em produção desde a Fase 2:
//
//   IFoodOrderPollingBackgroundService (30s)
//     -> SyncIFoodOrdersCommand / SyncIFoodOrdersCommandHandler
//        -> detecta pedido novo (evento CONFIRMED), cria o CustomerOrder, casa itens por EAN,
//           confirma no iFood, trata cancelamento (CANCELLED), deduplica eventos, sempre faz ACK.
//
// Rodar os dois pollers ao mesmo tempo era perigoso, não só redundante: este worker fazia ACK de
// TODOS os eventos recebidos sem tratar o conteúdo. Se ele corresse antes do
// IFoodOrderPollingBackgroundService num mesmo ciclo, o evento de pedido novo seria confirmado
// (ACK) e descartado pelo iFood antes do SyncBar processar — o pedido nunca apareceria no
// sistema, silenciosamente (o iFood não reenvia evento já reconhecido).
//
// Além disso o arquivo não compilava: referenciava tipos que não existem neste projeto
// (IIFoodMerchantService, SyncBar.Application.DTOs.IFoodMerchantSummaryDto,
// PollIFoodEventsQuery/AcknowledgeIFoodEventsCommand em SyncBar.Application.Features.IFood.Events,
// além de métodos inexistentes em IIFoodIntegrationSettingRepository/IFoodIntegrationSetting —
// GetAllActiveCompaniesAsync, GetByCompanyIdAsync, Update, UpdateTokenAndMerchant).
//
// O que realmente faltava (alertar o operador de um pedido novo) foi implementado no front-end,
// em IFoodOrdersPage.tsx — a tela já reconsulta a lista de pedidos a cada 15s; agora ela compara
// com a lista anterior e toca um aviso sonoro + mostra um toast quando um pedido novo aparece.
//
// Este arquivo pode ser excluído do projeto com segurança (junto com
// SyncBar.Application/Common/Interfaces/IFood/IIFoodAuthService.cs — ver comentário lá).
