namespace SyncBar.Domain.Primitives;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "O valor fornecido é nulo.");

    public static Error Validation(string code, string message) => new(code, message);
    public static Error NotFound(string code, string message) => new(code, message);
    public static Error Conflict(string code, string message) => new(code, message);
    public static Error Failure(string code, string message) => new(code, message);
}
