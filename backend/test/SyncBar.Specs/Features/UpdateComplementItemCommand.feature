Feature: Atualizar nome de um item de complemento
    Regras de negocio do UpdateComplementItemCommandHandler: o item precisa existir e estar
    ativo; o nome nao pode ficar vazio. Ao atualizar com sucesso, a sincronizacao com o Ifood e
    disparada, pois o nome do item vira o nome de cada opcao (option) que o usa no cardapio.

Scenario: Atualizar item de complemento inexistente deve falhar
    Given nao existe nenhum item de complemento com o id 10
    When eu tento atualizar o item de complemento 10 com nome "Novo Nome"
    Then a operacao deve falhar com o erro "ComplementItem.NotFound"

Scenario: Atualizar item de complemento com nome vazio deve falhar
    Given um item de complemento ativo com id 10 da empresa 100
    When eu tento atualizar o item de complemento 10 com nome ""
    Then a operacao deve falhar com o erro "ComplementItem.EmptyName"

Scenario: Atualizar item de complemento com sucesso
    Given um item de complemento ativo com id 10 da empresa 100
    When eu tento atualizar o item de complemento 10 com nome "Novo Nome"
    Then a operacao deve ter sucesso
