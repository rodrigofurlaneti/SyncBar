Feature: Reverter versao do catalogo Ifood

    Regras de negocio do DowngradeIfoodCatalogVersionCommandHandler: resolve o merchant da filial
    informada; caso a resolucao falhe o erro e propagado; caso a chamada ao Ifood falhe retorna
    "IfoodCatalog.DowngradeVersionFailed"; caso contrario a operacao tem sucesso. E uma operacao
    destrutiva e irreversivel contra o catalogo real do merchant, por isso so deve ser acionada
    mediante confirmacao explicita do usuario.

Scenario: Reverter versao com filial inexistente deve falhar
    Given nao existe nenhuma filial cadastrada com o id 1
    When eu tento reverter a versao do catalogo Ifood da filial 1
    Then a operacao deve falhar com o erro "IfoodMerchant.BranchNotFound"

Scenario: Reverter versao quando a chamada ao Ifood falha deve falhar
    Given a filial 1 esta com o merchant Ifood resolvido com sucesso
    And a reversao de versao do catalogo no Ifood falha com a mensagem "erro remoto"
    When eu tento reverter a versao do catalogo Ifood da filial 1
    Then a operacao deve falhar com o erro "IfoodCatalog.DowngradeVersionFailed"

Scenario: Reverter versao com sucesso
    Given a filial 1 esta com o merchant Ifood resolvido com sucesso
    And a reversao de versao do catalogo no Ifood tem sucesso
    When eu tento reverter a versao do catalogo Ifood da filial 1
    Then a operacao deve ter sucesso
