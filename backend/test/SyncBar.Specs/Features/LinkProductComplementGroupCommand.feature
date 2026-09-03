Feature: Vincular grupo de complemento a um produto
    Regras de negocio do LinkProductComplementGroupCommandHandler: o produto e o grupo de
    complemento precisam existir e estar ativos, o grupo precisa pertencer a mesma empresa do
    produto, o mesmo grupo nao pode ser vinculado duas vezes ao mesmo produto e a ordem de
    exibicao nao pode ser negativa.

Scenario: Vincular grupo a um produto inexistente deve falhar
    Given nao existe nenhum produto com o id 5
    When eu tento vincular o grupo de complemento 1 ao produto 5 com ordem de exibicao 0
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Vincular grupo de complemento inexistente a um produto deve falhar
    Given um produto ativo com id 5 da empresa 100
    And nao existe nenhum grupo de complemento com o id 1
    When eu tento vincular o grupo de complemento 1 ao produto 5 com ordem de exibicao 0
    Then a operacao deve falhar com o erro "ComplementGroup.NotFound"

Scenario: Vincular grupo de complemento de outra empresa a um produto deve falhar
    Given um produto ativo com id 5 da empresa 100
    And um grupo de complemento ativo com id 1 da empresa 200
    When eu tento vincular o grupo de complemento 1 ao produto 5 com ordem de exibicao 0
    Then a operacao deve falhar com o erro "ComplementGroup.NotFound"

Scenario: Vincular um grupo de complemento que ja esta vinculado ao produto deve falhar
    Given um produto ativo com id 5 da empresa 100
    And um grupo de complemento ativo com id 1 da empresa 100
    And o produto 5 ja tem o grupo de complemento 1 vinculado
    When eu tento vincular o grupo de complemento 1 ao produto 5 com ordem de exibicao 0
    Then a operacao deve falhar com o erro "ProductComplementGroup.AlreadyLinked"

Scenario: Vincular grupo com ordem de exibicao negativa deve falhar
    Given um produto ativo com id 5 da empresa 100
    And um grupo de complemento ativo com id 1 da empresa 100
    When eu tento vincular o grupo de complemento 1 ao produto 5 com ordem de exibicao -1
    Then a operacao deve falhar com o erro "ProductComplementGroup.InvalidDisplayOrder"

Scenario: Vincular grupo a um produto com sucesso
    Given um produto ativo com id 5 da empresa 100
    And um grupo de complemento ativo com id 1 da empresa 100
    When eu tento vincular o grupo de complemento 1 ao produto 5 com ordem de exibicao 0
    Then a operacao deve ter sucesso
