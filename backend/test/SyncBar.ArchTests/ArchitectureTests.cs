using FluentAssertions;
using FluentValidation;
using NetArchTest.Rules;
using SyncBar.Domain.Primitives;
using Xunit;

namespace SyncBar.ArchTests;

public sealed class ArchitectureTests
{
    private const string ApplicationNamespace = "SyncBar.Application";
    private const string InfrastructureNamespace = "SyncBar.Infrastructure";
    private const string ApiNamespace = "SyncBar.API";

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Entity).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(SyncBar.Application.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(SyncBar.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOn_OuterLayersOrFrameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace,
                "MediatR", "Microsoft.EntityFrameworkCore", "FluentValidation")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_ShouldBe_InternalSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("CommandHandler")
            .Should().BeClasses().And().BeSealed().And().NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryHandlers_ShouldBe_InternalSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("QueryHandler")
            .Should().BeClasses().And().BeSealed().And().NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Repositories_ShouldBe_InternalSealed_InPersistence()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().HaveNameEndingWith("Repository")
            .Should().BeSealed().And().NotBePublic()
            .And().ResideInNamespace("SyncBar.Infrastructure.Persistence.Repositories")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void EfConfigurations_ShouldBe_InternalSealed()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().HaveNameEndingWith("Configuration")
            .And().ResideInNamespace("SyncBar.Infrastructure.Persistence.Configurations")
            .Should().BeSealed().And().NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Responses_ShouldBe_Sealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Response")
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Validators_ShouldInherit_AbstractValidator()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Validator")
            .Should().Inherit(typeof(AbstractValidator<>))
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandsAndQueries_ShouldResideIn_Features()
    {
        var commands = Types.InAssembly(ApplicationAssembly)
            .That().AreClasses().And().HaveNameEndingWith("Command")
            .Should().ResideInNamespace("SyncBar.Application.Features")
            .GetResult();

        var queries = Types.InAssembly(ApplicationAssembly)
            .That().AreClasses().And().HaveNameEndingWith("Query")
            .Should().ResideInNamespace("SyncBar.Application.Features")
            .GetResult();

        commands.IsSuccessful.Should().BeTrue();
        queries.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Entities_ShouldBe_Sealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace("SyncBar.Domain.Entities")
            .And().AreClasses()
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Aggregates_ShouldInherit_AggregateRoot()
    {
        var aggregates = new[]
        {
            typeof(SyncBar.Domain.Entities.Company), typeof(SyncBar.Domain.Entities.Branch),
            typeof(SyncBar.Domain.Entities.AppUser), typeof(SyncBar.Domain.Entities.CustomerOrder),
            typeof(SyncBar.Domain.Entities.Sale), typeof(SyncBar.Domain.Entities.CashSession),
            typeof(SyncBar.Domain.Entities.StockItem), typeof(SyncBar.Domain.Entities.Purchase)
        };

        foreach (var type in aggregates)
            type.Should().BeAssignableTo<AggregateRoot>();
    }

    [Fact]
    public void ChildEntities_ShouldInherit_Entity_NotAggregateRoot()
    {
        var children = new[]
        {
            typeof(SyncBar.Domain.Entities.OrderItem), typeof(SyncBar.Domain.Entities.SalePayment),
            typeof(SyncBar.Domain.Entities.CashMovement), typeof(SyncBar.Domain.Entities.StockMovement),
            typeof(SyncBar.Domain.Entities.PurchaseItem)
        };

        foreach (var type in children)
        {
            type.Should().BeAssignableTo<Entity>();
            type.Should().NotBeAssignableTo<AggregateRoot>();
        }
    }

    [Fact]
    public void RepositoryInterfaces_ShouldResideIn_DomainRepositories()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().AreInterfaces().And().HaveNameEndingWith("Repository")
            .Should().ResideInNamespace("SyncBar.Domain.Repositories")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
