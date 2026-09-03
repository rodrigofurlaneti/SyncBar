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

Scenario: Empresa com grupos de complemento retorna a lista ordenada por nome
    Given um grupo de complemento ativo com id 1 nome "Bebidas" da empresa 100
    And um grupo de complemento ativo com id 2 nome "Adicionais" da empresa 100
    And o grupo 2 tem o complemento apontando para o item de complemento 10 chamado "Bacon extra" com preco extra 5
    And o item de complemento 10 esta vinculado ao produto 90 com imagem "https://cdn/bacon.png"
    When eu busco os grupos de complemento da empresa 100
    Then a operacao deve ter sucesso
    And a lista de grupos de complemento retornada deve ter 2 grupos
    And o primeiro grupo da lista deve se chamar "Adicionais"
    And o complemento do item "Bacon extra" deve ter preco extra 5 e imagem do produto vinculado "https://cdn/bacon.png"
