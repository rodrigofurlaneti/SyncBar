Feature: Desativar categoria
    Regras de negocio do DeactivateCategoryCommandHandler: falha se a categoria nao existe ou ja
    esta inativa; caso contrario desativa a categoria (soft delete, sem cascata para os produtos
    ja cadastrados nela) e dispara a sincronizacao do cardapio com o Ifood.

Scenario: Desativar categoria inexistente deve falhar
    Given nao ha nenhuma categoria cadastrada com o id 1
    When eu tento desativar a categoria 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Desativar categoria ja inativa deve falhar
    Given uma categoria Bebidas com id 1 ja esta inativa
    When eu tento desativar a categoria 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Desativar categoria ativa deve ter sucesso
    Given existe uma categoria ativa Bebidas com id 1
    When eu tento desativar a categoria 1
    Then a operacao deve ter sucesso
    And a categoria deve estar inativa
