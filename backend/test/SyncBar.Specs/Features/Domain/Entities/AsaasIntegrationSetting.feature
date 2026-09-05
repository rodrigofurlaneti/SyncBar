Feature: Configuracao de integracao Asaas
    Regras de negocio da entidade AsaasIntegrationSetting: a chave de API e obrigatoria; a
    empresa e obrigatoria e, quando informada, a filial precisa ser um id valido; sem ambiente
    informado o padrao e Sandbox; atualizar a chave de API para um valor vazio deve falhar sem
    alterar o valor anterior.

Scenario: Criar configuracao com chave de API vazia deve falhar
    When eu tento criar a configuracao Asaas para a empresa 1 sem filial com a chave de API vazia
    Then a operacao da entidade deve falhar com o erro "ApiKey.Empty"

Scenario: Criar configuracao com empresa invalida deve falhar
    When eu tento criar a configuracao Asaas para a empresa 0 sem filial com a chave de API "chave-1"
    Then a operacao da entidade deve falhar com o erro "CompanyId.Invalid"

Scenario: Criar configuracao sem informar o ambiente deve usar Sandbox como padrao
    When eu tento criar a configuracao Asaas para a empresa 1 sem filial com a chave de API "chave-1"
    Then a operacao da entidade deve ter sucesso
    And o ambiente da configuracao deve ser "Sandbox"

Scenario: Atualizar a chave de API para um valor vazio deve falhar e manter o valor anterior
    Given uma configuracao Asaas da empresa 1 com a chave de API "chave-original" esta criada
    When eu tento atualizar a chave de API da configuracao para vazia
    Then a operacao da entidade deve falhar com o erro "ApiKey.Empty"
    And a chave de API da configuracao deve continuar "chave-original"

Scenario: Desativar a configuracao deve marcar como inativa
    Given uma configuracao Asaas da empresa 1 com a chave de API "chave-original" esta criada
    When eu desativo a configuracao
    Then a configuracao deve estar inativa
