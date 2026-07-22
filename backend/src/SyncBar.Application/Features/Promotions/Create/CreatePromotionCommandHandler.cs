using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Promotions.Create;

internal sealed class CreatePromotionCommandHandler(
    IPromotionRepository promotionRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePromotionCommand, long>
{
    public async Task<Result<long>> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure<long>(new Error("Product.NotFound", "Product not found."));

        var promotion = Promotion.Create(
            request.BranchId, request.ProductId, request.Name.Trim(),
            request.DayOfWeek, request.StartMinuteOfDay, request.EndMinuteOfDay,
            request.PromotionTypeId, request.DiscountRate);
        if (promotion.IsFailure)
            return Result.Failure<long>(promotion.Error);

        await promotionRepository.AddAsync(promotion.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(promotion.Value.Id);
    }
}
