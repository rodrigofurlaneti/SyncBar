using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.CreateComplementItem;

internal sealed class CreateComplementItemCommandHandler : BaseCommandHandler<CreateComplementItemCommand, long>
{
    private readonly IComplementItemRepository _complementItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IUnitOfWork _unitOfWork;

    public CreateComplementItemCommandHandler(
        IComplementItemRepository complementItemRepository,
        IProductRepository productRepository,
        ICurrentTenantService currentTenant,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _complementItemRepository = complementItemRepository;
        _productRepository = productRepository;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<long>> Handle(CreateComplementItemCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CreateComplementItemCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                // Achado de review (mesma lacuna encontrada e corrigida em
                // CreatePizzaFlavorCommandHandler, Fase 17): CompanyId vem do corpo da requisição
                // sem checar contra o tenant autenticado. Fechado junto nesta rodada por serem o
                // mesmo bug.
                if (_currentTenant.CompanyId is not { } tenantCompanyId || tenantCompanyId != request.CompanyId)
                    return Result.Failure<long>(new Error("Tenant.Forbidden", "CompanyId does not match the authenticated tenant."));

                // Fase 18 (combos) — se um LinkedProductId foi informado, confere que o Product
                // existe e pertence à mesma empresa antes de vincular (mesma responsabilidade já
                // dividida em LinkProductComplementGroupCommandHandler: o domínio não valida isso).
                if (request.LinkedProductId is { } linkedProductId)
                {
                    var linkedProduct = await _productRepository.GetByIdAsync(linkedProductId, cancellationToken);
                    if (linkedProduct is null || !linkedProduct.IsActive || linkedProduct.CompanyId != request.CompanyId)
                        return Result.Failure<long>(new Error("Product.NotFound", "Linked product not found for this company."));
                }

                var complementItem = ComplementItem.Create(request.CompanyId, request.Name, request.LinkedProductId);
                if (complementItem.IsFailure)
                    return Result.Failure<long>(complementItem.Error);

                await _complementItemRepository.AddAsync(complementItem.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Não dispara TriggerCompanySync aqui: um ComplementItem sozinho (fora de um
                // ComplementGroup) não afeta o catálogo do iFood — só quando vira um Complement
                // dentro de um grupo vinculado a um produto (ver AddComplement/LinkProductComplementGroup).
                return Result.Success(complementItem.Value.Id);
            });
}
