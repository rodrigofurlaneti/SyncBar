Feature: Log de webhook do Asaas
    Regras de negocio da entidade AsaasIntegrationWebhookLog: o evento e o payload sao
    obrigatorios na criacao; todo log nasce com status Pendente; marcar como processado um log
    ja processado deve falhar (idempotencia); marcar como falha exige uma mensagem de erro.

Scenario: Criar log com evento vazio deve falhar
    When eu tento registrar o webhook da empresa 1 com evento vazio e payload "{}"
    Then a operacao da entidade deve falhar com o erro "Event.Empty"

Scenario: Criar log com payload vazio deve falhar
    When eu tento registrar o webhook da empresa 1 com evento "PAYMENT_RECEIVED" e payload vazio
    Then a operacao da entidade deve falhar com o erro "Payload.Empty"

Scenario: Criar log com dados validos deve nascer pendente
    When eu tento registrar o webhook da empresa 1 com evento "PAYMENT_RECEIVED" e payload "{}"
    Then a operacao da entidade deve ter sucesso
    And o status do log deve ser "Pending"

Scenario: Marcar um log pendente como processado deve ter sucesso
    Given um log de webhook da empresa 1 com evento "PAYMENT_RECEIVED" esta registrado
    When eu marco o log como processado
    Then a operacao da entidade deve ter sucesso
    And o status do log deve ser "Processed"

Scenario: Marcar um log ja processado como processado novamente deve falhar
    Given um log de webhook da empresa 1 com evento "PAYMENT_RECEIVED" esta registrado
    And o log ja foi marcado como processado
    When eu marco o log como processado
    Then a operacao da entidade deve falhar com o erro "WebhookLog.AlreadyProcessed"

Scenario: Marcar um log como falha sem mensagem de erro deve falhar
    Given um log de webhook da empresa 1 com evento "PAYMENT_RECEIVED" esta registrado
    When eu tento marcar o log como falha com mensagem vazia
    Then a operacao da entidade deve falhar com o erro "ErrorMessage.Empty"

Scenario: Marcar um log como falha com mensagem valida deve ter sucesso
    Given um log de webhook da empresa 1 com evento "PAYMENT_RECEIVED" esta registrado
    When eu tento marcar o log como falha com mensagem "pedido nao encontrado"
    Then a operacao da entidade deve ter sucesso
    And o status do log deve ser "Failed"
