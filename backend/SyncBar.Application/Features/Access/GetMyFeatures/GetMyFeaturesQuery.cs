using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Access.GetMyFeatures;

// IsManager vem das roles do JWT (Administrador/Gerente) — bypass com acesso total.
public sealed record GetMyFeaturesQuery(long AppUserId, bool IsManager) : IQuery<MyFeaturesResponse>;
