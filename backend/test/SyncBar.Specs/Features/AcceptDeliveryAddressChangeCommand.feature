Feature: Aceitar troca de endereco de entrega no Ifood
    Regras de negocio do AcceptDeliveryAddressChangeCommandHandler: valida existencia do pedido
    Ifood e da filial, exige token valido de integracao com o Ifood, e so tem sucesso se o Ifood
    confirmar a aceitacao da troca de endereco.

Scenario: Aceitar troca de endereco de pedido inexistente deve falhar
    Given nao existe nenhum pedido Ifood com o id 1
    When eu tento aceitar a troca de endereco do pedido Ifood 1
    Then a operacao deve falhar com o erro "IfoodOrder.NotFound"

Scenario: Aceitar troca de endereco sem filial cadastrada deve falhar
    Given um pedido Ifood aberto com id 1 na filial 10
    And a filial 10 nao esta cadastrada
    When eu tento aceitar a troca de endereco do pedido Ifood 1
    Then a operacao deve falhar com o erro "Branch.NotFound"

Scenario: Aceitar troca de endereco sem token valido do Ifood deve falhar
    Given um pedido Ifood aberto com id 1 na filial 10
    And a filial 10 nao tem um token valido do Ifood
    When eu tento aceitar a troca de endereco do pedido Ifood 1
    Then a operacao deve falhar com o erro "Ifood.NotConnected"

Scenario: Ifood recusar a aceitacao da troca de endereco deve falhar
    Given um pedido Ifood aberto com id 1 na filial 10
    And a filial 10 esta conectada ao Ifood com um token valido
    And o Ifood recusa a troca de endereco de entrega
    When eu tento aceitar a troca de endereco do pedido Ifood 1
    Then a operacao deve falhar com o erro "IfoodShipping.AcceptAddressChangeFailed"

Scenario: Aceitar troca de endereco com sucesso
    Given um pedido Ifood aberto com id 1 na filial 10
    And a filial 10 esta conectada ao Ifood com um token valido
    And o Ifood aceita a troca de endereco de entrega
    When eu tento aceitar a troca de endereco do pedido Ifood 1
    Then a operacao deve ter sucesso
