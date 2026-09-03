Feature: Registrar movimentacao de caixa
    Regras de negocio do RegisterCashMovementCommandHandler: so registra suprimento, sangria ou
    despesa em uma sessao de caixa aberta e ativa.

Scenario: Registrar movimentacao em sessao de caixa inexistente deve falhar
    Given nao existe uma sessao de caixa para movimentacao com o id 1
    When eu registro uma movimentacao do tipo 1 no valor de 50.00 na sessao de caixa 1 do funcionario 5
    Then a operacao deve falhar com o erro "CashSession.NotFound"

Scenario: Registrar movimentacao em sessao de caixa fechada deve falhar
    Given a sessao de caixa 1 para movimentacao esta fechada
    When eu registro uma movimentacao do tipo 1 no valor de 50.00 na sessao de caixa 1 do funcionario 5
    Then a operacao deve falhar com o erro "CashSession.NotOpen"

Scenario: Registrar movimentacao em sessao de caixa aberta deve ter sucesso
    Given a sessao de caixa 1 para movimentacao esta aberta
    When eu registro uma movimentacao do tipo 1 no valor de 50.00 na sessao de caixa 1 do funcionario 5
    Then a operacao deve ter sucesso
