Feature: Consultar resumo de uma sessao de caixa
    Regras de negocio do GetCashSummaryQueryHandler: totaliza as vendas por metodo de pagamento e
    apura o caixa esperado (CashMath) a partir do fundo de troco, das vendas e das movimentacoes.

Scenario: Consultar resumo de sessao de caixa inexistente deve falhar
    Given nao existe uma sessao de caixa para resumo com o id 1
    When eu consulto o resumo da sessao de caixa 1
    Then a operacao deve falhar com o erro "CashSession.NotFound"

Scenario: Consultar resumo de sessao com vendas em dinheiro e cartao soma os totais corretamente
    Given a sessao de caixa 1 para resumo tem fundo de troco de 100.00
    And a sessao tem uma venda em dinheiro de 50.00
    And a sessao tem uma venda no cartao de credito de 30.00
    When eu consulto o resumo da sessao de caixa 1
    Then a operacao deve ter sucesso
    And o total de vendas do resumo deve ser 80.00
    And o resumo deve conter 2 totais de metodo de pagamento
    And o caixa esperado do resumo deve ser 150.00
