using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Application;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Keeta.Authorization;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;
using SyncBar.Application.Features.Storefront.AddOrder;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class DependencyInjectionTests
{
    // Nota: este teste verifica os registros diretamente na ServiceCollection (descriptors) em vez
    // de chamar BuildServiceProvider() + GetService(...). AddMediatR/AddValidatorsFromAssembly
    // registram automaticamente TODOS os handlers/validators do assembly inteiro, que por sua vez
    // dependem de dezenas de repositórios/serviços de infraestrutura (DbContext, provedores de
    // token, clientes HTTP de integração, etc.) que não fazem sentido montar aqui só para provar
    // que a composição está correta — isso duplicaria testes de integração sem agregar valor.
    // Conferir os ServiceDescriptor's é suficiente para garantir que AddApplication() está
    // registrando o que deveria, sem precisar resolver a árvore de dependências inteira.

    [Fact]
    public void AddApplication_ShouldReturnSameServiceCollectionInstance()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddApplication_ShouldRegisterCheckoutOrderPreparerAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ICheckoutOrderPreparer) &&
            d.ImplementationType == typeof(CheckoutOrderPreparer) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplication_ShouldRegisterKeetaAccessTokenProviderAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IKeetaAccessTokenProvider) &&
            d.ImplementationType == typeof(KeetaAccessTokenProvider) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplication_ShouldRegisterKeetaOrderEventProcessorAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IKeetaOrderEventProcessor) &&
            d.ImplementationType == typeof(KeetaOrderEventProcessor) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplication_ShouldRegisterMediatRSender()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.Should().Contain(d => d.ServiceType == typeof(ISender));
        services.Should().Contain(d => d.ServiceType == typeof(IMediator));
    }

    [Fact]
    public void AddApplication_ShouldRegisterValidatorsFromApplicationAssembly()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        // AddValidatorsFromAssembly(includeInternalTypes: true) varre o assembly inteiro — conferimos
        // um validator concreto conhecido (internal) pra provar que a varredura pegou tipos internos.
        services.Should().Contain(d =>
            d.ServiceType == typeof(IValidator<AddWebStorefrontOrderCommand>));
    }
}
