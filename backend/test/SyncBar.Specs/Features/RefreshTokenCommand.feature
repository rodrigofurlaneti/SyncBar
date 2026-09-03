Feature: Renovar token de acesso
    Regras de negocio do RefreshTokenCommandHandler: exige um refresh token valido (existente, nao
    expirado e nao revogado) associado a um usuario ativo, revoga o token antigo e so emite os
    novos tokens se essa geracao tiver sucesso.

Scenario: Renovar um token inexistente deve falhar
    Given nao existe nenhum refresh token com o valor token-invalido
    When eu tento renovar o token token-invalido
    Then a operacao deve falhar com o erro "Auth.InvalidRefreshToken"

Scenario: Renovar um token ja revogado deve falhar
    Given existe um refresh token valido token-antigo do usuario 1
    And o token token-antigo ja foi revogado
    When eu tento renovar o token token-antigo
    Then a operacao deve falhar com o erro "Auth.InvalidRefreshToken"

Scenario: Renovar um token de usuario inexistente deve falhar
    Given existe um refresh token valido token-antigo do usuario 1
    And nao existe usuario com o id 1
    When eu tento renovar o token token-antigo
    Then a operacao deve falhar com o erro "Auth.InvalidRefreshToken"

Scenario: Renovar um token de usuario inativo deve falhar sem revogar o token
    Given existe um refresh token valido token-antigo do usuario 1
    And o usuario 1 esta inativo
    When eu tento renovar o token token-antigo
    Then a operacao deve falhar com o erro "Auth.InvalidRefreshToken"
    And o token antigo nao deve estar revogado

Scenario: Renovar um token valido com sucesso
    Given existe um refresh token valido token-antigo do usuario 1
    And o usuario 1 esta ativo
    When eu tento renovar o token token-antigo
    Then a operacao deve ter sucesso
    And o token antigo deve estar revogado

Scenario: Falha ao gerar o novo refresh token deve impedir a renovacao
    Given existe um refresh token valido token-antigo do usuario 1
    And o usuario 1 esta ativo
    And o provedor de token gera um novo refresh token vazio
    When eu tento renovar o token token-antigo
    Then a operacao deve falhar com o erro "RefreshToken.EmptyToken"
    And o token antigo deve estar revogado
