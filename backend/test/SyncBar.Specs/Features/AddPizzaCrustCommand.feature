Feature: Adicionar borda a uma configuracao de pizza
    Regras de negocio do AddPizzaCrustCommandHandler: exige uma configuracao de pizza ativa e um
    produto valido antes de cadastrar a borda (PizzaCrust — ex.: "Borda Fina", "Borda Grossa"), e
    valida nome e preco extra da borda.

Scenario: Adicionar borda a uma configuracao de pizza inexistente deve falhar
    Given nao existe nenhuma configuracao de pizza com o id 1
    When eu tento adicionar a borda "Borda Fina" com preco extra 5 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar borda a uma configuracao de pizza inativa deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    And a configuracao de pizza 1 esta inativa
    When eu tento adicionar a borda "Borda Fina" com preco extra 5 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar borda quando o produto da configuracao nao existe mais deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    And o produto 100 da configuracao de pizza nao existe mais
    When eu tento adicionar a borda "Borda Fina" com preco extra 5 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar borda com nome vazio deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar a borda "" com preco extra 5 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaCrust.EmptyName"

Scenario: Adicionar borda com preco extra negativo deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar a borda "Borda Fina" com preco extra -5 na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaCrust.InvalidExtraPrice"

Scenario: Adicionar borda valida a uma configuracao de pizza ativa deve ter sucesso
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar a borda "Borda Fina" com preco extra 5 na configuracao de pizza 1
    Then a operacao deve ter sucesso
