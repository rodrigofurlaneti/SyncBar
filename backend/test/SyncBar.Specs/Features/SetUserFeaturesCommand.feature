Feature: Definir features de um usuario
    Regras de negocio do SetUserFeaturesCommandHandler: exige um usuario existente e ativo, e
    sincroniza os vinculos diretos de features do usuario com a lista desejada — desativando os
    que saem, reativando os que voltam e criando os que nunca existiram.

Scenario: Definir features de um usuario inexistente deve falhar sem tocar vinculos nem commitar
    Given nao existe o usuario 1
    When eu defino as features 10,20 para o usuario 1
    Then a operacao deve falhar com o erro "AppUser.NotFound"
    And nenhum novo vinculo do usuario deve ser criado

Scenario: Definir features de um usuario inativo deve falhar
    Given existe o usuario 1 inativo
    When eu defino as features 10 para o usuario 1
    Then a operacao deve falhar com o erro "AppUser.NotFound"

Scenario: Definir features de um usuario ativo deve desativar reativar e criar vinculos conforme necessario
    Given existe o usuario 1 ativo
    And o usuario 1 tem a feature 10 vinculada e ativa
    And o usuario 1 tem a feature 20 vinculada mas desativada
    And o usuario 1 tem a feature 30 vinculada e ativa
    When eu defino as features 20,30,40 para o usuario 1
    Then a operacao deve ter sucesso
    And o vinculo da feature 10 do usuario deve estar inativo
    And o vinculo da feature 20 do usuario deve estar ativo
    And o vinculo da feature 30 do usuario deve estar ativo
    And deve ser criado um novo vinculo para a feature 40 do usuario

Scenario: Falha ao criar um novo vinculo deve impedir a operacao sem commitar
    Given existe o usuario 1 ativo
    And o usuario 1 nao tem vinculos existentes
    When eu defino as features 0 para o usuario 1
    Then a operacao deve falhar com o erro "AppUserFeature.InvalidIds"
    And nenhum novo vinculo do usuario deve ser criado
