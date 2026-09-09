# Cadastro no checkout móvel

## Diagnóstico registrado antes da correção

Reprodução local com Playwright: Chromium desktop, Pixel 7/Chromium e iPhone 13/WebKit.
API e ViaCEP simulados; sem cadastros ou pedidos em produção.

- O cenário de sucesso existente passou nos três perfis.
- Ao abortar a consulta ViaCEP, os campos obrigatórios Rua / Avenida e Bairro / Cidade
  permanecem vazios e readonly. O usuário não consegue corrigir o endereço e concluir.
  A consulta também não tem timeout: uma resposta pendente mantém o botão desabilitado.
- Uma resposta 409 com “CPF já cadastrado.” cria um SweetAlert com z-index 1060,
  atrás do modal de cadastro (10000) e do carrinho (9999). A mensagem existe no DOM,
  mas o teste de hit-testing confirma que está encoberta. Só testar toBeVisible
  não detectava o problema.

Essas falhas não são exclusivas de mobile; condições de erro também as reproduzem
no desktop. Não há evidência de bloqueio de token por ITP/localStorage: a sessão
do cliente usa token em memória e Authorization. As chamadas à API são relativas
à origem; ViaCEP é uma dependência externa.

Não foram fornecidos URL do incidente, logs de rede ou acesso a Android/iPhone
físicos. A associação entre os defeitos reproduzidos e o incidente em Azure ainda
precisa ser confirmada. Nenhum card externo foi identificado para publicar este registro.

## Correção

- ViaCEP tem timeout de 8 segundos e cancelamento ao trocar o CEP ou fechar o modal.
  Respostas antigas são descartadas. Rua e bairro/cidade podem ser digitados ou corrigidos;
  o botão “Preencher endereço manualmente” permite abandonar a consulta imediatamente.
- Os erros de login, cadastro e campos obrigatórios aparecem dentro do formulário,
  com role=alert, foco e rolagem até a mensagem. Não dependem de outro modal sobreposto.
- O formulário valida CPF/CEP pelo comprimento, e-mail e preenchimento antes de enviar.
  A validação de negócio permanece na API; senhas não são normalizadas.
- O cadastro continua com login do cliente, endereço autenticado e retorno ao checkout.
  Não foram alterados contratos de API, persistência de tokens ou CORS.

## Validação

Antes da correção, os dois testes de regressão falharam nos três perfis (6 falhas);
o cenário de sucesso existente passou nos três. Depois, os 7 cenários de cadastro
passaram nos três perfis (21 combinações), incluindo cadastro e pedido com endereço
manual após falha de CEP, timeout, cancelamento manual, troca de CEP, erro de CPF
encoberto e campos obrigatórios em viewport de 390 × 360.

Comando: `node node_modules/@playwright/test/cli.js test --config playwright.storefront-devices.config.ts`
na pasta frontend. TypeScript e build de produção também passaram.
Regressão completa: **63 testes aprovados** (21 cenários × 3 perfis), cobrindo
cadastro, retirada/entrega, opções de pagamento e falhas de pedido/pagamento.

Os testes simulam respostas HTTP, inclusive autenticação, endereço e pedido.
Emulação de Pixel/iPhone não equivale a testes em Chrome Android e Safari iOS físicos.
Após publicar o frontend, repetir no endereço Azure do incidente, em ambos os aparelhos,
com teclado aberto e conexão móvel, verificando cadastro, endereço e pedido e os erros
de validação. Registrar status das requisições sem expor senha, CPF ou tokens.
