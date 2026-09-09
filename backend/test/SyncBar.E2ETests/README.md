# SyncBar.E2ETests

Selenium + xUnit, .NET 9. Testes de fumaça contra a aplicação publicada, sem mocks.
Destino padrão: http://9.205.156.87:84. Não desabilita a validação de certificados.

Pré-requisitos: SDK .NET 9, Google Chrome e acesso à rede. Selenium Manager resolve o
driver compatível; na primeira execução pode precisar baixá-lo.

## Executar no PowerShell

Na raiz do repositório:

```powershell
$env:E2E_RUN = '1'
dotnet test backend/test/SyncBar.E2ETests/SyncBar.E2ETests.csproj --logger 'trx;LogFileName=e2e.trx'
```

Para ver o navegador, defina `$env:E2E_HEADLESS = '0'`.
Para mudar o destino, defina `$env:E2E_BASE_URL = 'https://9.205.156.87'` após configurar HTTPS.

## Login real (opcional)

Use uma conta exclusiva de testes com acesso ao Salão. Defina E2E_AUTH=1,
E2E_USERNAME e E2E_PASSWORD no ambiente do processo ou nos secrets da execução.
Nunca versione credenciais. Se AUTH estiver ligado e as credenciais faltarem, o teste falha.
No endereço HTTP atual é necessário definir E2E_ALLOW_HTTP_LOGIN=1 explicitamente:
a senha trafega sem criptografia. Prefira executar este cenário depois de habilitar HTTPS.

O login real pode atualizar auditoria de acesso/sessões. Há uma única tentativa,
sem retry automático para evitar bloqueio de conta. O navegador é descartado por teste.
Não capturamos HTML, screenshots ou credenciais nos relatórios desta suíte de produção.

## Cobertura e limites

Consulte [COVERAGE.md](COVERAGE.md) para a matriz de cenários implementados e pendentes.

- Proteção anônima de todas as 36 rotas explicitamente protegidas no App.tsx.
- Layout de login em três tamanhos de tela e nove campos obrigatórios no cadastro de estabelecimento.

- Formulário de login visível e utilizável.
- Campos obrigatórios, sem envio de senhas inválidas.
- Redirecionamento de visitante anônimo ao abrir Delivery.
- Login real e persistência de sessão após recarregar (opt-in).

Os três primeiros cenários não comprovam a saúde da API nem a conectividade com o banco.
Mesmo o login não comprova os fluxos de caixa, pedido ou pagamento. Cenários que criam
vendas, acionam integrações ou movimentam dinheiro devem usar homologação com dados isolados.

O projeto está na solução, mas sem E2E_RUN=1 os testes são exibidos como ignorados,
evitando acesso à produção durante o CI comum. Para rodar isoladamente use o comando acima.
Resultados TRX ficam em TestResults (ignorado pelo Git). Não há execução agendada.

## Cadastro do cliente do site — validação após publicar

Execute na raiz do repositório, substituindo o domínio e o ID da filial:

```powershell
./backend/test/SyncBar.E2ETests/Run-CustomerSignup.ps1 `
  -BaseUrl 'https://seu-site-publicado.com' -BranchId 7
```

São três cadastros reais, sem mocks:

1. **API:** cadastro → login → criação do endereço → renovação de token → leitura do endereço persistido.
2. **Desktop:** produto no carrinho → novo cliente → validação de campos obrigatórios → cadastro/login automático/endereço → verificação independente pela API.
3. **Android:** o mesmo fluxo com Chrome em modo de emulação mobile, toque e user agent Android 16.

O teste para antes de enviar pedidos ou pagamentos. Os registros ficam no servidor.
O perfil Android usa o Chrome instalado na máquina; não equivale a executar num
Android físico nem instala a versão 152. ViaCEP é consultado de verdade. Caso ele
falhe, testa-se o preenchimento manual permitido pelo aplicativo e isso é registrado.

**Dados:** `João Furlaneti Teste 1`, `João Furlaneti Teste 2`, etc.; CPF aleatório com
os dois dígitos verificadores válidos; CEP sempre **07025020**; e-mail único no domínio
example.com e senha aleatória. CPF sintético não representa identidade real verificada.
Senhas, tokens e CPF completo não são escritos nos relatórios. Os logs mostram nome,
perfil, filial, CustomerId, horário UTC e endpoint da falha, quando houver.

**Sequência:** por padrão, o contador fica em `%LOCALAPPDATA%/SyncBar/E2E`, separado
por origem e filial. Cada cenário reserva um número antes da tentativa, então falhas
podem deixar lacunas. Uma nova execução usa o próximo número. Não se consulta nem
se altera o nome de clientes já existentes no banco.
Para compartilhar a sequência entre máquinas/CI, preserve e reutilize o mesmo arquivo:

```powershell
./backend/test/SyncBar.E2ETests/Run-CustomerSignup.ps1 `
  -BaseUrl 'https://seu-site-publicado.com' -BranchId 7 `
  -Profile Android -SequenceFile 'C:/SyncBar-E2E/customer-sequence.txt'
```

Sem esse arquivo compartilhado, outra máquina inicia sua própria sequência em 1.
Não apague o contador se quiser manter a numeração. O arquivo contém somente o
último número reservado. Para continuar após cadastros feitos fora da suíte, grave
nele esse último número antes da execução, com nenhum teste rodando.

Use `-Profile API`, `-Profile Desktop` ou `-Profile Android` para apenas um cadastro;
`-ShowBrowser` exibe o Chrome. Opcionalmente, `E2E_PRODUCT_ID` seleciona um produto
específico do cardápio; por padrão usa-se o primeiro. Grupos obrigatórios do wizard
são preenchidos com as primeiras opções disponíveis. A filial precisa ter produto ativo.

O script exige URL/filial explícitas, habilita somente os cenários de cliente e
confere no TRX se todos os cenários esperados passaram. Qualquer falha retorna código
não zero para o pipeline. **Teste ignorado não significa ambiente operacional.**
Sem as flags, os testes de ambiente continuam ignorados. Apenas os testes locais
de geração de CPF e contador executam no CI comum, sem acessar rede ou banco.
