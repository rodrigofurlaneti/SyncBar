using MediatR;
using SyncBar.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Orders.GetQrViewSetting
{
    public sealed record GetQrViewSettingQuery(long BranchId) : IRequest<Result<QrViewSettingResponse>>;
}
