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
