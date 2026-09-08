Feature: Listar grupos de complemento vinculados a um produto
    Regras de negocio do GetProductComplementGroupsQueryHandler: quando o produto nao tem
    nenhum vinculo, retorna lista vazia sem consultar mais nada; a resposta vem ordenada por
    ordem de exibicao (DisplayOrder); um vinculo cujo grupo de complemento nao existe mais
    (orfao) e silenciosamente ignorado na resposta.

Scenario: Produto sem grupos de complemento vinculados retorna lista vazia
    Given o produto 5 nao possui vinculos de grupo de complemento
    When eu busco os grupos de complemento vinculados ao produto 5
    Then a operacao deve ter sucesso
    And a lista de vinculos retornada deve ter 0 grupos

Scenario: Vinculo para um grupo de complemento que nao existe mais e ignorado
    Given o produto 5 esta vinculado a um grupo de complemento inexistente com ordem de exibicao 1 no vinculo 901
    When eu busco os grupos de complemento vinculados ao produto 5
    Then a operacao deve ter sucesso
    And a lista de vinculos retornada deve ter 0 grupos
