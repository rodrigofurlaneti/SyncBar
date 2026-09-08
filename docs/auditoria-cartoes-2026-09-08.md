# Auditoria dos cartões — 08/09/2026

Escopo: 13 cartões recebidos durante a revisão; textos duplicados de Mesas/Comandas consolidados. Revisão de código e execução local de testes. Nenhuma correção de produção foi aplicada nesta auditoria.

**Conclusão: não estão todos concluídos.** Os testes anteriores validavam recortes dos fluxos e não sustentam aprovação integral dos critérios apresentados.

## Evidências executadas

- Backend: lote com filtros de usuários, funcionários, categorias, turno, leitura, pedidos públicos e caixa: **578 testes aprovados**.
- Backend: lote Asaas, WhatsApp e permissões: **851 testes aprovados**. Os números são por execução, sem afirmar que todos são testes distintos entre lotes.
- Frontend: **27 testes existentes aprovados**, nas suítes PaymentMethodsTab, CashDrawer, DeliveryBoard e AccessPage, com API simulada.
- Reproduções adicionais de navegador: **3 testes aprovados verificando o comportamento atual**, incluindo dois bugs. Digitar `abc` em dinheiro conferido enviou `closingAmount: 0`; o mesmo cenário deixou Pix esperado em R$ 50 sem conferência. Salvar um toggle com PUT 204 e falhar o GET seguinte fez a tela retornar ao valor anterior. O terceiro teste verificou título e cores computadas da pesquisa do Delivery.
- Delivery: inspeção de capturas em desktop e 390 × 844. Na largura móvel, cabeçalho/controles de pesquisa ultrapassam a área visível.
- Não houve transações reais, envio de WhatsApp, homologação Asaas, execução de SonarQube ou validação do banco implantado. Imagens/PDF remotos que não puderam ser recuperados não foram tratados como evidência visual inspecionada.

## 1. Métodos de pagamento Asaas por empresa/filial — com bugs e gaps

Atendido: GET resolve, padrão ligado sem configuração, cinco toggles, cancelamento e seleção POST/PUT. Quando o GET resolve uma configuração herdada da matriz, criar a configuração da filial via POST está correto; usar o ID herdado em PUT alteraria a matriz.

- **P1 — aplicação incompleta das flags:** o seletor de novo pagamento da comanda enumera todos os métodos estáticos, sem consultar a configuração. Desativar um método não o remove desse fluxo. Ver [PaymentPanel.tsx](../frontend/src/features/orders/PaymentPanel.tsx), `Object.entries(paymentMethodLabel)`, e [PartialPaymentDialog.tsx](../frontend/src/features/orders/PartialPaymentDialog.tsx). A conferência de pagamentos históricos deve continuar mostrando modalidades recebidas, mesmo que depois desativadas.
- **P2 — estado visual reverte depois de salvar:** `onSuccess` limpa `overrides` antes de atualizar os dados usados como base. Se o refetch falha, permanece o estado antigo apesar da confirmação de sucesso. Reproduzido no navegador. [AsaasPage.tsx](../frontend/src/features/asaas/AsaasPage.tsx), linha 1612.
- Débito online: existe toggle, mas o checkout disponível implementa cartão de crédito; ativar débito não fornece o fluxo correspondente.
- Datas de criação/atualização e apresentação específica das badges solicitadas não estão integralmente implementadas.

## 2. Gestão de Caixa — com bugs e gaps

Atendido: abertura, resumo, vendas, estorno, suprimento/sangria e fechamento integrados aos serviços e cobertos por testes de UI simulados.

- **P1 — entrada inválida encerra como zero:** `parseAmount` converte texto não numérico em zero e o botão exige apenas texto não vazio. Reproduzido com `abc`. [CashDrawer.tsx](../frontend/src/features/cash/CashDrawer.tsx), linhas 30, 152 e 465.
- **P1 — terminal fixo:** o drawer usa `DEFAULT_CASH_REGISTER_ID` e título `Caixa 01`, sem resolver o terminal da filial/operador. Isso não atende operação com diferentes caixas. Mesmo arquivo, linhas 72–73 e 166.
- Não há extrato detalhado de todas as movimentações: a listagem é de vendas, acompanhada de totais e formulário de movimento.
- Falhas na consulta inicial de sessão/resumo não têm tratamento equivalente ao das mutations; uma falha de carregamento pode deixar conteúdo vazio/incompleto.

