using FluentValidation;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Auth.CustomerLogin;

namespace SyncBar.Application.Features.Auth.CustomerRefresh;

public sealed record CustomerRefreshTokenCommand(string RefreshToken) : ICommand<CustomerLoginResponse>;

public sealed class CustomerRefreshTokenCommandValidator : AbstractValidator<CustomerRefreshTokenCommand>
{
    public CustomerRefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(500);
}
