using System;
using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Dining.Assignment.End
{
    public sealed record EndDiningAreaAssignmentCommand(
        long Id,
        DateTime EndAt) : ICommand;
}
