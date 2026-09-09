# Opcionais e boosts no pedido público por QR Code

## Diagnóstico antes da implementação

O PublicOrderPage utilizava somente os grupos de complementos antigos. O menu público não retornava as flags e coleções novas, o contrato de adição não recebia seus IDs e o callback do modal substituía a quantidade por 1. Além disso, o destino mesa não passava pelo TableReadingValidation mesmo quando o backend exigia comprovação de leitura; código de barras/QR sem câmera também escapavam desse caminho. Os overlays com z-index 9999 podiam encobrir os alertas de erro.

## Comportamento implementado

- Produto simples mantém o fluxo de lançamento existente. Produto com flags ativas ou grupos abre o ProductCustomizationModal compartilhado, usando dados do menu público, sem acessar endpoints administrativos.
- Opcionais/boosts ativos são retornados ordenados e somente quando a flag correspondente estiver ligada. O wizard oculta etapas vazias, permite pular opcionais e mantém os grupos antigos.
- A quantidade escolhida é preservada no subtotal e em todas as etapas até o envio. Exemplo: 2 unidades de R$ 20 + boost de R$ 6 = R$ 52.
- O payload público inclui optionalExtraIds e boostIds; o servidor consulta o catálogo, rejeita IDs duplicados, inativos, de outro produto ou com flag desligada e calcula o preço. Nomes/preços ficam gravados como snapshots nos itens.
- O destino mesa ou comanda continua separado. As seleções sobrevivem à escolha do destino e à validação de leitura. Sem visualização QR e com câmera ligada, permanece o vínculo de comanda existente. Sem câmera e com leitura obrigatória, é solicitada a leitura da mesa.
- Conta da mesa e da comanda retornam e exibem os snapshots. Inativar posteriormente uma opção no catálogo não apaga as escolhas do pedido.
- QR de mesa inativa é rejeitado também no menu e na consulta. ExpectedOrderId deve estar aberto, na filial e no destino correto. Erros ficam acima dos overlays, e o envio em andamento bloqueia confirmação/cancelamento.

## Validação automatizada executada

201 testes de backend aprovados, incluindo PublicOrdering, Storefront e AddOrderItem. Cobertura nova: envio com boosts para mesa/comanda, quantidade e total, seleções inválidas/duplicadas/inativas/desligadas, menu ordenado, snapshots da conta, bloqueio sem leitura obrigatória e destino incompatível. A suíte existente cobre token inválido, filial/produto/comanda indisponíveis, empresa diferente, abertura/reuso do pedido, limite da comanda e falha de impressão.

32 testes de navegador do fluxo público aprovados (8 cenários em quatro perfis: desktop Chromium, tablet, Android pequeno Chromium e iPhone WebKit):

| Cenário | Resultado esperado |
| --- | --- |
| Personalização para mesa | IDs corretos, quantidade 2 e subtotal R$ 52 |
| Personalização para comanda | Mesmas escolhas com código 001 |
| Produto simples e conta | Sem wizard; conta exibe nomes e preços salvos |
| Cancelamento e leitura obrigatória | Nenhum lançamento antes da validação |
| Erro do servidor | Alerta visível e escolhas preservadas para nova confirmação |
| Foto de validação da mesa | Proof encaminhado junto das seleções |
| Foto de validação da comanda | Proof e destino encaminhados corretamente |
| Token inválido | Erro no menu, sem possibilidade de pedir |

Também passaram 8 testes de integração do wizard no storefront, TypeScript e build Vite. Os testes de navegador usam APIs simuladas; os de backend usam repositórios simulados. Não equivalem a uma execução completa contra Azure/banco real.

Comandos (na raiz, backend; na pasta frontend, navegador):

~~~powershell
dotnet test backend/test/SyncBar.Tests/SyncBar.Tests.csproj --no-restore --filter 'FullyQualifiedName~PublicOrdering|FullyQualifiedName~Storefront|FullyQualifiedName~AddOrderItem'
node node_modules/@playwright/test/cli.js test --config playwright.wizard.config.ts --grep 'QR '
~~~

## Homologação após publicação

1. Publicar API e frontend compatíveis. Esta integração reutiliza as tabelas/migrations anteriores de opcionais, boosts e snapshots; não cria migration nova.
2. Abrir o QR real de uma mesa de teste no Chrome Android e Safari iPhone. Conferir filial, mesa e funcionário de autoatendimento configurado.
3. Pedir produto simples, somente opcional, somente boost e combinação com quantidade 2; conferir conta, painel operacional e impressão na cozinha/bar.
4. Repetir para uma comanda da mesma filial e confirmar que seu consumo não entra na conta da mesa.
5. Testar as configurações utilizadas pelo estabelecimento: sem leitura, foto, código de barras e QR. Negar a permissão de câmera, permitir novamente e conferir recuperação. Os testes automatizados de foto simulam arquivo e resposta da API; não acionam câmera física.
6. Desativar uma opção após carregar o menu, tentar enviá-la e conferir o erro. Reabrir o cardápio para selecionar as opções atuais.
7. Encerrar o atendimento no painel e tentar enviar novamente pela aba antiga; deve ser rejeitado. Iniciar um novo acesso pelo QR para novo atendimento.

Não houve publicação, alteração do banco de produção ou pedido real nesta execução. A cobertura descrita é uma matriz de regressão, não prova de todos os estados possíveis de rede/dispositivo. Em perda de conexão depois de um envio, conferir a conta antes de repetir: o endpoint existente não oferece chave de idempotência para garantir deduplicação entre requisições independentes.
