using FluentValidation;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Orders.Open;

public sealed class OpenOrderCommandValidator : AbstractValidator<OpenOrderCommand>
{
    public OpenOrderCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.OrderTypeId).InclusiveBetween(1, 3);
        // Mesa exige mesa/comanda; Retirada/Delivery exigem nome do cliente (e endereço, se Delivery).
        RuleFor(x => x)
            .Must(x => x.DiningTableId.HasValue || x.ComandaId.HasValue)
            .WithMessage("Order must have a dining table or a comanda.")
            .When(x => x.OrderTypeId == OrderTypeIds.Mesa);
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Takeaway/delivery orders require a customer name.")
            .When(x => x.OrderTypeId != OrderTypeIds.Mesa);
        RuleFor(x => x.DeliveryAddress)
            .NotEmpty().WithMessage("Delivery orders require a delivery address.")
            .When(x => x.OrderTypeId == OrderTypeIds.Delivery);
        RuleFor(x => x.GuestCount).GreaterThan(0).When(x => x.GuestCount.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
