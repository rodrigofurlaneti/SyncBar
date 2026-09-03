Feature: Login de usuario
    Regras de negocio do LoginCommandHandler: valida existencia, status ativo e bloqueio por
    tentativas do usuario, confere a senha e so emite os tokens de acesso e de refresh se todas as
    validacoes passarem.

Scenario: Login com usuario inexistente deve falhar
    Given nao existe nenhum usuario com o nome de usuario joao
    When eu tento fazer login com o usuario joao e a senha qualquer-senha
    Then a operacao deve falhar com o erro "Auth.InvalidCredentials"

Scenario: Login com usuario inativo deve falhar
    Given existe um usuario inativo joao
    When eu tento fazer login com o usuario joao e a senha qualquer-senha
    Then a operacao deve falhar com o erro "Auth.InvalidCredentials"

Scenario: Login com usuario bloqueado por excesso de tentativas deve falhar
    Given o usuario joao esta bloqueado por excesso de tentativas de login
    When eu tento fazer login com o usuario joao e a senha qualquer-senha
    Then a operacao deve falhar com o erro "Auth.LockedOut"

Scenario: Login com senha incorreta deve falhar e registrar a tentativa
    Given existe um usuario ativo joao
    And a senha informada esta incorreta
    When eu tento fazer login com o usuario joao e a senha senha-errada
    Then a operacao deve falhar com o erro "Auth.InvalidCredentials"
    And o numero de tentativas de login falhas do usuario deve ser 1

Scenario: Login com credenciais validas deve ter sucesso
    Given existe um usuario ativo joao
    And a senha informada esta correta
    When eu tento fazer login com o usuario joao e a senha senha-correta
    Then a operacao deve ter sucesso

Scenario: Falha ao gerar o refresh token deve impedir o login
    Given existe um usuario ativo joao
    And a senha informada esta correta
    And o provedor de token gera um refresh token vazio
    When eu tento fazer login com o usuario joao e a senha senha-correta
    Then a operacao deve falhar com o erro "RefreshToken.EmptyToken"
