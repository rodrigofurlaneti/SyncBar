Feature: Definir limite padrao de comanda da filial
    Regras de negocio do SetComandaDefaultLimitCommandHandler: faz upsert por filial — cria a
    configuracao se ainda nao existir, ou atualiza a existente — e o limite deve ser maior que zero.

Scenario: Definir limite para filial sem configuracao cria uma nova configuracao
    Given a filial 1 ainda nao tem configuracao de limite de comanda cadastrada
    When eu defino o limite padrao de comanda da filial 1 como 100.00
    Then a operacao deve ter sucesso

Scenario: Definir limite para filial com configuracao existente atualiza o limite
    Given a filial 1 ja tem uma configuracao de limite de comanda cadastrada
    When eu defino o limite padrao de comanda da filial 1 como 200.00
    Then a operacao deve ter sucesso

Scenario: Definir limite invalido para filial com configuracao existente deve falhar
    Given a filial 1 ja tem uma configuracao de limite de comanda cadastrada
    When eu defino o limite padrao de comanda da filial 1 como 0.00
    Then a operacao deve falhar com o erro "ComandaSetting.InvalidLimit"
