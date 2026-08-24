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

    // Contexto validado da sincronização — reúne tudo que o Handle precisa depois de resolver o
    // merchant e carregar/validar a configuração de pizza, pra manter o Handle enxuto.
    private sealed record PizzaSyncContext(
        long CompanyId,
        string MerchantId,
        string Token,
        PizzaConfiguration Configuration,
        List<PizzaSize> ActiveSizes,
        List<long> FlavorIds,
        Dictionary<long, PizzaFlavor> FlavorsById);

    public override async Task<Result<SyncIFoodPizzaResult>> Handle(SyncIFoodPizzaCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SyncIFoodPizzaCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var contextResult = await LoadContextAsync(request, cancellationToken);
                if (contextResult.IsFailure)
                    return Result.Failure<SyncIFoodPizzaResult>(contextResult.Error);
                var context = contextResult.Value;

                var jsonBody = BuildPayloadJson(context);

                var existingMapping = await ifoodPizzaMappingRepository.GetByPizzaConfigurationAndBranchForUpdateAsync(
                    request.PizzaConfigurationId, request.BranchId, cancellationToken);

                var syncResult = await SyncPizzaWithIFoodAsync(
                    context.Token, context.MerchantId, existingMapping, jsonBody, cancellationToken);
                if (syncResult.IsFailure)
                    return Result.Failure<SyncIFoodPizzaResult>(syncResult.Error);

                using var document = syncResult.Value.Document;
                var ifoodPizzaId = syncResult.Value.IFoodPizzaId;

                var mappingResult = await UpsertMappingAsync(
                    request, existingMapping, ifoodPizzaId, document.RootElement, cancellationToken);
                if (mappingResult.IsFailure)
                    return Result.Failure<SyncIFoodPizzaResult>(mappingResult.Error);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(new SyncIFoodPizzaResult(ifoodPizzaId));
            });
    }

    // Resolve o merchant e carrega/valida a configuração de pizza, o produto dono dela, os
    // tamanhos ativos e os sabores com preço ativo — tudo que precisa existir antes de montar o
    // payload e chamar o iFood. Qualquer falha aqui aborta a sincronização.
    private async Task<Result<PizzaSyncContext>> LoadContextAsync(SyncIFoodPizzaCommand request, CancellationToken cancellationToken)
    {
        var resolved = await IFoodMerchantResolution.ResolveAsync(
            request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<PizzaSyncContext>(resolved.Error);
        var (companyId, merchantId, token, _) = resolved.Value;

        var configuration = await pizzaConfigurationRepository.GetByIdAsync(request.PizzaConfigurationId, cancellationToken);
        if (configuration is null || !configuration.IsActive)
            return Result.Failure<PizzaSyncContext>(new Error("PizzaConfiguration.NotFound", "Pizza configuration not found."));

        var product = await productRepository.GetByIdAsync(configuration.ProductId, cancellationToken);
        if (product is null || product.CompanyId != companyId)
            return Result.Failure<PizzaSyncContext>(new Error("Product.NotFound", "Product not found for this company."));

        var activeSizes = configuration.Sizes.Where(s => s.IsActive).ToList();
        if (activeSizes.Count == 0)
            return Result.Failure<PizzaSyncContext>(new Error("PizzaConfiguration.NoSizes", "Pizza configuration has no active sizes to sync."));

        var flavorIds = configuration.FlavorPrices.Where(p => p.IsActive).Select(p => p.PizzaFlavorId).Distinct().ToList();
        if (flavorIds.Count == 0)
            return Result.Failure<PizzaSyncContext>(new Error("PizzaConfiguration.NoFlavors", "Pizza configuration has no active flavor prices to sync."));

        var flavors = await pizzaFlavorRepository.GetByIdsAsync(flavorIds, cancellationToken);
        var flavorsById = flavors.ToDictionary(f => f.Id);

        return Result.Success(new PizzaSyncContext(companyId, merchantId, token, configuration, activeSizes, flavorIds, flavorsById));
    }

    // MVP (Fase 17): só envia elementos ATIVOS — remoção/pausa de tamanho/borda/sabor
    // já sincronizado não é propagada como UNAVAILABLE nesta fase (o elemento
    // simplesmente para de ser incluído no próximo PUT). Suficiente pro fluxo de
    // criação/edição normal; um comando dedicado de pausa por elemento fica pra uma
    // fase futura caso o lojista precise disso sem editar a configuração inteira.
    private static string BuildPayloadJson(PizzaSyncContext context)
    {
        var payload = new
        {
            sizes = context.ActiveSizes.Select(s => new
            {
                name = s.Name,
                status = "AVAILABLE",
                slices = s.Slices,
                acceptedFractions = s.AcceptedFractions,
                externalCode = $"{SizePrefix}{s.Id}",
                index = s.DisplayOrder,
            }).ToArray(),
            crusts = context.Configuration.Crusts.Where(c => c.IsActive).Select(c => new
            {
                name = c.Name,
                status = "AVAILABLE",
                externalCode = $"{CrustPrefix}{c.Id}",
                index = c.DisplayOrder,
            }).ToArray(),
            edges = context.Configuration.Edges.Where(e => e.IsActive).Select(e => new
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
            toppings = context.FlavorIds.Select((id, index) =>
            {
                var flavor = context.FlavorsById.GetValueOrDefault(id);
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

        return JsonSerializer.Serialize(payload);
    }

    // Decide create vs update (com base em já existir mapeamento pra essa pizza nessa filial),
    // chama o catálogo v1 do iFood e extrai o id da pizza da resposta. Devolve o JsonDocument pro
    // chamador ler os elementos (sizes/crusts/edges/toppings) — quem chama é responsável por
    // dar dispose (via using) depois de consumir o RootElement.
    private async Task<Result<(JsonDocument Document, string IFoodPizzaId)>> SyncPizzaWithIFoodAsync(
        string token,
        string merchantId,
        IFoodPizzaMapping? existingMapping,
        string jsonBody,
        CancellationToken cancellationToken)
    {
        var operation = existingMapping is null
            ? IFoodCatalogV1Operation.CreatePizza
            : IFoodCatalogV1Operation.UpdatePizza;
        var routeParams = existingMapping is null
            ? null
            : new Dictionary<string, string> { ["pizzaId"] = existingMapping.IFoodPizzaId };

        var apiResult = await catalogClient.InvokeCatalogV1Async(
            token, merchantId, operation, routeParams, null, jsonBody, cancellationToken);
        if (!apiResult.Success || apiResult.ResponseBody is null)
            return Result.Failure<(JsonDocument Document, string IFoodPizzaId)>(new Error("IFoodCatalog.PizzaSyncFailed",
                apiResult.ErrorMessage ?? $"iFood retornou {apiResult.StatusCode} ao sincronizar a pizza."));

        var document = JsonDocument.Parse(apiResult.ResponseBody);
        if (!document.RootElement.TryGetProperty("id", out var idProp) || idProp.GetString() is not { Length: > 0 } ifoodPizzaId)
        {
            document.Dispose();
            return Result.Failure<(JsonDocument Document, string IFoodPizzaId)>(new Error("IFoodCatalog.PizzaSyncNoId", "iFood não retornou o id da pizza."));
        }

        return Result.Success((document, ifoodPizzaId));
    }

    // Cria ou atualiza o IFoodPizzaMapping local com o id devolvido pelo iFood e casa cada
    // elemento (tamanho/borda/sabor) da resposta de volta com o id local via ExtractElements.
    private async Task<Result> UpsertMappingAsync(
        SyncIFoodPizzaCommand request,
        IFoodPizzaMapping? existingMapping,
        string ifoodPizzaId,
        JsonElement root,
        CancellationToken cancellationToken)
    {
        IFoodPizzaMapping mapping;
        if (existingMapping is null)
        {
            var created = IFoodPizzaMapping.Create(request.PizzaConfigurationId, request.BranchId, ifoodPizzaId);
            if (created.IsFailure)
                return Result.Failure(created.Error);

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

        return Result.Success();
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
