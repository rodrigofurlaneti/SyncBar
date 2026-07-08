using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class JobTitle : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private JobTitle() : base(0) { }

    private JobTitle(long companyId, string name) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<JobTitle> Create(long companyId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<JobTitle>(new Error("JobTitle.EmptyName", "Name is required."));
        return Result.Success(new JobTitle(companyId, name));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
