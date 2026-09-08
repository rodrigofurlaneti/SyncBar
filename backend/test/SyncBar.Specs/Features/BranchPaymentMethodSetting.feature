Feature: Configuracao de metodos de pagamento por filial/empresa
    Regras de negocio do dominio BranchPaymentMethodSetting: criacao de configuracao de metodos de
    pagamento (Pix, Boleto, Cartao de Credito, Cartao de Debito, Maquininha) por filial ou por
    empresa (matriz), resolucao com fallback da filial para a configuracao geral da empresa quando
    a filial nao tem configuracao propria, e exclusao de uma configuracao existente.

Scenario: Criacao de configuracao de pagamento para uma filial
    Given que existe uma empresa "Furlaneti Suporte" com a filial "Matriz"
    And nao existe configuracao de pagamento para a filial "Matriz"
    When eu envio uma solicitacao para habilitar apenas "Pix" e "Maquininha" para a filial "Matriz"
    Then a configuracao deve ser salva com sucesso
    And os metodos "Boleto", "Cartao de Credito" e "Cartao de Debito" devem estar desabilitados

Scenario: Fallback de configuracao para a matriz quando a filial nao tem configuracao propria
    Given que existe uma configuracao geral para a empresa "Furlaneti Suporte" com "Pix" habilitado
    And a filial "Filial 2" nao possui configuracao propria
    When o sistema consulta os metodos de pagamento usando a regra de fallback para a filial "Filial 2"
    Then o sistema deve retornar a configuracao geral da empresa

Scenario: Exclusao de configuracao de pagamento
    Given que existe uma configuracao de pagamento para a filial "Matriz"
    When o administrador solicita a exclusao desta configuracao
    Then a configuracao deve ser removida do banco de dados
    And a filial passara a depender da configuracao geral da empresa, se houver
