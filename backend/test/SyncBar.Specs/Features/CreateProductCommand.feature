Feature: Criar produto
    Regras de negocio do CreateProductCommandHandler: a categoria informada precisa existir,
    estar ativa e pertencer a mesma empresa do produto; o nome do produto e obrigatorio
    (validado no dominio via Product.Create); caso contrario o produto e criado, adicionado ao
    repositorio e a sincronizacao do cardapio com o Ifood e disparada para a empresa.

Scenario: Criar produto com categoria inexistente deve falhar
    Given nao existe nenhuma categoria cadastrada com o id 5
    When eu tento criar o produto "Refrigerante" na categoria 5 para a empresa 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Criar produto com categoria inativa deve falhar
    Given a categoria Bebidas com id 5 esta inativa para a empresa 1
    When eu tento criar o produto "Refrigerante" na categoria 5 para a empresa 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Criar produto com categoria de outra empresa deve falhar
    Given a categoria Bebidas com id 5 esta ativa para a empresa 2
    When eu tento criar o produto "Refrigerante" na categoria 5 para a empresa 1
    Then a operacao deve falhar com o erro "Category.NotFound"

Scenario: Criar produto com categoria valida e sucesso
    Given a categoria Bebidas com id 5 esta ativa para a empresa 1
    When eu tento criar o produto "Refrigerante" na categoria 5 para a empresa 1
    Then a operacao deve ter sucesso
