Feature: Criar sessao de autorizacao Keeta
    Regras de negocio do CreateKeetaIntegrationAuthorizationSessionCommandHandler: nao pode
    existir outra sessao com o mesmo AuthId; caso contrario a sessao e criada e persistida.

Scenario: Criar sessao com AuthId ja existente deve falhar
    Given ja existe uma sessao de autorizacao Keeta com o authId "auth-1" para a empresa 1 filial 2
    When eu tento criar uma sessao de autorizacao Keeta para a empresa 1 filial 2 com authId "auth-1" e tipo de operacao 1
    Then a operacao deve falhar com o erro "KeetaAuthorizationSession.AlreadyExists"

Scenario: Criar sessao com AuthId novo deve ter sucesso
    Given nao existe sessao de autorizacao Keeta com o authId "auth-1"
    When eu tento criar uma sessao de autorizacao Keeta para a empresa 1 filial 2 com authId "auth-1" e tipo de operacao 1
    Then a operacao deve ter sucesso
    And a sessao de autorizacao Keeta criada deve ter o authId "auth-1" e tipo de operacao 1
