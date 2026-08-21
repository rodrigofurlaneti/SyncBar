using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog;

/// <summary>
/// Núcleo da sincronização de cardápio ("fluxo essencial", fase 3) — endpoints/formatos
/// confirmados contra a doc oficial do módulo Catalog em 2026-08-19 (ver comentário completo em
/// IIFoodCatalogClient). Resync completo por empresa a cada disparo: cria categorias que
/// faltarem, cria/atualiza (PUT /items) todo produto ativo, sincroniza estoque de produtos com
/// controle de estoque, e pausa (PATCH /items/status) itens cujo Product saiu da lista de ativos
/// (foi desativado) — essa última parte é o que permite ao DeactivateProductCommandHandler
/// disparar o MESMO comando dos outros handlers em vez de precisar de um comando dedicado.
///
/// Fase 6a (extensão): produtos com ProductComplementGroup vinculado agora enviam
/// optionGroups/options reais no PUT /items — get-or-create de IFoodComplementGroupMapping/
/// IFoodComplementMapping por filial (mesmo padrão já usado para categoria/produto acima). Ver
/// ressalva sobre nomes de campo em IIFoodCatalogClient.
/// </summary>
internal sealed class SyncIFoodCatalogCommandHandler : BaseCommandHandler<SyncIFoodCatalogCommand, IFoodCatalogSyncSummary>
{
    private readonly IIFoodMerchantMappingRepository _merchantMappingRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIFoodCategoryMappingRepository _categoryMappingRepository;
    private readonly IIFoodProductMappingRepository _productMappingRepository;
    private readonly IProductComplementGroupRepository _productComplementGroupRepository;
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IComplementItemRepository _complementItemRepository;
    private readonly IIFoodComplementGroupMappingRepository _complementGroupMappingRepository;
    private readonly IIFoodComplementMappingRepository _complementMappingRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IIFoodCatalogClient _catalogClient;
    private readonly IUnitOfWork _unitOfWork;

