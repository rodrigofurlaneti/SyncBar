Feature: Listar filiais de uma empresa
    Regra de negocio do GetBranchesByCompanyQueryHandler: retorna todas as filiais da empresa
    devolvidas pelo repositorio, mapeando id, nome, cnpj, telefone, cidade, estado e status, sem
    filtrar por filiais ativas ou inativas.

Scenario: Empresa sem filiais deve retornar lista vazia
    Given a empresa 1 nao tem filiais
    When eu busco as filiais da empresa 1
    Then a operacao deve ter sucesso
    And a lista de filiais deve estar vazia

Scenario: Buscar filiais deve retornar filiais ativas e inativas sem filtrar
    Given a empresa 1 tem a filial ativa "Filial Centro"
    And a empresa 1 tem a filial inativa "Filial Zona Sul"
    When eu busco as filiais da empresa 1
    Then a operacao deve ter sucesso
    And a lista de filiais deve conter 2 itens
    And a filial "Filial Centro" deve aparecer como ativa na lista
    And a filial "Filial Zona Sul" deve aparecer como inativa na lista
