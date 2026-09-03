Feature: Consultar produto por id
    Regras de negocio do GetProductByIdQueryHandler: falha se o produto nao existe ou esta
    inativo (um produto desativado nao pode ser consultado por esta via); caso contrario
    retorna os dados do produto.

Scenario: Consultar produto inexistente deve falhar
    Given nao existe nenhum produto com o id 1 no catalogo
    When eu busco o produto pelo id 1
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Consultar produto inativo deve falhar
    Given um produto Refrigerante com id 1 esta cadastrado mas inativo no catalogo
    When eu busco o produto pelo id 1
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Consultar produto ativo deve ter sucesso
    Given um produto Refrigerante com id 1 e preco 8.50 esta cadastrado e ativo no catalogo
    When eu busco o produto pelo id 1
    Then a operacao deve ter sucesso
    And o nome do produto retornado deve ser "Refrigerante"
    And o preco de venda do produto retornado deve ser 8.50
