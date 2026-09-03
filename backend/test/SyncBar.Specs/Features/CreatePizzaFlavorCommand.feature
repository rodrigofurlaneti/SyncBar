Feature: Criar sabor de pizza
    Regras de negocio do CreatePizzaFlavorCommandHandler: o CompanyId do comando deve bater com o
    CompanyId do tenant autenticado (protecao contra escrita cruzada entre empresas, mesma correcao
    aplicada em CreateComplementItemCommandHandler) e o nome do sabor e obrigatorio.

Scenario: Criar sabor de pizza para uma empresa diferente da do tenant autenticado deve falhar
    Given o usuario autenticado pertence a empresa 1
    When eu tento criar o sabor de pizza "Calabresa" para a empresa 2
    Then a operacao deve falhar com o erro "Tenant.Forbidden"

Scenario: Criar sabor de pizza sem usuario autenticado deve falhar
    Given nao ha usuario autenticado
    When eu tento criar o sabor de pizza "Calabresa" para a empresa 1
    Then a operacao deve falhar com o erro "Tenant.Forbidden"

Scenario: Criar sabor de pizza com nome vazio deve falhar
    Given o usuario autenticado pertence a empresa 1
    When eu tento criar o sabor de pizza "" para a empresa 1
    Then a operacao deve falhar com o erro "PizzaFlavor.EmptyName"

Scenario: Criar sabor de pizza valido para a propria empresa deve ter sucesso
    Given o usuario autenticado pertence a empresa 1
    When eu tento criar o sabor de pizza "Calabresa" para a empresa 1
    Then a operacao deve ter sucesso
