Feature: Abrir sessao de caixa
    Regras de negocio do OpenCashSessionCommandHandler: um caixa so pode ter uma sessao aberta por
    vez.

Scenario: Abrir caixa que ja tem uma sessao aberta deve falhar
    Given o caixa 1 ja tem uma sessao aberta
    When eu abro o caixa 1 com fundo de troco de 100.00 pelo funcionario 5
    Then a operacao deve falhar com o erro "CashSession.AlreadyOpen"

Scenario: Abrir caixa sem sessao aberta deve ter sucesso
    Given o caixa 1 nao tem sessao aberta para abertura
    When eu abro o caixa 1 com fundo de troco de 100.00 pelo funcionario 5
    Then a operacao deve ter sucesso
