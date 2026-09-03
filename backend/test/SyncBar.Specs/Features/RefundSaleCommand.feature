Feature: Estornar venda
    Regras de negocio do RefundSaleCommandHandler: so estorna vendas ativas cuja sessao de caixa
    ainda esta aberta; ao estornar, desativa a venda, reabre o pedido para pagamento e registra
    um lancamento de estorno no caixa.

Scenario: Estornar venda inexistente deve falhar
    Given nao existe nenhuma venda com o id 1
    When eu tento estornar a venda 1 do funcionario 5
    Then a operacao deve falhar com o erro "Sale.NotFound"

Scenario: Estornar venda ja estornada anteriormente deve falhar
    Given uma venda 1 ja estornada anteriormente na sessao de caixa 10
    When eu tento estornar a venda 1 do funcionario 5
    Then a operacao deve falhar com o erro "Sale.NotFound"

Scenario: Estornar venda com a sessao de caixa ja fechada deve falhar
    Given uma venda 1 ativa na sessao de caixa 10 no valor de 50.00
    And a sessao de caixa 10 esta fechada
    When eu tento estornar a venda 1 do funcionario 5
    Then a operacao deve falhar com o erro "Sale.SessionClosed"

Scenario: Estornar venda com a sessao de caixa aberta deve ter sucesso
    Given uma venda 1 ativa na sessao de caixa 10 no valor de 50.00
    And a sessao de caixa 10 esta aberta
    When eu tento estornar a venda 1 do funcionario 5
    Then a operacao deve ter sucesso
