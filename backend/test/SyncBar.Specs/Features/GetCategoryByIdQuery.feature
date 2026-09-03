Feature: Consultar categoria por id
    Regras de negocio do GetCategoryByIdQueryHandler: falha se a categoria nao existe ou esta
    inativa (uma categoria desativada nao pode ser consultada por esta via); caso contrario
    retorna os dados da categoria.

Scenario: Consultar categoria inexistente deve falhar
    Given a categoria com id 1 nao esta cadastrada
    When eu busco a categoria pelo id 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Consultar categoria inativa deve falhar
    Given uma categoria Bebidas com id 1 esta cadastrada mas inativa
    When eu busco a categoria pelo id 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Consultar categoria ativa deve ter sucesso
    Given uma categoria Bebidas com id 1, ordem 2, esta cadastrada e ativa
    When eu busco a categoria pelo id 1
    Then a operacao deve ter sucesso
    And o nome da categoria retornada deve ser "Bebidas"
    And a ordem de exibicao da categoria retornada deve ser 2
