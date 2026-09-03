Feature: Listar features vinculadas a um usuario
    Regra de negocio do GetUserFeaturesQueryHandler: retorna os ids das features vinculadas
    diretamente ao usuario, na ordem devolvida pelo repositorio, sem filtrar por vinculos ativos
    ou inativos.

Scenario: Usuario sem features vinculadas deve retornar lista vazia
    Given o usuario 1 nao tem features vinculadas diretamente
    When eu busco as features vinculadas ao usuario 1
    Then a operacao deve ter sucesso
    And a lista de ids de features deve estar vazia

Scenario: Usuario com features vinculadas deve retornar os ids inclusive de vinculos desativados
    Given o usuario 7 tem a feature 10 vinculada e ativa
    And o usuario 7 tem a feature 20 vinculada e ativa
    And o usuario 7 tem a feature 30 vinculada mas desativada
    When eu busco as features vinculadas ao usuario 7
    Then a operacao deve ter sucesso
    And a lista de ids de features deve ser 10, 20, 30
