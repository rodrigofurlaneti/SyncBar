Feature: Atualizar produto
    Regras de negocio do UpdateProductCommandHandler: falha se o produto nao existe ou esta
    inativo; a categoria informada precisa existir, estar ativa e pertencer a mesma empresa do
    produto; o preco de venda nao pode ficar negativo (validado no dominio via
    Product.UpdateDetails); caso contrario atualiza os dados e dispara a sincronizacao do
    cardapio com o Ifood. Itens ja lancados em pedidos nao sao afetados (preco congelado no
    lancamento).

Scenario: Atualizar produto inexistente deve falhar
    Given nao existe nenhum produto para atualizar com o id 1
    When eu tento atualizar o produto 1 na categoria 1 com preco 10.00
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Atualizar produto para categoria inexistente deve falhar
    Given um produto Pizza com id 1 esta ativo e pertence a empresa 1
    And nao existe categoria alguma com o id 5 para atualizacao de produto
    When eu tento atualizar o produto 1 na categoria 5 com preco 10.00
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Atualizar produto para categoria de outra empresa deve falhar
    Given um produto Pizza com id 1 esta ativo e pertence a empresa 1
    And a categoria Massas com id 5 esta ativa e pertence a empresa 2
    When eu tento atualizar o produto 1 na categoria 5 com preco 10.00
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Atualizar produto com preco de venda negativo deve falhar
    Given um produto Pizza com id 1 esta ativo e pertence a empresa 1
    And a categoria Massas com id 5 esta ativa e pertence a empresa 1
    When eu tento atualizar o produto 1 na categoria 5 com preco -10.00
    Then a operacao deve falhar com o erro "Product.InvalidSalePrice"

Scenario: Atualizar produto com dados validos deve ter sucesso
    Given um produto Pizza com id 1 esta ativo e pertence a empresa 1
    And a categoria Massas com id 5 esta ativa e pertence a empresa 1
    When eu tento atualizar o produto 1 na categoria 5 com preco 45.00
    Then a operacao deve ter sucesso
    And o preco de venda do produto atualizado deve ser 45.00
    And a categoria do produto atualizado deve ser 5
