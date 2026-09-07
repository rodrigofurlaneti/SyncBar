Feature: Excluir estoque em lote no Ifood

    Regras de negocio do DeleteIfoodInventoryBatchCommandHandler: resolve o merchant da filial
    informada; caso a resolucao falhe o erro e propagado; caso a chamada ao Ifood falhe retorna
    "IfoodCatalog.DeleteInventoryBatchFailed"; caso contrario a operacao tem sucesso.

Scenario: Excluir estoque em lote com filial inexistente deve falhar
    Given nao existe nenhuma filial cadastrada com o id 1
    When eu tento excluir o estoque em lote dos produtos da filial 1
    Then a operacao deve falhar com o erro "IfoodMerchant.BranchNotFound"

Scenario: Excluir estoque em lote quando a chamada ao Ifood falha deve falhar
    Given a filial 1 esta com o merchant Ifood resolvido com sucesso
    And a exclusao de estoque em lote no Ifood falha com a mensagem "erro remoto"
    When eu tento excluir o estoque em lote dos produtos da filial 1
    Then a operacao deve falhar com o erro "IfoodCatalog.DeleteInventoryBatchFailed"

Scenario: Excluir estoque em lote com sucesso
    Given a filial 1 esta com o merchant Ifood resolvido com sucesso
    And a exclusao de estoque em lote no Ifood tem sucesso
    When eu tento excluir o estoque em lote dos produtos da filial 1
    Then a operacao deve ter sucesso
