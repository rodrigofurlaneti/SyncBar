Feature: Criar item de complemento
    Regras de negocio do CreateComplementItemCommandHandler: o CompanyId informado precisa bater
    com a empresa do tenant autenticado; quando um produto vinculado (LinkedProductId) e
    informado, ele precisa existir, estar ativo e pertencer a mesma empresa; nome e obrigatorio.

Scenario: Criar item de complemento sem tenant autenticado deve falhar
    Given o tenant autenticado nao possui empresa
    When eu tento criar um item de complemento para a empresa 100 com nome "Bacon extra"
    Then a operacao deve falhar com o erro "Tenant.Forbidden"

Scenario: Criar item de complemento com tenant de outra empresa deve falhar
    Given o tenant autenticado pertence a empresa 200
    When eu tento criar um item de complemento para a empresa 100 com nome "Bacon extra"
    Then a operacao deve falhar com o erro "Tenant.Forbidden"

Scenario: Criar item de complemento vinculado a um produto inexistente deve falhar
    Given o tenant autenticado pertence a empresa 100
    And nao existe nenhum produto com o id 50
    When eu tento criar um item de complemento para a empresa 100 com nome "X-Salada" vinculado ao produto 50
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Criar item de complemento vinculado a um produto de outra empresa deve falhar
    Given o tenant autenticado pertence a empresa 100
    And um produto ativo com id 50 da empresa 200
    When eu tento criar um item de complemento para a empresa 100 com nome "X-Salada" vinculado ao produto 50
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Criar item de complemento com nome vazio deve falhar
    Given o tenant autenticado pertence a empresa 100
    When eu tento criar um item de complemento para a empresa 100 com nome ""
    Then a operacao deve falhar com o erro "ComplementItem.EmptyName"

Scenario: Criar item de complemento sem produto vinculado com sucesso
    Given o tenant autenticado pertence a empresa 100
    When eu tento criar um item de complemento para a empresa 100 com nome "Bacon extra"
    Then a operacao deve ter sucesso

Scenario: Criar item de complemento vinculado a um produto valido com sucesso
    Given o tenant autenticado pertence a empresa 100
    And um produto ativo com id 50 da empresa 100
    When eu tento criar um item de complemento para a empresa 100 com nome "X-Salada" vinculado ao produto 50
    Then a operacao deve ter sucesso
