Feature: Criar grupo de complemento
    Regras de negocio do CreateComplementGroupCommandHandler: nome obrigatorio, selecao minima
    nao pode ser negativa, selecao maxima precisa ser pelo menos 1 e a selecao minima nao pode
    ser maior que a maxima.

Scenario: Criar grupo de complemento com nome vazio deve falhar
    When eu tento criar um grupo de complemento para a empresa 100 com nome "", selecao minima 0 e selecao maxima 1
    Then a operacao deve falhar com o erro "ComplementGroup.EmptyName"

Scenario: Criar grupo de complemento com selecao minima negativa deve falhar
    When eu tento criar um grupo de complemento para a empresa 100 com nome "Adicionais", selecao minima -1 e selecao maxima 1
    Then a operacao deve falhar com o erro "ComplementGroup.InvalidMinSelection"

Scenario: Criar grupo de complemento com selecao maxima menor que 1 deve falhar
    When eu tento criar um grupo de complemento para a empresa 100 com nome "Adicionais", selecao minima 0 e selecao maxima 0
    Then a operacao deve falhar com o erro "ComplementGroup.InvalidMaxSelection"

Scenario: Criar grupo de complemento com selecao minima maior que a maxima deve falhar
    When eu tento criar um grupo de complemento para a empresa 100 com nome "Adicionais", selecao minima 3 e selecao maxima 1
    Then a operacao deve falhar com o erro "ComplementGroup.MinGreaterThanMax"

Scenario: Criar grupo de complemento com sucesso
    When eu tento criar um grupo de complemento para a empresa 100 com nome "Adicionais", selecao minima 0 e selecao maxima 1
    Then a operacao deve ter sucesso
