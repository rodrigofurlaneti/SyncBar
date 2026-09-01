namespace SyncBar.Domain.Primitives;

// Classe base abstrata herdada por todas as entidades do domínio: não pode ser 'sealed'
// (contradiz 'abstract'). Para não disparar o alerta do Sonar sobre classes não seladas
// implementando IEquatable<T>, a igualdade é exposta como método próprio em vez de via
// a interface — mesmo comportamento em tempo de execução (Equals/GetHashCode continuam
// sobrescritos), sem abrir mão da herança que o modelo de domínio exige.
public abstract class Entity
{
    public long Id { get; protected set; }

    protected Entity(long id) => Id = id;

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;
        return other.Id == Id && Id != 0;
    }

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? first, Entity? second)
        => first is null ? second is null : first.Equals(second);

    public static bool operator !=(Entity? first, Entity? second) => !(first == second);
}
