Feature: Definir imagem do produto
    Regras de negocio do SetProductImageCommandHandler: falha se o produto nao existe ou esta
    inativo; caso contrario salva o arquivo no armazenamento de imagens e atualiza a URL da
    imagem do produto com o resultado.

Scenario: Definir imagem de produto inexistente deve falhar
    Given nao existe nenhum produto para definir imagem com o id 1
    When eu tento definir a imagem do produto 1 com extensao ".png"
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Definir imagem de produto inativo deve falhar
    Given um produto Pizza com id 1 esta inativo e sem imagem
    When eu tento definir a imagem do produto 1 com extensao ".png"
    Then a operacao deve falhar com o erro "Product.NotFound"

Scenario: Definir imagem de produto ativo deve ter sucesso
    Given um produto Pizza com id 1 esta ativo e sem imagem
    And o armazenamento de imagens salva a imagem do produto e retorna a url "/images/products/1.png"
    When eu tento definir a imagem do produto 1 com extensao ".png"
    Then a operacao deve ter sucesso
    And a url da imagem retornada deve ser "/images/products/1.png"
    And a imagem do produto deve ser "/images/products/1.png"
