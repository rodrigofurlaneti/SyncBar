Feature: Desativar categoria
    Regras de negocio do DeactivateCategoryCommandHandler: falha se a categoria nao existe ou ja
    esta inativa; falha tambem se houver produto ativo vinculado a ela (precisa desativar os
    produtos primeiro); caso contrario desativa a categoria (soft delete) e dispara a
    sincronizacao do cardapio com o Ifood.

Scenario: Desativar categoria inexistente deve falhar
    Given nao ha nenhuma categoria cadastrada com o id 1
    When eu tento desativar a categoria 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Desativar categoria ja inativa deve falhar
    Given uma categoria Bebidas com id 1 ja esta inativa
    When eu tento desativar a categoria 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Desativar categoria com produto ativo vinculado deve falhar
    Given existe uma categoria ativa Bebidas com id 1
    And a categoria 1 tem produto ativo vinculado
    When eu tento desativar a categoria 1
    Then a operacao deve falhar com o erro "Category.HasLinkedProducts"
    And a categoria deve continuar ativa

Scenario: Desativar categoria ativa sem produtos vinculados deve ter sucesso
    Given existe uma categoria ativa Bebidas com id 1
    And a categoria 1 nao tem produto ativo vinculado
    When eu tento desativar a categoria 1
    Then a operacao deve ter sucesso
    And a categoria deve estar inativa
