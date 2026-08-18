# SyncBar — Plano de melhoria de UX/UI

*Análise sênior do front end (`/frontend`, React 18 + Vite + TS + TanStack Query + Zustand). Revisão de tokens, shell, roteamento, fluxo crítico do PDV (Salão → Pedido → Pagamento), cliente de API e overlays.*

---

## 1. Sumário executivo

O front está **arquiteturalmente saudável** — organização por feature, cache/estado bem resolvidos, cliente de API robusto (refresh de token, mensagens amigáveis) e um tema visual com personalidade ("quadro de bar"). O que segura a experiência não é o "esqueleto", é a **camada de interface**: quase tudo é montado com `style` inline (≈470 ocorrências), **não há responsividade** (0 media queries), a **acessibilidade é praticamente ausente** (1 uso de `aria/role/htmlFor` no projeto inteiro), e ações importantes usam **`window.confirm/prompt/alert`** (13 usos em 8 telas), que quebram o tema e não servem para tablet.

Para um PDV — usado em pé, no toque, sob pressão de tempo — esses pontos custam velocidade e erros. O plano abaixo prioriza o **fluxo que gera dinheiro** (salão/pedido/pagamento) e cria uma **base de componentes** que elimina a inconsistência na raiz.

---

## 2. O que já está bom (preservar)

- **Design tokens** sólidos em `styles/global.css` (`:root`): paleta, tipografia condensada, `--radius`, e um `--touch: 48px` já pensado para toque. Boa fundação — falta *aplicá-la* de forma consistente.
- **Cliente de API** (`lib/apiClient.ts`): refresh único concorrente, tradução de 401/403/404/500 em mensagens em PT, agregação de erros do FluentValidation. Exemplar.
- **Controle de acesso fail-closed** (`AppShell`/`FeatureGate`): sem resposta de acessos, nada aparece. Correto.
- **Estado servidor** com TanStack Query + polling de 15s no salão: simples e funcional.
- **Identidade visual** coerente e agradável (tema escuro âmbar, `.rise`, `.kds-overdue`).

---

## 3. Diagnóstico por severidade

Prioridade: **P0** = trava o uso em campo · **P1** = custa velocidade/erros diários · **P2** = polish e consistência.

