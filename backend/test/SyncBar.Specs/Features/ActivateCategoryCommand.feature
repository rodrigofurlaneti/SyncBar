Feature: Ativar categoria
    Regras de negocio do ActivateCategoryCommandHandler: falha se a categoria nao existe;
    ativar uma categoria ja ativa e uma operacao idempotente (sucesso sem alterar nada);
    caso contrario ativa a categoria e dispara a sincronizacao do cardapio com o Ifood.

Scenario: Ativar categoria inexistente deve falhar
    Given nao existe nenhuma categoria com o id 1
    When eu tento ativar a categoria 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Ativar categoria ja ativa deve ter sucesso sem alterar nada
    Given uma categoria Bebidas com id 1 esta ativa
    When eu tento ativar a categoria 1
    Then a operacao deve ter sucesso
    And a categoria deve continuar ativa

Scenario: Ativar categoria inativa deve ter sucesso
    Given uma categoria Bebidas com id 1 esta inativa
    When eu tento ativar a categoria 1
    Then a operacao deve ter sucesso
    And a categoria deve continuar ativa
