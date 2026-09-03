Feature: Registrar empresa no onboarding
    Regras de negocio do RegisterCompanyCommandHandler: onboarding self-service que cria a empresa,
    a primeira filial (com mesas, comandas e categorias padrao) e o usuario administrador em uma
    unica operacao. CNPJ da empresa, usuario/e-mail do administrador e CPF do administrador devem
    ser unicos no sistema antes de qualquer coisa ser criada.

Scenario: Registrar empresa com cnpj ja cadastrado deve falhar
    Given ja existe uma empresa cadastrada com o mesmo cnpj do onboarding
    When eu registro a nova empresa no onboarding
    Then a operacao deve falhar com o erro "Company.AlreadyExists"

Scenario: Registrar empresa com usuario ou email do administrador ja em uso deve falhar
    Given o nome de usuario ou email do administrador do onboarding ja esta em uso
    When eu registro a nova empresa no onboarding
    Then a operacao deve falhar com o erro "AppUser.AlreadyExists"

Scenario: Registrar empresa com cpf do administrador ja cadastrado deve falhar
    Given ja existe um funcionario cadastrado com o cpf do administrador do onboarding
    When eu registro a nova empresa no onboarding
    Then a operacao deve falhar com o erro "Employee.AlreadyExists"

Scenario: Registrar empresa com dados unicos cria a empresa, a filial e o administrador
    Given os dados do onboarding ainda nao estao cadastrados no sistema
    When eu registro a nova empresa no onboarding
    Then a operacao deve ter sucesso
    And a empresa, a filial e o usuario administrador devem ser criados
    And as 5 categorias, mesas e comandas padrao devem ser criadas
    And o usuario administrador deve ser vinculado ao perfil de administrador criado
