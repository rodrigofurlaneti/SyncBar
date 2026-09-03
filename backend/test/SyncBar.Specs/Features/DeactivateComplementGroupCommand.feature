Feature: Desativar grupo de complemento
    Regras de negocio do DeactivateComplementGroupCommandHandler: o grupo precisa existir e
    estar ativo para poder ser desativado; ao desativar com sucesso, a sincronizacao com o
    Ifood e disparada.

Scenario: Desativar grupo de complemento inexistente deve falhar
    Given nao existe nenhum grupo de complemento com o id 1
    When eu tento desativar o grupo de complemento 1
    Then a operacao deve falhar com o erro "ComplementGroup.NotFound"

Scenario: Desativar grupo de complemento ja inativo deve falhar
    Given um grupo de complemento inativo com id 1 da empresa 100
    When eu tento desativar o grupo de complemento 1
    Then a operacao deve falhar com o erro "ComplementGroup.NotFound"

Scenario: Desativar grupo de complemento com sucesso
    Given um grupo de complemento ativo com id 1 da empresa 100
    When eu tento desativar o grupo de complemento 1
    Then a operacao deve ter sucesso
    And o grupo de complemento deve estar inativo
