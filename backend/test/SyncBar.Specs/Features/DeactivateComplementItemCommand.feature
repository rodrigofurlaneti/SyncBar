Feature: Desativar item de complemento
    Regras de negocio do DeactivateComplementItemCommandHandler: o item precisa existir e estar
    ativo para poder ser desativado. A desativacao NAO propaga em cascata para os Complement
    (opcoes) que usam este item em algum grupo — quem gerencia o grupo decide se remove a opcao.

Scenario: Desativar item de complemento inexistente deve falhar
    Given nao existe nenhum item de complemento com o id 10
    When eu tento desativar o item de complemento 10
    Then a operacao deve falhar com o erro "ComplementItem.NotFound"

Scenario: Desativar item de complemento ja inativo deve falhar
    Given um item de complemento inativo com id 10 da empresa 100
    When eu tento desativar o item de complemento 10
    Then a operacao deve falhar com o erro "ComplementItem.NotFound"

Scenario: Desativar item de complemento com sucesso
    Given um item de complemento ativo com id 10 da empresa 100
    When eu tento desativar o item de complemento 10
    Then a operacao deve ter sucesso
    And o item de complemento deve estar inativo
