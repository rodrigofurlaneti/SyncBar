Feature: Consultar cardapio
    Regras de negocio do GetMenuQueryHandler: lista os produtos ativos da empresa (o filtro de
    inativos fica a cargo do repositorio) com o nome da categoria resolvido; se o produto
    referencia uma categoria que nao foi encontrada no mapa de categorias da empresa, usa
    "Geral" como nome de categoria (fallback).

Scenario: Consultar cardapio de empresa sem produtos retorna lista vazia
    Given a empresa 1 nao possui nenhum produto no cardapio
    When eu busco o cardapio da empresa 1
    Then a operacao deve ter sucesso
    And a lista do cardapio deve estar vazia

@ignore
Scenario: Consultar cardapio resolve o nome da categoria do produto
    Given a categoria 1 com nome "Bebidas" pertence a empresa 1
    And um produto ativo "Refrigerante" com id 10, categoria 1 e preco 8.50 pertence a empresa 1
    When eu busco o cardapio da empresa 1
    Then a operacao deve ter sucesso
    And a lista do cardapio deve conter 1 item
    And o nome da categoria do item na posicao 0 do cardapio deve ser "Bebidas"

Scenario: Consultar cardapio com categoria nao encontrada usa o nome padrao Geral
    Given um produto ativo "Porcao de batata" com id 11, categoria 99 e preco 20.00 pertence a empresa 1
    When eu busco o cardapio da empresa 1
    Then a operacao deve ter sucesso
    And o nome da categoria do item na posicao 0 do cardapio deve ser "Geral"
