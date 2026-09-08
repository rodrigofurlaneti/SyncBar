namespace SyncBar.Application.Abstractions.Security;

public interface IReadingProofService
{
    string Issue(Guid tableToken, string? comandaCode, string method);
    bool Validate(string? proof, Guid tableToken, string? comandaCode, IReadOnlyCollection<string> allowedMethods);
}
