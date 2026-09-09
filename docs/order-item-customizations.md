# Opcionais e boosts no lançamento de Mesas/Comandas

O cardápio operacional retorna HasOptionalExtras e HasBoosts. O drawer mantém
o lançamento direto para produtos simples e abre o seletor quando há configuração
ou grupos de complementos existentes. As opções atuais são carregadas por
GET /api/products/{id}, acessível ao atendimento; não são usadas as consultas de
manutenção restritas a gestores.

O seletor mostra somente seções habilitadas com itens disponíveis, permite nenhuma
seleção, exibe subtotal e conserva as escolhas caso o lançamento falhe.
Cancelar não lança itens. Escape fecha apenas o seletor sobre o drawer.

POST /api/orders/{id}/items aceita optionalExtraIds e boostIds, além do payload
anterior. O backend rejeita IDs duplicados, indisponíveis, inativos ou vinculados
a outro produto e obtém os preços do cadastro. O cliente não envia preços.

## Preços e histórico

OrderItem.UnitPrice inclui o preço base (com eventual desconto promocional) e
os boosts escolhidos por unidade. Exemplo: base R$ 20 + boost R$ 2, quantidade 3
= R$ 66. Opcionais não acrescentam valor. Complementos anteriores conservam
sua regra de cobrança por linha. O limite da comanda considera os boosts.

Nomes e preços são congelados em OrderItemOptionalExtra e OrderItemBoost.
Alterações/inativações futuras do catálogo não mudam pedidos já lançados.
As escolhas acompanham transferências e aparecem na fila de preparo e no ticket
da cozinha/bar. O resumo lateral usa os nomes congelados retornados pelo pedido.
Como nos complementos existentes, a configuração é aplicada à linha solicitada;
a linha gratuita gerada pela promoção em dobro continua sendo o produto básico.

## Banco e validação

Migration: 202609090003_AddOrderItemCustomizations. Cria orderitemoptionalextra
e orderitemboost, com auditoria, FK para o item do pedido e referências ao cadastro.
Excluir o item do pedido remove suas escolhas; referências históricas ao cadastro
usam Restrict, compatível com a inativação já utilizada pelos CRUDs.
A API executa as migrations ao iniciar: publicar API e frontend juntos.

Validação realizada:

- 701 testes backend selecionados aprovados, incluindo preço por quantidade,
  promoção, limite, IDs inválidos e persistência/transferência com SQLite.
- 10 cenários de customização no Playwright aprovados, com API simulada;
  regressão das telas de pedidos também aprovada (11 testes na rodada anterior).
- Build da API sem avisos/erros, TypeScript e build de produção React aprovados.
- Migration executada pelo EF em MySQL 8 temporário; FKs, persistência de preços
  após edição do cadastro e cascade das escolhas verificados.

Nenhuma migration foi aplicada ao banco do estabelecimento e não houve deploy.
