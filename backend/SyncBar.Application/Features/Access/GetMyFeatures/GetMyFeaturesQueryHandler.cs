using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.GetMyFeatures;

internal sealed class GetMyFeaturesQueryHandler(
    IAppUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IAppFeatureRepository featureRepository,
    IJobTitleFeatureRepository jobTitleFeatureRepository,
    IAppUserFeatureRepository userFeatureRepository)
    : IQueryHandler<GetMyFeaturesQuery, MyFeaturesResponse>
{
    public async Task<Result<MyFeaturesResponse>> Handle(GetMyFeaturesQuery request, CancellationToken cancellationToken)
    {
        var allFeatures = await featureRepository.GetAllAsync(cancellationToken);

        if (request.IsManager)
            return Result.Success(new MyFeaturesResponse(true, allFeatures.Select(f => f.Code).ToList()));

        var user = await userRepository.GetByIdAsync(request.AppUserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure<MyFeaturesResponse>(new Error("AppUser.NotFound", "User not found."));

        var featureIds = new HashSet<long>();

        // Telas herdadas do CARGO (via funcionario vinculado).
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

        // Telas extras da PESSOA — uniao com o cargo.
        var byUser = await userFeatureRepository.GetByUserAsync(user.Id, cancellationToken);
        foreach (var link in byUser)
            featureIds.Add(link.AppFeatureId);

        var codes = allFeatures
            .Where(f => featureIds.Contains(f.Id))
            .Select(f => f.Code)
            .ToList();

        return Result.Success(new MyFeaturesResponse(false, codes));
    }
}