## 3. Fechamento por forma de pagamento — parcialmente atendido

Atendido: crédito, débito e Pix têm esperado/conferido/diferença; dinheiro e diferenças retornadas após o fechamento são exibidos.

- **P1 — conferência incompleta permitida:** modalidades não preenchidas são omitidas do fechamento, mesmo com recebimentos esperados. O cenário reproduzido deixou Pix de R$ 50 sem conferir. Não há bloqueio ou validação específica dessa ausência. [CashDrawer.tsx](../frontend/src/features/cash/CashDrawer.tsx), linhas 148–152.
- Falta a soma geral de valores esperados e conferidos de todas as modalidades antes de encerrar. A quebra geral apresentada depois não substitui a conferência geral solicitada.
- Os totais por modalidade usam pagamentos de vendas. Recebimentos parciais possuem tratamento separado; validar explicitamente o cenário de recebimento eletrônico parcial ainda sem venda liquidada antes de aprovar o fechamento financeiro completo.

## 4. Cadastro de Equipe e Usuários — com gap de regra obrigatória

Atendido: comandos/handlers com Result, consultas de duplicidade de CPF e username/e-mail, validação de funcionário informado ativo, BCrypt com fator 12 e validação de cargos/perfis.

- **P1 — funcionário opcional:** validator permite `EmployeeId = null` e handler só verifica funcionário quando o ID foi informado. Viola o critério de exigir funcionário válido para criar AppUser. O próprio fixture válido dos testes usa null. [CreateUserCommandValidator.cs](../backend/src/SyncBar.Application/Features/Users/Create/CreateUserCommandValidator.cs), linha 10; [CreateUserCommandHandler.cs](../backend/src/SyncBar.Application/Features/Users/Create/CreateUserCommandHandler.cs), linha 47.
- O vínculo informado é validado por existência/atividade, sem conferir nesse handler se o funcionário pertence à empresa de `CompanyId`; precisa completar essa consistência.
- SonarQube e comportamento dos índices de unicidade no banco implantado não foram homologados nesta revisão.

## 5. Ajustes visuais do Delivery — parcialmente atendido

Atendido em navegador: pesquisa com texto `rgb(26,26,26)` sobre branco; título da aba `Ding.food — Delivery`; cabeçalho `Delivery`.

- A motoca usa `width: clamp(44px, 5vw, 72px)`: não é largura fixa simples, mas a propriedade width não foi removida literalmente. [DeliveryBoardPage.tsx](../frontend/src/features/orders/DeliveryBoardPage.tsx), linha 369.
- No viewport de 390px, cabeçalho e controles de pesquisa ultrapassam o espaço visível. Ainda há ajuste responsivo necessário.
- O cartão não informa o texto exato da nova diretriz do título; o texto implementado acima precisa ser confrontado com essa definição.

## 6. Flags de leitura por câmera/barcode/QR — domínio atendido, exigência incompleta

Atendido: propriedades encapsuladas na entidade, mapeamento EF, script SQL, comandos e handlers com Result. Script: [2026-09-01_add_diningtable_reading_validation_flags.sql](../sql/2026-09-01_add_diningtable_reading_validation_flags.sql).

- **P1 — leitura não é pré-condição no backend para adicionar item:** `AddPublicOrderItemCommandHandler` não exige prova de leitura validada quando as flags estão habilitadas. A chamada direta consegue evitar o passo da interface.
- A validação de barcode/QR exige texto não vazio e registra o valor, mas não compara o valor lido ao identificador da comanda. [ValidateComandaReadingCommandHandler.cs](../backend/src/SyncBar.Application/Features/PublicOrdering/ValidateComandaReading/ValidateComandaReadingCommandHandler.cs), ramo barcode/qrcode; [validator](../backend/src/SyncBar.Application/Features/PublicOrdering/ValidateComandaReading/ValidateComandaReadingCommandValidator.cs).
- Aplicação do script no ambiente implantado e Quality Gate ainda precisam de comprovação.

