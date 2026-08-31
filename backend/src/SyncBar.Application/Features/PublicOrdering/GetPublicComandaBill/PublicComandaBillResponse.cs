using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.PublicOrdering.GetPublicComandaBill
{
    public sealed record PublicComandaBillResponse(
        long OrderId,
        string ComandaCode,
        string Status,
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal ServiceFeeAmount,
        decimal TotalAmount,
        decimal? CreditLimitAmount,
        IReadOnlyCollection<PublicComandaBillItemResponse> Items
    );
}
