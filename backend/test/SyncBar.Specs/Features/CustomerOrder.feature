Feature: Pedido em mesa e comanda
    Regras de abertura, lancamento de itens e fechamento de pedidos

Scenario: Abrir pedido sem mesa e sem comanda deve falhar
    When I open an order without a table and without a comanda
    Then the order creation should fail with error "CustomerOrder.MissingOrigin"

Scenario: Abrir pedido em mesa deve iniciar com status Aberto
    When I open an order for table 10
    Then the order should be created with status "Aberto"

Scenario: Lancar item congela o preco do cardapio
    Given an open order for table 10
    When I add 2 units of a product priced at 14.90
    Then the order subtotal should be 29.80

Scenario: Fechar pedido aplica taxa de servico de 10 por cento
    Given an open order for table 10
    And the order has 1 unit of a product priced at 100.00
    When I close the order with a service fee of 10 percent
    Then the order total should be 110.00
    And the order status should be "AguardandoPagamento"
