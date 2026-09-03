Feature: Consultar comandas de uma filial
    Regras de negocio do GetComandasByBranchQueryHandler: lista as comandas de uma filial ordenadas
    primeiro pelo tamanho do codigo e depois pelo codigo em si (ex.: "5" antes de "10").

Scenario: Consultar filial sem comandas retorna lista vazia
    Given a filial 1 nao tem nenhuma comanda cadastrada
    When eu consulto as comandas da filial 1
    Then a operacao deve ter sucesso
    And a lista de comandas retornada deve estar vazia

Scenario: Consultar filial com comandas retorna a lista ordenada por tamanho do codigo
    Given a filial 1 tem a comanda "10" com status 1
    And a filial 1 tem a comanda "5" com status 1
    When eu consulto as comandas da filial 1
    Then a operacao deve ter sucesso
    And a lista de comandas retornada deve conter 2 comandas
    And a primeira comanda da lista deve ter o codigo "5"
