Feature: Listar minhas features de acesso
    Regras de negocio do GetMyFeaturesQueryHandler: gerentes tem acesso total a todas as features
    cadastradas; demais usuarios precisam existir e estar ativos, e suas features efetivas vem da
    combinacao dos vinculos do cargo com os vinculos diretos ao usuario.

Scenario: Gerente deve ter acesso total a todas as features cadastradas
    Given existe a feature cadastrada "orders.read"
    And existe a feature cadastrada "orders.write"
    When eu busco minhas features como gerente do usuario 1
    Then a operacao deve ter sucesso
    And eu devo poder gerenciar o acesso
    And a lista das minhas features deve conter o codigo "orders.read"
    And a lista das minhas features deve conter o codigo "orders.write"

Scenario: Usuario nao gerente inexistente deve falhar
    Given nao existe nenhum usuario com o id 1
    When eu busco minhas features do usuario 1
    Then a operacao deve falhar com o erro "AppUser.NotFound"

Scenario: Usuario nao gerente inativo deve falhar
    Given o usuario 1 esta inativo e sem cargo
    When eu busco minhas features do usuario 1
    Then a operacao deve falhar com o erro "AppUser.NotFound"

Scenario: Usuario nao gerente sem cargo e sem features vinculadas deve ter lista vazia
    Given o usuario 1 esta ativo sem cargo e sem features vinculadas diretamente
    When eu busco minhas features do usuario 1
    Then a operacao deve ter sucesso
    And eu nao devo poder gerenciar o acesso
    And a lista das minhas features deve estar vazia
