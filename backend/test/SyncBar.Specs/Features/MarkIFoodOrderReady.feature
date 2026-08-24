Feature: Marcar pedido do iFood como pronto para retirada
    Fluxo "readyToPickup" do modulo Order do iFood — obrigatorio para retirada/DINE_IN e
    tambem valido para delivery com entregador do proprio iFood.

Scenario: Pedido iFood inexistente nao pode ser marcado como pronto
    Given nao existe nenhum pedido iFood com o id 999
    When eu tento marcar o pedido iFood 999 como pronto
    Then a operacao deve falhar com o erro "IFoodOrder.NotFound"

Scenario: Filial sem token valido nao consegue falar com o iFood
    Given um pedido iFood aberto com id 1 na filial 10
    And a filial 10 nao tem um token valido do iFood
    When eu tento marcar o pedido iFood 1 como pronto
    Then a operacao deve falhar com o erro "IFood.NotConnected"

Scenario: iFood recusa a chamada de pronto para retirada
    Given um pedido iFood aberto com id 1 na filial 10
    And a filial 10 esta conectada ao iFood com um token valido
    And o iFood recusa a chamada de pronto para retirada
    When eu tento marcar o pedido iFood 1 como pronto
    Then a operacao deve falhar com o erro "IFood.ActionFailed"

Scenario: Marcar pedido como pronto com sucesso atualiza o status
    Given um pedido iFood aberto com id 1 na filial 10
    And a filial 10 esta conectada ao iFood com um token valido
    And o iFood aceita a chamada de pronto para retirada
    When eu tento marcar o pedido iFood 1 como pronto
    Then a operacao deve ter sucesso
    And o status do pedido iFood deve ser "READY_TO_PICKUP"
