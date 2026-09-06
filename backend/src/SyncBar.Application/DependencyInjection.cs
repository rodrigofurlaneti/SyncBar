using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Application.Features.Checkout.Shared;
using SyncBar.Application.Features.Integrations.Keeta.Authorization;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;

namespace SyncBar.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(configuration =>
                configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

            services.AddScoped<ICheckoutOrderPreparer, CheckoutOrderPreparer>();
            services.AddScoped<IKeetaAccessTokenProvider, KeetaAccessTokenProvider>();
            services.AddScoped<IKeetaOrderEventProcessor, KeetaOrderEventProcessor>();

            return services;
        }
    }
}


