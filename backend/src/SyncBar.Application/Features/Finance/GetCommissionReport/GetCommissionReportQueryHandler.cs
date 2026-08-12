using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.GetCommissionReport;

// Comissão do garçom/vendedor: % configurado no cadastro do funcionário (Employee.CommissionPercent)
// aplicado sobre o faturamento dos pedidos que ele abriu (mesmo critério de atribuição do
// relatório de vendas por funcionário em GetSalesReport — quem atendeu, não quem fechou o caixa).
internal sealed class GetCommissionReportQueryHandler(
    ISaleRepository saleRepository,
    ICustomerOrderRepository orderRepository,
    IEmployeeRepository employeeRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetCommissionReportQuery, IReadOnlyCollection<EmployeeCommissionResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<EmployeeCommissionResponse>>> Handle(
        GetCommissionReportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetCommissionReportQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está executando a ação, preencha:
                // userIdBox.Value = request.UserId;

                var sales = await saleRepository.GetByBranchAndPeriodAsync(request.BranchId, request.From, request.To, cancellationToken);
                var orders = await orderRepository.GetByIdsAsync(
                    sales.Select(s => s.CustomerOrderId).Distinct().ToList(), cancellationToken);
                var employees = await employeeRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                IReadOnlyCollection<EmployeeCommissionResponse> response = sales
                    .Join(orders, s => s.CustomerOrderId, o => o.Id, (s, o) => new { s, o })
                    .GroupBy(x => x.o.EmployeeId)
                    .Select(g =>
                    {
                        var employee = employees.FirstOrDefault(e => e.Id == g.Key);
                        var revenue = g.Sum(x => x.s.TotalAmount);
                        var percent = employee?.CommissionPercent;
                        var commission = percent.HasValue ? Math.Round(revenue * percent.Value / 100, 2) : 0;

                        return new EmployeeCommissionResponse(
                            g.Key, employee?.Name ?? $"Funcionário {g.Key}", percent, g.Count(), revenue, commission);
                    })
                    .OrderByDescending(e => e.CommissionAmount)
                    .ToList();

                return Result.Success(response);
            });
    }
}