| # | Sev | Problema | Evidência | Impacto |
|---|-----|----------|-----------|---------|
| 1 | **P0** | **Sem responsividade.** Topbar é uma fila única de ~12 links + chips + botões; sem menu compacto/overflow. Zero `@media`. | `components/AppShell.tsx` (nav flat); `grep @media` = 0 | Em tablet/celular a barra estoura e vira scroll horizontal. PDV roda em telas variadas. |
| 2 | **P0** | **`window.confirm/prompt/alert`** para ações reais (retirar 10%, liberar limite, cancelar pedido, abrir caixa). | 13 usos — `OrderDrawer`, `CashDrawer`, `ProductsPage`, `Users`, `Finance`, `Printing`, `Promotions`, `Employees` | Bloqueiam a UI, ignoram o tema, ruins no toque; `prompt` para "novo limite" é frágil (parse manual). |
| 3 | **P0** | **Acessibilidade ausente.** Botões só-ícone (`✕`, `→`, `💸`, `🖨`) sem rótulo; overlay sem `role="dialog"`, sem trap de foco, sem `Esc`, sem `aria-labelledby`; status só por cor. | `Overlay.tsx`, `OrderDrawer` (ícones), `grep aria/role/htmlFor` = 1 | Leitores de tela inutilizáveis; navegação por teclado quebrada; daltônicos não distinguem status. |
| 4 | **P1** | **UI montada em `style` inline**, sem camada de componentes. Tokens ótimos, mas reimplementados à mão em cada tela. | 40 inline styles em `ProductsPage`, 35 em `Printing`/`Finance`, 34 em `OrderDrawer`… | Inconsistência de espaçamento/estado (hover/focus), duplicação, manutenção cara, sem fonte única de verdade. |
| 5 | **P1** | **Sem feedback transitório (toast).** Sucesso é silencioso (item lançado, desconto aplicado); erro é texto inline que aparece/some. Sem UI otimista — todo toque espera refetch. | `OrderDrawer` (`actionError`), padrão em todas as mutations | No ritmo do balcão, o operador não tem confirmação clara; sensação de lentidão entre toque e tela. |
| 6 | **P1** | **Hierarquia do OrderDrawer** — a tela mais usada é uma pilha vertical de 6–8 botões com emojis; a ação primária ("Fechar conta") fica **no fim**, sem barra de ação fixa nem agrupamento primário/secundário. | `OrderDrawer.tsx` linhas 340–492 | Carga cognitiva alta e rolagem para a ação mais frequente; risco de toque errado. |
| 7 | **P1** | **Alvos de toque encolhidos.** O token é 48px, mas botões sobrescrevem para 36–44px em ações rápidas (avançar item, remover pagamento). | `minHeight: 36/38/44` em `OrderDrawer`, `PaymentPanel` | Abaixo do recomendado (44–48px); erros de toque em operação veloz. |
| 8 | **P2** | **Estados de carregamento/vazio** inconsistentes: "Carregando mesas…" em texto puro; sem skeleton; vazios mínimos. | `OrdersPage` L129, várias | Percepção de app "cru"; flashes durante polling. |
| 9 | **P2** | **Contraste** de `--ink-faint` (#6d6963) sobre `--bg` (#131316) provavelmente < WCAG AA para texto pequeno; `--ink-dim` no limite. | tokens em `global.css` | Legibilidade sob luz de ambiente de bar. |
| 10 | **P2** | **Sem atalhos de teclado / retorno de foco** após fechar modal; PDV costuma usar teclado/leitor de código. | `Overlay`, dialogs | Perde produtividade de operadores experientes. |

---

## 4. Design system proposto (a base da Fase 0)

Hoje existem tokens, mas não uma camada de componentes. Proposta de inventário mínimo em `src/ui/` (nomes sugeridos), tudo consumindo os tokens atuais:

- **Primitivos**: `Button` (variantes `primary`/`ghost`/`danger` + tamanhos + estado `loading` + `iconOnly` obrigando `aria-label`), `Input`/`Field` (label persistente + erro + `htmlFor`), `Select`, `Chip`/`StatusBadge` (cor **+** rótulo/ícone, nunca só cor), `Card`/`Ticket`.
- **Layout**: `Page` (padding/maxWidth padrão), `Toolbar`, `Stack`/`Cluster` (substituem `display:flex/grid` inline repetidos), `ActionBar` fixo (rodapé de ações do drawer).
- **Overlay**: `Modal`/`Drawer` acessível — `role="dialog"`, `aria-modal`, trap de foco, `Esc`, retorno de foco, scroll lock. Evolução direta do `Overlay.tsx` atual.
- **Feedback**: `Toast` (provider global) para sucesso/erro; `ConfirmDialog` e `PromptDialog` para **substituir** `window.confirm/prompt`; `Skeleton`, `EmptyState`, `ErrorState` (padroniza o `QueryError`).
- **Migração**: converter as classes já existentes (`.btn-*`, `.chip`, `.ticket`) em componentes; migrar tela a tela começando pelo fluxo crítico. Meta: reduzir `style={{…}}` de ~470 para uso pontual.

> Skills disponíveis para apoiar esta fase: `frontend-design`, `bencium-controlled-ux-designer` (decisões visuais/acessibilidade) e `vercel-react-best-practices` (performance dos componentes).

---

## 5. Roadmap em fases

### Fase 0 — Fundação (semana 1–2) · destrava tudo
Objetivo: base de componentes + acessibilidade mínima, sem mudar features.
- Criar `src/ui/` com `Button`, `Field`, `Modal/Drawer` acessível, `Toast` provider, `ConfirmDialog`/`PromptDialog`.
- Trocar os 13 `window.confirm/prompt/alert` por `ConfirmDialog`/`PromptDialog` (**P0 #2**).
- Tornar `Overlay` acessível (dialog/trap/Esc/foco) (**P0 #3**).
- Auditar contraste dos tokens e ajustar `--ink-faint`/`--ink-dim` (**P2 #9**).

### Fase 1 — Fluxo que gera receita (semana 3–4)
Foco em `OrdersPage` → `OrderDrawer` → `PaymentPanel` (o coração do PDV).
- Reestruturar o `OrderDrawer`: separar **itens** de **ações**; `ActionBar` fixo com ação primária "Fechar conta" sempre visível; agrupar secundárias (parcial, reabrir, retirar 10%, imprimir) em um grupo/menu (**P1 #6**).
- Toasts de sucesso em lançar item / aplicar desconto / pagamento; considerar **UI otimista** para lançar item (**P1 #5**).
- Padronizar alvos de toque ≥ 44px no fluxo (**P1 #7**).
- Skeletons/empty states no salão e no drawer (**P2 #8**).

### Fase 2 — Responsividade (semana 5)
- Topbar com navegação compacta (menu/overflow) e quebra responsiva de chips/ações (**P0 #1**).
- Grades já usam `auto-fill/minmax` (bom) — validar breakpoints de mesas/comandas em tablet retrato e celular.
- Testar em 3 larguras-alvo: celular (~390px), tablet retrato (~768px), balcão (~1280px).

### Fase 3 — Consistência & polish (semana 6+)
- Migrar telas administrativas restantes (`Products`, `Stock`, `Users`, `Employees`, `Finance`, `Printing`, `Promotions`) para os componentes de UI.
- Atalhos de teclado no fluxo (Enter confirma, foco automático) e retorno de foco (**P2 #10**).
- Revisão final de a11y (axe/Lighthouse) e de contraste.

---

## 6. Quick wins (dá para começar esta semana)

1. **`Button` + `Field` + `Toast`** — 3 componentes que já cortam a maior parte da inconsistência e do silêncio de feedback.
2. **`ConfirmDialog`** substituindo `window.confirm` no `OrderDrawer` (cancelar pedido, retirar 10%, reabrir) — impacto imediato no toque e no tema.
3. **`aria-label` nos botões só-ícone** (`✕`, `→`, `💸`, `🖨`) e `role="dialog"` no `Overlay` — baixo custo, alto ganho de acessibilidade.
4. **Ajuste de contraste** de `--ink-faint` — 1 linha de CSS, legibilidade melhor em todas as telas.

---

## 7. Como medir sucesso

- **Acessibilidade**: Lighthouse/axe a11y ≥ 90 nas telas do fluxo crítico; navegação completa por teclado; 0 botões só-ícone sem rótulo.
- **Consistência**: `style={{…}}` reduzido de ~470 para uso pontual; 0 `window.confirm/prompt/alert`.
- **Responsividade**: fluxo Salão→Pagamento sem scroll horizontal em 390/768/1280px.
- **Velocidade percebida**: feedback (toast/otimista) em 100% das mutations do fluxo; tempo toque→confirmação visual < 200ms nas ações otimistas.
- **Ergonomia**: 100% dos alvos de toque do fluxo ≥ 44px.

---

## 8. Observação

Vi que "Retirar 10% (gerente)" e o limite de comanda **já existem no front** (`OrderDrawer` usa `removeServiceFee`/`raiseCreditLimit`). Ou seja, a feature de isenção da taxa que preparei no backend provavelmente **já tem contraparte de UI** — vale alinhar os contratos antes de duplicar. O `raiseCreditLimit` hoje usa `window.prompt`, que é justamente um dos casos a migrar para `PromptDialog` na Fase 0.

## 11. Auditoria complementar — 2026-08-18

*Segunda passada, feita sob um prompt de "Frontend Architect / Senior UI-UX Designer" genérico (React + Tailwind). Adaptado à stack real do projeto — ver nota abaixo — e cruzado com números medidos no código, não estimativas.*

### 11.0 Nota sobre a adaptação do prompt

O prompt-base pedia Tailwind CSS. O SyncBar **não usa Tailwind** — usa CSS puro com design tokens (`:root` em `global.css`) e uma biblioteca de componentes própria em `src/ui/` (`Button`, `Field`, `Modal`, `Toast`, `Dialog`, `Switch`, `StatusBadge`). Essa base é sólida e já **consistente** (tema escuro + claro, tokens de cor/tipografia/raio/toque). Introduzir Tailwind por cima seria trocar um sistema coerente por dois sistemas concorrentes — o problema aqui não é falta de design system, é **adoção parcial** do que já existe. Por isso as recomendações abaixo estendem os componentes existentes, não substituem por outra stack.

### 11.1 Saúde do projeto — números medidos em 2026-08-18

| Métrica | Valor medido | Onde |
|---|---|---|
| `style={{…}}` inline | **708** ocorrências em 31 arquivos | grep em `src/` |
| `window.confirm/alert/prompt` | **0** | migrado 100% para `useDialog()` (era 13) |
| Regras `@media` no CSS global | **3** — todas dentro do bloco do `.topbar` | `global.css` |
| Uso de `aria-`/`role=` | **32** ocorrências em 14 arquivos, mas **24 delas concentradas nos 7 arquivos de `src/ui/`** — as ~24 telas de feature fora de `src/ui/` têm pouquíssima cobertura | grep em `src/` |
| Classe `.skeleton` definida vs. usada | Definida em `global.css`; **usada em 0 lugares** antes desta auditoria | grep `<Skeleton` / `className="skeleton"` |
| Texto "Carregando…" (único indicador de loading do app) | Presente em **4 de ~30 telas** (`OrdersPage`, `OrderDrawer`, `CashDrawer`, `PublicOrderPage`) — as outras ~26 não mostram nada durante o `isLoading` | grep em `src/` |
| Estados vazios ("Nenhum X…") | **16** ocorrências em 13 arquivos, todos texto simples sem CTA | grep em `src/` |
| `ErrorBoundary` | **Nenhum** — um erro de render em qualquer tela derruba a árvore inteira do React | grep em `src/` |
| Biblioteca de animação (Framer Motion etc.) | Não instalada — só CSS keyframes manuais (`rise`, `ui-shimmer`, `kds-pulse`, `ui-spin`) | `package.json` |
| Telas administrativas densas sem nenhuma responsividade | `PurchasingPage`, `StockPage`, `FinancePage`, `ReservationsPage`, `EmployeesPage`, `UsersPage` — zero `@media`/`overflow-x`/grid responsivo | grep + leitura |

O que já estava bom continua bom (ver seção 2 do documento original) e evoluiu: a acessibilidade dos componentes-base (`Modal`, `Dialog`, `Switch`, `Button`) está genuinely madura agora — foco preso, `Esc`, `role="dialog"`, `aria-checked`, `iconOnly` forçando `aria-label` no tipo do TypeScript. O que não evoluiu foi a **adoção** disso nas telas de feature.

### 11.2 Componentes novos implementados nesta rodada

Três peças de infraestrutura que faltavam, todas usando os tokens existentes (nenhuma cor nova, nenhuma dependência nova):

- **`src/ui/EmptyState.tsx`** — ícone + título + descrição + CTA opcional. Substitui a linha de texto cinza solta.
- **`src/ui/Skeleton.tsx`** (`SkeletonRow`, `SkeletonList`) — finalmente consome a classe `.skeleton` que já existia sem uso.
- **`src/components/ErrorBoundary.tsx`** — tela de fallback com botão "Recarregar"; já plugado em `main.tsx` envolvendo toda a árvore.

Aplicados como demonstração em **`ProductsPage.tsx`** (a tela que acabamos de modernizar): `menuQuery.isLoading` agora mostra `SkeletonList` em vez de nada, e a lista vazia virou `EmptyState` com botão "+ Novo produto" embutido — antes era só a frase "Nenhum produto cadastrado."

### 11.3 Checklist priorizado (novos itens, complementam a seção 3 do documento original)

**P0 — risco real de tela quebrada/em branco**
- ~~`ErrorBoundary` ausente~~ → feito (`main.tsx`).
- ~~Responsividade zero em `PurchasingPage`, `StockPage`, `FinancePage`, `ReservationsPage`, `EmployeesPage`, `UsersPage`~~ → feito nesta rodada. `StockPage`, `PurchasingPage` e `ReservationsPage` tinham modais artesanais com largura fixa em pixel (`width: 480/600/650/450/400`) — migrados para o `Modal` acessível do design system (`min(420-560px, 92-94vw)`, já responsivo). `FinancePage` já não tinha modais e usava grid `auto-fit`; `EmployeesPage`/`UsersPage` já usavam `Overlay`→`Modal`. O item de compra (produto/qtd/custo) que era um grid fixo de 4 colunas virou `ui-row ui-row-wrap` (empilha em telas estreitas).

**P1 — custa percepção de qualidade todo dia**
- ~~Levar `SkeletonList`/`EmptyState` para `StockPage` → `PurchasingPage` → `FinancePage` → `ReservationsPage` → `EmployeesPage`/`UsersPage`~~ → feito nesta rodada para essas 6 telas (fornecedores e compras tratados como duas listas independentes em `PurchasingPage`). Ainda restam ~20 outras telas de feature sem o mesmo tratamento — CustomersPage é a próxima da fila sugerida.
- Continuar a migração dos 708 `style={{…}}` restantes — não como um rewrite único (a build já demanda restart manual toda vez, então mudanças grandes de uma vez custam caro pra testar), e sim numa cota por sprint. As 6 telas desta rodada tiveram os campos de formulário dos modais migrados para `TextField`/`SelectField` (reduz uma fatia real do total, mas as listas/linhas ainda têm `style` inline pontual).

**P2 — polish**
- Auditoria de `aria-label` nas ~24 telas de feature fora de `src/ui/` — ícones sozinhos ali provavelmente ainda não têm rótulo. Nas 6 telas desta rodada os botões só-ícone dos modais antigos (✕ de fechar) foram substituídos pelo close nativo e acessível do `Modal`; o único novo ícone-só (remover item da compra) já leva `aria-label`.
- ~~Escala de transição~~ → feito. Tokens `--ease-standard`, `--ease-in-out`, `--duration-instant/fast/medium/base/entrance/slow/slower` adicionados ao `:root` em `global.css`, substituindo os números mágicos (`60ms`, `120ms`, `140ms`, `200ms`, `240ms`, `1.2s`, `1.6s`) nos pontos onde apareciam.

### 11.4 Micro-interações — observações

- ~~`.table-tile`/`.ticket` sem hover~~ → feito. `.table-tile` ganhou `transform: translateY(-1px)` além da borda; `.ticket-row` (linha de lista — usada em praticamente toda tela administrativa: estoque, funcionários, usuários, fornecedores, reservas, pedidos) ganhou destaque de fundo (`background: var(--bg-press)`) no hover, sinalizando "isso é uma linha" em telas usadas com mouse/trackpad.
- As classes `.rise`/`.rise-1`/`.rise-2`/`.rise-3` (entrada com fade+slide, stagger de 40-140ms) já existem e são usadas de forma inconsistente — algumas telas aplicam em todos os blocos, outras não aplicam em nenhum. Padronizar isso reforça a sensação de "sistema", não custa nada em bundle (é CSS puro) e já respeita `prefers-reduced-motion`.
- Sem Framer Motion instalado e com o projeto deliberadamente enxuto em dependências (Vite + poucos pacotes), a recomendação é continuar com CSS puro em vez de trazer uma lib de animação — o app não tem hoje nenhuma orquestração complexa (drag, layout animation entre rotas) que justifique o peso extra.

---

*Auditoria conduzida via leitura direta do código (não amostragem) para as métricas da seção 11.1; achados de "0 ocorrências" foram conferidos por grep, não inferidos.*
