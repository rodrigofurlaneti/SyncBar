Feature: Marcar pedido Ifood como pronto para retirada
    Regras de negocio do MarkIfoodOrderReadyCommandHandler: valida existencia do pedido, da filial
    e do token de integracao antes de notificar o Ifood, e so atualiza o status local se o Ifood
    confirmar a acao.

Scenario: Marcar pedido inexistente como pronto deve falhar
    Given nao existe nenhum pedido Ifood com o id 1
    When eu tento marcar o pedido Ifood 1 como pronto
    Then a operacao deve falhar com o erro "IfoodOrder.NotFound"

Scenario: Marcar pedido como pronto sem token valido do Ifood deve falhar
    Given um pedido Ifood aberto com id 1 na filial 10
    And a filial 10 nao tem um token valido do Ifood
    When eu tento marcar o pedido Ifood 1 como pronto
    Then a operacao deve falhar com o erro "Ifood.NotConnected"

Scenario: Ifood recusar a chamada de pronto para retirada deve falhar
    Given um pedido Ifood aberto com id 1 na filial 10
    And a filial 10 esta conectada ao Ifood com um token valido
    And o Ifood recusa a chamada de pronto para retirada
    When eu tento marcar o pedido Ifood 1 como pronto
    Then a operacao deve falhar com o erro "Ifood.ActionFailed"

Scenario: Marcar pedido como pronto com sucesso
    Given um pedido Ifood aberto com id 1 na filial 10
    And a filial 10 esta conectada ao Ifood com um token valido
    And o Ifood aceita a chamada de pronto para retirada
    When eu tento marcar o pedido Ifood 1 como pronto
    Then a operacao deve ter sucesso
