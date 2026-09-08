Feature: Listar grupos de complemento de uma empresa
    Regras de negocio do GetComplementGroupsQueryHandler: retorna os grupos da empresa
    ordenados por nome, cada um com seus complementos ativos, resolvendo o nome do item de
    complemento e, quando o item aponta para um produto vinculado (Fase 18 - combos), a
    imagem desse produto.

Scenario: Empresa sem grupos de complemento retorna lista vazia
    Given a empresa 100 nao possui grupos de complemento
    When eu busco os grupos de complemento da empresa 100
    Then a operacao deve ter sucesso
    And a lista de grupos de complemento retornada deve ter 0 grupos
