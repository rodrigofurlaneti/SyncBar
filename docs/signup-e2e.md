# Cadastro de empresa no servidor

O teste Selenium `SignupTests.NewCompany_ShouldRegisterAndAllowAdministratorLogin`
abre `/cadastro`, gera uma empresa/filial/administrador de teste, preenche o formulário,
confirma o redirecionamento para login, entra com a conta criada e recarrega o painel.
Os CPFs/CNPJs são sintéticos, com dígitos verificadores matematicamente válidos.
Nome, usuário e e-mail são diferentes a cada execução. A senha não é escrita nos logs.

Cada execução cria registros reais que permanecem no servidor. Por isso o teste
é habilitado explicitamente e não possui repetição automática.

```powershell
$env:E2E_RUN = '1'
$env:E2E_SIGNUP = '1'
$env:E2E_BASE_URL = 'http://9.205.156.87:84'
dotnet test backend/test/SyncBar.E2ETests/SyncBar.E2ETests.csproj --filter 'FullyQualifiedName~SignupTests' --logger 'console;verbosity=detailed'
```

Use a origem HTTPS quando estiver disponível com certificado válido. O navegador
não ignora erro de certificado. `E2E_SIGNUP` não habilita outros testes de login.

Em 08/09/2026, uma execução no servidor HTTP passou: cadastro, login e sessão após
recarregamento. Isso valida esse cenário; não valida cobrança, iFood ou os demais
fluxos do painel. O usuário de teste aparece na saída detalhada.

O carregamento Selenium usa `PageLoadStrategy.Eager` e espera os elementos da aplicação,
para não depender da conclusão de todos os recursos secundários. Falhas de API ou
ausência do formulário continuam falhando, sem serem mascaradas por esperas fixas.
