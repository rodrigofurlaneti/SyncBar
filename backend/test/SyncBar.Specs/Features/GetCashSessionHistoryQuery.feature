Feature: Consultar historico de sessoes de caixa
    Regras de negocio do GetCashSessionHistoryQueryHandler: o mes de referencia deve estar entre 1
    e 12; retorna as sessoes de caixa da filial no periodo (mais recentes primeiro) com o total de
    vendas de cada sessao.

Scenario: Consultar historico com mes de referencia invalido deve falhar
    Given a filial 1 nao tem sessoes de caixa no periodo
    When eu consulto o historico de caixa da filial 1 para o mes 13 do ano 2026
    Then a operacao deve falhar com o erro "CashHistory.InvalidMonth"

Scenario: Consultar historico de filial sem sessoes no periodo retorna lista vazia
    Given a filial 1 nao tem sessoes de caixa no periodo
    When eu consulto o historico de caixa da filial 1 para o mes 9 do ano 2026
    Then a operacao deve ter sucesso
    And a lista de sessoes retornada deve estar vazia

Scenario: Consultar historico de filial com sessao fechada no periodo retorna o total de vendas
    Given a filial 1 tem uma sessao de caixa fechada no periodo com uma venda de 80.00
    When eu consulto o historico de caixa da filial 1 para o mes 9 do ano 2026
    Then a operacao deve ter sucesso
    And a lista de sessoes retornada deve conter 1 sessao
    And o total de vendas da sessao retornada deve ser 80.00
