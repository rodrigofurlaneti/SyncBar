Feature: Cobranca do Asaas
    Regras de negocio da entidade AsaasIntegrationPayment: a filial, o pedido, o id da cobranca
    no Asaas e o valor sao obrigatorios na criacao (valor precisa ser maior que zero); toda
    cobranca nasce com status PENDING; marcar como paga sempre grava o status RECEIVED.

Scenario: Criar cobranca com valor zero deve falhar
    When eu tento criar a cobranca da filial 1, pedido 10, id "pay_1" no valor de 0
    Then a operacao da entidade deve falhar com o erro "Value.Invalid"

Scenario: Criar cobranca com id vazio deve falhar
    When eu tento criar a cobranca da filial 1, pedido 10, id vazio, no valor de 100
    Then a operacao da entidade deve falhar com o erro "AsaasPaymentId.Empty"

Scenario: Criar cobranca com dados validos deve nascer com status pendente
    When eu tento criar a cobranca da filial 1, pedido 10, id "pay_1" no valor de 100
    Then a operacao da entidade deve ter sucesso
    And o status da cobranca deve ser "PENDING"

Scenario: Marcar a cobranca como paga deve definir o status como recebido
    Given uma cobranca da filial 1, pedido 10, id "pay_1", no valor de 100 esta criada
    When eu marco a cobranca como paga com valor liquido de 98
    Then o status da cobranca deve ser "RECEIVED"
    And o valor liquido da cobranca deve ser 98

Scenario: Atualizar o status da cobranca para um valor vazio nao deve alterar o status atual
    Given uma cobranca da filial 1, pedido 10, id "pay_1", no valor de 100 esta criada
    When eu tento atualizar o status da cobranca para vazio
    Then o status da cobranca deve ser "PENDING"
