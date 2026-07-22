# SyncBar

Sistema de gestão para bares e restaurantes: pedidos em mesa e comanda, cozinha/preparo, caixa, faturamento, estoque e cadastros — construído com **.NET 9 + EF Core + SQL Server** no backend (Clean Architecture / DDD / CQRS) e **React + TypeScript + Vite** no frontend.

---

## Sumário

- [Visão geral](#visão-geral)
- [Stack tecnológico](#stack-tecnológico)
- [Estrutura do repositório](#estrutura-do-repositório)
- [Arquitetura](#arquitetura)
- [Banco de dados](#banco-de-dados)
- [Como rodar](#como-rodar)
- [Testes](#testes)
- [CI/CD e deploy](#cicd-e-deploy)
- [Convenções](#convenções)

---

## Visão geral

O SyncBar cobre a operação completa de um estabelecimento, organizada em módulos:

| Módulo | Responsabilidade |
|---|---|
| Organizacional | Empresas (`Company`) e filiais (`Branch`), com isolamento de dados por filial |
| Autenticação e acesso | Usuários, papéis, permissões, refresh tokens e log de acesso (JWT) |
| Funcionários | Cargos e cadastro de funcionários |
| Cardápio | Categorias, produtos e unidades de medida |
| Mesas e comandas | Controle de mesas e cartões de comanda, com status |
| Pedidos | Pedidos do cliente e itens, com fluxo de preparo até entrega |
| Cozinha / preparo | Acompanhamento dos itens em produção |
| Caixa | Sessões de caixa, suprimento, sangria e conferência |
| Faturamento | Vendas, pagamentos (inclusive dividido) e estornos |
| Estoque | Fornecedores, compras e livro-razão de movimentações |
| Extras | Clientes, reservas, promoções, impressão, pedido público (self-service) |

O frontend espelha esses módulos em `features/` (access, auth, billing, cash, catalog, comandas, customers, employees, finance, orders, preparation, printing, promotions, publicOrdering, purchasing, reservations, settings, stock, tables, users).

---

## Stack tecnológico

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET / C# | 9.0 / C# 13 |
| Web API | ASP.NET Core Web API | 9.0 |
| ORM | EF Core (SQL Server) | 9.0.5 |
| Mensageria (CQRS) | MediatR | 12.4.1 |
| Validação | FluentValidation | 11.11.0 |
| Auth | JWT Bearer + BCrypt (workFactor 12) | — |
| Testes unitários | xUnit + FluentAssertions + NSubstitute | — |
| Testes BDD | Reqnroll (Gherkin) + Moq | — |
| Testes de arquitetura | NetArchTest.Rules | — |
| Banco de dados | SQL Server (`BarRestauranteDb`) | 2022 |
| Frontend | React 18 + TypeScript 5 + Vite 5 | — |
| Estado / dados | Zustand + TanStack React Query | — |
| Roteamento | React Router 6 | — |
| Containers | Docker + Docker Compose | — |

---

## Estrutura do repositório

```
SyncBar/
├── backend/
│   ├── src/                         # código de produção + solution + Dockerfile
│   │   ├── SyncBar.Domain           # entidades e regras — zero dependências externas
│   │   ├── SyncBar.Application       # CQRS (MediatR) + FluentValidation
│   │   ├── SyncBar.Infrastructure    # EF Core + repositórios
│   │   ├── SyncBar.API               # controllers + JWT + Swagger
│   │   ├── SyncBar.sln
│   │   └── Dockerfile
│   └── test/                        # projetos de teste
│       ├── SyncBar.Tests            # unitários (xUnit)
│       ├── SyncBar.Specs            # BDD (Reqnroll/Gherkin)
│       └── SyncBar.ArchTests        # testes de arquitetura (NetArchTest)
├── frontend/                        # React + TypeScript + Vite
│   └── src/{components,features,lib,stores,styles,ui}
├── sql/                             # DDL, seeds, modelagem e diagrama ER
├── deploy/                          # .env.example e guia de deploy (CI/CD)
├── .github/workflows/              # ci.yml (build+testes) e deploy.yml (GHCR + SSH)
└── docker-compose.yml               # SQL Server + API + frontend (ambiente local)
```

---

## Arquitetura

Backend em **Clean Architecture** com a regra de dependência:

```
API → Application → Domain
Infrastructure implementa as interfaces do Domain
```

- **Domain** não depende de nada externo (sem MediatR, EF Core ou FluentValidation) — só entidades, invariantes de negócio e interfaces de repositório.
- **Application** implementa casos de uso em **CQRS** com MediatR (commands/queries + handlers) e validação via FluentValidation.
- **Infrastructure** cuida de EF Core (`AppDbContext`), configurações de mapeamento e repositórios concretos.
- **API** expõe os controllers, autenticação JWT e Swagger.

Essas fronteiras são verificadas automaticamente por `SyncBar.ArchTests`.

---

## Banco de dados

Banco `BarRestauranteDb` (SQL Server), com **36 tabelas** organizadas nos módulos acima. O schema é definido em SQL versionado na pasta `sql/`:

- `BarRestaurante_DDL.sql` — DDL completo (tabelas + índices), idempotente
- `BarRestaurante_Seed.sql` / `BarRestaurante_SeedComplemento.sql` — lookups, permissões e dados de exemplo
- `BarRestaurante_DiagramaER.mermaid` — diagrama de entidade-relação
- scripts complementares por módulo (comandas, faturamento, estoque, fidelidade, etc.)

Pontos importantes do modelo:

- Cadastros pertencem a `Company`; transações pertencem a `Branch` (isolamento por filial).
- Nomes evitam palavras reservadas do T-SQL: `AppUser` (usuário), `CustomerOrder` (pedido), `DiningTable` (mesa).
- **Soft delete** com índices únicos filtrados por `IsActive = 1`.
- Status de negócio (pedido, mesa, caixa, estoque, pagamento) são lookups com Ids fixos, usados como enums no C#.

> A API **não** aplica migrations no startup. O banco deve ser criado/atualizado à parte — aplicando os scripts de `sql/` ou via `dotnet ef database update` — antes de subir o serviço.

---

## Como rodar

### Opção A — Docker Compose (tudo em containers)

Sobe SQL Server, API e frontend de uma vez:

```bash
# defina um segredo JWT com 64+ caracteres (obrigatório)
export JWT_SECRET="um-segredo-bem-longo-e-aleatorio-com-64-caracteres-ou-mais"
export SA_PASSWORD="UmaSenhaForte!2026"      # senha do SQL Server (opcional; tem default)

docker compose up --build
```

| Serviço | URL local |
|---|---|
| API | http://localhost:8080 (health em `/health`) |
| Frontend | http://localhost:8081 |
| SQL Server | localhost:1433 |

### Opção B — desenvolvimento local

**Backend** (.NET 9 SDK):

```bash
cd backend/src
dotnet restore SyncBar.sln
dotnet run --project SyncBar.API
# Swagger disponível em ambiente Development
```

Configure a connection string em `appsettings.Development.json` ou via variável de ambiente `ConnectionStrings__DefaultConnection`, e o `Jwt__Secret`.

**Frontend** (Node 22):

```bash
cd frontend
npm ci
npm run dev
```

---

## Testes

Todos os projetos de teste estão em `backend/test`:

```bash
cd backend/src
dotnet test ../test/SyncBar.Tests        # unitários (xUnit + FluentAssertions + NSubstitute)
dotnet test ../test/SyncBar.Specs        # BDD (Reqnroll / Gherkin)
dotnet test ../test/SyncBar.ArchTests    # regras de arquitetura (NetArchTest)
```

---

## CI/CD e deploy

Pipeline no GitHub Actions, no mesmo padrão dos serviços irmãos (Email/Sms/WhatsApp/Parking):

- **CI** (`.github/workflows/ci.yml`) — a cada push/PR na `main`/`master`/`develop`: build + testes do backend (.NET 9), build do frontend e validação das imagens Docker.
- **CD** (`.github/workflows/deploy.yml`) — após o CI passar na `main`: build e push da imagem no GHCR (`ghcr.io/rodrigofurlaneti/syncbar`) e deploy via SSH na VM, recriando o container `syncbar-api` na **porta 84** com health check em `/health`.

Mapa de portas dos serviços na VM:

| Serviço | Porta |
|---|---|
| EmailSendingService | 80 |
| SmsSendingService | 81 |
| WhatsAppSendingService | 82 |
| Parking | 83 |
| **SyncBar** | **84** |

Detalhes de secrets, preparação da VM e variáveis de ambiente em [`deploy/README.md`](deploy/README.md).

---

## Convenções

- **Nunca alterar features já homologadas** — apenas adicionar novas seguindo os mesmos padrões.
- Consultar `sql/BarRestaurante_DDL.sql` e a modelagem antes de criar entidades — **não inventar colunas**.
- Toda alteração de saldo de estoque gera um `StockMovement` (livro-razão auditável) — nunca `UPDATE` direto.
- Padrões detalhados de cada camada estão documentados em `SKILL.md` (skill `syncbar-architect`).
