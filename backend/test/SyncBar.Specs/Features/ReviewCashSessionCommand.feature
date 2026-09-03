Feature: Conferir sessao de caixa
    Regras de negocio do ReviewCashSessionCommandHandler: so pode ser conferida uma sessao de caixa
    ja fechada.

Scenario: Conferir sessao de caixa inexistente deve falhar
    Given nao existe uma sessao de caixa para conferencia com o id 1
    When eu concluo a conferencia da sessao de caixa 1
    Then a operacao deve falhar com o erro "CashSession.NotFound"

Scenario: Conferir sessao de caixa ainda aberta deve falhar
    Given a sessao de caixa 1 para conferencia ainda esta aberta
    When eu concluo a conferencia da sessao de caixa 1
    Then a operacao deve falhar com o erro "CashSession.NotClosed"

Scenario: Conferir sessao de caixa fechada deve ter sucesso
    Given a sessao de caixa 1 para conferencia esta fechada
    When eu concluo a conferencia da sessao de caixa 1
    Then a operacao deve ter sucesso
