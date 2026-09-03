Feature: Fechar sessao de caixa
    Regras de negocio do CloseCashSessionCommandHandler: so fecha uma sessao aberta, com valor de
    fechamento nao negativo, e apura o caixa esperado (fundo de troco + recebimentos em dinheiro −
    sangrias − despesas + suprimentos) para calcular a diferenca de caixa.

Scenario: Fechar sessao de caixa inexistente deve falhar
    Given nao existe nenhuma sessao de caixa com o id 1
    When eu fecho a sessao de caixa 1 do funcionario 5 com valor de fechamento de 100.00
    Then a operacao deve falhar com o erro "CashSession.NotFound"

Scenario: Fechar sessao de caixa ja fechada deve falhar
    Given a sessao de caixa 1 ja esta fechada
    When eu fecho a sessao de caixa 1 do funcionario 5 com valor de fechamento de 100.00
    Then a operacao deve falhar com o erro "CashSession.NotOpen"

Scenario: Fechar sessao de caixa com valor de fechamento negativo deve falhar
    Given a sessao de caixa 1 esta aberta com fundo de troco de 100.00
    When eu fecho a sessao de caixa 1 do funcionario 5 com valor de fechamento de -10.00
    Then a operacao deve falhar com o erro "CashSession.InvalidClosingAmount"

Scenario: Fechar sessao de caixa aberta sem movimentacoes apura diferenca zero
    Given a sessao de caixa 1 esta aberta com fundo de troco de 100.00
    When eu fecho a sessao de caixa 1 do funcionario 5 com valor de fechamento de 100.00
    Then a operacao deve ter sucesso
    And a diferenca de caixa apurada deve ser 0.00
