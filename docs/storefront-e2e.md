# Testes do cardápio público

## Execução local

Na pasta `frontend`, execute `npm run test:e2e:storefront`.
Playwright inicia o Vite e executa a interface no navegador com respostas HTTP
simuladas. Nenhum pedido, cobrança ou cliente é criado no servidor nessa suíte.

Cobertura: cadastro com nome/e-mail dinâmicos e CPF sintético, endereço por CEP,
validação e visibilidade de senha, busca, carrinho e remoção, retirada e entrega,
PIX, boleto, crédito, débito e maquininha, confirmação de pagamento simulada,
formas de pagamento desativadas/indisponíveis, falha de cadastro, pedido e pagamento,
falha ao carregar cardápio e cópia do link.

Os testes verificam os vínculos entre cliente, endereço, pedido e pagamento e
a quantidade de requisições. Um sucesso simulado não valida Asaas, banco ou webhook.
Complementos, cupons, cancelamento e o processamento de webhook no backend não
estão cobertos por esta suíte.

## Execução contra o servidor

Execute `npm run test:e2e:storefront:live`. O destino padrão é
`http://9.205.156.87:84/cardapio/1`; altere a origem com `STOREFRONT_BASE_URL`.
Não há mocks nessa execução: ela cadastra um cliente e endereço, recarrega a página,
faz login com a senha recém-gerada e chega às opções de entrega/pagamento.
Não envia pedido nem cria cobrança. Requer cardápio com um produto sem complementos,
ViaCEP disponível e pelo menos uma forma de pagamento configurada.

Cada execução usa `João Furlaneti Teste <timestamp>-<sufixo>`, e-mail em
`example.com`, senha aleatória e CPF com dígitos verificadores calculados por módulo 11.
CPF sintético significa validade matemática, não confirmação de cadastro na Receita
nem garantia de que o número não pertença a alguém. Os dados servem para testes.

O teste real não repete automaticamente cadastros após falha. Clientes criados
permanecem no banco, identificados pelo nome de teste. O relatório registra nome,
e-mail e ID, sem senha ou CPF; traces de rede ficam desativados.

## Falha encontrada em 08/09/2026

A execução real retornou **401 em POST /api/customerappusers**, antes de criar
cliente/endereço/pedido. O controller `CustomerAppUsersController` tem `[Authorize]`
e o POST não possui acesso público. O cardápio tenta chamar essa rota sem sessão
ao registrar um visitante. É necessário implementar o cadastro público com validação
de filial/empresa e proteção apropriada; não remover a autorização das operações
administrativas de consulta, alteração e exclusão.

Até corrigir e publicar esse contrato, o teste real deve continuar falhando:
não aceitar 401 como sucesso nem simular a resposta na suíte real.
