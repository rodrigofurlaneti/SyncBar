Feature: Reconhecer alerta operacional do Ifood
    Regras de negocio do AcknowledgeIfoodOperationalAlertCommandHandler: reconhece o alerta no
    armazenamento em memoria; a operacao e idempotente, entao reconhecer um alerta que ja nao
    existe mais (por ja ter sido reconhecido em outra aba, ou por ter estourado o limite por
    empresa) tambem deve ter sucesso.

Scenario: Reconhecer um alerta existente deve ter sucesso
    Given existe um alerta operacional pendente para a empresa 1
    When eu tento reconhecer o alerta operacional da empresa 1
    Then a operacao deve ter sucesso

Scenario: Reconhecer um alerta que ja nao existe mais deve ter sucesso de forma idempotente
    Given nao existe mais o alerta operacional para a empresa 1
    When eu tento reconhecer o alerta operacional da empresa 1
    Then a operacao deve ter sucesso
