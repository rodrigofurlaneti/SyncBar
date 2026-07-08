using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Employee : AggregateRoot
{
    public long BranchId { get; private set; }
    public long JobTitleId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public DateTime HiredAt { get; private set; }
    public DateTime? DismissedAt { get; private set; }
    public decimal? Salary { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Employee() : base(0) { }

    private Employee(long branchId, long jobTitleId, string name, string cpf, string? email, string? phone, DateTime hiredAt, DateTime? dismissedAt, decimal? salary) : base(0)
    {
        BranchId = branchId;
        JobTitleId = jobTitleId;
        Name = name;
        Cpf = cpf;
        Email = email;
        Phone = phone;
        HiredAt = hiredAt;
        DismissedAt = dismissedAt;
        Salary = salary;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Employee> Create(long branchId, long jobTitleId, string name, string cpf, string? email, string? phone, DateTime hiredAt, DateTime? dismissedAt, decimal? salary)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Employee>(new Error("Employee.EmptyName", "Name is required."));
        if (string.IsNullOrWhiteSpace(cpf))
            return Result.Failure<Employee>(new Error("Employee.EmptyCpf", "Cpf is required."));
        return Result.Success(new Employee(branchId, jobTitleId, name, cpf, email, phone, hiredAt, dismissedAt, salary));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
