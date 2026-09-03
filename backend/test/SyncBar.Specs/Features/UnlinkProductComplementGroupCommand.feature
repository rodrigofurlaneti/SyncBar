Feature: Desvincular grupo de complemento de um produto
    Regras de negocio do UnlinkProductComplementGroupCommandHandler: o vinculo precisa existir e
    estar ativo. Apos desativar o vinculo, a sincronizacao com o Ifood so e disparada se o
    produto dono do vinculo ainda for encontrado — se o produto nao existir mais, a operacao
    continua tendo sucesso, apenas sem disparar a sincronizacao.

Scenario: Desvincular um vinculo inexistente deve falhar
    Given nao existe nenhum vinculo produto-grupo com o id 900
    When eu tento desvincular o vinculo produto-grupo 900
    Then a operacao deve falhar com o erro "ProductComplementGroup.NotFound"

Scenario: Desvincular com sucesso quando o produto ainda existe
    Given um vinculo produto-grupo ativo com id 900 do produto 5 e grupo 1
    And um produto ativo com id 5 da empresa 100
    When eu tento desvincular o vinculo produto-grupo 900
    Then a operacao deve ter sucesso

Scenario: Desvincular com sucesso mesmo quando o produto nao existe mais
    Given um vinculo produto-grupo ativo com id 900 do produto 5 e grupo 1
    And nao existe nenhum produto com o id 5
    When eu tento desvincular o vinculo produto-grupo 900
    Then a operacao deve ter sucesso
