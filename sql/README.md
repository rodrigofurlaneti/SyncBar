# Banco de dados do SyncBar

Este documento explica **o que cada tabela guarda, por que ela existe e como se relaciona com as
outras**. É a referência para quem vai mexer no schema, escrever uma migration ou só entender por
que um dado está modelado de um jeito específico. Ele foi escrito a partir de um dump real do banco
(`Dump20260907.sql`) — reflete o schema **como ele está hoje em produção**, não um desenho teórico.

Se você está chegando agora no projeto: leia a seção [Convenções gerais](#convenções-gerais)
primeiro — ela explica um punhado de padrões que se repetem em quase toda tabela, então evita
repetição no resto do documento.

> Contexto arquitetural mais amplo (camadas do backend, CQRS, como o código C# se organiza) está no
> [`README.md`](../README.md) da raiz do repositório. Este documento aqui é só sobre o **banco**.

---

## Sumário

- [Convenções gerais](#convenções-gerais)
- [O núcleo do modelo (o "esqueleto" do sistema)](#o-núcleo-do-modelo-o-esqueleto-do-sistema)
- [Organizacional](#organizacional)
- [Acesso e segurança](#acesso-e-segurança)
- [Funcionários](#funcionários)
- [Cardápio](#cardápio)
- [Pizza (configuração avançada de produto)](#pizza-configuração-avançada-de-produto)
- [Salão — mesas, comandas e praças](#salão--mesas-comandas-e-praças)
- [Pedidos](#pedidos)
- [Transferência de itens entre mesa/comanda](#transferência-de-itens-entre-mesacomanda)
- [Caixa](#caixa)
- [Faturamento](#faturamento)
- [Estoque e compras](#estoque-e-compras)
- [Financeiro](#financeiro)
- [Clientes e autoatendimento](#clientes-e-autoatendimento)
- [Promoções](#promoções)
- [Impressão](#impressão)
- [Reservas](#reservas)
- [Mensagens internas (garçom)](#mensagens-internas-garçom)
- [Auditoria e técnico](#auditoria-e-técnico)
- [Integração — Asaas (pagamentos online)](#integração--asaas-pagamentos-online)
- [Integração — iFood (marketplace de delivery)](#integração--ifood-marketplace-de-delivery)
- [Integração — Keeta (marketplace de delivery)](#integração--keeta-marketplace-de-delivery)
- [Tabelas fora do escopo do SyncBar](#tabelas-fora-do-escopo-do-syncbar)

---

## Convenções gerais

Antes de entrar tabela por tabela, alguns padrões que se repetem o tempo todo:

- **Toda tabela de negócio do SyncBar tem, no mínimo**: `Id` (bigint, auto-incremento),
  `CreatedAt`/`UpdatedAt` (datetime(6), preenchidos automaticamente) e `IsActive` (tinyint, soft
  delete). Colunas idênticas a essas nas tabelas abaixo **não são repetidas na explicação** — só o
  que é específico daquela tabela.
- **Soft delete, nunca `DELETE` físico.** Desativar é sempre `IsActive = 0`. Isso existe porque quase
  todo dado aqui tem valor histórico/fiscal (uma venda, um turno de caixa, um pedido) — apagar de
  verdade destruiria rastreabilidade. Vários índices únicos (`UQ_...`) são **filtrados por
  `IsActive = 1`**, exatamente para permitir reaproveitar um valor (ex.: número de mesa) depois que o
  registro antigo foi desativado, sem violar unicidade.
- **Isolamento por empresa/filial.** Cadastros (`Category`, `Product`, `Employee`...) pertencem a uma
  `Company` (`CompanyId`); toda transação (`CustomerOrder`, `Sale`, `StockMovement`...) pertence a uma
  `Branch` (`BranchId`). O backend aplica um filtro global por tenant em cima disso — nenhuma consulta
  vaza dado de uma empresa para outra.
- **Nomes que fogem de palavra reservada do SQL**: `appuser` (não `user`), `customerorder` (não
  `order`), `diningtable` (não `table`).
- **Tabelas de "lookup" (status, tipo, método)** — `orderstatus`, `tablestatus`, `comandastatus`,
  `cashsessionstatus`, `cashmovementtype`, `stockmovementtype`, `paymentmethod`, `orderitemstatus`,
  `costtype`, `promotiontype` — são só `Id` + `Name` (+ `IsInflow`/`AllowsChange` quando cabe) e têm
  **poucas linhas fixas**, seedadas uma vez. O C# nunca compara pelo nome — usa o `Id` fixo, espelhado
  em `SyncBar.Domain.Constants.LookupIds` no backend e em `lib/types.ts` no frontend. Os valores de
  cada uma estão listados na tabela correspondente abaixo.
- **`CHECK` constraints carregam regra de negócio de verdade**, não são só validação de tipo — por
  exemplo, `CK_CustomerOrder_Origin` garante que um pedido de mesa sempre tenha `DiningTableId` ou
  `ComandaId`. Onde isso acontece, o texto abaixo explica o porquê.
- **Migrations do EF Core** aplicam-se automaticamente a cada subida da API — a tabela
  `__efmigrationshistory` (ver [Auditoria e técnico](#auditoria-e-técnico)) é como o EF Core sabe
  quais já rodaram.

---

## O núcleo do modelo (o "esqueleto" do sistema)

Se for pra entender só uma cadeia de relacionamento no banco inteiro, é esta — o caminho de um
pedido do início ao fim:

```
company ─┬─ branch ─┬─ diningtable ─┐
          │          ├─ comanda ────┼─ customerorder ─┬─ orderitem ─── stockmovement (se o produto controla estoque)
          │          └─ cashregister─cashsession       │
          │                                            └─ sale ─── salepayment
          └─ (cadastros: category, product, employee, customer...)
```

Um `customerorder` nasce vinculado a uma `diningtable` **ou** a uma `comanda` (mesa física ou cartão
de comanda) — ou, se vier de delivery/retirada/site, sem nenhuma das duas (ver
`CK_CustomerOrder_Origin`). Ele acumula `orderitem`s até ser fechado; fechar gera uma `sale` dentro de
uma `cashsession` aberta, com um ou mais `salepayment` cobrindo o total. Cada `orderitem` de um
produto com controle de estoque, ao ser pago, gera uma linha em `stockmovement` — o "livro-razão" que
nunca é sobrescrito, só acumulado.

---

## Organizacional

| Tabela | Para que serve |
|---|---|
| `company` | A empresa dona do sistema (razão social, CNPJ). É o topo do isolamento multi-tenant — tudo mais pertence, direta ou indiretamente, a uma `company`. |
| `branch` | Uma filial da empresa. Toda transação (pedido, venda, movimento de caixa/estoque) pertence a uma filial, não à empresa diretamente — é o nível real de isolamento operacional. `SelfServiceEmployeeId` é o funcionário "genérico" usado como responsável por pedidos que não têm um garçom de verdade por trás (autoatendimento por QR Code, pedidos do site, pedidos do iFood). |

---

## Acesso e segurança

Controla quem loga, o que cada um pode ver/fazer, e registra o histórico disso.

| Tabela | Para que serve |
|---|---|
| `appuser` | Usuário que loga no sistema (senha com hash BCrypt). Vinculado a uma `company` e, opcionalmente, a um `employee` (nem todo usuário do sistema é necessariamente um funcionário cadastrado). `FailedAccessCount`/`LockoutEndAt` implementam bloqueio por tentativas de login erradas. |
| `role` | Papel nomeado por empresa (ex.: "Gerente", "Garçom") — agrupamento de permissões, hoje mais um recurso legado/paralelo ao controle por feature descrito abaixo. |
| `permission` | Uma permissão nomeada (`Code`, `ModuleName`) associável a um `role`. |
| `rolepermission` | Tabela de junção `role` × `permission`. |
| `userrole` | Tabela de junção `appuser` × `role`. |
| `appfeature` | Uma **tela/funcionalidade** do sistema (ex.: `Salao`, `Estoque`, `Financeiro`) — é o mecanismo de autorização que a API realmente usa hoje: cada controller exige uma `Feature:<código>` via policy do ASP.NET, e o usuário só acessa a tela se tiver essa feature liberada (ver `jobtitlefeature`/`appuserfeature` abaixo). |
| `jobtitlefeature` | Quais features um **cargo** (`jobtitle`) libera por padrão — novo funcionário herda isso ao ser cadastrado. |
| `appuserfeature` | Override por usuário específico — libera ou revoga uma feature além (ou apesar) do que o cargo already dá. |
| `refreshtoken` | Token de renovação de sessão (JWT de acesso é de vida curta; o refresh token permite pedir um novo sem logar de novo). `RevokedAt` marca invalidação manual (logout, troca de senha). |
| `accesslog` | Histórico de eventos de autenticação (`Login`, `LoginFailed`, `Logout`, `Lockout` — ver `CK_AccessLog_EventType`), com IP e user agent. Auditoria de segurança, não é usado para lógica de negócio. |

---

## Funcionários

| Tabela | Para que serve |
|---|---|
| `jobtitle` | Cargo (ex.: "Garçom", "Cozinheiro", "Gerente"), por empresa. |
| `employee` | Funcionário de uma filial, com CPF, cargo (`JobTitleId`), data de contratação/desligamento e `CommissionPercent` opcional (comissão sobre venda). `DismissedAt` preenchido marca desligamento sem apagar o histórico de vendas/pedidos que ele processou. |

---

## Cardápio

| Tabela | Para que serve |
|---|---|
| `unitofmeasure` | Unidade de medida do produto (Un, Kg, L...) — lookup global, não por empresa. |
| `category` | Categoria do cardápio (ex.: "Bebidas", "Petiscos"), por empresa, com `DisplayOrder` para a ordem de exibição. |
| `product` | O item vendável: preço de venda, custo, se controla estoque (`IsStockControlled`) e tempo de preparo (usado na fila da cozinha para estimar atraso). `Barcode` permite leitura por código de barras (autoatendimento/balcão) e é também a chave usada para casar um item recebido do iFood com um produto local. |
| `complementgroup` | Um grupo de complementos de um produto (ex.: "Escolha o ponto da carne"). `ComplementGroupTypeId` distingue o tipo (1=seleção adicional, 2=especificação, 3=ingredientes, 4=utensílios — espelha 1:1 o `optionGroupType` do catálogo do iFood). `MinSelection`/`MaxSelection` controlam quantos itens do grupo são obrigatórios/permitidos. |
| `complementitem` | Um item de complemento reutilizável (ex.: "Bacon", "Ao ponto") — pode opcionalmente já corresponder a um `product` vendido separadamente (`LinkedProductId`, ex.: bacon também é vendido como porção avulsa). |
| `complement` | Liga um `complementitem` a um `complementgroup` com o preço extra específico daquela combinação (o mesmo item de complemento pode custar diferente em grupos diferentes). |
| `productcomplementgroup` | Liga um `product` aos grupos de complemento que ele oferece, com ordem de exibição. |

---

## Pizza (configuração avançada de produto)

Um produto "pizza" tem uma modelagem própria porque o preço depende da combinação
tamanho × sabor(es) × borda × massa — não cabe no preço fixo de `product`.

| Tabela | Para que serve |
|---|---|
| `pizzaconfiguration` | Marca um `product` como sendo uma pizza configurável e é o ponto central a que as tabelas abaixo se conectam. |
| `pizzasize` | Um tamanho disponível (ex.: "Grande", 8 fatias) — `AcceptedFractions` diz em quantas frações esse tamanho pode ser dividido (pizza meio a meio, 1/3 sabores etc.). |
| `pizzacrust` | Tipo de massa (com preço extra opcional). |
| `pizzaedge` | Tipo de borda (recheada ou não, com preço extra). |
| `pizzaflavor` | Um sabor de pizza, cadastrado por empresa (nome, descrição, imagem) — reutilizável entre várias `pizzaconfiguration`. |
| `pizzaflavorprice` | O preço de **um sabor, num tamanho específico, dentro de uma configuração** — o mesmo sabor pode custar diferente em tamanhos diferentes. |

---

## Salão — mesas, comandas e praças

| Tabela | Para que serve |
|---|---|
| `tablestatus` | Lookup: `1` Livre, `2` Ocupada, `3` Reservada, `4` EmFechamento, `5` Interditada. |
| `diningtable` | Uma mesa física. `QrToken` é o identificador único usado no link/QR Code de autoatendimento daquela mesa especificamente; os `IsXxxEnabled` controlam quais recursos de autoatendimento estão ligados nela (câmera para ler cardápio, leitor de código de barras, QR Code). |
| `comandastatus` | Lookup: `1` Disponível, `2` EmUso, `3` Extraviada, `4` Bloqueada. |
| `comanda` | Um cartão de comanda (consumo por pessoa/grupo, não vinculado a uma mesa fixa) — `Code` é o número impresso no cartão físico. |
| `comandasetting` | Limite de crédito **padrão** aplicado a uma comanda nova de uma filial (`DefaultLimitAmount`) — pode ser elevado pontualmente por pedido (`CustomerOrder.CreditLimitAmount`). |
| `diningarea` | Uma "praça" do salão (ex.: "Área externa", "Salão principal") — usada para dividir o atendimento entre equipes de garçom. |
| `diningareatable` | Liga uma `diningtable` à `diningarea` a que ela pertence (uma mesa só pode estar em uma praça por vez — `UK_DiningAreaTable_Table`). |
| `diningareaassignment` | Registra qual `employee` está responsável por qual `diningarea` num turno (`StartAt`/`EndAt`) — usado para rotear mensagens do garçom (ver `waitermessage`) para quem está de fato atendendo aquela área agora. |

---

## Pedidos

O coração operacional do sistema.

| Tabela | Para que serve |
|---|---|
| `orderstatus` | Lookup: `1` Aberto, `2` EmAndamento, `3` AguardandoPagamento, `4` Pago, `5` Cancelado, `6` WebSite. |
| `orderitemstatus` | Lookup: `1` Lançado, `2` EnviadoCozinha, `3` EmPreparo, `4` Pronto, `5` Entregue, `6` Cancelado — é o que move o item pela fila de preparo da cozinha. |
| `orderorigin` | De onde o pedido veio: `1` Local (garçom lançou direto, inclusive autoatendimento presencial por QR Code), `2` WebSite, `3` IFood, `4` Keeta. `CompanyId`/`BranchId` nulos nas linhas seedadas globalmente — origem é a mesma em todo lugar, não configurável por filial. |
| `customerorder` | **A tabela central do sistema**: um pedido, de qualquer canal. `OrderTypeId` (`1` Mesa, `2` Retirada, `3` Delivery, `4` WebSite) determina se `DiningTableId`/`ComandaId` são obrigatórios (`CK_CustomerOrder_Origin` — só pedido de mesa exige um dos dois). `SubtotalAmount`/`DiscountAmount`/`ServiceFeeAmount`/`TotalAmount` são recalculados a cada mudança de item, não somados sob demanda. `CreditLimitAmount` é o teto de consumo quando o pedido é de uma comanda com limite elevado pontualmente. Campos `CustomerName`/`CustomerPhone`/`DeliveryAddress` existem porque pedido de delivery/retirada nem sempre tem um `Customer` cadastrado por trás. |
| `orderitem` | Um item lançado num pedido — preço (`UnitPrice`) **congelado no momento do lançamento** (nunca recalculado se o preço do produto mudar depois). `SentToKitchenAt`/`DeliveredAt` marcam os carimbos de tempo usados para medir atraso na cozinha. Os `PizzaXxxId` só são preenchidos quando o item é uma pizza configurada. `CancelledByEmployeeId` registra quem cancelou (cancelamento depois de enviado à cozinha exige gerente — regra aplicada no backend, não só no banco). |
| `orderitemcomplement` | Um complemento escolhido para um item específico do pedido, com o preço cobrado **naquele momento** (`UnitPriceCharged`, também congelado). |
| `orderitempizzaflavor` | Quando o item é uma pizza dividida em sabores, guarda cada sabor escolhido e a fração que ele ocupa (`FractionShare` — ex.: 0.5 pra meio a meio). |

---

## Transferência de itens entre mesa/comanda

Cobre o caso operacional de "esse item foi lançado na mesa errada" ou "o cliente trocou de mesa" sem
precisar cancelar e relançar (o que perderia o carimbo de tempo de preparo).

| Tabela | Para que serve |
|---|---|
| `tableitemtransfer` | Registro de um item movido de uma `diningtable` para outra dentro do mesmo pedido. |
| `comandaitemtransfer` | Mesmo conceito, entre duas `comanda`s. **Achado de revisão**: diferente de `tableitemtransfer` (que tem FK `Restrict` para as mesas de origem/destino), esta tabela não declara foreign key para `SourceComandaId`/`TargetComandaId` — só a coluna, sem integridade referencial garantida pelo banco nessas duas colunas especificamente. Vale avaliar se foi omissão. |

---

## Caixa

| Tabela | Para que serve |
|---|---|
| `cashregister` | Um caixa físico da filial (a maioria das filiais tem só um, mas o modelo permite mais). |
| `cashsessionstatus` | Lookup: `1` Aberto, `2` Fechado, `3` Conferido. |
| `cashsession` | Um turno de caixa — abre com `OpeningAmount` (fundo de troco), fecha com `ClosingAmount` contado manualmente; `ExpectedAmount`/`DifferenceAmount` comparam o que deveria ter (por sistema) contra o que foi contado (sobra/falta). |
| `cashmovementtype` | Lookup: `1` Suprimento (+), `2` Sangria (−), `3` RecebimentoVenda (+), `4` EstornoVenda (−), `5` Despesa (−). O sinal do valor não fica na tabela de movimento — vem do `IsInflow` daqui. |
| `cashmovement` | Uma entrada/saída de dinheiro do caixa dentro de uma sessão — `SaleId` preenchido quando o movimento é o recebimento automático de uma venda; nulo quando é suprimento/sangria/despesa manual. `Amount` é sempre positivo (`CK_CashMovement_Amount`); o tipo é que diz se soma ou subtrai. |
| `paymentmethod` | Lookup: `1` Dinheiro, `2` CartãoCrédito, `3` CartãoDébito, `4` Pix, `5` ValeRefeição, `6` ValeAlimentação, `7` Cortesia. `AllowsChange` (só verdadeiro pra Dinheiro) é a regra que bloqueia troco em Pix/cartão. |
| `orderpartialpayment` | Pagamento parcial de um pedido **antes** dele ser fechado de vez (ex.: um convidado paga a parte dele e vai embora antes do fechamento da conta) — abatido do total na hora de registrar a venda final. |

---

## Faturamento

| Tabela | Para que serve |
|---|---|
| `sale` | O fechamento financeiro de um `customerorder` — existe **no máximo uma venda ativa por pedido** (regra garantida por índice único filtrado por `IsActive=1` no nível do banco, reforçada por uma checagem de idempotência na aplicação para tratar reenvio de clique duplo sem erro técnico). `SaleNumber` é sequencial **por filial**, não global. |
| `salepayment` | Um pagamento dentro de uma venda — uma venda pode ter vários (pagamento dividido entre métodos). `ChangeAmount` só faz sentido quando o `paymentmethod` permite troco. |

---

## Estoque e compras

Regra de ouro do módulo: **saldo nunca é alterado direto — toda variação passa por um
`stockmovement`**, que funciona como livro-razão auditável (dá pra reconstruir o saldo a qualquer
momento somando os movimentos).

| Tabela | Para que serve |
|---|---|
| `supplier` | Fornecedor, por empresa. |
| `purchase` | Uma compra registrada — cabeçalho (fornecedor, data, número de documento, valor total). |
| `purchaseitem` | Os itens de uma compra (produto, quantidade, custo unitário) — cada item gera, ao ser confirmado, um `stockmovement` de entrada. |
| `stockitem` | O **saldo atual** de um produto numa filial (`CurrentQuantity`), com mínimo/máximo para alerta de reposição. Uma linha por par filial×produto. |
| `stockmovementtype` | Lookup com 10 tipos: `1` EntradaCompra(+), `2` SaidaVenda(−), `3` AjusteEntrada(+), `4` AjusteSaida(−), `5` Perda(−), `6` Quebra(−), `7` TransferênciaEntrada(+), `8` TransferênciaSaida(−), `9` DevoluçãoFornecedor(−), `10` ConsumoInterno(−). |
| `stockmovement` | Uma linha do livro-razão — sempre com `Quantity > 0` (`CK_StockMovement_Quantity`), o sinal vem do tipo. Referencia opcionalmente o `purchaseitem` (se for entrada de compra) ou o `orderitem` (se for saída por venda). |
| `productstock` | **Tabela paralela a `stockitem`**, com chave primária direto em `ProductId` (sem `BranchId`) e `RowVersion` para controle de concorrência otimista — parece um modelo mais antigo/simplificado de saldo de estoque ainda presente no schema. Vale confirmar com o time se ainda está em uso ativo ou é resquício de uma versão anterior do módulo. |

---

## Financeiro

| Tabela | Para que serve |
|---|---|
| `costtype` | Lookup: `1` Fixo, `2` Variável — categoriza um custo operacional. |
| `operatingcost` | Um custo operacional lançado por filial/mês/ano (aluguel, energia, salário...) — usado nos relatórios financeiros, não afeta pedido/venda diretamente. |
| `revenuetarget` | Meta de faturamento de uma filial num mês/ano — comparada contra o realizado (`sale`) nos relatórios. |
| `servicefeesetting` | Se a taxa de serviço (os "10%") está habilitada para a filial — a taxa em si é aplicada por `CustomerOrder.Close(serviceFeeRate)` no momento do fechamento, não fixa nesta tabela. |

---

## Clientes e autoatendimento

| Tabela | Para que serve |
|---|---|
| `customer` | Cliente de fato (nome, CPF, telefone, pontos de fidelidade) — quem compra, independente de canal. |
| `customeraddress` | Um endereço de entrega salvo do cliente, com `LastOrderId`/`LastOrderAt` pra saber qual foi usado mais recentemente (sugestão de endereço no checkout seguinte). |
| `customerappuser` | O **login do cliente** no autoatendimento (site/app) — separado de `appuser` de propósito: é uma conta de consumidor final, não de funcionário, com seu próprio controle de tentativas de login. Vinculado opcionalmente a um `customer` (o cadastro comercial) e a uma `branch`. |

---

## Promoções

| Tabela | Para que serve |
|---|---|
| `promotiontype` | Lookup: `1` EmDobro (leva 2 pelo preço de 1), `2` Desconto (percentual). |
| `promotion` | Uma promoção de um produto, válida numa janela de dia da semana + horário (`DayOfWeek`, `StartMinuteOfDay`/`EndMinuteOfDay` — minutos desde meia-noite, não `TIME`, pra facilitar comparação). `DiscountRate` só é obrigatório e validado (`> 0` e `< 1`) quando o tipo é Desconto (`CK_Promotion_DiscountRate`). |

---

## Impressão

| Tabela | Para que serve |
|---|---|
| `printer` | Uma impressora térmica cadastrada — `ConnectionType` distingue impressora do sistema operacional (`PrinterName`) de impressora de rede (`IpAddress`+`Port`); `CK_Printer_Target` garante que os campos certos estejam preenchidos pra cada tipo. `PrintsOrders`/`PrintsBills` dizem o que aquela impressora especificamente imprime (cozinha vs. conta do cliente). |
| `printersetting` | Liga/desliga impressão de pedidos e de contas **por filial**, no nível mais alto (acima da config de impressora individual). |

---

## Reservas

| Tabela | Para que serve |
|---|---|
| `tablereservation` | Reserva de uma mesa para uma data/hora futura, com nome/telefone do cliente e tamanho do grupo. `ReservationStatusId` (não é lookup com tabela própria — `CK_TableReservation_Status` só garante 1–5): `1` Pending, `2` Confirmed, `3` Cancelled, `4` Seated, `5` NoShow. |

---

## Mensagens internas (garçom)

| Tabela | Para que serve |
|---|---|
| `waitermessage` | Um recado curto entre funcionários (ex.: "item X da mesa 4 está pronto") — `RecipientEmployeeId` nulo quando é uma notificação de área (`DiningAreaId`) em vez de pessoa específica; roteado usando `diningareaassignment` para achar quem está responsável pela praça agora. `IsRead` controla o badge de não lida na tela. |

---

## Auditoria e técnico

| Tabela | Para que serve |
|---|---|
| `logtracker` | Log de auditoria de **toda** operação de escrita do backend (`ClassName`/`MethodName`, sucesso/falha, tempo de execução, stack trace se falhou) — gravado automaticamente pelo `BaseCommandHandler`/`BaseQueryHandler` em todo handler, não é algo que o desenvolvedor precisa lembrar de chamar. |
| `__efmigrationshistory` | Tabela de controle do próprio Entity Framework Core — uma linha por migration já aplicada (`MigrationId`). A API consulta isso sozinha para saber o que falta rodar; nunca editar manualmente. |

---

## Integração — Asaas (pagamentos online)

Emissão de cobrança Pix/cartão, split de recebíveis e cartão salvo — usado tanto no caixa físico
(cobrar por Pix na mesa) quanto no checkout do autoatendimento.

| Tabela | Para que serve |
|---|---|
| `asaasintegrationsetting` | Credenciais do Asaas (API key criptografada) por empresa, com override opcional por filial — `AsaasCredentialsResolver` no backend resolve qual usar, caindo para a configuração global do `appsettings` se não houver nenhuma no banco. |
| `asaasintegrationcustomer` | Vínculo entre um `customer` do SyncBar e o cliente correspondente no Asaas (`AsaasCustomerId`) — único por par cliente/empresa, pra nunca criar o mesmo cliente duas vezes no gateway. |
| `asaasintegrationpayment` | Uma cobrança emitida — Pix (`PixQrCodeBase64`/`PixPayload`) ou cartão/boleto (`InvoiceUrl`/`BankSlipUrl`), com o status vindo do Asaas. Vinculada ao `customerorder` que está sendo pago. |
| `asaasintegrationsavedcard` | Um cartão de crédito tokenizado guardado pelo cliente para compras futuras (só o token e os últimos dígitos — nunca o número completo). |
| `asaasintegrationwebhooklog` | Registro bruto de todo webhook recebido do Asaas (`Payload` completo), com status de processamento — existe pra permitir reprocessar/depurar um evento que falhou, sem depender de reenvio pelo Asaas. |

---

## Integração — iFood (marketplace de delivery)

A integração mais extensa do banco: sincroniza pedido, cardápio, financeiro, logística e reputação.
Todo `ifoodxxxmapping` segue o mesmo desenho — liga uma entidade local a um identificador do iFood,
**por filial** (o catálogo do iFood é por loja/merchant, então o mesmo produto local pode ter um id
diferente em cada filial cadastrada no iFood).

| Tabela | Para que serve |
|---|---|
| `ifoodintegrationsetting` | Credenciais OAuth (`ClientId`/`ClientSecretEncrypted`) da integração, por empresa, e se está habilitada. |
| `ifoodmerchantmapping` | Liga uma `branch` à loja (`MerchantId`/`MerchantUuid`) correspondente no iFood — sem isso, a filial não recebe/sincroniza nada. `PreparationTimeMinutes` é publicado de volta pro iFood como tempo estimado de preparo. |
| `ifoodcategorymapping` / `ifoodproductmapping` / `ifoodcomplementgroupmapping` / `ifoodcomplementmapping` | Ligam `category`/`product`/`complementgroup`/`complement` locais aos ids equivalentes no catálogo do iFood — usados tanto para publicar o cardápio quanto para reconhecer, num pedido recebido, qual produto local corresponde a cada item. |
| `ifoodpizzamapping` / `ifoodpizzaelementmapping` | Mesmo conceito, para pizza: liga uma `pizzaconfiguration` e cada elemento dela (tamanho/borda/sabor — `Kind` diferencia qual) ao formato de pizza do catálogo do iFood, que modela isso de forma diferente do restante do cardápio. |
| `ifoodopeninghours` | Cópia local dos horários de funcionamento publicados no iFood (`DayOfWeek`, `Start`, `DurationMinutes`) — usada para decidir se um fechamento de loja fora do horário configurado é esperado (não gera alerta) ou é uma queda de verdade (gera alerta operacional). |
| `ifoodorder` | Um pedido recebido do iFood, sempre com um `customerorder` correspondente (`CustomerOrderId`) — é a ponte entre o pedido "genérico" do SyncBar e os dados específicos do iFood (prazo de confirmação, se teve item não reconhecido no cardápio — `HasUnmappedItems`). |
| `ifoodlogisticsdelivery` | Acompanhamento da entrega feita por entregador do próprio iFood (nome/telefone do motoboy, carimbos de tempo de cada etapa) — só existe quando `DeliveredBy` do pedido é o iFood, não o próprio estabelecimento. |
| `ifoodshippingdelivery` | Solicitação de entrega via serviço de logística do iFood quando é o **próprio SyncBar** que aciona um entregador terceirizado (endereço completo do cliente, cotação, rastreamento) — fluxo diferente de `ifoodlogisticsdelivery`. |
| `ifoodfinancialevent` | Um evento financeiro (taxa, repasse, ajuste) reportado pelo iFood — histórico bruto (`RawPayload`) usado nos relatórios financeiros da integração. |
| `ifoodsettlement` | Um repasse bancário consolidado do iFood — quando e quanto caiu na conta, com os dados bancários do repasse. |

---

## Integração — Keeta (marketplace de delivery)

Mesmo princípio do iFood, superfície ainda menor. Nota de schema: estas tabelas usam `Id` do tipo
`varchar(36)` (GUID como string), diferente do padrão `bigint AUTO_INCREMENT` do resto do banco —
provavelmente porque o identificador nasce do lado do Keeta ou do sistema de origem, não sequencial.

| Tabela | Para que serve |
|---|---|
| `keetaintegrationsetting` | Credenciais da API do Keeta por empresa/filial (`ClientId`/`ClientSecret`/`AppId`), incluindo o token de acesso corrente e sua expiração. |
| `keetaintegrationauthorizationsession` | Uma sessão do fluxo de autorização OAuth em andamento — `OperationType` distingue conceder (1) de cancelar (2) autorização. |
| `keetaintegrationmerchantmapping` | Liga uma `branch` à loja correspondente no Keeta, com status de autorização/onboarding e URL do cardápio/webhook. |
| `keetaintegrationorder` | Um pedido recebido do Keeta, com o `customerorder` correspondente e o payload bruto (`RawOrderJson`) — carimbos de tempo de confirmação/pronto/concluído específicos do fluxo do Keeta. |
| `keetaintegrationordereventlog` | Log bruto de cada evento de pedido recebido (criado, confirmado, despachado, entregue, cancelado...), com sucesso de processamento — mesmo papel do `asaasintegrationwebhooklog`, mas para eventos do Keeta. |
| `keetaintegrationrefunddispute` | Uma disputa de reembolso aberta pelo cliente/Keeta sobre um pedido — motivo, valor, status de resolução (`PENDING`/`ACCEPTED`/`REJECTED`) e o motivo de negativa quando aplicável. |

---

## Tabelas fora do escopo do SyncBar

`faq`, `faq_category` e `faq_section` **não fazem parte do domínio do SyncBar**. Sinais claros disso:

- Convenção de nomes em `snake_case` (`created_at`, `category_id`) — todo o resto do banco usa
  `PascalCase` (`CreatedAt`, `CompanyId`), convenção do Entity Framework Core.
- Nenhuma das três tem `CompanyId`/`BranchId` — quebra o isolamento multi-tenant que toda tabela real
  do SyncBar respeita.
- Não aparecem em nenhuma migration do EF Core nem em nenhuma entidade do backend (`SyncBar.Domain`).

O mais provável é que sejam de outro serviço da mesma organização compartilhando esta instância de
MySQL (o próprio `deploy/README.md` já menciona outros serviços irmãos — Email/Sms/WhatsApp/Parking —
rodando na mesma VM). Documentadas aqui só para deixar claro que **não devem ser alteradas a partir
do código do SyncBar**, nem usadas como referência de convenção ao criar uma tabela nova.
