using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — configuração de pizza de um Product (1:1 via ProductId). Um Product vira "uma
// pizza vendável" quando tem uma PizzaConfiguration ativa com pelo menos 1 PizzaSize e pelo
// menos 1 PizzaFlavorPrice. Aggregate root dono de Sizes/Crusts/Edges/FlavorPrices — mesmo
// padrão de ComplementGroup dono de Complements. Motivo de ser uma configuração separada do
// Product (em vez de campos direto nele): Product.SalePrice não faz sentido pra pizza (o preço
// varia por tamanho × sabor × borda × recheio de borda) — ProductSalePrice, quando existe pra um
// Product com PizzaConfiguration, é ignorado no lançamento do pedido (ver AddPizzaOrderItem).
public sealed class PizzaConfiguration : AggregateRoot
{
    private readonly List<PizzaSize> _sizes = [];
    private readonly List<PizzaCrust> _crusts = [];
    private readonly List<PizzaEdge> _edges = [];
    private readonly List<PizzaFlavorPrice> _flavorPrices = [];

    public long ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<PizzaSize> Sizes => _sizes.AsReadOnly();
    public IReadOnlyCollection<PizzaCrust> Crusts => _crusts.AsReadOnly();
    public IReadOnlyCollection<PizzaEdge> Edges => _edges.AsReadOnly();
    public IReadOnlyCollection<PizzaFlavorPrice> FlavorPrices => _flavorPrices.AsReadOnly();

    private PizzaConfiguration() : base(0) { }

