Feature: Atualizar preco extra de uma opcao de complemento
    Regras de negocio do UpdateComplementPriceCommandHandler: o grupo precisa existir e estar
    ativo; a opcao (Complement) precisa existir e estar ativa dentro do grupo; o novo preco
    extra nao pode ser negativo.

Scenario: Atualizar preco em um grupo inexistente deve falhar
    Given nao existe nenhum grupo de complemento com o id 1
    When eu tento atualizar o preco do complemento 50 do grupo 1 para 8
    Then a operacao deve falhar com o erro "ComplementGroup.NotFound"

Scenario: Atualizar preco de um complemento que nao existe no grupo deve falhar
    Given um grupo de complemento ativo com id 1 da empresa 100
    When eu tento atualizar o preco do complemento 50 do grupo 1 para 8
    Then a operacao deve falhar com o erro "ComplementGroup.ComplementNotFound"

Scenario: Atualizar preco para um valor negativo deve falhar
    Given um grupo de complemento ativo com id 1 da empresa 100
    And o grupo 1 tem o complemento 50 apontando para o item de complemento 10
    When eu tento atualizar o preco do complemento 50 do grupo 1 para -1
    Then a operacao deve falhar com o erro "Complement.InvalidExtraPrice"

Scenario: Atualizar preco com sucesso
    Given um grupo de complemento ativo com id 1 da empresa 100
    And o grupo 1 tem o complemento 50 apontando para o item de complemento 10
    When eu tento atualizar o preco do complemento 50 do grupo 1 para 8
    Then a operacao deve ter sucesso
