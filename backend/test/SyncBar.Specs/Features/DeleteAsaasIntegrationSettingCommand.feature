Feature: Remover configuracao Asaas
    Regras de negocio do DeleteAsaasIntegrationSettingCommandHandler: a configuracao precisa
    existir e pertencer a empresa informada; caso contrario e removida do repositorio.

Scenario: Remover configuracao inexistente deve falhar
    Given a configuracao Asaas com id 1 nao esta cadastrada
    When eu tento remover a configuracao 1 da empresa 1
    Then a operacao deve falhar com o erro "AsaasSetting.NotFound"

Scenario: Remover configuracao de outra empresa deve falhar
    Given uma configuracao Asaas com id 1 da empresa 1 esta cadastrada
    When eu tento remover a configuracao 1 da empresa 2
    Then a operacao deve falhar com o erro "AsaasSetting.NotFound"

Scenario: Remover configuracao valida deve ter sucesso e remove-la do repositorio
    Given uma configuracao Asaas com id 1 da empresa 1 esta cadastrada
    When eu tento remover a configuracao 1 da empresa 1
    Then a operacao deve ter sucesso
    And a configuracao deve ser removida do repositorio
