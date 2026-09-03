Feature: Consultar cardapio para gerenciamento
    Regras de negocio do GetMenuForManagementQueryHandler: ao contrario da consulta usada nas
    telas de pedido/venda, inclui produtos inativos (para a tela admin poder listar e
    reativa-los) e expoe o status de cada um; se a categoria do produto nao for encontrada usa
    "Categoria removida" como nome (fallback).

Scenario: Consultar cardapio de gerenciamento inclui produtos inativos
    Given um produto inativo "Sanduiche antigo" com id 20, categoria 1 e preco 15.00 esta cadastrado na empresa 1 para gerenciamento
    When eu busco o cardapio de gerenciamento da empresa 1
    Then a operacao deve ter sucesso
    And o produto "Sanduiche antigo" na lista de gerenciamento do cardapio deve estar inativo

Scenario: Consultar cardapio de gerenciamento com categoria removida usa o nome padrao
    Given um produto ativo "Combo especial" com id 21, categoria 99 e preco 30.00 esta cadastrado na empresa 1 para gerenciamento
    When eu busco o cardapio de gerenciamento da empresa 1
    Then a operacao deve ter sucesso
    And o nome da categoria do produto "Combo especial" na lista de gerenciamento deve ser "Categoria removida"
