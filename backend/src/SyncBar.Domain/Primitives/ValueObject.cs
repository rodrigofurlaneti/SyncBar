namespace SyncBar.Domain.Primitives;

// Classe base abstrata herdada pelos value objects do domínio: não pode ser 'sealed'
// (contradiz 'abstract'). Para não disparar o alerta do Sonar sobre classes não seladas
// implementando IEquatable<T>, a igualdade é exposta como método próprio em vez de via
// a interface — mesmo comportamento em tempo de execução (Equals/GetHashCode continuam
// sobrescritos), sem abrir mão da herança que o modelo de domínio exige.
public abstract class ValueObject
{
    public abstract IEnumerable<object> GetAtomicValues();

    public bool Equals(ValueObject? other)
        => other is not null && other.GetType() == GetType()
           && GetAtomicValues().SequenceEqual(other.GetAtomicValues());

    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    public override int GetHashCode()
        => GetAtomicValues().Aggregate(0, (hash, value) => HashCode.Combine(hash, value));
}
