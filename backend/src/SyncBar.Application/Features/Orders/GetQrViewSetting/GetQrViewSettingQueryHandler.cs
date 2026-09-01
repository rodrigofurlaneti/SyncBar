using MediatR;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Orders.GetQrViewSetting
{
    internal sealed class GetQrViewSettingQueryHandler : IRequestHandler<GetQrViewSettingQuery, Result<QrViewSettingResponse>>
    {
        private readonly IDiningTableRepository _diningTableRepository;

        public GetQrViewSettingQueryHandler(IDiningTableRepository diningTableRepository)
        {
            _diningTableRepository = diningTableRepository;
        }

        public async Task<Result<QrViewSettingResponse>> Handle(GetQrViewSettingQuery request, CancellationToken cancellationToken)
        {
            var tables = await _diningTableRepository.GetByBranchAsync(request.BranchId, cancellationToken);
            var isEnabled = tables.Count > 0 ? tables.First().IsQrViewEnabled : true;
            return Result.Success(new QrViewSettingResponse(isEnabled));
        }
    }
}
