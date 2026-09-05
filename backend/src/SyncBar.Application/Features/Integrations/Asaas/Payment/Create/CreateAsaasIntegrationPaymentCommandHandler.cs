using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Application.Abstractions.Integrations.Asaas;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Create
{
    internal sealed class CreateAsaasIntegrationPaymentCommandHandler
        : BaseCommandHandler<CreateAsaasIntegrationPaymentCommand, CreateAsaasIntegrationPaymentResponse>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;
        private readonly IAsaasIntegrationSettingRepository _settingRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IAsaasService _asaasService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAsaasIntegrationPaymentCommandHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            IAsaasIntegrationSettingRepository settingRepository,
            IBranchRepository branchRepository,
            IAsaasService asaasService,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
            _asaasCustomerRepository = asaasCustomerRepository;
            _settingRepository = settingRepository;
            _branchRepository = branchRepository;
            _asaasService = asaasService;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateAsaasIntegrationPaymentResponse>> Handle(
            CreateAsaasIntegrationPaymentCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateAsaasIntegrationPaymentCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // 1. Obter a Branch para resolver o CompanyId e validar existência
                    var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
                    if (branch is null)
                    {
                        return Result.Failure<CreateAsaasIntegrationPaymentResponse>(
                            Error.NotFound("Branch.NotFound", $"Filial com ID {request.BranchId} não foi encontrada."));
                    }

                    // 2. Obter as credenciais da integração Asaas (Filial ou fallback Empresa)
                    var setting = await _settingRepository.GetByBranchOrCompanyFallbackAsync(
                        branch.CompanyId,
                        request.BranchId,
                        cancellationToken);

                    if (setting is null || !setting.IsActive || string.IsNullOrWhiteSpace(setting.ApiKeyEncrypted))
                    {
                        return Result.Failure<CreateAsaasIntegrationPaymentResponse>(
                            Error.Validation("AsaasSetting.NotFound", "Configuração de integração Asaas não configurada ou inativa para esta unidade."));
                    }

                    // 3. Obter ou validar o CustomerId no Asaas
                    string? asaasCustomerId = null;
                    if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
                    {
                        var customerBinding = await _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(
                            request.CustomerId.Value,
                            branch.CompanyId,
                            cancellationToken);

                        asaasCustomerId = customerBinding?.AsaasCustomerId;
                    }

                    // 4. Emitir a cobrança no gateway Asaas via serviço de integração
                    var asaasResult = await _asaasService.CreatePaymentAsync(
                        setting,
                        request,
                        asaasCustomerId,
                        cancellationToken);

                    if (asaasResult.IsFailure)
                        return Result.Failure<CreateAsaasIntegrationPaymentResponse>(asaasResult.Error);

                    var asaasPaymentData = asaasResult.Value;

                    // 5. Criar a entidade de Domínio
                    var paymentEntityResult = AsaasIntegrationPayment.Create(
                        request.BranchId,
                        request.CustomerOrderId,
                        request.CustomerId,
                        asaasPaymentData.Id,
                        request.BillingType.ToUpperInvariant(),
                        request.Value,
                        request.DueDate,
                        request.InstallmentCount);

                    if (paymentEntityResult.IsFailure)
                        return Result.Failure<CreateAsaasIntegrationPaymentResponse>(paymentEntityResult.Error);

                    var paymentEntity = paymentEntityResult.Value;

                    // 6. Tratar retorno específico de PIX (QR Code e Copia e Cola)
                    string? pixQrCode = null;
                    string? pixPayload = null;
                    if (request.BillingType.Equals("PIX", StringComparison.OrdinalIgnoreCase))
                    {
                        var qrResult = await _asaasService.GetPixQrCodeAsync(setting, asaasPaymentData.Id, cancellationToken);
                        if (qrResult.IsSuccess)
                        {
                            pixQrCode = qrResult.Value.EncodedImage;
                            pixPayload = qrResult.Value.Payload;
                            paymentEntity.SetPixDetails(pixQrCode, pixPayload);
                        }
                    }

                    // 7. URLs auxiliares e token de cartão
                    paymentEntity.SetUrls(asaasPaymentData.InvoiceUrl, asaasPaymentData.BankSlipUrl);
                    if (!string.IsNullOrWhiteSpace(request.CreditCardToken))
                    {
                        paymentEntity.SetCreditCardToken(request.CreditCardToken);
                    }

                    // 8. Persistência
                    await _paymentRepository.AddAsync(paymentEntity, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateAsaasIntegrationPaymentResponse(
                        paymentEntity.Id,
                        paymentEntity.AsaasPaymentId,
                        paymentEntity.Status,
                        pixQrCode,
                        pixPayload,
                        paymentEntity.InvoiceUrl,
                        paymentEntity.BankSlipUrl);

                    return Result.Success(response);
                });
        }
    }
}
