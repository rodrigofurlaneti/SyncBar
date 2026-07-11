using Microsoft.EntityFrameworkCore;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Infrastructure.Persistence;

namespace SyncBar.Infrastructure.Printing;

internal sealed class PrintingService(
    AppDbContext context,
    IEnumerable<IRawPrinterTransport> transports) : IPrintingService
{
    public async Task PrintOrderItemsAsync(long customerOrderId, IReadOnlyCollection<long> orderItemIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await context.CustomerOrders.AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == customerOrderId, cancellationToken);
            if (order is null) return;

            var settings = await GetSettingsAsync(order.BranchId, cancellationToken);
            if (settings is { PrintOrdersEnabled: false }) return;

            var printers = await GetPrintersAsync(order.BranchId, ordersOnly: true, cancellationToken);
            if (printers.Count == 0) return;

            var items = order.Items.Where(i => orderItemIds.Contains(i.Id) && i.IsActive).ToList();
            if (items.Count == 0) return;

            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = await context.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);
            var employees = await context.Employees.AsNoTracking()
                .Where(e => e.BranchId == order.BranchId).ToListAsync(cancellationToken);

            var ticketItems = items.Select(i =>
            {
                var product = products.FirstOrDefault(p => p.Id == i.ProductId);
                var requesterId = i.EmployeeId ?? order.EmployeeId;
                return new TicketFormatter.OrderTicketItem(
                    i.Quantity,
                    product?.Name ?? $"Produto {i.ProductId}",
                    i.Notes,
                    product?.PreparationTimeMinutes ?? 5,
                    employees.FirstOrDefault(e => e.Id == requesterId)?.Name);
            }).ToList();

            var content = TicketFormatter.OrderTicket(
                await OriginLabelAsync(order, cancellationToken),
                ExtractCustomerName(order.Notes),
                order.Id, DateTime.Now, ticketItems);

            await SendToAllAsync(printers, content, cancellationToken);
        }
        catch
        {
            // Impressao de pedido NUNCA derruba o lancamento — falha silenciosa aqui,
            // o operador percebe pela ausencia do cupom e usa o teste de impressora.
        }
    }

    public async Task<Result> PrintBillAsync(long customerOrderId, CancellationToken cancellationToken = default)
    {
        var order = await context.CustomerOrders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == customerOrderId, cancellationToken);
        if (order is null)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        var settings = await GetSettingsAsync(order.BranchId, cancellationToken);
        if (settings is { PrintBillsEnabled: false })
            return Result.Failure(new Error("Printing.Disabled", "A impressão de contas está desligada."));

        var printers = await GetPrintersAsync(order.BranchId, ordersOnly: false, cancellationToken);
        if (printers.Count == 0)
            return Result.Failure(new Error("Printing.NoPrinter", "Nenhuma impressora de contas configurada."));

        var items = order.Items.Where(i => i.IsActive && i.OrderItemStatusId != OrderItemStatusIds.Cancelado).ToList();
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await context.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);
        var company = await context.Companies.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        var billItems = items.Select(i => new TicketFormatter.BillItem(
            i.Quantity,
            products.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? $"Produto {i.ProductId}",
            i.UnitPrice, i.TotalAmount)).ToList();

        var content = TicketFormatter.Bill(
            company?.TradeName ?? "SyncBar",
            await OriginLabelAsync(order, cancellationToken),
            ExtractCustomerName(order.Notes),
            order.Id, DateTime.Now, billItems,
            order.SubtotalAmount, order.DiscountAmount, order.ServiceFeeAmount, order.TotalAmount);

        return await SendToAllAsync(printers, content, cancellationToken);
    }

    public async Task<Result> PrintPaymentReceiptAsync(long saleId, CancellationToken cancellationToken = default)
    {
        var sale = await context.Sales.AsNoTracking()
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == saleId && s.IsActive, cancellationToken);
        if (sale is null)
            return Result.Failure(new Error("Sale.NotFound", "Sale not found."));

        var settings = await GetSettingsAsync(sale.BranchId, cancellationToken);
        if (settings is { PrintBillsEnabled: false })
            return Result.Failure(new Error("Printing.Disabled", "A impressão de contas está desligada."));

        var printers = await GetPrintersAsync(sale.BranchId, ordersOnly: false, cancellationToken);
        if (printers.Count == 0)
            return Result.Failure(new Error("Printing.NoPrinter", "Nenhuma impressora de contas configurada."));

        var order = await context.CustomerOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == sale.CustomerOrderId, cancellationToken);
        var methods = await context.PaymentMethods.AsNoTracking().ToListAsync(cancellationToken);
        var company = await context.Companies.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var operatorName = (await context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == sale.EmployeeId, cancellationToken))?.Name;

        var payments = sale.Payments.Where(p => p.IsActive)
            .Select(p => new TicketFormatter.ReceiptPayment(
                methods.FirstOrDefault(m => m.Id == p.PaymentMethodId)?.Name ?? $"Forma {p.PaymentMethodId}",
                p.Amount, p.ChangeAmount, p.AuthorizationCode))
            .ToList();

        var previouslyPaid = await context.OrderPartialPayments.AsNoTracking()
            .Where(p => p.CustomerOrderId == sale.CustomerOrderId && p.IsActive)
            .SumAsync(p => p.Amount, cancellationToken);

        var content = TicketFormatter.PaymentReceipt(
            company?.TradeName ?? "SyncBar",
            order is null ? "" : await OriginLabelAsync(order, cancellationToken),
            ExtractCustomerName(order?.Notes),
            sale.SaleNumber, sale.CustomerOrderId, DateTime.Now,
            sale.TotalAmount, payments, operatorName, previouslyPaid);

        return await SendToAllAsync(printers, content, cancellationToken);
    }

    public async Task<Result> PrintPartialReceiptAsync(long partialPaymentId, CancellationToken cancellationToken = default)
    {
        var partial = await context.OrderPartialPayments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == partialPaymentId && p.IsActive, cancellationToken);
        if (partial is null)
            return Result.Failure(new Error("PartialPayment.NotFound", "Partial payment not found."));

        var order = await context.CustomerOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == partial.CustomerOrderId, cancellationToken);
        if (order is null)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        var settings = await GetSettingsAsync(order.BranchId, cancellationToken);
        if (settings is { PrintBillsEnabled: false })
            return Result.Failure(new Error("Printing.Disabled", "A impressão de contas está desligada."));

        var printers = await GetPrintersAsync(order.BranchId, ordersOnly: false, cancellationToken);
        if (printers.Count == 0)
            return Result.Failure(new Error("Printing.NoPrinter", "Nenhuma impressora de contas configurada."));

        var allPartials = await context.OrderPartialPayments.AsNoTracking()
            .Where(p => p.CustomerOrderId == order.Id && p.IsActive)
            .ToListAsync(cancellationToken);
        var method = await context.PaymentMethods.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == partial.PaymentMethodId, cancellationToken);
        var company = await context.Companies.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var operatorName = (await context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == partial.EmployeeId, cancellationToken))?.Name;

        var content = TicketFormatter.PartialPaymentReceipt(
            company?.TradeName ?? "SyncBar",
            await OriginLabelAsync(order, cancellationToken),
            partial.PayerName,
            order.Id, DateTime.Now,
            method?.Name ?? $"Forma {partial.PaymentMethodId}",
            partial.Amount, partial.AuthorizationCode,
            order.TotalAmount,
            order.TotalAmount - allPartials.Sum(p => p.Amount),
            operatorName);

        return await SendToAllAsync(printers, content, cancellationToken);
    }

    public async Task<Result> PrintCashClosingAsync(long cashSessionId, CancellationToken cancellationToken = default)
    {
        var session = await context.CashSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == cashSessionId, cancellationToken);
        if (session is null)
            return Result.Failure(new Error("CashSession.NotFound", "Cash session not found."));

        var register = await context.CashRegisters.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == session.CashRegisterId, cancellationToken);
        var branchId = register?.BranchId ?? 0;

        var printers = await GetPrintersAsync(branchId, ordersOnly: false, cancellationToken);
        if (printers.Count == 0)
            return Result.Failure(new Error("Printing.NoPrinter", "Nenhuma impressora de contas configurada."));

        var sales = await context.Sales.AsNoTracking()
            .Include(s => s.Payments)
            .Where(s => s.CashSessionId == session.Id && s.IsActive)
            .ToListAsync(cancellationToken);
        var movements = await context.CashMovements.AsNoTracking()
            .Where(m => m.CashSessionId == session.Id && m.IsActive)
            .ToListAsync(cancellationToken);
        var methods = await context.PaymentMethods.AsNoTracking().ToListAsync(cancellationToken);
        var employees = await context.Employees.AsNoTracking()
            .Where(e => e.BranchId == branchId).ToListAsync(cancellationToken);

        var payments = sales.SelectMany(s => s.Payments).Where(p => p.IsActive)
            .GroupBy(p => p.PaymentMethodId)
            .OrderBy(g => g.Key)
            .Select(g => new TicketFormatter.PaymentLine(
                methods.FirstOrDefault(m => m.Id == g.Key)?.Name ?? $"Forma {g.Key}",
                g.Sum(p => p.Amount - (p.ChangeAmount ?? 0))))
            .ToList();

        var content = TicketFormatter.CashClosing(
            register?.Name ?? "Caixa",
            session.Id, session.OpenedAt, session.ClosedAt,
            employees.FirstOrDefault(e => e.Id == session.OpenedByEmployeeId)?.Name,
            session.ClosedByEmployeeId.HasValue
                ? employees.FirstOrDefault(e => e.Id == session.ClosedByEmployeeId.Value)?.Name
                : null,
            session.OpeningAmount,
            sales.Count, sales.Sum(s => s.TotalAmount), payments,
            movements.Where(m => m.CashMovementTypeId == CashMovementTypeIds.Suprimento).Sum(m => m.Amount),
            movements.Where(m => m.CashMovementTypeId == CashMovementTypeIds.Sangria).Sum(m => m.Amount),
            movements.Where(m => m.CashMovementTypeId == CashMovementTypeIds.Despesa).Sum(m => m.Amount),
            session.ExpectedAmount ?? 0, session.ClosingAmount ?? 0, session.DifferenceAmount ?? 0);

        return await SendToAllAsync(printers, content, cancellationToken);
    }

    public async Task<Result> PrintTestAsync(long printerId, CancellationToken cancellationToken = default)
    {
        var printer = await context.Printers.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == printerId && p.IsActive, cancellationToken);
        if (printer is null)
            return Result.Failure(new Error("Printer.NotFound", "Printer not found."));

        var content = $"SYNCBAR - TESTE DE IMPRESSAO\n{printer.Name}\n{DateTime.Now:dd/MM/yyyy HH:mm:ss}\nOK!";
        return await SendToAllAsync([printer], content, cancellationToken);
    }

    // ------------------------------------------------------------------

    private async Task<PrinterSetting?> GetSettingsAsync(long branchId, CancellationToken ct)
        => await context.PrinterSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.IsActive, ct);

    private async Task<IReadOnlyCollection<Printer>> GetPrintersAsync(long branchId, bool ordersOnly, CancellationToken ct)
        => await context.Printers.AsNoTracking()
            .Where(p => p.BranchId == branchId && p.IsActive && (ordersOnly ? p.PrintsOrders : p.PrintsBills))
            .ToListAsync(ct);

    private async Task<string> OriginLabelAsync(CustomerOrder order, CancellationToken ct)
    {
        if (order.DiningTableId.HasValue)
        {
            var table = await context.DiningTables.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == order.DiningTableId.Value, ct);
            return $"MESA {table?.Number}";
        }
        var comanda = await context.Comandas.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == order.ComandaId!.Value, ct);
        return $"COMANDA {comanda?.Code}";
    }

    // O nome do cliente da comanda vai nas notas do pedido como "Cliente: X".
    internal static string? ExtractCustomerName(string? orderNotes)
    {
        if (string.IsNullOrWhiteSpace(orderNotes)) return null;
        const string prefix = "Cliente:";
        var index = orderNotes.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? null : orderNotes[(index + prefix.Length)..].Trim();
    }

    private async Task<Result> SendToAllAsync(IReadOnlyCollection<Printer> printers, string content, CancellationToken ct)
    {
        var payload = EscPos.Build(content);
        var errors = new List<string>();
        var printed = 0;

        foreach (var printer in printers)
        {
            var transport = transports.FirstOrDefault(t => t.CanHandle(printer));
            if (transport is null)
            {
                errors.Add($"{printer.Name}: tipo de conexão não suportado.");
                continue;
            }
            try
            {
                await transport.SendAsync(printer, payload, ct);
                printed++;
            }
            catch (Exception ex)
            {
                errors.Add($"{printer.Name}: {ex.Message}");
            }
        }

        return printed > 0
            ? Result.Success()
            : Result.Failure(new Error("Printing.Failed", string.Join(" | ", errors)));
    }
}
