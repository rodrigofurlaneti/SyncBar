Feature: Definir preco de um sabor de pizza num tamanho
    Regras de negocio do SetPizzaFlavorPriceCommandHandler: exige configuracao de pizza ativa,
    produto existente, sabor da MESMA empresa do produto (isolamento de tenant — o id do sabor
    sozinho nao garante isso, so a FK) e um tamanho cadastrado ativo na configuracao. E um upsert
    (ver PizzaConfiguration.SetFlavorPrice): a EXISTENCIA da linha de preco e o que torna o sabor
    vendavel naquele tamanho, e so essa operacao dispara a sincronizacao do catalogo com o Ifood
    (diferente de AddSize/AddCrust/AddEdge, que nao disparam).

Scenario: Definir preco em uma configuracao de pizza inexistente deve falhar
    Given nao existe nenhuma configuracao de pizza com o id 1
    When eu tento definir o preco 45 do sabor 5 para o tamanho 1 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Definir preco em uma configuracao de pizza inativa deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100, com um tamanho "Grande"
    And a configuracao de pizza 1 esta inativa
    When eu tento definir o preco 45 do sabor 5 para o tamanho cadastrado na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Definir preco quando o produto da configuracao nao existe mais deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100, com um tamanho "Grande"
    And o produto 100 da configuracao de pizza nao existe mais
    When eu tento definir o preco 45 do sabor 5 para o tamanho cadastrado na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Definir preco para um sabor inexistente deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100, com um tamanho "Grande"
    And nao existe nenhum sabor de pizza com o id 5
    When eu tento definir o preco 45 do sabor 5 para o tamanho cadastrado na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaFlavor.NotFound"

Scenario: Definir preco para um sabor de outra empresa deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100, com um tamanho "Grande"
    And um sabor de pizza 5 da empresa 2
    When eu tento definir o preco 45 do sabor 5 para o tamanho cadastrado na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaFlavor.NotFound"

Scenario: Definir preco para um tamanho que nao existe na configuracao deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100, com um tamanho "Grande"
    And um sabor de pizza 5 da empresa 1
    When eu tento definir o preco 45 do sabor 5 para o tamanho 999 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.SizeNotFound"

Scenario: Definir preco negativo deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100, com um tamanho "Grande"
    And um sabor de pizza 5 da empresa 1
    When eu tento definir o preco -10 do sabor 5 para o tamanho cadastrado na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaFlavorPrice.InvalidPrice"

Scenario: Definir preco pela primeira vez deve ter sucesso e disparar a sincronizacao do catalogo
    Given uma configuracao de pizza ativa com id 1 para o produto 100, com um tamanho "Grande"
    And um sabor de pizza 5 da empresa 1
    When eu tento definir o preco 45 do sabor 5 para o tamanho cadastrado na configuracao de pizza 1
    Then a operacao deve ter sucesso
    And o preco do sabor no tamanho cadastrado deve ser 45
    And a sincronizacao do catalogo da empresa deve ser disparada

Scenario: Definir preco novamente para o mesmo sabor e tamanho deve atualizar o preco existente (upsert)
    Given uma configuracao de pizza ativa com id 1 para o produto 100, com um tamanho "Grande"
    And um sabor de pizza 5 da empresa 1
    And a configuracao de pizza 1 ja tem um preco de 40 para o sabor 5 no tamanho cadastrado
    When eu tento definir o preco 55 do sabor 5 para o tamanho cadastrado na configuracao de pizza 1
    Then a operacao deve ter sucesso
    And o preco do sabor no tamanho cadastrado deve ser 55
