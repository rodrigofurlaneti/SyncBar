Feature: Registrar pagamento parcial de conta de mesa
    Regras de negocio do RegisterPartialPaymentCommandHandler: pagamento parcial so e permitido em
    contas de mesa ainda nao encerradas, com a sessao de caixa aberta, e o valor nao pode faltar
    nem exceder o saldo restante da conta (descontados pagamentos parciais ja feitos).

Scenario: Pagamento parcial em pedido inexistente deve falhar
    Given nao existe nenhum pedido com o id 1
    When eu registro um pagamento parcial de 20.00 no pedido 1 na sessao de caixa 10 pelo metodo 2 do funcionario 5
    Then a operacao deve falhar com o erro "CustomerOrder.NotFound"

Scenario: Pagamento parcial em pedido de comanda deve falhar
    Given um pedido de comanda 1 sem mesa associada
    When eu registro um pagamento parcial de 20.00 no pedido 1 na sessao de caixa 10 pelo metodo 2 do funcionario 5
    Then a operacao deve falhar com o erro "PartialPayment.TableOnly"

Scenario: Pagamento parcial em pedido de mesa ja pago deve falhar
    Given um pedido de mesa 1 ja pago com total de 100.00
    When eu registro um pagamento parcial de 20.00 no pedido 1 na sessao de caixa 10 pelo metodo 2 do funcionario 5
    Then a operacao deve falhar com o erro "PartialPayment.OrderClosed"

Scenario: Pagamento parcial com a sessao de caixa fechada deve falhar
    Given um pedido de mesa 1 aberto com total de 100.00
    And nao existe uma sessao de caixa aberta com o id 10
    When eu registro um pagamento parcial de 20.00 no pedido 1 na sessao de caixa 10 pelo metodo 2 do funcionario 5
    Then a operacao deve falhar com o erro "CashSession.NotOpen"

Scenario: Pagamento parcial sem saldo restante deve falhar
    Given um pedido de mesa 1 aberto com total de 100.00
    And a sessao de caixa 10 esta aberta
    And o pedido ja tem pagamentos parciais totalizando 100.00
    When eu registro um pagamento parcial de 20.00 no pedido 1 na sessao de caixa 10 pelo metodo 2 do funcionario 5
    Then a operacao deve falhar com o erro "PartialPayment.NothingRemaining"

Scenario: Pagamento parcial que excede o saldo restante deve falhar
    Given um pedido de mesa 1 aberto com total de 100.00
    And a sessao de caixa 10 esta aberta
    When eu registro um pagamento parcial de 150.00 no pedido 1 na sessao de caixa 10 pelo metodo 2 do funcionario 5
    Then a operacao deve falhar com o erro "PartialPayment.ExceedsRemaining"

Scenario: Pagamento parcial dentro do saldo restante deve ter sucesso
    Given um pedido de mesa 1 aberto com total de 100.00
    And a sessao de caixa 10 esta aberta
    When eu registro um pagamento parcial de 40.00 no pedido 1 na sessao de caixa 10 pelo metodo 2 do funcionario 5
    Then a operacao deve ter sucesso
