Feature: Desativar produto
    Regras de negocio do DeactivateProductCommandHandler: falha se o produto nao existe ou ja
    esta inativo; caso contrario desativa o produto e dispara a sincronizacao do cardapio com o
    Ifood.

Scenario: Desativar produto inexistente deve falhar
    Given nao ha nenhum produto cadastrado com o id 1
    When eu tento desativar o produto 1
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Desativar produto ja inativo deve falhar
    Given um produto Refrigerante com id 1 ja esta inativo
    When eu tento desativar o produto 1
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Desativar produto ativo deve ter sucesso
    Given existe um produto ativo Refrigerante com id 1
    When eu tento desativar o produto 1
    Then a operacao deve ter sucesso
    And o produto deve estar inativo
