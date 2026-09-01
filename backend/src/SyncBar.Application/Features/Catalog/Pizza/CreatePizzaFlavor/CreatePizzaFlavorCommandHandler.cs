using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Pizza.CreatePizzaFlavor;

internal sealed class CreatePizzaFlavorCommandHandler(
    IPizzaFlavorRepository pizzaFlavorRepository,
    ICurrentTenantService currentTenant,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreatePizzaFlavorCommand, long>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override Task<Result<long>> Handle(CreatePizzaFlavorCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CreatePizzaFlavorCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                // Achado de review (Devin): CompanyId vem do corpo da requisição — sem checar
                // contra o tenant autenticado, um usuário com Feature:Cardapio numa empresa
                // conseguiria gravar sabor no catálogo de outra empresa (PizzaFlavor tem
                // HasQueryFilter por CompanyId direto no AppDbContext, então a leitura já isola
                // por tenant, mas a escrita não checava nada). ICurrentTenantService lê o claim
                // "companyId" do JWT — mesma fonte que os filtros de leitura usam. Mesma correção
                // aplicada em CreateComplementItemCommandHandler, que tinha a mesma lacuna.
                if (currentTenant.CompanyId is not { } tenantCompanyId || tenantCompanyId != request.CompanyId)
                    return Result.Failure<long>(new Error("Tenant.Forbidden", "CompanyId does not match the authenticated tenant."));

                var flavor = PizzaFlavor.Create(request.CompanyId, request.Name, request.Description);
                if (flavor.IsFailure)
                    return Result.Failure<long>(flavor.Error);

                await pizzaFlavorRepository.AddAsync(flavor.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Sem TriggerCompanySync aqui: um sabor sozinho (sem preço em nenhuma
                // PizzaConfiguration) não afeta o catálogo do Ifood ainda — mesmo critério de
                // CreateComplementItemCommandHandler.
                return Result.Success(flavor.Value.Id);
            });
}
