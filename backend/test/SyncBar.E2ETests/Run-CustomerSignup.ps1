[CmdletBinding()]
param(
    [Parameter(Mandatory)][uri]$BaseUrl,
    [Parameter(Mandatory)][ValidateRange(1, [long]::MaxValue)][long]$BranchId,
    [ValidateSet('All', 'API', 'Desktop', 'Android')][string]$Profile = 'All',
    [string]$SequenceFile,
    [switch]$ShowBrowser
)

$ErrorActionPreference = 'Stop'
if ($BaseUrl.Scheme -notin @('http', 'https') -or $BaseUrl.UserInfo) {
    throw 'BaseUrl deve ser HTTP(S), sem credenciais.'
}
$settings = @{
    E2E_RUN = '1'
    E2E_CUSTOMER_SIGNUP = '1'
    E2E_BASE_URL = $BaseUrl.AbsoluteUri
    E2E_BRANCH_ID = $BranchId.ToString()
    E2E_HEADLESS = $(if ($ShowBrowser) { '0' } else { '1' })
}
if ($SequenceFile) { $settings.E2E_CUSTOMER_SEQUENCE_FILE = [IO.Path]::GetFullPath($SequenceFile) }
$previous = @{}
$result = 1
try {
    foreach ($key in $settings.Keys) {
        $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, $settings[$key], 'Process')
    }
    $filter = 'Category=CustomerSignup'
    if ($Profile -ne 'All') { $filter += "&DisplayName~$Profile" }
    $reportDirectory = Join-Path $PSScriptRoot ("TestResults/customer-signup-" + [Guid]::NewGuid().ToString('N'))
    Write-Host "Validando cadastro de cliente em $BaseUrl (filial $BranchId, perfil $Profile). Os cadastros de teste serão mantidos."
    & dotnet test (Join-Path $PSScriptRoot 'SyncBar.E2ETests.csproj') --filter $filter --results-directory $reportDirectory --logger 'trx;LogFileName=customer-signup.trx' --logger 'console;verbosity=normal'
    $result = $LASTEXITCODE
    $reportPath = Join-Path $reportDirectory 'customer-signup.trx'
    if ($result -eq 0) {
        if (!(Test-Path -LiteralPath $reportPath)) { throw 'A execução não produziu relatório TRX.' }
        [xml]$report = Get-Content -LiteralPath $reportPath -Raw
        $expected = if ($Profile -eq 'All') { 3 } else { 1 }
        if ([int]$report.TestRun.ResultSummary.Counters.passed -ne $expected) {
            throw "Esperados $expected cenários aprovados. Testes ignorados ou não descobertos não comprovam operação."
        }
        Write-Host "OPERACIONAL: $expected cenário(s) de cadastro, login, sessão e endereço passaram. Relatório: $reportPath"
    }
}
finally {
    foreach ($key in $previous.Keys) { [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process') }
}
exit $result
