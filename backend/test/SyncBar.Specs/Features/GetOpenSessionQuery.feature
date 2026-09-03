Feature: Consultar sessao de caixa aberta
    Regras de negocio do GetOpenSessionQueryHandler: retorna a sessao atualmente aberta de um caixa,
    ou falha quando o caixa nao tem nenhuma sessao aberta no momento.

Scenario: Consultar caixa sem sessao aberta deve falhar
    Given o caixa 1 nao tem sessao aberta
    When eu consulto a sessao aberta do caixa 1
    Then a operacao deve falhar com o erro "CashSession.NotFound"

Scenario: Consultar caixa com sessao aberta retorna os dados da sessao
    Given o caixa 1 tem uma sessao aberta com fundo de troco de 75.00
    When eu consulto a sessao aberta do caixa 1
    Then a operacao deve ter sucesso
    And o fundo de troco da sessao retornada deve ser 75.00