## 7. CRUD de Categorias — núcleo atendido no recorte revisado

Create/Update/Deactivate, queries e endpoints existem; `UpdateDetails` altera nome e DisplayOrder, nome obrigatório é validado. Frontend ProductsPage integra as operações. Soft delete preserva registros e impede desativação enquanto existem produtos ativos vinculados, retornando mensagem de negócio.

Não identifiquei bug funcional específico no núcleo desse cartão durante esta revisão. Os testes backend selecionados passaram. A restrição a categorias com produtos ativos é uma regra explícita existente, e não exclusão em cascata. Ainda faltam evidências de homologação ponta a ponta e Sonar; a falha geral de autorização por feature abaixo também se aplica à aprovação da API como um todo.

## 8. Fechamento Diário / Turno — com bugs de consolidação

Atendido: ShiftClosing, vínculo ShiftClosingSession, configurações de persistência, abertura/fechamento, consolidação e testes.

- **P1 — caixa aberto antes do turno fica fora da trava:** a consulta seleciona apenas sessões com `OpenedAt >= PeriodStart`. Uma sessão ainda aberta de antes desse instante não chega à validação de encerramento. [CashSessionRepository.cs](../backend/src/SyncBar.Infrastructure/Persistence/Repositories/CashSessionRepository.cs), linha 26.
- **P1 — diferenças eletrônicas não entram no consolidado geral:** turno soma `ExpectedAmount`/`ClosingAmount` das sessões e recalcula a diferença, ignorando `CashSession.TotalDifferenceAmount`, que inclui as reconciliações de cartão/Pix. Uma falta em cartão pode desaparecer no fechamento geral. [ShiftClosing.cs](../backend/src/SyncBar.Domain/Entities/ShiftClosing.cs), linhas 79–81; [CashSession.cs](../backend/src/SyncBar.Domain/Entities/CashSession.cs), linha 62.

## 9. Perfil de acesso por cargo — bloqueador de autorização

Atendido: entidades/associações, consulta que une permissões de cargo e usuário, interface de edição e FeatureGate das páginas.

- **P1 — políticas não vinculadas aos endpoints:** Program registra `Feature:*` e existe FeatureAuthorizationHandler, mas a busca no código da API não encontrou aplicação das policies nos controllers ou no mapeamento dos endpoints. Portanto, o bloqueio de página não garante o bloqueio da chamada API por feature. [Program.cs](../backend/src/SyncBar.API/Program.cs), linhas 145–148; [FeatureAuthorizationHandler.cs](../backend/src/SyncBar.API/Authorization/FeatureAuthorizationHandler.cs).
- A consulta de permissões do cargo também não verifica `employee.IsActive` ao carregar o funcionário; o repositório GetById retorna inativos. Um usuário ainda ativo pode manter permissões herdadas do colaborador desativado. [GetFeaturesQueryHandler.cs](../backend/src/SyncBar.Application/Features/Access/GetFeatures/GetFeaturesQueryHandler.cs), método AddJobTitleFeatureIdsAsync. Observação: os nomes dos arquivos GetFeatures/GetMyFeatures estão trocados em relação às classes.

## 10. Pedido do Cliente via Website — parcialmente atendido

Atendido por código/testes: cardápio público, resolução de token, filial ativa, produto ativo da empresa, comanda da filial, inclusão e impressão dos itens usando o domínio.