    private PizzaConfiguration(long productId) : base(0)
    {
        ProductId = productId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<PizzaConfiguration> Create(long productId) =>
        Result.Success(new PizzaConfiguration(productId));

    public Result<PizzaSize> AddSize(string name, int? slices, int acceptedFractions, int displayOrder)
    {
        if (_sizes.Any(s => s.IsActive && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<PizzaSize>(new Error("PizzaConfiguration.DuplicateSizeName", "A size with this name already exists."));

        var size = PizzaSize.Create(Id, name, slices, acceptedFractions, displayOrder);
        if (size.IsFailure)
            return Result.Failure<PizzaSize>(size.Error);

        _sizes.Add(size.Value);
        UpdatedAt = DateTime.Now;
        return Result.Success(size.Value);
    }

    public Result UpdateSize(long pizzaSizeId, string name, int? slices, int acceptedFractions, int displayOrder)
    {
        var size = _sizes.FirstOrDefault(s => s.Id == pizzaSizeId && s.IsActive);
        if (size is null)
            return Result.Failure(new Error("PizzaConfiguration.SizeNotFound", "Size not found."));

        var result = size.UpdateDetails(name, slices, acceptedFractions, displayOrder);
        if (result.IsFailure)
            return result;

        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public Result RemoveSize(long pizzaSizeId)
    {
        var size = _sizes.FirstOrDefault(s => s.Id == pizzaSizeId && s.IsActive);
        if (size is null)
            return Result.Failure(new Error("PizzaConfiguration.SizeNotFound", "Size not found."));

        size.Deactivate();
        foreach (var price in _flavorPrices.Where(p => p.IsActive && p.PizzaSizeId == pizzaSizeId))
            price.Deactivate();

        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public Result<PizzaCrust> AddCrust(string name, decimal extraPrice, int displayOrder)
    {
        var crust = PizzaCrust.Create(Id, name, extraPrice, displayOrder);
        if (crust.IsFailure)
            return Result.Failure<PizzaCrust>(crust.Error);

        _crusts.Add(crust.Value);
        UpdatedAt = DateTime.Now;
        return Result.Success(crust.Value);
    }

    public Result RemoveCrust(long pizzaCrustId)
    {
        var crust = _crusts.FirstOrDefault(c => c.Id == pizzaCrustId && c.IsActive);
        if (crust is null)
            return Result.Failure(new Error("PizzaConfiguration.CrustNotFound", "Crust not found."));

        crust.Deactivate();
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public Result<PizzaEdge> AddEdge(string name, decimal extraPrice, int displayOrder)
    {
        var edge = PizzaEdge.Create(Id, name, extraPrice, displayOrder);
        if (edge.IsFailure)
            return Result.Failure<PizzaEdge>(edge.Error);

        _edges.Add(edge.Value);
        UpdatedAt = DateTime.Now;
        return Result.Success(edge.Value);
    }

    public Result RemoveEdge(long pizzaEdgeId)
    {
        var edge = _edges.FirstOrDefault(e => e.Id == pizzaEdgeId && e.IsActive);
        if (edge is null)
            return Result.Failure(new Error("PizzaConfiguration.EdgeNotFound", "Edge not found."));

        edge.Deactivate();
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    // Upsert: se já existe um preço ativo pra esse par sabor×tamanho, atualiza; senão, cria.
    // É essa linha que torna o sabor "vendável" naquele tamanho (ver comentário da classe).
    public Result<PizzaFlavorPrice> SetFlavorPrice(long pizzaFlavorId, long pizzaSizeId, decimal price)
    {
        if (_sizes.All(s => s.Id != pizzaSizeId || !s.IsActive))
            return Result.Failure<PizzaFlavorPrice>(new Error("PizzaConfiguration.SizeNotFound", "Size not found."));

        var existing = _flavorPrices.FirstOrDefault(p =>
            p.IsActive && p.PizzaFlavorId == pizzaFlavorId && p.PizzaSizeId == pizzaSizeId);
        if (existing is not null)
        {
            var updateResult = existing.UpdatePrice(price);
            if (updateResult.IsFailure)
                return Result.Failure<PizzaFlavorPrice>(updateResult.Error);

            UpdatedAt = DateTime.Now;
            return Result.Success(existing);
        }

        var created = PizzaFlavorPrice.Create(Id, pizzaFlavorId, pizzaSizeId, price);
        if (created.IsFailure)
            return Result.Failure<PizzaFlavorPrice>(created.Error);

        _flavorPrices.Add(created.Value);
        UpdatedAt = DateTime.Now;
        return Result.Success(created.Value);
    }

    public Result RemoveFlavor(long pizzaFlavorId)
    {
        var prices = _flavorPrices.Where(p => p.IsActive && p.PizzaFlavorId == pizzaFlavorId).ToList();
        if (prices.Count == 0)
            return Result.Failure(new Error("PizzaConfiguration.FlavorNotFound", "This flavor has no price set on this pizza."));

        foreach (var price in prices)
            price.Deactivate();

        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    // Regra de negócio (decisão do SyncBar, não imposta pelo iFood — a API só guarda o preço por
    // sabor×tamanho, quem decide como cobrar um meio-a-meio é o lojista): o preço da pizza
    // fracionada é o do sabor MAIS CARO entre os escolhidos, na convenção mais comum entre
    // pizzarias brasileiras. Ver PizzaConfiguration.CalculateUnitPrice.
    public Result<decimal> CalculateUnitPrice(long pizzaSizeId, long? pizzaCrustId, long? pizzaEdgeId, IReadOnlyCollection<long> pizzaFlavorIds)
    {
        var size = _sizes.FirstOrDefault(s => s.Id == pizzaSizeId && s.IsActive);
        if (size is null)
            return Result.Failure<decimal>(new Error("PizzaConfiguration.SizeNotFound", "Size not found."));

        if (pizzaFlavorIds.Count == 0)
            return Result.Failure<decimal>(new Error("PizzaConfiguration.NoFlavorsSelected", "At least one flavor must be selected."));

        if (pizzaFlavorIds.Count > size.AcceptedFractions)
            return Result.Failure<decimal>(new Error("PizzaConfiguration.TooManyFractions",
                $"This size accepts at most {size.AcceptedFractions} flavor(s)."));

        decimal maxFlavorPrice = 0;
        foreach (var flavorId in pizzaFlavorIds)
        {
            var price = _flavorPrices.FirstOrDefault(p => p.IsActive && p.PizzaFlavorId == flavorId && p.PizzaSizeId == pizzaSizeId);
            if (price is null)
                return Result.Failure<decimal>(new Error("PizzaConfiguration.FlavorNotAvailableForSize",
                    $"Flavor {flavorId} is not available for this size."));

            if (price.Price > maxFlavorPrice)
                maxFlavorPrice = price.Price;
        }

        decimal crustPrice = 0;
        if (pizzaCrustId.HasValue)
        {
            var crust = _crusts.FirstOrDefault(c => c.Id == pizzaCrustId.Value && c.IsActive);
            if (crust is null)
                return Result.Failure<decimal>(new Error("PizzaConfiguration.CrustNotFound", "Crust not found."));
            crustPrice = crust.ExtraPrice;
        }

        decimal edgePrice = 0;
        if (pizzaEdgeId.HasValue)
        {
            var edge = _edges.FirstOrDefault(e => e.Id == pizzaEdgeId.Value && e.IsActive);
            if (edge is null)
                return Result.Failure<decimal>(new Error("PizzaConfiguration.EdgeNotFound", "Edge not found."));
            edgePrice = edge.ExtraPrice;
        }

        return Result.Success(maxFlavorPrice + crustPrice + edgePrice);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
