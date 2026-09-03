Feature: Ativar produto
    Regras de negocio do ActivateProductCommandHandler: falha se o produto nao existe;
    ativar um produto ja ativo e uma operacao idempotente (sucesso sem alterar nada);
    caso contrario ativa o produto e dispara a sincronizacao do cardapio com o Ifood.

Scenario: Ativar produto inexistente deve falhar
    Given nao existe nenhum produto com o id 1
    When eu tento ativar o produto 1
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Ativar produto ja ativo deve ter sucesso sem alterar nada
    Given um produto Refrigerante com id 1 esta ativo
    When eu tento ativar o produto 1
    Then a operacao deve ter sucesso
    And o produto deve continuar ativo

Scenario: Ativar produto inativo deve ter sucesso
    Given um produto Refrigerante com id 1 esta inativo
    When eu tento ativar o produto 1
    Then a operacao deve ter sucesso
    And o produto deve continuar ativo
