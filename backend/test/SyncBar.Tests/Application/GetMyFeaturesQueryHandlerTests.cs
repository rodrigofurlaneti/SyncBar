using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Access.GetMyFeatures;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class GetMyFeaturesQueryHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IAppFeatureRepository _featureRepository = Substitute.For<IAppFeatureRepository>();
    private readonly IJobTitleFeatureRepository _jobTitleFeatureRepository = Substitute.For<IJobTitleFeatureRepository>();
    private readonly IAppUserFeatureRepository _userFeatureRepository = Substitute.For<IAppUserFeatureRepository>();

    private GetMyFeaturesQueryHandler CreateHandler()
        => new(_userRepository, _employeeRepository, _featureRepository, _jobTitleFeatureRepository, _userFeatureRepository);

    private void SetupCatalog()
        => _featureRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<AppFeature>
        {
            WithId(AppFeature.Create("Salao", "Salao").Value, 1),
            WithId(AppFeature.Create("Cardapio", "Cardapio").Value, 2),
            WithId(AppFeature.Create("Estoque", "Estoque").Value, 3),
        });

    // Ids sao atribuidos pelo banco; nos testes forcamos via reflexao.
    private static T WithId<T>(T entity, long id) where T : SyncBar.Domain.Primitives.Entity
    {
        typeof(SyncBar.Domain.Primitives.Entity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    [Fact]
    public async Task Handle_Manager_ShouldReturnAllFeatures()
    {
        SetupCatalog();

        var result = await CreateHandler().Handle(new GetMyFeaturesQuery(1, IsManager: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageAccess.Should().BeTrue();
        result.Value.Features.Should().BeEquivalentTo(["Salao", "Cardapio", "Estoque"]);
    }

    [Fact]
    public async Task Handle_Waiter_ShouldReturnUnionOfJobTitleAndPersonalGrants()
    {
        SetupCatalog();

        // Garcom (cargo 2): Salao + Cardapio pelo cargo; Estoque concedido so a ele.
        var user = WithId(AppUser.Create(1, 10, "garcom1", "g1@bar.com", "hash").Value, 5);
        _userRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(user);
        _employeeRepository.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(Employee.Create(1, 2, "Joao", "12345678901", null, null, DateTime.UtcNow, null, null).Value);
        _jobTitleFeatureRepository.GetByJobTitleAsync(2, Arg.Any<CancellationToken>())
            .Returns(new List<JobTitleFeature>
            {
                JobTitleFeature.Create(2, 1).Value,
                JobTitleFeature.Create(2, 2).Value,
            });
        _userFeatureRepository.GetByUserAsync(5, Arg.Any<CancellationToken>())
            .Returns(new List<AppUserFeature> { AppUserFeature.Create(5, 3).Value });

        var result = await CreateHandler().Handle(new GetMyFeaturesQuery(5, IsManager: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageAccess.Should().BeFalse();
        result.Value.Features.Should().BeEquivalentTo(["Salao", "Cardapio", "Estoque"]);
    }

    [Fact]
    public async Task Handle_UserWithoutEmployee_ShouldReturnOnlyPersonalGrants()
    {
        SetupCatalog();

        var user = WithId(AppUser.Create(1, null, "avulso", "a@bar.com", "hash").Value, 7);
        _userRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(user);
        _userFeatureRepository.GetByUserAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<AppUserFeature> { AppUserFeature.Create(7, 2).Value });

        var result = await CreateHandler().Handle(new GetMyFeaturesQuery(7, IsManager: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Features.Should().BeEquivalentTo(["Cardapio"]);
    }
}
