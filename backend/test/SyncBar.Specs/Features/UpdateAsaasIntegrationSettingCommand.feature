Feature: Atualizar configuracao Asaas
    Regras de negocio do UpdateAsaasIntegrationSettingCommandHandler: a configuracao precisa
    existir e pertencer a empresa informada (isolamento multi-tenant); a nova chave de API, se
    informada, nao pode ser vazia; caso contrario os dados sao atualizados.

Scenario: Atualizar configuracao inexistente deve falhar
    Given a configuracao Asaas com id 1 nao esta cadastrada
    When eu tento atualizar a configuracao 1 da empresa 1 com a chave de API "nova-chave"
    Then a operacao deve falhar com o erro "AsaasSetting.NotFound"

Scenario: Atualizar configuracao de outra empresa deve falhar
    Given uma configuracao Asaas com id 1 da empresa 1 esta cadastrada
    When eu tento atualizar a configuracao 1 da empresa 2 com a chave de API "nova-chave"
    Then a operacao deve falhar com o erro "AsaasSetting.NotFound"

Scenario: Atualizar configuracao com chave de API vazia deve falhar
    Given uma configuracao Asaas com id 1 da empresa 1 esta cadastrada
    When eu tento atualizar a configuracao 1 da empresa 1 com a chave de API vazia
    Then a operacao deve falhar com o erro "ApiKey.Empty"

Scenario: Atualizar configuracao com dados validos deve ter sucesso
    Given uma configuracao Asaas com id 1 da empresa 1 esta cadastrada
    When eu tento atualizar a configuracao 1 da empresa 1 com a chave de API "nova-chave"
    Then a operacao deve ter sucesso
