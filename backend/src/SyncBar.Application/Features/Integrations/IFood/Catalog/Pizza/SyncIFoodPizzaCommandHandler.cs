using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Pizza;

internal sealed class SyncIFoodPizzaCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    IPizzaConfigurationRepository pizzaConfigurationRepository,
    IPizzaFlavorRepository pizzaFlavorRepository,
    IProductRepository productRepository,
    IIFoodPizzaMappingRepository ifoodPizzaMappingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SyncIFoodPizzaCommand, SyncIFoodPizzaResult>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    // Prefixos usados no externalCode de cada elemento — é assim que casamos a resposta do iFood
    // (que devolve os elementos sem garantia de ordem) de volta com o id local que os gerou, já
    // que a API não deixa a gente propor o id do elemento no create (só o externalCode).
    private const string SizePrefix = "pizzasize-";
    private const string CrustPrefix = "pizzacrust-";
    private const string EdgePrefix = "pizzaedge-";
    private const string ToppingPrefix = "pizzaflavor-";

    public override async Task<Result<SyncIFoodPizzaResult>> Handle(SyncIFoodPizzaCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SyncIFoodPizzaCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<SyncIFoodPizzaResult>(resolved.Error);
                var (companyId, merchantId, token, _) = resolved.Value;

                var configuration = await pizzaConfigurationRepository.GetByIdAsync(request.PizzaConfigurationId, cancellationToken);
                if (configuration is null || !configuration.IsActive)
                    return Result.Failure<SyncIFoodPizzaResult>(new Error("PizzaConfiguration.NotFound", "Pizza configuration not found."));

                var product = await productRepository.GetByIdAsync(configuration.ProductId, cancellationToken);
                if (product is null || product.CompanyId != companyId)
                    return Result.Failure<SyncIFoodPizzaResult>(new Error("Product.NotFound", "Product not found for this company."));

                var activeSizes = configuration.Sizes.Where(s => s.IsActive).ToList();
                if (activeSizes.Count == 0)
                    return Result.Failure<SyncIFoodPizzaResult>(new Error("PizzaConfiguration.NoSizes", "Pizza configuration has no active sizes to sync."));

                var flavorIds = configuration.FlavorPrices.Where(p => p.IsActive).Select(p => p.PizzaFlavorId).Distinct().ToList();
                if (flavorIds.Count == 0)
                    return Result.Failure<SyncIFoodPizzaResult>(new Error("PizzaConfiguration.NoFlavors", "Pizza configuration has no active flavor prices to sync."));

                var flavors = await pizzaFlavorRepository.GetByIdsAsync(flavorIds, cancellationToken);
                var flavorsById = flavors.ToDictionary(f => f.Id);

                // MVP (Fase 17): só envia elementos ATIVOS — remoção/pausa de tamanho/borda/sabor
                // já sincronizado não é propagada como UNAVAILABLE nesta fase (o elemento
                // simplesmente para de ser incluído no próximo PUT). Suficiente pro fluxo de
                // criação/edição normal; um comando dedicado de pausa por elemento fica pra uma
                // fase futura caso o lojista precise disso sem editar a configuração inteira.
                var payload = new
                {
                    sizes = activeSizes.Select(s => new
                    {
                        name = s.Name,
                        status = "AVAILABLE",
                        slices = s.Slices,
                        acceptedFractions = s.AcceptedFractions,
                        externalCode = $"{SizePrefix}{s.Id}",
                        index = s.DisplayOrder,
                    }).ToArray(),
                    crusts = configuration.Crusts.Where(c => c.IsActive).Select(c => new
                    {
                        name = c.Name,
                        status = "AVAILABLE",
                        externalCode = $"{CrustPrefix}{c.Id}",
                        index = c.DisplayOrder,
                    }).ToArray(),
                    edges = configuration.Edges.Where(e => e.IsActive).Select(e => new
                    {
                        name = e.Name,
                        status = "AVAILABLE",
                        externalCode = $"{EdgePrefix}{e.Id}",
                        index = e.DisplayOrder,
                    }).ToArray(),
                    // ⚠️ RISCO CONHECIDO: image/imagePath do topping não são enviados aqui —
                    // PizzaFlavor.ImageUrl guarda uma URL, mas o schema oficial não deixa claro se
                    // "image" espera uma URL ou um base64 (mesma ressalva já registrada em
                    // IFoodImageUploadResult pra upload de imagem de produto). Até confirmar contra
                    // o sandbox, o sabor sincroniza sem imagem — o iFood aceita o topping normalmente.
                    toppings = flavorIds.Select((id, index) =>
                    {
                        var flavor = flavorsById.GetValueOrDefault(id);
                        return new
                        {
                            name = flavor?.Name ?? $"Sabor {id}",
                            externalCode = $"{ToppingPrefix}{id}",
                            description = flavor?.Description,
                            status = "AVAILABLE",
                            index,
                        };
                    }).ToArray(),
                };

                var jsonBody = JsonSerializer.Serialize(payload);

                var existingMapping = await ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(
                    request.PizzaConfigurationId, request.BranchId, cancellationToken);

                var operation = existingMapping is null
                    ? IFoodCatalogV1Operation.CreatePizza
                    : IFoodCatalogV1Operation.UpdatePizza;
                var routeParams = existingMapping is null
                    ? null
                    : new Dictionary<string, string> { ["pizzaId"] = existingMapping.IFoodPizzaId };

                var apiResult = await catalogClient.InvokeCatalogV1Async(
                    token, merchantId, operation, routeParams, null, jsonBody, cancellationToken);
                if (!apiResult.Success || apiResult.ResponseBody is null)
                    return Result.Failure<SyncIFoodPizzaResult>(new Error("IFoodCatalog.PizzaSyncFailed",
                        apiResult.ErrorMessage ?? $"iFood retornou {apiResult.StatusCode} ao sincronizar a pizza."));

                using var responseDoc = JsonDocument.Parse(apiResult.ResponseBody);
                var root = responseDoc.RootElement;
                if (!root.TryGetProperty("id", out var idProp) || idProp.GetString() is not { Length: > 0 } ifoodPizzaId)
                    return Result.Failure<SyncIFoodPizzaResult>(new Error("IFoodCatalog.PizzaSyncNoId", "iFood não retornou o id da pizza."));

                IFoodPizzaMapping mapping;
                if (existingMapping is null)
                {
                    var created = IFoodPizzaMapping.Create(request.PizzaConfigurationId, request.BranchId, ifoodPizzaId);
                    if (created.IsFailure)
                        return Result.Failure<SyncIFoodPizzaResult>(created.Error);

                    mapping = created.Value;
                    await ifoodPizzaMappingRepository.AddAsync(mapping, cancellationToken);
                }
                else
                {
                    mapping = existingMapping;
                    mapping.UpdateIFoodPizzaId(ifoodPizzaId);
                }

                ExtractElements(root, "sizes", SizePrefix, IFoodPizzaElementKind.Size, mapping);
                ExtractElements(root, "crusts", CrustPrefix, IFoodPizzaElementKind.Crust, mapping);
                ExtractElements(root, "edges", EdgePrefix, IFoodPizzaElementKind.Edge, mapping);
                ExtractElements(root, "toppings", ToppingPrefix, IFoodPizzaElementKind.Topping, mapping);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(new SyncIFoodPizzaResult(ifoodPizzaId));
            });
    }

    // Lê o array de elementos da resposta do iFood (sizes/crusts/edges/toppings) e casa cada um de
    // volta com o id local via externalCode (formato "{prefix}{localId}") — a resposta não garante
    // a mesma ordem do request, então não dá pra casar por índice.
    private static void ExtractElements(JsonElement root, string arrayName, string prefix, byte kind, IFoodPizzaMapping mapping)
    {
        if (!root.TryGetProperty(arrayName, out var array) || array.ValueKind != JsonValueKind.Array)
            return;

        foreach (var element in array.EnumerateArray())
        {
            if (!element.TryGetProperty("externalCode", out var codeProp) || codeProp.GetString() is not { } code)
                continue;
            if (!code.StartsWith(prefix, StringComparison.Ordinal))
                continue;
            if (!long.TryParse(code.AsSpan(prefix.Length), out var localId))
                continue;
            if (!element.TryGetProperty("id", out var idProp) || idProp.GetString() is not { Length: > 0 } elementId)
                continue;

            mapping.SetElement(kind, localId, elementId);
        }
    }
}
