Feature: Atualizar categoria
    Regras de negocio do UpdateCategoryCommandHandler: falha se a categoria nao existe ou esta
    inativa; o nome nao pode ficar vazio e a ordem de exibicao nao pode ficar negativa
    (validado no dominio via Category.UpdateDetails); caso contrario atualiza os dados e
    dispara a sincronizacao do cardapio com o Ifood.

Scenario: Atualizar categoria inexistente deve falhar
    Given nao existe nenhuma categoria para atualizar com o id 1
    When eu tento atualizar a categoria 1 para o nome "Bebidas" e ordem 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Atualizar categoria para nome vazio deve falhar
    Given uma categoria Bebidas com id 1 esta cadastrada e ativa para atualizacao
    When eu tento atualizar a categoria 1 para o nome "" e ordem 1
    Then a operacao deve falhar com o erro "Category.EmptyName"

Scenario: Atualizar categoria com ordem negativa deve falhar
    Given uma categoria Bebidas com id 1 esta cadastrada e ativa para atualizacao
    When eu tento atualizar a categoria 1 para o nome "Bebidas" e ordem -1
    Then a operacao deve falhar com o erro "Category.InvalidDisplayOrder"

Scenario: Atualizar categoria com dados validos deve ter sucesso
    Given uma categoria Bebidas com id 1 esta cadastrada e ativa para atualizacao
    When eu tento atualizar a categoria 1 para o nome "Bebidas e Sucos" e ordem 2
    Then a operacao deve ter sucesso
    And o nome da categoria deve ser "Bebidas e Sucos"
    And a ordem de exibicao da categoria deve ser 2
