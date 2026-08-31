using MediatR;
using SyncBar.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.PublicOrdering.GetPublicComandaBill
{
    public sealed record GetPublicComandaBillQuery(Guid TableToken, string ComandaCode) : IRequest<Result<PublicComandaBillResponse>>;
}
