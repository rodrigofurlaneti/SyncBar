Feature: Listar features vinculadas a um cargo
    Regra de negocio do GetJobTitleFeaturesQueryHandler: retorna os ids das features vinculadas ao
    cargo, na ordem devolvida pelo repositorio, sem filtrar por vinculos ativos ou inativos.

Scenario: Cargo sem features vinculadas deve retornar lista vazia
    Given o cargo 1 nao tem features vinculadas
    When eu busco as features vinculadas ao cargo 1
    Then a operacao deve ter sucesso
    And a lista de ids de features deve estar vazia

Scenario: Cargo com features vinculadas deve retornar os ids inclusive de vinculos desativados
    Given o cargo 7 tem a feature 10 vinculada e ativa
    And o cargo 7 tem a feature 20 vinculada e ativa
    And o cargo 7 tem a feature 30 vinculada mas desativada
    When eu busco as features vinculadas ao cargo 7
    Then a operacao deve ter sucesso
    And a lista de ids de features deve ser 10, 20, 30
