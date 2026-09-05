Feature: Vinculo de cliente com o Asaas
    Regras de negocio da entidade AsaasIntegrationCustomer: o CustomerId, a empresa e o
    AsaasCustomerId sao obrigatorios na criacao; atualizar o AsaasCustomerId para um valor vazio
    nao tem efeito (mantem o vinculo anterior).

Scenario: Criar vinculo com AsaasCustomerId vazio deve falhar
    When eu tento vincular o cliente 1 da empresa 1 ao Asaas com o id vazio
    Then a operacao da entidade deve falhar com o erro "AsaasCustomerId.Empty"

Scenario: Criar vinculo com cliente invalido deve falhar
    When eu tento vincular o cliente 0 da empresa 1 ao Asaas com o id "cus_123"
    Then a operacao da entidade deve falhar com o erro "CustomerId.Invalid"

Scenario: Criar vinculo com dados validos deve ter sucesso
    When eu tento vincular o cliente 1 da empresa 1 ao Asaas com o id "cus_123"
    Then a operacao da entidade deve ter sucesso
    And o AsaasCustomerId do vinculo deve ser "cus_123"

Scenario: Atualizar o AsaasCustomerId para um valor vazio nao deve alterar o vinculo
    Given um vinculo do cliente 1 da empresa 1 com o AsaasCustomerId "cus_original" esta criado
    When eu tento atualizar o AsaasCustomerId do vinculo para vazio
    Then o AsaasCustomerId do vinculo deve continuar "cus_original"

Scenario: Atualizar o AsaasCustomerId para um novo valor valido deve substitui-lo
    Given um vinculo do cliente 1 da empresa 1 com o AsaasCustomerId "cus_original" esta criado
    When eu tento atualizar o AsaasCustomerId do vinculo para "cus_novo"
    Then o AsaasCustomerId do vinculo deve continuar "cus_novo"
