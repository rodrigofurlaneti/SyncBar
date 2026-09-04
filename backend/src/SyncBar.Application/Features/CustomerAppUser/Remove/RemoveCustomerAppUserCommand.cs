using FluentValidation;
using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.CustomerAppUser.Remove
{
    public sealed record RemoveCustomerAppUserCommand(long Id) : ICommand;
}
