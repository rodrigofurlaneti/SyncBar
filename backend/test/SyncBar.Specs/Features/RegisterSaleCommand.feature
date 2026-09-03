Feature: Registrar venda
    Regras de negocio do RegisterSaleCommandHandler: so registra a venda de um pedido aguardando
    pagamento, com a sessao de caixa aberta e sem venda ativa duplicada; a soma dos pagamentos deve
    cobrir o total do pedido e troco so e permitido em pagamentos em dinheiro. Ao concluir, marca o
    pedido como pago.

Scenario: Registrar venda de pedido inexistente deve falhar
    Given nao existe nenhum pedido para venda com o id 1
    When eu registro a venda do pedido 1 na sessao de caixa 10 do funcionario 5 com um pagamento de 100.00 no metodo 1
    Then a operacao deve falhar com o erro "CustomerOrder.NotFound"

Scenario: Registrar venda de pedido ainda em andamento deve falhar
    Given um pedido de mesa 1 ainda em andamento com total de 100.00
    When eu registro a venda do pedido 1 na sessao de caixa 10 do funcionario 5 com um pagamento de 100.00 no metodo 1
    Then a operacao deve falhar com o erro "Sale.OrderNotAwaitingPayment"

Scenario: Registrar venda com a sessao de caixa fechada deve falhar
    Given um pedido de mesa 1 aguardando pagamento com total de 100.00
    And a sessao de caixa 10 esta fechada para vendas
    When eu registro a venda do pedido 1 na sessao de caixa 10 do funcionario 5 com um pagamento de 100.00 no metodo 1
    Then a operacao deve falhar com o erro "CashSession.NotOpen"

Scenario: Registrar venda de pedido que ja tem venda ativa deve falhar
    Given um pedido de mesa 1 aguardando pagamento com total de 100.00
    And a sessao de caixa 10 esta aberta para vendas
    And o pedido ja possui uma venda ativa registrada
    When eu registro a venda do pedido 1 na sessao de caixa 10 do funcionario 5 com um pagamento de 100.00 no metodo 1
    Then a operacao deve falhar com o erro "Sale.Duplicate"

Scenario: Registrar venda com troco em pagamento que nao e dinheiro deve falhar
    Given um pedido de mesa 1 aguardando pagamento com total de 100.00
    And a sessao de caixa 10 esta aberta para vendas
    When eu registro a venda do pedido 1 na sessao de caixa 10 do funcionario 5 com um pagamento de 100.00 no metodo 2 e troco de 5.00
    Then a operacao deve falhar com o erro "Sale.ChangeNotAllowed"

Scenario: Registrar venda com pagamento insuficiente deve falhar
    Given um pedido de mesa 1 aguardando pagamento com total de 100.00
    And a sessao de caixa 10 esta aberta para vendas
    When eu registro a venda do pedido 1 na sessao de caixa 10 do funcionario 5 com um pagamento de 40.00 no metodo 1
    Then a operacao deve falhar com o erro "Sale.InsufficientPayment"

Scenario: Registrar venda com pagamento completo deve ter sucesso
    Given um pedido de mesa 1 aguardando pagamento com total de 100.00
    And a sessao de caixa 10 esta aberta para vendas
    When eu registro a venda do pedido 1 na sessao de caixa 10 do funcionario 5 com um pagamento de 100.00 no metodo 1
    Then a operacao deve ter sucesso
