using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
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
        private readonly IBranchRepository _branchRepository;
        private readonly IAsaasService _asaasService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAsaasIntegrationPaymentCommandHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            IBranchRepository branchRepository,
            IAsaasService asaasService,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
            _asaasCustomerRepository = asaasCustomerRepository;
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

                    // 2. Aplica a credencial Asaas da filial/empresa (banco, com fallback pro appsettings)
                    await _asaasService.ConfigureForTenantAsync(branch.CompanyId, request.BranchId, cancellationToken);

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

                    if (string.IsNullOrWhiteSpace(asaasCustomerId))
                    {
                        return Result.Failure<CreateAsaasIntegrationPaymentResponse>(
                            Error.Validation("AsaasCustomer.NotFound", "O cliente não possui cadastro vinculado no Asaas para esta empresa."));
                    }

                    // 4. Emitir a cobrança no gateway Asaas via serviço de integração
                    AsaasPaymentResponse asaasPaymentData;
                    try
                    {
                        asaasPaymentData = await _asaasService.CreatePaymentAsync(
                            asaasCustomerId,
                            request.BillingType,
                            request.Value,
                            request.DueDate,
                            $"Pedido #{request.CustomerOrderId}",
                            request.CreditCardToken,
                            request.InstallmentCount,
                            cancellationToken);
                    }
                    catch (HttpRequestException ex)
                    {
                        return Result.Failure<CreateAsaasIntegrationPaymentResponse>(
                            Error.Failure("AsaasApi.CreatePaymentFailed", $"Falha ao criar cobrança no Asaas: {ex.Message}"));
                    }

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

                    // 6. Persiste já com o AsaasPaymentId — o Asaas dispara o webhook PAYMENT_CREATED
                    // assim que a cobrança é criada (passo 4), e se o registro só fosse salvo depois de
                    // buscar o QR Code (mais uma chamada ao Asaas), o webhook chegava antes do pagamento
                    // existir no banco e o ReceiveAsaasWebhookCommandHandler descartava o evento por não
                    // encontrar o pagamento (retornava sucesso sem registrar o log de auditoria).
                    await _paymentRepository.AddAsync(paymentEntity, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    // 7. Tratar retorno específico de PIX (QR Code e Copia e Cola)
                    string? pixQrCode = null;
                    string? pixPayload = null;
                    if (request.BillingType.Equals("PIX", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var qrCode = await _asaasService.GetPixQrCodeAsync(asaasPaymentData.Id, cancellationToken);
                            pixQrCode = qrCode.EncodedImage;
                            pixPayload = qrCode.Payload;
                            paymentEntity.SetPixDetails(pixQrCode, pixPayload);
                        }
                        catch (HttpRequestException)
                        {
                            // QR Code pode ser consultado novamente depois; não interrompe a criação da cobrança
                        }
                    }

                    // 8. URLs auxiliares e token de cartão
                    paymentEntity.SetUrls(asaasPaymentData.InvoiceUrl, asaasPaymentData.BankSlipUrl);
                    if (!string.IsNullOrWhiteSpace(request.CreditCardToken))
                    {
                        paymentEntity.SetCreditCardToken(request.CreditCardToken);
                    }

                    // 9. Persiste as atualizações (QR code, urls, token de cartão)
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
