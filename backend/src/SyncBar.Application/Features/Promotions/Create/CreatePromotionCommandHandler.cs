using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Promotions.Create;

internal sealed class CreatePromotionCommandHandler : BaseCommandHandler<CreatePromotionCommand, long>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePromotionCommandHandler(
        IPromotionRepository promotionRepository,
        IProductRepository productRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _promotionRepository = promotionRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreatePromotionCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente que está criando a promoção, preencha:

                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure<long>(new Error("Product.NotFound", "Product not found."));

                var promotion = Promotion.Create(
                    request.BranchId, request.ProductId, request.Name.Trim(),
                    request.DayOfWeek, request.StartMinuteOfDay, request.EndMinuteOfDay,
                    request.PromotionTypeId, request.DiscountRate);

                if (promotion.IsFailure)
                    return Result.Failure<long>(promotion.Error);

                await _promotionRepository.AddAsync(promotion.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(promotion.Value.Id);
            });
    }
}