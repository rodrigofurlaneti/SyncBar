Feature: Definir atendente self-service da filial
    Regras de negocio do SetSelfServiceEmployeeCommandHandler: exige uma filial existente e ativa;
    quando um funcionario e informado, ele precisa existir, estar ativo e pertencer a essa mesma
    filial; quando nao e informado, o atendente self-service e apenas removido, sem consultar o
    repositorio de funcionarios.

Scenario: Definir atendente de uma filial inexistente deve falhar
    Given nao existe a filial 1
    When eu defino o funcionario 5 como atendente self-service da filial 1
    Then a operacao deve falhar com o erro "Branch.NotFound"

Scenario: Definir atendente de uma filial inativa deve falhar
    Given existe a filial 1 inativa
    When eu defino o funcionario 5 como atendente self-service da filial 1
    Then a operacao deve falhar com o erro "Branch.NotFound"

Scenario: Definir um funcionario inexistente como atendente deve falhar
    Given existe a filial 1 ativa
    And nao existe o funcionario 99
    When eu defino o funcionario 99 como atendente self-service da filial 1
    Then a operacao deve falhar com o erro "Employee.NotFound"

Scenario: Definir um funcionario inativo como atendente deve falhar
    Given existe a filial 1 ativa
    And existe o funcionario 5 inativo na filial 1
    When eu defino o funcionario 5 como atendente self-service da filial 1
    Then a operacao deve falhar com o erro "Employee.NotFound"

Scenario: Definir um funcionario de outra filial como atendente deve falhar
    Given existe a filial 1 ativa
    And existe o funcionario 5 ativo na filial 2
    When eu defino o funcionario 5 como atendente self-service da filial 1
    Then a operacao deve falhar com o erro "Employee.NotFound"

Scenario: Remover o atendente self-service nao deve consultar o repositorio de funcionarios
    Given existe a filial 1 ativa
    When eu removo o atendente self-service da filial 1
    Then a operacao deve ter sucesso
    And a filial nao deve ter atendente self-service
    And o repositorio de funcionarios nao deve ser consultado

Scenario: Definir um funcionario valido da mesma filial deve ter sucesso
    Given existe a filial 1 ativa
    And existe o funcionario 5 ativo na filial 1
    When eu defino o funcionario 5 como atendente self-service da filial 1
    Then a operacao deve ter sucesso
    And a filial deve ter o funcionario 5 como atendente self-service
