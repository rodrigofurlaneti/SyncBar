using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId
{
    internal sealed class GetByCustomerIdAndCompanyIdQueryHandler
        : BaseQueryHandler<GetByCustomerIdAndCompanyIdQuery, AsaasIntegrationCustomerResponse>
    {
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;

        public GetByCustomerIdAndCompanyIdQueryHandler(
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _asaasCustomerRepository = asaasCustomerRepository;
        }

        public override async Task<Result<AsaasIntegrationCustomerResponse>> Handle(
            GetByCustomerIdAndCompanyIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByCustomerIdAndCompanyIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var customer = await _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(
                        request.CustomerId,
                        request.CompanyId,
                        cancellationToken);

                    if (customer is null)
                    {
                        return Result.Failure<AsaasIntegrationCustomerResponse>(
                            Error.NotFound(
                                "AsaasCustomer.NotFound",
                                $"Vínculo do cliente {request.CustomerId} para a empresa {request.CompanyId} não foi encontrado."));
                    }

                    var response = new AsaasIntegrationCustomerResponse(
                        customer.Id,
                        customer.CustomerId,
                        customer.CompanyId,
                        customer.AsaasCustomerId,
                        customer.CreatedAt,
                        customer.UpdatedAt,
                        customer.IsActive);

                    return Result.Success(response);
                });
        }
    }
}
