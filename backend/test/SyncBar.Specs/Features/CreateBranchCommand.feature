Feature: Criar filial
    Regra de negocio do CreateBranchCommandHandler: uma filial exige um nome nao vazio; se valida,
    e persistida com todos os dados informados e o handler retorna o id gerado.

Scenario: Criar uma filial com nome vazio deve falhar sem persistir
    When eu crio uma filial para a empresa 1 com o nome "   "
    Then a operacao deve falhar com o erro "Branch.EmptyName"
    And nenhuma filial deve ser persistida

Scenario: Criar uma filial com dados validos deve ter sucesso e persistir a filial
    When eu crio uma filial para a empresa 1 com o nome "Filial Centro"
    Then a operacao deve ter sucesso
    And a filial deve ser persistida com os dados informados
