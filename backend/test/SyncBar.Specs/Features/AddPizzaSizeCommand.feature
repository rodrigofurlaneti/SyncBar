Feature: Adicionar tamanho a uma configuracao de pizza
    Regras de negocio do AddPizzaSizeCommandHandler: exige uma configuracao de pizza ativa e um
    produto valido, nao permite dois tamanhos ativos com o mesmo nome (case-insensitive), exige
    nome nao vazio e AcceptedFractions entre 1 e 4.

Scenario: Adicionar tamanho a uma configuracao de pizza inexistente deve falhar
    Given nao existe nenhuma configuracao de pizza com o id 1
    When eu tento adicionar o tamanho "Grande" com 8 fatias e 4 fracoes aceitas na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar tamanho a uma configuracao de pizza inativa deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    And a configuracao de pizza 1 esta inativa
    When eu tento adicionar o tamanho "Grande" com 8 fatias e 4 fracoes aceitas na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar tamanho quando o produto da configuracao nao existe mais deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    And o produto 100 da configuracao de pizza nao existe mais
    When eu tento adicionar o tamanho "Grande" com 8 fatias e 4 fracoes aceitas na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.NotFound"

Scenario: Adicionar tamanho com nome duplicado (case-insensitive) deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    And a configuracao de pizza 1 ja tem um tamanho ativo chamado "Grande"
    When eu tento adicionar o tamanho "grande" com 8 fatias e 4 fracoes aceitas na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaConfiguration.DuplicateSizeName"

Scenario: Adicionar tamanho com nome vazio deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar o tamanho "" com 8 fatias e 4 fracoes aceitas na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaSize.EmptyName"

Scenario: Adicionar tamanho com fracoes aceitas abaixo do minimo deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar o tamanho "Grande" com 8 fatias e 0 fracoes aceitas na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaSize.InvalidAcceptedFractions"

Scenario: Adicionar tamanho com fracoes aceitas acima do maximo deve falhar
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar o tamanho "Grande" com 8 fatias e 5 fracoes aceitas na configuracao de pizza 1
    Then a operacao deve falhar com o erro "PizzaSize.InvalidAcceptedFractions"

Scenario: Adicionar tamanho valido a uma configuracao de pizza ativa deve ter sucesso
    Given uma configuracao de pizza ativa com id 1 para o produto 100
    When eu tento adicionar o tamanho "Grande" com 8 fatias e 4 fracoes aceitas na configuracao de pizza 1
    Then a operacao deve ter sucesso
