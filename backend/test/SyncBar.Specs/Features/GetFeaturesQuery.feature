Feature: Listar features do sistema
    Regra de negocio do GetFeaturesQueryHandler: retorna todas as features cadastradas, mapeando
    id, codigo e nome de cada uma.

Scenario: Nenhuma feature cadastrada deve retornar lista vazia
    Given nao existem features cadastradas
    When eu busco a lista de features
    Then a operacao deve ter sucesso
    And a lista de features deve estar vazia

Scenario: Buscar a lista de features deve retornar o codigo de cada uma
    Given existe a feature "orders.read" com o nome "Ver pedidos"
    And existe a feature "orders.write" com o nome "Editar pedidos"
    When eu busco a lista de features
    Then a operacao deve ter sucesso
    And a lista de features deve conter o codigo "orders.read"
    And a lista de features deve conter o codigo "orders.write"
