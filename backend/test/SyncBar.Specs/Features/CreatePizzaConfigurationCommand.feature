Feature: Criar configuracao de pizza para um produto
    Regras de negocio do CreatePizzaConfigurationCommandHandler: exige um produto ativo e
    existente, e e um get-or-create — se o produto ja tem uma configuracao de pizza, retorna o id
    dela em vez de criar uma duplicada.

Scenario: Criar configuracao de pizza para produto inexistente deve falhar
    Given nao existe nenhum produto com o id 1
    When eu tento criar uma configuracao de pizza para o produto 1
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Criar configuracao de pizza para produto inativo deve falhar
    Given o produto 1 esta inativo
    When eu tento criar uma configuracao de pizza para o produto 1
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Criar configuracao de pizza para produto que ja tem uma configuracao deve retornar a existente sem duplicar
    Given um produto ativo com id 1
    And o produto 1 ja tem uma configuracao de pizza com id 50
    When eu tento criar uma configuracao de pizza para o produto 1
    Then a operacao deve ter sucesso
    And nenhuma nova configuracao de pizza deve ser criada

Scenario: Criar configuracao de pizza para produto sem configuracao deve criar uma nova
    Given um produto ativo com id 1
    When eu tento criar uma configuracao de pizza para o produto 1
    Then a operacao deve ter sucesso
    And uma nova configuracao de pizza deve ser criada
