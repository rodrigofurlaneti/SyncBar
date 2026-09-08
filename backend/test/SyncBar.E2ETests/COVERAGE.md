# Cobertura E2E e sequência de implementação

Esta matriz separa cenários implementados de fluxos ainda pendentes. Testar o
redirecionamento de uma página não valida seu CRUD nem as regras de negócio.

## Implementados

| Área | Cenários | Classe |
|---|---|---|
| Login | Renderização, campos obrigatórios, proteção de Delivery | ProductionTests |
| Sessão | Login real e recarregamento, habilitação explícita | ProductionTests |
| Controle de acesso anônimo | 36 rotas protegidas exigem login | AnonymousAccessTests |
| Responsividade do login | 390, 768 e 1440 de largura; controles acessíveis e sem overflow horizontal | AnonymousAccessTests |
| Cadastro de estabelecimento | 9 campos obrigatórios, sem submissão | AnonymousAccessTests |

## Fluxos a implementar com o cadastro inicial compartilhado

O usuário autorizou usar empresa/filial exclusiva de testes em produção, com
integrações em sandbox. Ainda é preciso distinguir cadastro de estabelecimento
de cadastro de consumidor para definir a preparação dos dados.

| Área | Cenários funcionais pendentes |
|---|---|
| Cadastro inicial | Sucesso, campos inválidos, duplicidade, dados persistidos e conta utilizável |
| Equipe e usuários | Criar/editar/desativar, CPF/e-mail/username duplicados, vínculo e senha |
| Permissões | Gerente, funcionário com e sem feature, rotas e API, isolamento por empresa |
| Catálogo | Categoria/produto/complementos: criar/editar/desativar, preço, ordem, vínculo e pesquisa |
| Clientes | Cadastro, edição disponível, busca, fidelidade e duplicidade |
| Salão | Mesas/comandas, abertura de pedido, itens, observações, transferência e cancelamento |
| Cardápio digital | Identificação, senha/confirmar senha, carrinho, complementos, token inválido/encerrado |
| Preparo/Delivery | Pedido recebido, cozinha, pronto, despacho, entrega, cancelamento e atualização visual |
| Caixa | Abertura, suprimento/sangria, pagamento parcial/total, troco, estorno e conferência |
| Fechamento/turno | Valores por modalidade, diferenças, persistência, bloqueio com caixa aberto |
| Estoque/compras | Entrada, baixa, saldo, fornecedores e compras |
| Reservas/promoções | Cadastro, validações, vigência, aplicação e cancelamento |
| Impressão | Configuração, conteúdo da comanda, falha do dispositivo e reimpressão |
| Asaas | Credenciais, métodos/herança, cobrança sandbox, webhook duplicado, status e modais |
| iFood | Autorização, loja, catálogo, pedidos/eventos, presença, assinatura, duplicidade, logística, avaliações e financeiro |
| Keeta | OAuth/HMAC, onboarding, catálogo, eventos, pedido e entrega sandbox |
| WhatsApp | Fila, envio de mídia para destinatário de teste autorizado, timeout e retentativa |

## Reutilização dos dados

O cadastro inicial deve ser executado uma vez por fluxo, e não depender da ordem
alfabética dos testes xUnit. O contexto compartilhado deve conter apenas IDs retornados
pelo cadastro e os registros criados pela execução. Nunca usar IDs fixos (por exemplo
empresa/filial 1) como fallback. Senhas/tokens permanecem na memória ou no ambiente,
fora dos relatórios. Se o cadastro inicial falhar, os cenários dependentes não podem
continuar contra outro cliente. Não executar cobrança, emissão fiscal, impressão física
ou mensagens reais como efeito colateral de preparação de dados.

Não há alegação de cobertura total: os fluxos na segunda tabela ainda não estão implementados.