    public SyncIFoodCatalogCommandHandler(
        IIFoodMerchantMappingRepository merchantMappingRepository,
        IIFoodTokenProvider tokenProvider,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IIFoodCategoryMappingRepository categoryMappingRepository,
        IIFoodProductMappingRepository productMappingRepository,
        IProductComplementGroupRepository productComplementGroupRepository,
        IComplementGroupRepository complementGroupRepository,
        IComplementItemRepository complementItemRepository,
        IIFoodComplementGroupMappingRepository complementGroupMappingRepository,
        IIFoodComplementMappingRepository complementMappingRepository,
        IStockItemRepository stockItemRepository,
        IIFoodCatalogClient catalogClient,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _merchantMappingRepository = merchantMappingRepository;
        _tokenProvider = tokenProvider;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _categoryMappingRepository = categoryMappingRepository;
        _productMappingRepository = productMappingRepository;
        _productComplementGroupRepository = productComplementGroupRepository;
        _complementGroupRepository = complementGroupRepository;
        _complementItemRepository = complementItemRepository;
        _complementGroupMappingRepository = complementGroupMappingRepository;
        _complementMappingRepository = complementMappingRepository;
        _stockItemRepository = stockItemRepository;
        _catalogClient = catalogClient;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<IFoodCatalogSyncSummary>> Handle(SyncIFoodCatalogCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(SyncIFoodCatalogCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                var accessToken = await _tokenProvider.GetAccessTokenAsync(request.CompanyId, cancellationToken);
                if (accessToken is null)
                    return Result.Success(new IFoodCatalogSyncSummary(true, 0, 0, 0, 0, 0));

                var merchantMappings = await _merchantMappingRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var enabledBranches = merchantMappings.Values.Where(m => !string.IsNullOrWhiteSpace(m.MerchantId)).ToList();
                if (enabledBranches.Count == 0)
                    return Result.Success(new IFoodCatalogSyncSummary(true, 0, 0, 0, 0, 0));

                var categories = await _categoryRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var products = await _productRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
                var activeProductIds = products.Select(p => p.Id).ToHashSet();

                // Fase 6a (extensão): complementos vinculados aos produtos ativos, resolvidos em
                // lote UMA vez por empresa (não por filial) — os mapeamentos iFood (grupo/opção)
                // é que são por filial, ver dentro do loop de branches abaixo.
                var productComplementLinks = await _productComplementGroupRepository.GetByProductsAsync(
                    products.Select(p => p.Id).ToList(), cancellationToken);
                var complementGroupIds = productComplementLinks.Select(l => l.ComplementGroupId).Distinct().ToList();
                var complementGroups = complementGroupIds.Count > 0
                    ? await _complementGroupRepository.GetByIdsAsync(complementGroupIds, cancellationToken)
                    : [];
                var complementGroupsById = complementGroups.ToDictionary(g => g.Id);
                var complementItemIds = complementGroups.SelectMany(g => g.Complements).Select(c => c.ComplementItemId).Distinct().ToList();
                var complementItems = complementItemIds.Count > 0
                    ? await _complementItemRepository.GetByIdsAsync(complementItemIds, cancellationToken)
                    : [];
                var complementItemNames = complementItems.ToDictionary(i => i.Id, i => i.Name);
                var complementLinksByProduct = productComplementLinks
                    .Where(l => complementGroupsById.ContainsKey(l.ComplementGroupId))
                    .GroupBy(l => l.ProductId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(l => l.DisplayOrder).ToList());

                var branchesSynced = 0;
                var categoriesCreated = 0;
                var productsSynced = 0;
                var productsPaused = 0;
                var errors = 0;

                foreach (var mapping in enabledBranches)
                {
                    var branchId = mapping.BranchId;
                    var merchantId = mapping.MerchantId!;

                    // Categorias: get-or-create por filial — o catálogo do iFood é por merchant,
                    // então a mesma Category vira uma categoria diferente em cada loja.
                    var ifoodCategoryIdByCategory = new Dictionary<long, string>();
                    foreach (var category in categories)
                    {
                        var existingCategoryMapping = await _categoryMappingRepository.GetByCategoryAndBranchAsync(category.Id, branchId, cancellationToken);
                        if (existingCategoryMapping is not null)
                        {
                            ifoodCategoryIdByCategory[category.Id] = existingCategoryMapping.IFoodCategoryId;
                            continue;
                        }

                        var createdCategory = await _catalogClient.CreateCategoryAsync(accessToken, merchantId, category.Name, cancellationToken);
                        if (!createdCategory.Success || createdCategory.IFoodCategoryId is null)
                        {
                            errors++;
                            continue;
                        }

                        var newCategoryMapping = IFoodCategoryMapping.Create(category.Id, branchId, createdCategory.IFoodCategoryId);
                        if (newCategoryMapping.IsFailure)
                        {
                            errors++;
                            continue;
                        }

                        await _categoryMappingRepository.AddAsync(newCategoryMapping.Value, cancellationToken);
                        await _unitOfWork.CommitAsync(cancellationToken);
                        ifoodCategoryIdByCategory[category.Id] = createdCategory.IFoodCategoryId;
                        categoriesCreated++;
                    }

                    // Produtos ativos: get-or-create mapeamento (ids UUID estáveis) + PUT /items.
                    foreach (var product in products)
                    {
                        if (!ifoodCategoryIdByCategory.TryGetValue(product.CategoryId, out var ifoodCategoryId))
                        {
                            errors++; // categoria desse produto falhou ao criar acima
                            continue;
                        }

                        var productMapping = await _productMappingRepository.GetByProductAndBranchAsync(product.Id, branchId, cancellationToken);
                        if (productMapping is null)
                        {
                            var createdMapping = IFoodProductMapping.Create(product.Id, branchId);
                            if (createdMapping.IsFailure)
                            {
                                errors++;
                                continue;
                            }

                            await _productMappingRepository.AddAsync(createdMapping.Value, cancellationToken);
                            await _unitOfWork.CommitAsync(cancellationToken);
                            productMapping = createdMapping.Value;
                        }

                        // Fase 6a (extensão): monta optionGroups/options reais quando o produto
                        // tem grupos de complemento vinculados — get-or-create dos mapeamentos
                        // iFood (grupo/opção) por filial, mesmo padrão do mapeamento de produto acima.
                        var optionGroups = new List<IFoodUpsertItemOptionGroup>();
                        if (complementLinksByProduct.TryGetValue(product.Id, out var productComplementLinksForProduct))
                        {
                            foreach (var link in productComplementLinksForProduct)
                            {
                                if (!complementGroupsById.TryGetValue(link.ComplementGroupId, out var group) || !group.IsActive)
                                    continue;

                                var groupMapping = await _complementGroupMappingRepository.GetByComplementGroupAndBranchAsync(group.Id, branchId, cancellationToken);
                                if (groupMapping is null)
                                {
                                    var createdGroupMapping = IFoodComplementGroupMapping.Create(group.Id, branchId);
                                    if (createdGroupMapping.IsFailure)
                                    {
                                        errors++;
                                        continue;
                                    }

                                    await _complementGroupMappingRepository.AddAsync(createdGroupMapping.Value, cancellationToken);
                                    await _unitOfWork.CommitAsync(cancellationToken);
                                    groupMapping = createdGroupMapping.Value;
                                }

                                var options = new List<IFoodUpsertItemOption>();
                                foreach (var complement in group.Complements.Where(c => c.IsActive))
                                {
                                    var complementMapping = await _complementMappingRepository.GetByComplementAndBranchAsync(complement.Id, branchId, cancellationToken);
                                    if (complementMapping is null)
                                    {
                                        var createdComplementMapping = IFoodComplementMapping.Create(complement.Id, branchId);
                                        if (createdComplementMapping.IsFailure)
                                        {
                                            errors++;
                                            continue;
                                        }

                                        await _complementMappingRepository.AddAsync(createdComplementMapping.Value, cancellationToken);
                                        await _unitOfWork.CommitAsync(cancellationToken);
                                        complementMapping = createdComplementMapping.Value;
                                    }

                                    options.Add(new IFoodUpsertItemOption(
                                        complementMapping.IFoodOptionId,
                                        complementMapping.IFoodProductId,
                                        complementItemNames.TryGetValue(complement.ComplementItemId, out var itemName) ? itemName : "?",
                                        complement.ExtraPrice,
                                        true));
                                }

                                if (options.Count > 0)
                                {
                                    optionGroups.Add(new IFoodUpsertItemOptionGroup(
                                        groupMapping.IFoodOptionGroupId, group.Name, group.MinSelection, group.MaxSelection, options));
                                }
                            }
                        }

                        var upsertResult = await _catalogClient.UpsertItemAsync(accessToken, merchantId, new IFoodUpsertItemRequest(
                            productMapping.IFoodItemId,
                            ifoodCategoryId,
                            true,
                            product.SalePrice,
                            $"SB-{product.Id}",
                            productMapping.IFoodProductId,
                            product.Name,
                            product.Description,
                            $"SB-{product.Id}-P",
                            optionGroups), cancellationToken);

                        if (!upsertResult.Success)
                        {
                            errors++;
                            continue;
                        }

                        productsSynced++;

                        if (product.IsStockControlled)
                        {
                            var stockItem = await _stockItemRepository.GetByBranchAndProductForUpdateAsync(branchId, product.Id, cancellationToken);
                            if (stockItem is not null)
                            {
                                // iFood espera uma quantidade vendível inteira — produtos vendidos
                                // fracionados (kg, L) não têm representação exata aqui; arredonda
                                // pra baixo (nunca oferece mais do que realmente existe em estoque).
                                var quantity = (int)Math.Max(0, Math.Floor(stockItem.CurrentQuantity));
                                var inventoryResult = await _catalogClient.SetInventoryAsync(accessToken, merchantId, productMapping.IFoodProductId, quantity, cancellationToken);
                                if (!inventoryResult.Success) errors++;
                            }
                        }
                    }

                    // Produtos que saíram da lista de ativos (foram desativados) mas ainda têm
                    // item publicado no iFood — pausa em vez de deixar vendendo o que sumiu do
                    // cardápio. É isso que permite DeactivateProductCommandHandler disparar o
                    // mesmo SyncIFoodCatalogCommand dos outros handlers, sem comando dedicado.
                    var existingBranchMappings = await _productMappingRepository.GetByBranchAsync(branchId, cancellationToken);
                    foreach (var staleMapping in existingBranchMappings.Where(m => !activeProductIds.Contains(m.ProductId)))
                    {
                        var pauseResult = await _catalogClient.SetItemStatusAsync(accessToken, merchantId, staleMapping.IFoodItemId, false, cancellationToken);
                        if (pauseResult.Success) productsPaused++;
                        else errors++;
                    }

                    branchesSynced++;
                }

                return Result.Success(new IFoodCatalogSyncSummary(false, branchesSynced, categoriesCreated, productsSynced, productsPaused, errors));
            });
}
