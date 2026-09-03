Feature: Criar categoria
    Regras de negocio do CreateCategoryCommandHandler: o nome da categoria e obrigatorio
    (validado no dominio via Category.Create); caso contrario a categoria e criada, adicionada
    ao repositorio e a sincronizacao do cardapio com o Ifood e disparada para a empresa.

Scenario: Criar categoria com nome vazio deve falhar
    When eu tento criar a categoria com nome vazio, ordem 0, para a empresa 1
    Then a operacao deve falhar com o erro "Category.EmptyName"

Scenario: Criar categoria com nome valido deve ter sucesso
    When eu tento criar a categoria "Bebidas" com ordem 1 para a empresa 1
    Then a operacao deve ter sucesso

Scenario: Criar categoria com nome valido deve adiciona-la ao repositorio
    When eu tento criar a categoria "Bebidas" com ordem 1 para a empresa 1
    Then a categoria criada deve ser adicionada ao repositorio da empresa 1
