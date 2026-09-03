Feature: Atualizar dados de um grupo de complemento
    Regras de negocio do UpdateComplementGroupCommandHandler: o grupo precisa existir e estar
    ativo; as mesmas regras de dominio da criacao valem para a atualizacao (nome obrigatorio,
    selecao minima nao pode ser maior que a maxima).

Scenario: Atualizar grupo de complemento inexistente deve falhar
    Given nao existe nenhum grupo de complemento com o id 1
    When eu tento atualizar o grupo de complemento 1 com nome "Novo Nome", selecao minima 0 e selecao maxima 1
    Then a operacao deve falhar com o erro "ComplementGroup.NotFound"

Scenario: Atualizar grupo de complemento com nome vazio deve falhar
    Given um grupo de complemento ativo com id 1 da empresa 100
    When eu tento atualizar o grupo de complemento 1 com nome "", selecao minima 0 e selecao maxima 1
    Then a operacao deve falhar com o erro "ComplementGroup.EmptyName"

Scenario: Atualizar grupo de complemento com selecao minima maior que a maxima deve falhar
    Given um grupo de complemento ativo com id 1 da empresa 100
    When eu tento atualizar o grupo de complemento 1 com nome "Novo Nome", selecao minima 3 e selecao maxima 1
    Then a operacao deve falhar com o erro "ComplementGroup.MinGreaterThanMax"

Scenario: Atualizar grupo de complemento com sucesso
    Given um grupo de complemento ativo com id 1 da empresa 100
    When eu tento atualizar o grupo de complemento 1 com nome "Novo Nome", selecao minima 0 e selecao maxima 1
    Then a operacao deve ter sucesso
