Feature: Adicionar opcao a um grupo de complemento
    Regras de negocio do AddComplementCommandHandler: o grupo de complemento e o item de
    complemento precisam existir, estar ativos e pertencer a mesma empresa antes de a opcao
    ser adicionada; nao pode duplicar um item de complemento ja ativo no mesmo grupo.

Scenario: Adicionar complemento a um grupo inexistente deve falhar
    Given nao existe nenhum grupo de complemento com o id 1
    When eu tento adicionar o item de complemento 10 ao grupo 1 com preco extra 5
    Then a operacao deve falhar com o erro "ComplementGroup.NotFound"

Scenario: Adicionar complemento com item de complemento inexistente deve falhar
    Given um grupo de complemento ativo com id 1 da empresa 100
    And nao existe nenhum item de complemento com o id 10
    When eu tento adicionar o item de complemento 10 ao grupo 1 com preco extra 5
    Then a operacao deve falhar com o erro "ComplementItem.NotFound"

Scenario: Adicionar complemento com item de complemento de outra empresa deve falhar
    Given um grupo de complemento ativo com id 1 da empresa 100
    And um item de complemento ativo com id 10 da empresa 200
    When eu tento adicionar o item de complemento 10 ao grupo 1 com preco extra 5
    Then a operacao deve falhar com o erro "ComplementItem.NotFound"

Scenario: Adicionar um item de complemento que ja esta no grupo deve falhar
    Given um grupo de complemento ativo com id 1 da empresa 100
    And um item de complemento ativo com id 10 da empresa 100
    And o grupo 1 ja contem o item de complemento 10
    When eu tento adicionar o item de complemento 10 ao grupo 1 com preco extra 5
    Then a operacao deve falhar com o erro "ComplementGroup.DuplicateComplementItem"

Scenario: Adicionar complemento com sucesso
    Given um grupo de complemento ativo com id 1 da empresa 100
    And um item de complemento ativo com id 10 da empresa 100
    When eu tento adicionar o item de complemento 10 ao grupo 1 com preco extra 5
    Then a operacao deve ter sucesso
