using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Um grupo de opções vinculável a um ou mais Products (ex.: "Escolha o ponto da carne",
// "Escolha uma bebida", "Adicionais") — espelha optionGroup do módulo Catalog do iFood.
// Dono de uma coleção de Complement (mesmo padrão de CustomerOrder dono de OrderItem).
// MinSelection/MaxSelection controlam quantas opções o cliente pode/deve escolher deste
// grupo (ex.: MinSelection 1, MaxSelection 1 = obrigatório escolher exatamente uma).
public sealed class ComplementGroup : AggregateRoot
{
    private readonly List<Complement> _complements = [];

    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public long ComplementGroupTypeId { get; private set; }
    public int MinSelection { get; private set; }
    public int MaxSelection { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<Complement> Complements => _complements.AsReadOnly();

    private ComplementGroup() : base(0) { }

    private ComplementGroup(long companyId, string name, long complementGroupTypeId, int minSelection, int maxSelection) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        ComplementGroupTypeId = complementGroupTypeId;
        MinSelection = minSelection;
        MaxSelection = maxSelection;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<ComplementGroup> Create(long companyId, string name, long complementGroupTypeId, int minSelection, int maxSelection)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ComplementGroup>(new Error("ComplementGroup.EmptyName", "Name is required."));
        if (minSelection < 0)
            return Result.Failure<ComplementGroup>(new Error("ComplementGroup.InvalidMinSelection", "Minimum selection cannot be negative."));
        if (maxSelection < 1)
            return Result.Failure<ComplementGroup>(new Error("ComplementGroup.InvalidMaxSelection", "Maximum selection must be at least 1."));
        if (minSelection > maxSelection)
            return Result.Failure<ComplementGroup>(new Error("ComplementGroup.MinGreaterThanMax", "Minimum selection cannot be greater than maximum selection."));

        return Result.Success(new ComplementGroup(companyId, name, complementGroupTypeId, minSelection, maxSelection));
    }

    public Result UpdateDetails(string name, long complementGroupTypeId, int minSelection, int maxSelection)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("ComplementGroup.EmptyName", "Name is required."));
        if (minSelection < 0)
            return Result.Failure(new Error("ComplementGroup.InvalidMinSelection", "Minimum selection cannot be negative."));
        if (maxSelection < 1)
            return Result.Failure(new Error("ComplementGroup.InvalidMaxSelection", "Maximum selection must be at least 1."));
        if (minSelection > maxSelection)
            return Result.Failure(new Error("ComplementGroup.MinGreaterThanMax", "Minimum selection cannot be greater than maximum selection."));

        Name = name;
        ComplementGroupTypeId = complementGroupTypeId;
        MinSelection = minSelection;
        MaxSelection = maxSelection;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public Result<Complement> AddComplement(long complementItemId, decimal extraPrice)
    {
        if (_complements.Any(c => c.IsActive && c.ComplementItemId == complementItemId))
            return Result.Failure<Complement>(new Error("ComplementGroup.DuplicateComplementItem", "This complement item is already in the group."));

        var complement = Complement.Create(Id, complementItemId, extraPrice);
        if (complement.IsFailure)
            return Result.Failure<Complement>(complement.Error);

        _complements.Add(complement.Value);
        UpdatedAt = DateTime.Now;
        return Result.Success(complement.Value);
    }

    public Result UpdateComplementPrice(long complementId, decimal extraPrice)
    {
        var complement = _complements.FirstOrDefault(c => c.Id == complementId && c.IsActive);
        if (complement is null)
            return Result.Failure(new Error("ComplementGroup.ComplementNotFound", "Complement not found."));

        var result = complement.UpdateExtraPrice(extraPrice);
        if (result.IsFailure)
            return result;

        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public Result RemoveComplement(long complementId)
    {
        var complement = _complements.FirstOrDefault(c => c.Id == complementId && c.IsActive);
        if (complement is null)
            return Result.Failure(new Error("ComplementGroup.ComplementNotFound", "Complement not found."));

        complement.Deactivate();
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
