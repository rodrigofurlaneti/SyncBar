Feature: Definir features de um cargo
    Regras de negocio do SetJobTitleFeaturesCommandHandler: exige um cargo existente e ativo, e
    sincroniza os vinculos de features do cargo com a lista desejada — desativando os que saem,
    reativando os que voltam e criando os que nunca existiram.

Scenario: Definir features de um cargo inexistente deve falhar
    Given nao existe o cargo 1
    When eu defino as features 10,20 para o cargo 1
    Then a operacao deve falhar com o erro "JobTitle.NotFound"

Scenario: Definir features de um cargo inativo deve falhar
    Given existe o cargo 1 inativo
    When eu defino as features 10 para o cargo 1
    Then a operacao deve falhar com o erro "JobTitle.NotFound"

Scenario: Definir features de um cargo ativo deve desativar reativar e criar vinculos conforme necessario
    Given existe o cargo 1 ativo
    And o cargo 1 tem a feature 10 vinculada e ativa
    And o cargo 1 tem a feature 20 vinculada mas desativada
    And o cargo 1 tem a feature 30 vinculada e ativa
    When eu defino as features 20,30,40 para o cargo 1
    Then a operacao deve ter sucesso
    And o vinculo da feature 10 do cargo deve estar inativo
    And o vinculo da feature 20 do cargo deve estar ativo
    And o vinculo da feature 30 do cargo deve estar ativo
    And deve ser criado um novo vinculo para a feature 40 do cargo

Scenario: Falha ao criar um novo vinculo deve impedir a operacao
    Given existe o cargo 1 ativo
    And o cargo 1 nao tem vinculos existentes
    When eu defino as features 0 para o cargo 1
    Then a operacao deve falhar com o erro "JobTitleFeature.InvalidIds"
    And nenhum novo vinculo do cargo deve ser criado