- Compartilha o bloqueador de leitura do cartão 6.
- **P2 — pedido encerrado pode originar outro pelo mesmo acesso:** o fluxo procura pedido aberto e, se não existe, cria novo para mesa/comanda ativa. Não há verificação de encerramento do acesso anterior nesse handler. Isso exige ajuste ou definição explícita de reutilização para atender o critério de bloquear mesas/comandas encerradas. [AddPublicOrderItemCommandHandler.cs](../backend/src/SyncBar.Application/Features/PublicOrdering/AddItem/AddPublicOrderItemCommandHandler.cs), GetOrCreateOrderAsync/GetOrCreateComandaOrderAsync.
- Não foi executado fluxo real de navegador → backend → banco → fila da cozinha/bar. Não considero esse critério homologado apenas pela presença do handler/testes isolados.

## 11. Cartões Mesas/Comandas — implementação principal atendida, documentação pendente

TableCard é tipado, recebe dados/callback, usa memo, botão inteiro com nome acessível e estado disabled. Há skeleton, grades responsivas, foco, active e suporte a redução de movimento. A semântica nativa de button atende teclado sem exigir role/tabIndex redundantes. [TableCard.tsx](../frontend/src/features/orders/TableCard.tsx), [global.css](../frontend/src/styles/global.css), [OrdersPage.tsx](../frontend/src/features/orders/OrdersPage.tsx).

Gap: não encontrei Storybook/stories dos estados Skeleton/Livre/Ocupada/Fechando solicitados. A análise visual do PDF remoto não pôde ser confrontada porque o arquivo não foi recuperado.

## 12. WhatsApp / envio de imagem — serviço atendido, disparo em background pendente

Atendido: HttpClient POST `sendImage`, FormUrlEncodedContent com os cinco parâmetros, configuração externa, timeout de 30s, validação TLS padrão preservada, Result para HTTP/rede/timeout. Testes locais passaram; credenciais não são reproduzidas neste relatório.

- **P2 — sem fila/background:** o handler aguarda diretamente SendImageAsync. Isso não bloqueia uma thread durante I/O, mas mantém o processamento da requisição dependente da resposta externa e não implementa a fila/background pedida. Não encontrei disparador de produção chamando o comando. [SendWhatsAppImageCommandHandler.cs](../backend/src/SyncBar.Application/Features/Integrations/WhatsApp/SendImage/SendWhatsAppImageCommandHandler.cs), linha 28.
- Os testes usam transporte simulado; não comprovam recebimento real da imagem pelo destinatário. Nenhuma mensagem foi enviada nesta auditoria.

## 13. Integração completa Asaas — implementada com lacunas no webhook

Atendido: serviço HTTP, fluxos de cobrança Pix/boleto/crédito, receiver e atualização de status; testes locais selecionados aprovados.

- **P1 — autenticação do webhook condicional:** quando não há setting/segredo configurado, a comparação de token é pulada e o pagamento conhecido pode ser atualizado. Não há bloqueio obrigatório por ausência de configuração. [ReceiveAsaasWebhookCommandHandler.cs](../backend/src/SyncBar.Application/Features/Integrations/Asaas/WebhookLog/Receive/ReceiveAsaasWebhookCommandHandler.cs), bloco de validação de setting.
- **P2 — deduplicação protege somente o log:** evento já registrado sai de RegisterAuditLogAsync, mas o handler principal continua atualizando o pagamento. Reentrega antiga após evento mais recente pode sobrescrever o status atual.
- O handler marca o pedido pago, mas explicitamente não gera Sale/SalePayment. A inclusão desses recebimentos nos relatórios/caixa precisa ser validada junto ao fluxo financeiro; não é possível afirmar integração financeira completa a partir desse webhook sozinho.
- Não houve homologação no sandbox do Asaas com cobrança e webhook reais nesta auditoria.

## Ordem recomendada para correções

1. Políticas de autorização por feature; autenticação obrigatória do webhook; exigência de leitura no backend.
2. Fechamento de caixa com entrada inválida, conferência incompleta, terminal fixo e consolidação de turno.
3. Obrigatoriedade/consistência do funcionário; aplicação das flags e sincronização após salvar.
4. Fluxos externos em background/homologação, responsividade e documentação visual.

Não marcar os cartões com esses bloqueadores como concluídos antes de corrigir e adicionar testes que exijam o comportamento dos critérios de aceite.
