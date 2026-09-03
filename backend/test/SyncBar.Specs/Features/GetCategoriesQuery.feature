Feature: Consultar categorias ativas
    Regras de negocio do GetCategoriesQueryHandler: retorna as categorias ativas da empresa
    (o filtro de inativas fica a cargo do repositorio) ordenadas por ordem de exibicao e, em
    caso de empate, por nome.

Scenario: Consultar empresa sem categorias retorna lista vazia
    Given a empresa 1 nao possui nenhuma categoria ativa
    When eu busco as categorias ativas da empresa 1
    Then a operacao deve ter sucesso
    And a lista de categorias retornada deve estar vazia

Scenario: Consultar categorias retorna a lista ordenada por ordem de exibicao
    Given a categoria ativa "Sobremesas" com id 2 e ordem 1 pertence a empresa 1
    And a categoria ativa "Bebidas" com id 1 e ordem 0 pertence a empresa 1
    When eu busco as categorias ativas da empresa 1
    Then a operacao deve ter sucesso
    And a lista de categorias retornada deve conter 2 categorias
    And a categoria na posicao 0 da lista deve ser "Bebidas"
    And a categoria na posicao 1 da lista deve ser "Sobremesas"
