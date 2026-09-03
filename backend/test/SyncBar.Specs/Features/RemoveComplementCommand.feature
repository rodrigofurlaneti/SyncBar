Feature: Remover opcao de um grupo de complemento
    Regras de negocio do RemoveComplementCommandHandler: o grupo precisa existir e estar ativo;
    a opcao (Complement) removida precisa existir e estar ativa dentro do grupo. A remocao e
    logica (desativa a opcao) e dispara a sincronizacao com o Ifood.

Scenario: Remover complemento de um grupo inexistente deve falhar
    Given nao existe nenhum grupo de complemento com o id 1
    When eu tento remover o complemento 50 do grupo 1
    Then a operacao deve falhar com o erro "ComplementGroup.NotFound"

Scenario: Remover um complemento que nao existe no grupo deve falhar
    Given um grupo de complemento ativo com id 1 da empresa 100
    When eu tento remover o complemento 50 do grupo 1
    Then a operacao deve falhar com o erro "ComplementGroup.ComplementNotFound"

Scenario: Remover complemento com sucesso
    Given um grupo de complemento ativo com id 1 da empresa 100
    And o grupo 1 tem o complemento 50 apontando para o item de complemento 10
    When eu tento remover o complemento 50 do grupo 1
    Then a operacao deve ter sucesso
