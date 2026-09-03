Feature: Consultar configuracao de limite de comanda da filial
    Regras de negocio do GetComandaSettingQueryHandler: quando a filial nao tem configuracao
    cadastrada, o limite padrao retornado e 0 (interpretado como ilimitado na exibicao).

Scenario: Consultar filial sem configuracao de comanda retorna limite zero
    Given a filial 1 nao tem configuracao de limite de comanda
    When eu consulto a configuracao de comanda da filial 1
    Then a operacao deve ter sucesso
    And o limite padrao retornado deve ser 0

Scenario: Consultar filial com configuracao de comanda retorna o limite cadastrado
    Given a filial 1 tem o limite padrao de comanda de 150.00
    When eu consulto a configuracao de comanda da filial 1
    Then a operacao deve ter sucesso
    And o limite padrao retornado deve ser 150.00
