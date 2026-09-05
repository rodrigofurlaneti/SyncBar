Feature: Criar configuracao Asaas
    Regras de negocio do CreateAsaasIntegrationSettingCommandHandler: a chave de API e
    obrigatoria (validada no dominio via AsaasIntegrationSetting.Create); nao pode existir outra
    configuracao para o mesmo escopo (a mesma filial, ou a configuracao padrao da empresa quando
    nenhuma filial e informada); caso contrario a configuracao e criada e adicionada ao
    repositorio.

Scenario: Criar configuracao com chave de API vazia deve falhar
    When eu tento criar a configuracao Asaas para a empresa 1 sem filial com a chave de API vazia
    Then a operacao deve falhar com o erro "ApiKey.Empty"

Scenario: Criar configuracao quando ja existe uma para o mesmo escopo deve falhar
    Given ja existe uma configuracao Asaas para a empresa 1 sem filial
    When eu tento criar a configuracao Asaas para a empresa 1 sem filial com a chave de API "chave-nova"
    Then a operacao deve falhar com o erro "AsaasSetting.AlreadyExists"

Scenario: Criar configuracao com dados validos deve ter sucesso
    When eu tento criar a configuracao Asaas para a empresa 1 sem filial com a chave de API "chave-valida"
    Then a operacao deve ter sucesso

Scenario: Criar configuracao com dados validos deve adiciona-la ao repositorio
    When eu tento criar a configuracao Asaas para a empresa 1 sem filial com a chave de API "chave-valida"
    Then a configuracao criada deve ser adicionada ao repositorio da empresa 1
