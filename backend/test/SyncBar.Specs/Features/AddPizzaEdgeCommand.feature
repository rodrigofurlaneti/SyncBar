Feature: Adicionar recheio de borda a uma configuracao de pizza
    Regras de negocio do AddPizzaEdgeCommandHandler: exige uma configuracao de pizza ativa e um
    produto valido antes de cadastrar o recheio de borda (PizzaEdge — ex.: "Catupiry", "Cheddar"),
    e valida nome e preco extra.

Scenario: Adicionar recheio de borda a uma configuracao de pizza inexistente deve falhar
    Given nao existe nenhuma configuracao de pizza com o id 1
    When eu tento adicionar o recheio de borda "Catupiry" com preco extra 6 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar recheio de borda a uma configuracao de pizza inativa deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    And a configuracao de pizza 1 esta inativa
    When eu tento adicionar o recheio de borda "Catupiry" com preco extra 6 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar recheio de borda quando o produto da configuracao nao existe mais deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    And o produto 100 da configuracao de pizza nao existe mais
    When eu tento adicionar o recheio de borda "Catupiry" com preco extra 6 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar recheio de borda com nome vazio deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar o recheio de borda "" com preco extra 6 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaEdge.EmptyName"

Scenario: Adicionar recheio de borda com preco extra negativo deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar o recheio de borda "Catupiry" com preco extra -1 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaEdge.InvalidExtraPrice"

Scenario: Adicionar recheio de borda valido a uma configuracao de pizza ativa deve ter sucesso
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar o recheio de borda "Catupiry" com preco extra 6 na configuracao de pizza 1
    Then a operacao deve ter sucesso
