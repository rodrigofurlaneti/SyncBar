Feature: Aceitar disputa do Ifood
    Regras de negocio do AcceptIfoodDisputeCommandHandler: valida existencia da filial e exige
    token valido de integracao com o Ifood antes de aceitar a disputa; se o Ifood recusar a
    aceitacao, propaga a mensagem de erro recebida (ou uma mensagem padrao); em caso de sucesso
    retorna o status devolvido pelo Ifood.

Scenario: Aceitar disputa de filial inexistente deve falhar
    Given nao existe nenhuma filial cadastrada com o id 10
    When eu tento aceitar a disputa "dispute-1" da filial 10
    Then a operacao deve falhar com o erro "Branch.NotFound"

Scenario: Aceitar disputa sem token valido do Ifood deve falhar
    Given a filial 10 esta cadastrada para a empresa 1
    And a filial 10 nao tem um token valido do Ifood
    When eu tento aceitar a disputa "dispute-1" da filial 10
    Then a operacao deve falhar com o erro "Ifood.NotConnected"

Scenario: Ifood recusar a aceitacao da disputa com mensagem deve falhar propagando a mensagem
    Given a filial 10 esta cadastrada para a empresa 1
    And a filial 10 esta conectada ao Ifood com um token valido
    And o Ifood recusa a aceitacao da disputa "dispute-1" com a mensagem "Disputa ja foi resolvida."
    When eu tento aceitar a disputa "dispute-1" da filial 10
    Then a operacao deve falhar com o erro "Ifood.ActionFailed"
    And a mensagem de erro deve ser "Disputa ja foi resolvida."

Scenario: Ifood recusar a aceitacao da disputa sem mensagem deve falhar com mensagem padrao
    Given a filial 10 esta cadastrada para a empresa 1
    And a filial 10 esta conectada ao Ifood com um token valido
    And o Ifood recusa a aceitacao da disputa "dispute-1" sem mensagem
    When eu tento aceitar a disputa "dispute-1" da filial 10
    Then a mensagem de erro deve ser "Falha ao aceitar a disputa no Ifood."

Scenario: Aceitar disputa com sucesso retorna o status do Ifood
    Given a filial 10 esta cadastrada para a empresa 1
    And a filial 10 esta conectada ao Ifood com um token valido
    And o Ifood aceita a disputa "dispute-1" com status "ACCEPTED"
    When eu tento aceitar a disputa "dispute-1" da filial 10
    Then a operacao deve ter sucesso
    And o status da disputa retornado deve ser "ACCEPTED"
