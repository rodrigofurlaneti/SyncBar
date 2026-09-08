# SyncBar

Sistema de gestão para bares e restaurantes — do balcão até a entrega: pedidos em mesa e comanda,
cozinha/preparo, caixa, faturamento (incluindo Pix/cartão via Asaas), estoque, autoatendimento por
QR Code e integrações com marketplaces de delivery (iFood, Keeta). Backend em **.NET 9** (Clean
Architecture / DDD / CQRS) com banco **MySQL**, frontend em **React + TypeScript + Vite**.

Este documento é a referência principal do projeto: o que cada parte faz, como as camadas se
encaixam e como rodar tudo localmente. Ele foi escrito para que alguém em nível pleno ou júnior
consiga se orientar sozinho — se algo aqui ficou confuso, é um bug de documentação tanto quanto um
bug de código seria.

---

## Sumário

- [Visão geral](#visão-geral)
- [Stack tecnológico](#stack-tecnológico)
- [Estrutura do repositório](#estrutura-do-repositório)
- [Arquitetura do backend](#arquitetura-do-backend)
- [Módulos de negócio](#módulos-de-negócio)
- [Integrações externas](#integrações-externas)
- [Frontend](#frontend)
- [Banco de dados](#banco-de-dados)
- [Segurança](#segurança)
- [Como rodar](#como-rodar)
- [Testes](#testes)
- [CI/CD e deploy](#cicd-e-deploy)
- [Convenções e boas práticas](#convenções-e-boas-práticas)
- [Onde procurar cada coisa](#onde-procurar-cada-coisa)

---

## Visão geral

O SyncBar cobre a operação completa de um bar/restaurante, ponta a ponta:

| Módulo | O que resolve |
|---|---|
| Salão (mesas e comandas) | Abrir/fechar conta, lançar itens, controlar status de mesa e comanda |
| Cozinha / preparo | Fila de itens em produção, tempo de preparo, alerta de atraso |
| Delivery / retirada | Kanban de pedidos de delivery e retirada balcão, incluindo os que chegam do iFood |
| Caixa | Abertura/fechamento de sessão, suprimento, sangria, conferência de valores |
| Faturamento | Registro de venda, pagamento (dividido entre métodos), Pix/cartão online via Asaas |
| Estoque | Fornecedores, compras, saldo por produto e livro-razão de movimentações |
| Cardápio | Categorias, produtos, complementos, pizzas (tamanho/borda/sabor) |
| Autoatendimento | Pedido do cliente pelo celular via QR Code (mesa ou link geral da loja) |
| Marketplaces | Sincronização de pedidos, cardápio, financeiro e status de loja com iFood e Keeta |
| Gestão | Funcionários, cargos, permissões por tela, usuários, empresas/filiais |
| Financeiro | Metas de receita, custos operacionais, relatórios, fechamento de turno |
| Extras | Clientes, reservas de mesa, promoções, impressão de comandas/recibos, WhatsApp |

Cada módulo aparece nos três lugares do jeito mais direto possível: uma pasta em
`backend/src/SyncBar.Application/Features/<Módulo>`, uma pasta em
`backend/src/SyncBar.Domain/Entities` para as entidades, e uma pasta em
`frontend/src/features/<módulo>` no cliente.

---

## Stack tecnológico

### Backend

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET / C# | 9.0 / C# 13 |
| Web API | ASP.NET Core Web API | 9.0 |
| ORM | Entity Framework Core | 9.0.2 |
| Provedor de banco | Pomelo.EntityFrameworkCore.MySql | 9.0.0-preview |
| Mensageria (CQRS) | MediatR | 12.4.1 |
| Validação | FluentValidation (+ auto-validation MVC) | 11.11.0 |
| Autenticação | JWT Bearer + BCrypt (work factor 12) | — |
| Logging | Serilog (console + arquivo com retenção) | 8.0.3 |
| Documentação de API | Swashbuckle (Swagger/OpenAPI) | 7.2.0 |
| Health check | AspNetCore.HealthChecks.MySql (`/health`) | 9.0.0 |
| Testes unitários | xUnit + FluentAssertions + NSubstitute | 2.9.2 / 6.12.2 / 5.3.0 |
| Testes BDD | Reqnroll (Gherkin) + Moq | 2.4.1 / 4.20.72 |
| Testes de arquitetura | NetArchTest.Rules | 1.3.2 |

### Frontend

| Camada | Tecnologia | Versão |
|---|---|---|
| Framework | React | 18.3 |
| Linguagem | TypeScript | 5.6 |
| Build tool | Vite | 5.4 |
| Dados/cache do servidor | TanStack React Query | 5.62 |
| Estado de UI/sessão | Zustand | 4.5 |
| Roteamento | React Router | 6.28 |
| Alertas/toasts | SweetAlert2 | 11.26 |
| Leitura de QR Code | html5-qrcode | 2.3.8 |
| Testes end-to-end | Playwright | 1.42 |
| Estilo | CSS puro com variáveis (`src/styles/global.css`) — sem framework de UI |

> Não há Tailwind, styled-components nem biblioteca de componentes de terceiros no frontend — o
> projeto usa um pequeno design system próprio baseado em CSS custom properties (cores, espaçamento,
> tipografia, transições) definido em `global.css`. Ao criar algo novo, reaproveite essas classes
> (`.btn-primary`, `.btn-ghost`, `.chip`, `.ticket`, `.table-tile` etc.) em vez de estilo inline.

### Infraestrutura

Docker + Docker Compose, imagens publicadas no GitHub Container Registry (GHCR), deploy automático
via GitHub Actions em uma VM (detalhes em [CI/CD e deploy](#cicd-e-deploy)).

---

## Estrutura do repositório

```
SyncBar/
├── backend/
│   ├── SyncBar.sln                     # solution — abre todos os projetos de uma vez
│   ├── src/
│   │   ├── SyncBar.Domain              # entidades e regras de negócio — zero dependências externas
│   │   ├── SyncBar.Application         # casos de uso (CQRS/MediatR) + validação
│   │   ├── SyncBar.Infrastructure      # EF Core, repositórios, clientes HTTP das integrações
│   │   ├── SyncBar.API                 # controllers, autenticação, Swagger, Program.cs
│   │   └── Dockerfile
│   └── test/
│       ├── SyncBar.Tests               # unitários (xUnit) — a maior parte da suíte
│       ├── SyncBar.Specs               # BDD (Reqnroll/Gherkin), cenários de negócio em texto
│       └── SyncBar.ArchTests           # valida as regras de dependência entre camadas
├── frontend/
│   ├── src/
│   │   ├── features/                   # uma pasta por módulo de tela (orders, catalog, stock…)
│   │   ├── components/                 # AppShell, ErrorBoundary, QueryError — usados em várias telas
│   │   ├── ui/                         # biblioteca de componentes genéricos (Button, Modal, Table…)
│   │   ├── lib/                        # apiClient (fetch com refresh de token), types.ts, formatação
│   │   ├── stores/                     # Zustand — authStore (sessão/JWT) e themeStore (claro/escuro)
│   │   └── styles/global.css           # design system (cores, espaçamento, componentes base)
│   ├── test/                           # specs do Playwright (end-to-end)
│   └── Dockerfile + nginx.conf         # build estático servido por nginx, proxy /api e /uploads
├── sql/                                # DDL do MySQL e migrações manuais avulsas
├── deploy/README.md                    # guia de CI/CD (parcialmente defasado — ver seção própria aqui)
├── postman/                            # coleção Postman para os webhooks do Asaas
├── .github/workflows/                  # ci.yml (build + testes) e deploy.yml (build de imagens + deploy na VM)
└── docker-compose.yml                  # api + frontend, usado tanto em dev quanto em produção
```

---

## Arquitetura do backend

O backend segue **Clean Architecture** com quatro projetos e uma regra de dependência em uma única
direção:

```
SyncBar.API  →  SyncBar.Application  →  SyncBar.Domain
                       ↑
SyncBar.Infrastructure ┘  (implementa interfaces definidas no Domain)
```

Essa direção de dependência não é só uma convenção documentada — é **verificada automaticamente**
pelo projeto `SyncBar.ArchTests` a cada build. As regras incluem, entre outras:

- `Domain` não pode depender de nenhuma camada externa nem de frameworks (nem EF Core, nem MediatR).
- `Application` não pode depender de `Infrastructure` nem de `API`.
- `Infrastructure` não pode depender de `API`.
- Handlers de command/query devem ser `internal sealed`.
- Repositórios e configurações do EF Core devem ser `internal sealed`.
- Toda interface de repositório mora em `SyncBar.Domain.Repositories`.
- Todo Aggregate Root herda de `AggregateRoot`; toda entidade filha herda de `Entity` (mas não de
  `AggregateRoot`).
- Validadores herdam de `AbstractValidator<T>`.

Se uma dessas regras for quebrada, `dotnet test` falha imediatamente — é a forma do projeto se
autodefender contra "vamos só chamar o EF Core direto do Domain só dessa vez".

### SyncBar.Domain

A camada mais interna. Não referencia **nenhum** pacote NuGet além do próprio .NET — sem EF Core,
sem MediatR, sem FluentValidation. Contém:

- **Entidades** (`Entities/`, ~100 classes) — as regras de negócio de verdade vivem aqui, não nos
  handlers. Uma entidade típica:
  ```csharp
  public sealed class CustomerOrder : AggregateRoot
  {
      public long OrderStatusId { get; private set; }   // nunca public set — só muda via método
      private CustomerOrder() : base(0) { }              // construtor vazio, exigido pelo EF Core
      private CustomerOrder(...) : base(0) { ... }        // construtor real, privado
      public static Result<CustomerOrder> Create(...) { ... }  // única forma de criar uma instância
      public Result Close(decimal serviceFeeRate) { ... }      // mudanças de estado retornam Result
  }
  ```
- **Primitivos** (`Primitives/`) — `Entity`, `AggregateRoot`, `Result`/`Result<T>` (em vez de lançar
  exceção para erro esperado, um método devolve `Result.Failure(new Error("Code", "Mensagem"))`),
  `Error`, `ValueObject`, `IDomainEvent`.
- **Constantes** (`Constants/LookupIds.cs`) — os Ids fixos dos lookups do banco (status de pedido,
  mesa, comanda, caixa, estoque, pagamento, origem do pedido etc.), usados como se fossem enums:
  `OrderStatusIds.Pago`, `TableStatusIds.Ocupada`, `OrderOriginIds.IFood`.
- **Interfaces de repositório** (`Repositories/`) — o Domain define o contrato (`ISaleRepository`,
  `ICustomerOrderRepository`...); quem implementa é a Infrastructure. Isso é o que permite trocar de
  banco sem tocar em uma linha de regra de negócio.
- **Exceções de domínio** (`Exceptions/`) — ex. `ConcurrencyException`, usada para traduzir conflitos
  de concorrência do EF Core em algo que a Application sabe tratar sem conhecer EF Core.

### SyncBar.Application

Implementa os casos de uso em **CQRS** (Command Query Responsibility Segregation) usando MediatR:
toda ação vira um **Command** (muda estado) ou uma **Query** (só lê), cada um com seu **Handler**.

```
Features/
└── Billing/
    └── RegisterSale/
        ├── RegisterSaleCommand.cs            # record : ICommand<long>
        ├── RegisterSaleCommandHandler.cs     # internal sealed : trata a lógica de fato
        └── RegisterSaleCommandValidator.cs   # AbstractValidator<RegisterSaleCommand>
```

- **Por que separar em Command/Query?** Fica óbvio pelo nome se uma ação muda dado ou só lê, o que
  facilita cache, auditoria e até otimizar o lado de leitura de forma independente do de escrita.
- Handlers herdam de `BaseCommandHandler`/`BaseQueryHandler`, que embrulham a execução em
  `ExecuteWithLogAsync` — grava uma linha de auditoria (`LogTracker`) para toda operação, com sucesso
  ou falha, tempo de execução e stack trace se der erro. Isso roda no `finally`, então **toda
  operação bem-sucedida acaba fazendo dois `CommitAsync()`**: um da própria regra de negócio e um do
  log — detalhe que aparece bastante nos testes.
- Validação de formato (campo obrigatório, tamanho, intervalo) fica no `Validator` (FluentValidation,
  disparado automaticamente antes do handler via `AddFluentValidationAutoValidation`). Regra de
  negócio de verdade (pedido pode ser fechado? caixa está aberto?) fica na entidade ou no handler.
- `Abstractions/Integrations/` define os contratos (`IAsaasService`, `IIfoodOrderClient`,
  `IKeetaOrderClient`...) que a Infrastructure implementa — o mesmo princípio de inversão de
  dependência do Domain, aplicado às integrações externas.

### SyncBar.Infrastructure

Onde tudo que é "detalhe técnico" mora:

- **`Persistence/`** — `AppDbContext` (um `DbSet<T>` por tabela, ~90 no total), `Configurations/`
  (uma classe `IEntityTypeConfiguration<T>` por entidade — é aqui que se define nome de coluna,
  tamanho, índice, FK; nunca usar `[Attribute]` de mapeamento na entidade), e `Repositories/`
  (implementação concreta de cada interface do Domain).
- **`Integrations/`** — um cliente HTTP por parceiro externo: `Asaas/`, `IFood/`, `Keeta/`,
  `WhatsApp/` (detalhes na seção [Integrações externas](#integrações-externas)).
- **`Authentication/`** — `JwtTokenProvider` (emissão do token) e o hashing de senha via BCrypt.
- **`Storage/`** — `LocalImageStorage`, upload de imagem de produto para `wwwroot/uploads`.
- **`Printing/`** — geração de recibo/comanda para impressora térmica, incluindo o transporte
  Windows via `LibraryImportAttribute` (P/Invoke) usado quando a impressão acontece localmente.

### SyncBar.API

A camada mais externa — só orquestra, não decide nada sozinha:

- **Controllers** (53 arquivos) — cada um só monta o `Command`/`Query`, manda pro `IMediator` e
  traduz o `Result` em `IActionResult` (`ApiController.HandleFailure` decide o HTTP status pelo
  sufixo do código de erro: `.NotFound` → 404, `.AlreadyExists`/`.Duplicate` → 409, senão → 400).
  As dezenas de endpoints do iFood ficam concentradas em um único `IntegrationsController.cs`
  (~100 rotas) em vez de um controller por sub-recurso.
- **`Program.cs`** — ponto de entrada; monta pipeline de logging (Serilog), autenticação JWT,
  autorização por feature (ver [Segurança](#segurança)), CORS, rate limiting, Swagger, health check e
  **aplica as migrations do EF Core automaticamente a cada subida** (`dbContext.Database.MigrateAsync()`
  — diferente do que versões antigas deste documento diziam). Também falha rápido (lança exceção
  antes de subir) se `Jwt:Secret` ou a connection string ainda estiverem com o valor placeholder do
  `appsettings.json`.
- **`Middleware/ExceptionHandlingMiddleware`** — captura qualquer exceção não tratada e devolve um
  `ProblemDetails` genérico (nunca vaza stack trace pro cliente em produção).

---

## Módulos de negócio

Cada linha é uma pasta em `backend/src/SyncBar.Application/Features/` e a pasta espelho em
`frontend/src/features/`:

| Módulo (Application) | Frontend | O que faz |
|---|---|---|
| `Auth` | `auth` | Login, refresh token, cadastro de conta |
| `Access` | `access` | Papéis, permissões por tela (feature), log de acesso |
| `Companies` / `Branches` | `settings` | Empresas e filiais — toda transação pertence a uma filial |
| `Employees` | `employees` | Cargos e funcionários |
| `Users` | `users` | Usuários do sistema (`AppUser`) e vínculo com funcionário |
| `Catalog` | `catalog` | Categorias, produtos, complementos, pizza (tamanho/borda/sabor) |
| `Tables` / `Dining` | `tables`, `diningareas` | Mesas, praças do salão, vínculo mesa↔praça |
| `Comandas` | `comandas` | Comandas (cartão de consumo), limite de crédito |
| `Orders` | `orders`, `waiter` | Pedido (`CustomerOrder`), itens, transferência entre mesa/comanda |
| `Preparation` | `preparation` | Fila de preparo da cozinha (itens por status, tempo, atraso) |
| `Checkout` / `PublicOrdering` / `Storefront` | `publicOrdering`, `storeFront`, `digitalmenu` | Autoatendimento via QR Code — cliente faz o próprio pedido e paga online |
| `Cash` | `cash` | Sessão de caixa, suprimento, sangria, movimentações |
| `Billing` | `billing` | Registro de venda e pagamentos (inclusive dividido) |
| `Payments` | `asaas` (parte) | Pagamento online (Pix/cartão) via Asaas |
| `Shift` | — | Fechamento de turno, conferência de caixa consolidada |
| `Stock` | `stock` | Saldo por produto/filial e livro-razão (`StockMovement`) |
| `Purchases` / `Suppliers` | `purchasing` | Fornecedores e compras, que alimentam o estoque |
| `Finance` | `finance` | Metas de receita, custos operacionais, relatórios |
| `Fiscal` | — | Dados fiscais associados à venda |
| `Customers` / `CustomerAddresses` / `CustomerAppUser` | `customers` | Cadastro de clientes do autoatendimento, endereços, login do cliente |
| `Reservations` | `reservations` | Reserva de mesa |
| `Promotions` | `promotions` | Descontos e promoções aplicáveis a itens do pedido |
| `Printing` | `printing` | Configuração de impressoras e templates de impressão |
| `OrderOrigin` | — (consumido em várias telas) | De onde o pedido veio: Local, Site, iFood, Keeta |
| `Integrations/Asaas` | `asaas` | Pagamentos online, split de recebíveis, cartão salvo |
| `Integrations/IFood` | `integrations` (10 páginas) | Cardápio, pedidos, financeiro, logística, avaliações |
| `Integrations/Keeta` | `keeta` | Autorização OAuth, pedidos, disputas de reembolso |
| `Integrations/WhatsApp` | — | Envio de notificações (ex.: status de pedido) |

> **Regra importante do projeto:** nunca alterar uma feature já homologada — sempre adicionar uma
> nova seguindo o mesmo padrão. Ver [Convenções](#convenções-e-boas-práticas).

---

## Integrações externas

Cada integração segue o mesmo desenho: uma interface em `Application/Abstractions/Integrations/<X>`,
implementação HTTP em `Infrastructure/Integrations/<X>`, e handlers em
`Application/Features/Integrations/<X>` que orquestram a chamada.

### Asaas — pagamentos online

Emissão de cobrança Pix/cartão de crédito, split de recebíveis, cartão salvo e recebimento de
webhooks de status de pagamento. Usado tanto no caixa físico (cobrar o cliente por Pix na mesa)
quanto no checkout do autoatendimento (`StorefrontPaymentModal`). As credenciais (API key) podem ser
configuradas por empresa/filial no banco, com fallback para as chaves globais do `appsettings`
(`AsaasCredentialsResolver`). Tela: **Config → Asaas**.

### iFood — marketplace de delivery

A integração mais extensa do projeto: sincronização de pedidos por polling
(`IfoodOrderPollingBackgroundService`, a cada 30s), publicação de cardápio (Catalog API), consulta
financeira (repasses/eventos), logística (motoboy), acompanhamento de avaliações e monitoramento de
status da loja (`IfoodMerchantStatusWatcherBackgroundService`, verifica a cada 5 min se a loja caiu
"disponível"/"indisponível" fora do horário configurado). Pedidos sincronizados viram um
`CustomerOrder` normal com `OrderOriginId = IFood` — aparecem no mesmo Kanban de Delivery que um
pedido feito pelo site. Telas: **Config → iFood** + as 10 páginas em `IFood*Page.tsx`.

### Keeta — marketplace de delivery

Mesmo princípio do iFood, ainda em superfície menor: autorização OAuth, sincronização de pedidos e
tratamento de disputas de reembolso. Tela: **Config → Keeta**.

### WhatsApp

Envio de mensagens (ex.: notificar o cliente sobre o status do pedido). Não tem tela própria no
frontend — é usado como serviço de notificação a partir de outros fluxos.

---

## Frontend

SPA em React + TypeScript, roteada por `react-router-dom` (rotas em `App.tsx`), sem framework de UI
— o "design system" é só CSS com variáveis (`styles/global.css`): cores (`--amber`, `--bg-raise`,
`--free`/`--busy`/`--closing`...), espaçamento, tipografia condensada (visual "quadro de bar/giz") e
classes reutilizáveis (`.btn-primary`, `.chip`, `.table-tile`, `.ticket`...).

- **`stores/authStore.ts`** (Zustand) — sessão do usuário: token JWT, refresh token, filial ativa.
  Persistido em `localStorage`.
- **`lib/apiClient.ts`** — wrapper de `fetch` único: injeta o Bearer token, tenta renovar via refresh
  token automaticamente numa resposta 401 e refaz a chamada original antes de desistir.
- **`lib/types.ts`** — todos os tipos de resposta da API e os lookups (`OrderStatus`, `TableStatus`,
  `PaymentMethod`...) espelhando `LookupIds.cs` do backend — **os dois lados precisam ser mantidos em
  sincronia manualmente**, não há geração automática de tipos a partir do backend.
- **`ui/`** — componentes de UI genéricos e sem regra de negócio (`Button`, `Modal`, `DataTable`,
  `Skeleton`, `StatusBadge`...), reaproveitados por várias `features/`.
- **`features/<módulo>/`** — cada módulo tem sua própria página (`XyzPage.tsx`), chamadas de API
  (`api.ts`) e componentes específicos da tela. Um componente de apresentação "burro" (só recebe
  props já resolvidas, sem `useQuery` dentro) fica separado da página que busca os dados — ver
  `TableCard.tsx`/`ComandaCard.tsx` em `features/orders` como referência desse padrão.
- **Dados do servidor sempre via TanStack Query** (`useQuery`/`useMutation`), nunca `fetch` direto
  dentro de um componente — dá cache, refetch automático (a maioria das telas operacionais faz
  polling a cada 15s) e invalidação de cache centralizada.

Rotas relevantes (ver `App.tsx` para a lista completa): `/` (Salão), `/delivery`, `/preparo`,
`/garcom` (Modo Garçom), `/pedido/:token` (autoatendimento por QR Code de mesa),
`/cardapio/:branchIdParam` (autoatendimento pelo link geral da loja), `/integracoes/ifood/*`
(catálogo, pedidos, financeiro, logística, avaliações, status, dashboard, indicadores),
`/integracoes/keeta`, `/integracoes/asaas`.

---

## Banco de dados

**MySQL** (via Pomelo.EntityFrameworkCore.MySql) — não SQL Server. O schema atual tem **84 tabelas**.

- **DDL versionado** em `sql/CreateDatabaseMySql.sql`; alterações pontuais posteriores ficam em
  arquivos avulsos com data no nome (`sql/2026-09-01_...sql`, `sql/2026-09-03_...sql`) até serem
  consolidadas numa migration do EF Core.
- **Migrations do EF Core** vivem em `SyncBar.Infrastructure/Persistence/Migrations/` e **rodam
  automaticamente a cada subida da API** (`Program.cs`, via `MigrateAsync()`) — não é preciso rodar
  `dotnet ef database update` manualmente antes de subir o serviço.
- Cadastros pertencem a `Company`; toda transação pertence a uma `Branch` (isolamento por filial,
  aplicado via *query filter* global do EF Core baseado no tenant autenticado).
- Nomes de tabela evitam palavras reservadas do SQL: `AppUser` (não `User`), `CustomerOrder` (não
  `Order`), `DiningTable` (não `Table`).
- **Soft delete**: nunca `DELETE` físico, sempre `IsActive = 0`. Índices únicos relevantes são
  filtrados por `IsActive = 1` (ex.: uma mesa pode ter o mesmo número reaproveitado após desativar a
  antiga).
- Status de negócio (pedido, mesa, comanda, caixa, estoque, pagamento, origem do pedido) são tabelas
  de lookup com Ids fixos, espelhados como constantes em `LookupIds.cs` no C# e em `types.ts` no
  frontend — nunca comparar por string, sempre pelo Id.
- Toda alteração de saldo de estoque **precisa** gerar uma linha em `StockMovement` (livro-razão) —
  nunca um `UPDATE` direto no saldo.

---

## Segurança

- **Autenticação**: JWT Bearer (access token curto + refresh token), senha com BCrypt (work factor
  12).
- **Autorização por feature**: em vez de papéis fixos (admin/gerente/garçom), cada tela tem um código
  (`FeatureCodes`) e o controller exige `[Authorize(Policy = "Feature:X")]`. Um `AppUser` só acessa a
  tela se tiver essa feature liberada — liberação configurável em **Config → Acessos**.
- **Rate limiting** (`Program.cs`): 200 requisições/min por IP globalmente; política `auth` mais
  restritiva (10/min) em login/refresh contra força bruta; política `public-ordering` (60/min) para o
  autoatendimento sem login, mais generosa que `auth` mas ainda limitada por IP.
- **CORS**: origens permitidas vêm de configuração (`Cors:AllowedOrigins`) — nunca `*` combinado com
  credenciais. Em desenvolvimento sem essa configuração, libera qualquer origem `localhost`.
- **Segredos**: a API falha ao subir (de propósito) se `Jwt:Secret` ou a connection string do MySQL
  ainda estiverem com o valor placeholder do `appsettings.json` — força configurar via variável de
  ambiente antes de ir pro ar.
- **Data Protection**: chaves persistidas em volume Docker (`syncbar-dataprotection-keys`) — usadas
  para criptografar segredos guardados no banco (ex.: `ClientSecret` do iFood). Sem esse volume, cada
  `docker compose up --force-recreate` gerava uma chave nova e invalidava segredos já criptografados.

---

## Como rodar

### Pré-requisitos

- .NET 9 SDK
- Node.js 22+
- MySQL 8+ acessível (local, Docker ou um serviço gerenciado) — **ou** use a Opção A abaixo

### Opção A — Docker Compose

```bash
docker compose up --build
```

Isso sobe os containers `api` e `frontend` (imagens publicadas em produção; para rodar a partir do
código local, ajuste o `docker-compose.yml` para buildar a partir de `backend/src/Dockerfile` e
`frontend/Dockerfile` em vez de usar a imagem do GHCR). Requer um MySQL externo já configurado via
`MYSQL_CONNECTION_STRING` — o Compose deste repositório não sobe banco.

| Serviço | URL |
|---|---|
| Frontend (serve a SPA e faz proxy de `/api` e `/uploads` pro backend) | http://localhost:84 |
| API (health em `/health`) | http://localhost:8080 |

### Opção B — desenvolvimento local, cada parte na sua porta

**Backend:**

```bash
cd backend
dotnet restore SyncBar.sln
dotnet run --project src/SyncBar.API
```

Configure antes (via `dotnet user-secrets`, variável de ambiente ou `appsettings.Development.json`):

```
ConnectionStrings__DefaultConnection = "Server=localhost;Database=BarRestauranteDb;User=root;Password=...;"
Jwt__Secret = "um-segredo-aleatorio-com-pelo-menos-64-caracteres"
```

Swagger fica disponível em `/swagger`. As migrations do EF Core rodam sozinhas na primeira subida —
não é preciso criar o banco manualmente, só garantir que o schema/usuário do MySQL já exista.

**Frontend:**

```bash
cd frontend
npm ci
npm run dev
```

Abre em `http://localhost:5173` com proxy do Vite para a API local.

---

## Testes

```bash
# a partir de backend/ (onde fica SyncBar.sln)
dotnet test test/SyncBar.Tests            # unitários — xUnit + FluentAssertions + NSubstitute
dotnet test test/SyncBar.Specs            # BDD — Reqnroll/Gherkin, cenários em arquivos .feature
dotnet test test/SyncBar.ArchTests        # valida as regras de dependência entre camadas
# ou simplesmente `dotnet test SyncBar.sln` pra rodar os três projetos de uma vez

# frontend
cd frontend
npm run test:e2e                          # Playwright (precisa da API e do frontend rodando)
```

- `SyncBar.Tests` cobre entidades de domínio, handlers de Application, repositórios (com SQLite em
  memória simulando o MySQL — ver `RepositoryTestBase`), configurações do EF Core e os clientes HTTP
  das integrações (com um `HttpMessageHandler` fake, sem bater na API real).
- `SyncBar.Specs` descreve regras de negócio em Gherkin (`.feature`) legível por quem não programa —
  útil para validar entendimento de regra com alguém do time de produto/operação.
- Cobertura de código: `backend/coverage.runsettings` configura o coletor `XPlat Code Coverage`
  (exclui migrations geradas e a classe de interop com a impressora, que quebra a instrumentação do
  assembly inteiro se não for excluída à parte — ver comentário no próprio arquivo). Rode, a partir
  de `backend/`:
  ```bash
  dotnet test test/SyncBar.Tests/SyncBar.Tests.csproj --collect:"XPlat Code Coverage" --settings coverage.runsettings
  ```
  e gere um relatório HTML com `dotnet tool install -g dotnet-reportgenerator-globaltool` +
  `reportgenerator -reports:"test/**/coverage.cobertura.xml" -targetdir:coveragereport -reporttypes:Html`.

---

## CI/CD e deploy

Dois workflows em `.github/workflows/`:

1. **`ci.yml`** — a cada push/PR: builda e testa o backend (.NET 9) e o frontend (Vite), valida as
   imagens Docker.
2. **`deploy.yml`** — dispara automaticamente quando o `ci.yml` termina verde **na branch `main`**
   (ou manualmente via `workflow_dispatch`):
   - builda `syncbar-api` (contexto `backend/src`) e `syncbar-frontend` (contexto `frontend`) e faz
     push das duas imagens no GHCR (`ghcr.io/rodrigofurlaneti/syncbar-api` /
     `-frontend`);
   - copia o `docker-compose.yml` da raiz do repositório para a VM (`/opt/syncbarservice/`);
   - via SSH, faz `docker compose pull` + `docker compose up -d --force-recreate` e confere
     `GET /health`.

> **Importante**: o deploy só dispara quando o CI passa **na branch `main`**. Trabalho em uma branch
> de feature (`features/...`) nunca sobe sozinho pro ambiente publicado — é preciso mergear na `main`
> primeiro. Isso costuma ser a explicação quando "uma mudança que já foi feita" não aparece no
> ambiente publicado.

Segredos do repositório usados pelo `deploy.yml`: `VM_HOST`, `VM_USER`, `VM_SSH_KEY` (acesso SSH à
VM) e `GHCR_PAT` (Personal Access Token com escopo `read:packages`, usado pela própria VM para logar
no GHCR e puxar as imagens) — o *push* das imagens, feito pelo runner do Actions, usa o
`GITHUB_TOKEN` automático, sem precisar de PAT.

Na VM, o `docker-compose.yml` publicado expõe o frontend (que serve a SPA e faz proxy de `/api` e
`/uploads` para o container da API) na porta **84**; a mesma VM hospeda outros serviços da empresa em
portas vizinhas (80, 81, 82, 83). Detalhes adicionais (bastante desatualizados quanto ao SQL Server
vs. MySQL — confiar neste README, não lá) em [`deploy/README.md`](deploy/README.md).

---

## Convenções e boas práticas

- **Nunca alterar uma feature já homologada** — adicionar uma nova sempre seguindo o mesmo padrão já
  existente na vizinhança (mesma pasta de Features, mesmo nome de método, mesma forma de validar).
- Antes de criar uma entidade nova, checar `sql/CreateDatabaseMySql.sql` — **não inventar coluna**.
- Entidades: sempre `sealed`, propriedades com `private set`, construtor vazio `private` (exigência
  do EF Core) + construtor real `private` + factory pública `static Result<T> Create(...)`.
- Handlers: sempre `internal sealed`, sempre herdando de `BaseCommandHandler`/`BaseQueryHandler`.
- Toda mudança de saldo de estoque gera um `StockMovement` — nunca `UPDATE` direto.
- O padrão detalhado de cada camada (com exemplos de código completos) está em `SKILL.md`, usado como
  skill de IA (`syncbar-architect`) para manter consistência entre sessões de desenvolvimento
  assistido — vale a leitura mesmo fazendo o trabalho manualmente.
- Ao adicionar uma classe nova em `Infrastructure` (repositório, configuração EF, cliente HTTP),
  escrever o teste correspondente junto — é convenção do projeto manter cobertura alta em código de
  infraestrutura, não só em regra de negócio.

---

## Onde procurar cada coisa

Um guia rápido para quem está chegando agora:

| Eu preciso... | Vou em... |
|---|---|
| Entender uma regra de negócio (ex.: quando um pedido pode ser cancelado) | `backend/src/SyncBar.Domain/Entities/<Entidade>.cs` |
| Adicionar um novo endpoint | `Application/Features/<Módulo>/<Ação>/` (Command+Handler+Validator) e depois o Controller em `SyncBar.API/Controllers` |
| Mudar como uma tabela é mapeada (coluna, índice, FK) | `Infrastructure/Persistence/Configurations/<Entidade>Configuration.cs` |
| Ver/alterar a estrutura real do banco | `sql/CreateDatabaseMySql.sql` + migrations em `Infrastructure/Persistence/Migrations` |
| Mexer numa tela existente | `frontend/src/features/<módulo>/` |
| Reaproveitar um componente de UI genérico | `frontend/src/ui/` |
| Entender um status/lookup (ex.: `OrderStatusId = 3`) | `backend/src/SyncBar.Domain/Constants/LookupIds.cs` (fonte da verdade) e `frontend/src/lib/types.ts` (espelho) |
| Mexer numa integração externa | `Infrastructure/Integrations/<Parceiro>/` (chamada HTTP) + `Application/Features/Integrations/<Parceiro>/` (orquestração) |
| Rodar os testes de uma camada específica | ver [Testes](#testes) |
| Saber por que uma mudança não apareceu no ambiente publicado | ver a nota em [CI/CD e deploy](#cicd-e-deploy) — provavelmente falta merge na `main` |
