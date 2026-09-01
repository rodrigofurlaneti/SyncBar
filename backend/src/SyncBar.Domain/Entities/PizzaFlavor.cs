using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — cadastro de SABOR de pizza, reutilizável entre várias PizzaConfiguration da mesma
// empresa (ex.: "Calabresa" pode ser sabor da pizza "Pizza Grande" e da pizza "Pizza Broto" ao
// mesmo tempo) — mesmo espírito de ComplementItem (cadastro leve reaproveitável), mas com
// Description/ImageUrl porque o Ifood exige esses campos no objeto "topping" da API de pizza
// (ver IfoodCatalogClient — CreatePizza/UpdatePizza, campo toppings[].description/image).
public sealed class PizzaFlavor : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private PizzaFlavor() : base(0) { }

    private PizzaFlavor(long companyId, string name, string? description) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<PizzaFlavor> Create(long companyId, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<PizzaFlavor>(new Error("PizzaFlavor.EmptyName", "Name is required."));

        return Result.Success(new PizzaFlavor(companyId, name, description));
    }

    public Result UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("PizzaFlavor.EmptyName", "Name is required."));

        Name = name;
        Description = description;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public void SetImage(string? imageUrl)
    {
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.Now;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
