Feature: Cartao de credito tokenizado do Asaas
    Regras de negocio da entidade AsaasIntegrationSavedCard: o token do cartao e obrigatorio na
    criacao; um cartao criado sem marcacao explicita nao e o padrao; marcar como padrao e
    remover a marcacao de padrao alteram apenas a flag IsDefault.

Scenario: Criar cartao com token vazio deve falhar
    When eu tento salvar um cartao do cliente 1 da empresa 1 com o token vazio
    Then a operacao da entidade deve falhar com o erro "CreditCardToken.Empty"

Scenario: Criar cartao com dados validos deve ter sucesso e nao ser padrao por padrao
    When eu tento salvar um cartao do cliente 1 da empresa 1 com o token "token-1"
    Then a operacao da entidade deve ter sucesso
    And o cartao nao deve ser o padrao

Scenario: Marcar o cartao como padrao deve ativar a flag
    Given um cartao do cliente 1 da empresa 1 com o token "token-1" esta salvo
    When eu marco o cartao como padrao
    Then o cartao deve ser o padrao

Scenario: Remover a marcacao de padrao deve desativar a flag
    Given um cartao padrao do cliente 1 da empresa 1 com o token "token-1" esta salvo
    When eu removo a marcacao de padrao do cartao
    Then o cartao nao deve ser o padrao
