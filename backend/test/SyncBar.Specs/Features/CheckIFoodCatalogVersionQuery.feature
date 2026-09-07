Feature: Consultar versao do catalogo Ifood

    Regras de negocio do CheckIfoodCatalogVersionQueryHandler: resolve o merchant da filial
    informada (filial precisa existir, integracao habilitada, merchant configurado e token
    valido); caso a resolucao falhe o erro e propagado; caso a chamada ao Ifood falhe retorna
    "IfoodCatalog.VersionCheckFailed"; caso contrario retorna a versao do catalogo.

Scenario: Consultar versao com filial inexistente deve falhar
    Given nao existe nenhuma filial cadastrada com o id 1
    When eu consulto a versao do catalogo Ifood da filial 1
    Then a operacao deve falhar com o erro "IfoodMerchant.BranchNotFound"

Scenario: Consultar versao quando a chamada ao Ifood falha deve falhar
    Given a filial 1 esta com o merchant Ifood resolvido com sucesso
    And a consulta de versao do catalogo no Ifood falha com a mensagem "erro remoto"
    When eu consulto a versao do catalogo Ifood da filial 1
    Then a operacao deve falhar com o erro "IfoodCatalog.VersionCheckFailed"

Scenario: Consultar versao com sucesso retorna a versao do catalogo
    Given a filial 1 esta com o merchant Ifood resolvido com sucesso
    And a consulta de versao do catalogo no Ifood retorna a versao "v2"
    When eu consulto a versao do catalogo Ifood da filial 1
    Then a operacao deve ter sucesso
    And a versao do catalogo retornada deve ser "v2"
