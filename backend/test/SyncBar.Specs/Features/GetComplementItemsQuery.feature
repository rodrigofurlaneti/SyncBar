Feature: Listar itens de complemento de uma empresa
    Regras de negocio do GetComplementItemsQueryHandler: retorna todos os itens de complemento
    da empresa (ativos e inativos), ordenados por nome.

Scenario: Empresa sem itens de complemento retorna lista vazia
    Given a empresa 100 nao possui itens de complemento
    When eu busco os itens de complemento da empresa 100
    Then a operacao deve ter sucesso
    And a lista de itens de complemento retornada deve ter 0 itens

Scenario: Empresa com itens de complemento retorna a lista ordenada por nome
    Given um item de complemento com nome "Queijo extra" e ativo true da empresa 100
    And um item de complemento com nome "Bacon extra" e ativo false da empresa 100
    When eu busco os itens de complemento da empresa 100
    Then a operacao deve ter sucesso
    And a lista de itens de complemento retornada deve ter 2 itens
    And o primeiro item da lista deve se chamar "Bacon extra"
