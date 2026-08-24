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
            _ => RunSyncAsync(request, cancellationToken));

    // Sequência linear do fluxo essencial (ver comentário de classe): resolve token, resolve
    // filiais habilitadas, carrega o contexto de sincronização UMA vez por empresa, sincroniza
    // cada filial e agrega os totais no resumo final. Cada etapa foi extraída em método próprio
    // (Sonar: Cognitive Complexity) — o objetivo deste método é permanecer uma leitura direta do
    // fluxo, sem lógica condicional própria além dos dois retornos antecipados (early exits).
    private async Task<Result<IFoodCatalogSyncSummary>> RunSyncAsync(SyncIFoodCatalogCommand request, CancellationToken cancellationToken)
    {
        var accessToken = await _tokenProvider.GetAccessTokenAsync(request.CompanyId, cancellationToken);
        if (accessToken is null)
            return Result.Success(EmptySummary());

        var enabledBranches = await GetEnabledBranchesAsync(request.CompanyId, cancellationToken);
        if (enabledBranches.Count == 0)
            return Result.Success(EmptySummary());

        var context = await LoadCatalogContextAsync(request.CompanyId, cancellationToken);

        var totals = new SyncTotals();
        foreach (var mapping in enabledBranches)
            await SyncBranchAsync(mapping, accessToken, context, totals, cancellationToken);

        return Result.Success(new IFoodCatalogSyncSummary(
            false, totals.BranchesSynced, totals.CategoriesCreated, totals.ProductsSynced, totals.ProductsPaused, totals.Errors));
    }

    private static IFoodCatalogSyncSummary EmptySummary() => new(true, 0, 0, 0, 0, 0);

    private async Task<List<IFoodMerchantMapping>> GetEnabledBranchesAsync(long companyId, CancellationToken cancellationToken)
    {
        var merchantMappings = await _merchantMappingRepository.GetByCompanyAsync(companyId, cancellationToken);
        return merchantMappings.Values.Where(m => !string.IsNullOrWhiteSpace(m.MerchantId)).ToList();
    }

    // Carrega tudo que é resolvido UMA vez por empresa (não por filial): categorias, produtos
    // ativos e — Fase 6a (extensão) — os complementos vinculados aos produtos ativos. Os
    // mapeamentos iFood (categoria/produto/grupo/opção) continuam sendo resolvidos por filial,
    // dentro do loop de branches em SyncBranchAsync.
    private async Task<CatalogSyncContext> LoadCatalogContextAsync(long companyId, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetByCompanyAsync(companyId, cancellationToken);
        var products = await _productRepository.GetByCompanyAsync(companyId, cancellationToken);
        var activeProductIds = products.Select(p => p.Id).ToHashSet();

        var (complementGroupsById, complementItemNames, complementGroupIdsByProduct) =
            await LoadComplementContextAsync(products, cancellationToken);

        return new CatalogSyncContext
        {
            Categories = categories,
            Products = products,
            ActiveProductIds = activeProductIds,
            ComplementGroupsById = complementGroupsById,
            ComplementItemNames = complementItemNames,
            ComplementGroupIdsByProduct = complementGroupIdsByProduct
        };
    }

    private async Task<(Dictionary<long, ComplementGroup> GroupsById, Dictionary<long, string> ItemNames, Dictionary<long, List<long>> GroupIdsByProduct)>
        LoadComplementContextAsync(IReadOnlyCollection<Product> products, CancellationToken cancellationToken)
    {
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

        // Guarda só o ComplementGroupId (ordenado por DisplayOrder) por produto — é tudo que o
        // resto do fluxo precisa; simplifica o tipo carregado no contexto de sincronização.
        var complementGroupIdsByProduct = productComplementLinks
            .Where(l => complementGroupsById.ContainsKey(l.ComplementGroupId))
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderBy(l => l.DisplayOrder).Select(l => l.ComplementGroupId).ToList());

        return (complementGroupsById, complementItemNames, complementGroupIdsByProduct);
    }

    // Sincroniza uma filial: categorias → produtos (com seus optionGroups) → pausa de produtos
    // desativados. Os totais de cada etapa são acumulados no acumulador compartilhado `totals`.
    private async Task SyncBranchAsync(
        IFoodMerchantMapping mapping,
        string accessToken,
        CatalogSyncContext context,
        SyncTotals totals,
        CancellationToken cancellationToken)
    {
        var branchId = mapping.BranchId;
        var merchantId = mapping.MerchantId!;

        var categoryResult = await SyncCategoriesForBranchAsync(context.Categories, branchId, merchantId, accessToken, cancellationToken);
        totals.CategoriesCreated += categoryResult.Created;
        totals.Errors += categoryResult.Errors;

        var productResult = await SyncProductsForBranchAsync(
            context.Products, categoryResult.IFoodCategoryIdByCategory, branchId, merchantId, accessToken, context, cancellationToken);
        totals.ProductsSynced += productResult.Synced;
        totals.Errors += productResult.Errors;

        var pauseResult = await PauseStaleProductsAsync(branchId, merchantId, accessToken, context.ActiveProductIds, cancellationToken);
        totals.ProductsPaused += pauseResult.Paused;
        totals.Errors += pauseResult.Errors;

        totals.BranchesSynced++;
    }

    // Categorias: get-or-create por filial — o catálogo do iFood é por merchant, então a mesma
    // Category vira uma categoria diferente em cada loja.
    private async Task<(Dictionary<long, string> IFoodCategoryIdByCategory, int Created, int Errors)> SyncCategoriesForBranchAsync(
        IReadOnlyCollection<Category> categories,
        long branchId,
        string merchantId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var ifoodCategoryIdByCategory = new Dictionary<long, string>();
        var catalogState = new CatalogIdResolutionState();
        var created = 0;
        var errors = 0;

        foreach (var category in categories)
        {
            var outcome = await SyncSingleCategoryAsync(category, branchId, merchantId, accessToken, catalogState, cancellationToken);
            if (outcome.IFoodCategoryId is not null)
                ifoodCategoryIdByCategory[category.Id] = outcome.IFoodCategoryId;
            if (outcome.Created)
                created++;
            if (outcome.HasError)
                errors++;
        }

        return (ifoodCategoryIdByCategory, created, errors);
    }

    private async Task<CategorySyncOutcome> SyncSingleCategoryAsync(
        Category category,
        long branchId,
        string merchantId,
        string accessToken,
        CatalogIdResolutionState catalogState,
        CancellationToken cancellationToken)
    {
        var existingCategoryMapping = await _categoryMappingRepository.GetByCategoryAndBranchAsync(category.Id, branchId, cancellationToken);
        if (existingCategoryMapping is not null)
            return new CategorySyncOutcome(existingCategoryMapping.IFoodCategoryId, false, false);

        if (catalogState.CatalogId is null && !catalogState.ResolutionFailed)
            await ResolveCatalogIdAsync(catalogState, accessToken, merchantId, cancellationToken);

        if (catalogState.CatalogId is null)
            return new CategorySyncOutcome(null, false, true);

        var catalogId = catalogState.CatalogId;
        var createdCategory = await _catalogClient.CreateCategoryAsync(accessToken, merchantId, catalogId, category.Name, cancellationToken);
        if (!createdCategory.Success || createdCategory.IFoodCategoryId is null)
            return new CategorySyncOutcome(null, false, true);

        var newCategoryMapping = IFoodCategoryMapping.Create(category.Id, branchId, createdCategory.IFoodCategoryId);
        if (newCategoryMapping.IsFailure)
            return new CategorySyncOutcome(null, false, true);

        await _categoryMappingRepository.AddAsync(newCategoryMapping.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new CategorySyncOutcome(createdCategory.IFoodCategoryId, true, false);
    }

    // Fase 10 — correção de bug presente desde a Fase 3: criar categoria SEMPRE exige um
    // catalogId (merchants/{id}/catalogs/{catalogId}/categories) — o código antigo chamava
    // merchants/{id}/categories direto, path que não existe na doc oficial. Resolve o catalogId
    // da filial (via GetCatalogsAsync, pega o primeiro catálogo retornado — sem confirmação
    // oficial de qual escolher quando há mais de um; ver IFoodCatalogResolution) antes de criar
    // qualquer categoria nova, uma única vez por filial (o resultado — sucesso ou falha — fica
    // memoizado em `catalogState` para as categorias seguintes). Categorias já mapeadas
    // continuam funcionando sem isso.
    private async Task ResolveCatalogIdAsync(CatalogIdResolutionState catalogState, string accessToken, string merchantId, CancellationToken cancellationToken)
    {
        var resolvedCatalogId = await IFoodCatalogResolution.ResolveDefaultCatalogIdAsync(accessToken, merchantId, _catalogClient, cancellationToken);
        if (resolvedCatalogId.IsFailure)
            catalogState.ResolutionFailed = true;
        else
            catalogState.CatalogId = resolvedCatalogId.Value;
    }

    // Produtos ativos: get-or-create mapeamento (ids UUID estáveis) + PUT /items.
    private async Task<(int Synced, int Errors)> SyncProductsForBranchAsync(
        IReadOnlyCollection<Product> products,
        Dictionary<long, string> ifoodCategoryIdByCategory,
        long branchId,
        string merchantId,
        string accessToken,
        CatalogSyncContext context,
        CancellationToken cancellationToken)
    {
        var synced = 0;
        var errors = 0;

        foreach (var product in products)
        {
            var outcome = await SyncSingleProductAsync(product, ifoodCategoryIdByCategory, branchId, merchantId, accessToken, context, cancellationToken);
            if (outcome.Synced)
                synced++;
            errors += outcome.Errors;
        }

        return (synced, errors);
    }

    private async Task<ProductSyncOutcome> SyncSingleProductAsync(
        Product product,
        Dictionary<long, string> ifoodCategoryIdByCategory,
        long branchId,
        string merchantId,
        string accessToken,
        CatalogSyncContext context,
        CancellationToken cancellationToken)
    {
        if (!ifoodCategoryIdByCategory.TryGetValue(product.CategoryId, out var ifoodCategoryId))
            return new ProductSyncOutcome(false, 1); // categoria desse produto falhou ao criar acima

        var (productMapping, mappingFailed) = await GetOrCreateProductMappingAsync(product, branchId, cancellationToken);
        if (productMapping is null)
            return new ProductSyncOutcome(false, mappingFailed ? 1 : 0);

        var (optionGroups, optionGroupErrors) = await BuildOptionGroupsAsync(product, branchId, context, cancellationToken);

        var upsertSuccess = await UpsertProductItemAsync(product, ifoodCategoryId, productMapping, optionGroups, merchantId, accessToken, cancellationToken);
        if (!upsertSuccess)
            return new ProductSyncOutcome(false, optionGroupErrors + 1);

        var stockErrors = product.IsStockControlled
            ? await SyncProductInventoryAsync(product, productMapping, branchId, merchantId, accessToken, cancellationToken)
            : 0;

        return new ProductSyncOutcome(true, optionGroupErrors + stockErrors);
    }

    private async Task<(IFoodProductMapping? Mapping, bool Failed)> GetOrCreateProductMappingAsync(
        Product product, long branchId, CancellationToken cancellationToken)
    {
        var productMapping = await _productMappingRepository.GetByProductAndBranchAsync(product.Id, branchId, cancellationToken);
        if (productMapping is not null)
            return (productMapping, false);

        var createdMapping = IFoodProductMapping.Create(product.Id, branchId);
        if (createdMapping.IsFailure)
            return (null, true);

        await _productMappingRepository.AddAsync(createdMapping.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return (createdMapping.Value, false);
    }

    // Fase 6a (extensão): monta optionGroups/options reais quando o produto tem grupos de
    // complemento vinculados — get-or-create dos mapeamentos iFood (grupo/opção) por filial,
    // mesmo padrão do mapeamento de produto acima.
    private async Task<(List<IFoodUpsertItemOptionGroup> OptionGroups, int Errors)> BuildOptionGroupsAsync(
        Product product, long branchId, CatalogSyncContext context, CancellationToken cancellationToken)
    {
        var optionGroups = new List<IFoodUpsertItemOptionGroup>();
        var errors = 0;

        if (!context.ComplementGroupIdsByProduct.TryGetValue(product.Id, out var groupIds))
            return (optionGroups, errors);

        foreach (var complementGroupId in groupIds)
        {
            if (!context.ComplementGroupsById.TryGetValue(complementGroupId, out var group) || !group.IsActive)
                continue;

            var (optionGroup, groupErrors) = await BuildOptionGroupAsync(group, branchId, context.ComplementItemNames, cancellationToken);
            errors += groupErrors;
            if (optionGroup is not null)
                optionGroups.Add(optionGroup);
        }

        return (optionGroups, errors);
    }

    private async Task<(IFoodUpsertItemOptionGroup? OptionGroup, int Errors)> BuildOptionGroupAsync(
        ComplementGroup group, long branchId, Dictionary<long, string> complementItemNames, CancellationToken cancellationToken)
    {
        var (groupMapping, mappingFailed) = await GetOrCreateComplementGroupMappingAsync(group.Id, branchId, cancellationToken);
        if (groupMapping is null)
            return (null, mappingFailed ? 1 : 0);

        var (options, optionErrors) = await BuildOptionsAsync(group, branchId, complementItemNames, cancellationToken);
        if (options.Count == 0)
            return (null, optionErrors);

        var optionGroup = new IFoodUpsertItemOptionGroup(
            groupMapping.IFoodOptionGroupId, group.Name, group.MinSelection, group.MaxSelection, options);

        return (optionGroup, optionErrors);
    }

    private async Task<(IFoodComplementGroupMapping? Mapping, bool Failed)> GetOrCreateComplementGroupMappingAsync(
        long complementGroupId, long branchId, CancellationToken cancellationToken)
    {
        var groupMapping = await _complementGroupMappingRepository.GetByComplementGroupAndBranchAsync(complementGroupId, branchId, cancellationToken);
        if (groupMapping is not null)
            return (groupMapping, false);

        var createdGroupMapping = IFoodComplementGroupMapping.Create(complementGroupId, branchId);
        if (createdGroupMapping.IsFailure)
            return (null, true);

        await _complementGroupMappingRepository.AddAsync(createdGroupMapping.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return (createdGroupMapping.Value, false);
    }

    private async Task<(List<IFoodUpsertItemOption> Options, int Errors)> BuildOptionsAsync(
        ComplementGroup group, long branchId, Dictionary<long, string> complementItemNames, CancellationToken cancellationToken)
    {
        var options = new List<IFoodUpsertItemOption>();
        var errors = 0;

        foreach (var complement in group.Complements.Where(c => c.IsActive))
        {
            var (complementMapping, mappingFailed) = await GetOrCreateComplementMappingAsync(complement.Id, branchId, cancellationToken);
            if (complementMapping is null)
            {
                if (mappingFailed)
                    errors++;
                continue;
            }

            var itemName = complementItemNames.TryGetValue(complement.ComplementItemId, out var name) ? name : "?";
            options.Add(new IFoodUpsertItemOption(
                complementMapping.IFoodOptionId, complementMapping.IFoodProductId, itemName, complement.ExtraPrice, true));
        }

        return (options, errors);
    }

    private async Task<(IFoodComplementMapping? Mapping, bool Failed)> GetOrCreateComplementMappingAsync(
        long complementId, long branchId, CancellationToken cancellationToken)
    {
        var complementMapping = await _complementMappingRepository.GetByComplementAndBranchAsync(complementId, branchId, cancellationToken);
        if (complementMapping is not null)
            return (complementMapping, false);

        var createdComplementMapping = IFoodComplementMapping.Create(complementId, branchId);
        if (createdComplementMapping.IsFailure)
            return (null, true);

        await _complementMappingRepository.AddAsync(createdComplementMapping.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return (createdComplementMapping.Value, false);
    }

    private async Task<bool> UpsertProductItemAsync(
        Product product,
        string ifoodCategoryId,
        IFoodProductMapping productMapping,
        List<IFoodUpsertItemOptionGroup> optionGroups,
        string merchantId,
        string accessToken,
        CancellationToken cancellationToken)
    {
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

        return upsertResult.Success;
    }

    // iFood espera uma quantidade vendível inteira — produtos vendidos fracionados (kg, L) não
    // têm representação exata aqui; arredonda pra baixo (nunca oferece mais do que realmente
    // existe em estoque). Retorna 1 quando a chamada falha (contribui para o total de erros do
    // produto), 0 caso contrário.
    private async Task<int> SyncProductInventoryAsync(
        Product product, IFoodProductMapping productMapping, long branchId, string merchantId, string accessToken, CancellationToken cancellationToken)
    {
        var stockItem = await _stockItemRepository.GetByBranchAndProductForUpdateAsync(branchId, product.Id, cancellationToken);
        if (stockItem is null)
            return 0;

        var quantity = (int)Math.Max(0, Math.Floor(stockItem.CurrentQuantity));
        var inventoryResult = await _catalogClient.SetInventoryAsync(accessToken, merchantId, productMapping.IFoodProductId, quantity, cancellationToken);

        return inventoryResult.Success ? 0 : 1;
    }

    // Produtos que saíram da lista de ativos (foram desativados) mas ainda têm item publicado no
    // iFood — pausa em vez de deixar vendendo o que sumiu do cardápio. É isso que permite
    // DeactivateProductCommandHandler disparar o mesmo SyncIFoodCatalogCommand dos outros
    // handlers, sem comando dedicado.
    private async Task<(int Paused, int Errors)> PauseStaleProductsAsync(
        long branchId, string merchantId, string accessToken, HashSet<long> activeProductIds, CancellationToken cancellationToken)
    {
        var existingBranchMappings = await _productMappingRepository.GetByBranchAsync(branchId, cancellationToken);
        var paused = 0;
        var errors = 0;

        foreach (var staleMapping in existingBranchMappings.Where(m => !activeProductIds.Contains(m.ProductId)))
        {
            var pauseResult = await _catalogClient.SetItemStatusAsync(accessToken, merchantId, staleMapping.IFoodItemId, false, cancellationToken);
            if (pauseResult.Success)
                paused++;
            else
                errors++;
        }

        return (paused, errors);
    }

    // Contexto de sincronização resolvido uma única vez por empresa (não por filial) — ver
    // LoadCatalogContextAsync.
    private sealed class CatalogSyncContext
    {
        public required IReadOnlyCollection<Category> Categories { get; init; }
        public required IReadOnlyCollection<Product> Products { get; init; }
        public required HashSet<long> ActiveProductIds { get; init; }
        public required Dictionary<long, ComplementGroup> ComplementGroupsById { get; init; }
        public required Dictionary<long, string> ComplementItemNames { get; init; }
        public required Dictionary<long, List<long>> ComplementGroupIdsByProduct { get; init; }
    }

    // Memoiza a resolução (sucesso ou falha) do catalogId da filial entre chamadas sucessivas de
    // SyncSingleCategoryAsync dentro do mesmo branch — equivalente às variáveis locais
    // catalogId/catalogResolutionFailed do método Handle original.
    private sealed class CatalogIdResolutionState
    {
        public string? CatalogId { get; set; }
        public bool ResolutionFailed { get; set; }
    }

    private sealed record CategorySyncOutcome(string? IFoodCategoryId, bool Created, bool HasError);

    private sealed record ProductSyncOutcome(bool Synced, int Errors);

    // Acumulador mutável dos totais do resumo final, compartilhado entre as chamadas de
    // SyncBranchAsync para cada filial.
    private sealed class SyncTotals
    {
        public int BranchesSynced { get; set; }
        public int CategoriesCreated { get; set; }
        public int ProductsSynced { get; set; }
        public int ProductsPaused { get; set; }
        public int Errors { get; set; }
    }
}
