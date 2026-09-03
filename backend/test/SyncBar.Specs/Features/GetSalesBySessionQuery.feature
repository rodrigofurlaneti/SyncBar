Feature: Consultar vendas de uma sessao de caixa
    Regras de negocio do GetSalesBySessionQueryHandler: lista as vendas de uma sessao de caixa,
    incluindo um resumo textual dos pagamentos ativos de cada venda.

Scenario: Consultar sessao sem vendas retorna lista vazia
    Given a sessao de caixa 1 nao tem nenhuma venda
    When eu consulto as vendas da sessao de caixa 1
    Then a operacao deve ter sucesso
    And a lista de vendas retornada deve estar vazia

Scenario: Consultar sessao com venda retorna o resumo do pagamento
    Given a venda 1 da sessao de caixa 1 no valor de 50.00 com um pagamento no metodo 2
    When eu consulto as vendas da sessao de caixa 1
    Then a operacao deve ter sucesso
    And a lista de vendas retornada deve conter 1 venda
    And o resumo de pagamento da venda deve ser "2:50.00"
