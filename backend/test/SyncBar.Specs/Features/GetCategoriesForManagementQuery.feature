Feature: Consultar categorias para gerenciamento
    Regras de negocio do GetCategoriesForManagementQueryHandler: ao contrario da consulta usada
    nas telas de pedido/venda, inclui categorias inativas (para a tela admin poder listar e
    reativa-las) e conta quantos produtos cada categoria tem, ordenando por ordem de exibicao e
    nome.

Scenario: Consultar categorias para gerenciamento sem nenhuma cadastrada retorna lista vazia
    Given a empresa 1 nao possui nenhuma categoria cadastrada para gerenciamento
    When eu busco as categorias para gerenciamento da empresa 1
    Then a operacao deve ter sucesso
    And a lista de categorias para gerenciamento deve conter 0 categorias

Scenario: Consultar categorias para gerenciamento inclui categorias inativas
    Given a categoria inativa "Promocoes antigas" com id 3 e ordem 0 pertence a empresa 1
    When eu busco as categorias para gerenciamento da empresa 1
    Then a operacao deve ter sucesso
    And a categoria "Promocoes antigas" na lista de gerenciamento deve estar inativa

Scenario: Consultar categorias para gerenciamento conta os produtos de cada categoria
    Given a categoria ativa "Bebidas" com id 1 e ordem 0 esta cadastrada na empresa 1 para gerenciamento
    And existem 3 produtos cadastrados na categoria 1
    When eu busco as categorias para gerenciamento da empresa 1
    Then a operacao deve ter sucesso
    And a categoria "Bebidas" na lista de gerenciamento deve ter 3 produtos
