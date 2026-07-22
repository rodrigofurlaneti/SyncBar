namespace SyncBar.Domain.Primitives;

public abstract class ValueObject : IEquatable<ValueObject>
{
    public abstract IEnumerable<object> GetAtomicValues();

    public bool Equals(ValueObject? other)
        => other is not null && other.GetType() == GetType()
           && GetAtomicValues().SequenceEqual(other.GetAtomicValues());

    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    public override int GetHashCode()
        => GetAtomicValues().Aggregate(0, (hash, value) => HashCode.Combine(hash, value));
}
