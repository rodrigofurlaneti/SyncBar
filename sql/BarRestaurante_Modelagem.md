# Modelagem de Banco de Dados — Sistema de Gestão de Bar e Restaurante

**Banco:** `BarRestauranteDb` (SQL Server) · **Escopo:** multi-empresa/multi-filial · **Faturamento:** controle interno (sem emissão fiscal)

## Convenções aplicadas

PascalCase em tabelas, colunas, índices e FKs. PKs `BIGINT IDENTITY(1,1)`. Datas em `DATETIME2`. Valores monetários em `DECIMAL(18,2)`; quantidades de estoque em `DECIMAL(18,3)` (permite venda fracionada — kg, litro, dose). Soft delete via `IsActive BIT` — nunca DELETE físico; as unicidades usam **índices únicos filtrados** (`WHERE IsActive = 1`) para permitir reuso de CPF, CNPJ, e-mail e códigos após inativação. Toda tabela carrega `CreatedAt` (default `SYSDATETIME()`), `UpdatedAt` e `IsActive`. Toda FK tem índice `IX_` correspondente. Datas inválidas (ano < 1753) devem ser gravadas como NULL pela aplicação.

## Módulos e tabelas (36 tabelas)

### 1. Estrutura organizacional
`Company` (empresa, CNPJ único filtrado) → `Branch` (filial, com endereço). Todas as entidades de cadastro apontam para `Company`; todas as transacionais apontam para `Branch`, garantindo isolamento por filial.

### 2. Autenticação e acesso
`AppUser` (evita a palavra reservada `USER`) guarda `PasswordHash`/`PasswordSalt`, lockout (`FailedAccessCount`, `LockoutEndAt`) e vínculo opcional com `Employee`. RBAC clássico: `Role` (por empresa) × `Permission` (catálogo global por módulo, código único ex.: `Cash.OpenSession`) via `RolePermission`; usuários recebem perfis via `UserRole`. `RefreshToken` suporta JWT com renovação; `AccessLog` audita login/logout/falhas/lockout.

### 3. Funcionários
`JobTitle` (cargo, por empresa) e `Employee` (por filial, CPF único filtrado, admissão/demissão, salário).

### 4. Mesas, comandas e pedidos
- `DiningTable`: mesa física, número único por filial, status via `TableStatus` (Livre, Ocupada, Reservada, EmFechamento, Interditada).
- `Comanda`: o **cartão físico** (código de barras), com `ComandaStatus` (Disponível, EmUso, Extraviada, Bloqueada). O consumo em si fica em `CustomerOrder`.
- `CustomerOrder`: o pedido/atendimento (nome evita a palavra reservada `ORDER`). Aponta para mesa **ou** comanda — a `CHECK CK_CustomerOrder_Origin` exige pelo menos uma das duas, o que também cobre o cenário híbrido (comanda vinculada a mesa). Guarda garçom, status, totais (subtotal, desconto, taxa de serviço de 10%, total) e horários de abertura/fechamento.
- `OrderItem`: item lançado com preço congelado no momento da venda (`UnitPrice`), status de produção (Lançado → EnviadoCozinha → EmPreparo → Pronto → Entregue / Cancelado) e timestamps de cozinha.

### 5. Caixa
`CashRegister` (caixa físico por filial) → `CashSession` (sessão de abertura/fechamento com fundo de troco, valor esperado, valor conferido e diferença) → `CashMovement` (sangria, suprimento, despesa, estorno — tipo com flag `IsInflow`). Recebimentos de venda também podem gerar `CashMovement` vinculado à `Sale`.

### 6. Faturamento
`Sale` fecha um `CustomerOrder` (1:1 garantido por índice único filtrado — permite reemissão se a venda for cancelada via soft delete) dentro de uma `CashSession`, com numeração sequencial única por filial (`SaleNumber`). `SalePayment` permite **pagamento dividido** em múltiplas formas (`PaymentMethod`: Dinheiro com troco, crédito, débito, Pix, vales, cortesia), com código de autorização para cartões.

### 7. Estoque e controle de entrada/saída
- `Product`: catálogo por empresa, com categoria, unidade de medida, preços de venda/custo e flag `IsStockControlled` (pratos preparados podem ficar fora do controle direto de estoque).
- `StockItem`: saldo **por filial × produto** (único filtrado), com mínimo/máximo para alerta de reposição.
- `Purchase`/`PurchaseItem`: notas de compra de `Supplier`.
- `StockMovement`: **livro-razão de entrada e saída** — todo ajuste de saldo gera um registro tipado (`StockMovementType` com `IsInflow`: EntradaCompra, SaidaVenda, Ajustes, Perda, Quebra, Transferências, DevolucaoFornecedor, ConsumoInterno), com rastreio opcional da origem (`PurchaseItemId` ou `OrderItemId`), custo e responsável. `StockItem.CurrentQuantity` é saldo materializado; a soma dos movimentos é a fonte de verdade para auditoria.

## Regras de integridade principais

| Regra | Implementação |
|---|---|
| Pedido precisa de origem | `CK_CustomerOrder_Origin` (mesa ou comanda) |
| 1 venda ativa por pedido | `UQ_Sale_CustomerOrderId` filtrado |
| Número de venda único por filial | `UQ_Sale_BranchId_SaleNumber` filtrado |
| 1 saldo de estoque por filial×produto | `UQ_StockItem_BranchId_ProductId` filtrado |
| Mesa/comanda únicas por filial | `UQ_DiningTable_BranchId_Number`, `UQ_Comanda_BranchId_Code` filtrados |
| Quantidades e valores positivos | CHECKs em `OrderItem`, `PurchaseItem`, `SalePayment`, `CashMovement`, `StockMovement` |
| Login/e-mail/CPF/CNPJ únicos | Índices únicos filtrados por `IsActive = 1` |

## Fluxo operacional típico

Abertura do caixa (`CashSession`) → garçom abre `CustomerOrder` na mesa ou comanda → lança `OrderItem` (cozinha acompanha pelo status) → fechamento calcula totais + 10% → `Sale` é registrada na sessão de caixa com um ou mais `SalePayment` → baixa de estoque gera `StockMovement` (SaidaVenda) para produtos controlados → fechamento do caixa apura `ExpectedAmount` × `ClosingAmount` e grava `DifferenceAmount`.

## Extensões futuras sugeridas

Ficha técnica (`Recipe`/`RecipeIngredient`) para baixa automática de insumos de pratos preparados; reserva de mesas; delivery; integração fiscal (NFC-e) acrescentando campos de chave de acesso/status SEFAZ em `Sale`; tabela `PriceHistory` se houver reajustes frequentes.

## Arquivos entregues

1. `BarRestaurante_DDL.sql` — criação do banco, 36 tabelas, FKs, índices e constraints (idempotente: `IF OBJECT_ID ... IS NULL`).
2. `BarRestaurante_Seed.sql` — lookups, permissões, perfis, empresa/filial exemplo, 10 mesas, 20 comandas, produtos exemplo (idempotente, com `DBCC CHECKIDENT` e `IDENTITY_INSERT` + TRY/CATCH).
3. `BarRestaurante_DiagramaER.mermaid` — diagrama entidade-relacionamento.
