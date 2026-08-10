using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.GetMyFeatures;

internal sealed class GetMyFeaturesQueryHandler(
    IAppUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IAppFeatureRepository featureRepository,
    IJobTitleFeatureRepository jobTitleFeatureRepository,
    IAppUserFeatureRepository userFeatureRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetMyFeaturesQuery, MyFeaturesResponse>(logRepository, unitOfWork)
{
    public override Task<Result<MyFeaturesResponse>> Handle(GetMyFeaturesQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(GetMyFeaturesQueryHandler), nameof(Handle), null, async (userIdBox) =>
        {
            // Informa explicitamente o ID do usuário logado para o rastreador de log salvar na coluna AppUserId
            userIdBox.Value = request.AppUserId;

            var allFeatures = await featureRepository.GetAllAsync(cancellationToken);

            if (request.IsManager)
                return Result.Success(new MyFeaturesResponse(true, allFeatures.Select(f => f.Code).ToList()));

            var user = await userRepository.GetByIdAsync(request.AppUserId, cancellationToken);
            if (user is null || !user.IsActive)
                return Result.Failure<MyFeaturesResponse>(new Error("AppUser.NotFound", "User not found."));

            var featureIds = new HashSet<long>();

            if (user.EmployeeId.HasValue)
            {
                var employee = await employeeRepository.GetByIdAsync(user.EmployeeId.Value, cancellationToken);
                if (employee is not null)
                {
                    var byJobTitle = await jobTitleFeatureRepository.GetByJobTitleAsync(employee.JobTitleId, cancellationToken);
                    foreach (var link in byJobTitle)
                        featureIds.Add(link.AppFeatureId);
                }
            }

            var byUser = await userFeatureRepository.GetByUserAsync(user.Id, cancellationToken);
            foreach (var link in byUser)
                featureIds.Add(link.AppFeatureId);

            var codes = allFeatures
                .Where(f => featureIds.Contains(f.Id))
                .Select(f => f.Code)
                .ToList();

            return Result.Success(new MyFeaturesResponse(false, codes));
        });
}